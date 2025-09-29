using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class ReservationsApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<RolesApiService> logger) : GenericService<RolesApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
    private readonly ILogger<RolesApiService> _logger = logger;
    
    public async Task<List<ReservationDto>> GetReservationsAsync(int? branchId = null)
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var url = "/api/warehouse/reservations";
        if (branchId.HasValue)
            url += $"?branchId={branchId}";

        var res = await http.GetAsync(url);

        if (!res.IsSuccessStatusCode)
            return new List<ReservationDto>();

        return await res.Content.ReadFromJsonAsync<List<ReservationDto>>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }
    
    public async Task<bool> FulfillBranchReservationsAsync(int orderId, int branchId)
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var res = await http.PostAsync($"/api/warehouse/orders/{orderId}/fulfill/{branchId}", null);

        return res.IsSuccessStatusCode;
    }
}