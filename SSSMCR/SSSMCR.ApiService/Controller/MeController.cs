using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SSSMCR.ApiService.Services.Interfaces;
using SSSMCR.Shared.Model;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController(IUserService users) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("data")]
    public async Task<ActionResult<UserDto>> GetMe(CancellationToken ct)
    {
        var u = await users.GetByIdAsync(CurrentUserId);
        
        if (u is null) return NotFound();

        var roles = await users.GetRolesAsync(u.Id, ct);

        return Ok(new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Roles = roles
        });
    }

    [HttpPatch]
    public async Task<IActionResult> PatchMe([FromBody] UserUpdateRequest req, CancellationToken ct)
    {
        await users.UpdateProfileAsync(CurrentUserId, req, ct);
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        await users.ChangePasswordAsync(CurrentUserId, req.CurrentPassword, req.NewPassword, ct);
        return NoContent();
    }
}