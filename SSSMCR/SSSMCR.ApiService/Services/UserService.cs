using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;
using BCrypt.Net;
using Microsoft.AspNetCore.Identity;
using SSSMCR.ApiService.Database;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public class UserService(
    AppDbContext context,
    IPasswordHasher hasher,
    IRoleService roleService,
    IBranchService branchService)
    : GenericService<User>(context), IUserService
{
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

        try { return hasher.Verify(user.PasswordHash, password); }
        catch { return false; }
    }

    public async Task<Role> GetRoleAsync(int userId, CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Role)
            .FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("User not found");
    }
    
    public new async Task<User> GetByIdAsync(int userId, CancellationToken ct = default)
        => await _dbSet.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    public new async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .ToListAsync(ct);
    }
    
    public new async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        var email = user.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(user.Email));

        var exists = await _dbSet.AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == email.ToLower(), ct);

        if (exists)
            throw new InvalidOperationException("User already exists");
        
        await _dbSet.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
        
        return await _dbSet
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .FirstAsync(u => u.Id == user.Id, ct);
    }

    
    public async Task UpdateProfileAsync(int userId, User user, CancellationToken ct = default)
    {
        var existing = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId, ct)
                       ?? throw new KeyNotFoundException("User not found");

        existing.FirstName = user.FirstName;
        existing.LastName  = user.LastName;

        _dbSet.Update(existing);
        await _context.SaveChangesAsync(ct);
    }
    
    public async Task UpdateUserAsync(int userId, User user, CancellationToken ct = default)
    {
        var existing = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId, ct)
                       ?? throw new KeyNotFoundException("User not found");

        var email = user.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(user.Email));
        
        var exists = await _dbSet.AsNoTracking()
            .AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != userId, ct);

        if (exists)
            throw new InvalidOperationException("User with this email already exists");

        existing.FirstName = user.FirstName;
        existing.LastName  = user.LastName;
        existing.Email     = email;
        existing.RoleId    = user.RoleId;
        existing.BranchId  = user.BranchId;

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            existing.PasswordHash = user.PasswordHash;

        existing.Branch = await branchService.GetByIdAsync(user.BranchId ?? 0, ct);
        existing.Role   = await roleService.GetByIdAsync(user.RoleId, ct);
    
        _dbSet.Update(existing);
        await _context.SaveChangesAsync(ct);
    }



    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _dbSet.FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw new KeyNotFoundException("User not found");
        
        var verify = hasher.Verify(user.PasswordHash, currentPassword);
        if (!verify)
            throw new InvalidOperationException("Current password is invalid.");
        
        user.PasswordHash = hasher.Hash(newPassword);
        _dbSet.Update(user);
        await _context.SaveChangesAsync(ct);
    }
}