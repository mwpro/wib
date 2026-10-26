using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Chores;
using Wib.Domain.Chores;

namespace Wib.UnitTests.Chores;

public class ChoreEndpointsTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ChoreEndpointsTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ChoresEndpoints_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act & Assert
        var getRes = await unauthenticatedClient.GetAsync("/api/chores");
        getRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var postRes = await unauthenticatedClient.PostAsJsonAsync("/api/chores", new CreateChoreRequest("Test", null));
        postRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostChore_WithValidDataAndTags_ShouldCreateChoreAndReturn201()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var request = new CreateChoreRequest(
            Title: "Zmywanie naczyń",
            Description: "Umyć wszystko w zlewie",
            Points: 3,
            CadenceDays: 2,
            Tags: [" Kuchnia ", "sprzątanie", "KUCHNIA"]
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/chores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var chore = await response.Content.ReadFromJsonAsync<ChoreResponse>(_jsonOptions);
        chore.Should().NotBeNull();
        chore.Id.Should().BeGreaterThan(0);
        chore.Title.Should().Be("Zmywanie naczyń");
        chore.Description.Should().Be("Umyć wszystko w zlewie");
        chore.Points.Should().Be(3);
        chore.CadenceDays.Should().Be(2);
        chore.IsArchived.Should().BeFalse();
        chore.Tags.Should().BeEquivalentTo(["kuchnia", "sprzątanie"]);
        chore.Urgency.Should().Be("Fresh");
        chore.DaysSinceLastDone.Should().Be(0);
        chore.UrgencyRatio.Should().Be(0.0);

        // Verify in DB
        await _factory.ExecuteDbContext(async db =>
        {
            var entity = await db.Chores
                .Include(c => c.Tags)
                .FirstOrDefaultAsync(c => c.Id == chore.Id);

            entity.Should().NotBeNull();
            entity.Title.Should().Be("Zmywanie naczyń");
            entity.Tags.Select(t => t.Name).Should().BeEquivalentTo(["kuchnia", "sprzątanie"]);
        });
    }

    [Fact]
    public async Task PostChore_WithInvalidFields_ShouldReturn400WithErrorCode()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var request = new CreateChoreRequest(Title: "Valid", Points: 0);

        // Act
        var response = await client.PostAsJsonAsync("/api/chores", request);

        // Assert
        await response.ShouldBeProblem(ChoreErrors.PointsTooLow);
    }

    [Fact]
    public async Task PostChore_WithTooLongTagName_ShouldReturn400WithoutCreatingTag()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var tooLongTag = new string('x', 51);
        var request = new CreateChoreRequest(Title: "Valid", Tags: [tooLongTag]);

        // Act
        var response = await client.PostAsJsonAsync("/api/chores", request);

        // Assert
        await response.ShouldBeProblem(ChoreErrors.TagTooLong);
        await _factory.ExecuteDbContext(async db =>
            (await db.Tags.AnyAsync(t => t.Name == tooLongTag)).Should().BeFalse());
    }

    [Fact]
    public async Task PostChore_WithNullCadenceDays_ShouldReturnUnscheduled()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var request = new CreateChoreRequest(Title: "Zadanie ad-hoc", Points: 2);

        // Act
        var response = await client.PostAsJsonAsync("/api/chores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var chore = await response.Content.ReadFromJsonAsync<ChoreResponse>(_jsonOptions);
        chore.Should().NotBeNull();
        chore.CadenceDays.Should().BeNull();
        chore.Urgency.Should().Be("Unscheduled");
        chore.UrgencyRatio.Should().BeNull();
        chore.DaysSinceLastDone.Should().BeNull();
    }

    [Fact]
    public async Task GetChores_ShouldReturnSortedByUrgencyDescendingAndUnscheduledLast()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var now = DateTime.UtcNow;

        var seedMember = await _factory.Seed(A.Member().Build());

        var freshChore = A.Chore().WithCadenceDays(10).Build();
        var overdueChore = A.Chore().WithCadenceDays(5).Build();
        var neglectedChore = A.Chore().WithCadenceDays(5).Build();
        var unscheduledChore = A.Chore().Unscheduled().Build();

        freshChore.Complete(seedMember, now);                  // 0 days ago -> 0%
        overdueChore.Complete(seedMember, now.AddDays(-6));    // 6 days ago -> 120%
        neglectedChore.Complete(seedMember, now.AddDays(-10)); // 10 days ago -> 200%

        await _factory.Seed(freshChore, overdueChore, neglectedChore, unscheduledChore);
        var (freshId, overdueId, neglectedId, unscheduledId) =
            (freshChore.Id, overdueChore.Id, neglectedChore.Id, unscheduledChore.Id);

        // Act
        var response = await client.GetAsync("/api/chores");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<ChoreResponse>>(_jsonOptions);
        list.Should().NotBeNull();

        var ourChores = list.Where(c => c.Id == freshId || c.Id == overdueId || c.Id == neglectedId || c.Id == unscheduledId).ToList();

        // Order: Neglected (200%) -> Overdue (120%) -> Fresh (0%) -> Unscheduled
        ourChores.Should().SatisfyRespectively(
            neglected =>
            {
                neglected.Id.Should().Be(neglectedId);
                neglected.Urgency.Should().Be("Neglected");
            },
            overdue =>
            {
                overdue.Id.Should().Be(overdueId);
                overdue.Urgency.Should().Be("Overdue");
            },
            fresh =>
            {
                fresh.Id.Should().Be(freshId);
                fresh.Urgency.Should().Be("Fresh");
            },
            unscheduled =>
            {
                unscheduled.Id.Should().Be(unscheduledId);
                unscheduled.Urgency.Should().Be("Unscheduled");
                unscheduled.UrgencyRatio.Should().BeNull();
            }
        );
    }

    [Fact]
    public async Task GetChores_ShouldSortByUrgencyBandBeforeRatio()
    {
        // Arrange: a daily chore one day late (Overdue, 200%) must not outrank a weekly chore that is Neglected (143%)
        var client = _factory.CreateAuthenticatedClient();
        var now = DateTime.UtcNow;

        var seedMember = await _factory.Seed(A.Member().Build());

        var dailyChore = A.Chore().WithCadenceDays(1).Build();
        var weeklyChore = A.Chore().WithCadenceDays(7).Build();

        dailyChore.Complete(seedMember, now.AddDays(-2));   // 2 days ago -> 200%, first day late -> Overdue
        weeklyChore.Complete(seedMember, now.AddDays(-10)); // 10 days ago -> 143% -> Neglected

        await _factory.Seed(dailyChore, weeklyChore);
        var (dailyOverdueId, weeklyNeglectedId) = (dailyChore.Id, weeklyChore.Id);

        // Act
        var response = await client.GetAsync("/api/chores");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<ChoreResponse>>(_jsonOptions);
        list.Should().NotBeNull();

        var ourChores = list.Where(c => c.Id == dailyOverdueId || c.Id == weeklyNeglectedId).ToList();

        // Order: Neglected band first, regardless of the Overdue chore's higher ratio
        ourChores.Should().SatisfyRespectively(
            neglected =>
            {
                neglected.Id.Should().Be(weeklyNeglectedId);
                neglected.Urgency.Should().Be("Neglected");
            },
            overdue =>
            {
                overdue.Id.Should().Be(dailyOverdueId);
                overdue.Urgency.Should().Be("Overdue");
            }
        );
    }

    [Fact]
    public async Task GetChores_WithTagFilter_ShouldOnlyReturnMatchingChores()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var tagged = A.Chore().Build();
        tagged.SetTags([Tag.Create("ogród", DateTime.UtcNow)]); // the new tag is inserted with the chore
        var other = A.Chore().Build();

        await _factory.Seed(tagged, other);
        var (taggedChoreId, otherChoreId) = (tagged.Id, other.Id);

        // Act
        var response = await client.GetAsync("/api/chores?tag=ogród");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var chores = await response.Content.ReadFromJsonAsync<List<ChoreResponse>>(_jsonOptions);
        chores.Should().NotBeNull();
        chores.Any(c => c.Id == taggedChoreId).Should().BeTrue();
        chores.Any(c => c.Id == otherChoreId).Should().BeFalse();
    }

    [Fact]
    public async Task PutChore_ShouldUpdateFieldsAndTags()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var choreId = (await _factory.Seed(A.Chore().WithCadenceDays(3).Build())).Id;

        var updateRequest = new UpdateChoreRequest(
            Title: "Nowy tytuł",
            Description: "Zaktualizowany opis",
            Points: 4,
            CadenceDays: 7,
            Tags: ["salon", "porządki"]
        );

        // Act
        var response = await client.PutAsJsonAsync($"/api/chores/{choreId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ChoreResponse>(_jsonOptions);
        updated.Should().NotBeNull();
        updated.Title.Should().Be("Nowy tytuł");
        updated.Description.Should().Be("Zaktualizowany opis");
        updated.Points.Should().Be(4);
        updated.CadenceDays.Should().Be(7);
        updated.Tags.Should().BeEquivalentTo(["salon", "porządki"]);
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PutChore_WithInvalidFields_ShouldReturn400AndKeepChoreUnchanged()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var choreId = (await _factory.Seed(A.Chore().WithTitle("Stary tytuł").Build())).Id;
        var request = new UpdateChoreRequest("Nowy tytuł", null, 1, CadenceDays: 0, null);

        // Act
        var response = await client.PutAsJsonAsync($"/api/chores/{choreId}", request);

        // Assert
        await response.ShouldBeProblem(ChoreErrors.CadenceTooShort);
        await _factory.ExecuteDbContext(async db =>
            (await db.Chores.SingleAsync(c => c.Id == choreId)).Title.Should().Be("Stary tytuł"));
    }

    [Fact]
    public async Task PutChore_WhenArchivedOrNotFound_ShouldReturn404()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var archivedId = (await _factory.Seed(A.Chore().Archived().Build())).Id;

        var request = new UpdateChoreRequest("Update", null, 1, 1, null);

        // Act & Assert
        var resArchived = await client.PutAsJsonAsync($"/api/chores/{archivedId}", request);
        await resArchived.ShouldBeProblem(ChoreErrors.NotFound);

        var resNonExistent = await client.PutAsJsonAsync("/api/chores/999999", request);
        await resNonExistent.ShouldBeProblem(ChoreErrors.NotFound);
    }

    [Fact]
    public async Task DeleteChore_ShouldSoftDeleteAndExcludeFromGet()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var choreId = (await _factory.Seed(A.Chore().Build())).Id;

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/chores/{choreId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify in DB that it is soft-deleted
        await _factory.ExecuteDbContext(async db =>
        {
            var entity = await db.Chores.FindAsync(choreId);
            entity.Should().NotBeNull();
            entity.IsArchived.Should().BeTrue();
            entity.UpdatedAt.Should().NotBeNull();
        });

        // Verify excluded from GET
        var getResponse = await client.GetAsync("/api/chores");
        var list = await getResponse.Content.ReadFromJsonAsync<List<ChoreResponse>>(_jsonOptions);
        list.Should().NotBeNull();
        list.Any(c => c.Id == choreId).Should().BeFalse();

        // Second DELETE should return 404
        var reDeleteResponse = await client.DeleteAsync($"/api/chores/{choreId}");
        await reDeleteResponse.ShouldBeProblem(ChoreErrors.NotFound);
    }

    [Fact]
    public async Task PostCompletion_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.PostAsync("/api/chores/1/completion", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostCompletion_WhenChoreNotFoundOrArchived_ShouldReturnNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var archivedId = (await _factory.Seed(A.Chore().Archived().Build())).Id;

        // Act & Assert - non-existent
        var notFoundRes = await client.PostAsync("/api/chores/999999/completion", null);
        await notFoundRes.ShouldBeProblem(ChoreErrors.NotFound);

        // Act & Assert - archived
        var archivedRes = await client.PostAsync($"/api/chores/{archivedId}/completion", null);
        await archivedRes.ShouldBeProblem(ChoreErrors.NotFound);
    }

    [Fact]
    public async Task PostCompletion_WithValidChore_ShouldUpdateChore_CreditWallet_AndReturnCompleteChoreResponse()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var choreId = (await _factory.Seed(A.Chore().WithPoints(4).Build())).Id;

        // Act
        var response = await client.PostAsync($"/api/chores/{choreId}/completion", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteChoreResponse>(_jsonOptions);
        result.Should().NotBeNull();
        result!.PointsAwarded.Should().Be(4);
        result.MemberWalletBalance.Should().Be(4);
        result.Chore.Id.Should().Be(choreId);
        result.Chore.LastCompletedAt.Should().NotBeNull();
        result.Chore.Urgency.Should().Be("Fresh");
        result.Chore.DaysSinceLastDone.Should().Be(0);

        // Verify in DB
        await _factory.ExecuteDbContext(async db =>
        {
            var choreEntity = await db.Chores
                .Include(c => c.Completions)
                .FirstOrDefaultAsync(c => c.Id == choreId);
            choreEntity.Should().NotBeNull();
            choreEntity!.LastCompletedAt.Should().NotBeNull();
            choreEntity.Completions.Should().ContainSingle();

            var completion = choreEntity.Completions.First();
            completion.ChoreId.Should().Be(choreId);
            completion.PointsAwarded.Should().Be(4);

            var memberEntity = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == WibWebApplicationFactory.DefaultTestSub);
            memberEntity.Should().NotBeNull();
            memberEntity!.WalletBalance.Should().Be(4);
        });
    }

    [Fact]
    public async Task PostCompletion_WithUnscheduledChore_ShouldReturnZeroDaysSinceLastDone()
    {
        // Arrange: own member so the shared test member's wallet balance isn't affected
        var client = _factory.CreateAuthenticatedClient("unscheduled-completer-" + Guid.NewGuid());
        var choreId = (await _factory.Seed(A.Chore().Unscheduled().Build())).Id;

        // Act
        var response = await client.PostAsync($"/api/chores/{choreId}/completion", null);

        // Assert: frontend locks "Zrobione" for the rest of the day based on DaysSinceLastDone == 0
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CompleteChoreResponse>(_jsonOptions);
        result!.Chore.Urgency.Should().Be("Unscheduled");
        result.Chore.UrgencyRatio.Should().BeNull();
        result.Chore.DaysSinceLastDone.Should().Be(0);
    }

    [Fact]
    public async Task PostCompletion_MultipleTimes_ShouldAccumulatePointsAndCompletions()
    {
        // Arrange
        var memberSub = "repeat-completer-" + Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClient(memberSub);

        var choreId = (await _factory.Seed(A.Chore().WithPoints(3).Build())).Id;

        // Act: Complete twice
        var firstResponse = await client.PostAsync($"/api/chores/{choreId}/completion", null);
        var secondResponse = await client.PostAsync($"/api/chores/{choreId}/completion", null);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<CompleteChoreResponse>(_jsonOptions);
        firstResult!.MemberWalletBalance.Should().Be(3);
        firstResult.PointsAwarded.Should().Be(3);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<CompleteChoreResponse>(_jsonOptions);
        secondResult!.MemberWalletBalance.Should().Be(6);
        secondResult.PointsAwarded.Should().Be(3);

        // Verify in DB that two completions exist
        await _factory.ExecuteDbContext(async db =>
        {
            var completions = await db.ChoreCompletions.Where(cc => cc.ChoreId == choreId).ToListAsync();
            completions.Should().HaveCount(2);

            var member = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == memberSub);
            member.Should().NotBeNull();
            member!.WalletBalance.Should().Be(6);
        });
    }
}
