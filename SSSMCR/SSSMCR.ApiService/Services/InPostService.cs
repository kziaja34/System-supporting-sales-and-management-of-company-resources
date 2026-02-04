using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using SSSMCR.ApiService.Database;
using SSSMCR.ApiService.Model;
using SSSMCR.Shared.Model; // Jeśli tu są jakieś wspólne typy, opcjonalnie

namespace SSSMCR.ApiService.Services;

public interface IInPostService
{
    // Główna metoda biznesowa wywoływana przez kontroler
    Task<InPostLabelResult> ProcessAndGenerateLabelAsync(int orderId);
}

// Prosty DTO do zwrócenia wyniku z serwisu (plik + nazwa)
public record InPostLabelResult(byte[] FileContent, string TrackingNumber);

public class InPostService(HttpClient httpClient, AppDbContext db, ILogger<InPostService> logger) : IInPostService
{
    private const string OrganizationId = "TWOJE_ORG_ID";
    private const string AccessToken = "TWOJ_TOKEN";
    private const string BaseApiUrl = "https://sandbox-api-shipx-pl.easypack24.net/v1/";

    // Konstruktor z Primary Constructor (C# 12) załatwia wstrzykiwanie httpClient, db i loggera
    // Musimy tylko skonfigurować HttpClienta, jeśli nie jest skonfigurowany w Program.cs
    // Ale zakładając, że w Program.cs jest AddHttpClient, to tutaj tylko ustawiamy nagłówki,
    // albo robimy to w metodach. Najlepiej ustawić BaseAddress w Program.cs lub tutaj w bloku inicjalizacji.
    
    private void EnsureClientConfigured()
    {
        if (httpClient.BaseAddress == null)
        {
            httpClient.BaseAddress = new Uri(BaseApiUrl);
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", AccessToken);
        }
    }

    public async Task<InPostLabelResult> ProcessAndGenerateLabelAsync(int orderId)
    {
        EnsureClientConfigured();

        // 1. Logika biznesowa: Pobranie i Walidacja
        var order = await db.Orders
            .Include(o => o.Shipping)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) 
            throw new KeyNotFoundException($"Zamówienie o ID {orderId} nie istnieje.");
        
        if (order.Shipping == null) 
            throw new InvalidOperationException("Zamówienie nie posiada zdefiniowanego sposobu wysyłki (brak obiektu Shipping).");

        if (order.Shipping.IsLabelGenerated) 
            throw new InvalidOperationException($"Etykieta dla zamówienia {orderId} została już wygenerowana (Nr: {order.Shipping.TrackingNumber}).");

        if (string.IsNullOrEmpty(order.Shipping.TargetPoint))
            throw new InvalidOperationException("Brak wybranego Paczkomatu docelowego.");

        // 2. Logika biznesowa: Utworzenie przesyłki w InPost
        var (shipmentId, trackingNumber) = await CreateShipmentInternalAsync(order);

        // 3. Logika biznesowa: Aktualizacja stanu systemu (Baza Danych)
        order.Shipping.ExternalShipmentId = shipmentId;
        order.Shipping.TrackingNumber = trackingNumber;
        order.Shipping.IsLabelGenerated = true;
        order.Status = OrderStatus.Sent; // Zmiana statusu zamówienia

        await db.SaveChangesAsync();
        logger.LogInformation($"Wygenerowano etykietę InPost dla zamówienia {orderId}. Tracking: {trackingNumber}");

        // 4. Logika biznesowa: Pobranie fizycznego pliku
        var pdfBytes = await GetLabelPdfInternalAsync(shipmentId);

        return new InPostLabelResult(pdfBytes, trackingNumber);
    }

    // Metody prywatne - szczegóły implementacji API InPost
    private async Task<(string shipmentId, string trackingNumber)> CreateShipmentInternalAsync(Order order)
    {
        var payload = new
        {
            receiver = new
            {
                email = order.CustomerEmail,
                phone = "500500500", // TODO: Dodać telefon do modelu Order
                name = order.CustomerName
            },
            parcels = new[]
            {
                new { template = "small", dimensions = new { length = 80, width = 380, height = 640, unit = "mm" } }
            },
            service = "inpost_locker_standard",
            custom_attributes = new { target_point = order.Shipping!.TargetPoint }
        };

        var response = await httpClient.PostAsJsonAsync($"organizations/{OrganizationId}/shipments", payload);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogError($"Błąd API InPost (Create): {error}");
            throw new Exception($"Błąd tworzenia przesyłki w InPost: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<InPostShipmentResponse>();
        if (result == null) throw new Exception("Pusta odpowiedź z InPost API.");

        return (result.Id.ToString(), result.TrackingNumber);
    }

    private async Task<byte[]> GetLabelPdfInternalAsync(string shipmentId)
    {
        var response = await httpClient.GetAsync($"shipments/{shipmentId}/label?format=pdf&type=normal");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogError($"Błąd API InPost (GetLabel): {error}");
            throw new Exception("Nie udało się pobrać pliku etykiety.");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    private class InPostShipmentResponse
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
    }
}