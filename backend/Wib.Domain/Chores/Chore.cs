using Wib.Domain.Common;
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

    public const int TitleMaxLength = 255;
    public const int DescriptionMaxLength = 2000;

    public static Result<Chore> Create(string title, string? description, int points, int? cadenceDays, DateTime nowUtc)
    {
        var validation = Validate(title, description, points, cadenceDays);
        if (validation.IsFailure)
            return validation.Error;

        return new Chore
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Points = points,
            CadenceDays = cadenceDays,
            CreatedAt = nowUtc
        };
    }

    public Result Update(string title, string? description, int points, int? cadenceDays, DateTime nowUtc)
    {
        var validation = Validate(title, description, points, cadenceDays);
        if (validation.IsFailure)
            return validation;

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Points = points;
        CadenceDays = cadenceDays;
        UpdatedAt = nowUtc;
        return Result.Success;
    }

    public static Result<IReadOnlyList<string>> NormalizeTagNames(IEnumerable<string>? rawNames)
    {
        var names = (rawNames ?? [])
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(Tag.NormalizeName)
            .Distinct()
            .ToList();

        if (names.Any(name => name.Length > Tag.NameMaxLength))
            return ChoreErrors.TagTooLong;

        return names;
    }

    private static Result Validate(string title, string? description, int points, int? cadenceDays)
    {
        if (string.IsNullOrWhiteSpace(title))
            return ChoreErrors.TitleRequired;
        if (title.Trim().Length > TitleMaxLength)
            return ChoreErrors.TitleTooLong;
        if (description is not null && description.Trim().Length > DescriptionMaxLength)
            return ChoreErrors.DescriptionTooLong;
        if (points < 1)
            return ChoreErrors.PointsTooLow;
        if (cadenceDays < 1)
            return ChoreErrors.CadenceTooShort;

        return Result.Success;
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
