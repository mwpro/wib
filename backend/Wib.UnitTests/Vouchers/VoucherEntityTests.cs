using FluentAssertions;
using Wib.Domain.Members;
using Wib.Domain.Store;
using Wib.Domain.Vouchers;

namespace Wib.UnitTests.Vouchers;

public class VoucherEntityTests
{
    [Fact]
    public void Create_FromRewardItemAndMember_ShouldSnapshotAndBeAvailable()
    {
        var nowUtc = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var member = Member.Create("auth0|123", "Alice", nowUtc);
        var rewardItem = RewardItem.Create("Masaż", "Relaksujący masaż", 25, quantity: null, member.Id, nowUtc);

        var voucher = Voucher.Create(rewardItem, member, nowUtc);

        voucher.RewardItemId.Should().Be(rewardItem.Id);
        voucher.TitleSnapshot.Should().Be("Masaż");
        voucher.PointCostSnapshot.Should().Be(25);
        voucher.OwnedByMemberId.Should().Be(member.Id);
        voucher.IsRedeemed.Should().BeFalse();
        voucher.PurchasedAt.Should().Be(nowUtc);
        voucher.RedeemedAt.Should().BeNull();
    }

    [Fact]
    public void Create_FromInactiveRewardItem_ShouldThrowInvalidOperationException()
    {
        var nowUtc = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var member = Member.Create("auth0|123", "Alice", nowUtc);
        var rewardItem = RewardItem.Create("Kino", null, 10, quantity: null, member.Id, nowUtc);
        rewardItem.Deactivate(nowUtc);

        var act = () => Voucher.Create(rewardItem, member, nowUtc);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*nieaktywnej*");
    }

    [Fact]
    public void Redeem_WhenAvailable_ShouldTransitionToRedeemedAndSetRedeemedAt()
    {
        var purchasedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var redeemedAt = new DateTime(2026, 10, 2, 18, 0, 0, DateTimeKind.Utc);
        var member = Member.Create("auth0|123", "Alice", purchasedAt);
        var rewardItem = RewardItem.Create("Masaż", null, 25, quantity: null, member.Id, purchasedAt);

        var voucher = Voucher.Create(rewardItem, member, purchasedAt);

        voucher.Redeem(redeemedAt);

        voucher.IsRedeemed.Should().BeTrue();
        voucher.RedeemedAt.Should().Be(redeemedAt);
    }

    [Fact]
    public void Redeem_WhenAlreadyRedeemed_ShouldThrowInvalidOperationException()
    {
        var purchasedAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var redeemedAt = new DateTime(2026, 10, 2, 18, 0, 0, DateTimeKind.Utc);
        var member = Member.Create("auth0|123", "Alice", purchasedAt);
        var rewardItem = RewardItem.Create("Masaż", null, 25, quantity: null, member.Id, purchasedAt);

        var voucher = Voucher.Create(rewardItem, member, purchasedAt);
        voucher.Redeem(redeemedAt);

        var act = () => voucher.Redeem(redeemedAt.AddHours(1));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*zrealizowany*");
    }
}
