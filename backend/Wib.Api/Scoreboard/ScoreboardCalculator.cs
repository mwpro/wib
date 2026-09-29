using Wib.Api.Common;

namespace Wib.Api.Scoreboard;

public class ScoreboardCalculator : IScoreboardCalculator
{
    public ScoreboardResponse Calculate(MonthlyPeriod period, IReadOnlyList<MemberStatsInput> memberStats)
    {
        var totalPoints = memberStats.Sum(m => m.MonthlyPoints);
        var totalChores = memberStats.Sum(m => m.MonthlyChoresCompleted);

        var workShareMap = CalculateWorkSharePercentages(memberStats, totalPoints);

        var sorted = memberStats
            .OrderByDescending(m => m.MonthlyPoints)
            .ThenByDescending(m => m.MonthlyChoresCompleted)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.MemberId)
            .ToList();

        var memberItems = new List<MemberScoreboardItem>(sorted.Count);
        var currentRank = 1;

        for (var i = 0; i < sorted.Count; i++)
        {
            var member = sorted[i];

            if (i > 0 && member.MonthlyPoints < sorted[i - 1].MonthlyPoints)
            {
                currentRank = i + 1;
            }

            var workShare = workShareMap.TryGetValue(member.MemberId, out var share) ? share : 0.0;

            memberItems.Add(new MemberScoreboardItem(
                MemberId: member.MemberId,
                Name: member.Name,
                MonthlyPoints: member.MonthlyPoints,
                MonthlyChoresCompleted: member.MonthlyChoresCompleted,
                WorkSharePercentage: workShare,
                LifetimePoints: member.LifetimePoints,
                LifetimeChoresCompleted: member.LifetimeChoresCompleted,
                Rank: currentRank
            ));
        }

        return new ScoreboardResponse(
            Year: period.Year,
            Month: period.Month,
            PeriodStartUtc: period.StartUtc,
            PeriodEndUtc: period.EndUtc,
            TotalHouseholdChoresCompleted: totalChores,
            TotalHouseholdPointsEarned: totalPoints,
            Members: memberItems
        );
    }

    private static Dictionary<int, double> CalculateWorkSharePercentages(
        IReadOnlyList<MemberStatsInput> memberStats, int totalPoints)
    {
        var result = new Dictionary<int, double>();

        if (totalPoints <= 0 || memberStats.Count == 0)
        {
            foreach (var m in memberStats)
            {
                result[m.MemberId] = 0.0;
            }
            return result;
        }

        // Largest Remainder Method (Hamilton-Hare) scaled to 1000 tenths (100.0%)
        const int targetTenths = 1000;

        var intermediate = memberStats.Select(m =>
        {
            var exactTenths = (double)m.MonthlyPoints * targetTenths / totalPoints;
            var floorTenths = (int)Math.Floor(exactTenths);
            var remainder = exactTenths - floorTenths;
            return new
            {
                Member = m,
                FloorTenths = floorTenths,
                Remainder = remainder
            };
        }).ToList();

        var allocatedSum = intermediate.Sum(x => x.FloorTenths);
        var remainderShortage = targetTenths - allocatedSum;

        // Order by largest remainder; tie-break deterministically
        var sortedByRemainder = intermediate
            .OrderByDescending(x => x.Remainder)
            .ThenByDescending(x => x.Member.MonthlyPoints)
            .ThenBy(x => x.Member.MemberId)
            .ToList();

        var extraTenthsMap = new HashSet<int>();
        for (var i = 0; i < remainderShortage && i < sortedByRemainder.Count; i++)
        {
            extraTenthsMap.Add(sortedByRemainder[i].Member.MemberId);
        }

        foreach (var item in intermediate)
        {
            var totalMemberTenths = item.FloorTenths + (extraTenthsMap.Contains(item.Member.MemberId) ? 1 : 0);
            result[item.Member.MemberId] = Math.Round(totalMemberTenths / 10.0, 1);
        }

        return result;
    }
}
