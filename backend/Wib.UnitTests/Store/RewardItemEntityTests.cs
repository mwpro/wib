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
        ).Value;

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
        var item = RewardItem.Create("Kawa", null, 5, quantity: null, createdByMemberId: 1, DateTime.UtcNow).Value;
        item.Quantity.Should().BeNull();
        item.IsActive.Should().BeTrue();
    }

    public static TheoryData<string, string?, int, int?, string> InvalidInput => new()
    {
        { "", null, 10, null, "RewardItem.TitleRequired" },
        { "   ", null, 10, null, "RewardItem.TitleRequired" },
        { new string('a', 256), null, 10, null, "RewardItem.TitleTooLong" },
        { "Nagroda", new string('a', 2001), 10, null, "RewardItem.DescriptionTooLong" },
        { "Nagroda", null, 0, null, "RewardItem.PointCostTooLow" },
        { "Nagroda", null, -5, null, "RewardItem.PointCostTooLow" },
        { "Nagroda", null, 10, 0, "RewardItem.QuantityTooLow" },
        { "Nagroda", null, 10, -1, "RewardItem.QuantityTooLow" },
    };

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void Create_WithInvalidInput_ShouldReturnError(
        string title, string? description, int pointCost, int? quantity, string expectedCode)
    {
        var result = RewardItem.Create(title, description, pointCost, quantity, 1, DateTime.UtcNow);

        result.Error.Code.Should().Be(expectedCode);
    }

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void Update_WithInvalidInput_ShouldReturnErrorAndLeaveItemUnchanged(
        string title, string? description, int pointCost, int? quantity, string expectedCode)
    {
        var item = RewardItem.Create("Kawa", "Opis", 5, 3, 1, DateTime.UtcNow).Value;

        var result = item.Update(title, description, pointCost, quantity, DateTime.UtcNow);

        result.Error.Code.Should().Be(expectedCode);
        item.Title.Should().Be("Kawa");
        item.Description.Should().Be("Opis");
        item.PointCost.Should().Be(5);
        item.Quantity.Should().Be(3);
        item.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithMaximumLengths_ShouldSucceed()
    {
        var result = RewardItem.Create(new string('a', 255), new string('a', 2000), 1, 1, 1, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdatePropertiesAndSetUpdatedAt()
    {
        var createdAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 10, 2, 14, 30, 0, DateTimeKind.Utc);

        var item = RewardItem.Create("Stary tytuł", "Stary opis", 10, 1, 1, createdAt).Value;

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

        var item = RewardItem.Create("Tytuł", null, 10, null, 1, createdAt).Value;
        item.IsActive.Should().BeTrue();

        item.Deactivate(deactivatedAt);

        item.IsActive.Should().BeFalse();
        item.UpdatedAt.Should().Be(deactivatedAt);
    }

    [Fact]
    public void Purchase_WithSufficientBalanceAndQuantity_ShouldDeductBalanceAndDecrementQuantity()
    {
        var nowUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var member = A.Member().Build();
        member.CreditWallet(30);

        var item = RewardItem.Create("Masaż", null, 15, quantity: 2, 1, nowUtc).Value;

        var voucher = item.Purchase(member, nowUtc).Value;

        voucher.TitleSnapshot.Should().Be("Masaż");
        voucher.PointCostSnapshot.Should().Be(15);
        voucher.OwnedByMemberId.Should().Be(member.Id);
        voucher.PurchasedAt.Should().Be(nowUtc);
        voucher.IsRedeemed.Should().BeFalse();
        voucher.RedeemedAt.Should().BeNull();

        member.WalletBalance.Should().Be(15);
        member.Vouchers.Should().Contain(voucher);
        item.Quantity.Should().Be(1);
        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Purchase_SingleClaimItem_ShouldDeactivateWhenQuantityReachesZero()
    {
        var nowUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var member = A.Member().Build();
        member.CreditWallet(20);

        var item = RewardItem.Create("Ostatni kawałek sernika", null, 10, quantity: 1, 1, nowUtc).Value;

        var voucher = item.Purchase(member, nowUtc).Value;

        item.Quantity.Should().Be(0);
        item.IsActive.Should().BeFalse();
        member.WalletBalance.Should().Be(10);
        member.Vouchers.Should().Contain(voucher);

        var secondPurchase = item.Purchase(member, nowUtc);

        secondPurchase.Error.Should().Be(RewardItemErrors.Unavailable);
        member.WalletBalance.Should().Be(10);
    }

    [Fact]
    public void Purchase_WhenDeactivated_ShouldReturnUnavailableWithoutCharging()
    {
        var nowUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var member = A.Member().Build();
        member.CreditWallet(50);

        var item = RewardItem.Create("Kino", null, 10, quantity: null, 1, nowUtc).Value;
        item.Deactivate(nowUtc);

        var result = item.Purchase(member, nowUtc);

        result.Error.Should().Be(RewardItemErrors.Unavailable);
        member.WalletBalance.Should().Be(50);
        member.Vouchers.Should().BeEmpty();
    }

    [Fact]
    public void Purchase_WithInsufficientBalance_ShouldReturnNotEnoughPointsWithoutConsumingQuantity()
    {
        var nowUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var member = A.Member().Build();
        member.CreditWallet(5);

        var item = RewardItem.Create("Kino", null, 15, quantity: 1, 1, nowUtc).Value;

        var result = item.Purchase(member, nowUtc);

        result.Error.Should().Be(WalletErrors.NotEnoughPoints);
        member.WalletBalance.Should().Be(5);
        member.Vouchers.Should().BeEmpty();
        item.Quantity.Should().Be(1);
        item.IsActive.Should().BeTrue();
    }
}
