using Wib.Domain.Common;

namespace Wib.Domain.Scoreboard;

public interface IScoreboardCalculator
{
    ScoreboardResponse Calculate(MonthlyPeriod period, IReadOnlyList<MemberStatsInput> memberStats);
}
