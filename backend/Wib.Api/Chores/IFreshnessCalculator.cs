namespace Wib.Api.Chores;

public interface IFreshnessCalculator
{
    FreshnessResult Calculate(DateTime? lastCompletedAtUtc, DateTime createdAtUtc, int? cadenceDays);
}
