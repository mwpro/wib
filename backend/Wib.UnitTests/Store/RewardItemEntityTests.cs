using FluentAssertions;
using Wib.Domain.Members;
using Wib.Domain.Store;

namespace Wib.UnitTests.Store;

public class RewardItemEntityTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldInitializeCorrectly()
    {
        var nowUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        var item = RewardItem.Create(
            title: "Masaż",
            description: "30 minut masażu pleców",
            pointCost: 15,
            quantity: 2,
            createdByMemberId: 42,
            nowUtc: nowUtc
        );

        item.Title.Should().Be("Masaż");
        item.Description.Should().Be("30 minut masażu pleców");
        item.PointCost.Should().Be(15);
        item.Quantity.Should().Be(2);
        item.IsActive.Should().BeTrue();
        item.CreatedByMemberId.Should().Be(42);
        item.CreatedAt.Should().Be(nowUtc);
        item.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullQuantity_ShouldAllowUnlimited()
    {
        var item = RewardItem.Create("Kawa", null, 5, quantity: null, createdByMemberId: 1, DateTime.UtcNow);
        item.Quantity.Should().BeNull();
        item.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidTitle_ShouldThrowArgumentException(string title)
    {
        var act = () => RewardItem.Create(title, null, 10, null, 1, DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositivePointCost_ShouldThrowArgumentOutOfRangeException(int cost)
    {
        var act = () => RewardItem.Create("Nagroda", null, cost, null, 1, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidQuantity_ShouldThrowArgumentOutOfRangeException(int quantity)
    {
        var act = () => RewardItem.Create("Nagroda", null, 10, quantity, 1, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdatePropertiesAndSetUpdatedAt()
    {
        var createdAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 10, 2, 14, 30, 0, DateTimeKind.Utc);

        var item = RewardItem.Create("Stary tytuł", "Stary opis", 10, 1, 1, createdAt);

        item.Update("Nowy tytuł", "Nowy opis", 20, 5, updatedAt);

        item.Title.Should().Be("Nowy tytuł");
        item.Description.Should().Be("Nowy opis");
        item.PointCost.Should().Be(20);
        item.Quantity.Should().Be(5);
        item.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalseAndSetUpdatedAt()
    {
        var createdAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var deactivatedAt = new DateTime(2026, 10, 2, 14, 30, 0, DateTimeKind.Utc);

        var item = RewardItem.Create("Tytuł", null, 10, null, 1, createdAt);
        item.IsActive.Should().BeTrue();

        item.Deactivate(deactivatedAt);

        item.IsActive.Should().BeFalse();
        item.UpdatedAt.Should().Be(deactivatedAt);
    }

    [Fact]
    public void Purchase_WithSufficientBalanceAndQuantity_ShouldDeductBalanceAndDecrementQuantity()
    {
        var nowUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var member = Member.Create("auth0|alice", "Alice", nowUtc);
        member.CreditWallet(30);

        var item = RewardItem.Create("Masaż", null, 15, quantity: 2, 1, nowUtc);

        var voucher = item.Purchase(member, nowUtc);

        voucher.Should().NotBeNull();
        voucher.TitleSnapshot.Should().Be("Masaż");
        voucher.PointCostSnapshot.Should().Be(15);
        voucher.IsRedeemed.Should().BeFalse();

        member.WalletBalance.Should().Be(15);
        member.Vouchers.Should().Contain(voucher);
        item.Quantity.Should().Be(1);
        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Purchase_SingleClaimItem_ShouldDeactivateWhenQuantityReachesZero()
    {
        var nowUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var member = Member.Create("auth0|alice", "Alice", nowUtc);
        member.CreditWallet(20);

        var item = RewardItem.Create("Ostatni kawałek sernika", null, 10, quantity: 1, 1, nowUtc);

        var voucher = item.Purchase(member, nowUtc);

        item.Quantity.Should().Be(0);
        item.IsActive.Should().BeFalse();
        member.WalletBalance.Should().Be(10);
        member.Vouchers.Should().Contain(voucher);

        // Attempting to purchase again should throw
        var act = () => item.Purchase(member, nowUtc);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*nie jest już aktywna*");
    }

    [Fact]
    public void Purchase_WithInsufficientBalance_ShouldThrowInvalidOperationException()
    {
        var nowUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var member = Member.Create("auth0|alice", "Alice", nowUtc);
        member.CreditWallet(5);

        var item = RewardItem.Create("Kino", null, 15, quantity: null, 1, nowUtc);

        var act = () => item.Purchase(member, nowUtc);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Niewystarczająca liczba punktów*");

        member.WalletBalance.Should().Be(5);
        member.Vouchers.Should().BeEmpty();
    }
}
