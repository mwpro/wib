using Wib.Domain.Vouchers;

namespace Wib.Api.Vouchers;

public record VoucherResponse(
    int Id,
    int RewardItemId,
    string TitleSnapshot,
    int PointCostSnapshot,
    int OwnedByMemberId,
    bool IsRedeemed,
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
            voucher.IsRedeemed,
            voucher.PurchasedAt,
            voucher.RedeemedAt
        );
    }
}
