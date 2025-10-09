using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class SuppliersApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<SuppliersApiService> logger)
    : GenericService<SuppliersApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<SuppliersApiService> _logger = logger;
    
    public async Task<List<SupplierResponse>> GetSuppliersAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/suppliers";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSuppliersAsync: request exception");
            return new();
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetSuppliersAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return new();
        }

        var dto = await ReadJsonAsync<List<SupplierResponse>>(res.Content);
        return dto ?? new();
    }
}