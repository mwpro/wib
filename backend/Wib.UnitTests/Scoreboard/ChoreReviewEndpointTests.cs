using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Wib.Domain.Common;
using Wib.Domain.Scoreboard;

namespace Wib.UnitTests.Scoreboard;

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

        var lastMonthStart = WarsawTimeZone.GetCurrentMonthlyPeriod(DateTimeOffset.UtcNow).StartUtc.AddDays(-1);
        var period = WarsawTimeZone.GetCurrentMonthlyPeriod(lastMonthStart);
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
}
