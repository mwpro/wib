using FluentAssertions;
using Wib.Api.Common;
using Wib.Api.Scoreboard;

namespace Wib.UnitTests.Scoreboard;

public class ScoreboardCalculatorTests
{
    private readonly ScoreboardCalculator _calculator = new();
    private readonly MonthlyPeriod _testPeriod = new(2026, 9, new DateTime(2026, 8, 31, 22, 0, 0, DateTimeKind.Utc), new DateTime(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Calculate_WhenTotalPointsIsZero_ShouldReturnZeroPercentForEveryone()
    {
        // Arrange
        var inputs = new List<MemberStatsInput>
        {
            new(1, "Alice", MonthlyPoints: 0, MonthlyChoresCompleted: 0, LifetimePoints: 100, LifetimeChoresCompleted: 5),
            new(2, "Bob", MonthlyPoints: 0, MonthlyChoresCompleted: 0, LifetimePoints: 50, LifetimeChoresCompleted: 2)
        };

        // Act
        var result = _calculator.Calculate(_testPeriod, inputs);

        // Assert
        result.TotalHouseholdPointsEarned.Should().Be(0);
        result.TotalHouseholdChoresCompleted.Should().Be(0);
        result.Members.Should().HaveCount(2);
        result.Members.Should().AllSatisfy(m =>
        {
            m.WorkSharePercentage.Should().Be(0.0);
            m.Rank.Should().Be(1);
        });
    }

    [Fact]
    public void Calculate_WhenOneMemberHasAllPoints_ShouldReturn100Percent()
    {
        // Arrange
        var inputs = new List<MemberStatsInput>
        {
            new(1, "Alice", MonthlyPoints: 80, MonthlyChoresCompleted: 4, LifetimePoints: 80, LifetimeChoresCompleted: 4),
            new(2, "Bob", MonthlyPoints: 0, MonthlyChoresCompleted: 0, LifetimePoints: 0, LifetimeChoresCompleted: 0)
        };

        // Act
        var result = _calculator.Calculate(_testPeriod, inputs);

        // Assert
        result.TotalHouseholdPointsEarned.Should().Be(80);
        result.TotalHouseholdChoresCompleted.Should().Be(4);
        result.Members.Should().SatisfyRespectively(
            alice =>
            {
                alice.MemberId.Should().Be(1);
                alice.WorkSharePercentage.Should().Be(100.0);
                alice.Rank.Should().Be(1);
            },
            bob =>
            {
                bob.MemberId.Should().Be(2);
                bob.WorkSharePercentage.Should().Be(0.0);
                bob.Rank.Should().Be(2);
            }
        );
    }

    [Fact]
    public void Calculate_WithThreeMembersSplittingEqually_ShouldDistributeRemaindersAndSumToExactly100Percent()
    {
        // Arrange: 3 members with 10 points each
        var inputs = new List<MemberStatsInput>
        {
            new(1, "Alice", 10, 1, 10, 1),
            new(2, "Bob", 10, 1, 10, 1),
            new(3, "Charlie", 10, 1, 10, 1)
        };

        // Act
        var result = _calculator.Calculate(_testPeriod, inputs);

        // Assert
        result.Members.Should().HaveCount(3);
        result.Members.Sum(m => m.WorkSharePercentage).Should().BeApproximately(100.0, 0.0001);

        // Largest Remainder Method gives one 33.4 and two 33.3
        result.Members.Count(m => m.WorkSharePercentage == 33.4).Should().Be(1);
        result.Members.Count(m => m.WorkSharePercentage == 33.3).Should().Be(2);
    }

    [Theory]
    [InlineData(new[] { 1, 2 })]
    [InlineData(new[] { 7, 13, 21 })]
    [InlineData(new[] { 10, 20, 30, 40 })]
    [InlineData(new[] { 1, 1, 1, 1, 1, 1, 1 })]
    [InlineData(new[] { 100, 300, 700 })]
    [InlineData(new[] { 3, 3, 3, 3 })]
    [InlineData(new[] { 17, 43, 89, 102 })]
    public void Calculate_WithVariousPointDistributions_ShouldAlwaysSumToExactly100Percent(int[] points)
    {
        // Arrange
        var inputs = points.Select((p, idx) => new MemberStatsInput(
            MemberId: idx + 1,
            Name: $"Member_{idx + 1}",
            MonthlyPoints: p,
            MonthlyChoresCompleted: 1,
            LifetimePoints: p,
            LifetimeChoresCompleted: 1
        )).ToList();

        // Act
        var result = _calculator.Calculate(_testPeriod, inputs);

        // Assert
        result.Members.Sum(m => m.WorkSharePercentage).Should().BeApproximately(100.0, 0.0001);
    }

    [Fact]
    public void Calculate_Ranking_ShouldRankByMonthlyPointsDescendingAndHandleTies()
    {
        // Arrange:
        // Alice: 100
        // Bob: 75
        // Charlie: 75 (tied with Bob)
        // Dave: 20
        var inputs = new List<MemberStatsInput>
        {
            new(1, "Alice", 100, 5, 200, 10),
            new(2, "Bob", 75, 4, 150, 8),
            new(3, "Charlie", 75, 3, 120, 6),
            new(4, "Dave", 20, 1, 50, 2)
        };

        // Act
        var result = _calculator.Calculate(_testPeriod, inputs);

        // Assert
        result.Members.Should().SatisfyRespectively(
            alice =>
            {
                alice.MemberId.Should().Be(1);
                alice.Rank.Should().Be(1);
                alice.MonthlyPoints.Should().Be(100);
                alice.MonthlyChoresCompleted.Should().Be(5);
            },
            bob =>
            {
                // Bob and Charlie both tied at 75 points -> Rank 2
                // Bob had 4 chores, Charlie had 3 -> Bob ordered before Charlie
                bob.MemberId.Should().Be(2);
                bob.Rank.Should().Be(2);
                bob.MonthlyPoints.Should().Be(75);
                bob.MonthlyChoresCompleted.Should().Be(4);
            },
            charlie =>
            {
                charlie.MemberId.Should().Be(3);
                charlie.Rank.Should().Be(2);
                charlie.MonthlyPoints.Should().Be(75);
                charlie.MonthlyChoresCompleted.Should().Be(3);
            },
            dave =>
            {
                // Dave in 4th place (standard competition ranking: 1, 2, 2, 4)
                dave.MemberId.Should().Be(4);
                dave.Rank.Should().Be(4);
                dave.MonthlyPoints.Should().Be(20);
                dave.MonthlyChoresCompleted.Should().Be(1);
            }
        );
    }

    [Fact]
    public void Calculate_InactiveMembers_ShouldRetainLifetimeStatsWhileHavingZeroMonthlyPoints()
    {
        // Arrange
        var inputs = new List<MemberStatsInput>
        {
            new(1, "Active", MonthlyPoints: 50, MonthlyChoresCompleted: 2, LifetimePoints: 200, LifetimeChoresCompleted: 10),
            new(2, "Inactive", MonthlyPoints: 0, MonthlyChoresCompleted: 0, LifetimePoints: 500, LifetimeChoresCompleted: 25)
        };

        // Act
        var result = _calculator.Calculate(_testPeriod, inputs);

        // Assert
        result.Members.Should().SatisfyRespectively(
            active =>
            {
                active.MemberId.Should().Be(1);
                active.Rank.Should().Be(1);
                active.MonthlyPoints.Should().Be(50);
                active.WorkSharePercentage.Should().Be(100.0);
                active.LifetimePoints.Should().Be(200);
                active.LifetimeChoresCompleted.Should().Be(10);
            },
            inactive =>
            {
                inactive.MemberId.Should().Be(2);
                inactive.Rank.Should().Be(2);
                inactive.MonthlyPoints.Should().Be(0);
                inactive.WorkSharePercentage.Should().Be(0.0);
                inactive.LifetimePoints.Should().Be(500);
                inactive.LifetimeChoresCompleted.Should().Be(25);
            }
        );
    }
}
