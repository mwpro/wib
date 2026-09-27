using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Auth;
using Wib.Api.Common;
using Wib.Api.Data;
using Wib.Api.Data.Entities;

namespace Wib.Api.Chores;

public static class ChoreEndpoints
{
    public static IEndpointRouteBuilder MapChoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chores")
            .RequireAuthorization()
            .WithTags("Chores")
            .WithValidation();

        group.MapGet("/", async (
            [FromQuery] string? tag,
            [FromServices] WibDbContext db,
            [FromServices] IFreshnessCalculator freshnessCalculator,
            CancellationToken cancellationToken) =>
        {
            IQueryable<Chore> query = db.Chores
                .Where(c => !c.IsArchived)
                .Include(c => c.ChoreTags)
                .ThenInclude(ct => ct.Tag)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var normalizedTag = Tag.NormalizeName(tag);
                query = query.Where(c => c.ChoreTags.Any(ct => ct.Tag.Name == normalizedTag));
            }

            var chores = await query.ToListAsync(cancellationToken);

            var responseList = chores
                .Select(chore =>
                {
                    var freshness = freshnessCalculator.Calculate(chore.LastCompletedAt, chore.CreatedAt, chore.CadenceDays);
                    return new
                    {
                        Freshness = freshness,
                        Response = ChoreResponse.Create(chore, freshness)
                    };
                })
                .OrderByDescending(x => x.Freshness.Urgency != FreshnessUrgency.Unscheduled)
                .ThenByDescending(x => x.Freshness.UrgencyRatio ?? -1.0)
                .ThenByDescending(x => x.Response.CreatedAt)
                .Select(x => x.Response)
                .ToList();

            return Results.Ok(responseList);
        })
        .WithName("GetChores");

        group.MapPost("/", async (
            [FromBody] CreateChoreRequest request,
            [FromServices] WibDbContext db,
            [FromServices] IFreshnessCalculator freshnessCalculator,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

            var chore = Chore.Create(request.Title, request.Description, request.Points, request.CadenceDays, nowUtc);

            var tags = await ResolveTagsAsync(request.Tags, db, nowUtc, cancellationToken);
            chore.SetTags(tags);

            db.Chores.Add(chore);
            await db.SaveChangesAsync(cancellationToken);

            var freshness = freshnessCalculator.Calculate(chore.LastCompletedAt, chore.CreatedAt, chore.CadenceDays);
            var response = ChoreResponse.Create(chore, freshness);

            return Results.Created($"/api/chores/{chore.Id}", response);
        })
        .WithName("CreateChore");

        group.MapPut("/{id:int}", async (
            [FromRoute] int id,
            [FromBody] UpdateChoreRequest request,
            [FromServices] WibDbContext db,
            [FromServices] IFreshnessCalculator freshnessCalculator,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var chore = await db.Chores
                .Include(c => c.ChoreTags)
                .ThenInclude(ct => ct.Tag)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (chore == null || chore.IsArchived)
            {
                return Results.NotFound();
            }

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

            chore.Update(request.Title, request.Description, request.Points, request.CadenceDays, nowUtc);

            var tags = await ResolveTagsAsync(request.Tags, db, nowUtc, cancellationToken);
            chore.SetTags(tags);

            await db.SaveChangesAsync(cancellationToken);

            var freshness = freshnessCalculator.Calculate(chore.LastCompletedAt, chore.CreatedAt, chore.CadenceDays);
            var response = ChoreResponse.Create(chore, freshness);

            return Results.Ok(response);
        })
        .WithName("UpdateChore");

        group.MapDelete("/{id:int}", async (
            [FromRoute] int id,
            [FromServices] WibDbContext db,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var chore = await db.Chores.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (chore == null || chore.IsArchived)
            {
                return Results.NotFound();
            }

            chore.Archive(timeProvider.GetUtcNow().UtcDateTime);
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
        .WithName("DeleteChore");

        group.MapPost("/{id:int}/completion", async (
            [FromRoute] int id,
            [FromServices] WibDbContext db,
            [FromServices] ICurrentMemberAccessor currentMemberAccessor,
            [FromServices] IFreshnessCalculator freshnessCalculator,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var member = await currentMemberAccessor.GetCurrentMemberAsync(cancellationToken);
            if (member == null)
            {
                return Results.Unauthorized();
            }

            var chore = await db.Chores
                .Include(c => c.ChoreTags)
                .ThenInclude(ct => ct.Tag)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (chore == null || chore.IsArchived)
            {
                return Results.NotFound();
            }

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var completion = chore.Complete(member, nowUtc);

            await db.SaveChangesAsync(cancellationToken);

            var freshness = freshnessCalculator.Calculate(chore.LastCompletedAt, chore.CreatedAt, chore.CadenceDays);
            var choreResponse = ChoreResponse.Create(chore, freshness);

            return Results.Ok(new CompleteChoreResponse(choreResponse, member.WalletBalance, completion.PointsAwarded));
        })
        .WithName("CreateChoreCompletion");

        return app;
    }

    private static async Task<List<Tag>> ResolveTagsAsync(
        IReadOnlyList<string>? rawTags, WibDbContext db, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var normalizedNames = (rawTags ?? Array.Empty<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(Tag.NormalizeName)
            .Distinct()
            .ToList();

        if (normalizedNames.Count == 0)
            return [];

        var existingTags = await db.Tags
            .Where(t => normalizedNames.Contains(t.Name))
            .ToListAsync(cancellationToken);

        var existingTagMap = existingTags.ToDictionary(t => t.Name);
        var result = new List<Tag>();

        foreach (var name in normalizedNames)
        {
            if (!existingTagMap.TryGetValue(name, out var tag))
            {
                tag = Tag.Create(name, nowUtc);
                db.Tags.Add(tag);
            }
            result.Add(tag);
        }

        return result;
    }
}
