using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Exceptions;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public interface IReservationService : IGenericService<StockReservation>
{
    public Task<List<StockReservation>> GetReservations(int? branchId);
    Task<PageResponse<ReservationDto>> GetPagedAsync(
        int page,
        int size,
        string sort,
        string? search = null,
        int? branchId = null,
        string? importance = null,
        CancellationToken ct = default);
}

public class ReservationService(AppDbContext context, IOrderService orderService, FuzzyPriorityEvaluatorService fuzzyService) : GenericService<StockReservation>(context), IReservationService
{
    public Task<List<StockReservation>> GetReservations(int? branchId)
    {
        var reservations = _dbSet
            .Include(r => r.OrderItem)
            .ThenInclude(oi => oi.Order)
            .Include(r => r.ProductStock)
            .ThenInclude(ps => ps.Branch)
            .Include(r => r.ProductStock.Product)
            .AsQueryable();

        if (reservations == null)
            throw new ReservationNotFoundException(0);
        
        if (branchId.HasValue)
        {
            reservations = reservations.Where(r => r.ProductStock.BranchId == branchId);
        }

        return reservations.ToListAsync();
    }
    
    public async Task<PageResponse<ReservationDto>> GetPagedAsync(
        int page,
        int size,
        string sort,
        string? search = null,
        int? branchId = null,
        string? importance = null,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(r => r.OrderItem)
                .ThenInclude(oi => oi.Order)
                    .ThenInclude(o => o.Items)
            .Include(r => r.ProductStock)
                .ThenInclude(ps => ps.Branch)
            .Include(r => r.ProductStock.Product)
            .AsQueryable();

        if (branchId.HasValue)
            query = query.Where(r => r.ProductStock.BranchId == branchId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                r.OrderItem.Order.CustomerName.Contains(search) ||
                r.OrderItem.Order.CustomerEmail.Contains(search) ||
                r.OrderItem.Order.ShippingAddress.Contains(search) ||
                r.ProductStock.Product.Name.Contains(search) ||
                r.ProductStock.Branch.Name.Contains(search));
        }

        // sort — analogicznie do OrderService
        query = sort switch
        {
            "createdAt,asc" => query.OrderBy(r => r.CreatedAt),
            "createdAt,desc" => query.OrderByDescending(r => r.CreatedAt),

            "orderId,asc" => query.OrderBy(r => r.OrderItem.OrderId),
            "orderId,desc" => query.OrderByDescending(r => r.OrderItem.OrderId),

            "customerName,asc" => query.OrderBy(r => r.OrderItem.Order.CustomerName),
            "customerName,desc" => query.OrderByDescending(r => r.OrderItem.Order.CustomerName),

            "productName,asc" => query.OrderBy(r => r.ProductStock.Product.Name),
            "productName,desc" => query.OrderByDescending(r => r.ProductStock.Product.Name),

            "branchName,asc" => query.OrderBy(r => r.ProductStock.Branch.Name),
            "branchName,desc" => query.OrderByDescending(r => r.ProductStock.Branch.Name),

            "quantity,asc" => query.OrderBy(r => r.Quantity),
            "quantity,desc" => query.OrderByDescending(r => r.Quantity),

            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        // materializacja przed fuzzy (potrzebujemy listy)
        var reservations = await query.ToListAsync(ct);

        // zbiór wszystkich zamówień do normalizacji (jak w OrderService)
        var allOrders = await _context.Orders
            .Include(o => o.Items)
            .ToListAsync(ct);

        // oblicz fuzzy importance wg priorytetu zamówienia
        foreach (var r in reservations)
        {
            var order = r.OrderItem.Order;
            var priority = orderService.CalculatePriority(order, allOrders);
            var (low, med, high) = fuzzyService.Evaluate(priority);

            // zapisz na obiekcie (jeśli masz pola) albo od razu do DTO przy mapowaniu
            r.OrderItem.Order.Priority = priority;
            r.OrderItem.Order.MembershipLow = low;
            r.OrderItem.Order.MembershipMedium = med;
            r.OrderItem.Order.MembershipHigh = high;
        }

        // filtr importance (jak w OrderService)
        if (!string.IsNullOrWhiteSpace(importance))
        {
            var imp = importance.ToLowerInvariant();
            reservations = imp switch
            {
                "low" => reservations.Where(r => r.OrderItem.Order.MembershipLow > 0.5).ToList(),
                "medium" => reservations.Where(r => r.OrderItem.Order.MembershipMedium > 0.5).ToList(),
                "high" => reservations.Where(r => r.OrderItem.Order.MembershipHigh > 0.5).ToList(),
                _ => reservations
            };
        }

        var total = reservations.Count;
        var pageItems = reservations
            .Skip(page * size)
            .Take(size)
            .Select(ToResponse)
            .ToList();

        return new PageResponse<ReservationDto>
        {
            Items = pageItems,
            Page = page,
            TotalElements = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
    
    private static ReservationDto ToResponse(StockReservation r)
    {
        var o = r.OrderItem.Order;

        var importance = o.MembershipHigh > 0.5 ? "High"
            : o.MembershipMedium > 0.5 ? "Medium"
            : "Low";

        return new ReservationDto
        {
            ReservationId = r.Id,
            OrderId = r.OrderItem.OrderId,
            BranchId = r.ProductStock.BranchId,
            BranchName = r.ProductStock.Branch.Name,
            ProductName = r.ProductStock.Product.Name,
            CustomerName = o.CustomerName,
            ShippingAddress = o.ShippingAddress,
            Quantity = r.Quantity,
            Status = r.Status.ToString(),
            OrderStatus = o.Status.ToString(),
            CreatedAt = r.CreatedAt,
            Importance = importance,
            ULow = o.MembershipLow,
            UMedium = o.MembershipMedium,
            UHigh = o.MembershipHigh
        };
    }
}