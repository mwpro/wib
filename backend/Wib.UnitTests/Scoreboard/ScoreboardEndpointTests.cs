using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Wib.Domain.Common;
using Wib.Domain.Scoreboard;

namespace Wib.UnitTests.Scoreboard;

public class ScoreboardEndpointTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ScoreboardEndpointTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ScoreboardEndpoints_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act & Assert
        var monthRes = await unauthenticatedClient.GetAsync("/api/scoreboard/2026/9");
        monthRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetScoreboard_ShouldAggregateTargetMonthAndIncludeLifetimeStats()
    {
        // Arrange: JIT provision 2 members
        var client1 = _factory.CreateAuthenticatedClient("sb-member-1", "Maciej");
        var client2 = _factory.CreateAuthenticatedClient("sb-member-2", "Kasia");

        // Trigger JIT provisioning via /api/members/me
        await client1.GetAsync("/api/members/me");
        await client2.GetAsync("/api/members/me");

        var member1Id = 0;
        var member2Id = 0;
        var choreId = 0;

        var period = WarsawTimeZone.GetCurrentMonthlyPeriod(DateTimeOffset.UtcNow);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var m1 = db.Members.First(m => m.ExternalSubjectId == "sb-member-1");
            var m2 = db.Members.First(m => m.ExternalSubjectId == "sb-member-2");
            member1Id = m1.Id;
            member2Id = m2.Id;

            var chore = A.Chore().WithPoints(50).Build();
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            choreId = chore.Id;

            // Add completions:
            // m1 completed chore today (active month) -> 50 points
            var activeCompletion = chore.Complete(m1, DateTime.UtcNow);
            // m2 completed chore 60 days ago (past month) -> 50 points
            var pastCompletion = chore.Complete(m2, DateTime.UtcNow.AddDays(-60));

            db.ChoreCompletions.AddRange(activeCompletion, pastCompletion);
            await db.SaveChangesAsync();
        });

        // Act
        var response = await client1.GetAsync($"/api/scoreboard/{period.Year}/{period.Month}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ScoreboardResponse>(_jsonOptions);
        body.Should().NotBeNull();

        body!.Members
            .Where(m => m.MemberId == member1Id || m.MemberId == member2Id)
            .Should()
            .SatisfyRespectively(
                m1 =>
                {
                    m1.MemberId.Should().Be(member1Id);
                    m1.MonthlyPoints.Should().Be(50);
                    m1.MonthlyChoresCompleted.Should().Be(1);
                    m1.LifetimePoints.Should().Be(50);
                    m1.LifetimeChoresCompleted.Should().Be(1);
                    m1.WorkSharePercentage.Should().Be(100.0);
                    m1.Rank.Should().Be(1);
                },
                m2 =>
                {
                    m2.MemberId.Should().Be(member2Id);
                    m2.MonthlyPoints.Should().Be(0);
                    m2.MonthlyChoresCompleted.Should().Be(0);
                    m2.LifetimePoints.Should().Be(50);
                    m2.LifetimeChoresCompleted.Should().Be(1);
                    m2.WorkSharePercentage.Should().Be(0.0);
                    m2.Rank.Should().Be(2);
                }
            );

        body.TotalHouseholdChoresCompleted.Should().Be(1);
        body.TotalHouseholdPointsEarned.Should().Be(50);
    }

    [Fact]
    public async Task GetScoreboard_WithHistoricalMonth_ShouldReturnHistoricalMonthStats()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient("sb-hist-user", "History User");
        await client.GetAsync("/api/members/me");

        var memberId = 0;
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var m = db.Members.First(m => m.ExternalSubjectId == "sb-hist-user");
            memberId = m.Id;

            var chore = A.Chore().WithPoints(30).Build();
            db.Chores.Add(chore);
            await db.SaveChangesAsync();

            // Completion in June 2026 (2026-06-15 12:00:00 UTC)
            var juneDate = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
            var juneCompletion = chore.Complete(m, juneDate);
            db.ChoreCompletions.Add(juneCompletion);
            await db.SaveChangesAsync();
        });

        // Act - Request via route /api/scoreboard/2026/6
        var responseRoute = await client.GetAsync("/api/scoreboard/2026/6");
        responseRoute.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyRoute = await responseRoute.Content.ReadFromJsonAsync<ScoreboardResponse>(_jsonOptions);
        bodyRoute.Should().NotBeNull();
        bodyRoute!.Year.Should().Be(2026);
        bodyRoute.Month.Should().Be(6);

        bodyRoute.Members.Where(m => m.MemberId == memberId).Should().SatisfyRespectively(
            member =>
            {
                member.MemberId.Should().Be(memberId);
                member.MonthlyPoints.Should().Be(30);
                member.MonthlyChoresCompleted.Should().Be(1);
                member.WorkSharePercentage.Should().Be(100.0);
            }
        );

        // Act - Request July 2026 (when user had 0 chores)
        var responseJuly = await client.GetAsync("/api/scoreboard/2026/7");
        responseJuly.StatusCode.Should().Be(HttpStatusCode.OK);
        var bodyJuly = await responseJuly.Content.ReadFromJsonAsync<ScoreboardResponse>(_jsonOptions);

        bodyJuly!.Members.Where(m => m.MemberId == memberId).Should().SatisfyRespectively(
            member =>
            {
                member.MemberId.Should().Be(memberId);
                member.MonthlyPoints.Should().Be(0);
                member.MonthlyChoresCompleted.Should().Be(0);
                member.WorkSharePercentage.Should().Be(0.0);
                member.LifetimePoints.Should().BeGreaterThanOrEqualTo(30);
            }
        );
    }

    [Theory]
    [InlineData("/api/scoreboard/2026/0")]
    [InlineData("/api/scoreboard/2026/13")]
    [InlineData("/api/scoreboard/1999/5")]
    [InlineData("/api/scoreboard/2101/5")]
    public async Task GetScoreboard_WithInvalidParameters_ShouldReturn400BadRequest(string uri)
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
