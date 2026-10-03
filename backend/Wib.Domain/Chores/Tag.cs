namespace Wib.Domain.Chores;

public class Tag
{
    private readonly List<Chore> _chores = [];

    private Tag() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<Chore> Chores => _chores.AsReadOnly();

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
