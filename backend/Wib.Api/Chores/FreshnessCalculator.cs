using Wib.Api.Common;

namespace Wib.Api.Chores;

public class FreshnessCalculator(TimeProvider timeProvider) : IFreshnessCalculator
{
    public FreshnessResult Calculate(DateTime? lastCompletedAtUtc, DateTime createdAtUtc, int? cadenceDays)
    {
        if (!cadenceDays.HasValue || cadenceDays.Value <= 0)
        {
            return new FreshnessResult(FreshnessUrgency.Unscheduled, null, null);
        }

        var referenceUtc = lastCompletedAtUtc ?? createdAtUtc;
        var daysElapsed = WarsawTimeZone.GetDaysElapsed(referenceUtc, timeProvider.GetUtcNow());
        var ratio = (double)daysElapsed / cadenceDays.Value;

        var urgency = ratio switch
        {
            < 0.80 => FreshnessUrgency.Fresh,
            < 1.00 => FreshnessUrgency.DueSoon,
            < 1.30 => FreshnessUrgency.Overdue,
            _ => FreshnessUrgency.Neglected
        };

        return new FreshnessResult(urgency, ratio, daysElapsed);
    }
}
