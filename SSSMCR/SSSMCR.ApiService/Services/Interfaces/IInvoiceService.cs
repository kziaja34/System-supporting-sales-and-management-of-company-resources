using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IInvoiceService : IGenericService<Invoice>
{
    Task<IEnumerable<Invoice>> GetByOrderAsync(int orderId);
}