using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;

namespace SSSMCR.ApiService.Services;

public class BranchService(AppDbContext context) : GenericService<Branch>(context), IBranchService
{
    public new async Task<Branch> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public new async Task<Branch> CreateAsync(Branch branch, CancellationToken ct = default)
    {
        var name = branch?.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(branch.Name));
        
        var exists = await _dbSet.AsNoTracking()
            .AnyAsync(b => b.Name.ToLower() == name.ToLower(), ct);
        
        if (exists)
            throw new InvalidOperationException("Branch with this name already exists");
        
        await _dbSet.AddAsync(branch, ct);
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
        
        _dbSet.Update(existing);
        await _context.SaveChangesAsync(ct);
    }
}