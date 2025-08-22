using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IRoleService : IGenericService<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
}