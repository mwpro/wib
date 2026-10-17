using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Wib.Api.Chores;
using Wib.Domain.Chores;
using Wib.Domain.Members;

namespace Wib.UnitTests.Chores;

public class ChoreHistoryEndpointTests : IClassFixture<WibWebApplicationFactory>
{
    // Gaps (in days) between consecutive completions of a 7-day chore, and the outcome each one earns.
    private static readonly (int Gap, string Urgency)[] Pattern =
    [
        (5, "Fresh"),
        (7, "DueSoon"),
        (8, "Overdue"),
        (10, "Neglected"),
        (6, "DueSoon")
    ];

    private readonly WibWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ChoreHistoryEndpointTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", "auth0|test-history-user");
        client.DefaultRequestHeaders.Add("X-Test-User-Name", "History Tester");
        return client;
    }

    private async Task<int> SeedChoreWithHistoryAsync(int completionCount)
    {
        var choreId = 0;
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var createdAt = DateTime.UtcNow.Date.AddHours(10).AddDays(-400);
            var ania = Member.Create($"auth0|ania-{Guid.NewGuid()}", "Ania", createdAt);
            var tomek = Member.Create($"auth0|tomek-{Guid.NewGuid()}", "Tomek", createdAt);
            db.Members.AddRange(ania, tomek);

            var chore = Chore.Create("Odkurzanie", null, 3, 7, createdAt);
            db.Chores.Add(chore);
            await db.SaveChangesAsync();

            var at = createdAt;
            for (var i = 0; i < completionCount; i++)
            {
                at = at.AddDays(Pattern[i % Pattern.Length].Gap);
                chore.Complete(i % 2 == 0 ? ania : tomek, at);
            }

            await db.SaveChangesAsync();
            choreId = chore.Id;
        });
        return choreId;
    }

    [Fact]
    public async Task GetHistory_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/chores/1/completions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHistory_WhenChoreNotFoundOrArchived_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var archivedId = 0;
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var archived = Chore.Create("Archived History Chore", null, 2, 3, DateTime.UtcNow);
            archived.Archive(DateTime.UtcNow);
            db.Chores.Add(archived);
            await db.SaveChangesAsync();
            archivedId = archived.Id;
        });

        // Act & Assert
        (await client.GetAsync("/api/chores/999999/completions")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/chores/{archivedId}/completions")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistory_ShouldPageNewestFirst_WithSummaryAcrossAllPages()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var choreId = await SeedChoreWithHistoryAsync(25);

        // Act
        var first = await client.GetFromJsonAsync<ChoreHistoryResponse>($"/api/chores/{choreId}/completions?page=1", _jsonOptions);
        var second = await client.GetFromJsonAsync<ChoreHistoryResponse>($"/api/chores/{choreId}/completions?page=2", _jsonOptions);

        // Assert - paging
        first!.Items.Should().HaveCount(20);
        first.PageSize.Should().Be(20);
        first.TotalCount.Should().Be(25);
        first.HasMore.Should().BeTrue();
        second!.Items.Should().HaveCount(5);
        second.HasMore.Should().BeFalse();

        var all = first.Items.Concat(second.Items).ToList();
        all.Select(i => i.CompletedAt).Should().BeInDescendingOrder();
        all.Select(i => i.Id).Should().OnlyHaveUniqueItems();

        // Assert - stored outcomes, oldest last
        var oldestFirst = Enumerable.Reverse(all).ToList();
        for (var i = 0; i < oldestFirst.Count; i++)
        {
            oldestFirst[i].DaysSincePrevious.Should().Be(Pattern[i % Pattern.Length].Gap);
            oldestFirst[i].UrgencyAtCompletion.Should().Be(Pattern[i % Pattern.Length].Urgency);
        }
        oldestFirst[0].MemberName.Should().Be("Ania");
        oldestFirst[0].PointsAwarded.Should().Be(3);

        // Assert - summary is identical on every page and spans all completions
        foreach (var summary in new[] { first.Summary, second.Summary })
        {
            summary.TotalCount.Should().Be(25);
            summary.OutcomeCounts.Should().BeEquivalentTo(new Dictionary<string, int>
            {
                ["Fresh"] = 5,
                ["DueSoon"] = 10,
                ["Overdue"] = 5,
                ["Neglected"] = 5
            });
            summary.OnTimeCount.Should().Be(15);
            summary.Members.Select(m => (m.MemberName, m.Count))
                .Should().Equal(("Ania", 13), ("Tomek", 12));
        }
    }

    [Fact]
    public async Task GetHistory_WithNoCompletions_ShouldReturnEmptyListAndZeroSummary()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var choreId = await SeedChoreWithHistoryAsync(0);

        // Act
        var response = await client.GetFromJsonAsync<ChoreHistoryResponse>($"/api/chores/{choreId}/completions", _jsonOptions);

        // Assert
        response!.Items.Should().BeEmpty();
        response.HasMore.Should().BeFalse();
        response.Summary.TotalCount.Should().Be(0);
        response.Summary.OutcomeCounts.Should().HaveCount(4).And.OnlyContain(o => o.Value == 0);
        response.Summary.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHistory_WithInvalidPageSize_ShouldReturn400()
    {
        var client = CreateAuthenticatedClient();
        var choreId = await SeedChoreWithHistoryAsync(1);

        var response = await client.GetAsync($"/api/chores/{choreId}/completions?pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
