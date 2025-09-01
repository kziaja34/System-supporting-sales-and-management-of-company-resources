using Blazored.LocalStorage;

namespace SSSMCR.Web.Services;

public class GenericService(ILocalStorageService storage)
{
    private readonly ILocalStorageService _storage = storage;
    
    public async Task AttachBearerAsync(HttpClient http)
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
}