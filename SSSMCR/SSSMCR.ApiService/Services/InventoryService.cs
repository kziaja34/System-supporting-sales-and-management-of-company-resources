using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services;

public interface IInventoryService : IGenericService<ProductStock>
{
    Task<IEnumerable<ProductStock>> GetLowStockAsync();
}
public class InventoryService : GenericService<ProductStock>, IInventoryService
{
    public InventoryService(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<ProductStock>> GetLowStockAsync() =>
        await _dbSet
            .Where(i => i.Quantity <= i.CriticalThreshold)
            .Include(i => i.Product)
            .Include(i => i.Branch)
            .ToListAsync();
}