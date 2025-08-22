using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;

namespace SSSMCR.ApiService.Services;

public class RoleService : GenericService<Role>, IRoleService
{
    public RoleService(AppDbContext context) : base(context) { }

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        _dbSet.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name, ct);
}