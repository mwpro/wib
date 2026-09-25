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

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", "auth0|test-chores-user");
        client.DefaultRequestHeaders.Add("X-Test-User-Name", "Chores Tester");
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
    public async Task GetChores_ShouldReturnSortedByUrgencyDescendingAndUnscheduledLast()
    {
        // Arrange
        var client = CreateAuthenticatedClient();
        var now = DateTime.UtcNow;

        int freshId = 0, overdueId = 0, neglectedId = 0, unscheduledId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var freshChore = new Chore
            {
                Title = "Fresh Chore",
                Points = 1,
                CadenceDays = 10,
                LastCompletedAt = now, // 0 days ago -> 0%
                CreatedAt = now,
                IsArchived = false
            };

            var overdueChore = new Chore
            {
                Title = "Overdue Chore",
                Points = 2,
                CadenceDays = 5,
                LastCompletedAt = now.AddDays(-5), // 5 days ago -> 100%
                CreatedAt = now.AddDays(-10),
                IsArchived = false
            };

            var neglectedChore = new Chore
            {
                Title = "Neglected Chore",
                Points = 3,
                CadenceDays = 5,
                LastCompletedAt = now.AddDays(-10), // 10 days ago -> 200%
                CreatedAt = now.AddDays(-20),
                IsArchived = false
            };

            var unscheduledChore = new Chore
            {
                Title = "Unscheduled Chore",
                Points = 5,
                CadenceDays = null,
                LastCompletedAt = null,
                CreatedAt = now.AddDays(-1),
                IsArchived = false
            };

            db.Chores.AddRange(freshChore, overdueChore, neglectedChore, unscheduledChore);
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
        int taggedChoreId = 0;
        int otherChoreId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var gardenTag = new Tag { Name = "ogród", CreatedAt = DateTime.UtcNow };
            db.Tags.Add(gardenTag);

            var tagged = new Chore
            {
                Title = "Koszenie trawy",
                Points = 2,
                CadenceDays = 7,
                CreatedAt = DateTime.UtcNow,
                IsArchived = false
            };
            tagged.ChoreTags.Add(new ChoreTag { Chore = tagged, Tag = gardenTag });

            var other = new Chore
            {
                Title = "Inne zadanie",
                Points = 1,
                CadenceDays = 7,
                CreatedAt = DateTime.UtcNow,
                IsArchived = false
            };

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
        int choreId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var chore = new Chore
            {
                Title = "Stary tytuł",
                Points = 1,
                CadenceDays = 3,
                CreatedAt = DateTime.UtcNow,
                IsArchived = false
            };
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
        int archivedId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var archived = new Chore
            {
                Title = "Archived",
                CreatedAt = DateTime.UtcNow,
                IsArchived = true
            };
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
        int choreId = 0;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var chore = new Chore
            {
                Title = "Do usunięcia",
                Points = 1,
                CreatedAt = DateTime.UtcNow,
                IsArchived = false
            };
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
}
