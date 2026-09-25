namespace Wib.Api.Chores;

public record FreshnessResult(
    FreshnessUrgency Urgency,
    double? UrgencyRatio,
    int? DaysSinceLastDone
);
