namespace Wib.Api.Scoreboard;

public record ScoreboardResponse(
    int Year,
    int Month,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    int TotalHouseholdChoresCompleted,
    int TotalHouseholdPointsEarned,
    IReadOnlyList<MemberScoreboardItem> Members
);

public record MemberScoreboardItem(
    int MemberId,
    string Name,
    int MonthlyPoints,
    int MonthlyChoresCompleted,
    double WorkSharePercentage,
    int LifetimePoints,
    int LifetimeChoresCompleted,
    int Rank
);

public record MemberStatsInput(
    int MemberId,
    string Name,
    int MonthlyPoints,
    int MonthlyChoresCompleted,
    int LifetimePoints,
    int LifetimeChoresCompleted
);
