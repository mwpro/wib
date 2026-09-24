namespace Wib.Api.Common;

public class JwtAuthOptions
{
    public const string SectionName = "JwtAuth";

    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public string GetDomain()
    {
        if (string.IsNullOrWhiteSpace(Authority))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(Authority, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        return Authority
            .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
            .Trim('/');
    }
}
