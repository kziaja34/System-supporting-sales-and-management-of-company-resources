using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface ISupplyOrderService : IGenericService<SupplyOrder>
{
    Task<IEnumerable<SupplyOrder>> GetBySupplierAsync(int supplierId);
}