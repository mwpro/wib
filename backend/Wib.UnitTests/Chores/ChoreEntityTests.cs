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
        var chore = Chore.Create("Mycie podłóg", null, 5, 7, now.AddDays(-3)).Value;
        var member = Member.Create("test-member", "Test Member", now.AddDays(-10));

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

    public static TheoryData<string, string?, int, int?, string> InvalidInput => new()
    {
        { "", null, 1, 7, "Chore.TitleRequired" },
        { "   ", null, 1, 7, "Chore.TitleRequired" },
        { new string('a', 256), null, 1, 7, "Chore.TitleTooLong" },
        { "Zmywanie", new string('a', 2001), 1, 7, "Chore.DescriptionTooLong" },
        { "Zmywanie", null, 0, 7, "Chore.PointsTooLow" },
        { "Zmywanie", null, -5, 7, "Chore.PointsTooLow" },
        { "Zmywanie", null, 1, 0, "Chore.CadenceTooShort" },
        { "Zmywanie", null, 1, -3, "Chore.CadenceTooShort" },
    };

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void Create_WithInvalidInput_ShouldReturnError(
        string title, string? description, int points, int? cadenceDays, string expectedCode)
    {
        var result = Chore.Create(title, description, points, cadenceDays, DateTime.UtcNow);

        result.Error.Code.Should().Be(expectedCode);
    }

    [Theory]
    [MemberData(nameof(InvalidInput))]
    public void Update_WithInvalidInput_ShouldReturnErrorAndLeaveChoreUnchanged(
        string title, string? description, int points, int? cadenceDays, string expectedCode)
    {
        var chore = Chore.Create("Odkurzanie", "Salon", 2, 14, DateTime.UtcNow).Value;

        var result = chore.Update(title, description, points, cadenceDays, DateTime.UtcNow);

        result.Error.Code.Should().Be(expectedCode);
        chore.Title.Should().Be("Odkurzanie");
        chore.Description.Should().Be("Salon");
        chore.Points.Should().Be(2);
        chore.CadenceDays.Should().Be(14);
        chore.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithMaximumLengthsAndNoCadence_ShouldSucceed()
    {
        var result = Chore.Create(new string('a', 255), new string('a', 2000), 1, null, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void NormalizeTagNames_ShouldTrimLowercaseDeduplicateAndSkipBlanks()
    {
        var result = Chore.NormalizeTagNames([" Kuchnia ", "sprzątanie", "KUCHNIA", "  ", ""]);

        result.Value.Should().Equal("kuchnia", "sprzątanie");
    }

    [Fact]
    public void NormalizeTagNames_WithNull_ShouldReturnEmpty()
    {
        Chore.NormalizeTagNames(null).Value.Should().BeEmpty();
    }

    [Fact]
    public void NormalizeTagNames_WithNameLongerThan50AfterTrimming_ShouldReturnTagTooLong()
    {
        Chore.NormalizeTagNames([$"  {new string('a', 50)}  "]).IsSuccess.Should().BeTrue();

        Chore.NormalizeTagNames([new string('a', 51)]).Error.Should().Be(ChoreErrors.TagTooLong);
    }
}
