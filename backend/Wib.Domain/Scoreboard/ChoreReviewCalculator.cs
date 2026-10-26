using Wib.Domain.Chores;
using Wib.Domain.Common;

namespace Wib.Domain.Scoreboard;

public static class ChoreReviewCalculator
{
    public const int WellDoneThresholdPercent = 75;

    public static DateTime GetCutoff(MonthlyPeriod period, DateTime nowUtc) =>
        nowUtc < period.EndUtc ? nowUtc : period.EndUtc;

    public static ChoreReviewResponse Calculate(
        MonthlyPeriod period, DateTime nowUtc, IReadOnlyList<ChoreReviewInput> chores)
    {
        var cutoff = GetCutoff(period, nowUtc);
        var isInProgress = nowUtc >= period.StartUtc && nowUtc < period.EndUtc;

        if (cutoff < period.StartUtc)
        {
            return new ChoreReviewResponse(period.Year, period.Month, isInProgress, [], []);
        }

        var judgedAt = isInProgress ? nowUtc : period.EndUtc.AddTicks(-1);

        var scored = chores
            .Where(c => c.CreatedAt < cutoff)
            .Select(c => Score(c, judgedAt))
            .OfType<ChoreReviewItem>()
            .ToList();

        var needsImprovement = scored
            .Where(i => !IsWellDone(i))
            .OrderBy(i => i.QualityScore)
            .ThenByDescending(i => i.LateCount)
            .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.ChoreId)
            .ToList();

        var wellDone = scored
            .Where(IsWellDone)
            .OrderByDescending(i => i.QualityScore)
            .ThenByDescending(i => i.OnTimeCount + i.LateCount)
            .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.ChoreId)
            .ToList();

        return new ChoreReviewResponse(period.Year, period.Month, isInProgress, needsImprovement, wellDone);
    }

    private static ChoreReviewItem? Score(ChoreReviewInput chore, DateTime judgedAt)
    {
        var onTime = chore.OutcomesInMonth.Count(o => o.IsOnTime);
        var late = chore.OutcomesInMonth.Count(o => o is FreshnessUrgency.Overdue or FreshnessUrgency.Neglected);
        var isCurrentlyBad = IsCurrentlyBad(chore, judgedAt);

        var denominator = onTime + late + (isCurrentlyBad ? 1 : 0);
        if (denominator == 0)
        {
            return null;
        }

        return new ChoreReviewItem(
            chore.ChoreId,
            chore.Title,
            (double)onTime / denominator,
            onTime,
            late,
            isCurrentlyBad,
            chore.IsArchived);
    }

    private static bool IsCurrentlyBad(ChoreReviewInput chore, DateTime judgedAt)
    {
        if (chore.IsArchived)
        {
            return false;
        }

        var freshness = FreshnessCalculator.Evaluate(
            chore.LastCompletedBeforeCutoff ?? chore.CreatedAt, judgedAt, chore.CadenceDays);

        return freshness.Urgency is FreshnessUrgency.Overdue or FreshnessUrgency.Neglected;
    }

    private static bool IsWellDone(ChoreReviewItem item)
    {
        var denominator = item.OnTimeCount + item.LateCount + (item.IsCurrentlyBad ? 1 : 0);
        return item.OnTimeCount * 100 >= denominator * WellDoneThresholdPercent;
    }
}
