using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Data;
using Wib.Domain.Analytics;
using Wib.Domain.Common;
using Wib.Domain.Scoreboard;

namespace Wib.Api.Analytics;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/analytics/{year:int}/{month:int}")
            .RequireAuthorization()
            .WithTags("Analytics");

        group.MapGet("/scoreboard", async (
            [FromRoute] int year,
            [FromRoute] int month,
            [FromServices] WibDbContext db,
            [FromServices] IScoreboardCalculator calculator,
            CancellationToken cancellationToken) =>
        {
            if (ValidatePeriod(year, month) is { } invalid) return invalid;

            var period = WarsawTimeZone.GetMonthlyPeriod(year, month);
            var response = await BuildScoreboard(period, db, calculator, cancellationToken);
            return Results.Ok(response);
        })
        .WithName("GetScoreboardByMonth");

        group.MapGet("/chore-review", async (
            [FromRoute] int year,
            [FromRoute] int month,
            [FromServices] WibDbContext db,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            if (ValidatePeriod(year, month) is { } invalid) return invalid;

            var period = WarsawTimeZone.GetMonthlyPeriod(year, month);
            var window = ChoreReviewWindow.For(period, timeProvider.GetUtcNow().UtcDateTime);
            var cutoff = window.Cutoff;

            var chores = await db.Chores
                .AsNoTracking()
                .Where(c => c.CreatedAt < cutoff)
                .Select(c => new ChoreReviewInput(
                    c.Id,
                    c.Title,
                    c.CadenceDays,
                    c.CreatedAt,
                    c.IsArchived,
                    c.Completions.Where(cc => cc.CompletedAt < cutoff).Max(cc => (DateTime?)cc.CompletedAt),
                    c.Completions
                        .Where(cc => cc.CompletedAt >= period.StartUtc && cc.CompletedAt < cutoff)
                        .Select(cc => cc.UrgencyAtCompletion)
                        .ToList()))
                .ToListAsync(cancellationToken);

            return Results.Ok(ChoreReviewCalculator.Calculate(window, chores));
        })
        .WithName("GetChoreReview");

        return app;
    }

    private static IResult? ValidatePeriod(int year, int month)
    {
        var errors = new Dictionary<string, string[]>();
        if (month is < 1 or > 12) errors["month"] = ["Month must be between 1 and 12."];
        if (year is < 2000 or > 2100) errors["year"] = ["Year must be between 2000 and 2100."];
        return errors.Count > 0 ? Results.ValidationProblem(errors) : null;
    }

    private static async Task<ScoreboardResponse> BuildScoreboard(
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
