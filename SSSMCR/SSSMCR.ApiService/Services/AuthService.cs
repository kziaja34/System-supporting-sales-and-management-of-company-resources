using SSSMCR.ApiService.Services.Interfaces;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services;

public sealed class AuthService(IUserService users, IJwtTokenGenerator tokens) : IAuthService
{
    public async Task<TokenResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return null;
        
        var isValid = await users.VerifyPasswordAsync(req.Email, req.Password, ct);
        if (!isValid) return null;
        
        var u = await users.GetByEmailAsync(req.Email, ct);
        if (u is null) return null;
        
        var role = await users.GetRoleAsync(u.Id, ct);

        return tokens.Generate(u.Id, u.Email, $"{u.FirstName} {u.LastName}", role.Name);
    }
}