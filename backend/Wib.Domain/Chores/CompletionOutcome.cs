namespace Wib.Domain.Chores;

public record CompletionOutcome(int DaysSincePrevious, FreshnessUrgency Urgency);
