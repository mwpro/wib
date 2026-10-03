using FluentAssertions;
using Wib.Domain.Common;

namespace Wib.UnitTests.Scoreboard;

public class WarsawMonthlyBoundaryTests
{
    [Fact]
    public void GetMonthlyPeriod_InWinterStandardTime_ShouldHaveUtcPlusOneBoundary()
    {
        // January 2026 is CET (UTC+1)
        // 2026-01-01 00:00:00 Warsaw = 2025-12-31 23:00:00 UTC
        // 2026-02-01 00:00:00 Warsaw = 2026-01-31 23:00:00 UTC
        var period = WarsawTimeZone.GetMonthlyPeriod(2026, 1);

        period.Year.Should().Be(2026);
        period.Month.Should().Be(1);
        period.StartUtc.Should().Be(new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc));
        period.EndUtc.Should().Be(new DateTime(2026, 1, 31, 23, 0, 0, DateTimeKind.Utc));

        (period.EndUtc - period.StartUtc).TotalDays.Should().Be(31);
    }

    [Fact]
    public void GetMonthlyPeriod_InSummerDaylightSavingTime_ShouldHaveUtcPlusTwoBoundary()
    {
        // July 2026 is CEST (UTC+2)
        // 2026-07-01 00:00:00 Warsaw = 2026-06-30 22:00:00 UTC
        // 2026-08-01 00:00:00 Warsaw = 2026-07-31 22:00:00 UTC
        var period = WarsawTimeZone.GetMonthlyPeriod(2026, 7);

        period.Year.Should().Be(2026);
        period.Month.Should().Be(7);
        period.StartUtc.Should().Be(new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc));
        period.EndUtc.Should().Be(new DateTime(2026, 7, 31, 22, 0, 0, DateTimeKind.Utc));

        (period.EndUtc - period.StartUtc).TotalDays.Should().Be(31);
    }

    [Fact]
    public void GetMonthlyPeriod_DuringMarchSpringForward_ShouldSpanTransitionCorrectly()
    {
        // March starts in CET (UTC+1) and April starts in CEST (UTC+2)
        // March 1 00:00 Warsaw = Feb 28 23:00 UTC
        // April 1 00:00 Warsaw = March 31 22:00 UTC
        var period = WarsawTimeZone.GetMonthlyPeriod(2026, 3);

        period.StartUtc.Should().Be(new DateTime(2026, 2, 28, 23, 0, 0, DateTimeKind.Utc));
        period.EndUtc.Should().Be(new DateTime(2026, 3, 31, 22, 0, 0, DateTimeKind.Utc));

        // 31 calendar days in Warsaw, but physically 31 days minus 1 hour due to spring forward
        (period.EndUtc - period.StartUtc).TotalHours.Should().Be(31 * 24 - 1);
    }

    [Fact]
    public void GetMonthlyPeriod_DuringOctoberFallBack_ShouldSpanTransitionCorrectly()
    {
        // October starts in CEST (UTC+2) and November starts in CET (UTC+1)
        // Oct 1 00:00 Warsaw = Sept 30 22:00 UTC
        // Nov 1 00:00 Warsaw = Oct 31 23:00 UTC
        var period = WarsawTimeZone.GetMonthlyPeriod(2026, 10);

        period.StartUtc.Should().Be(new DateTime(2026, 9, 30, 22, 0, 0, DateTimeKind.Utc));
        period.EndUtc.Should().Be(new DateTime(2026, 10, 31, 23, 0, 0, DateTimeKind.Utc));

        // 31 calendar days in Warsaw, but physically 31 days plus 1 hour due to fall back
        (period.EndUtc - period.StartUtc).TotalHours.Should().Be(31 * 24 + 1);
    }

    [Fact]
    public void GetMonthlyPeriod_InLeapYearFebruary_ShouldSpan29Days()
    {
        // 2028 is a leap year (Feb 1 to March 1)
        var period = WarsawTimeZone.GetMonthlyPeriod(2028, 2);

        period.StartUtc.Should().Be(new DateTime(2028, 1, 31, 23, 0, 0, DateTimeKind.Utc));
        period.EndUtc.Should().Be(new DateTime(2028, 2, 29, 23, 0, 0, DateTimeKind.Utc));
        (period.EndUtc - period.StartUtc).TotalDays.Should().Be(29);
    }

    [Fact]
    public void GetMonthlyPeriod_InNonLeapYearFebruary_ShouldSpan28Days()
    {
        // 2027 is not a leap year (Feb 1 to March 1)
        var period = WarsawTimeZone.GetMonthlyPeriod(2027, 2);

        period.StartUtc.Should().Be(new DateTime(2027, 1, 31, 23, 0, 0, DateTimeKind.Utc));
        period.EndUtc.Should().Be(new DateTime(2027, 2, 28, 23, 0, 0, DateTimeKind.Utc));
        (period.EndUtc - period.StartUtc).TotalDays.Should().Be(28);
    }

    [Fact]
    public void GetCurrentMonthlyPeriod_CrossingWarsawMidnight_ShouldRolloverMonth()
    {
        // In August (CEST, UTC+2):
        // 2026-08-31 21:59:59 UTC = 2026-08-31 23:59:59 Warsaw -> Still August
        var justBeforeMidnightUtc = new DateTimeOffset(2026, 8, 31, 21, 59, 59, TimeSpan.Zero);
        var augustPeriod = WarsawTimeZone.GetCurrentMonthlyPeriod(justBeforeMidnightUtc);

        augustPeriod.Year.Should().Be(2026);
        augustPeriod.Month.Should().Be(8);

        // 2026-08-31 22:00:00 UTC = 2026-09-01 00:00:00 Warsaw -> Rolled over to September
        var exactlyMidnightUtc = new DateTimeOffset(2026, 8, 31, 22, 0, 0, TimeSpan.Zero);
        var septemberPeriod = WarsawTimeZone.GetCurrentMonthlyPeriod(exactlyMidnightUtc);

        septemberPeriod.Year.Should().Be(2026);
        septemberPeriod.Month.Should().Be(9);
    }
}
