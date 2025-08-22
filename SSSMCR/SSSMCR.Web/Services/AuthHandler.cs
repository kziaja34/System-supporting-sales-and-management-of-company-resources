using System.Net.Http.Headers;
using Blazored.LocalStorage;

public sealed class AuthHandler : DelegatingHandler
{
    private readonly ILocalStorageService _storage;
    public AuthHandler(ILocalStorageService storage) => _storage = storage;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        string? token = null;
        try
        {
            token = await _storage.GetItemAsStringAsync("jwt");
        }
        catch (InvalidOperationException)
        {
            // Prerendering: JS interop niedozwolony – pomijamy dodanie nagłówka.
            // Żądanie pójdzie bez tokena, a po rozpoczęciu interakcji kolejne będą już miały token.
        }

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, ct);
    }
}