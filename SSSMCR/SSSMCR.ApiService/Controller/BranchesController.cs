using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/branches")]
[Authorize]
public class BranchesController(IBranchService branchService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrator, Seller, Manager, WarehouseWorker")]
    public async Task<IEnumerable<BranchResponse>> GetAll()
    {
        var branches = await branchService.GetAllAsync();
        return branches.Select(ToResponse);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrator, Seller, Manager, WarehouseWorker")]
    public async Task<ActionResult<BranchResponse>> GetById(int id)
    {
        var branch = await branchService.GetByIdAsync(id);
        if (branch is null) return NotFound();
        return Ok(ToResponse(branch));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<BranchResponse>> Create([FromBody] BranchCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var entity = ToEntity(req);
            entity = await branchService.CreateAsync(entity, ct);
            
            var response = ToResponse(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<BranchResponse>> Update(int id, [FromBody] BranchCreateRequest req,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var entity = ToEntity(req);
            
            await branchService.UpdateBranchAsync(id, entity, ct);
            
            var updated = await branchService.GetByIdAsync(id, ct);
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
    }
    
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await branchService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private static BranchResponse ToResponse(Branch b) => new()
    {
        Name = b.Name,
        Id = b.Id,
        Location = b.Location
    };
    
    private static Branch ToEntity(BranchCreateRequest b) => new()
    {
        Name = b.Name,
        Location = b.Location
    };
}