namespace Wib.Api.Data.Entities;

public class Chore
{
    private readonly List<ChoreTag> _choreTags = [];

    private Chore() { }

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Points { get; private set; } = 1;
    public int? CadenceDays { get; private set; }
    public DateTime? LastCompletedAt { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<ChoreTag> ChoreTags => _choreTags.AsReadOnly();

    public static Chore Create(string title, string? description, int points, int? cadenceDays, DateTime nowUtc)
    {
        return new Chore
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Points = points,
            CadenceDays = cadenceDays,
            CreatedAt = nowUtc
        };
    }

    public void Update(string title, string? description, int points, int? cadenceDays, DateTime nowUtc)
    {
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Points = points;
        CadenceDays = cadenceDays;
        UpdatedAt = nowUtc;
    }

    public void Archive(DateTime nowUtc)
    {
        IsArchived = true;
        UpdatedAt = nowUtc;
    }

    public void MarkCompleted(DateTime completedAtUtc)
    {
        LastCompletedAt = completedAtUtc;
    }

    public void SetTags(IReadOnlyList<Tag> tags)
    {
        _choreTags.Clear();
        foreach (var tag in tags)
        {
            _choreTags.Add(new ChoreTag { Chore = this, Tag = tag });
        }
    }
}
