using Wib.Domain.Chores;

namespace Wib.Domain.Analytics;

public record ChoreReviewResponse(
    int Year,
    int Month,
    bool IsInProgress,
    IReadOnlyList<ChoreReviewItem> NeedsImprovement,
    IReadOnlyList<ChoreReviewItem> WellDone
);

public record ChoreReviewItem(
    int ChoreId,
    string Title,
    double QualityScore,
    int OnTimeCount,
    int LateCount,
    bool IsCurrentlyBad,
    bool IsArchived
);

public record ChoreReviewInput(
    int ChoreId,
    string Title,
    int? CadenceDays,
    DateTime CreatedAt,
    bool IsArchived,
    DateTime? LastCompletedBeforeCutoff,
    IReadOnlyList<FreshnessUrgency> OutcomesInMonth
);
