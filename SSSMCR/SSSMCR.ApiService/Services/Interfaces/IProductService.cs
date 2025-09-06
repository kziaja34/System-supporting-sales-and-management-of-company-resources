using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IProductService : IGenericService<Product>
{
    Task UpdateAsync(int productId, Product product, CancellationToken ct = default);
}