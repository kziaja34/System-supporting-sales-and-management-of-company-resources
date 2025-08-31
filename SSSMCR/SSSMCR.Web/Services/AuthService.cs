using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

public interface IAuthService
{
    Task<bool> LoginAsync(LoginRequest req);
    Task LogoutAsync();
    IEnumerable<string> PasswordStrengthOptional(string pw);
    IEnumerable<string> PasswordStrengthRequired(string pw);
    IEnumerable<string> EmailValidation(string pw);
}

public sealed class AuthService(
    IHttpClientFactory httpFactory,
    ILocalStorageService storage,
    ILogger<AuthService> logger)
    : IAuthService
{
    public async Task<bool> LoginAsync(LoginRequest req)
    {
        var http = httpFactory.CreateClient("api");
        var url = "/api/auth/login";
        logger.LogInformation("LoginAsync -> POST {Url} (email={Email})", url, req?.Email);

        HttpResponseMessage res;
        try
        {
            res = await http.PostAsJsonAsync(url, req);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LoginAsync: request exception (connectivity?)");
            return false;
        }

        logger.LogInformation("LoginAsync <- HTTP {Status} ({Code})", res.StatusCode, (int)res.StatusCode);

        // Na czas debugowania rzuć wyjątkiem przy błędzie, żeby mieć pełny stack/diagnozę
        if (!res.IsSuccessStatusCode)
        {
            string body = string.Empty;
            try { body = await res.Content.ReadAsStringAsync(); } catch { /* ignore */ }
            logger.LogWarning("LoginAsync failed: {Status} body: {Body}", res.StatusCode, Truncate(body, 1000));
            return false;
        }

        // Spróbuj najpierw tekst – łatwiej zdiagnozować, co przyszło
        string raw = await res.Content.ReadAsStringAsync();
        logger.LogDebug("LoginAsync response body (first 500 chars): {Body}", Truncate(raw, 500));

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
            logger.LogError(ex, "LoginAsync: JSON deserialize error");
            return false;
        }

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            logger.LogWarning("LoginAsync: token is null or AccessToken empty. Response body: {Body}", Truncate(raw, 1000));
            return false;
        }

        await storage.SetItemAsync("jwt", token.AccessToken);
        if (token.ExpiresAtUtc is not null) await storage.SetItemAsync("jwt_expires", token.ExpiresAtUtc);
        if (!string.IsNullOrWhiteSpace(token.RefreshToken)) await storage.SetItemAsync("refresh", token.RefreshToken);
        
        if (!string.IsNullOrWhiteSpace(req?.Email))
            await storage.SetItemAsStringAsync("user_email", req.Email);


        logger.LogInformation("LoginAsync succeeded, token stored");
        return true;
    }

    public async Task LogoutAsync()
    {
        await storage.RemoveItemAsync("jwt");
        await storage.RemoveItemAsync("jwt_expires");
        await storage.RemoveItemAsync("refresh");
        await storage.RemoveItemAsync("user_email");
    }
    
    public IEnumerable<string> PasswordStrengthRequired(string pw)
    {
        if (string.IsNullOrWhiteSpace(pw))
        {
            yield return "Password is required!";
            yield break;
        }
        if (pw.Length < 6)
            yield return "Password must be at least of length 6";
    }

    public IEnumerable<string> PasswordStrengthOptional(string pw)
    {
        if (string.IsNullOrWhiteSpace(pw))
            yield break;

        if (pw.Length < 6)
            yield return "Password must be at least of length 6";
    }
    
    public IEnumerable<string> EmailValidation(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            yield return "Email is required!";
            yield break;
        }
        
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            yield return "Email is not valid!";
        }
    }

    private static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
}