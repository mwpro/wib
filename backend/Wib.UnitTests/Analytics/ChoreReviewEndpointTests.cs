using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Wib.Domain.Analytics;
using Wib.Domain.Common;

namespace Wib.UnitTests.Analytics;

public class ChoreReviewEndpointTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ChoreReviewEndpointTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetChoreReview_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/analytics/2026/9/chore-review");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/analytics/2026/0/chore-review")]
    [InlineData("/api/analytics/2026/13/chore-review")]
    [InlineData("/api/analytics/1999/5/chore-review")]
    [InlineData("/api/analytics/2101/5/chore-review")]
    public async Task GetChoreReview_WithInvalidParameters_ShouldReturn400BadRequest(string uri)
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetChoreReview_ShouldSplitChoresByQualityScore()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("review-member", "Ola");
        await client.GetAsync("/api/members/me");

        var period = LastMonth();
        var goodChoreId = 0;
        var missedChoreId = 0;

        await _factory.ExecuteDbContext(async db =>
        {
            var member = db.Members.First(m => m.ExternalSubjectId == "review-member");

            var good = A.Chore().WithTitle("Review good").WithCadenceDays(30).CreatedAt(period.StartUtc.AddDays(-5)).Build();
            var missed = A.Chore().WithTitle("Review missed").WithCadenceDays(7).CreatedAt(period.StartUtc.AddDays(-30)).Build();
            db.Chores.AddRange(good, missed);
            await db.SaveChangesAsync();
            goodChoreId = good.Id;
            missedChoreId = missed.Id;

            db.ChoreCompletions.Add(good.Complete(member, period.StartUtc.AddDays(2)));
            await db.SaveChangesAsync();
        });

        // Act
        var response = await client.GetAsync($"/api/analytics/{period.Year}/{period.Month}/chore-review");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ChoreReviewResponse>(_jsonOptions);
        body!.IsInProgress.Should().BeFalse();

        body.WellDone.Should().ContainSingle(i => i.ChoreId == goodChoreId)
            .Which.Should().BeEquivalentTo(new { QualityScore = 1.0, OnTimeCount = 1, LateCount = 0, IsCurrentlyBad = false });
        body.NeedsImprovement.Should().ContainSingle(i => i.ChoreId == missedChoreId)
            .Which.Should().BeEquivalentTo(new { QualityScore = 0.0, IsCurrentlyBad = true });
    }

    [Fact]
    public async Task GetChoreReview_ShouldSplitCompletionsAtWarsawMidnightNotUtcMidnight()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("review-boundary-member", "Ola");
        await client.GetAsync("/api/members/me");

        var period = LastMonth();
        var choreId = 0;

        await _factory.ExecuteDbContext(async db =>
        {
            var member = db.Members.First(m => m.ExternalSubjectId == "review-boundary-member");

            var chore = A.Chore().WithTitle("Review boundary").WithCadenceDays(30).CreatedAt(period.EndUtc.AddDays(-3)).Build();
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            choreId = chore.Id;

            db.ChoreCompletions.AddRange(
                chore.Complete(member, period.EndUtc.AddMinutes(-30)),
                chore.Complete(member, period.EndUtc.AddMinutes(30)));
            await db.SaveChangesAsync();
        });

        // Act
        var response = await client.GetAsync($"/api/analytics/{period.Year}/{period.Month}/chore-review");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ChoreReviewResponse>(_jsonOptions);
        body!.WellDone.Should().ContainSingle(i => i.ChoreId == choreId)
            .Which.Should().BeEquivalentTo(new { OnTimeCount = 1, LateCount = 0, IsCurrentlyBad = false });
    }

    [Fact]
    public async Task GetChoreReview_WhenChoreIsArchived_ShouldCountItsCompletionsButNeverBeCurrentlyBad()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("review-archived-member", "Ola");
        await client.GetAsync("/api/members/me");

        var period = LastMonth();
        var choreId = 0;

        await _factory.ExecuteDbContext(async db =>
        {
            var member = db.Members.First(m => m.ExternalSubjectId == "review-archived-member");

            var chore = A.Chore().WithTitle("Review archived").WithCadenceDays(7).CreatedAt(period.StartUtc.AddDays(-30)).Archived().Build();
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            choreId = chore.Id;

            db.ChoreCompletions.Add(chore.Complete(member, period.StartUtc.AddDays(2)));
            await db.SaveChangesAsync();
        });

        // Act
        var response = await client.GetAsync($"/api/analytics/{period.Year}/{period.Month}/chore-review");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ChoreReviewResponse>(_jsonOptions);
        body!.NeedsImprovement.Should().ContainSingle(i => i.ChoreId == choreId)
            .Which.Should().BeEquivalentTo(new { QualityScore = 0.0, OnTimeCount = 0, LateCount = 1, IsCurrentlyBad = false, IsArchived = true });
    }

    private static MonthlyPeriod LastMonth() =>
        WarsawTimeZone.GetCurrentMonthlyPeriod(
            WarsawTimeZone.GetCurrentMonthlyPeriod(DateTimeOffset.UtcNow).StartUtc.AddDays(-1));
}
