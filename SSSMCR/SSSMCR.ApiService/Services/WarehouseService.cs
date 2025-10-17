using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Exceptions;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public interface IWarehouseService : IGenericService<ProductStock>
{
    Task<ReserveResult> ReserveForOrderAsync(int orderId, int currentUserId, int? preferredBranchId = null, CancellationToken ct = default);
    Task FulfillSingleReservationAsync(int orderId, int reservationId, CancellationToken ct = default);
    Task FulfillReservationForBranchAsync(int orderId, int branchId, CancellationToken ct = default);
    Task ReleaseReservationsForOrderAsync(int orderId, bool confirm, CancellationToken ct = default);
    Task<List<ProductStockDto>> GetStocksAsync(int? branchId, CancellationToken ct);

    Task UpdateDynamicCriticalThresholdsAsync(CancellationToken ct = default);
}

public class WarehouseService(AppDbContext context) : GenericService<ProductStock>(context), IWarehouseService
{
    public async Task<ReserveResult> ReserveForOrderAsync(int orderId, int currentUserId, int? preferredBranchId, CancellationToken ct)
{
    await using var tx = await _context.Database.BeginTransactionAsync(ct);

    var order = await _context.Orders
        .Include(o => o.Items)
        .ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(o => o.Id == orderId, ct)
        ?? throw new InvalidOperationException("Order not found.");

    if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
        throw new InvalidOperationException($"Cannot reserve order in status {order.Status}.");

    if (order.Status == OrderStatus.PartiallyFulfilled)
    {
        var releasedReservations = await _context.StockReservations
            .Where(r => r.OrderItem.OrderId == orderId && r.Status == ReservationStatus.Released)
            .ToListAsync(ct);

        foreach (var r in releasedReservations)
        {
            r.Status = ReservationStatus.Active;
            r.CreatedAt = DateTime.UtcNow;
        }
    }

    var needs = order.Items
        .Select(i => new { Item = i, Need = i.Quantity - GetAlreadyReservedQty(i.Id) })
        .Where(x => x.Need > 0)
        .ToList();

    if (needs.Count == 0)
    {
        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new ReserveResult(new List<ReserveLineResult>(), false);
    }

    double? originLat = null, originLon = null;
    if (preferredBranchId.HasValue)
    {
        var origin = await _context.Branches
            .Where(b => b.Id == preferredBranchId.Value)
            .Select(b => new { b.Latitude, b.Longitude })
            .FirstOrDefaultAsync(ct);
        originLat = origin?.Latitude;
        originLon = origin?.Longitude;
    }

    var productIds = needs.Select(n => n.Item.ProductId).Distinct().ToList();

    var stocks = await _context.ProductStock
        .Include(ps => ps.Branch)
        .Where(ps => productIds.Contains(ps.ProductId))
        .Select(ps => new
        {
            ps.BranchId,
            ps.Branch,
            ps.ProductId,
            Available = ps.Quantity - ps.ReservedQuantity,
            Lat = ps.Branch!.Latitude,
            Lon = ps.Branch.Longitude
        })
        .ToListAsync(ct);

    var candidates = stocks
        .GroupBy(s => s.BranchId)
        .Select(g => new
        {
            BranchId = g.Key,
            Branch = g.First().Branch,
            Availability = g.ToDictionary(x => x.ProductId, x => x.Available),
            Lat = g.First().Lat,
            Lon = g.First().Lon
        })
        .ToList();

    bool CoversAll(Dictionary<int, int> availability)
    {
        foreach (var n in needs)
        {
            if (!availability.TryGetValue(n.Item.ProductId, out int avail) || avail < n.Need)
                return false;
        }
        return true;
    }

    int CoverageScore(Dictionary<int, int> availability)
    {
        var sum = 0;
        foreach (var n in needs)
        {
            availability.TryGetValue(n.Item.ProductId, out int a);
            sum += Math.Min(a, n.Need);
        }
        return sum;
    }

    var chosen = preferredBranchId.HasValue
        ? candidates.FirstOrDefault(c => c.BranchId == preferredBranchId.Value && CoversAll(c.Availability))
        : null;

    chosen ??= candidates
        .Where(c => CoversAll(c.Availability))
        .OrderBy(c => (originLat.HasValue && originLon.HasValue && c.Lat.HasValue && c.Lon.HasValue)
            ? DistanceKm(originLat.Value, originLon.Value, c.Lat.Value, c.Lon.Value)
            : double.MaxValue)
        .ThenByDescending(c => CoverageScore(c.Availability))
        .FirstOrDefault();

    if (chosen == null)
    {
        var report = needs.Select(n => new ReserveLineResult(n.Item.Product.Name, "—", 0, n.Need)).ToList();
        await tx.RollbackAsync(ct);
        return new ReserveResult(report, true);
    }

    var perItemReport = new List<ReserveLineResult>();
    var now = DateTime.UtcNow;

    foreach (var n in needs)
    {
        var stock = await _context.ProductStock
            .FirstOrDefaultAsync(ps => ps.BranchId == chosen.BranchId && ps.ProductId == n.Item.ProductId, ct);

        if (stock == null)
        {
            stock = new ProductStock
            {
                ProductId = n.Item.ProductId,
                BranchId = chosen.BranchId,
                Quantity = 0,
                ReservedQuantity = 0,
                LastUpdatedAt = now
            };
            _context.ProductStock.Add(stock);
            await _context.SaveChangesAsync(ct);
        }

        stock.ReservedQuantity += n.Need;
        stock.LastUpdatedAt = now;

        var existingReservation = await _context.StockReservations
            .FirstOrDefaultAsync(r =>
                r.OrderItemId == n.Item.Id &&
                r.ProductStockId == stock.Id &&
                (r.Status == ReservationStatus.Released || r.Status == ReservationStatus.Active), ct);

        if (existingReservation != null)
        {
            existingReservation.Status = ReservationStatus.Active;
            existingReservation.Quantity = n.Need;
            existingReservation.CreatedAt = now;
        }
        else
        {
            _context.StockReservations.Add(new StockReservation
            {
                OrderItemId = n.Item.Id,
                ProductStockId = stock.Id,
                Quantity = n.Need,
                Status = ReservationStatus.Active,
                CreatedAt = now
            });
        }

        perItemReport.Add(new ReserveLineResult(n.Item.Product.Name, chosen.Branch?.Name ?? "Unknown", n.Need, 0));
    }

    order.Status = OrderStatus.Processing;

    if (order.BranchId == null)
    {
        var userBranchId = await _context.Users
            .Where(u => u.Id == currentUserId)
            .Select(u => u.BranchId)
            .FirstOrDefaultAsync(ct);

        if (userBranchId != 0)
            order.BranchId = userBranchId;
        else if (preferredBranchId.HasValue)
            order.BranchId = preferredBranchId;
        else
            order.BranchId = chosen.BranchId;
    }

    await _context.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return new ReserveResult(perItemReport, false);
}


    public async Task FulfillSingleReservationAsync(int orderId, int reservationId, CancellationToken ct = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);

        var order = await _context.Orders.FindAsync(orderId, ct);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Pending)
            throw new InvalidOperationException("You can only fulfill processing orders.");

        var reservation = await _context.StockReservations
            .Include(r => r.ProductStock)
            .Include(r => r.OrderItem)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.OrderItem.OrderId == orderId && r.Status == ReservationStatus.Active, ct);

        if (reservation == null)
            throw new ReservationNotFoundException(orderId);

        var stock = reservation.ProductStock;

        stock.Quantity         -= reservation.Quantity;
        stock.ReservedQuantity -= reservation.Quantity;
        stock.LastUpdatedAt     = DateTime.UtcNow;

        _context.StockMovements.Add(new StockMovement
        {
            ProductStockId = stock.Id,
            QuantityDelta  = -reservation.Quantity,
            Type           = StockMovementType.Outbound,
            OrderItemId    = reservation.OrderItemId,
            Reference      = $"ORDER#{orderId}/RES#{reservation.Id}"
        });

        reservation.Status      = ReservationStatus.Fulfilled;
        reservation.FulfilledAt = DateTime.UtcNow;

        var remaining = await _context.StockReservations
            .Where(r => r.OrderItem.OrderId == orderId && r.Status != ReservationStatus.Released)
            .ToListAsync(ct);

        if (remaining.All(r => r.Status == ReservationStatus.Fulfilled))
            order.Status = OrderStatus.Completed;
        else if (remaining.Any(r => r.Status == ReservationStatus.Fulfilled))
            order.Status = OrderStatus.PartiallyFulfilled;
        else
            order.Status = OrderStatus.Processing;

        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    
    public async Task FulfillReservationForBranchAsync(int orderId, int branchId, CancellationToken ct)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);

        var order = await _context.Orders.FindAsync(orderId, ct);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Pending)
            throw new InvalidOperationException("You can only fulfill processing orders.");

        var reservations = await _context.StockReservations
            .Include(r => r.ProductStock)
            .Include(r => r.OrderItem)
            .Where(r => r.OrderItem.OrderId == orderId 
                        && r.Status == ReservationStatus.Active
                        && r.ProductStock.BranchId == branchId)
            .ToListAsync(ct);

        if (!reservations.Any())
            throw new ReservationNotFoundException(orderId);


        foreach (var r in reservations)
        {
            var stock = r.ProductStock;

            stock.Quantity        -= r.Quantity;
            stock.ReservedQuantity -= r.Quantity;
            stock.LastUpdatedAt    = DateTime.UtcNow;

            _context.StockMovements.Add(new StockMovement
            {
                ProductStockId = stock.Id,
                QuantityDelta  = -r.Quantity,
                Type           = StockMovementType.Outbound,
                OrderItemId    = r.OrderItemId,
                Reference      = $"ORDER#{orderId}/BRANCH#{branchId}"
            });

            r.Status      = ReservationStatus.Fulfilled;
            r.FulfilledAt = DateTime.UtcNow;
        }

        var allReservations = await _context.StockReservations
            .Where(r => r.OrderItem.OrderId == orderId && r.Status != ReservationStatus.Released)
            .ToListAsync(ct);
        
        if (allReservations.All(r => r.Status == ReservationStatus.Fulfilled))
        {
            order.Status = OrderStatus.Completed;
        }
        else if (allReservations.Any(r => r.Status == ReservationStatus.Fulfilled))
        {
            order.Status = OrderStatus.PartiallyFulfilled;
        }
        else
        {
            order.Status = OrderStatus.Processing;
        }
        
        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        
    }


    public async Task ReleaseReservationsForOrderAsync(int orderId, bool confirm, CancellationToken ct)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);

        var order = await _context.Orders.FindAsync(orderId, ct);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot release a completed order.");

        var reservations = await _context.StockReservations
            .Include(r => r.ProductStock)
            .Where(r => r.OrderItem.OrderId == orderId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        if (!reservations.Any())
            throw new ReservationNotFoundException(orderId);


        var allReservations = await _context.StockReservations
            .Where(r => r.OrderItem.OrderId == orderId)
            .ToListAsync(ct);

        var anyFulfilled = allReservations.Any(r => r.Status == ReservationStatus.Fulfilled);

        if (anyFulfilled && !confirm)
            throw new PartialReleaseConfirmationRequiredException(orderId);

        foreach (var r in reservations)
        {
            r.ProductStock.ReservedQuantity -= r.Quantity;
            r.ProductStock.LastUpdatedAt = DateTime.UtcNow;

            r.Status = ReservationStatus.Released;
            r.ReleasedAt = DateTime.UtcNow;
        }

        if (anyFulfilled)
            order.Status = OrderStatus.PartiallyFulfilled;
        else
            order.Status = OrderStatus.Pending;


        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
    
    public async Task<List<ProductStockDto>> GetStocksAsync(int? branchId, CancellationToken ct)
    {
        var query = _dbSet
            .Include(ps => ps.Product)
            .Include(ps => ps.Branch)
            .AsQueryable();

        if (branchId.HasValue)
        {
            var exists = await _context.Branches.AnyAsync(b => b.Id == branchId.Value, ct);
            if (!exists)
                throw new BranchNotFoundException(branchId.Value);
            
            query = query.Where(ps => ps.BranchId == branchId.Value);
        }
        
        return await query
            .Select(ps => new ProductStockDto
            {
                ProductId = ps.ProductId,
                ProductName = ps.Product!.Name,
                BranchId = ps.BranchId,
                BranchName = ps.Branch!.Name,
                Quantity = ps.Quantity,
                ReservedQuantity = ps.ReservedQuantity,
                CriticalThreshold = ps.CriticalThreshold,
                LastUpdatedAt = ps.LastUpdatedAt
            })
            .ToListAsync(ct);
    }
    
    public async Task UpdateDynamicCriticalThresholdsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthAgo = now.AddDays(-30);

        var stocks = await _context.ProductStock
            .Include(s => s.Product)
            .Include(s => s.Branch)
            .ToListAsync(ct);

        foreach (var stock in stocks)
        {
            var movements = await _context.StockMovements
                .Where(m => m.ProductStockId == stock.Id &&
                            m.Type == StockMovementType.Outbound &&
                            m.CreatedAt >= monthAgo)
                .ToListAsync(ct);

            var avgDailyOrders = movements
                .GroupBy(m => m.CreatedAt.Date)
                .Select(g => g.Sum(m => Math.Abs(m.QuantityDelta)))
                .DefaultIfEmpty(0)
                .Average();

            var dynamicPart = (int)Math.Ceiling(avgDailyOrders * 2);

            if (stock.Product != null) stock.CriticalThreshold = stock.Product.BaseCriticalThreshold + dynamicPart;
            stock.LastUpdatedAt = now;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double r = 6371.0; // km
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return r * c;
    }

    private int GetAlreadyReservedQty(int orderItemId) =>
        _context.StockReservations
            .Where(r => r.OrderItemId == orderItemId 
                        && (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled))
            .Sum(r => (int?)r.Quantity) ?? 0;
}