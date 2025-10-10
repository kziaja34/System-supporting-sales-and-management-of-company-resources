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

    [HttpGet("byproduct/{productId:int}")]
    public async Task<IActionResult> GetAllForProduct(int productId, CancellationToken ct)
    {
        var list = await context.Suppliers
            .AsNoTracking()
            .Where(s => s.Products.Any(p => p.ProductId == productId))
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
    
    [HttpGet("{id:int}/products")]
    [Authorize(Roles = "Administrator, Manager")]
    public async Task<IActionResult> GetSupplierProducts(int id, CancellationToken ct)
    {
        var supplierExists = await context.Suppliers.AsNoTracking().AnyAsync(s => s.Id == id, ct);
        if (!supplierExists)
            return NotFound(new { error = $"Supplier {id} not found." });

        var items = await context.SupplierProducts
            .AsNoTracking()
            .Where(sp => sp.SupplierId == id)
            .Include(sp => sp.Product)
            .Select(sp => new SupplierProductResponse
            {
                ProductId = sp.ProductId,
                ProductName = sp.Product.Name,
                Price = sp.Price
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // Ustawienie (nadpisanie) oferty dostawcy
    [HttpPut("{id:int}/products")]
    [Authorize(Roles = "Administrator, Manager")]
    public async Task<IActionResult> SetSupplierProducts(int id, [FromBody] SupplierProductsUpdateRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (supplier is null)
            return NotFound(new { error = $"Supplier {id} not found." });

        // Zweryfikuj istniejące produkty (ignoruj nieistniejące id)
        var reqProductIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var validProductIds = await context.Products
            .AsNoTracking()
            .Where(p => reqProductIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        var target = req.Items
            .Where(i => validProductIds.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Price = g.Last().Price })
            .ToDictionary(x => x.ProductId, x => x.Price);

        var currentLinks = await context.SupplierProducts
            .Where(sp => sp.SupplierId == id)
            .ToListAsync(ct);

        var currentSet = currentLinks.ToDictionary(sp => sp.ProductId, sp => sp);

        // Usuń nieobecne
        var toRemove = currentLinks.Where(sp => !target.ContainsKey(sp.ProductId)).ToList();
        if (toRemove.Count > 0) context.SupplierProducts.RemoveRange(toRemove);

        // Dodaj/aktualizuj obecne
        foreach (var kv in target)
        {
            if (currentSet.TryGetValue(kv.Key, out var existing))
            {
                existing.Price = kv.Value;
            }
            else
            {
                context.SupplierProducts.Add(new SupplierProduct
                {
                    SupplierId = id,
                    ProductId = kv.Key,
                    Price = kv.Value
                });
            }
        }

        await context.SaveChangesAsync(ct);
        return NoContent();
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