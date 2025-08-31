using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class UsersApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<UserService> logger)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
    private readonly ILogger<UserService> _logger = logger;

    public async Task<List<UserResponse>> GetUsersAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/users";

        try
        {
            var res = await http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                string body = string.Empty;
                try { body = await res.Content.ReadAsStringAsync(); } catch { }
                _logger.LogWarning("GetUsersAsync failed: {Status} body: {Body}", res.StatusCode, body);
                return new List<UserResponse>();
            }

            var dto = await res.Content.ReadFromJsonAsync<List<UserResponse>>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return dto ?? new List<UserResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetUsersAsync: request exception");
            return new List<UserResponse>();
        }
    }

    public async Task<UserResponse?> CreateUserAsync(UserCreateRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/users";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateUserAsync: request exception");
            throw;
        }

        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("CreateUserAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            throw new HttpRequestException($"HTTP {(int)res.StatusCode} {res.StatusCode}: {body}");
        }

        return await res.Content.ReadFromJsonAsync<UserResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }


    public async Task<UserResponse?> UpdateUserAsync(int id, UserUpdateRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/users/{id}";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.PutAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateUserAsync: request exception");
            throw;
        }

        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("UpdateUserAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            // Rzuć wyjątek z treścią, żeby UI mógł pokazać szczegóły (np. pola walidacji).
            throw new HttpRequestException($"HTTP {(int)res.StatusCode} {res.StatusCode}: {body}");
        }

        return await res.Content.ReadFromJsonAsync<UserResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }


    public async Task DeleteUserAsync(int id)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/users/{id}";

        var res = await http.DeleteAsync(url);
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

        var t = token.Trim();
        if (t.StartsWith("\"") && t.EndsWith("\"") && t.Length >= 2)
            t = t.Substring(1, t.Length - 2);
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length);

        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t.Trim());
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));

}