namespace Wib.Domain.Chores;

public record FreshnessResult(
    FreshnessUrgency Urgency,
    double? UrgencyRatio,
    int? DaysSinceLastDone
);
