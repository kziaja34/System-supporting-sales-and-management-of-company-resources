using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductController(
    IProductService productService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Administrator, Manager, Seller, WarehouseWorker")]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAll(CancellationToken ct)
    {
        var products = await productService.GetAllAsync(ct);
        return Ok(products.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrator, Manager, Seller, WarehouseWorker")]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken ct)
    {
        try
        {
            var product = await productService.GetByIdAsync(id, ct);
        
            return Ok(ToResponse(product));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var entity = ToEntity(req);
            entity.CreatedAt = DateTime.UtcNow;
            entity = await productService.CreateAsync(entity, ct);
            
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
    public async Task<ActionResult<ProductResponse>> Update(int id, [FromBody] ProductCreateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var entity = ToEntity(req);
            await productService.UpdateAsync(id, entity, ct);
            
            var updated = await productService.GetByIdAsync(id, ct);
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
            await productService.DeleteAsync(id);
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

    private static ProductResponse ToResponse(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        UnitPrice = product.UnitPrice
    };

    private static Product ToEntity(ProductCreateRequest req) => new()
    {
        Name = req.Name?.Trim() ?? string.Empty,
        Description = req.Description?.Trim() ?? string.Empty,
        UnitPrice = req.UnitPrice
    };
}