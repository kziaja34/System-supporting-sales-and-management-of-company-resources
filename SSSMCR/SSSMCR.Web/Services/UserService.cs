using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

public interface IUserService
{
    Task<UserResponse> GetMeAsync();
    Task<bool> UpdateMeAsync(UpdateMeRequest req);
    Task ChangePasswordAsync(ChangePasswordRequest req);
}

public class UserService : IUserService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILocalStorageService _storage;
    private readonly ILogger<UserService> _logger;

    public UserService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<UserService> logger)
    {
        _httpFactory = httpFactory;
        _storage = storage;
        _logger = logger;
    }

    public async Task<UserResponse> GetMeAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/me/data";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMeAsync: request exception");
            return new UserResponse();
        }

        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("GetMeAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            return new UserResponse();
        }

        try
        {
            var dto = await res.Content.ReadFromJsonAsync<UserResponse>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return dto ?? new UserResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMeAsync: JSON deserialize error");
            return new UserResponse();
        }
    }

    public async Task<bool> UpdateMeAsync(UpdateMeRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/me";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.PutAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateMeAsync: request exception");
            return false;
        }

        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("UpdateMeAsync failed: {Status} body: {Body}",
                res.StatusCode, Truncate(body, 1000));
            return false;
        }

        return true;
    }


    public async Task ChangePasswordAsync(ChangePasswordRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/me/change-password";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ChangePasswordAsync: request exception");
            throw;
        }

        res.EnsureSuccessStatusCode();
    }

    private async Task AttachBearerAsync(HttpClient http)
    {
        var token = await _storage.GetItemAsStringAsync("jwt");
        if (string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization = null;
            return;
        }

        var raw = NormalizeToken(token);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", raw);
    }

    private static string NormalizeToken(string token)
    {
        var t = token.Trim();
        if (t.StartsWith("\"") && t.EndsWith("\"") && t.Length >= 2)
            t = t.Substring(1, t.Length - 2);
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length);
        return t.Trim();
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
}
