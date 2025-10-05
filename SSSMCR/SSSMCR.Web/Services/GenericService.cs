using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace SSSMCR.Web.Services;

public class GenericService<T>(ILogger<T> logger, ILocalStorageService storage) where T : class
{
    private readonly ILogger<T> _logger = logger;
    private readonly ILocalStorageService _storage = storage;
    
    protected async Task AttachBearerAsync(HttpClient http)
    {
        try
        {
            var token = await _storage.GetItemAsStringAsync("jwt");
            if (string.IsNullOrWhiteSpace(token))
            {
                http.DefaultRequestHeaders.Authorization = null;
                return;
            }

            var raw = NormalizeToken(token);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", raw);

            _logger.LogDebug("AttachBearerAsync attached: {Auth}", http.DefaultRequestHeaders.Authorization?.ToString());
        }
        catch (JSException jsEx)
        {
            _logger.LogDebug(jsEx, "AttachBearerAsync skipped: JS interop not available yet (prerender).");
        }
        catch (InvalidOperationException invEx)
        {
            _logger.LogDebug(invEx, "AttachBearerAsync skipped: JS runtime not ready.");
        }
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
    
    protected async Task EnsureSuccessOrThrowAsync(HttpResponseMessage res, string operationName)
    {
        if (res.IsSuccessStatusCode) return;

        var error = await ReadApiErrorAsync(res);
        _logger.LogWarning("{Operation} failed: {Status} error: {Error}", operationName, res.StatusCode, Truncate(error, 1000));
        throw new HttpRequestException(error);
    }

    protected async Task<string> ReadApiErrorAsync(HttpResponseMessage res)
    {
        try
        {
            var json = await res.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
                return $"HTTP {(int)res.StatusCode} {res.StatusCode}";

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var errorProp) && errorProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    return errorProp.GetString() ?? $"HTTP {(int)res.StatusCode} {res.StatusCode}";
                if (doc.RootElement.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == System.Text.Json.JsonValueKind.String)
                    return msgProp.GetString() ?? $"HTTP {(int)res.StatusCode} {res.StatusCode}";
            }
            catch
            {
                // body is not JSON
            }

            return json;
        }
        catch
        {
            return $"HTTP {(int)res.StatusCode} {res.StatusCode}";
        }
    }

    protected async Task<TRes?> ReadJsonAsync<TRes>(HttpContent content)
    {
        try
        {
            return await content.ReadFromJsonAsync<TRes>(
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadJsonAsync<{Type}>: JSON deserialize error", typeof(TRes).Name);
            return default;
        }
    }

    protected string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
}