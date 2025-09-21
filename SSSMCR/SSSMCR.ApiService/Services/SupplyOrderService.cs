using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services;

public interface ISupplyOrderService : IGenericService<SupplyOrder>
{
    Task<IEnumerable<SupplyOrder>> GetBySupplierAsync(int supplierId);
}
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