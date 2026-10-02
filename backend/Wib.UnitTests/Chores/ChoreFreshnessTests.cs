using FluentAssertions;
using Wib.Api.Chores;
using Wib.Api.Data.Entities;

namespace Wib.UnitTests.Chores;

public class ChoreFreshnessTests
{
    private readonly DateTime _fixedNowUtc = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc); // Warsaw is UTC+2 (14:00)

    [Fact]
    public void GetFreshness_WhenCadenceDaysIsNull_ShouldReturnUnscheduled()
    {
        // Arrange
        var createdAt = _fixedNowUtc.AddDays(-10);
        var chore = Chore.Create("Test", null, 1, null, createdAt);

        // Act
        var result = chore.GetFreshness(_fixedNowUtc);

        // Assert
        result.Urgency.Should().Be(FreshnessUrgency.Unscheduled);
        result.UrgencyRatio.Should().BeNull();
        result.DaysSinceLastDone.Should().BeNull();
    }

    [Fact]
    public void GetFreshness_WhenCadenceDaysIsZeroOrNegative_ShouldReturnUnscheduled()
    {
        // Arrange
        var createdAt = _fixedNowUtc.AddDays(-5);
        var choreZero = Chore.Create("Test Zero", null, 1, 0, createdAt);
        var choreNegative = Chore.Create("Test Negative", null, 1, -2, createdAt);

        // Act
        var resultZero = choreZero.GetFreshness(_fixedNowUtc);
        var resultNegative = choreNegative.GetFreshness(_fixedNowUtc);

        // Assert
        resultZero.Urgency.Should().Be(FreshnessUrgency.Unscheduled);
        resultZero.UrgencyRatio.Should().BeNull();
        resultNegative.Urgency.Should().Be(FreshnessUrgency.Unscheduled);
        resultNegative.UrgencyRatio.Should().BeNull();
    }

    [Fact]
    public void GetFreshness_WhenLastCompletedAtIsNull_ShouldUseCreatedAtAsReference()
    {
        // Arrange: created 2 days ago in Warsaw time, cadence 10 days -> 2/10 = 0.20 (Fresh)
        var createdAt = _fixedNowUtc.AddDays(-2);
        var chore = Chore.Create("Test", null, 1, 10, createdAt);

        // Act
        var result = chore.GetFreshness(_fixedNowUtc);

        // Assert
        result.DaysSinceLastDone.Should().Be(2);
        result.UrgencyRatio.Should().Be(0.2);
        result.Urgency.Should().Be(FreshnessUrgency.Fresh);
    }

    [Theory]
    [InlineData(0, 10, 0.0, FreshnessUrgency.Fresh)]      // 0% -> Fresh
    [InlineData(5, 10, 0.5, FreshnessUrgency.Fresh)]      // 50% -> Fresh
    [InlineData(79, 100, 0.79, FreshnessUrgency.Fresh)]   // 79% -> Fresh
    [InlineData(8, 10, 0.8, FreshnessUrgency.DueSoon)]    // 80% -> Due Soon
    [InlineData(9, 10, 0.9, FreshnessUrgency.DueSoon)]    // 90% -> Due Soon
    [InlineData(99, 100, 0.99, FreshnessUrgency.DueSoon)] // 99% -> Due Soon
    [InlineData(10, 10, 1.0, FreshnessUrgency.Overdue)]   // 100% -> Overdue
    [InlineData(12, 10, 1.2, FreshnessUrgency.Overdue)]   // 120% -> Overdue
    [InlineData(129, 100, 1.29, FreshnessUrgency.Overdue)]// 129% -> Overdue
    [InlineData(13, 10, 1.3, FreshnessUrgency.Neglected)] // 130% -> Neglected
    [InlineData(25, 10, 2.5, FreshnessUrgency.Neglected)] // 250% -> Neglected
    public void GetFreshness_ShouldMapUrgencyThresholdsCorrectly(
        int daysAgo,
        int cadenceDays,
        double expectedRatio,
        FreshnessUrgency expectedUrgency)
    {
        // Arrange
        var chore = Chore.Create("Test", null, 1, cadenceDays, _fixedNowUtc.AddDays(-100));
        var member = Member.Create("auth0|tester", "Tester", _fixedNowUtc.AddDays(-100));
        chore.Complete(member, _fixedNowUtc.AddDays(-daysAgo));

        // Act
        var result = chore.GetFreshness(_fixedNowUtc);

        // Assert
        result.DaysSinceLastDone.Should().Be(daysAgo);
        result.UrgencyRatio.Should().BeApproximately(expectedRatio, 0.0001);
        result.Urgency.Should().Be(expectedUrgency);
    }

    [Fact]
    public void GetFreshness_CrossingWarsawMidnight_ShouldCountAsNewCalendarDay()
    {
        // Warsaw is UTC+2 on 2026-06-15.
        // Completed at 23:30 Warsaw time on 2026-06-15 (which is 21:30 UTC on 2026-06-15).
        var completedAtUtc = new DateTime(2026, 6, 15, 21, 30, 0, DateTimeKind.Utc);

        // Current time: 00:30 Warsaw time on 2026-06-16 (which is 22:30 UTC on 2026-06-15).
        // Only 1 hour elapsed in physical time, but calendar date in Warsaw rolled from June 15 to June 16.
        var nowUtc = new DateTime(2026, 6, 15, 22, 30, 0, DateTimeKind.Utc);

        var chore = Chore.Create("Test", null, 1, 1, completedAtUtc.AddDays(-5));
        var member = Member.Create("auth0|tester", "Tester", completedAtUtc.AddDays(-5));
        chore.Complete(member, completedAtUtc);

        // Act: 1 calendar day elapsed, cadence 1 day -> ratio 1.0 (100% Overdue)
        var result = chore.GetFreshness(nowUtc);

        // Assert
        result.DaysSinceLastDone.Should().Be(1);
        result.UrgencyRatio.Should().Be(1.0);
        result.Urgency.Should().Be(FreshnessUrgency.Overdue);
    }

    [Fact]
    public void GetFreshness_WhenReferenceDateIsInFuture_ShouldClampToZeroDays()
    {
        // Arrange: reference time is tomorrow (clock skew)
        var chore = Chore.Create("Test", null, 1, 5, _fixedNowUtc);
        var member = Member.Create("auth0|tester", "Tester", _fixedNowUtc);
        chore.Complete(member, _fixedNowUtc.AddDays(1));

        // Act
        var result = chore.GetFreshness(_fixedNowUtc);

        // Assert
        result.DaysSinceLastDone.Should().Be(0);
        result.UrgencyRatio.Should().Be(0.0);
        result.Urgency.Should().Be(FreshnessUrgency.Fresh);
    }
}
