using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Auth;
using Wib.Api.Common;
using Wib.Api.Data;
using Wib.Api.Vouchers;
using Wib.Domain.Store;

namespace Wib.Api.Store;

public static class StoreEndpoints
{
    public static IEndpointRouteBuilder MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/store")
            .RequireAuthorization()
            .WithTags("Store");

        group.MapGet("/items", async (
            [FromServices] WibDbContext db,
            CancellationToken cancellationToken) =>
        {
            var items = await db.RewardItems
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.PointCost)
                .ThenBy(r => r.Title)
                .ToListAsync(cancellationToken);

            var responseList = items
                .Select(r => RewardItemResponse.Create(r))
                .ToList();

            return Results.Ok(responseList);
        })
        .WithName("GetStoreItems");

        group.MapPost("/items", async (
            [FromBody] CreateRewardItemRequest request,
            [FromServices] WibDbContext db,
            CurrentMember currentMember,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var member = currentMember.Member;

            var created = RewardItem.Create(
                request.Title,
                request.Description,
                request.PointCost,
                request.Quantity,
                member.Id,
                timeProvider.GetUtcNow().UtcDateTime
            );
            if (created.IsFailure)
            {
                return created.Error.ToProblem();
            }

            var item = created.Value;
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
            var item = await db.RewardItems.FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
            if (item == null)
            {
                return RewardItemErrors.NotFound.ToProblem();
            }

            var update = item.Update(
                request.Title, request.Description, request.PointCost, request.Quantity, timeProvider.GetUtcNow().UtcDateTime);
            if (update.IsFailure)
            {
                return update.Error.ToProblem();
            }

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
            var item = await db.RewardItems.FirstOrDefaultAsync(r => r.Id == id && r.IsActive, cancellationToken);
            if (item == null)
            {
                return RewardItemErrors.NotFound.ToProblem();
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
            CurrentMember currentMember,
            [FromServices] TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            var member = currentMember.Member;

            var item = await db.RewardItems.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            if (item == null)
            {
                return RewardItemErrors.NotFound.ToProblem();
            }

            var purchase = item.Purchase(member, timeProvider.GetUtcNow().UtcDateTime);
            if (purchase.IsFailure)
            {
                return purchase.Error.ToProblem();
            }

            await db.SaveChangesAsync(cancellationToken);

            var voucher = purchase.Value;
            var response = new BuyRewardResponse(VoucherResponse.Create(voucher), member.WalletBalance);
            return Results.Created($"/api/vouchers/{voucher.Id}", response);
        })
        .WithName("PurchaseStoreItem");

        return app;
    }
}
