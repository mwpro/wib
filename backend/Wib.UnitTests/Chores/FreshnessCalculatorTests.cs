using FluentAssertions;
using Wib.Api.Chores;

namespace Wib.UnitTests.Chores;

public class FreshnessCalculatorTests
{
    private readonly DateTimeOffset _fixedNowUtc = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero); // Warsaw is UTC+2 (14:00)

    private FreshnessCalculator CreateCalculator(DateTimeOffset? nowUtc = null)
    {
        var fakeTimeProvider = new FakeTimeProvider(nowUtc ?? _fixedNowUtc);
        return new FreshnessCalculator(fakeTimeProvider);
    }

    [Fact]
    public void Calculate_WhenCadenceDaysIsNull_ShouldReturnUnscheduled()
    {
        // Arrange
        var calculator = CreateCalculator();
        var createdAt = _fixedNowUtc.UtcDateTime.AddDays(-10);

        // Act
        var result = calculator.Calculate(lastCompletedAtUtc: null, createdAtUtc: createdAt, cadenceDays: null);

        // Assert
        result.Urgency.Should().Be(FreshnessUrgency.Unscheduled);
        result.UrgencyRatio.Should().BeNull();
        result.DaysSinceLastDone.Should().BeNull();
    }

    [Fact]
    public void Calculate_WhenCadenceDaysIsZeroOrNegative_ShouldReturnUnscheduled()
    {
        // Arrange
        var calculator = CreateCalculator();
        var createdAt = _fixedNowUtc.UtcDateTime.AddDays(-5);

        // Act
        var resultZero = calculator.Calculate(null, createdAt, 0);
        var resultNegative = calculator.Calculate(null, createdAt, -2);

        // Assert
        resultZero.Urgency.Should().Be(FreshnessUrgency.Unscheduled);
        resultZero.UrgencyRatio.Should().BeNull();
        resultNegative.Urgency.Should().Be(FreshnessUrgency.Unscheduled);
        resultNegative.UrgencyRatio.Should().BeNull();
    }

    [Fact]
    public void Calculate_WhenLastCompletedAtIsNull_ShouldUseCreatedAtAsReference()
    {
        // Arrange: created 2 days ago in Warsaw time, cadence 10 days -> 2/10 = 0.20 (Fresh)
        var calculator = CreateCalculator();
        var createdAt = _fixedNowUtc.UtcDateTime.AddDays(-2);

        // Act
        var result = calculator.Calculate(lastCompletedAtUtc: null, createdAtUtc: createdAt, cadenceDays: 10);

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
    public void Calculate_ShouldMapUrgencyThresholdsCorrectly(
        int daysAgo,
        int cadenceDays,
        double expectedRatio,
        FreshnessUrgency expectedUrgency)
    {
        // Arrange
        var calculator = CreateCalculator();
        var completedAt = _fixedNowUtc.UtcDateTime.AddDays(-daysAgo);

        // Act
        var result = calculator.Calculate(completedAt, _fixedNowUtc.UtcDateTime.AddDays(-100), cadenceDays);

        // Assert
        result.DaysSinceLastDone.Should().Be(daysAgo);
        result.UrgencyRatio.Should().BeApproximately(expectedRatio, 0.0001);
        result.Urgency.Should().Be(expectedUrgency);
    }

    [Fact]
    public void Calculate_CrossingWarsawMidnight_ShouldCountAsNewCalendarDay()
    {
        // Warsaw is UTC+2 on 2026-06-15.
        // Completed at 23:30 Warsaw time on 2026-06-15 (which is 21:30 UTC on 2026-06-15).
        var completedAtUtc = new DateTime(2026, 6, 15, 21, 30, 0, DateTimeKind.Utc);

        // Current time: 00:30 Warsaw time on 2026-06-16 (which is 22:30 UTC on 2026-06-15).
        // Only 1 hour elapsed in physical time, but calendar date in Warsaw rolled from June 15 to June 16.
        var nowUtc = new DateTimeOffset(2026, 6, 15, 22, 30, 0, TimeSpan.Zero);

        var calculator = CreateCalculator(nowUtc);

        // Act: 1 calendar day elapsed, cadence 1 day -> ratio 1.0 (100% Overdue)
        var result = calculator.Calculate(completedAtUtc, completedAtUtc.AddDays(-5), cadenceDays: 1);

        // Assert
        result.DaysSinceLastDone.Should().Be(1);
        result.UrgencyRatio.Should().Be(1.0);
        result.Urgency.Should().Be(FreshnessUrgency.Overdue);
    }

    [Fact]
    public void Calculate_WhenReferenceDateIsInFuture_ShouldClampToZeroDays()
    {
        // Arrange: reference time is tomorrow (clock skew)
        var calculator = CreateCalculator();
        var completedAt = _fixedNowUtc.UtcDateTime.AddDays(1);

        // Act
        var result = calculator.Calculate(completedAt, _fixedNowUtc.UtcDateTime, cadenceDays: 5);

        // Assert
        result.DaysSinceLastDone.Should().Be(0);
        result.UrgencyRatio.Should().Be(0.0);
        result.Urgency.Should().Be(FreshnessUrgency.Fresh);
    }
}
