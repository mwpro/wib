using FluentAssertions;
using Wib.Domain.Members;

namespace Wib.UnitTests.Members;

public class MemberWalletTests
{
    [Fact]
    public void CreditWallet_ShouldIncreaseBalance()
    {
        var member = Member.Create("123", "Test User", DateTime.UtcNow);
        member.WalletBalance.Should().Be(0);

        member.CreditWallet(15);

        member.WalletBalance.Should().Be(15);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void CreditWallet_WithZeroOrNegativeAmount_ShouldThrowArgumentOutOfRangeException(int creditAmount)
    {
        var member = Member.Create("123", "Test User", DateTime.UtcNow);

        var act = () => member.CreditWallet(creditAmount);

        act.Should().Throw<ArgumentOutOfRangeException>();
        member.WalletBalance.Should().Be(0);
    }

    [Fact]
    public void DebitWallet_WithSufficientBalance_ShouldDeductAndSucceed()
    {
        var member = Member.Create("123", "Test User", DateTime.UtcNow);
        member.CreditWallet(20);

        var result = member.DebitWallet(15);

        result.IsSuccess.Should().BeTrue();
        member.WalletBalance.Should().Be(5);
    }

    [Fact]
    public void DebitWallet_WithExactBalance_ShouldDeductToZeroAndSucceed()
    {
        var member = Member.Create("123", "Test User", DateTime.UtcNow);
        member.CreditWallet(10);

        var result = member.DebitWallet(10);

        result.IsSuccess.Should().BeTrue();
        member.WalletBalance.Should().Be(0);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(50)]
    public void DebitWallet_WithInsufficientBalance_ShouldNotDeductAndReturnNotEnoughPoints(int debitAmount)
    {
        var member = Member.Create("123", "Test User", DateTime.UtcNow);
        member.CreditWallet(10);

        var result = member.DebitWallet(debitAmount);

        result.Error.Should().Be(WalletErrors.NotEnoughPoints);
        member.WalletBalance.Should().Be(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void DebitWallet_WithZeroOrNegativeAmount_ShouldThrowArgumentOutOfRangeException(int debitAmount)
    {
        var member = Member.Create("123", "Test User", DateTime.UtcNow);
        member.CreditWallet(10);

        var act = () => member.DebitWallet(debitAmount);

        act.Should().Throw<ArgumentOutOfRangeException>();
        member.WalletBalance.Should().Be(10);
    }
}
