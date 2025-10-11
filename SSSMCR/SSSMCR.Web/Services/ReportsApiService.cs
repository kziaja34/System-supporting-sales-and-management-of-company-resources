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
}