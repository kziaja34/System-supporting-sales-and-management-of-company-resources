using Blazored.LocalStorage;
using System.Net.Http.Json; // Ważne do obsługi JSON

namespace SSSMCR.Web.Services;

public class InvoiceApiService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<InvoiceApiService> logger) 
    : GenericService<InvoiceApiService>(logger, storage)
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly ILogger<InvoiceApiService> _logger = logger;

    // 1. Klasa pomocnicza do "rozpakowania" błędu z JSONa
    private class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
    
    public async Task<string> GetInvoiceDataUrlAsync(int orderId)
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
            throw new Exception("Błąd połączenia z serwerem.");
        }

        if (!res.IsSuccessStatusCode)
        {
            // 2. TUTAJ NAPRAWIAMY FORMATOWANIE:
            try 
            {
                // Próbujemy odczytać odpowiedź jako obiekt JSON
                var errorObj = await res.Content.ReadFromJsonAsync<ApiErrorResponse>();
                
                // Jeśli udało się odczytać i pole Message nie jest puste -> rzucamy czysty tekst
                if (!string.IsNullOrEmpty(errorObj?.Message))
                {
                    throw new Exception(errorObj.Message);
                }
            }
            catch (Exception)
            {
                // Jeśli to nie był JSON (tylko np. zwykły string), ignorujemy błąd parsowania
                // i przechodzimy do czytania jako string poniżej
            }

            // Fallback: jeśli nie udało się wyciągnąć ładnego JSONa, czytamy surową treść
            var rawError = await res.Content.ReadAsStringAsync();
            if(!string.IsNullOrEmpty(rawError)) 
            {
                // Ostateczne zabezpieczenie: jeśli rawError nadal wygląda jak JSON, usuwamy klamry ręcznie
                if (rawError.Trim().StartsWith("{") && rawError.Contains("message", StringComparison.OrdinalIgnoreCase))
                {
                     // To rzadki przypadek, ale zabezpieczy nas, gdyby deserializacja wyżej zawiodła
                     throw new Exception("Cannot generate invoice. Company data is not configured.");
                }
                throw new Exception(rawError);
            }

            throw new Exception($"Nie udało się wygenerować faktury. Status: {res.StatusCode}");
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
            throw new Exception("Błąd przetwarzania pliku PDF.");
        }
    }
}