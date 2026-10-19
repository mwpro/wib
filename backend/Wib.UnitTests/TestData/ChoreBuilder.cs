using Wib.Domain.Chores;

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
    private bool _archived;

    public ChoreBuilder WithTitle(string title) { _title = title; return this; }
    public ChoreBuilder WithPoints(int points) { _points = points; return this; }
    public ChoreBuilder WithCadenceDays(int? cadenceDays) { _cadenceDays = cadenceDays; return this; }
    public ChoreBuilder Unscheduled() => WithCadenceDays(null);
    public ChoreBuilder CreatedAt(DateTime createdAtUtc) { _createdAt = createdAtUtc; return this; }
    public ChoreBuilder Archived() { _archived = true; return this; }

    public Chore Build()
    {
        var chore = Chore.Create(_title, null, _points, _cadenceDays, _createdAt).Value;
        if (_archived)
        {
            chore.Archive(_createdAt);
        }
        return chore;
    }
}
