using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Data.Entities;
using Wib.Api.Store;
using Wib.Api.Vouchers;

namespace Wib.UnitTests.Vouchers;

public class VoucherEndpointsTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public VoucherEndpointsTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string? sub = null, string? name = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub ?? "auth0|test-voucher-user");
        client.DefaultRequestHeaders.Add("X-Test-User-Name", name ?? "Voucher Tester");
        return client;
    }

    private async Task EnsureMemberHasBalanceAsync(string sub, string name, int balance)
    {
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var member = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == sub);
            if (member == null)
            {
                member = Member.Create(sub, name, DateTime.UtcNow);
                db.Members.Add(member);
            }
            // Reset to requested balance
            var diff = balance - member.WalletBalance;
            if (diff > 0)
            {
                member.CreditWallet(diff);
            }
            else if (diff < 0)
            {
                member.TryDebitWallet(-diff);
            }
            await db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task PurchaseAndVoucherEndpoints_WithoutAuth_ShouldReturnUnauthorized()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var buyRes = await unauthenticatedClient.PostAsync("/api/store/items/1/purchase", null);
        buyRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var getRes = await unauthenticatedClient.GetAsync("/api/vouchers");
        getRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var redeemRes = await unauthenticatedClient.PostAsync("/api/vouchers/1/redemption", null);
        redeemRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PurchaseRewardItem_WhenInsufficientBalance_ShouldReturn400()
    {
        var sub = "auth0|poor-buyer";
        var client = CreateAuthenticatedClient(sub, "Poor Buyer");
        await EnsureMemberHasBalanceAsync(sub, "Poor Buyer", 5);

        // Create an item costing 20 points
        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Drogi Masaż", null, 20));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        // Act: attempt to buy with only 5 points
        var buyRes = await client.PostAsync($"/api/store/items/{item!.Id}/purchase", null);

        // Assert
        buyRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await buyRes.Content.ReadAsStringAsync();
        content.Should().Contain("Niewystarczająca liczba punktów");
    }

    [Fact]
    public async Task PurchaseRewardItem_WhenInactive_ShouldReturn400()
    {
        var sub = "auth0|inactive-buyer";
        var client = CreateAuthenticatedClient(sub, "Inactive Buyer");
        await EnsureMemberHasBalanceAsync(sub, "Inactive Buyer", 50);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Wycieczka", null, 20));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        // Deactivate item
        await client.DeleteAsync($"/api/store/items/{item!.Id}");

        // Act
        var buyRes = await client.PostAsync($"/api/store/items/{item.Id}/purchase", null);

        // Assert
        buyRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await buyRes.Content.ReadAsStringAsync();
        content.Should().Contain("nie jest już aktywna");
    }

    [Fact]
    public async Task PurchaseRewardItem_WhenNotFound_ShouldReturn404()
    {
        var client = CreateAuthenticatedClient();
        var buyRes = await client.PostAsync("/api/store/items/999999/purchase", null);
        buyRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PurchaseRewardItem_WithSufficientBalance_ShouldDeductBalanceAndCreateAvailableVoucher()
    {
        var sub = "auth0|wealthy-buyer";
        var client = CreateAuthenticatedClient(sub, "Wealthy Buyer");
        await EnsureMemberHasBalanceAsync(sub, "Wealthy Buyer", 100);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Kolacja sushi", "Zestaw premium", 40));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        // Act
        var buyRes = await client.PostAsync($"/api/store/items/{item!.Id}/purchase", null);

        // Assert
        buyRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var buyData = await buyRes.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions);
        buyData.Should().NotBeNull();
        buyData!.MemberWalletBalance.Should().Be(60);
        buyData.Voucher.Should().NotBeNull();
        buyData.Voucher.TitleSnapshot.Should().Be("Kolacja sushi");
        buyData.Voucher.PointCostSnapshot.Should().Be(40);
        buyData.Voucher.Status.Should().Be("Available");
        buyData.Voucher.RedeemedAt.Should().BeNull();
        buyData.Voucher.PurchasedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify in DB
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var member = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == sub);
            member!.WalletBalance.Should().Be(60);

            var voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == buyData.Voucher.Id);
            voucher.Should().NotBeNull();
            voucher!.Status.Should().Be(VoucherStatus.Available);
            voucher.TitleSnapshot.Should().Be("Kolacja sushi");
            voucher.PointCostSnapshot.Should().Be(40);
        });
    }

    [Fact]
    public async Task GetVouchers_ShouldReturnOnlyUserVouchersOrderedByPurchasedAtDesc()
    {
        var user1Sub = "auth0|user-one";
        var user2Sub = "auth0|user-two";
        var client1 = CreateAuthenticatedClient(user1Sub, "User One");
        var client2 = CreateAuthenticatedClient(user2Sub, "User Two");

        await EnsureMemberHasBalanceAsync(user1Sub, "User One", 100);
        await EnsureMemberHasBalanceAsync(user2Sub, "User Two", 100);

        var itemRes = await client1.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Książka", null, 10));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        // User 1 buys twice
        var buy1 = await client1.PostAsync($"/api/store/items/{item!.Id}/purchase", null);
        var v1 = (await buy1.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        var buy2 = await client1.PostAsync($"/api/store/items/{item.Id}/purchase", null);
        var v2 = (await buy2.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        // User 2 buys once
        var buy3 = await client2.PostAsync($"/api/store/items/{item.Id}/purchase", null);
        var v3 = (await buy3.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        // Act: User 1 queries vouchers
        var listRes = await client1.GetAsync("/api/vouchers");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var vouchers = await listRes.Content.ReadFromJsonAsync<List<VoucherResponse>>(_jsonOptions);

        vouchers.Should().NotBeNull();
        vouchers!.Count.Should().BeGreaterThanOrEqualTo(2);
        vouchers.Any(v => v.Id == v1.Id).Should().BeTrue();
        vouchers.Any(v => v.Id == v2.Id).Should().BeTrue();
        vouchers.Any(v => v.Id == v3.Id).Should().BeFalse(); // Does not contain User 2's voucher
    }

    [Fact]
    public async Task GetVouchers_WithStatusFilter_ShouldFilterCorrectly()
    {
        var sub = "auth0|filter-test-user";
        var client = CreateAuthenticatedClient(sub, "Filter Tester");
        await EnsureMemberHasBalanceAsync(sub, "Filter Tester", 100);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Kawa latte", null, 5));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var buy1 = await client.PostAsync($"/api/store/items/{item!.Id}/purchase", null);
        var v1 = (await buy1.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        var buy2 = await client.PostAsync($"/api/store/items/{item.Id}/purchase", null);
        var v2 = (await buy2.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        // Redeem v1
        var redeemRes = await client.PostAsync($"/api/vouchers/{v1.Id}/redemption", null);
        redeemRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Filter Available
        var availRes = await client.GetAsync("/api/vouchers?status=Available");
        availRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var availableList = await availRes.Content.ReadFromJsonAsync<List<VoucherResponse>>(_jsonOptions);
        availableList.Should().NotBeNull();
        availableList!.Any(v => v.Id == v2.Id).Should().BeTrue();
        availableList.Any(v => v.Id == v1.Id).Should().BeFalse();

        // Filter Redeemed
        var redeemedRes = await client.GetAsync("/api/vouchers?status=Redeemed");
        redeemedRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var redeemedList = await redeemedRes.Content.ReadFromJsonAsync<List<VoucherResponse>>(_jsonOptions);
        redeemedList.Should().NotBeNull();
        redeemedList!.Any(v => v.Id == v1.Id).Should().BeTrue();
        redeemedList.Any(v => v.Id == v2.Id).Should().BeFalse();

        // Invalid filter
        var badRes = await client.GetAsync("/api/vouchers?status=NotValid");
        badRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RedeemVoucher_WhenAvailable_ShouldTransitionToRedeemedWithTimestamp()
    {
        var sub = "auth0|redeemer-user";
        var client = CreateAuthenticatedClient(sub, "Redeemer");
        await EnsureMemberHasBalanceAsync(sub, "Redeemer", 100);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Masaż stóp", null, 15));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var buyRes = await client.PostAsync($"/api/store/items/{item!.Id}/purchase", null);
        var voucher = (await buyRes.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        // Act
        var redeemRes = await client.PostAsync($"/api/vouchers/{voucher.Id}/redemption", null);

        // Assert
        redeemRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var redeemed = await redeemRes.Content.ReadFromJsonAsync<VoucherResponse>(_jsonOptions);
        redeemed.Should().NotBeNull();
        redeemed!.Status.Should().Be("Redeemed");
        redeemed.RedeemedAt.Should().NotBeNull();
        redeemed.RedeemedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify in DB
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == voucher.Id);
            entity!.Status.Should().Be(VoucherStatus.Redeemed);
            entity.RedeemedAt.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task RedeemVoucher_WhenAlreadyRedeemed_ShouldReturn400()
    {
        var sub = "auth0|double-redeemer";
        var client = CreateAuthenticatedClient(sub, "Double Redeemer");
        await EnsureMemberHasBalanceAsync(sub, "Double Redeemer", 100);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Lody", null, 10));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var buyRes = await client.PostAsync($"/api/store/items/{item!.Id}/purchase", null);
        var voucher = (await buyRes.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        // First redeem succeeds
        await client.PostAsync($"/api/vouchers/{voucher.Id}/redemption", null);

        // Second redeem fails
        var secondRedeem = await client.PostAsync($"/api/vouchers/{voucher.Id}/redemption", null);
        secondRedeem.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await secondRedeem.Content.ReadAsStringAsync();
        content.Should().Contain("został już zrealizowany");
    }

    [Fact]
    public async Task RedeemVoucher_WhenOwnedByAnotherMember_ShouldReturn403()
    {
        var ownerSub = "auth0|voucher-owner";
        var hackerSub = "auth0|voucher-thief";
        var ownerClient = CreateAuthenticatedClient(ownerSub, "Owner");
        var thiefClient = CreateAuthenticatedClient(hackerSub, "Thief");

        await EnsureMemberHasBalanceAsync(ownerSub, "Owner", 100);

        var itemRes = await ownerClient.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Czekolada", null, 10));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var buyRes = await ownerClient.PostAsync($"/api/store/items/{item!.Id}/purchase", null);
        var voucher = (await buyRes.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        // Act: thief tries to redeem owner's voucher
        var redeemRes = await thiefClient.PostAsync($"/api/vouchers/{voucher.Id}/redemption", null);

        // Assert
        redeemRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RedeemVoucher_WhenNotFound_ShouldReturn404()
    {
        var client = CreateAuthenticatedClient();
        var redeemRes = await client.PostAsync("/api/vouchers/999999/redemption", null);
        redeemRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
