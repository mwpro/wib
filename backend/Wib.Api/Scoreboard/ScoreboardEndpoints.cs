using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Common;
using Wib.Api.Data;

namespace Wib.Api.Scoreboard;

public static class ScoreboardEndpoints
{
    public static IEndpointRouteBuilder MapScoreboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scoreboard")
            .RequireAuthorization()
            .WithTags("Scoreboard");

        group.MapGet("/{year:int}/{month:int}", async (
            [FromRoute] int year,
            [FromRoute] int month,
            [FromServices] WibDbContext db,
            [FromServices] IScoreboardCalculator calculator,
            CancellationToken cancellationToken) =>
        {
            if (month < 1 || month > 12 || year < 2000 || year > 2100)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["month"] = ["Month must be between 1 and 12, and year between 2000 and 2100."]
                });
            }

            var period = WarsawTimeZone.GetMonthlyPeriod(year, month);
            var response = await BuildScoreboardAsync(period, db, calculator, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("GetScoreboardByMonth");

        return app;
    }

    private static async Task<ScoreboardResponse> BuildScoreboardAsync(
        MonthlyPeriod period,
        WibDbContext db,
        IScoreboardCalculator calculator,
        CancellationToken cancellationToken)
    {
        var members = await db.Members
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var monthlyCompletions = await db.ChoreCompletions
            .AsNoTracking()
            .Where(cc => cc.CompletedAt >= period.StartUtc && cc.CompletedAt < period.EndUtc)
            .GroupBy(cc => cc.CompletedByMemberId)
            .Select(g => new
            {
                MemberId = g.Key,
                Points = g.Sum(x => x.PointsAwarded),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var lifetimeStats = await db.ChoreCompletions
            .AsNoTracking()
            .GroupBy(cc => cc.CompletedByMemberId)
            .Select(g => new
            {
                MemberId = g.Key,
                Points = g.Sum(x => x.PointsAwarded),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var monthlyMap = monthlyCompletions.ToDictionary(x => x.MemberId);
        var lifetimeMap = lifetimeStats.ToDictionary(x => x.MemberId);

        var statsInputs = members.Select(m =>
        {
            monthlyMap.TryGetValue(m.Id, out var mStats);
            lifetimeMap.TryGetValue(m.Id, out var lStats);

            return new MemberStatsInput(
                MemberId: m.Id,
                Name: m.Name,
                MonthlyPoints: mStats?.Points ?? 0,
                MonthlyChoresCompleted: mStats?.Count ?? 0,
                LifetimePoints: lStats?.Points ?? 0,
                LifetimeChoresCompleted: lStats?.Count ?? 0
            );
        }).ToList();

        return calculator.Calculate(period, statsInputs);
    }
}
