using Wib.Domain.Chores;
using Wib.Domain.Members;

namespace Wib.UnitTests.TestData;

/// <summary>
/// Builds a <see cref="Chore"/> with sane defaults: "Test chore", 1 point, a 7-day cadence, created now.
/// </summary>
public class ChoreBuilder
{
    private string _title = "Test chore";
    private int _points = 1;
    private int? _cadenceDays = 7;
    private DateTime _createdAt = DateTime.UtcNow;
    private readonly List<(Member Member, DateTime? At)> _completions = [];
    private bool _archived;

    public ChoreBuilder WithTitle(string title) { _title = title; return this; }
    public ChoreBuilder WithPoints(int points) { _points = points; return this; }
    public ChoreBuilder WithCadenceDays(int? cadenceDays) { _cadenceDays = cadenceDays; return this; }
    public ChoreBuilder Unscheduled() => WithCadenceDays(null);
    public ChoreBuilder CreatedAt(DateTime createdAtUtc) { _createdAt = createdAtUtc; return this; }

    /// <summary>
    /// Completes the chore when built. The completion is saved with the chore, so <paramref name="member"/> must already be saved.
    /// </summary>
    public ChoreBuilder CompletedBy(Member member, DateTime? atUtc = null) { _completions.Add((member, atUtc)); return this; }

    /// <summary>Only a chore with a completion can be archived, so combine with <see cref="CompletedBy"/>.</summary>
    public ChoreBuilder Archived() { _archived = true; return this; }

    public Chore Build()
    {
        var chore = Chore.Create(_title, null, _points, _cadenceDays, _createdAt).Value;
        foreach (var (member, at) in _completions)
        {
            chore.Complete(member, at ?? _createdAt);
        }

        if (_archived && chore.Remove(chore.LastCompletedAt ?? _createdAt) != ChoreRemovalResult.Archived)
        {
            throw new InvalidOperationException("A chore without completions is deleted, not archived. Add CompletedBy.");
        }

        return chore;
    }
}
