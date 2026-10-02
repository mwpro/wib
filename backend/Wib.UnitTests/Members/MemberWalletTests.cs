using FluentAssertions;
using Wib.Domain.Members;

namespace Wib.UnitTests.Members;

public class MemberWalletTests
{
    [Fact]
    public void CreditWallet_ShouldIncreaseBalance()
    {
        var member = Member.Create("auth0|123", "Test User", DateTime.UtcNow);
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
        var member = Member.Create("auth0|123", "Test User", DateTime.UtcNow);

        var act = () => member.CreditWallet(creditAmount);

        act.Should().Throw<ArgumentOutOfRangeException>();
        member.WalletBalance.Should().Be(0);
    }

    [Fact]
    public void TryDebitWallet_WithSufficientBalance_ShouldDeductAndReturnTrue()
    {
        var member = Member.Create("auth0|123", "Test User", DateTime.UtcNow);
        member.CreditWallet(20);

        var result = member.TryDebitWallet(15);

        result.Should().BeTrue();
        member.WalletBalance.Should().Be(5);
    }

    [Fact]
    public void TryDebitWallet_WithExactBalance_ShouldDeductToZeroAndReturnTrue()
    {
        var member = Member.Create("auth0|123", "Test User", DateTime.UtcNow);
        member.CreditWallet(10);

        var result = member.TryDebitWallet(10);

        result.Should().BeTrue();
        member.WalletBalance.Should().Be(0);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(50)]
    public void TryDebitWallet_WithInsufficientBalance_ShouldNotDeductAndReturnFalse(int debitAmount)
    {
        var member = Member.Create("auth0|123", "Test User", DateTime.UtcNow);
        member.CreditWallet(10);

        var result = member.TryDebitWallet(debitAmount);

        result.Should().BeFalse();
        member.WalletBalance.Should().Be(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TryDebitWallet_WithZeroOrNegativeAmount_ShouldReturnFalse(int debitAmount)
    {
        var member = Member.Create("auth0|123", "Test User", DateTime.UtcNow);
        member.CreditWallet(10);

        var result = member.TryDebitWallet(debitAmount);

        result.Should().BeFalse();
        member.WalletBalance.Should().Be(10);
    }
}
