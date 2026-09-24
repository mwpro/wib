using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Wib.Api.Common;

namespace Wib.Api.Auth;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestAuth";
    private readonly IOptionsMonitor<JwtAuthOptions> _jwtAuthOptions;

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptionsMonitor<JwtAuthOptions> jwtAuthOptions) : base(options, logger, encoder)
    {
        _jwtAuthOptions = jwtAuthOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!_jwtAuthOptions.CurrentValue.BypassAuth)
        {
            return Task.FromResult(AuthenticateResult.Fail("Test authentication bypass is disabled."));
        }

        string? sub = null;
        if (Request.Headers.TryGetValue("X-Test-Sub", out var subHeader) && !string.IsNullOrWhiteSpace(subHeader))
        {
            sub = subHeader.ToString();
        }
        else if (Request.Headers.TryGetValue("X-Test-User-Id", out var userIdHeader) && !string.IsNullOrWhiteSpace(userIdHeader))
        {
            sub = userIdHeader.ToString();
        }
        else if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerStr = authHeader.ToString();
            if (headerStr.StartsWith("Bearer test-", StringComparison.OrdinalIgnoreCase))
            {
                sub = headerStr["Bearer ".Length..].Trim();
            }
            else if (headerStr.StartsWith("Test ", StringComparison.OrdinalIgnoreCase))
            {
                sub = headerStr["Test ".Length..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(sub))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var name = Request.Headers.TryGetValue("X-Test-User-Name", out var nameHeader) && !string.IsNullOrWhiteSpace(nameHeader)
            ? nameHeader.ToString()
            : Request.Headers.TryGetValue("X-Test-Name", out var nHeader) && !string.IsNullOrWhiteSpace(nHeader)
                ? nHeader.ToString()
                : "Test User";

        var email = Request.Headers.TryGetValue("X-Test-User-Email", out var emailHeader) && !string.IsNullOrWhiteSpace(emailHeader)
            ? emailHeader.ToString()
            : Request.Headers.TryGetValue("X-Test-Email", out var eHeader) && !string.IsNullOrWhiteSpace(eHeader)
                ? eHeader.ToString()
                : "test@example.com";

        var picture = Request.Headers.TryGetValue("X-Test-User-Picture", out var pictureHeader) && !string.IsNullOrWhiteSpace(pictureHeader)
            ? pictureHeader.ToString()
            : Request.Headers.TryGetValue("X-Test-Picture", out var pHeader) && !string.IsNullOrWhiteSpace(pHeader)
                ? pHeader.ToString()
                : null;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sub),
            new("sub", sub),
            new(ClaimTypes.Name, name),
            new("name", name),
            new(ClaimTypes.Email, email),
            new("email", email)
        };

        if (!string.IsNullOrWhiteSpace(picture))
        {
            claims.Add(new Claim("picture", picture));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
