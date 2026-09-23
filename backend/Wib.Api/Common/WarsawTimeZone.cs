namespace Wib.Api.Common;

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
        var tz = GetTimeZone();
        var utcOffset = tz.GetUtcOffset(utcDateTime);
        var targetTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return new DateTimeOffset(targetTime).ToOffset(utcOffset);
    }
}
