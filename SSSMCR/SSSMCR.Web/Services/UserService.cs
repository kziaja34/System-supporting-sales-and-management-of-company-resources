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

public class UserService : SSSMCR.Web.Services.GenericService<UserService>, IUserService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILocalStorageService _storage;
    private readonly ILogger<UserService> _logger;

    public UserService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<UserService> logger)
        : base(logger, storage)
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
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetMeAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return new UserResponse();
        }

        var dto = await ReadJsonAsync<UserResponse>(res.Content);
        return dto ?? new UserResponse();
    }

    public async Task<bool> UpdateMeAsync(UpdateMeRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/me";

        await AttachBearerAsync(http);

        try
        {
            var res = await http.PutAsJsonAsync(url, req);
            await EnsureSuccessOrThrowAsync(res, "UpdateMeAsync");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UpdateMeAsync failed");
            return false;
        }
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

        await EnsureSuccessOrThrowAsync(res, "ChangePasswordAsync");
    }
}
