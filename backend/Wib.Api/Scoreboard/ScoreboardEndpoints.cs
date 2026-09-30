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
            var errors = new Dictionary<string, string[]>();
            if (month < 1 || month > 12) errors["month"] = ["Month must be between 1 and 12."];
            if (year < 2000 || year > 2100) errors["year"] = ["Year must be between 2000 and 2100."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

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

        var allStats = await db.ChoreCompletions
            .AsNoTracking()
            .GroupBy(cc => cc.CompletedByMemberId)
            .Select(g => new
            {
                MemberId = g.Key,
                MonthlyPoints = g.Where(x => x.CompletedAt >= period.StartUtc && x.CompletedAt < period.EndUtc).Sum(x => x.PointsAwarded),
                MonthlyCount = g.Count(x => x.CompletedAt >= period.StartUtc && x.CompletedAt < period.EndUtc),
                LifetimePoints = g.Sum(x => x.PointsAwarded),
                LifetimeCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var statsMap = allStats.ToDictionary(x => x.MemberId);

        var statsInputs = members.Select(m =>
        {
            statsMap.TryGetValue(m.Id, out var s);

            return new MemberStatsInput(
                MemberId: m.Id,
                Name: m.Name,
                MonthlyPoints: s?.MonthlyPoints ?? 0,
                MonthlyChoresCompleted: s?.MonthlyCount ?? 0,
                LifetimePoints: s?.LifetimePoints ?? 0,
                LifetimeChoresCompleted: s?.LifetimeCount ?? 0
            );
        }).ToList();

        return calculator.Calculate(period, statsInputs);
    }
}
