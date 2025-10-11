using Blazored.LocalStorage;

namespace SSSMCR.Web.Services;

public class InvoiceApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<InvoiceApiService> logger) 
    : GenericService<InvoiceApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<InvoiceApiService> _logger = logger;
    
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
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("GetInvoiceDataUrlAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
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

}
