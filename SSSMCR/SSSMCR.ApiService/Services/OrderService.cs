using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public interface IOrderService : IGenericService<Order>
{
    Task<IEnumerable<Order>> GetAllAsync(string? status = null, string? email = null, CancellationToken ct = default);

    Task<PageResponse<OrderListItemDto>> GetPagedAsync(int page, int size, string sort, string? search = null, CancellationToken ct = default);
    
    Task<bool> UpdateStatusAsync(int id, string newStatus);
    int CalculatePriority(Order order);
}

public class OrderService : GenericService<Order>, IOrderService
{
    public OrderService(AppDbContext context) : base(context) { }

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
        
        order.Priority = CalculatePriority(order);
        
        return order;
    }
    
    public async Task<PageResponse<OrderListItemDto>> GetPagedAsync(
        int page, int size, string sort, string? search = null, CancellationToken ct = default)
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
        
        foreach (var order in orders)
        {
            order.Priority = CalculatePriority(order);
        }
        
        if (sort.StartsWith("priority"))
        {
            orders = sort.EndsWith("asc")
                ? orders.OrderBy(o => o.Priority).ToList()
                : orders.OrderByDescending(o => o.Priority).ToList();
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
    
    public int CalculatePriority(Order order)
    {
        var ageFactor = (DateTime.UtcNow - order.CreatedAt).Days;
        var valueFactor = (int)order.Items.Sum(i => i.TotalPrice) / 100;
        var itemFactor = order.Items.Count;

        return ageFactor + valueFactor + itemFactor;
    }
    
    private static OrderListItemDto ToListItemDto(Order order)
    {
        return new OrderListItemDto()
        {
            Id = order.Id,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.CustomerName,
            CreatedAt = order.CreatedAt,
            Status = order.Status.ToString(),
            Priority = order.Priority,
            ItemsCount = order.Items.Count,
            TotalPrice = order.Items.Sum(i => i.TotalPrice)
        };
    }
}