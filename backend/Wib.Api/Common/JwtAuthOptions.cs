namespace Wib.Api.Common;

public class JwtAuthOptions
{
    public const string SectionName = "JwtAuth";

    public string Authority { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public string GetEffectiveAuthority()
    {
        if (!string.IsNullOrWhiteSpace(Authority))
        {
            return Authority.EndsWith('/') ? Authority : $"{Authority}/";
        }

        if (!string.IsNullOrWhiteSpace(Domain))
        {
            return $"https://{Domain}/";
        }

        return string.Empty;
    }

    public string GetEffectiveDomain()
    {
        if (!string.IsNullOrWhiteSpace(Domain))
        {
            return Domain;
        }

        if (!string.IsNullOrWhiteSpace(Authority))
        {
            var uri = new Uri(Authority);
            return uri.Host;
        }

        return string.Empty;
    }
}
