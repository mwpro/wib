namespace Wib.Domain.Chores;

public enum FreshnessUrgency
{
    Fresh,
    DueSoon,
    Overdue,
    Neglected,
    Unscheduled
}

public static class FreshnessUrgencyExtensions
{
    /// <summary>
    /// A completion outcome counts as "na czas" when it was done before or on its due day.
    /// </summary>
    public static bool IsOnTime(this FreshnessUrgency urgency) =>
        urgency is FreshnessUrgency.Fresh or FreshnessUrgency.DueSoon;
}
