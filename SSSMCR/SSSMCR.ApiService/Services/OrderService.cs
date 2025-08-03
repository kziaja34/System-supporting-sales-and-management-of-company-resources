using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Common;
using SSSMCR.ApiService.Services.Interfaces;

namespace SSSMCR.ApiService.Services;

public class OrderService : GenericService<Order>, IOrderService
{
    public OrderService(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status) =>
        await _dbSet
            .Where(o => o.Status == status)
            .ToListAsync();

    public async Task<IEnumerable<Order>> GetByCustomerEmailAsync(string email) =>
        await _dbSet
            .Where(o => o.CustomerEmail.ToLower() == email.ToLower())
            .ToListAsync();
}