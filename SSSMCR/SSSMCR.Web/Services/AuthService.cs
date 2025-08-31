using System.Net.Http.Json;
using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

public interface IAuthService
{
    Task<bool> LoginAsync(LoginRequest req);
    Task LogoutAsync();
    IEnumerable<string> PasswordStrength(string pw);
}

public sealed class AuthService : IAuthService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILocalStorageService _storage;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IHttpClientFactory httpFactory, ILocalStorageService storage, ILogger<AuthService> logger)
    {
        _httpFactory = httpFactory;
        _storage = storage;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(LoginRequest req)
    {
        var http = _httpFactory.CreateClient("api");
        var url = "/api/auth/login";
        _logger.LogInformation("LoginAsync -> POST {Url} (email={Email})", url, req?.Email);

        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoginAsync: request exception (connectivity?)");
            return false;
        }

        _logger.LogInformation("LoginAsync <- HTTP {Status} ({Code})", res.StatusCode, (int)res.StatusCode);

        // Na czas debugowania rzuć wyjątkiem przy błędzie, żeby mieć pełny stack/diagnozę
        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { /* ignore */ }
            _logger.LogWarning("LoginAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            return false;
        }

        // Spróbuj najpierw tekst – łatwiej zdiagnozować, co przyszło
        string raw = await res.Content.ReadAsStringAsync();
        _logger.LogDebug("LoginAsync response body (first 500 chars): {Body}", Truncate(raw, 500));

        TokenResponse? token;
        try
        {
            token = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(raw, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoginAsync: JSON deserialize error");
            return false;
        }

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            _logger.LogWarning("LoginAsync: token is null or AccessToken empty. Response body: {Body}", Truncate(raw, 1000));
            return false;
        }

        await _storage.SetItemAsync("jwt", token.AccessToken);
        if (token.ExpiresAtUtc is not null) await _storage.SetItemAsync("jwt_expires", token.ExpiresAtUtc);
        if (!string.IsNullOrWhiteSpace(token.RefreshToken)) await _storage.SetItemAsync("refresh", token.RefreshToken);
        
        if (!string.IsNullOrWhiteSpace(req?.Email))
            await _storage.SetItemAsStringAsync("user_email", req.Email);


        _logger.LogInformation("LoginAsync succeeded, token stored");
        return true;
    }

    public async Task LogoutAsync()
    {
        await _storage.RemoveItemAsync("jwt");
        await _storage.RemoveItemAsync("jwt_expires");
        await _storage.RemoveItemAsync("refresh");
        await _storage.RemoveItemAsync("user_email");
    }
    
    public IEnumerable<string> PasswordStrength(string pw)
    {
        if (string.IsNullOrWhiteSpace(pw))
        {
            yield return "Password is required!";
            yield break;
        }
        if (pw.Length < 6)
            yield return "Password must be at least of length 6";
        // if (!Regex.IsMatch(pw, @"[A-Z]"))
        //     yield return "Password must contain at least one capital letter";
        // if (!Regex.IsMatch(pw, @"[a-z]"))
        //     yield return "Password must contain at least one lowercase letter";
        // if (!Regex.IsMatch(pw, @"[0-9]"))
        //     yield return "Password must contain at least one digit";
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
}