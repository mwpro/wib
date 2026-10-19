using Wib.Domain.Common;
using Wib.Domain.Members;
using Wib.Domain.Store;

namespace Wib.Domain.Vouchers;

public class Voucher
{
    private Voucher() { }

    public int Id { get; private set; }
    public int RewardItemId { get; private set; }
    public RewardItem RewardItem { get; private set; } = null!;
    public string TitleSnapshot { get; private set; } = string.Empty;
    public int PointCostSnapshot { get; private set; }
    public int OwnedByMemberId { get; private set; }
    public Member OwnedByMember { get; private set; } = null!;
    public DateTime PurchasedAt { get; private set; }
    public DateTime? RedeemedAt { get; private set; }

    public bool IsRedeemed => RedeemedAt.HasValue;

    internal static Voucher Create(RewardItem rewardItem, Member member, DateTime nowUtc)
    {
        return new Voucher
        {
            RewardItemId = rewardItem.Id,
            TitleSnapshot = rewardItem.Title,
            PointCostSnapshot = rewardItem.PointCost,
            OwnedByMemberId = member.Id,
            PurchasedAt = nowUtc,
            RedeemedAt = null
        };
    }

    public Result Redeem(Member member, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (OwnedByMemberId != member.Id)
            return VoucherErrors.NotFound;

        if (IsRedeemed)
            return VoucherErrors.AlreadyRedeemed;

        RedeemedAt = nowUtc;
        return Result.Success;
    }
}
