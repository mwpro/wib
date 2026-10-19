using FluentAssertions;
using Wib.Domain.Members;
using Wib.Domain.Store;
using Wib.Domain.Vouchers;

namespace Wib.UnitTests.Vouchers;

public class VoucherEntityTests
{
    private static readonly DateTime PurchasedAt = new(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RedeemedAt = new(2026, 10, 2, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Redeem_WhenAvailable_ShouldTransitionToRedeemedAndSetRedeemedAt()
    {
        var (owner, voucher) = PurchaseVoucher();

        var result = voucher.Redeem(owner, RedeemedAt);

        result.IsSuccess.Should().BeTrue();
        voucher.IsRedeemed.Should().BeTrue();
        voucher.RedeemedAt.Should().Be(RedeemedAt);
    }

    [Fact]
    public void Redeem_WhenAlreadyRedeemed_ShouldReturnAlreadyRedeemedAndKeepFirstRedemption()
    {
        var (owner, voucher) = PurchaseVoucher();
        voucher.Redeem(owner, RedeemedAt);

        var result = voucher.Redeem(owner, RedeemedAt.AddHours(1));

        result.Error.Should().Be(VoucherErrors.AlreadyRedeemed);
        voucher.RedeemedAt.Should().Be(RedeemedAt);
    }

    private static (Member Owner, Voucher Voucher) PurchaseVoucher()
    {
        var owner = A.Member().Build();
        owner.CreditWallet(25);
        var rewardItem = RewardItem.Create("Masaż", null, 25, quantity: null, owner.Id, PurchasedAt).Value;

        return (owner, rewardItem.Purchase(owner, PurchasedAt).Value);
    }
}
