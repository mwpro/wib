using Wib.Api.Data.Entities;

namespace Wib.Api.Vouchers;

public record VoucherResponse(
    int Id,
    int RewardItemId,
    string TitleSnapshot,
    int PointCostSnapshot,
    int OwnedByMemberId,
    string Status,
    DateTime PurchasedAt,
    DateTime? RedeemedAt
)
{
    public static VoucherResponse Create(Voucher voucher)
    {
        return new VoucherResponse(
            voucher.Id,
            voucher.RewardItemId,
            voucher.TitleSnapshot,
            voucher.PointCostSnapshot,
            voucher.OwnedByMemberId,
            voucher.Status.ToString(),
            voucher.PurchasedAt,
            voucher.RedeemedAt
        );
    }
}
