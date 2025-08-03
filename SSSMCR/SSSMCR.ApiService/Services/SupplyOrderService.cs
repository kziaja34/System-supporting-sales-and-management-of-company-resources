using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;

namespace SSSMCR.ApiService.Services;

public class SupplyOrderService : GenericService<SupplyOrder>, ISupplyOrderService
{
    public SupplyOrderService(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<SupplyOrder>> GetBySupplierAsync(int supplierId) =>
        await _dbSet
            .Where(so => so.SupplierId == supplierId)
            .Include(so => so.Items)
            .Include(so => so.Branch)
            .ToListAsync();
}