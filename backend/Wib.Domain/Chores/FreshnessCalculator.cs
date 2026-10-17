using Wib.Domain.Common;

namespace Wib.Domain.Chores;

public static class FreshnessCalculator
{
    public static FreshnessResult Evaluate(DateTime referenceUtc, DateTime atUtc, int? cadenceDays)
    {
        var (daysElapsed, urgency, ratio) = Judge(referenceUtc, atUtc, cadenceDays);
        return new FreshnessResult(urgency, ratio, daysElapsed);
    }

    public static CompletionOutcome JudgeCompletion(DateTime previousUtc, DateTime completedAtUtc, int? cadenceDays)
    {
        var (daysElapsed, urgency, _) = Judge(previousUtc, completedAtUtc, cadenceDays);
        return new CompletionOutcome(daysElapsed, urgency);
    }

    private static (int DaysElapsed, FreshnessUrgency Urgency, double? Ratio) Judge(
        DateTime referenceUtc, DateTime atUtc, int? cadenceDays)
    {
        var daysElapsed = WarsawTimeZone.GetDaysElapsed(
            referenceUtc, new DateTimeOffset(DateTime.SpecifyKind(atUtc, DateTimeKind.Utc)));

        if (!cadenceDays.HasValue || cadenceDays.Value <= 0)
        {
            return (daysElapsed, FreshnessUrgency.Unscheduled, null);
        }

        var ratio = (double)daysElapsed / cadenceDays.Value;
        var daysPastDue = daysElapsed - cadenceDays.Value;

        var urgency = ratio switch
        {
            < 0.80 => FreshnessUrgency.Fresh,
            <= 1.00 => FreshnessUrgency.DueSoon,
            < 1.30 => FreshnessUrgency.Overdue,
            _ when daysPastDue == 1 => FreshnessUrgency.Overdue,
            _ => FreshnessUrgency.Neglected
        };

        return (daysElapsed, urgency, ratio);
    }
}
