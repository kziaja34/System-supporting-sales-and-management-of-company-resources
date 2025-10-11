using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Model.Exceptions;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Administrator")]
public class UsersController(IUserService userService, IPasswordHasher hasher) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken ct)
    {
        var users = await userService.GetAllAsync(ct);
        return Ok(users.Select(ToResponse));
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct)
    {
        try
        {
            var user = await userService.GetByIdAsync(id, ct);
            return Ok(ToResponse(user));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] UserCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var entity = ToEntity(req);
            entity.PasswordHash = hasher.Hash(req.Password);
            entity = await userService.CreateAsync(entity, ct);

            var resp = ToResponse(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, resp);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UserUpdateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var entity = ToEntity(req);

            if (!string.IsNullOrWhiteSpace(req.NewPassword))
            {
                entity.PasswordHash = hasher.Hash(req.NewPassword);
            }

            await userService.UpdateUserAsync(id, CurrentUserId, entity, ct);

            var updated = await userService.GetByIdAsync(id, ct);
            return Ok(ToResponse(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (CurrentUserException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await userService.DeleteUserAsync(id, CurrentUserId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (CurrentUserException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    private static UserResponse ToResponse(User u) => new()
    {
        Id = u.Id,
        FirstName = u.FirstName,
        LastName  = u.LastName,
        Email     = u.Email,
        RoleId    = u.RoleId,
        BranchId  = u.BranchId ?? 0,
        RoleName  = u.Role?.Name ?? string.Empty,
        BranchName = u.Branch?.Name ?? string.Empty
    };
    
    private static User ToEntity(UserUpdateRequest req) => new()
    {
        FirstName = req.FirstName.Trim(),
        LastName  = req.LastName.Trim(),
        Email     = req.Email.Trim(),
        RoleId    = req.RoleId,
        BranchId  = req.BranchId
    };
    private static User ToEntity(UserCreateRequest req) => new()
    {
        FirstName    = req.FirstName.Trim(),
        LastName     = req.LastName.Trim(),
        Email        = req.Email.Trim(),
        PasswordHash = req.Password,
        RoleId       = req.RoleId,
        BranchId     = req.BranchId
    };
}
