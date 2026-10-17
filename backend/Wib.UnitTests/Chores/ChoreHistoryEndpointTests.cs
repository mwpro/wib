using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Wib.Api.Chores;

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

    private async Task<int> SeedChoreWithHistoryAsync(int completionCount)
    {
        var createdAt = DateTime.UtcNow.Date.AddHours(10).AddDays(-400);
        var members = await _factory.SeedAsync(
            A.Member().WithName("Ania").Build(),
            A.Member().WithName("Tomek").Build());
        var chore = await _factory.SeedAsync(A.Chore().WithPoints(3).WithCadenceDays(7).CreatedAt(createdAt).Build());

        var at = createdAt;
        var completions = Enumerable.Range(0, completionCount)
            .Select(i =>
            {
                at = at.AddDays(Pattern[i % Pattern.Length].Gap);
                return chore.Complete(members[i % 2], at);
            })
            .ToArray();
        await _factory.SeedAsync(completions);

        return chore.Id;
    }

    private async Task<int> SeedArchivedChoreAsync() =>
        (await _factory.SeedAsync(A.Chore().Archived().Build())).Id;

    // ---- GET /api/chores/{id}/completions ----

    [Fact]
    public async Task GetCompletions_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/chores/1/completions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCompletions_WhenChoreNotFoundOrArchived_ShouldReturnNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var archivedId = await SeedArchivedChoreAsync();

        // Act & Assert
        (await client.GetAsync("/api/chores/999999/completions")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/chores/{archivedId}/completions")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCompletions_ShouldPageNewestFirstWithStoredOutcomes()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
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
    }

    [Fact]
    public async Task GetCompletions_WithNoCompletions_ShouldReturnEmptyPage()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var choreId = await SeedChoreWithHistoryAsync(0);

        // Act
        var response = await client.GetFromJsonAsync<ChoreHistoryResponse>($"/api/chores/{choreId}/completions", _jsonOptions);

        // Assert
        response!.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task GetCompletions_WithInvalidPageSize_ShouldReturn400()
    {
        var client = _factory.CreateAuthenticatedClient();
        var choreId = await SeedChoreWithHistoryAsync(1);

        var response = await client.GetAsync($"/api/chores/{choreId}/completions?pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- GET /api/chores/{id}/completions/summary ----

    [Fact]
    public async Task GetSummary_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/chores/1/completions/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSummary_WhenChoreNotFoundOrArchived_ShouldReturnNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var archivedId = await SeedArchivedChoreAsync();

        // Act & Assert
        (await client.GetAsync("/api/chores/999999/completions/summary")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/chores/{archivedId}/completions/summary")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSummary_ShouldSpanAllCompletions()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var choreId = await SeedChoreWithHistoryAsync(25);

        // Act
        var summary = await client.GetFromJsonAsync<ChoreHistorySummary>($"/api/chores/{choreId}/completions/summary", _jsonOptions);

        // Assert
        summary!.TotalCount.Should().Be(25);
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

    [Fact]
    public async Task GetSummary_WithNoCompletions_ShouldReturnZeroCountsForEveryOutcome()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var choreId = await SeedChoreWithHistoryAsync(0);

        // Act
        var summary = await client.GetFromJsonAsync<ChoreHistorySummary>($"/api/chores/{choreId}/completions/summary", _jsonOptions);

        // Assert
        summary!.TotalCount.Should().Be(0);
        summary.OutcomeCounts.Should().HaveCount(4).And.OnlyContain(o => o.Value == 0);
        summary.OnTimeCount.Should().Be(0);
        summary.Members.Should().BeEmpty();
    }
}
