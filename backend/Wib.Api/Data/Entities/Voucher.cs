namespace Wib.Api.Data.Entities;

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

    public static Voucher Create(RewardItem rewardItem, Member member, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(rewardItem);
        ArgumentNullException.ThrowIfNull(member);

        if (!rewardItem.IsActive)
            throw new InvalidOperationException("Nie można utworzyć kuponu dla nieaktywnej nagrody.");

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

    public void Redeem(DateTime nowUtc)
    {
        if (IsRedeemed)
            throw new InvalidOperationException("Kupon został już zrealizowany.");

        RedeemedAt = nowUtc;
    }
}
