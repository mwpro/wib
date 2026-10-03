using FluentAssertions;
using Wib.Domain.Chores;
using Wib.Domain.Members;

namespace Wib.UnitTests.Chores;

public class ChoreEntityTests
{
    [Fact]
    public void Complete_ShouldUpdateLastCompletedAt_CreditWallet_AndAddCompletion()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var chore = Chore.Create("Mycie podłóg", null, 5, 7, now.AddDays(-3));
        var member = Member.Create("auth0|test-member", "Test Member", now.AddDays(-10));

        // Act
        var completion = chore.Complete(member, now);

        // Assert
        chore.LastCompletedAt.Should().Be(now);
        member.WalletBalance.Should().Be(5);
        chore.Completions.Should().ContainSingle().Which.Should().Be(completion);
        completion.ChoreId.Should().Be(chore.Id);
        completion.CompletedByMemberId.Should().Be(member.Id);
        completion.PointsAwarded.Should().Be(5);
        completion.CompletedAt.Should().Be(now);
    }
}
