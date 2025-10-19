using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public interface IOrderService : IGenericService<Order>
{
    Task<IEnumerable<Order>> GetAllAsync(string? status = null, string? email = null, CancellationToken ct = default);

    Task<PageResponse<OrderListItemDto>> GetPagedAsync(
        int page, int size, string sort, string? search = null, string? importance = null, CancellationToken ct = default);
    
    Task<bool> UpdateStatusAsync(int id, string newStatus);
    double CalculatePriority(Order order, IEnumerable<Order> allOrders);
}

public class OrderService(AppDbContext context, FuzzyPriorityEvaluatorService fuzzyService) : GenericService<Order>(context), IOrderService
{
    public async Task<IEnumerable<Order>> GetAllAsync(string? status = null, string? email = null, CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        if (!string.IsNullOrEmpty(email))
        {
            query = query.Where(o => o.CustomerEmail.Contains(email));
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public new async Task<Order> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var order = await _dbSet
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
        
        if (order == null) throw new KeyNotFoundException("Order not found.");
        
        order.Priority = CalculatePriority(order, await GetAllAsync(ct));
        
        return order;
    }
    
    public async Task<PageResponse<OrderListItemDto>> GetPagedAsync(
        int page, int size, string sort, string? search = null, string? importance = null, CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .AsQueryable();
        
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(o =>
                o.CustomerName.Contains(search) ||
                o.CustomerEmail.Contains(search));
        }
        
        query = sort switch
        {
            "createdAt,asc" => query.OrderBy(o => o.CreatedAt),
            "createdAt,desc" => query.OrderByDescending(o => o.CreatedAt),
            "id,asc" => query.OrderBy(o => o.Id),
            "id,desc" => query.OrderByDescending(o => o.Id),
            "customerEmail,asc" => query.OrderBy(o => o.CustomerEmail),
            "customerEmail,desc" => query.OrderByDescending(o => o.CustomerEmail),
            "customerName,asc" => query.OrderBy(o => o.CustomerName),
            "customerName,desc" => query.OrderByDescending(o => o.CustomerName),
            _ => query.OrderByDescending(o => o.CreatedAt)
        };
        
        var orders = await query.ToListAsync(ct);
        
        var allOrders = await GetAllAsync(ct);
        foreach (var order in orders)
        {
            order.Priority = CalculatePriority(order, allOrders);

            var fuzzy = fuzzyService.Evaluate(order.Priority);
            order.MembershipLow = fuzzy.Low;
            order.MembershipMedium = fuzzy.Medium;
            order.MembershipHigh = fuzzy.High;
        }
        
        if (!string.IsNullOrEmpty(importance))
        {
            orders = importance.ToLower() switch
            {
                "low" => orders.Where(o => o.MembershipLow > 0.5).ToList(),
                "medium" => orders.Where(o => o.MembershipMedium > 0.5).ToList(),
                "high" => orders.Where(o => o.MembershipHigh > 0.5).ToList(),
                _ => orders
            };
        }
        
        var total = orders.Count;
        var items = orders.Skip(page * size).Take(size).ToList();

        return new PageResponse<OrderListItemDto>
        {
            Items = items.Select(ToListItemDto),
            Page = page,
            TotalElements = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }

    
    public async Task<bool> UpdateStatusAsync(int id, string newStatus)
    {
        var order = await _dbSet.FindAsync(id);
        if (order == null) return false;

        if (!Enum.TryParse<OrderStatus>(newStatus, true, out var parsed))
            throw new ArgumentException($"Invalid status: {newStatus}");

        order.Status = parsed;
        _dbSet.Update(order);
        await _context.SaveChangesAsync();

        return true;
    }
    
    public double CalculatePriority(Order order, IEnumerable<Order> allOrders)
    {
        var maxAge = allOrders.Max(o => (DateTime.Now - o.CreatedAt).TotalDays);
        var maxValue = allOrders.Max(o => o.Items.Sum(i => i.TotalPrice));
        var maxItems = allOrders.Max(o => o.Items.Count);
        
        var ageFactor = (DateTime.UtcNow - order.CreatedAt).TotalDays / maxAge;
        var valueFactor = (double)order.Items.Sum(i => i.TotalPrice) / (double)maxValue;
        var itemFactor = (double)order.Items.Count / maxItems;

        const double wAge = 0.5;
        const double wValue = 0.4;
        const double wItem = 0.1;
        
        var priority = wAge * ageFactor + wValue * valueFactor + wItem * itemFactor;
        
        return priority * 100.0;
    }
    
    private static OrderListItemDto ToListItemDto(Order order)
    {
        string importance = order.MembershipHigh > 0.5 ? "High"
            : order.MembershipMedium > 0.5 ? "Medium"
            : "Low";

        return new OrderListItemDto()
        {
            Id = order.Id,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.CustomerName,
            CreatedAt = order.CreatedAt,
            Status = order.Status.ToString(),
            Importance = importance,
            ItemsCount = order.Items.Count,
            TotalPrice = order.Items.Sum(i => i.TotalPrice)
        };
    }

}