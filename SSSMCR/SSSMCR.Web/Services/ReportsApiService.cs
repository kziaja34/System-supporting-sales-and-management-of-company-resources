using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class ReportsApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<ReportsApiService> logger) : GenericService<ReportsApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    
    public async Task<List<SalesByBranchDto>> GetSalesByBranchAsync()
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var res = await http.GetAsync("api/reports/sales-by-branch");
        res.EnsureSuccessStatusCode();

        return await res.Content.ReadFromJsonAsync<List<SalesByBranchDto>>() ?? new();
    }

    public async Task<List<SalesTrendDto>> GetSalesTrendAsync()
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var res = await http.GetAsync("api/reports/sales-trend");
        res.EnsureSuccessStatusCode();

        return await res.Content.ReadFromJsonAsync<List<SalesTrendDto>>() ?? new();
    }
    
    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        // Jeśli nie ma danych, zwracamy pusty obiekt (zamiast nulla), żeby uniknąć błędów
        return await http.GetFromJsonAsync<DashboardStatsDto>("api/reports/dashboard-stats") 
               ?? new DashboardStatsDto();
    }

    public async Task<List<TopProductDto>> GetTopProductsAsync()
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        return await http.GetFromJsonAsync<List<TopProductDto>>("api/reports/top-products") 
               ?? new List<TopProductDto>();
    }
}