using System.Net;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public class SuppliersApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<SuppliersApiService> logger)
    : GenericService<SuppliersApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<SuppliersApiService> _logger = logger;

    public async Task<List<SupplierResponse>> GetSuppliersAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/suppliers";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSuppliersAsync: request exception");
            return new();
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetSuppliersAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return new();
        }

        var dto = await ReadJsonAsync<List<SupplierResponse>>(res.Content);
        return dto ?? new();
    }

    public async Task<SupplierResponse?> GetSupplierAsync(int id)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/suppliers/{id}";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSupplierAsync: request exception");
            return null;
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetSupplierAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return null;
        }

        return await ReadJsonAsync<SupplierResponse>(res.Content);
    }

    public async Task<SupplierResponse?> CreateSupplierAsync(SupplierCreateRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/suppliers";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateSupplierAsync: request exception");
            throw;
        }

        await EnsureSuccessOrThrowAsync(res, "CreateSupplierAsync");
        return await ReadJsonAsync<SupplierResponse>(res.Content);
    }

    public async Task<SupplierResponse?> UpdateSupplierAsync(int id, SupplierUpdateRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/suppliers/{id}";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.PutAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateSupplierAsync: request exception");
            throw;
        }

        await EnsureSuccessOrThrowAsync(res, "UpdateSupplierAsync");
        return await ReadJsonAsync<SupplierResponse>(res.Content);
    }

    public async Task DeleteSupplierAsync(int id)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/suppliers/{id}";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.DeleteAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteSupplierAsync: request exception");
            throw;
        }

        if (res.StatusCode == HttpStatusCode.Conflict)
        {
            var error = await ReadApiErrorAsync(res) ?? "Cannot delete supplier. It is used in other records.";
            _logger.LogWarning("DeleteSupplierAsync conflict: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            throw new HttpRequestException(error);
        }

        await EnsureSuccessOrThrowAsync(res, "DeleteSupplierAsync");
    }

    public async Task<List<SupplierProductResponse>> GetSupplierProductsAsync(int supplierId)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/suppliers/{supplierId}/products";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSupplierProductsAsync: request exception");
            return new();
        }

        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetSupplierProductsAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return new();
        }

        var dto = await ReadJsonAsync<List<SupplierProductResponse>>(res.Content);
        return dto ?? new();
    }
    
    public async Task SetSupplierProductsAsync(int supplierId, List<SupplierProductUpsertDto> items)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/suppliers/{supplierId}/products";

        await AttachBearerAsync(http);

        var payload = new SupplierProductsUpdateRequest { Items = items };

        HttpResponseMessage res;
        try
        {
            res = await http.PutAsJsonAsync(url, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetSupplierProductsAsync: request exception");
            throw;
        }

        await EnsureSuccessOrThrowAsync(res, "SetSupplierProductsAsync");
    }

    public async Task<List<SupplierResponse>> GetSuppliersByProductAsync(int productId)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/suppliers/byproduct/{productId}";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSuppliersByProductAsync: request exception");
            return new();
        }

        if (!res.IsSuccessStatusCode)
        {
            if (res.StatusCode == HttpStatusCode.NotFound) return new();
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetSuppliersByProductAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return new();
        }

        var dto = await ReadJsonAsync<List<SupplierResponse>>(res.Content);
        return dto ?? new();
    }
}