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
        _member = A.Member().Build();
    }

    [Fact]
    public void Order_ShouldPutHigherUrgencyBandFirst_RegardlessOfRatio()
    {
        // Arrange: daily chore 1 day late (Overdue, 200%) vs weekly chore Neglected (143%)
        var dailyOverdue = CompletedChore(cadenceDays: 1, daysAgo: 2);
        var weeklyNeglected = CompletedChore(cadenceDays: 7, daysAgo: 10);

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
        var unscheduled = A.Chore().Unscheduled().Build();
        var fresh = CompletedChore(cadenceDays: 10, daysAgo: 0);
        var dueSoon = CompletedChore(cadenceDays: 10, daysAgo: 9);
        var overdue = CompletedChore(cadenceDays: 10, daysAgo: 12);
        var neglected = CompletedChore(cadenceDays: 10, daysAgo: 20);

        // Act
        var backlog = ChoreBacklog.Order([unscheduled, fresh, dueSoon, overdue, neglected], _fixedNowUtc);

        // Assert
        backlog.Select(e => e.Chore).Should().Equal(neglected, overdue, dueSoon, fresh, unscheduled);
    }

    [Fact]
    public void Order_WithinSameBand_ShouldPutHigherRatioFirst()
    {
        // Arrange: both Neglected
        var lessNeglected = CompletedChore(cadenceDays: 10, daysAgo: 15);  // 150%
        var moreNeglected = CompletedChore(cadenceDays: 10, daysAgo: 30);  // 300%

        // Act
        var backlog = ChoreBacklog.Order([lessNeglected, moreNeglected], _fixedNowUtc);

        // Assert
        backlog.Select(e => e.Chore).Should().Equal(moreNeglected, lessNeglected);
    }

    [Fact]
    public void Order_WithSameBandAndRatio_ShouldPutNewestChoreFirst()
    {
        // Arrange: both unscheduled (no ratio)
        var older = A.Chore().Unscheduled().CreatedAt(_fixedNowUtc.AddDays(-10)).Build();
        var newer = A.Chore().Unscheduled().CreatedAt(_fixedNowUtc.AddDays(-1)).Build();

        // Act
        var backlog = ChoreBacklog.Order([older, newer], _fixedNowUtc);

        // Assert
        backlog.Select(e => e.Chore).Should().Equal(newer, older);
    }

    [Fact]
    public void Order_ShouldExposeFreshnessComputedForEachChore()
    {
        // Arrange
        var chore = CompletedChore(cadenceDays: 7, daysAgo: 7);

        // Act
        var entry = ChoreBacklog.Order([chore], _fixedNowUtc).Single();

        // Assert
        entry.Freshness.Should().Be(chore.GetFreshness(_fixedNowUtc));
    }

    private Chore CompletedChore(int cadenceDays, int daysAgo)
    {
        var chore = A.Chore().WithCadenceDays(cadenceDays).Build();
        chore.Complete(_member, _fixedNowUtc.AddDays(-daysAgo));
        return chore;
    }
}
