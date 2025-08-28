using System.Net.Http.Headers;

public sealed class AuthHandler : DelegatingHandler
{
    private readonly AuthState _auth;

    public AuthHandler(AuthState auth)
    {
        _auth = auth;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var isAuth = path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);

        if (!isAuth && request.Headers.Authorization is null)
        {
            var token = _auth.Jwt;
            if (!string.IsNullOrWhiteSpace(token))
            {
                if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = token.Substring("Bearer ".Length);

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, ct);
    }
}