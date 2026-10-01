using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Data.Entities;
using Wib.Api.Store;

namespace Wib.UnitTests.Store;

public class StoreEndpointsTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public StoreEndpointsTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string? sub = null, string? name = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub ?? "auth0|test-store-user");
        client.DefaultRequestHeaders.Add("X-Test-User-Name", name ?? "Store Tester");
        return client;
    }

    [Fact]
    public async Task StoreEndpoints_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var getRes = await unauthenticatedClient.GetAsync("/api/store/items");
        getRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var postRes = await unauthenticatedClient.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Test", 10));
        postRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostRewardItem_WithValidDataAndQuantity_ShouldCreateRewardAndReturn201()
    {
        var client = CreateAuthenticatedClient("auth0|store-creator-1", "Creator 1");
        var request = new CreateRewardItemRequest("Masaż pleców", 20, "30 minut relaksu", 2);

        var response = await client.PostAsJsonAsync("/api/store/items", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await response.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);
        item.Should().NotBeNull();
        item!.Id.Should().BeGreaterThan(0);
        item.Title.Should().Be("Masaż pleców");
        item.Description.Should().Be("30 minut relaksu");
        item.PointCost.Should().Be(20);
        item.Quantity.Should().Be(2);
        item.IsActive.Should().BeTrue();
        item.CreatedByMemberId.Should().BeGreaterThan(0);
        item.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify in DB
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.RewardItems.FirstOrDefaultAsync(r => r.Id == item.Id);
            entity.Should().NotBeNull();
            entity!.Title.Should().Be("Masaż pleców");
            entity.Quantity.Should().Be(2);
            entity.IsActive.Should().BeTrue();
        });
    }

    [Theory]
    [InlineData("", 10, null)]
    [InlineData("   ", 10, null)]
    [InlineData("Valid Title", 0, null)]
    [InlineData("Valid Title", -5, null)]
    [InlineData("Valid Title", 10, 0)]
    [InlineData("Valid Title", 10, -1)]
    public async Task PostRewardItem_WithInvalidData_ShouldReturn400(string title, int pointCost, int? quantity)
    {
        var client = CreateAuthenticatedClient();
        var request = new CreateRewardItemRequest(title, pointCost, null, quantity);

        var response = await client.PostAsJsonAsync("/api/store/items", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRewardItems_ShouldReturnActiveItemsOrderedByTitleAscending()
    {
        var client = CreateAuthenticatedClient("auth0|store-lister", "Lister");

        // Create 3 items: "Zebra", "Albatros", "Bóbr"
        await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Zebra", 10));
        await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Albatros", 5));
        var bRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Bóbr", 15));
        var bobr = await bRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        // Deactivate "Bóbr"
        await client.DeleteAsync($"/api/store/items/{bobr!.Id}");

        // Query active items
        var listRes = await client.GetAsync("/api/store/items");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await listRes.Content.ReadFromJsonAsync<List<RewardItemResponse>>(_jsonOptions);

        items.Should().NotBeNull();
        items!.Any(i => i.Id == bobr.Id).Should().BeFalse();

        // Check alphabetical sorting
        var titles = items.Select(i => i.Title).ToList();
        titles.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task PutRewardItem_WithValidData_ShouldUpdateItem()
    {
        var client = CreateAuthenticatedClient();
        var postRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Stary Tytuł", 10, "Stary"));
        var created = await postRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var putRes = await client.PutAsJsonAsync($"/api/store/items/{created!.Id}", new UpdateRewardItemRequest("Nowy Tytuł", 25, "Nowy", 3));

        putRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await putRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Nowy Tytuł");
        updated.Description.Should().Be("Nowy");
        updated.PointCost.Should().Be(25);
        updated.Quantity.Should().Be(3);
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteRewardItem_ShouldSoftDeactivate()
    {
        var client = CreateAuthenticatedClient();
        var postRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Do Usunięcia", 10));
        var created = await postRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var delRes = await client.DeleteAsync($"/api/store/items/{created!.Id}");
        delRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.RewardItems.FirstOrDefaultAsync(r => r.Id == created.Id);
            entity.Should().NotBeNull();
            entity!.IsActive.Should().BeFalse();
            entity.UpdatedAt.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task PutRewardItem_WhenNotFound_ShouldReturn404()
    {
        var client = CreateAuthenticatedClient();
        var putRes = await client.PutAsJsonAsync("/api/store/items/99999", new UpdateRewardItemRequest("Test", 10));
        putRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRewardItem_WhenNotFound_ShouldReturn404()
    {
        var client = CreateAuthenticatedClient();
        var delRes = await client.DeleteAsync("/api/store/items/99999");
        delRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
