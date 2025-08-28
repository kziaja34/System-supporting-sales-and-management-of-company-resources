using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using SSSMCR.ApiService.Database;
using SSSMCR.Shared.Model;

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
    
    public async Task<User?> GetByIdAsync(int userId, CancellationToken ct = default)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
    
    public async Task UpdateProfileAsync(int userId, UserUpdateRequest req, CancellationToken ct = default)
    {
        var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new KeyNotFoundException("User not found");

        user.FirstName = req.FirstName;
        user.LastName  = req.LastName;

        _dbSet.Update(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new KeyNotFoundException("User not found");
        
        var verify = _hasher.Verify(user.PasswordHash, currentPassword);
        if (!verify)
            throw new InvalidOperationException("Current password is invalid.");
        
        user.PasswordHash = _hasher.Hash(newPassword);
        _dbSet.Update(user);
        await _context.SaveChangesAsync(ct);
    }
}