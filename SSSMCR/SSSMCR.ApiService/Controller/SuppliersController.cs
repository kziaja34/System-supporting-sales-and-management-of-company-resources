using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = "Administrator, WarehouseWorker, Manager")]
public class SuppliersController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await context.Suppliers
            .AsNoTracking()
            .Select(s => ToResponse(s))
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var s = await context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null)
            return NotFound(new { error = $"Supplier {id} not found." });

        return Ok(ToResponse(s));
    }

    [HttpPost]
    [Authorize(Roles = "Administrator, Manager")]
    public async Task<IActionResult> Create([FromBody] SupplierCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid supplier data." });

        var entity = new Supplier
        {
            Name = req.Name,
            ContactEmail = req.ContactEmail,
            Phone = req.Phone,
            Address = req.Address
        };

        context.Suppliers.Add(entity);
        await context.SaveChangesAsync(ct);

        return Ok(ToResponse(entity));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator, Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] SupplierUpdateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid supplier data." });

        var entity = await context.Suppliers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { error = $"Supplier {id} not found." });

        entity.Name = req.Name;
        entity.ContactEmail = req.ContactEmail;
        entity.Phone = req.Phone;
        entity.Address = req.Address;

        await context.SaveChangesAsync(ct);

        return Ok(ToResponse(entity));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator, Manager")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await context.Suppliers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { error = $"Supplier {id} not found." });

        context.Suppliers.Remove(entity);
        try
        {
            await context.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "Cannot delete supplier. It is used in other records." });
        }
    }

    private static SupplierResponse ToResponse(Supplier s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        ContactEmail = s.ContactEmail,
        Phone = s.Phone,
        Address = s.Address
    };
}