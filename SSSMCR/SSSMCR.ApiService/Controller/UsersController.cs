using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService, IPasswordHasher hasher, IRoleService roleService, IBranchService branchService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken ct)
    {
        var users = await userService.GetAllAsync(ct);
        return Ok(users.Select(ToResponse));
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(id, ct);
        if (user is null) return NotFound();
        return Ok(ToResponse(user));
    }
    
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] UserCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var entity = ToEntity(req);
        entity.PasswordHash = hasher.Hash(req.Password);
        entity = await userService.CreateAsync(entity, ct);

        var resp = ToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, resp);
    }

    
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UserUpdateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var entity = ToEntity(req);

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            entity.PasswordHash = hasher.Hash(req.NewPassword);
        }

        await userService.UpdateUserAsync(id, entity, ct);

        var updated = await userService.GetByIdAsync(id, ct);
        return Ok(ToResponse(updated));
    }

    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await userService.DeleteAsync(id);
        return NoContent();
    }


    private static UserResponse ToResponse(User u) => new()
    {
        Id = u.Id,
        FirstName = u.FirstName,
        LastName  = u.LastName,
        Email     = u.Email,
        RoleId    = u.RoleId,
        BranchId  = u.BranchId,
        RoleName  = u.Role?.Name ?? string.Empty,
        BranchName = u.Branch?.Name ?? string.Empty
    };
    
    private static User ToEntity(UserUpdateRequest req) => new()
    {
        FirstName = req.FirstName?.Trim() ?? string.Empty,
        LastName  = req.LastName?.Trim() ?? string.Empty,
        Email     = req.Email?.Trim() ?? string.Empty,
        RoleId    = req.RoleId,
        BranchId  = req.BranchId
    };
    private static User ToEntity(UserCreateRequest req) => new()
    {
        FirstName    = req.FirstName?.Trim() ?? string.Empty,
        LastName     = req.LastName?.Trim() ?? string.Empty,
        Email        = req.Email?.Trim() ?? string.Empty,
        PasswordHash = req.Password,
        RoleId       = req.RoleId,
        BranchId     = req.BranchId
    };
}
