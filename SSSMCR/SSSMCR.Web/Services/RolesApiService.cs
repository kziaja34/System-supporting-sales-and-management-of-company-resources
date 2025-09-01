using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class RolesApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<UserService> logger)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
    private readonly ILogger<UserService> _logger = logger;
    
    public async Task<List<RoleResponse>> GetRolesAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/roles";

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
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("GetRolesAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            return new();
        }

        try
        {
            var dto = await res.Content.ReadFromJsonAsync<List<RoleResponse>>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return dto ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRolesAsync: JSON deserialize error");
            return new();
        }
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
}