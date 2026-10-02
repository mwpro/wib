using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Data;
using Wib.Domain.Common;

namespace Wib.Api.ChoreCompletions;

public static class ChoreCompletionEndpoints
{
    public static IEndpointRouteBuilder MapChoreCompletionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chore-completions")
            .RequireAuthorization()
            .WithTags("ChoreCompletions");

        group.MapGet("/{year:int}/{month:int}", async (
            [FromRoute] int year,
            [FromRoute] int month,
            [FromQuery] int page,
            [FromQuery] int? pageSize,
            [FromQuery] int? limit,
            [FromServices] WibDbContext db,
            CancellationToken cancellationToken) =>
        {
            var effectivePage = page <= 0 ? 1 : page;
            var effectivePageSize = limit ?? pageSize ?? 10;

            var errors = new Dictionary<string, string[]>();
            if (month is < 1 or > 12) errors["month"] = ["Month must be between 1 and 12."];
            if (year is < 2000 or > 2100) errors["year"] = ["Year must be between 2000 and 2100."];
            if (page < 0) errors["page"] = ["Page must be greater than or equal to 1."];
            if (effectivePageSize is < 1 or > 100) errors["pageSize"] = ["Page size must be between 1 and 100."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var period = WarsawTimeZone.GetMonthlyPeriod(year, month);

            var query = db.ChoreCompletions
                .AsNoTracking()
                .Where(cc => cc.CompletedAt >= period.StartUtc && cc.CompletedAt < period.EndUtc);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(cc => cc.CompletedAt)
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .Select(cc => new ChoreCompletionItem(
                    cc.Id,
                    cc.ChoreId,
                    cc.Chore.Title,
                    cc.CompletedByMemberId,
                    cc.CompletedByMember.Name,
                    cc.PointsAwarded,
                    cc.CompletedAt
                ))
                .ToListAsync(cancellationToken);

            var hasMore = (effectivePage * effectivePageSize) < totalCount;

            return Results.Ok(new ChoreCompletionsResponse(
                items,
                effectivePage,
                effectivePageSize,
                totalCount,
                hasMore
            ));
        })
        .WithName("GetChoreCompletionsByMonth");

        return app;
    }
}
