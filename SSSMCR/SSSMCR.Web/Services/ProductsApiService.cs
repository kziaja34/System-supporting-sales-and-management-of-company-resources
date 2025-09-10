using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class ProductsApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<UserService> logger) : GenericService(storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
    private readonly ILogger<UserService> _logger = logger;
    
    public async Task<List<ProductResponse>> GetProductsAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/products";

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProductsAsync: request exception");
            return new();
        }

        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("GetProductsAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            return new();
        }

        try
        {
            var dto = await res.Content.ReadFromJsonAsync<List<ProductResponse>>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return dto ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetProductsAsync: JSON deserialize error");
            return new();
        }
    }
    
    public async Task<ProductResponse?> CreateProductAsync(ProductCreateRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/products";
        
        await AttachBearerAsync(http);
        
        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateProductAsync: request exception");
            throw;
        }
        
        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("CreateProductAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            throw new HttpRequestException($"HTTP {(int)res.StatusCode} {res.StatusCode}: {body}");
        }

        return await res.Content.ReadFromJsonAsync<ProductResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    
    public async Task<ProductResponse?> UpdateProductAsync(int id, ProductCreateRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/products/{id}";
        
        await AttachBearerAsync(http);
        
        HttpResponseMessage res;
        try
        {
            res = await http.PutAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateProductAsync: request exception");
            throw;
        }
        
        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("UpdateProductAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            throw new HttpRequestException($"HTTP {(int)res.StatusCode} {res.StatusCode}: {body}");
        }
        
        return await res.Content.ReadFromJsonAsync<ProductResponse>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    
    public async Task DeleteProductAsync(int id)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/products/{id}";
        
        await AttachBearerAsync(http);

        var res = await http.DeleteAsync(url);
        res.EnsureSuccessStatusCode();
    }
    
    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
}