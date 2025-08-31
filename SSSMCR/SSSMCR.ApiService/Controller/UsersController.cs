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
        var users = await userService.GetAllAsync();
        return Ok(users.Select(ToResponse));
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(id);
        if (user is null) return NotFound();
        return Ok(ToResponse(user));
    }
    
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] UserCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        var existing = await userService.GetByEmailAsync(req.Email, ct);
        if (existing is not null)
            return Conflict(new { message = "Email is already in use." });

        var entity = new User
        {
            FirstName = req.FirstName.Trim(),
            LastName  = req.LastName.Trim(),
            Email     = req.Email.Trim(),
            PasswordHash = hasher.Hash(req.Password),
            RoleId    = req.RoleId,
            BranchId  = req.BranchId
        };

        entity = await userService.CreateAsync(entity);
        var resp = ToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, resp);
    }
    
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UserUpdateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        await userService.UpdateUserAsync(id, req, ct);
        
        return Ok(req);
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
}
