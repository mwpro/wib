using FluentAssertions;
using Wib.Api.Common;

namespace Wib.UnitTests;

public class WarsawTimeZoneTests
{
    [Fact]
    public void GetTimeZone_ShouldResolveEuropeWarsawWithoutException()
    {
        // Act
        var tz = WarsawTimeZone.GetTimeZone();

        // Assert
        tz.Should().NotBeNull();
        tz.Id.Should().Be("Europe/Warsaw");
    }

    [Theory]
    [InlineData(2026, 1, 15, 12, 0, 1)] // Winter: UTC+1
    [InlineData(2026, 7, 15, 12, 0, 2)] // Summer: UTC+2
    public void ToWarsawTime_ShouldApplyCorrectOffset(int year, int month, int day, int hour, int minute, int expectedOffsetHours)
    {
        // Arrange
        var utcDate = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        // Act
        var warsawTime = WarsawTimeZone.ToWarsawTime(utcDate);

        // Assert
        warsawTime.Offset.Should().Be(TimeSpan.FromHours(expectedOffsetHours));
        warsawTime.Hour.Should().Be(hour + expectedOffsetHours);
    }
}
