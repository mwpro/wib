using Wib.Domain.Chores;

namespace Wib.Api.Chores;

public record ChoreResponse(
    int Id,
    string Title,
    string? Description,
    int Points,
    int? CadenceDays,
    DateTime? LastCompletedAt,
    IReadOnlyList<string> Tags,
    bool IsArchived,
    string Urgency,
    double? UrgencyRatio,
    int? DaysSinceLastDone,
    DateTime CreatedAt,
    DateTime? UpdatedAt
)
{
    public static ChoreResponse Create(Chore chore, DateTime nowUtc) =>
        Create(chore, chore.GetFreshness(nowUtc));

    public static ChoreResponse Create(ChoreBacklogEntry entry) =>
        Create(entry.Chore, entry.Freshness);

    private static ChoreResponse Create(Chore chore, FreshnessResult freshness)
    {
        var tagNames = chore.Tags
            .Select(t => t.Name)
            .OrderBy(t => t)
            .ToList();

        return new ChoreResponse(
            chore.Id,
            chore.Title,
            chore.Description,
            chore.Points,
            chore.CadenceDays,
            chore.LastCompletedAt,
            tagNames,
            chore.IsArchived,
            freshness.Urgency.ToString(),
            freshness.UrgencyRatio,
            freshness.DaysSinceLastDone,
            chore.CreatedAt,
            chore.UpdatedAt
        );
    }
}

public record CompleteChoreResponse(
    ChoreResponse Chore,
    int MemberWalletBalance,
    int PointsAwarded
);

public record CreateChoreRequest(
    string Title,
    string? Description = null,
    int Points = 1,
    int? CadenceDays = null,
    IReadOnlyList<string>? Tags = null
);

public record UpdateChoreRequest(
    string Title,
    string? Description = null,
    int Points = 1,
    int? CadenceDays = null,
    IReadOnlyList<string>? Tags = null
);

public record ChoreHistoryItem(
    int Id,
    int MemberId,
    string MemberName,
    int PointsAwarded,
    DateTime CompletedAt,
    int DaysSincePrevious,
    string UrgencyAtCompletion
)
{
    public static ChoreHistoryItem Create(ChoreCompletion completion, string memberName) =>
        new(
            completion.Id,
            completion.CompletedByMemberId,
            memberName,
            completion.PointsAwarded,
            completion.CompletedAt,
            completion.DaysSincePrevious,
            completion.UrgencyAtCompletion.ToString()
        );
}

public record ChoreHistoryMemberShare(
    int MemberId,
    string MemberName,
    int Count
);

/// <param name="OutcomeCounts">Completion count per judged outcome (Fresh, DueSoon, Overdue, Neglected), zeros included.</param>
public record ChoreHistorySummary(
    int TotalCount,
    IReadOnlyDictionary<string, int> OutcomeCounts,
    int OnTimeCount,
    IReadOnlyList<ChoreHistoryMemberShare> Members
);

public record ChoreHistoryResponse(
    IReadOnlyList<ChoreHistoryItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore
);
