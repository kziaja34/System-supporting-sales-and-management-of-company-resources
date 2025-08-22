using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Services.Interfaces;
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

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => Ok(new
    {
        name  = User.Identity?.Name,
        email = User.FindFirstValue(JwtRegisteredClaimNames.Email),
        roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
        sub   = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
    });
}