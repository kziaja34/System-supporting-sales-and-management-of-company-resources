using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest req, CancellationToken ct = default);
}