using System.Security.Claims;

namespace Wib.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string? GetSubject(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value
               ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
