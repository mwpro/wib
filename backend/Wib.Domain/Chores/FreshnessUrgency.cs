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
    // Extension blocks cannot declare fields or auto-properties, so the backing list lives here.
    private static readonly IReadOnlyList<FreshnessUrgency> JudgedUrgencies =
        Enum.GetValues<FreshnessUrgency>().Where(u => u.IsJudged).ToArray();

    extension(FreshnessUrgency urgency)
    {
        /// <summary>
        /// Every urgency that can be judged against a cadence, i.e. all but <see cref="FreshnessUrgency.Unscheduled"/>.
        /// </summary>
        public static IReadOnlyList<FreshnessUrgency> Judged => JudgedUrgencies;

        /// <summary>
        /// Whether the urgency was measured against a cadence.
        /// </summary>
        private bool IsJudged => urgency != FreshnessUrgency.Unscheduled;

        /// <summary>
        /// A completion outcome counts as "na czas" when it was done before or on its due day.
        /// </summary>
        public bool IsOnTime => urgency is FreshnessUrgency.Fresh or FreshnessUrgency.DueSoon;

        /// <summary>
        /// A completion outcome or live state counts as "z opóźnieniem" once it is past its due day.
        /// </summary>
        public bool IsLate => urgency is FreshnessUrgency.Overdue or FreshnessUrgency.Neglected;

        /// <summary>
        /// Backlog band order: higher comes first.
        /// </summary>
        public int BacklogPriority => urgency switch
        {
            FreshnessUrgency.Neglected => 4,
            FreshnessUrgency.Overdue => 3,
            FreshnessUrgency.DueSoon => 2,
            FreshnessUrgency.Fresh => 1,
            FreshnessUrgency.Unscheduled => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(urgency), urgency, null)
        };
    }
}
