using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Exceptions;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public interface IWarehouseService : IGenericService<ProductStock>
{
    Task<ReserveResult> ReserveForOrderAsync(int orderId, int? preferredBranchId = null, CancellationToken ct = default);
    Task FulfillReservationsAsync(int orderId, CancellationToken ct = default);
    Task FulfillReservationForBranchAsync(int orderId, int branchId, CancellationToken ct = default);
    Task ReleaseReservationsForOrderAsync(int orderId, bool confirm, CancellationToken ct = default);
}

public class WarehouseService(AppDbContext context) : GenericService<ProductStock>(context), IWarehouseService
{
    public async Task<ReserveResult> ReserveForOrderAsync(int orderId, int? preferredBranchId, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var order = await context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException($"Cannot reserve order in status {order.Status}.");
        
        if (order.Status == OrderStatus.PartiallyFulfilled)
        {
            var releasedReservations = await context.StockReservations
                .Where(r => r.OrderItem.OrderId == orderId && r.Status == ReservationStatus.Released)
                .ToListAsync(ct);

            foreach (var r in releasedReservations)
            {
                r.Status = ReservationStatus.Active;
                r.CreatedAt = DateTime.UtcNow;
            }
        }

        var perItemReport = new List<ReserveLineResult>();

        foreach (var item in order.Items)
        {
            var need = item.Quantity - GetAlreadyReservedQty(item.Id);
            if (need <= 0)
            {
                perItemReport.Add(ReserveLineResult.Done(item.Id, 0, 0));
                continue;
            }

            var stocks = await _dbSet
                .Where(s => s.ProductId == item.ProductId)
                .Select(s => new
                {
                    s,
                    Available = s.Quantity - s.ReservedQuantity,
                    Priority = (preferredBranchId.HasValue && s.BranchId == preferredBranchId.Value) ? 0 : 1
                })
                .Where(x => x.Available > 0)
                .OrderBy(x => x.Priority)
                .ThenByDescending(x => x.Available)
                .ToListAsync(ct);

            var reservedHere = 0;

            foreach (var x in stocks)
            {
                if (need <= 0) break;
                var take = Math.Min(need, x.Available);

                x.s.ReservedQuantity += take;
                x.s.LastUpdatedAt = DateTime.UtcNow;

                var existingReservation = await context.StockReservations
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
                    context.StockReservations.Add(new StockReservation
                    {
                        OrderItemId = item.Id,
                        ProductStockId = x.s.Id,
                        Quantity = take,
                        Status = ReservationStatus.Active,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                
                reservedHere += take;
                need -= take;
            }


            perItemReport.Add(new ReserveLineResult(item.Id, reservedHere, need));
        }

        var isPartial = perItemReport.Any(r => r.MissingQuantity > 0);
        order.Status = OrderStatus.Processing;

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new ReserveResult(perItemReport, isPartial);
    }


    public async Task FulfillReservationsAsync(int orderId, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var order = await context.Orders.FindAsync(orderId, ct);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Pending)
            throw new InvalidOperationException("You can only fulfill processing orders.");

        var reservations = await context.StockReservations
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

            context.StockMovements.Add(new StockMovement {
                ProductStockId = stock.Id,
                QuantityDelta  = -r.Quantity,
                Type           = StockMovementType.Outbound,
                OrderItemId    = r.OrderItemId,
                Reference      = $"ORDER#{orderId}"
            });

            r.Status      = ReservationStatus.Fulfilled;
            r.FulfilledAt = DateTime.UtcNow;
        }
        
        var allReservations = await context.StockReservations
            .Where(r => r.OrderItem.OrderId == orderId)
            .ToListAsync(ct);

        if (allReservations.All(r => r.Status == ReservationStatus.Fulfilled))
        {
            order.Status = OrderStatus.Completed;
        }

        await context.SaveChangesAsync(ct);
        
        await tx.CommitAsync(ct);
    }
    
    public async Task FulfillReservationForBranchAsync(int orderId, int branchId, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var order = await context.Orders.FindAsync(orderId, ct);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Pending)
            throw new InvalidOperationException("You can only fulfill processing orders.");

        var reservations = await context.StockReservations
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

            context.StockMovements.Add(new StockMovement
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

        var allReservations = await context.StockReservations
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
        
        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        
    }


    public async Task ReleaseReservationsForOrderAsync(int orderId, bool confirm, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var order = await context.Orders.FindAsync(orderId, ct);
        if (order == null) throw new InvalidOperationException("Order not found.");
        if (order.Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot release a completed order.");

        var reservations = await context.StockReservations
            .Include(r => r.ProductStock)
            .Where(r => r.OrderItem.OrderId == orderId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        if (!reservations.Any())
            throw new ReservationNotFoundException(orderId);


        var allReservations = await context.StockReservations
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


        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private int GetAlreadyReservedQty(int orderItemId) =>
        context.StockReservations
            .Where(r => r.OrderItemId == orderItemId 
                        && (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled))
            .Sum(r => (int?)r.Quantity) ?? 0;
}