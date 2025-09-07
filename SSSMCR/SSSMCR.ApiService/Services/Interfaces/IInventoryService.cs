using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IInventoryService : IGenericService<ProductStock>
{
    Task<IEnumerable<ProductStock>> GetLowStockAsync();
}