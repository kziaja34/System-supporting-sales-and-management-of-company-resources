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
    
    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var currentStart = now.AddDays(-30);
        var previousStart = now.AddDays(-60);

        // 1. Pobierz wszystkie zamówienia z ostatnich 60 dni (żeby nie strzelać do bazy 10 razy)
        // Pobieramy ID, Datę i Status oraz Items (dla przychodu)
        var rawData = await context.Orders
            .Include(o => o.Items)
            .Where(o => o.CreatedAt >= previousStart && o.Status == OrderStatus.Completed)
            .ToListAsync(ct);

        // 2. Rozdziel dane w pamięci na dwa koszyki
        var currentOrders = rawData.Where(o => o.CreatedAt >= currentStart).ToList();
        var prevOrders = rawData.Where(o => o.CreatedAt < currentStart).ToList();

        // 3. Oblicz statystyki dla OBECNEGO okresu
        var currentCount = currentOrders.Count;
        var currentRevenue = currentOrders.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity));
        var currentAvg = currentCount > 0 ? currentRevenue / currentCount : 0;

        // 4. Oblicz statystyki dla POPRZEDNIEGO okresu
        var prevCount = prevOrders.Count;
        var prevRevenue = prevOrders.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity));
        var prevAvg = prevCount > 0 ? prevRevenue / prevCount : 0;

        // 5. Funkcja pomocnicza do liczenia % zmiany
        double CalculateChange(decimal current, decimal previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0; // Jeśli wcześniej było 0, a teraz coś jest -> 100% wzrostu
            return (double)((current - previous) / previous) * 100;
        }

        return Ok(new DashboardStatsDto
        {
            TotalRevenue = currentRevenue,
            TotalOrders = currentCount,
            AverageOrderValue = currentAvg,

            // Obliczamy % zmian
            RevenueChange = CalculateChange(currentRevenue, prevRevenue),
            OrdersChange = CalculateChange(currentCount, prevCount),
            AvgValueChange = CalculateChange(currentAvg, prevAvg)
        });
    }

    [HttpGet("top-products")]
    public async Task<IActionResult> GetTopProducts(CancellationToken ct)
    {
        var lastMonth = DateTime.UtcNow.AddDays(-30);

        var data = await context.Orders
            .Where(o => o.CreatedAt >= lastMonth && o.Status == OrderStatus.Completed)
            .SelectMany(o => o.Items)       // Spłaszczamy do listy wszystkich sprzedanych sztuk
            .GroupBy(i => i.Product.Name)   // Grupujemy po nazwie produktu
            .Select(g => new TopProductDto
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(i => i.Quantity) // Sumujemy ilość
            })
            .OrderByDescending(x => x.QuantitySold) // Sortujemy od najlepszych
            .Take(5)                                // Bierzemy top 5
            .ToListAsync(ct);

        return Ok(data);
    }
}
