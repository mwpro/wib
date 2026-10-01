using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Auth;
using Wib.Api.Common;
using Wib.Api.Data;

namespace Wib.Api.Vouchers;

public static class VoucherEndpoints
{
    public static IEndpointRouteBuilder MapVoucherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vouchers")
            .RequireAuthorization()
            .WithTags("Vouchers")
            .WithValidation();

        group.MapGet("/", async (
            [FromQuery] bool? isRedeemed,
            [FromServices] WibDbContext db,
            [FromServices] ICurrentMemberAccessor currentMemberAccessor,
            CancellationToken cancellationToken) =>
        {
            var member = await currentMemberAccessor.GetCurrentMemberAsync(cancellationToken);
            if (member == null)
            {
                return Results.Unauthorized();
            }

            var query = db.Vouchers
                .AsNoTracking()
                .Where(v => v.OwnedByMemberId == member.Id);

            if (isRedeemed.HasValue)
            {
                query = isRedeemed.Value
                    ? query.Where(v => v.RedeemedAt != null)
                    : query.Where(v => v.RedeemedAt == null);
            }

            var vouchers = await query
                .OrderByDescending(v => v.PurchasedAt)
                .Select(v => VoucherResponse.Create(v))
                .ToListAsync(cancellationToken);

            return Results.Ok(vouchers);
        })
        .WithName("GetMyVouchers");

        group.MapPost("/{id:int}/redemption", async (
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

            var voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
            if (voucher == null)
            {
                return Results.NotFound();
            }

            if (voucher.OwnedByMemberId != member.Id)
            {
                return Results.Forbid();
            }

            if (voucher.IsRedeemed)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Kupon już zrealizowany",
                    Detail = "Kupon został już zrealizowany."
                });
            }

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            voucher.Redeem(nowUtc);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(VoucherResponse.Create(voucher));
        })
        .WithName("RedeemVoucher");

        return app;
    }
}
