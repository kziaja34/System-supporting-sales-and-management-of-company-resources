using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.ApiService.Services.Interfaces;

namespace SSSMCR.ApiService.Services;

public class StockAlertService : GenericService<StockAlert>, IStockAlertService
{
    public StockAlertService(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<StockAlert>> GetUnseenAsync() =>
        await _dbSet
            .Where(a => !a.Seen)
            .Include(a => a.Inventory)
            .ToListAsync();

    public async Task MarkAsSeenAsync(int alertId)
    {
        var alert = await GetByIdAsync(alertId);
        if (alert == null) return;
        alert.Seen = true;
        await _context.SaveChangesAsync();
    }
}