using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class SupplyApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<SupplyApiService> logger)
    : GenericService<SupplyApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<SupplyApiService> _logger = logger;

    public async Task<List<SupplyOrderResponseDto>> GetOrdersAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/supply/orders";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrdersAsync: request exception");
            return new();
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetOrdersAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return new();
        }

        var dto = await ReadJsonAsync<List<SupplyOrderResponseDto>>(res.Content);
        return dto ?? new();
    }

    public async Task<SupplyOrderResponseDto?> GetOrderAsync(int orderId)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/supply/orders/{orderId}";

        await AttachBearerAsync(http);

        try
        {
            var res = await http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                var error = await ReadApiErrorAsync(res);
                _logger.LogWarning("GetOrderAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
                return null;
            }

            return await ReadJsonAsync<SupplyOrderResponseDto>(res.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrderAsync: request exception");
            return null;
        }
    }

    public async Task<SupplyOrderResponseDto?> CreateOrderAsync(SupplyOrderCreateDto req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/supply/orders";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateOrderAsync: request exception");
            throw;
        }

        await EnsureSuccessOrThrowAsync(res, "CreateOrderAsync");
        return await ReadJsonAsync<SupplyOrderResponseDto>(res.Content);
    }

    public async Task<SupplyOrderResponseDto?> ReceiveOrderAsync(int orderId)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/supply/orders/{orderId}/receive";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.PostAsync(url, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReceiveOrderAsync: request exception");
            throw;
        }

        await EnsureSuccessOrThrowAsync(res, "ReceiveOrderAsync");
        return await ReadJsonAsync<SupplyOrderResponseDto>(res.Content);
    }
}