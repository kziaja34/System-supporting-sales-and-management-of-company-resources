using SSSMCR.ApiService.Model;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IUserService : IGenericService<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> VerifyPasswordAsync(string email, string password, CancellationToken ct = default);
    Task<Role> GetRoleAsync(int userId, CancellationToken ct = default);
    Task UpdateProfileAsync(int userId, User user, CancellationToken ct = default);
    Task UpdateUserAsync(int userId, User user, CancellationToken ct = default);
    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default);
}