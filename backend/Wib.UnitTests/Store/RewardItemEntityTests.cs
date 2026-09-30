using FluentAssertions;
using Wib.Api.Data.Entities;

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
            createdByMemberId: 42,
            nowUtc: nowUtc
        );

        item.Title.Should().Be("Masaż");
        item.Description.Should().Be("30 minut masażu pleców");
        item.PointCost.Should().Be(15);
        item.IsActive.Should().BeTrue();
        item.CreatedByMemberId.Should().Be(42);
        item.CreatedAt.Should().Be(nowUtc);
        item.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidTitle_ShouldThrowArgumentException(string title)
    {
        var act = () => RewardItem.Create(title, null, 10, 1, DateTime.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositivePointCost_ShouldThrowArgumentOutOfRangeException(int cost)
    {
        var act = () => RewardItem.Create("Nagroda", null, cost, 1, DateTime.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdatePropertiesAndSetUpdatedAt()
    {
        var createdAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2026, 10, 2, 14, 30, 0, DateTimeKind.Utc);

        var item = RewardItem.Create("Stary tytuł", "Stary opis", 10, 1, createdAt);

        item.Update("Nowy tytuł", "Nowy opis", 20, updatedAt);

        item.Title.Should().Be("Nowy tytuł");
        item.Description.Should().Be("Nowy opis");
        item.PointCost.Should().Be(20);
        item.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalseAndSetUpdatedAt()
    {
        var createdAt = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        var deactivatedAt = new DateTime(2026, 10, 2, 14, 30, 0, DateTimeKind.Utc);

        var item = RewardItem.Create("Tytuł", null, 10, 1, createdAt);
        item.IsActive.Should().BeTrue();

        item.Deactivate(deactivatedAt);

        item.IsActive.Should().BeFalse();
        item.UpdatedAt.Should().Be(deactivatedAt);
    }
}
