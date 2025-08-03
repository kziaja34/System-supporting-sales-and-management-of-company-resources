using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;

namespace SSSMCR.ApiService.Services;

public class InvoiceService : GenericService<Invoice>, IInvoiceService
{
    public InvoiceService(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Invoice>> GetByOrderAsync(int orderId) =>
        await _dbSet
            .Where(inv => inv.OrderId == orderId)
            .ToListAsync();
}