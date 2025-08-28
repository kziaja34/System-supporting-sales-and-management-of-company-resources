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
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken ct)
    {
        var u = await users.GetByIdAsync(CurrentUserId);
        
        if (u is null) return NotFound();

        return Ok(new UserResponse()
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            RoleId = u.RoleId,
            RoleName = u.Role.Name,
            BranchId = u.BranchId,
            BranchName = u.Branch.Name
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