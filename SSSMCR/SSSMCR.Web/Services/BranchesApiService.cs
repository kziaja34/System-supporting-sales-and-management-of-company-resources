using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class BranchesApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<BranchesApiService> logger) : GenericService<BranchesApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
    private readonly ILogger<BranchesApiService> _logger = logger;
    
    public async Task<List<BranchResponse>> GetBranchesAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/branches";

        await AttachBearerAsync(http);
        
        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBranchesAsync: request exception");
            return new();
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetBranchesAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return new();
        }

        var dto = await ReadJsonAsync<List<BranchResponse>>(res.Content);
        return dto ?? new();
    }
    
    public async Task<BranchResponse?> CreateBranchAsync(BranchCreateRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/branches";
        
        await AttachBearerAsync(http);
        
        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateBranchAsync: request exception");
            throw;
        }
        
        await EnsureSuccessOrThrowAsync(res, "CreateBranchAsync");

        return await ReadJsonAsync<BranchResponse>(res.Content);
    }

    public async Task<BranchResponse?> UpdateBranchAsync(int id, BranchCreateRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/branches/{id}";
        
        await AttachBearerAsync(http);
        
        HttpResponseMessage res;
        try
        {
            res = await http.PutAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateBranchAsync: request exception");
            throw;
        }
        
        await EnsureSuccessOrThrowAsync(res, "UpdateBranchAsync");
        
        return await ReadJsonAsync<BranchResponse>(res.Content);
    }
    
    public async Task DeleteBranchAsync(int id)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/branches/{id}";
        
        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.DeleteAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteBranchAsync: request exception");
            throw;
        }

        if (res.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await ReadApiErrorAsync(res);
            if (string.IsNullOrWhiteSpace(error))
                error = "Cannot delete branch, it is used in other records.";
            _logger.LogWarning("DeleteBranchAsync conflict: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            throw new HttpRequestException(error);
        }

        await EnsureSuccessOrThrowAsync(res, "DeleteBranchAsync");
    }
}