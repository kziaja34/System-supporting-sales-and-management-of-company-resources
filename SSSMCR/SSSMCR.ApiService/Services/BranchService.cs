using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;

namespace SSSMCR.ApiService.Services;

public interface IBranchService : IGenericService<Branch>
{
    Task UpdateBranchAsync(int branchId, Branch branch, CancellationToken ct = default);
}
public class BranchService(AppDbContext context) : GenericService<Branch>(context), IBranchService
{
    public new async Task<Branch> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw new KeyNotFoundException("Branch not found");
    }

    public new async Task<Branch> CreateAsync(Branch branch, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(branch);

        var name = branch.Name.Trim();
        
        var exists = await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Name.ToLower() == name.ToLower(), ct);
        
        if (exists)
            throw new InvalidOperationException("Branch with this name already exists");


        await _dbSet.AddAsync(branch ?? throw new ArgumentNullException(nameof(branch)), ct);
        await _context.SaveChangesAsync(ct);

        return await _dbSet
            .FirstAsync(b => b.Id == branch.Id, ct);
    }
    
    public async Task UpdateBranchAsync(int branchId, Branch branch, CancellationToken ct = default)
    {
        var existing = await _dbSet.FirstOrDefaultAsync(b => b.Id == branchId, ct)
                       ?? throw new KeyNotFoundException("Branch not found");
        
        var exists = await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Name.ToLower() == branch.Name.ToLower() && b.Id != branchId, ct);
        
        if (exists)
            throw new InvalidOperationException("Branch with this name already exists");
        
        existing.Name = branch.Name;
        existing.Location = branch.Location;
        
        existing.Latitude = branch.Latitude;
        existing.Longitude = branch.Longitude;
        
        _dbSet.Update(existing);
        await _context.SaveChangesAsync(ct);
    }
}