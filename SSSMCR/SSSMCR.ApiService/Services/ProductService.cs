using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services;

public interface IProductService : IGenericService<Product>
{
    Task UpdateAsync(int productId, Product product, CancellationToken ct = default);
}

public class ProductService(
    AppDbContext context)
    : GenericService<Product>(context), IProductService
{
    public new async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        var name = product?.Name?.Trim() ?? string.Empty;
        
        var exists = await _dbSet.AsNoTracking()
            .AnyAsync(p => p.Name.ToLower() == name.ToLower(), ct);
        
        if (exists)
            throw new InvalidOperationException("Product with this name already exists");
        
        await _dbSet.AddAsync(product ?? throw new ArgumentNullException(nameof(product)), ct);
        await _context.SaveChangesAsync(ct);
        
        return await _dbSet.FirstAsync(p => p.Id == product.Id, ct);
    }

    public async Task UpdateAsync(int productId, Product product, CancellationToken ct = default)
    {
        var existing = await _dbSet.FirstOrDefaultAsync(p => p.Id == productId, ct)
                            ?? throw new KeyNotFoundException("Product not found");

        var exists = await _dbSet.AsNoTracking()
            .AnyAsync(p => p.Name.ToLower() == product.Name.ToLower() && p.Id != productId, ct);
        
        if (exists)
            throw new InvalidOperationException("Product with this name already exists");
        
        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.UnitPrice = product.UnitPrice;
        
        _dbSet.Update(existing);
        await _context.SaveChangesAsync(ct);
    }
}