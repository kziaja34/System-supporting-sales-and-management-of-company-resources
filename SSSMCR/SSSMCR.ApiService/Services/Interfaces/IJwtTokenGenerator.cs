using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Services.Interfaces;

public interface IJwtTokenGenerator
{
    TokenResponse Generate(int userId, string email, string name, IEnumerable<string> roles);
}