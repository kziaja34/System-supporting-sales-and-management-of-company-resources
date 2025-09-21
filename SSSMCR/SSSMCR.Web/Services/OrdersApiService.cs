using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class OrdersApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<OrdersApiService> logger) 
    : GenericService<OrdersApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
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
                string body = string.Empty;
                try { body = await res.Content.ReadAsStringAsync(); } catch { }
                _logger.LogWarning("GetOrdersPageAsync failed: {Status} body: {Body}", res.StatusCode, body);
                return new PageResponse<OrderListItemDto>
                {
                    Items = [],
                    Page = page,
                    Size = size,
                    TotalElements = 0,
                    TotalPages = 0
                };
            }

            var dto = await res.Content.ReadFromJsonAsync<PageResponse<OrderListItemDto>>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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
                string body = string.Empty;
                try { body = await res.Content.ReadAsStringAsync(); } catch { }
                _logger.LogWarning("GetOrderByIdAsync failed: {Status} body: {Body}", res.StatusCode, body);
                return null;
            }

            return await res.Content.ReadFromJsonAsync<OrderDetailsDto>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        var res = await http.PutAsJsonAsync($"/api/orders/{id}/status", newStatus);
        return res.IsSuccessStatusCode;
    }

}