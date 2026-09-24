namespace Wib.Api.Common;

public class JwtAuthOptions
{
    public const string SectionName = "JwtAuth";

    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool BypassAuth { get; set; }

    /// <summary>
    /// Returns the normalized authority URI with a trailing slash.
    /// </summary>
    public string GetNormalizedAuthority()
    {
        if (string.IsNullOrWhiteSpace(Authority))
        {
            return string.Empty;
        }

        return Authority.EndsWith('/') ? Authority : $"{Authority}/";
    }

    /// <summary>
    /// Extracts the domain (host) from the Authority URI.
    /// Throws if Authority is not a valid absolute URI.
    /// </summary>
    public string GetDomain()
    {
        if (string.IsNullOrWhiteSpace(Authority))
        {
            return string.Empty;
        }

        var uri = new Uri(Authority, UriKind.Absolute);
        return uri.Host;
    }
}
