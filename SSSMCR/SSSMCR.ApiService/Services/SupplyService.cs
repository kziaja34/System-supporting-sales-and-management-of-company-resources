using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public interface ISupplyService : IGenericService<SupplyOrder>
{
    Task<SupplyOrder> CreateOrderAsync(int supplierId, int branchId, List<(int productId, int quantity)> items, CancellationToken ct);
    Task<List<SupplyOrder>> GetOrdersAsync(CancellationToken ct);
    Task<SupplyOrder> GetOrderByIdAsync(int orderId, CancellationToken ct);
    Task ReceiveOrderAsync(int orderId, CancellationToken ct);
}

public class SupplyService(AppDbContext context)
    : GenericService<SupplyOrder>(context), ISupplyService
{
    

    public async Task<SupplyOrder> CreateOrderAsync(int supplierId, int branchId, List<(int productId, int quantity)> items, CancellationToken ct)
    {
        _ = await _context.Suppliers.FindAsync(new object[] { supplierId }, ct)
            ?? throw new InvalidOperationException("Supplier not found");

        _ = await _context.Branches.FindAsync(new object[] { branchId }, ct)
            ?? throw new InvalidOperationException("Branch not found");

        var allowedProductIds = await _context.SupplierProducts
            .Where(sp => sp.SupplierId == supplierId)
            .Select(sp => sp.ProductId)
            .ToListAsync(ct);

        var requestedProductIds = items.Select(i => i.productId).Distinct().ToList();
        var notAllowed = requestedProductIds.Except(allowedProductIds).ToList();
        if (notAllowed.Any())
        {
            throw new InvalidOperationException(
                $"Selected supplier does not provide products with IDs: {string.Join(", ", notAllowed)}");
        }
        
        var order = new SupplyOrder
        {
            SupplierId = supplierId,
            BranchId = branchId,
            Status = SupplyOrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var (productId, qty) in items)
        {
            order.Items.Add(new SupplyItem
            {
                ProductId = productId,
                Quantity = qty
            });
        }

        _context.SupplyOrders.Add(order);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(order)
            .Collection(o => o.Items)
            .Query()
            .Include(i => i.Product)
            .ToListAsync(ct);

        return order;
    }

    public async Task<List<SupplyOrder>> GetOrdersAsync(CancellationToken ct)
    {
        return await _context.SupplyOrders
            .Include(o => o.Supplier)
            .Include(o => o.Branch)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<SupplyOrder> GetOrderByIdAsync(int orderId, CancellationToken ct)
    {
        return await _context.SupplyOrders
            .Include(o => o.Supplier)
            .Include(o => o.Branch)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct) ?? throw new KeyNotFoundException("Supply order not found");
    }

    public async Task ReceiveOrderAsync(int orderId, CancellationToken ct)
    {
        var order = await _context.SupplyOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) throw new InvalidOperationException("Supply order not found");
        if (order.Status != SupplyOrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be received");

        foreach (var item in order.Items)
        {
            var stock = await _context.ProductStock
                .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.BranchId == order.BranchId, ct);

            if (stock == null)
            {
                stock = new ProductStock
                {
                    ProductId = item.ProductId,
                    BranchId = order.BranchId,
                    Quantity = item.Quantity,
                    CriticalThreshold = 0,
                    LastUpdatedAt = DateTime.UtcNow
                };
                _context.ProductStock.Add(stock);
            }
            else
            {
                stock.Quantity += item.Quantity;
                stock.LastUpdatedAt = DateTime.UtcNow;
            }

            _context.StockMovements.Add(new StockMovement
            {
                ProductStockId = stock.Id,
                QuantityDelta = item.Quantity,
                Type = StockMovementType.Inbound,
                Reference = $"SUPPLY ORDER#{order.Id}/BRANCH#{order.BranchId}"
            });
        }

        order.Status = SupplyOrderStatus.Received;
        order.ReceivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
    }
}
