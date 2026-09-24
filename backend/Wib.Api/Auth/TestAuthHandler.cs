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

        if (!Request.Headers.TryGetValue("X-Test-Sub", out var subHeader) || string.IsNullOrWhiteSpace(subHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var sub = SafeUnescape(subHeader.ToString());

        var name = Request.Headers.TryGetValue("X-Test-User-Name", out var nameHeader) && !string.IsNullOrWhiteSpace(nameHeader)
            ? SafeUnescape(nameHeader.ToString())
            : "Test User";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sub),
            new("sub", sub),
            new(ClaimTypes.Name, name),
            new("name", name)
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string SafeUnescape(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch
        {
            return value;
        }
    }
}
