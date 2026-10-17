using FluentAssertions;
using Wib.Domain.Chores;
using Wib.Domain.Members;

namespace Wib.UnitTests.Chores;

public class ChoreCompletionOutcomeTests
{
    // 10:00 UTC is midday in Warsaw, far from any calendar-day boundary.
    private static readonly DateTime CreatedAt = new(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc);

    private static Member NewMember() => Member.Create("auth0|outcome-member", "Outcome Member", CreatedAt);

    [Fact]
    public void Complete_FirstCompletion_ShouldMeasureGapFromCreation()
    {
        // Arrange
        var chore = Chore.Create("Odkurzanie", null, 2, 7, CreatedAt);

        // Act
        var completion = chore.Complete(NewMember(), CreatedAt.AddDays(3));

        // Assert
        completion.DaysSincePrevious.Should().Be(3);
        completion.UrgencyAtCompletion.Should().Be(FreshnessUrgency.Fresh);
    }

    [Theory]
    [InlineData(5, FreshnessUrgency.Fresh)]      // 71%
    [InlineData(6, FreshnessUrgency.DueSoon)]    // 86%
    [InlineData(7, FreshnessUrgency.DueSoon)]    // due day itself is not late
    [InlineData(8, FreshnessUrgency.Overdue)]    // 114%
    [InlineData(9, FreshnessUrgency.Overdue)]    // 129%
    [InlineData(10, FreshnessUrgency.Neglected)] // 143%, 3 days past due
    public void Complete_ShouldJudgeOutcomeAgainstPreviousCompletion(int daysAfterPrevious, FreshnessUrgency expected)
    {
        // Arrange
        var member = NewMember();
        var chore = Chore.Create("Odkurzanie", null, 2, 7, CreatedAt);
        var previous = CreatedAt.AddDays(20);
        chore.Complete(member, previous);

        // Act
        var completion = chore.Complete(member, previous.AddDays(daysAfterPrevious));

        // Assert
        completion.DaysSincePrevious.Should().Be(daysAfterPrevious);
        completion.UrgencyAtCompletion.Should().Be(expected);
    }

    [Fact]
    public void Complete_SameDayRepeat_ShouldBeFreshWithZeroGap()
    {
        // Arrange
        var member = NewMember();
        var chore = Chore.Create("Zmywanie", null, 1, 1, CreatedAt);
        chore.Complete(member, CreatedAt.AddDays(1));

        // Act
        var completion = chore.Complete(member, CreatedAt.AddDays(1).AddHours(2));

        // Assert
        completion.DaysSincePrevious.Should().Be(0);
        completion.UrgencyAtCompletion.Should().Be(FreshnessUrgency.Fresh);
    }

    [Fact]
    public void Complete_DailyChoreOneDayLate_ShouldBeOverdueNotNeglected()
    {
        // Arrange
        var member = NewMember();
        var chore = Chore.Create("Zmywanie", null, 1, 1, CreatedAt);
        chore.Complete(member, CreatedAt.AddDays(1));

        // Act
        var completion = chore.Complete(member, CreatedAt.AddDays(3));

        // Assert
        completion.DaysSincePrevious.Should().Be(2);
        completion.UrgencyAtCompletion.Should().Be(FreshnessUrgency.Overdue);
    }

    [Theory]
    [InlineData(FreshnessUrgency.Fresh, true)]
    [InlineData(FreshnessUrgency.DueSoon, true)]
    [InlineData(FreshnessUrgency.Overdue, false)]
    [InlineData(FreshnessUrgency.Neglected, false)]
    [InlineData(FreshnessUrgency.Unscheduled, false)]
    public void IsOnTime_ShouldCountEarlyAndOnTimeOutcomesOnly(FreshnessUrgency urgency, bool expected)
    {
        urgency.IsOnTime().Should().Be(expected);
    }

    [Fact]
    public void Complete_UnscheduledChore_ShouldRecordGapWithUnscheduledOutcome()
    {
        // Arrange
        var chore = Chore.Create("Mycie okien", null, 5, null, CreatedAt);

        // Act
        var completion = chore.Complete(NewMember(), CreatedAt.AddDays(40));

        // Assert
        completion.DaysSincePrevious.Should().Be(40);
        completion.UrgencyAtCompletion.Should().Be(FreshnessUrgency.Unscheduled);
    }

    [Fact]
    public void Complete_AfterCadenceChange_ShouldJudgeByCadenceInForceAtCompletion()
    {
        // Arrange
        var member = NewMember();
        var chore = Chore.Create("Odkurzanie", null, 2, 7, CreatedAt);
        var early = chore.Complete(member, CreatedAt.AddDays(10));

        // Act
        chore.Update("Odkurzanie", null, 2, 30, CreatedAt.AddDays(11));
        var later = chore.Complete(member, CreatedAt.AddDays(20));

        // Assert
        early.UrgencyAtCompletion.Should().Be(FreshnessUrgency.Neglected);
        later.UrgencyAtCompletion.Should().Be(FreshnessUrgency.Fresh);
    }

    [Fact]
    public void Evaluate_ShouldCountWarsawCalendarDaysNotHours()
    {
        // Arrange: 23:30 Warsaw (CET, UTC+1) to 00:30 Warsaw next day
        var reference = new DateTime(2026, 3, 2, 22, 30, 0, DateTimeKind.Utc);
        var at = new DateTime(2026, 3, 2, 23, 30, 0, DateTimeKind.Utc);

        // Act
        var result = FreshnessCalculator.Evaluate(reference, at, 1);

        // Assert
        result.DaysSinceLastDone.Should().Be(1);
        result.Urgency.Should().Be(FreshnessUrgency.DueSoon);
    }
}
