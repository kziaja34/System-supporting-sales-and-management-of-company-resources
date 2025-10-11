using System.Text.RegularExpressions;
using Blazored.LocalStorage;
using SSSMCR.Shared.Model;

namespace SSSMCR.Web.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(LoginRequest req);
    Task LogoutAsync();
    IEnumerable<string> PasswordStrengthOptional(string pw);
    IEnumerable<string> PasswordStrengthRequired(string pw);
    IEnumerable<string> EmailValidation(string pw);
    
    bool PermittedForOrders(string? role);
    bool PermittedForManagement(string? role);
    bool PermittedForWarehouse(string? role);
}

public sealed class AuthService(
    IHttpClientFactory httpFactory,
    ILocalStorageService storage,
    ILogger<AuthService> logger)
    : GenericService<AuthService>(logger, storage), IAuthService
{
    private readonly ILogger<AuthService> _logger = logger;
    private readonly ILocalStorageService _storage = storage;

    public async Task<bool> LoginAsync(LoginRequest req)
    {
        var http = httpFactory.CreateClient("api");
        var url = "/api/auth/login";
        _logger.LogInformation("LoginAsync -> POST {Url} (email={Email})", url, req.Email);

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
        
        if (!res.IsSuccessStatusCode)
        {
            var error = await ReadApiErrorAsync(res);
            _logger.LogWarning("LoginAsync failed: {Status} error: {Error}", res.StatusCode, Truncate(error, 1000));
            return false;
        }
        
        var token = await ReadJsonAsync<TokenResponse>(res.Content);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            string raw = string.Empty;
            try { raw = await res.Content.ReadAsStringAsync(); }
            catch
            {
                // ignored
            }

            _logger.LogWarning("LoginAsync: token is null or AccessToken empty. Response body: {Body}", Truncate(raw, 1000));
            return false;
        }

        await _storage.SetItemAsync("jwt", token.AccessToken);
        if (token.ExpiresAtUtc is not null) await _storage.SetItemAsync("jwt_expires", token.ExpiresAtUtc);
        if (!string.IsNullOrWhiteSpace(token.RefreshToken)) await _storage.SetItemAsync("refresh", token.RefreshToken);
        
        if (!string.IsNullOrWhiteSpace(req.Email))
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
        await _storage.RemoveItemAsync("user_role");
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

    public bool PermittedForOrders(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        return role.Contains("Manager") || role.Contains("Administrator") || role.Contains("Seller");
    }

    public bool PermittedForManagement(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        return role.Contains("Administrator");
    }

    public bool PermittedForWarehouse(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return false;
        return role.Contains("WarehouseWorker") || role.Contains("Administrator") || role.Contains("Manager");   
    }
}