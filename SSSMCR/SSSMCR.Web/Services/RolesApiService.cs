using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class RolesApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<RolesApiService> logger) : GenericService<RolesApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<RolesApiService> _logger = logger;
    
    public async Task<List<RoleResponse>> GetRolesAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/roles";

        await AttachBearerAsync(http);
        
        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRolesAsync: request exception");
            return new();
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetRolesAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return new();
        }

        var dto = await ReadJsonAsync<List<RoleResponse>>(res.Content);
        return dto ?? new();
    }
}
