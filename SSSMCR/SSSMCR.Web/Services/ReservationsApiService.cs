using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class ReservationsApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<ReservationsApiService> logger) : GenericService<ReservationsApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
    private readonly ILogger<ReservationsApiService> _logger = logger;
    
    public async Task<List<ReservationDto>> GetReservationsAsync(int? branchId = null)
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        var url = "/api/warehouse/reservations";
        if (branchId.HasValue)
            url += $"?branchId={branchId}";

        try
        {
            var res = await http.GetAsync(url);

            if (!res.IsSuccessStatusCode)
            {
                var error = await ReadApiErrorAsync(res);
                _logger.LogWarning("GetReservationsAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
                return new();
            }

            var dto = await ReadJsonAsync<List<ReservationDto>>(res.Content);
            return dto ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetReservationsAsync: request exception");
            return new();
        }
    }
    
    public async Task<bool> FulfillBranchReservationsAsync(int orderId, int branchId)
    {
        var http = _httpFactory.CreateClient("api");
        await AttachBearerAsync(http);

        try
        {
            var res = await http.PostAsync($"/api/warehouse/orders/{orderId}/fulfill/{branchId}", null);
            await EnsureSuccessOrThrowAsync(res, "FulfillBranchReservationsAsync");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FulfillBranchReservationsAsync failed (orderId={OrderId}, branchId={BranchId})", orderId, branchId);
            return false;
        }
    }
}