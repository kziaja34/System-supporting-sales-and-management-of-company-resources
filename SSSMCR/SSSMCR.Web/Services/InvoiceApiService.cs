using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace SSSMCR.Web.Services;

public class InvoiceApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<InvoiceApiService> logger, IJSRuntime jsRuntime) 
    : GenericService<InvoiceApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILocalStorageService _storage = storage;
    private readonly ILogger<InvoiceApiService> _logger = logger;
    private readonly IJSRuntime _jsRuntime = jsRuntime;
    
    public async Task<string?> GetInvoiceDataUrlAsync(int orderId)
    {
        var http = _httpFactory.CreateClient("api");
        var url = $"/api/invoices/generate/{orderId}";

        await AttachBearerAsync(http);

        HttpResponseMessage res;
        try
        {
            res = await http.GetAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetInvoiceDataUrlAsync: request exception");
            return null;
        }

        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { }
            _logger.LogWarning("GetInvoiceDataUrlAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            return null;
        }

        try
        {
            var pdfBytes = await res.Content.ReadAsByteArrayAsync();
            var base64 = Convert.ToBase64String(pdfBytes);
            return $"data:application/pdf;base64,{base64}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetInvoiceDataUrlAsync: error handling PDF");
            return null;
        }
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
}
