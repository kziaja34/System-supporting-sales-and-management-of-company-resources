using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;
using BCrypt.Net;
using SSSMCR.ApiService.Database;

namespace SSSMCR.ApiService.Services;

public class UserService : GenericService<User>, IUserService
{
    private readonly IPasswordHasher _hasher;
    
    public UserService(AppDbContext context, IPasswordHasher hasher) : base(context)
    {
        _hasher = hasher;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        return await _dbSet
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);
    }

    public async Task<bool> VerifyPasswordAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _dbSet.AsNoTracking()
            .Where(u => u.Email.ToLower() == email.ToLower())
            .Select(u => new { u.PasswordHash })
            .FirstOrDefaultAsync(ct);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash)) return false;

        try { return _hasher.Verify(user.PasswordHash, password); }
        catch { return false; }
    }

    public Task<IEnumerable<string>> GetRolesAsync(int userId, CancellationToken ct = default)
    {
        return _dbSet.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Role.Name)
            .Select(n => new[] { n } as IEnumerable<string>)
            .FirstOrDefaultAsync(ct) ?? Task.FromResult(Enumerable.Empty<string>());
    }
}