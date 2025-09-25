using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public interface IWarehouseService : IGenericService<ProductStock>
{
    Task<ReserveResult> ReserveForOrderAsync(int orderId, int? preferredBranchId = null, CancellationToken ct = default);
    Task FulfillReservationsAsync(int orderId, CancellationToken ct = default);
    Task ReleaseReservationsForOrderAsync(int orderId, CancellationToken ct = default);
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

        var perItemReport = new List<ReserveLineResult>();

        foreach (var item in order.Items)
        {
            var need = item.Quantity - GetAlreadyReservedQty(item.Id);
            if (need <= 0) { perItemReport.Add(ReserveLineResult.Done(item.Id, 0, 0)); continue; }
            
            var stocks = await _dbSet
                .Where(s => s.ProductId == item.ProductId)
                .Select(s => new {
                    s, Available = s.Quantity - s.ReservedQuantity,
                    Priority = (preferredBranchId.HasValue && s.BranchId == preferredBranchId.Value) ? 0 : 1
                })
                .Where(x => x.Available > 0)
                .OrderBy(x => x.Priority).ThenByDescending(x => x.Available)
                .ToListAsync(ct);

            var reservedHere = 0;

            foreach (var x in stocks)
            {
                if (need <= 0) break;
                var take = Math.Min(need, x.Available);
                
                x.s.ReservedQuantity += take;
                x.s.LastUpdatedAt = DateTime.UtcNow;

                context.StockReservations.Add(new StockReservation {
                    OrderItemId = item.Id,
                    ProductStockId = x.s.Id,
                    Quantity = take,
                    Status = ReservationStatus.Active
                });

                reservedHere += take;
                need -= take;
            }

            perItemReport.Add(new ReserveLineResult(item.Id, reservedHere, need));
        }

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var isPartial = perItemReport.Any(r => r.MissingQuantity > 0);
        // Tu możesz zaktualizować status zamówienia: RESERVED/PARTIALLY_RESERVED
        return new ReserveResult(perItemReport, isPartial);
    }

    public async Task FulfillReservationsAsync(int orderId, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var reservations = await context.StockReservations
            .Include(r => r.ProductStock)
            .Include(r => r.OrderItem)
            .Where(r => r.OrderItem.OrderId == orderId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        foreach (var r in reservations)
        {
            var stock = r.ProductStock;

            // Wydanie: OUT = -Quantity, zdejmujemy i z OnHand, i z Reserved
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

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Tu możesz przeliczyć i podnieść status zamówienia: FULFILLED / PARTIALLY_FULFILLED
    }

    public async Task ReleaseReservationsForOrderAsync(int orderId, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var reservations = await context.StockReservations
            .Include(r => r.ProductStock)
            .Where(r => r.OrderItem.OrderId == orderId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        foreach (var r in reservations)
        {
            r.ProductStock.ReservedQuantity -= r.Quantity;
            r.ProductStock.LastUpdatedAt     = DateTime.UtcNow;

            r.Status     = ReservationStatus.Released;
            r.ReleasedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private int GetAlreadyReservedQty(int orderItemId) =>
        context.StockReservations
            .Where(r => r.OrderItemId == orderItemId && r.Status == ReservationStatus.Active)
            .Sum(r => (int?)r.Quantity) ?? 0;
}

public record ReserveResult(IReadOnlyList<ReserveLineResult> Lines, bool IsPartial);
public record ReserveLineResult(int OrderItemId, int ReservedQuantity, int MissingQuantity)
{
    public static ReserveLineResult Done(int orderItemId, int reserved, int missing) =>
        new(orderItemId, reserved, missing);
}