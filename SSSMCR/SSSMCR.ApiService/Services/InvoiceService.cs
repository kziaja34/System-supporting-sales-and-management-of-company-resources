using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services;

public interface IInvoiceService : IGenericService<Invoice>
{
    Task<IEnumerable<Invoice>> GetByOrderAsync(int orderId);
}
public class InvoiceService : GenericService<Invoice>, IInvoiceService
{
    public InvoiceService(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Invoice>> GetByOrderAsync(int orderId) =>
        await _dbSet
            .Where(inv => inv.OrderId == orderId)
            .ToListAsync();
}