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
    double CalculatePriority(dynamic order, IEnumerable<dynamic> allOrders);
}

public class OrderService(AppDbContext context, FuzzyPriorityEvaluatorService fuzzyService) : GenericService<Order>(context), IOrderService
{
    private static DateTime _fuzzyCacheTime = DateTime.MinValue;
    private static List<OrderFuzzyStats>? _fuzzyCache;
    
    private async Task<List<OrderFuzzyStats>> GetFuzzyStatsCachedAsync(CancellationToken ct)
    {
        if (_fuzzyCache != null &&
            DateTime.UtcNow - _fuzzyCacheTime < TimeSpan.FromSeconds(60))
        {
            return _fuzzyCache;
        }

        _fuzzyCache = await _dbSet
            .Select(o => new OrderFuzzyStats
            {
                Id = o.Id,
                CreatedAt = o.CreatedAt,
                ItemsCount = o.Items.Count,
                TotalPrice = o.Items.Sum(i => i.UnitPrice * i.Quantity)
            })
            .ToListAsync(ct);

        _fuzzyCacheTime = DateTime.UtcNow;

        return _fuzzyCache;
    }


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
            .Include(o => o.Branch)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order == null)
            throw new KeyNotFoundException("Order not found.");
        
        var stats = await _dbSet
            .Select(o => new
            {
                Age = (DateTime.UtcNow - o.CreatedAt).TotalDays,
                TotalPrice = o.Items.Sum(i => i.UnitPrice * i.Quantity),
                Items = o.Items.Count
            })
            .ToListAsync(ct);

        order.Priority = CalculatePriorityForDetails(order, stats);

        var fuzzy = fuzzyService.Evaluate(order.Priority);

        order.MembershipLow = fuzzy.Low;
        order.MembershipMedium = fuzzy.Medium;
        order.MembershipHigh = fuzzy.High;

        return order;
    }
    
    public async Task<PageResponse<OrderListItemDto>> GetPagedAsync(
    int page, int size, string sort, string? search = null, string? importance = null,
    CancellationToken ct = default)
    {
        var query = _dbSet.AsQueryable();

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
        
        var total = await query.CountAsync(ct);
        
        var pageItems = await query
            .Skip(page * size)
            .Take(size)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                CustomerName = o.CustomerName,
                CustomerEmail = o.CustomerEmail,
                CreatedAt = o.CreatedAt,
                Status = o.Status.ToString(),
                ItemsCount = o.ItemsCount,
                TotalPrice = o.TotalPrice,
                Importance = "",
                ULow = 0,
                UMedium = 0,
                UHigh = 0
            })
            .ToListAsync(ct);
        
        var stats = await GetFuzzyStatsCachedAsync(ct);

        foreach (var dto in pageItems)
        {
            var src = stats.FirstOrDefault(x => x.Id == dto.Id);
            if (src == null)
            {
                dto.ULow = 0;
                dto.UMedium = 1;
                dto.UHigh = 0;
                dto.Importance = "Medium";
                continue;
            }

            var priority = CalculatePriority(src, stats);
            var fuzzy = fuzzyService.Evaluate(priority);

            dto.ULow = fuzzy.Low;
            dto.UMedium = fuzzy.Medium;
            dto.UHigh = fuzzy.High;

            dto.Importance = dto.UHigh > 0.5 ? "High" :
                dto.UMedium > 0.5 ? "Medium" :
                "Low";
        }

        
        if (!string.IsNullOrEmpty(importance))
        {
            var imp = importance.ToLower();
            pageItems = imp switch
            {
                "low" => pageItems.Where(o => o.ULow > 0.5).ToList(),
                "medium" => pageItems.Where(o => o.UMedium > 0.5).ToList(),
                "high" => pageItems.Where(o => o.UHigh > 0.5).ToList(),
                _ => pageItems
            };
        }
        
        return new PageResponse<OrderListItemDto>
        {
            Items = pageItems,
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
    
    public double CalculatePriority(dynamic order, IEnumerable<dynamic> allOrders)
    {
        var maxAge = allOrders.Max(o => (DateTime.Now - o.CreatedAt).TotalDays);
        var maxValue = allOrders.Max(o => o.TotalPrice);
        var maxItems = allOrders.Max(o => o.ItemsCount);

        var ageFactor = (DateTime.UtcNow - order.CreatedAt).TotalDays / maxAge;
        var valueFactor = (double)order.TotalPrice / (double)maxValue!;
        var itemFactor = (double)order.ItemsCount / maxItems;

        const double wAge = 0.5;
        const double wValue = 0.4;
        const double wItem = 0.1;

        var priority = wAge * ageFactor + wValue * valueFactor + wItem * itemFactor;

        return priority * 100.0;
    }
    
    private double CalculatePriorityForDetails(Order order, IEnumerable<dynamic> stats)
    {
        var maxAge = stats.Max(o => o.Age);
        var maxValue = stats.Max(o => o.TotalPrice);
        var maxItems = stats.Max(o => o.Items);

        var thisAge = (DateTime.UtcNow - order.CreatedAt).TotalDays;
        var thisValue = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        var thisItems = order.Items.Count;

        var ageFactor = thisAge / maxAge;
        var valueFactor = (double)thisValue / (double)maxValue!;
        var itemFactor = thisItems / (double)maxItems!;

        const double wAge = 0.5;
        const double wValue = 0.4;
        const double wItem = 0.1;

        var priority = wAge * ageFactor + wValue * valueFactor + wItem * itemFactor;

        return priority * 100.0;
    }
}