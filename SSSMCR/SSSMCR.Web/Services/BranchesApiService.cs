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
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("GetBranchesAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            return new();
        }

        try
        {
            var dto = await res.Content.ReadFromJsonAsync<List<BranchResponse>>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return dto ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBranchesAsync: JSON deserialize error");
            return new();
        }
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
        
        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("CreateBranchAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            throw new HttpRequestException($"HTTP {(int)res.StatusCode} {res.StatusCode}: {body}");
        }

        return await res.Content.ReadFromJsonAsync<BranchResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
        
        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("UpdateBranchAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            throw new HttpRequestException($"HTTP {(int)res.StatusCode} {res.StatusCode}: {body}");
        }
        
        return await res.Content.ReadFromJsonAsync<BranchResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }

            if (res.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var message = string.IsNullOrWhiteSpace(body)
                    ? "Cannot delete branch, it is used in other records."
                    : body;

                _logger.LogWarning("DeleteBranchAsync conflict: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
                throw new HttpRequestException($"HTTP {(int)res.StatusCode} {res.StatusCode}: {message}");
            }

            _logger.LogWarning("DeleteBranchAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            throw new HttpRequestException($"HTTP {(int)res.StatusCode} {res.StatusCode}: {body}");
        }
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
}