using Wib.Api.Common;

namespace Wib.Api.Scoreboard;

public interface IScoreboardCalculator
{
    ScoreboardResponse Calculate(MonthlyPeriod period, IReadOnlyList<MemberStatsInput> memberStats);
}
