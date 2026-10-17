using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Auth;
using Wib.Api.Common;
using Wib.Api.Data;
using Wib.Domain.Chores;

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
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var query = db.Chores
                .Where(c => !c.IsArchived)
                .Include(c => c.Tags)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var normalizedTag = Tag.NormalizeName(tag);
                query = query.Where(c => c.Tags.Any(t => t.Name == normalizedTag));
            }

            var chores = await query.ToListAsync(cancellationToken);
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

            var responseList = ChoreBacklog.Order(chores, nowUtc)
                .Select(ChoreResponse.Create)
                .ToList();

            return Results.Ok(responseList);
        })
        .WithName("GetChores");

        group.MapPost("/", async (
            [FromBody] CreateChoreRequest request,
            [FromServices] WibDbContext db,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

            var chore = Chore.Create(request.Title, request.Description, request.Points, request.CadenceDays, nowUtc);

            var tags = await ResolveTagsAsync(request.Tags, db, nowUtc, cancellationToken);
            chore.SetTags(tags);

            db.Chores.Add(chore);
            await db.SaveChangesAsync(cancellationToken);

            var response = ChoreResponse.Create(chore, nowUtc);

            return Results.Created($"/api/chores/{chore.Id}", response);
        })
        .WithName("CreateChore");

        group.MapPut("/{id:int}", async (
            [FromRoute] int id,
            [FromBody] UpdateChoreRequest request,
            [FromServices] WibDbContext db,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var chore = await db.Chores
                .Include(c => c.Tags)
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

            var response = ChoreResponse.Create(chore, nowUtc);

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
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var member = await currentMemberAccessor.GetCurrentMemberAsync(cancellationToken);
            if (member == null)
            {
                return Results.Unauthorized();
            }

            var chore = await db.Chores
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (chore == null || chore.IsArchived)
            {
                return Results.NotFound();
            }

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var completion = chore.Complete(member, nowUtc);

            await db.SaveChangesAsync(cancellationToken);

            var choreResponse = ChoreResponse.Create(chore, nowUtc);

            return Results.Ok(new CompleteChoreResponse(choreResponse, member.WalletBalance, completion.PointsAwarded));
        })
        .WithName("CreateChoreCompletion");

        group.MapGet("/{id:int}/completions", async (
            [FromRoute] int id,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromServices] WibDbContext db,
            CancellationToken cancellationToken) =>
        {
            var effectivePage = page is null or <= 0 ? 1 : page.Value;
            var effectivePageSize = pageSize ?? 20;

            var errors = new Dictionary<string, string[]>();
            if (page < 0) errors["page"] = ["Page must be greater than or equal to 1."];
            if (effectivePageSize is < 1 or > 100) errors["pageSize"] = ["Page size must be between 1 and 100."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var choreExists = await db.Chores.AnyAsync(c => c.Id == id && !c.IsArchived, cancellationToken);
            if (!choreExists)
            {
                return Results.NotFound();
            }

            var completions = db.ChoreCompletions
                .AsNoTracking()
                .Where(cc => cc.ChoreId == id);

            var items = await completions
                .OrderByDescending(cc => cc.CompletedAt)
                .ThenByDescending(cc => cc.Id)
                .Skip((effectivePage - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .Select(cc => new
                {
                    cc.Id,
                    cc.CompletedByMemberId,
                    MemberName = cc.CompletedByMember.Name,
                    cc.PointsAwarded,
                    cc.CompletedAt,
                    cc.DaysSincePrevious,
                    cc.UrgencyAtCompletion
                })
                .ToListAsync(cancellationToken);

            var outcomes = await completions
                .GroupBy(cc => cc.UrgencyAtCompletion)
                .Select(g => new { Urgency = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Urgency, g => g.Count, cancellationToken);

            var members = await completions
                .GroupBy(cc => new { cc.CompletedByMemberId, cc.CompletedByMember.Name })
                .Select(g => new ChoreHistoryMemberShare(g.Key.CompletedByMemberId, g.Key.Name, g.Count()))
                .ToListAsync(cancellationToken);

            var totalCount = outcomes.Values.Sum();
            var summary = new ChoreHistorySummary(
                totalCount,
                Enum.GetValues<FreshnessUrgency>()
                    .Where(u => u != FreshnessUrgency.Unscheduled)
                    .ToDictionary(u => u.ToString(), u => outcomes.GetValueOrDefault(u)),
                outcomes.Where(o => o.Key.IsOnTime()).Sum(o => o.Value),
                members
                    .OrderByDescending(m => m.Count)
                    .ThenBy(m => m.MemberName)
                    .ToList());

            return Results.Ok(new ChoreHistoryResponse(
                items.Select(i => new ChoreHistoryItem(
                    i.Id,
                    i.CompletedByMemberId,
                    i.MemberName,
                    i.PointsAwarded,
                    i.CompletedAt,
                    i.DaysSincePrevious,
                    i.UrgencyAtCompletion.ToString())).ToList(),
                effectivePage,
                effectivePageSize,
                totalCount,
                effectivePage * effectivePageSize < totalCount,
                summary));
        })
        .WithName("GetChoreCompletionHistory");

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
