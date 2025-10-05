using System.Text.Json;
using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class ProductStocksApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<ProductStocksApiService> logger) : GenericService<ProductStocksApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
    private readonly ILogger<ProductStocksApiService> _logger = logger;
    
    public async Task<string> RecalculateThresholdsAsync()
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var url = "api/warehouse/stocks/recalculate-thresholds";
        HttpResponseMessage res;
        try
        {
            res = await http.PostAsync(url, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecalculateThresholdsAsync: request exception");
            throw;
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("RecalculateThresholdsAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            throw new HttpRequestException(error);
        }

        try
        {
            var json = await res.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String
                ? msgEl.GetString() ?? "Thresholds recalculated."
                : "Thresholds recalculated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecalculateThresholdsAsync: response parse error");
            return "Thresholds recalculated.";
        }
    }

    public async Task<List<ProductStockDto>> GetStocksAsync(int? branchId = null)
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var url = "/api/warehouse/stocks";
        if (branchId.HasValue)
            url += $"?branchId={branchId.Value}";

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStocksAsync: request exception");
            throw;
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);

            if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException(string.IsNullOrWhiteSpace(error) ? "Unauthorized to access stocks." : error);
            else if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new KeyNotFoundException(string.IsNullOrWhiteSpace(error) ? "Stocks not found for the given branch." : error);

            _logger.LogWarning("GetStocksAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            throw new HttpRequestException(error);
        }

        var stocks = await ReadJsonAsync<List<ProductStockDto>>(res.Content);
        return stocks ?? new List<ProductStockDto>();
    }
}
