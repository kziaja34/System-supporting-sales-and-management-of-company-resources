using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class CompanyApiService(
    IHttpClientFactory httpFactory,
    ILocalStorageService storage,
    ILogger<CompanyApiService> logger
) : GenericService<CompanyApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<CompanyApiService> _logger = logger;

    public async Task<CompanyResponse?> GetCompanyAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/company";

        await AttachBearerAsync(http);

        try
        {
            var res = await http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                var err = await ReadApiErrorAsync(res);
                _logger.LogWarning("GetCompanyAsync failed: {Status} {Error}", res.StatusCode, Truncate(err, 400));
                return null;
            }

            return await ReadJsonAsync<CompanyResponse>(res.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCompanyAsync request failed");
            return null;
        }
    }

    public async Task<bool> UpdateCompanyAsync(int id, CompanyRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/company/{id}";

        await AttachBearerAsync(http);

        try
        {
            var res = await http.PutAsJsonAsync(url, req);
            if (!res.IsSuccessStatusCode)
            {
                var err = await ReadApiErrorAsync(res);
                _logger.LogWarning("UpdateCompanyAsync failed: {Status} {Error}", res.StatusCode, Truncate(err, 400));
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateCompanyAsync request failed");
            return false;
        }
    }
}