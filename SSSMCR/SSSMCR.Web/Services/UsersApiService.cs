using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class UsersApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<UsersApiService> logger) : GenericService<UsersApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<UsersApiService> _logger = logger;

    public async Task<List<UserResponse>> GetUsersAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/users";

        await AttachBearerAsync(http);
        
        try
        {
            var res = await http.GetAsync(url);
            if (!res.IsSuccessStatusCode)
            {
                var error = await ReadApiErrorAsync(res);
                _logger.LogWarning("GetUsersAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
                return new List<UserResponse>();
            }

            var dto = await ReadJsonAsync<List<UserResponse>>(res.Content);
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

        await EnsureSuccessOrThrowAsync(res, "CreateUserAsync");

        return await ReadJsonAsync<UserResponse>(res.Content);
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

        await EnsureSuccessOrThrowAsync(res, "UpdateUserAsync");

        return await ReadJsonAsync<UserResponse>(res.Content);
    }


    public async Task DeleteUserAsync(int id)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/users/{id}";
        
        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.DeleteAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteUserAsync: request exception");
            throw;
        }

        await EnsureSuccessOrThrowAsync(res, "DeleteUserAsync");
    }
}