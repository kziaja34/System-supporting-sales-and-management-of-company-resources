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

        var res = await http.PostAsJsonAsync(url, req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<UserResponse>();
    }

    public async Task<UserResponse?> UpdateUserAsync(int id, UserUpdateRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/users/{id}";

        var res = await http.PutAsJsonAsync(url, req);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<UserResponse>();
    }

    public async Task DeleteUserAsync(int id)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/users/{id}";

        var res = await http.DeleteAsync(url);
        res.EnsureSuccessStatusCode();
    }
}