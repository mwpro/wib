using FluentAssertions;
using Wib.Domain.Chores;
using Wib.Domain.Members;

namespace Wib.UnitTests.Chores;

public class ChoreBacklogTests
{
    private readonly DateTime _fixedNowUtc = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private readonly Member _member;

    public ChoreBacklogTests()
    {
        _member = Member.Create("auth0|tester", "Tester", _fixedNowUtc.AddDays(-100));
    }

    [Fact]
    public void Order_ShouldPutHigherUrgencyBandFirst_RegardlessOfRatio()
    {
        // Arrange: daily chore 1 day late (Overdue, 200%) vs weekly chore Neglected (143%)
        var dailyOverdue = CompletedChore("Daily", cadenceDays: 1, daysAgo: 2);
        var weeklyNeglected = CompletedChore("Weekly", cadenceDays: 7, daysAgo: 10);

        // Act
        var backlog = ChoreBacklog.Order([dailyOverdue, weeklyNeglected], _fixedNowUtc);

        // Assert
        backlog.Select(e => e.Chore).Should().ContainInOrder(weeklyNeglected, dailyOverdue);
        backlog.Select(e => e.Freshness.Urgency).Should().ContainInOrder(FreshnessUrgency.Neglected, FreshnessUrgency.Overdue);
    }

    [Fact]
    public void Order_ShouldSortAllBandsMostUrgentFirst_WithUnscheduledLast()
    {
        // Arrange
        var unscheduled = Chore.Create("Unscheduled", null, 1, null, _fixedNowUtc.AddDays(-50));
        var fresh = CompletedChore("Fresh", cadenceDays: 10, daysAgo: 0);
        var dueSoon = CompletedChore("DueSoon", cadenceDays: 10, daysAgo: 9);
        var overdue = CompletedChore("Overdue", cadenceDays: 10, daysAgo: 12);
        var neglected = CompletedChore("Neglected", cadenceDays: 10, daysAgo: 20);

        // Act
        var backlog = ChoreBacklog.Order([unscheduled, fresh, dueSoon, overdue, neglected], _fixedNowUtc);

        // Assert
        backlog.Select(e => e.Chore).Should().Equal(neglected, overdue, dueSoon, fresh, unscheduled);
    }

    [Fact]
    public void Order_WithinSameBand_ShouldPutHigherRatioFirst()
    {
        // Arrange: both Neglected
        var lessNeglected = CompletedChore("Less", cadenceDays: 10, daysAgo: 15);  // 150%
        var moreNeglected = CompletedChore("More", cadenceDays: 10, daysAgo: 30);  // 300%

        // Act
        var backlog = ChoreBacklog.Order([lessNeglected, moreNeglected], _fixedNowUtc);

        // Assert
        backlog.Select(e => e.Chore).Should().Equal(moreNeglected, lessNeglected);
    }

    [Fact]
    public void Order_WithSameBandAndRatio_ShouldPutNewestChoreFirst()
    {
        // Arrange: both unscheduled (no ratio)
        var older = Chore.Create("Older", null, 1, null, _fixedNowUtc.AddDays(-10));
        var newer = Chore.Create("Newer", null, 1, null, _fixedNowUtc.AddDays(-1));

        // Act
        var backlog = ChoreBacklog.Order([older, newer], _fixedNowUtc);

        // Assert
        backlog.Select(e => e.Chore).Should().Equal(newer, older);
    }

    [Fact]
    public void Order_ShouldExposeFreshnessComputedForEachChore()
    {
        // Arrange
        var chore = CompletedChore("Weekly", cadenceDays: 7, daysAgo: 7);

        // Act
        var entry = ChoreBacklog.Order([chore], _fixedNowUtc).Single();

        // Assert
        entry.Freshness.Should().Be(chore.GetFreshness(_fixedNowUtc));
    }

    private Chore CompletedChore(string title, int cadenceDays, int daysAgo)
    {
        var chore = Chore.Create(title, null, 1, cadenceDays, _fixedNowUtc.AddDays(-100));
        chore.Complete(_member, _fixedNowUtc.AddDays(-daysAgo));
        return chore;
    }
}
