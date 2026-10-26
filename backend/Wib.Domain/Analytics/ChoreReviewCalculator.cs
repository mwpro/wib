using Wib.Domain.Chores;

namespace Wib.Domain.Analytics;

public static class ChoreReviewCalculator
{
    public const int WellDoneThresholdPercent = 75;

    public static ChoreReviewResponse Calculate(ChoreReviewWindow window, IReadOnlyList<ChoreReviewInput> chores)
    {
        var period = window.Period;

        if (!window.HasStarted)
        {
            return new ChoreReviewResponse(period.Year, period.Month, window.IsInProgress, [], []);
        }

        var scored = chores
            .Where(c => c.CreatedAt < window.Cutoff)
            .Select(c => Score(c, window.JudgedAt))
            .OfType<ScoredChore>()
            .ToList();

        var needsImprovement = scored
            .Where(s => !s.IsWellDone)
            .Select(s => s.Item)
            .OrderBy(i => i.QualityScore)
            .ThenByDescending(i => i.LateCount)
            .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.ChoreId)
            .ToList();

        var wellDone = scored
            .Where(s => s.IsWellDone)
            .Select(s => s.Item)
            .OrderByDescending(i => i.QualityScore)
            .ThenByDescending(i => i.OnTimeCount + i.LateCount)
            .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.ChoreId)
            .ToList();

        return new ChoreReviewResponse(period.Year, period.Month, window.IsInProgress, needsImprovement, wellDone);
    }

    private record ScoredChore(ChoreReviewItem Item, bool IsWellDone);

    private static ScoredChore? Score(ChoreReviewInput chore, DateTime judgedAt)
    {
        var onTime = chore.OutcomesInMonth.Count(o => o.IsOnTime);
        var late = chore.OutcomesInMonth.Count(o => o.IsLate);
        var isCurrentlyBad = IsCurrentlyBad(chore, judgedAt);

        var denominator = onTime + late + (isCurrentlyBad ? 1 : 0);
        if (denominator == 0)
        {
            return null;
        }

        var item = new ChoreReviewItem(
            chore.ChoreId,
            chore.Title,
            (double)onTime / denominator,
            onTime,
            late,
            isCurrentlyBad,
            chore.IsArchived);

        return new ScoredChore(item, onTime * 100 >= denominator * WellDoneThresholdPercent);
    }

    private static bool IsCurrentlyBad(ChoreReviewInput chore, DateTime judgedAt)
    {
        if (chore.IsArchived)
        {
            return false;
        }

        var freshness = FreshnessCalculator.Evaluate(
            chore.LastCompletedBeforeCutoff ?? chore.CreatedAt, judgedAt, chore.CadenceDays);

        return freshness.Urgency.IsLate;
    }
}
