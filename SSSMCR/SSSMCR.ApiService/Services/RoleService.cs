using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services;

public interface IRoleService : IGenericService<Role>
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
}
public class RoleService : GenericService<Role>, IRoleService
{
    public RoleService(AppDbContext context) : base(context) { }

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        _dbSet.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name, ct);
}