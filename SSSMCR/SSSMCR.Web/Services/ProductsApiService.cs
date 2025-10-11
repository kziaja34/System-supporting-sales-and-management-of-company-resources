using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class ProductsApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<ProductsApiService> logger) : GenericService<ProductsApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<ProductsApiService> _logger = logger;
    
    public async Task<List<ProductResponse>> GetProductsAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/products";

        await AttachBearerAsync(http);
        
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
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetProductsAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return new();
        }

        var dto = await ReadJsonAsync<List<ProductResponse>>(res.Content);
        return dto ?? new();
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
        
        await EnsureSuccessOrThrowAsync(res, "CreateProductAsync");

        return await ReadJsonAsync<ProductResponse>(res.Content);
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
        
        await EnsureSuccessOrThrowAsync(res, "UpdateProductAsync");
        
        return await ReadJsonAsync<ProductResponse>(res.Content);
    }
    
    public async Task DeleteProductAsync(int id)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/products/{id}";
        
        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.DeleteAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteProductAsync: request exception");
            throw;
        }
        
        if (res.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var error = await ReadApiErrorAsync(res);
            if (string.IsNullOrWhiteSpace(error))
                error = "Cannot delete product, it is used in other records.";
            _logger.LogWarning("DeleteProductAsync conflict: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            throw new HttpRequestException(error);
        }

        await EnsureSuccessOrThrowAsync(res, "DeleteProductAsync");
    }
}
