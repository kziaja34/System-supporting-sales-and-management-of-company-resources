using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class ProductStocksApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<ProductStocksApiService> logger) : GenericService<ProductStocksApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
    private readonly ILogger<ProductStocksApiService> _logger = logger;
    
    public async Task<List<ProductStockDto>> GetStocksAsync(int? branchId = null)
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var url = "/api/warehouse/stocks";
        if (branchId.HasValue)
            url += $"?branchId={branchId.Value}";

        var res = await http.GetAsync(url);

        if (!res.IsSuccessStatusCode)
        {
            if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Unauthorized to access stocks.");
            else if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new KeyNotFoundException("Stocks not found for the given branch.");
            else
                throw new Exception($"Error fetching stocks: {res.StatusCode}");
        }

        var stocks = await res.Content.ReadFromJsonAsync<List<ProductStockDto>>();
        return stocks ?? new List<ProductStockDto>();
    }
}
