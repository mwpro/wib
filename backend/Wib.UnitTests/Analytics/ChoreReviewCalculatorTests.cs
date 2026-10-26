using FluentAssertions;
using Wib.Domain.Analytics;
using Wib.Domain.Chores;
using Wib.Domain.Common;

namespace Wib.UnitTests.Analytics;

public class ChoreReviewCalculatorTests
{
    private static readonly MonthlyPeriod September = WarsawTimeZone.GetMonthlyPeriod(2026, 9);
    private static readonly DateTime AfterSeptember = Utc(2026, 10, 10);
    private static readonly DateTime LongAgo = Utc(2026, 8, 1);

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 10, 0, 0, DateTimeKind.Utc);

    private static ChoreReviewInput Chore(
        int id,
        int? cadenceDays = 7,
        DateTime? createdAt = null,
        bool isArchived = false,
        DateTime? lastCompleted = null,
        int fresh = 0,
        int dueSoon = 0,
        int overdue = 0,
        int neglected = 0,
        int unscheduled = 0,
        string? title = null) =>
        new(
            id,
            title ?? $"Chore {id}",
            cadenceDays,
            createdAt ?? LongAgo,
            isArchived,
            lastCompleted,
            [
                ..Enumerable.Repeat(FreshnessUrgency.Fresh, fresh),
                ..Enumerable.Repeat(FreshnessUrgency.DueSoon, dueSoon),
                ..Enumerable.Repeat(FreshnessUrgency.Overdue, overdue),
                ..Enumerable.Repeat(FreshnessUrgency.Neglected, neglected),
                ..Enumerable.Repeat(FreshnessUrgency.Unscheduled, unscheduled)
            ]);

    [Fact]
    public void Calculate_WhenScoreIsExactlySeventyFivePercent_ShouldCountAsWellDone()
    {
        var chore = Chore(1, lastCompleted: Utc(2026, 9, 29), fresh: 2, dueSoon: 1, overdue: 1);

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, AfterSeptember), [chore]);

        result.NeedsImprovement.Should().BeEmpty();
        result.WellDone.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ChoreId = 1, QualityScore = 0.75, OnTimeCount = 3, LateCount = 1, IsCurrentlyBad = false });
    }

    [Fact]
    public void Calculate_WhenChoreWasNeverDoneAndIsOverdueAtMonthEnd_ShouldScoreZero()
    {
        var chore = Chore(1);

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, AfterSeptember), [chore]);

        result.WellDone.Should().BeEmpty();
        result.NeedsImprovement.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { QualityScore = 0.0, OnTimeCount = 0, LateCount = 0, IsCurrentlyBad = true });
    }

    [Fact]
    public void Calculate_WhenChoreIsBadAtMonthEnd_ShouldAddOneToDenominator()
    {
        var chore = Chore(1, lastCompleted: Utc(2026, 9, 10), fresh: 2, overdue: 1);

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, AfterSeptember), [chore]);

        result.NeedsImprovement.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { QualityScore = 0.5, IsCurrentlyBad = true });
    }

    [Fact]
    public void Calculate_WhenChoreHasNothingToJudge_ShouldLeaveItOutOfBothLists()
    {
        var notDueYet = Chore(1, cadenceDays: 30, lastCompleted: Utc(2026, 9, 5));
        var unscheduled = Chore(2, cadenceDays: null, lastCompleted: Utc(2026, 9, 5), unscheduled: 3);

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, AfterSeptember), [notDueYet, unscheduled]);

        result.NeedsImprovement.Should().BeEmpty();
        result.WellDone.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ShouldIgnoreUnscheduledCompletionsInScore()
    {
        var chore = Chore(1, lastCompleted: Utc(2026, 9, 29), fresh: 1, unscheduled: 5);

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, AfterSeptember), [chore]);

        result.WellDone.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { QualityScore = 1.0, OnTimeCount = 1, LateCount = 0 });
    }

    [Fact]
    public void Calculate_WhenChoreIsArchived_ShouldNeverBeCurrentlyBad()
    {
        var neverDone = Chore(1, isArchived: true);
        var doneLate = Chore(2, isArchived: true, lastCompleted: Utc(2026, 9, 2), overdue: 1);

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, AfterSeptember), [neverDone, doneLate]);

        result.WellDone.Should().BeEmpty();
        result.NeedsImprovement.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ChoreId = 2, QualityScore = 0.0, OnTimeCount = 0, LateCount = 1, IsCurrentlyBad = false, IsArchived = true });
    }

    [Fact]
    public void Calculate_WhenChoreWasCreatedAfterMonthEnd_ShouldLeaveItOut()
    {
        var chore = Chore(1, createdAt: Utc(2026, 10, 2));

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, AfterSeptember), [chore]);

        result.NeedsImprovement.Should().BeEmpty();
        result.WellDone.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ShouldOrderNeedsImprovementByScoreThenLateCount()
    {
        var half = Chore(1, lastCompleted: Utc(2026, 9, 29), fresh: 1, overdue: 1);
        var zeroFewLate = Chore(2, lastCompleted: Utc(2026, 9, 29), overdue: 1);
        var zeroManyLate = Chore(3, lastCompleted: Utc(2026, 9, 29), overdue: 1, neglected: 2);

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, AfterSeptember), [half, zeroFewLate, zeroManyLate]);

        result.NeedsImprovement.Select(i => i.ChoreId).Should().Equal(3, 2, 1);
    }

    [Fact]
    public void Calculate_ShouldOrderWellDoneByScoreThenJudgedCount()
    {
        var mostlyOnTime = Chore(1, lastCompleted: Utc(2026, 9, 29), fresh: 4, overdue: 1);
        var perfectFew = Chore(2, lastCompleted: Utc(2026, 9, 29), fresh: 1);
        var perfectMany = Chore(3, lastCompleted: Utc(2026, 9, 29), fresh: 3);

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, AfterSeptember), [mostlyOnTime, perfectFew, perfectMany]);

        result.WellDone.Select(i => i.ChoreId).Should().Equal(3, 2, 1);
    }

    [Fact]
    public void Calculate_ForCurrentMonth_ShouldJudgeLiveStateAtNow()
    {
        var now = Utc(2026, 9, 15);
        var freshNow = Chore(1, lastCompleted: Utc(2026, 9, 12), fresh: 1);
        var overdueNow = Chore(2, lastCompleted: Utc(2026, 9, 5), fresh: 1);

        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, now), [freshNow, overdueNow]);

        result.IsInProgress.Should().BeTrue();
        result.WellDone.Select(i => i.ChoreId).Should().Equal(1);
        result.NeedsImprovement.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { ChoreId = 2, QualityScore = 0.5, IsCurrentlyBad = true });
    }

    [Fact]
    public void Calculate_ForFutureMonth_ShouldReturnEmptyLists()
    {
        var result = ChoreReviewCalculator.Calculate(ChoreReviewWindow.For(September, Utc(2026, 8, 15)), [Chore(1)]);

        result.IsInProgress.Should().BeFalse();
        result.NeedsImprovement.Should().BeEmpty();
        result.WellDone.Should().BeEmpty();
    }
}
