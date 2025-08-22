using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IUserService : IGenericService<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> VerifyPasswordAsync(string email, string password, CancellationToken ct = default);
    Task<IEnumerable<string>> GetRolesAsync(int userId, CancellationToken ct = default); // <- int
}