namespace Wib.Domain.Chores;

public record ChoreBacklogEntry(Chore Chore, FreshnessResult Freshness);

public static class ChoreBacklog
{
    // Most urgent first: by urgency band, then by ratio within a band, then newest chore first.
    public static IReadOnlyList<ChoreBacklogEntry> Order(IEnumerable<Chore> chores, DateTime nowUtc) =>
        chores
            .Select(chore => new ChoreBacklogEntry(chore, chore.GetFreshness(nowUtc)))
            .OrderByDescending(entry => entry.Freshness.Urgency.BacklogPriority)
            .ThenByDescending(entry => entry.Freshness.UrgencyRatio)
            .ThenByDescending(entry => entry.Chore.CreatedAt)
            .ToList();
}
