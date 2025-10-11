using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Exceptions;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public interface IWarehouseService : IGenericService<ProductStock>
{
    Task<ReserveResult> ReserveForOrderAsync(int orderId, int currentUserId, int? preferredBranchId = null, CancellationToken ct = default);
    Task FulfillReservationsAsync(int orderId, CancellationToken ct = default);
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

        var perItemReport = new List<ReserveLineResult>();

        foreach (var item in order.Items)
        {
            var need = item.Quantity - GetAlreadyReservedQty(item.Id);
            if (need <= 0)
                continue;

            var stocks = await _dbSet
                .Include(s => s.Branch)
                .Where(s => s.ProductId == item.ProductId)
                .Select(s => new
                {
                    s,
                    Available = s.Quantity - s.ReservedQuantity,
                    Priority = (preferredBranchId.HasValue && s.BranchId == preferredBranchId.Value) ? 0 : 1,
                    Lat = s.Branch.Latitude,
                    Lon = s.Branch.Longitude
                })
                .Where(x => x.Available > 0)
                .ToListAsync(ct);

            var sortedStocks = (originLat.HasValue && originLon.HasValue)
                ? stocks
                    .OrderBy(x => x.Priority)
                    .ThenBy(x => (x.Lat.HasValue && x.Lon.HasValue)
                        ? DistanceKm(originLat.Value, originLon.Value, x.Lat.Value, x.Lon.Value)
                        : double.MaxValue)
                    .ThenByDescending(x => x.Available)
                    .ToList()
                : stocks
                    .OrderBy(x => x.Priority)
                    .ThenByDescending(x => x.Available)
                    .ToList();
            
            foreach (var x in sortedStocks)
            {
                if (need <= 0) break;
                var take = Math.Min(need, x.Available);

                x.s.ReservedQuantity += take;
                x.s.LastUpdatedAt = DateTime.UtcNow;

                var existingReservation = await _context.StockReservations
                    .FirstOrDefaultAsync(r =>
                        r.OrderItemId == item.Id &&
                        r.ProductStockId == x.s.Id &&
                        (r.Status == ReservationStatus.Released || r.Status == ReservationStatus.Active), ct);

                if (existingReservation != null)
                {
                    existingReservation.Status = ReservationStatus.Active;
                    existingReservation.Quantity = take;
                    existingReservation.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    _context.StockReservations.Add(new StockReservation
                    {
                        OrderItemId = item.Id,
                        ProductStockId = x.s.Id,
                        Quantity = take,
                        Status = ReservationStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                perItemReport.Add(new ReserveLineResult(
                    item.Id,
                    item.Product.Name,
                    x.s.Branch.Name ?? "Unknown",
                    take,
                    need - take
                ));

                need -= take;
            }
            
            if (need > 0)
            {
                perItemReport.Add(new ReserveLineResult(
                    item.Id,
                    item.Product.Name,
                    "—",
                    0,
                    need
                ));
            }
        }

        var isPartial = perItemReport.Any(r => r.MissingQuantity > 0);
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
        }
        
        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new ReserveResult(perItemReport, isPartial);
    }


    public async Task FulfillReservationsAsync(int orderId, CancellationToken ct)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);

        var order = await _context.Orders.FindAsync(orderId, ct);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Pending)
            throw new InvalidOperationException("You can only fulfill processing orders.");

        var reservations = await _context.StockReservations
            .Include(r => r.ProductStock)
            .Include(r => r.OrderItem)
            .Where(r => r.OrderItem.OrderId == orderId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        if (!reservations.Any())
            throw new ReservationNotFoundException(orderId);


        foreach (var r in reservations)
        {
            var stock = r.ProductStock;
            
            stock.Quantity        -= r.Quantity;
            stock.ReservedQuantity -= r.Quantity;
            stock.LastUpdatedAt    = DateTime.UtcNow;

            _context.StockMovements.Add(new StockMovement {
                ProductStockId = stock.Id,
                QuantityDelta  = -r.Quantity,
                Type           = StockMovementType.Outbound,
                OrderItemId    = r.OrderItemId,
                Reference      = $"ORDER#{orderId}"
            });

            r.Status      = ReservationStatus.Fulfilled;
            r.FulfilledAt = DateTime.UtcNow;
        }
        
        var allReservations = await _context.StockReservations
            .Where(r => r.OrderItem.OrderId == orderId)
            .ToListAsync(ct);

        if (allReservations.All(r => r.Status == ReservationStatus.Fulfilled))
        {
            order.Status = OrderStatus.Completed;
        }

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
                ProductName = ps.Product.Name,
                BranchId = ps.BranchId,
                BranchName = ps.Branch.Name,
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

            stock.CriticalThreshold = stock.Product.BaseCriticalThreshold + dynamicPart;
            stock.LastUpdatedAt = now;
        }

        await _context.SaveChangesAsync(ct);
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // km
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private int GetAlreadyReservedQty(int orderItemId) =>
        _context.StockReservations
            .Where(r => r.OrderItemId == orderItemId 
                        && (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled))
            .Sum(r => (int?)r.Quantity) ?? 0;
}