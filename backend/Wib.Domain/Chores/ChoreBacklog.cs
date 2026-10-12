namespace Wib.Domain.Chores;

public record ChoreBacklogEntry(Chore Chore, FreshnessResult Freshness);

public static class ChoreBacklog
{
    // Most urgent first: by urgency band, then by ratio within a band, then newest chore first.
    public static IReadOnlyList<ChoreBacklogEntry> Order(IEnumerable<Chore> chores, DateTime nowUtc) =>
        chores
            .Select(chore => new ChoreBacklogEntry(chore, chore.GetFreshness(nowUtc)))
            .OrderByDescending(entry => BandPriority(entry.Freshness.Urgency))
            .ThenByDescending(entry => entry.Freshness.UrgencyRatio)
            .ThenByDescending(entry => entry.Chore.CreatedAt)
            .ToList();

    private static int BandPriority(FreshnessUrgency urgency) => urgency switch
    {
        FreshnessUrgency.Neglected => 4,
        FreshnessUrgency.Overdue => 3,
        FreshnessUrgency.DueSoon => 2,
        FreshnessUrgency.Fresh => 1,
        FreshnessUrgency.Unscheduled => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(urgency), urgency, null)
    };
}
