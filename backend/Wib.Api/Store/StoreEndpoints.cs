using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Auth;
using Wib.Api.Common;
using Wib.Api.Data;
using Wib.Api.Data.Entities;
using Wib.Api.Vouchers;

namespace Wib.Api.Store;

public static class StoreEndpoints
{
    public static IEndpointRouteBuilder MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/store")
            .RequireAuthorization()
            .WithTags("Store")
            .WithValidation();

        group.MapGet("/items", async (
            [FromServices] WibDbContext db,
            CancellationToken cancellationToken) =>
        {
            var items = await db.RewardItems
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderBy(r => r.Title)
                .Select(r => RewardItemResponse.Create(r))
                .ToListAsync(cancellationToken);

            return Results.Ok(items);
        })
        .WithName("GetStoreItems");

        group.MapPost("/items", async (
            [FromBody] CreateRewardItemRequest request,
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

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var item = RewardItem.Create(
                request.Title,
                request.Description,
                request.PointCost,
                request.Quantity,
                member.Id,
                nowUtc
            );

            db.RewardItems.Add(item);
            await db.SaveChangesAsync(cancellationToken);

            var response = RewardItemResponse.Create(item);
            return Results.Created($"/api/store/items/{item.Id}", response);
        })
        .WithName("CreateStoreItem");

        group.MapPut("/items/{id:int}", async (
            [FromRoute] int id,
            [FromBody] UpdateRewardItemRequest request,
            [FromServices] WibDbContext db,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var item = await db.RewardItems.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (item == null || !item.IsActive)
            {
                return Results.NotFound();
            }

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            item.Update(request.Title, request.Description, request.PointCost, request.Quantity, nowUtc);

            await db.SaveChangesAsync(cancellationToken);

            var response = RewardItemResponse.Create(item);
            return Results.Ok(response);
        })
        .WithName("UpdateStoreItem");

        group.MapDelete("/items/{id:int}", async (
            [FromRoute] int id,
            [FromServices] WibDbContext db,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var item = await db.RewardItems.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (item == null || !item.IsActive)
            {
                return Results.NotFound();
            }

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            item.Deactivate(nowUtc);

            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
        .WithName("DeleteStoreItem");

        group.MapPost("/items/{id:int}/purchase", async (
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

            var item = await db.RewardItems.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (item == null)
            {
                return Results.NotFound();
            }

            if (!item.IsActive)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Nagroda niedostępna",
                    Detail = "Wybrana nagroda nie jest już aktywna."
                });
            }

            if (member.WalletBalance < item.PointCost)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Niewystarczające środki",
                    Detail = "Niewystarczająca liczba punktów w portfelu."
                });
            }

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var voucher = item.Purchase(member, nowUtc);

            await db.SaveChangesAsync(cancellationToken);

            var response = new BuyRewardResponse(VoucherResponse.Create(voucher), member.WalletBalance);
            return Results.Created($"/api/vouchers/{voucher.Id}", response);
        })
        .WithName("PurchaseStoreItem");

        return app;
    }
}
