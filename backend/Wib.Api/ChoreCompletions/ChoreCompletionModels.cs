namespace Wib.Api.ChoreCompletions;

public record ChoreCompletionItem(
    int Id,
    int ChoreId,
    string ChoreTitle,
    int MemberId,
    string MemberName,
    int PointsAwarded,
    DateTime CompletedAt
);

public record ChoreCompletionsResponse(
    IReadOnlyList<ChoreCompletionItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore
);
