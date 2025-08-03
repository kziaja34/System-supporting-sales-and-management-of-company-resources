using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Common;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IOrderService : IGenericService<Order>
{
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<IEnumerable<Order>> GetByCustomerEmailAsync(string email);
}