using Wib.Domain.Common;

namespace Wib.Domain.Analytics;

public record ChoreReviewWindow(MonthlyPeriod Period, DateTime Cutoff, DateTime JudgedAt, bool IsInProgress)
{
    public bool HasStarted => Cutoff >= Period.StartUtc;

    public static ChoreReviewWindow For(MonthlyPeriod period, DateTime nowUtc)
    {
        var isInProgress = nowUtc >= period.StartUtc && nowUtc < period.EndUtc;
        var cutoff = nowUtc < period.EndUtc ? nowUtc : period.EndUtc;
        var judgedAt = isInProgress ? nowUtc : period.EndUtc.AddTicks(-1);
        return new ChoreReviewWindow(period, cutoff, judgedAt, isInProgress);
    }
}
