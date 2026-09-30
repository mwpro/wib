using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Wib.Api.ChoreCompletions;
using Wib.Api.Data.Entities;

namespace Wib.UnitTests.ChoreCompletions;

public class ChoreCompletionEndpointTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ChoreCompletionEndpointTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string? sub = null, string? name = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub ?? "auth0|test-completions-user-1");
        client.DefaultRequestHeaders.Add("X-Test-User-Name", name ?? "Completions Tester 1");
        return client;
    }

    [Fact]
    public async Task GetChoreCompletions_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act & Assert
        var res = await unauthenticatedClient.GetAsync("/api/chore-completions/2026/9");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetChoreCompletions_ShouldReturnMonthlyCompletionsOrderedDescending()
    {
        // Arrange
        var client = CreateAuthenticatedClient("auth0|cc-member-1", "Maciej");
        await client.GetAsync("/api/members/me");

        var choreId = 0;
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var m = db.Members.First(x => x.ExternalSubjectId == "auth0|cc-member-1");

            var chore = Chore.Create("Zmywarka", null, points: 5, cadenceDays: 1, DateTime.UtcNow);
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            choreId = chore.Id;

            // In August 2026
            var augDate1 = new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);
            var augDate2 = new DateTime(2026, 8, 20, 15, 0, 0, DateTimeKind.Utc);
            // In July 2026 (outside month)
            var julDate = new DateTime(2026, 7, 30, 12, 0, 0, DateTimeKind.Utc);

            var c1 = chore.Complete(m, augDate1);
            var c2 = chore.Complete(m, augDate2);
            var cPast = chore.Complete(m, julDate);

            db.ChoreCompletions.AddRange(c1, c2, cPast);
            await db.SaveChangesAsync();
        });

        // Act
        var res = await client.GetAsync("/api/chore-completions/2026/8?page=1&pageSize=10");

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<ChoreCompletionsResponse>(_jsonOptions);
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(2);
        body.Items.Should().HaveCount(2);
        body.HasMore.Should().BeFalse();
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(10);

        // Verify ordering: newest first (augDate2 then augDate1)
        body.Items.Should().SatisfyRespectively(
            first =>
            {
                first.CompletedAt.Should().Be(new DateTime(2026, 8, 20, 15, 0, 0, DateTimeKind.Utc));
                first.ChoreTitle.Should().Be("Zmywarka");
                first.MemberName.Should().Be("Maciej");
                first.PointsAwarded.Should().Be(5);
            },
            second =>
            {
                second.CompletedAt.Should().Be(new DateTime(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc));
                second.ChoreTitle.Should().Be("Zmywarka");
                second.MemberName.Should().Be("Maciej");
                second.PointsAwarded.Should().Be(5);
            }
        );
    }

    [Fact]
    public async Task GetChoreCompletions_Pagination_ShouldWorkCorrectly()
    {
        // Arrange
        var client = CreateAuthenticatedClient("auth0|cc-pag-user", "Pagination Tester");
        await client.GetAsync("/api/members/me");

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var m = db.Members.First(x => x.ExternalSubjectId == "auth0|cc-pag-user");

            var chore = Chore.Create("Śmieci", null, points: 2, cadenceDays: 2, DateTime.UtcNow);
            db.Chores.Add(chore);
            await db.SaveChangesAsync();

            var date1 = new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc);
            var date2 = new DateTime(2026, 5, 5, 10, 0, 0, DateTimeKind.Utc);
            var date3 = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc);

            db.ChoreCompletions.AddRange(
                chore.Complete(m, date1),
                chore.Complete(m, date2),
                chore.Complete(m, date3)
            );
            await db.SaveChangesAsync();
        });

        // Act - Page 1 with pageSize 2
        var resPage1 = await client.GetAsync("/api/chore-completions/2026/5?page=1&pageSize=2");
        resPage1.StatusCode.Should().Be(HttpStatusCode.OK);
        var body1 = await resPage1.Content.ReadFromJsonAsync<ChoreCompletionsResponse>(_jsonOptions);
        body1.Should().NotBeNull();
        body1!.TotalCount.Should().Be(3);
        body1.HasMore.Should().BeTrue();
        body1.Page.Should().Be(1);
        body1.Items.Should().SatisfyRespectively(
            first =>
            {
                first.CompletedAt.Should().Be(new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc));
                first.ChoreTitle.Should().Be("Śmieci");
                first.PointsAwarded.Should().Be(2);
            },
            second =>
            {
                second.CompletedAt.Should().Be(new DateTime(2026, 5, 5, 10, 0, 0, DateTimeKind.Utc));
                second.ChoreTitle.Should().Be("Śmieci");
                second.PointsAwarded.Should().Be(2);
            }
        );

        // Act - Page 2 with pageSize 2
        var resPage2 = await client.GetAsync("/api/chore-completions/2026/5?page=2&pageSize=2");
        resPage2.StatusCode.Should().Be(HttpStatusCode.OK);
        var body2 = await resPage2.Content.ReadFromJsonAsync<ChoreCompletionsResponse>(_jsonOptions);
        body2.Should().NotBeNull();
        body2!.TotalCount.Should().Be(3);
        body2.HasMore.Should().BeFalse();
        body2.Page.Should().Be(2);
        body2.Items.Should().SatisfyRespectively(
            third =>
            {
                third.CompletedAt.Should().Be(new DateTime(2026, 5, 2, 10, 0, 0, DateTimeKind.Utc));
                third.ChoreTitle.Should().Be("Śmieci");
                third.PointsAwarded.Should().Be(2);
            }
        );
    }

    [Theory]
    [InlineData("/api/chore-completions/2026/0")]
    [InlineData("/api/chore-completions/2026/13")]
    [InlineData("/api/chore-completions/1999/5")]
    [InlineData("/api/chore-completions/2101/5")]
    [InlineData("/api/chore-completions/2026/5?pageSize=0")]
    [InlineData("/api/chore-completions/2026/5?pageSize=101")]
    public async Task GetChoreCompletions_WithInvalidParameters_ShouldReturn400BadRequest(string uri)
    {
        // Arrange
        var client = CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
