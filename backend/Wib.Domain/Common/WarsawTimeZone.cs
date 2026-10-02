namespace Wib.Domain.Common;

public static class WarsawTimeZone
{
    public const string TimeZoneId = "Europe/Warsaw";

    private static readonly Lazy<TimeZoneInfo> LazyTimeZone = new(() =>
    {
        return TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
    });

    public static TimeZoneInfo GetTimeZone() => LazyTimeZone.Value;

    public static DateTimeOffset ToWarsawTime(DateTime utcDateTime)
    {
        var targetTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTime(new DateTimeOffset(targetTime), GetTimeZone());
    }

    public static DateTimeOffset ToWarsawTime(DateTimeOffset dateTimeOffset)
    {
        return TimeZoneInfo.ConvertTime(dateTimeOffset, GetTimeZone());
    }

    public static DateOnly ToWarsawDate(DateTimeOffset dateTimeOffset)
    {
        var warsawTime = ToWarsawTime(dateTimeOffset);
        return DateOnly.FromDateTime(warsawTime.DateTime);
    }

    public static DateOnly ToWarsawDate(DateTime utcDateTime)
    {
        return ToWarsawDate(new DateTimeOffset(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)));
    }

    public static int GetDaysElapsed(DateTime referenceUtc, DateTimeOffset nowUtc)
    {
        var today = ToWarsawDate(nowUtc);
        var referenceDate = ToWarsawDate(referenceUtc);
        return Math.Max(0, today.DayNumber - referenceDate.DayNumber);
    }

    public static MonthlyPeriod GetMonthlyPeriod(int year, int month)
    {
        var tz = GetTimeZone();
        var startLocal = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddMonths(1);

        var startOffset = tz.GetUtcOffset(startLocal);
        var endOffset = tz.GetUtcOffset(endLocal);

        var startUtc = new DateTimeOffset(startLocal, startOffset).UtcDateTime;
        var endUtc = new DateTimeOffset(endLocal, endOffset).UtcDateTime;

        return new MonthlyPeriod(year, month, startUtc, endUtc);
    }

    public static MonthlyPeriod GetCurrentMonthlyPeriod(DateTimeOffset nowUtc)
    {
        var warsawNow = ToWarsawTime(nowUtc);
        return GetMonthlyPeriod(warsawNow.Year, warsawNow.Month);
    }
}

public readonly record struct MonthlyPeriod(int Year, int Month, DateTime StartUtc, DateTime EndUtc);
