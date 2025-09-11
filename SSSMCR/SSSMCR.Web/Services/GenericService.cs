using System.Net.Http.Headers;
using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace SSSMCR.Web.Services;

public class GenericService<T>(ILogger<T> logger, ILocalStorageService storage) where T : class
{
    private readonly ILogger<T> _logger = logger;
    private readonly ILocalStorageService _storage = storage;
    
    public async Task AttachBearerAsync(HttpClient http)
    {
        try
        {
            // PROBOWAĆ ZAWSZE – podczas prerenderu Blazored.LocalStorage rzuci JSException/InvalidOperationException
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
            // prerender / brak JS – OK, poczekamy do kolejnego renderu
            _logger.LogDebug(jsEx, "AttachBearerAsync skipped: JS interop not available yet (prerender).");
        }
        catch (InvalidOperationException invEx)
        {
            // runtime JS nie gotowy – też OK
            _logger.LogDebug(invEx, "AttachBearerAsync skipped: JS runtime not ready.");
        }
    }


    public static string NormalizeToken(string token)
    {
        var t = token.Trim();
        if (t.StartsWith("\"") && t.EndsWith("\"") && t.Length >= 2)
            t = t.Substring(1, t.Length - 2);
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length);
        return t.Trim();
    }

}