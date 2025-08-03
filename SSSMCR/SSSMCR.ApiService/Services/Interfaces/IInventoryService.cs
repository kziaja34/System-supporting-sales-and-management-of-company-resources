using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IInventoryService : IGenericService<Inventory>
{
    Task<IEnumerable<Inventory>> GetLowStockAsync();
}