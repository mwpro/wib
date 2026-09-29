using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Chores;
using Wib.Api.Data.Entities;

namespace Wib.UnitTests.Chores;

public class ChoreEndpointsTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ChoreEndpointsTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string? sub = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub ?? "auth0|test-chores-user");
        client.DefaultRequestHeaders.Add("X-Test-User-Name", sub is null ? "Chores Tester" : "Test User");
        return client;
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
        var client = CreateAuthenticatedClient();
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
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Chores
                .Include(c => c.ChoreTags)
                .ThenInclude(ct => ct.Tag)
                .FirstOrDefaultAsync(c => c.Id == chore.Id);

            entity.Should().NotBeNull();
            entity.Title.Should().Be("Zmywanie naczyń");
            entity.ChoreTags.Select(ct => ct.Tag.Name).Should().BeEquivalentTo(["kuchnia", "sprzątanie"]);
        });
    }

    [Theory]
    [InlineData("", 1, 1)]        // Empty title
    [InlineData("   ", 1, 1)]     // Whitespace title
    [InlineData("Valid", 0, 1)]   // Points < 1
    [InlineData("Valid", -5, 1)]  // Points negative
    [InlineData("Valid", 1, 0)]   // Cadence < 1
    [InlineData("Valid", 1, -3)]  // Cadence negative
    public async Task PostChore_WithInvalidData_ShouldReturn400(string title, int points, int cadenceDays)
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var request = new CreateChoreRequest(
            Title: title,
            Description: null,
            Points: points,
            CadenceDays: cadenceDays,
            Tags: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/chores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostChore_WithNullCadenceDays_ShouldReturnUnscheduled()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
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

    [Theory]
    [InlineData(256, null, null)]     // Title too long
    [InlineData(10, 2001, null)]      // Description too long
    [InlineData(10, null, 51)]        // Tag name too long
    public async Task PostChore_WithFieldsExceedingMaxLength_ShouldReturn400(
        int titleLength, int? descriptionLength, int? tagNameLength)
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var request = new CreateChoreRequest(
            Title: new string('x', titleLength),
            Description: descriptionLength.HasValue ? new string('x', descriptionLength.Value) : null,
            Tags: tagNameLength.HasValue ? [new string('x', tagNameLength.Value)] : null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/chores", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetChores_ShouldReturnSortedByUrgencyDescendingAndUnscheduledLast()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var now = DateTime.UtcNow;

        int freshId = 0, overdueId = 0, neglectedId = 0, unscheduledId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var seedMember = Member.Create("auth0|seed-urgency-test", "Seed Member", now);
            db.Members.Add(seedMember);

            var freshChore = Chore.Create("Fresh Chore", null, 1, 10, now);
            var overdueChore = Chore.Create("Overdue Chore", null, 2, 5, now.AddDays(-10));
            var neglectedChore = Chore.Create("Neglected Chore", null, 3, 5, now.AddDays(-20));
            var unscheduledChore = Chore.Create("Unscheduled Chore", null, 5, null, now.AddDays(-1));

            db.Chores.AddRange(freshChore, overdueChore, neglectedChore, unscheduledChore);
            await db.SaveChangesAsync();

            freshChore.Complete(seedMember, now);          // 0 days ago -> 0%
            overdueChore.Complete(seedMember, now.AddDays(-5));   // 5 days ago -> 100%
            neglectedChore.Complete(seedMember, now.AddDays(-10)); // 10 days ago -> 200%

            await db.SaveChangesAsync();

            freshId = freshChore.Id;
            overdueId = overdueChore.Id;
            neglectedId = neglectedChore.Id;
            unscheduledId = unscheduledChore.Id;
        });

        // Act
        var response = await client.GetAsync("/api/chores");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<ChoreResponse>>(_jsonOptions);
        list.Should().NotBeNull();

        var ourChores = list.Where(c => c.Id == freshId || c.Id == overdueId || c.Id == neglectedId || c.Id == unscheduledId).ToList();

        // Order: Neglected (200%) -> Overdue (100%) -> Fresh (0%) -> Unscheduled
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
    public async Task GetChores_WithTagFilter_ShouldOnlyReturnMatchingChores()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var taggedChoreId = 0;
        var otherChoreId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var gardenTag = Tag.Create("ogród", DateTime.UtcNow);
            db.Tags.Add(gardenTag);

            var tagged = Chore.Create("Koszenie trawy", null, 2, 7, DateTime.UtcNow);
            tagged.SetTags([gardenTag]);

            var other = Chore.Create("Inne zadanie", null, 1, 7, DateTime.UtcNow);

            db.Chores.AddRange(tagged, other);
            await db.SaveChangesAsync();

            taggedChoreId = tagged.Id;
            otherChoreId = other.Id;
        });

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
        var client = CreateAuthenticatedClient();
        var choreId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var chore = Chore.Create("Stary tytuł", null, 1, 3, DateTime.UtcNow);
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            choreId = chore.Id;
        });

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

    [Theory]
    [InlineData("", 1, 1)]
    [InlineData("Valid", 0, 1)]
    [InlineData("Valid", 1, 0)]
    public async Task PutChore_WithInvalidData_ShouldReturn400(string title, int points, int cadenceDays)
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var request = new UpdateChoreRequest(title, null, points, cadenceDays, null);

        // Act
        var response = await client.PutAsJsonAsync("/api/chores/1", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutChore_WhenArchivedOrNotFound_ShouldReturn404()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var archivedId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var archived = Chore.Create("Archived", null, 1, null, DateTime.UtcNow);
            archived.Archive(DateTime.UtcNow);
            db.Chores.Add(archived);
            await db.SaveChangesAsync();
            archivedId = archived.Id;
        });

        var request = new UpdateChoreRequest("Update", null, 1, 1, null);

        // Act & Assert
        var resArchived = await client.PutAsJsonAsync($"/api/chores/{archivedId}", request);
        resArchived.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var resNonExistent = await client.PutAsJsonAsync("/api/chores/999999", request);
        resNonExistent.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteChore_ShouldSoftDeleteAndExcludeFromGet()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var choreId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var chore = Chore.Create("Do usunięcia", null, 1, null, DateTime.UtcNow);
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            choreId = chore.Id;
        });

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/chores/{choreId}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify in DB that it is soft-deleted
        await _factory.ExecuteDbContextAsync(async db =>
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
        reDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        var client = CreateAuthenticatedClient();
        var archivedId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var archived = Chore.Create("Archived Chore", null, 2, 3, DateTime.UtcNow);
            archived.Archive(DateTime.UtcNow);
            db.Chores.Add(archived);
            await db.SaveChangesAsync();
            archivedId = archived.Id;
        });

        // Act & Assert - non-existent
        var notFoundRes = await client.PostAsync("/api/chores/999999/completion", null);
        notFoundRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Act & Assert - archived
        var archivedRes = await client.PostAsync($"/api/chores/{archivedId}/completion", null);
        archivedRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostCompletion_WithValidChore_ShouldUpdateChore_CreditWallet_AndReturnCompleteChoreResponse()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var choreId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var chore = Chore.Create("Odkurzanie", "Dokładnie w sypialni", 4, 3, DateTime.UtcNow.AddDays(-5));
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            choreId = chore.Id;
        });

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
        await _factory.ExecuteDbContextAsync(async db =>
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

            var memberEntity = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == "auth0|test-chores-user");
            memberEntity.Should().NotBeNull();
            memberEntity!.WalletBalance.Should().Be(4);
        });
    }

    [Fact]
    public async Task PostCompletion_MultipleTimes_ShouldAccumulatePointsAndCompletions()
    {
        // Arrange
        var memberSub = "auth0|repeat-completer-" + Guid.NewGuid();
        var client = CreateAuthenticatedClient(memberSub);

        var choreId = 0;
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var chore = Chore.Create("Wyniesienie śmieci", null, 3, 2, DateTime.UtcNow);
            db.Chores.Add(chore);
            await db.SaveChangesAsync();
            choreId = chore.Id;
        });

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
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var completions = await db.ChoreCompletions.Where(cc => cc.ChoreId == choreId).ToListAsync();
            completions.Should().HaveCount(2);

            var member = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == memberSub);
            member.Should().NotBeNull();
            member!.WalletBalance.Should().Be(6);
        });
    }
}
