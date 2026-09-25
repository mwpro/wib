using Microsoft.EntityFrameworkCore;
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
            string? tag,
            WibDbContext db,
            IFreshnessCalculator freshnessCalculator,
            CancellationToken cancellationToken) =>
        {
            IQueryable<Chore> query = db.Chores
                .AsNoTracking()
                .Where(c => !c.IsArchived)
                .Include(c => c.ChoreTags)
                .ThenInclude(ct => ct.Tag);

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var normalizedTag = tag.Trim().ToLowerInvariant();
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
            CreateChoreRequest request,
            WibDbContext db,
            IFreshnessCalculator freshnessCalculator,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

            var tagNames = (request.Tags ?? Array.Empty<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            var existingTags = await db.Tags
                .Where(t => tagNames.Contains(t.Name))
                .ToListAsync(cancellationToken);

            var existingTagMap = existingTags.ToDictionary(t => t.Name);
            var tagsToAttach = new List<Tag>();

            foreach (var name in tagNames)
            {
                if (!existingTagMap.TryGetValue(name, out var tag))
                {
                    tag = new Tag { Name = name, CreatedAt = nowUtc };
                    db.Tags.Add(tag);
                }
                tagsToAttach.Add(tag);
            }

            var chore = new Chore
            {
                Title = request.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Points = request.Points,
                CadenceDays = request.CadenceDays,
                LastCompletedAt = null,
                IsArchived = false,
                CreatedAt = nowUtc,
                UpdatedAt = null
            };

            foreach (var tag in tagsToAttach)
            {
                chore.ChoreTags.Add(new ChoreTag { Chore = chore, Tag = tag });
            }

            db.Chores.Add(chore);
            await db.SaveChangesAsync(cancellationToken);

            var freshness = freshnessCalculator.Calculate(chore.LastCompletedAt, chore.CreatedAt, chore.CadenceDays);
            var response = ChoreResponse.Create(chore, freshness);

            return Results.Created($"/api/chores/{chore.Id}", response);
        })
        .WithName("CreateChore");

        group.MapPut("/{id:int}", async (
            int id,
            UpdateChoreRequest request,
            WibDbContext db,
            IFreshnessCalculator freshnessCalculator,
            TimeProvider timeProvider,
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

            chore.Title = request.Title.Trim();
            chore.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            chore.Points = request.Points;
            chore.CadenceDays = request.CadenceDays;
            chore.UpdatedAt = nowUtc;

            var tagNames = (request.Tags ?? Array.Empty<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            var existingTags = await db.Tags
                .Where(t => tagNames.Contains(t.Name))
                .ToListAsync(cancellationToken);

            var existingTagMap = existingTags.ToDictionary(t => t.Name);
            var desiredTags = new List<Tag>();

            foreach (var name in tagNames)
            {
                if (!existingTagMap.TryGetValue(name, out var tag))
                {
                    tag = new Tag { Name = name, CreatedAt = nowUtc };
                    db.Tags.Add(tag);
                }
                desiredTags.Add(tag);
            }

            chore.ChoreTags.Clear();
            foreach (var tag in desiredTags)
            {
                chore.ChoreTags.Add(new ChoreTag { ChoreId = chore.Id, Tag = tag });
            }

            await db.SaveChangesAsync(cancellationToken);

            var freshness = freshnessCalculator.Calculate(chore.LastCompletedAt, chore.CreatedAt, chore.CadenceDays);
            var response = ChoreResponse.Create(chore, freshness);

            return Results.Ok(response);
        })
        .WithName("UpdateChore");

        group.MapDelete("/{id:int}", async (
            int id,
            WibDbContext db,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var chore = await db.Chores.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (chore == null || chore.IsArchived)
            {
                return Results.NotFound();
            }

            chore.IsArchived = true;
            chore.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
        .WithName("DeleteChore");

        return app;
    }
}
