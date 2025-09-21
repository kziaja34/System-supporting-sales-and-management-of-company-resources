using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Common;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IOrderService : IGenericService<Order>
{
    Task<IEnumerable<Order>> GetAllAsync(string? status = null, string? email = null, CancellationToken ct = default);

    Task<PageResponse<OrderListItemDto>> GetPagedAsync(int page, int size, string sort, string? search = null, CancellationToken ct = default);
    
    Task<bool> UpdateStatusAsync(int id, string newStatus);
}