using Wib.Domain.Members;

namespace Wib.Domain.Chores;

public class Chore
{
    private readonly List<Tag> _tags = [];
    private readonly List<ChoreCompletion> _completions = [];

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

    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<ChoreCompletion> Completions => _completions.AsReadOnly();

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

    public ChoreCompletion Complete(Member member, DateTime completedAtUtc)
    {
        var outcome = FreshnessCalculator.JudgeCompletion(LastCompletedAt ?? CreatedAt, completedAtUtc, CadenceDays);

        LastCompletedAt = completedAtUtc;
        member.CreditWallet(Points);

        var completion = ChoreCompletion.Create(
            Id, member.Id, Points, completedAtUtc, outcome.DaysSincePrevious, outcome.Urgency);
        _completions.Add(completion);
        return completion;
    }

    public void SetTags(IReadOnlyList<Tag> tags)
    {
        _tags.Clear();
        _tags.AddRange(tags);
    }

    public FreshnessResult GetFreshness(DateTime nowUtc)
    {
        // An unscheduled chore that was never done has no meaningful "days since"; a scheduled one counts from creation.
        if (LastCompletedAt is null && CadenceDays is not > 0)
        {
            return new FreshnessResult(FreshnessUrgency.Unscheduled, null, null);
        }

        return FreshnessCalculator.Evaluate(LastCompletedAt ?? CreatedAt, nowUtc, CadenceDays);
    }
}
