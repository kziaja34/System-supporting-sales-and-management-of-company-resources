using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class OrdersApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<OrdersApiService> logger) 
    : GenericService<OrdersApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<OrdersApiService> _logger = logger;

    public async Task<PageResponse<OrderListItemDto>> GetOrdersPageAsync(int page = 0, int size = 20, string sort = "priority,desc", string? search = null)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/orders?page={page}&size={size}&sort={sort}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        await AttachBearerAsync(http);

        try
        {
            var res = await http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                var error = await ReadApiErrorAsync(res);
                _logger.LogWarning("GetOrdersPageAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
                return new PageResponse<OrderListItemDto>
                {
                    Items = [],
                    Page = page,
                    Size = size,
                    TotalElements = 0,
                    TotalPages = 0
                };
            }

            var dto = await ReadJsonAsync<PageResponse<OrderListItemDto>>(res.Content);

            return dto ?? new PageResponse<OrderListItemDto>
            {
                Items = [],
                Page = page,
                Size = size,
                TotalElements = 0,
                TotalPages = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrdersPageAsync: request exception");
            return new PageResponse<OrderListItemDto>
            {
                Items = [],
                Page = page,
                Size = size,
                TotalElements = 0,
                TotalPages = 0
            };
        }
    }

    public async Task<OrderDetailsDto?> GetOrderByIdAsync(int id)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/orders/{id}";

        await AttachBearerAsync(http);

        try
        {
            var res = await http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                var error = await ReadApiErrorAsync(res);
                _logger.LogWarning("GetOrderByIdAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
                return null;
            }

            return await ReadJsonAsync<OrderDetailsDto>(res.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetOrderByIdAsync: request exception");
            return null;
        }
    }
    
    public async Task<bool> UpdateOrderStatusAsync(int id, string newStatus)
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        try
        {
            var res = await http.PutAsJsonAsync($"/api/orders/{id}/status", newStatus);
            await EnsureSuccessOrThrowAsync(res, "UpdateOrderStatusAsync");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpdateOrderStatusAsync failed (id={Id}, status={Status})", id, newStatus);
            return false;
        }
    }

    public async Task<ReserveResult?> ReserveOrderAsync(int orderId, int? preferredBranchId = null)
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var res = await http.PostAsJsonAsync($"/api/warehouse/orders/{orderId}/reserve", preferredBranchId);

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("ReserveOrderAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return null;
        }

        return await ReadJsonAsync<ReserveResult>(res.Content);
    }

    public async Task<(bool Success, bool RequireConfirm)> ReleaseOrderAsync(int orderId, bool confirm = false)
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var url = $"/api/warehouse/orders/{orderId}/release?confirm={confirm.ToString().ToLower()}";
        var res = await http.PostAsync(url, null);

        if (res.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var body = await res.Content.ReadAsStringAsync();
            if (body.Contains("requireConfirm", StringComparison.OrdinalIgnoreCase))
                return (false, true);
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("ReleaseOrderAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
        }

        return (res.IsSuccessStatusCode, false);
    }

}