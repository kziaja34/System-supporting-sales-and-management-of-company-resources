using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPasswordHasher _hasher;

    public UsersController(IUserService userService, IPasswordHasher hasher)
    {
        _userService = userService;
        _hasher = hasher;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll(CancellationToken ct)
    {
        var users = await _userService.GetAllAsync();
        return Ok(users.Select(ToResponse));
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetById(int id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();
        return Ok(ToResponse(user));
    }
    
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] UserCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        
        var existing = await _userService.GetByEmailAsync(req.Email, ct);
        if (existing is not null)
            return Conflict(new { message = "Email is already in use." });

        var entity = new User
        {
            FirstName = req.FirstName.Trim(),
            LastName  = req.LastName.Trim(),
            Email     = req.Email.Trim(),
            PasswordHash = _hasher.Hash(req.Password),
            RoleId    = req.RoleId,
            BranchId  = req.BranchId
        };

        entity = await _userService.CreateAsync(entity);
        var resp = ToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, resp);
    }
    
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UserUpdateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound();
        
        if (!string.Equals(user.Email, req.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailOwner = await _userService.GetByEmailAsync(req.Email, ct);
            if (emailOwner is not null && emailOwner.Id != id)
                return Conflict(new { message = "Email is already in use." });
        }

        user.FirstName = req.FirstName.Trim();
        user.LastName  = req.LastName.Trim();
        user.Email     = req.Email.Trim();
        user.RoleId    = req.RoleId;
        user.BranchId  = req.BranchId;
        
        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            user.PasswordHash = _hasher.Hash(req.NewPassword);
        }

        await _userService.UpdateAsync(user);
        return Ok(ToResponse(user));
    }
    
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteAsync(id);
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
        RoleName  = u.Role.Name,
        BranchName = u.Branch.Name
    };
}
