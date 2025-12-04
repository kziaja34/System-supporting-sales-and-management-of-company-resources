using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var token = await auth.LoginAsync(req, ct);
        if (token is null) return Unauthorized(new { message = "Invalid credentials." });
        return Ok(token);
    }
}