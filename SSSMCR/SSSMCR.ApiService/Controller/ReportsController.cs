using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.Shared.Model;

namespace SSSMCR.ApiService.Controller;

[ApiController]
[Route("api/reports")]
public class ReportsController(AppDbContext context) : ControllerBase
{
    [HttpGet("sales-by-branch")]
    public async Task<IActionResult> GetSalesByBranch(CancellationToken ct)
    {
        var lastMonth = DateTime.UtcNow.AddDays(-30);

        var data = await context.Orders
            .Include(o => o.Branch)
            .Where(o => o.CreatedAt >= lastMonth && o.Status == OrderStatus.Completed && o.BranchId != null)
            .GroupBy(o => o.Branch!.Name)
            .Select(g => new
            {
                Branch = g.Key,
                Total = g.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity))
            })
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpGet("sales-trend")]
    public async Task<IActionResult> GetSalesTrend(CancellationToken ct)
    {
        var lastMonth = DateTime.UtcNow.AddDays(-30);

        var data = await context.Orders
            .Where(o => o.CreatedAt >= lastMonth && o.Status == OrderStatus.Completed)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new {
                Date = g.Key,
                Total = g.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity))
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        return Ok(data);
    }
}
