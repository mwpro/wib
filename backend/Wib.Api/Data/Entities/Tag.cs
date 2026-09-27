namespace Wib.Api.Data.Entities;

public class Tag
{
    private readonly List<ChoreTag> _choreTags = [];

    private Tag() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<ChoreTag> ChoreTags => _choreTags.AsReadOnly();

    public static string NormalizeName(string raw) => raw.Trim().ToLowerInvariant();

    public static Tag Create(string name, DateTime nowUtc)
    {
        return new Tag
        {
            Name = NormalizeName(name),
            CreatedAt = nowUtc
        };
    }
}
