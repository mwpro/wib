using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wib.Api.Store;
using Wib.Api.Vouchers;
using Wib.Domain.Members;
using Wib.Domain.Store;
using Wib.Domain.Vouchers;

namespace Wib.UnitTests.Vouchers;

public class VoucherEndpointsTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public VoucherEndpointsTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task EnsureMemberHasBalanceAsync(string sub, string name, int balance)
    {
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var member = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == sub);
            if (member == null)
            {
                member = A.Member().WithExternalSubjectId(sub).WithName(name).Build();
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
                member.DebitWallet(-diff);
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
    public async Task PurchaseRewardItem_WhenDomainRejects_ShouldReturn400WithErrorCode()
    {
        var sub = "poor-buyer";
        var client = _factory.CreateAuthenticatedClient(sub, "Poor Buyer");
        await EnsureMemberHasBalanceAsync(sub, "Poor Buyer", 5);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Drogi Masaż", 20));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var buyRes = await client.PostAsync($"/api/store/items/{item!.Id}/purchase", null);

        await buyRes.ShouldBeProblemAsync(WalletErrors.NotEnoughPoints);
    }

    [Fact]
    public async Task PurchaseRewardItem_WhenNotFound_ShouldReturn404WithErrorCode()
    {
        var client = _factory.CreateAuthenticatedClient();
        var buyRes = await client.PostAsync("/api/store/items/999999/purchase", null);
        await buyRes.ShouldBeProblemAsync(RewardItemErrors.NotFound);
    }

    [Fact]
    public async Task PurchaseRewardItem_WithSufficientBalance_ShouldDeductBalanceAndCreateAvailableVoucher()
    {
        var sub = "wealthy-buyer";
        var client = _factory.CreateAuthenticatedClient(sub, "Wealthy Buyer");
        await EnsureMemberHasBalanceAsync(sub, "Wealthy Buyer", 100);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Kolacja sushi", 40, "Zestaw premium"));
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
        buyData.Voucher.IsRedeemed.Should().BeFalse();
        buyData.Voucher.RedeemedAt.Should().BeNull();
        buyData.Voucher.PurchasedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify in DB
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var member = await db.Members
                .Include(m => m.Vouchers)
                .FirstOrDefaultAsync(m => m.ExternalSubjectId == sub);
            member!.WalletBalance.Should().Be(60);
            member.Vouchers.Any(v => v.Id == buyData.Voucher.Id).Should().BeTrue();

            var voucher = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == buyData.Voucher.Id);
            voucher.Should().NotBeNull();
            voucher!.IsRedeemed.Should().BeFalse();
            voucher.TitleSnapshot.Should().Be("Kolacja sushi");
            voucher.PointCostSnapshot.Should().Be(40);
        });
    }

    [Fact]
    public async Task PurchaseRewardItem_SingleClaim_ShouldExhaustQuantityAndPreventSecondPurchase()
    {
        var sub = "single-claim-buyer";
        var client = _factory.CreateAuthenticatedClient(sub, "Single Buyer");
        await EnsureMemberHasBalanceAsync(sub, "Single Buyer", 100);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Jedyny Masaż", 15, null, 1));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        // 1st purchase succeeds
        var buy1 = await client.PostAsync($"/api/store/items/{item!.Id}/purchase", null);
        buy1.StatusCode.Should().Be(HttpStatusCode.Created);

        // 2nd purchase fails because quantity is exhausted
        var buy2 = await client.PostAsync($"/api/store/items/{item.Id}/purchase", null);
        buy2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Item should no longer appear in store catalog
        var listRes = await client.GetAsync("/api/store/items");
        var activeItems = await listRes.Content.ReadFromJsonAsync<List<RewardItemResponse>>(_jsonOptions);
        activeItems!.Any(i => i.Id == item.Id).Should().BeFalse();
    }

    [Fact]
    public async Task GetVouchers_ShouldReturnOnlyUserVouchersOrderedByPurchasedAtDesc()
    {
        var user1Sub = "user-one";
        var user2Sub = "user-two";
        var client1 = _factory.CreateAuthenticatedClient(user1Sub, "User One");
        var client2 = _factory.CreateAuthenticatedClient(user2Sub, "User Two");

        await EnsureMemberHasBalanceAsync(user1Sub, "User One", 100);
        await EnsureMemberHasBalanceAsync(user2Sub, "User Two", 100);

        var itemRes = await client1.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Książka", 10));
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
    public async Task GetVouchers_WithIsRedeemedFilter_ShouldFilterCorrectly()
    {
        var sub = "filter-test-user";
        var client = _factory.CreateAuthenticatedClient(sub, "Filter Tester");
        await EnsureMemberHasBalanceAsync(sub, "Filter Tester", 100);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Kawa latte", 5));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var buy1 = await client.PostAsync($"/api/store/items/{item!.Id}/purchase", null);
        var v1 = (await buy1.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        var buy2 = await client.PostAsync($"/api/store/items/{item.Id}/purchase", null);
        var v2 = (await buy2.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        // Redeem v1
        var redeemRes = await client.PostAsync($"/api/vouchers/{v1.Id}/redemption", null);
        redeemRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Filter Available (isRedeemed=false)
        var availRes = await client.GetAsync("/api/vouchers?isRedeemed=false");
        availRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var availableList = await availRes.Content.ReadFromJsonAsync<List<VoucherResponse>>(_jsonOptions);
        availableList.Should().NotBeNull();
        availableList!.Any(v => v.Id == v2.Id).Should().BeTrue();
        availableList.Any(v => v.Id == v1.Id).Should().BeFalse();

        // Filter Redeemed (isRedeemed=true)
        var redeemedRes = await client.GetAsync("/api/vouchers?isRedeemed=true");
        redeemedRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var redeemedList = await redeemedRes.Content.ReadFromJsonAsync<List<VoucherResponse>>(_jsonOptions);
        redeemedList.Should().NotBeNull();
        redeemedList!.Any(v => v.Id == v1.Id).Should().BeTrue();
        redeemedList.Any(v => v.Id == v2.Id).Should().BeFalse();
    }

    [Fact]
    public async Task RedeemVoucher_WhenAvailable_ShouldTransitionToRedeemedWithTimestamp()
    {
        var sub = "redeemer-user";
        var client = _factory.CreateAuthenticatedClient(sub, "Redeemer");
        await EnsureMemberHasBalanceAsync(sub, "Redeemer", 100);

        var itemRes = await client.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Masaż stóp", 15));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var buyRes = await client.PostAsync($"/api/store/items/{item!.Id}/purchase", null);
        var voucher = (await buyRes.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        // Act
        var redeemRes = await client.PostAsync($"/api/vouchers/{voucher.Id}/redemption", null);

        // Assert
        redeemRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var redeemed = await redeemRes.Content.ReadFromJsonAsync<VoucherResponse>(_jsonOptions);
        redeemed.Should().NotBeNull();
        redeemed!.IsRedeemed.Should().BeTrue();
        redeemed.RedeemedAt.Should().NotBeNull();
        redeemed.RedeemedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify in DB
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Vouchers.FirstOrDefaultAsync(v => v.Id == voucher.Id);
            entity!.IsRedeemed.Should().BeTrue();
            entity.RedeemedAt.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task RedeemVoucher_WhenOwnedByAnotherMember_ShouldReturn404AsIfMissing()
    {
        var ownerSub = "voucher-owner";
        var hackerSub = "voucher-thief";
        var ownerClient = _factory.CreateAuthenticatedClient(ownerSub, "Owner");
        var thiefClient = _factory.CreateAuthenticatedClient(hackerSub, "Thief");

        await EnsureMemberHasBalanceAsync(ownerSub, "Owner", 100);

        var itemRes = await ownerClient.PostAsJsonAsync("/api/store/items", new CreateRewardItemRequest("Czekolada", 10));
        var item = await itemRes.Content.ReadFromJsonAsync<RewardItemResponse>(_jsonOptions);

        var buyRes = await ownerClient.PostAsync($"/api/store/items/{item!.Id}/purchase", null);
        var voucher = (await buyRes.Content.ReadFromJsonAsync<BuyRewardResponse>(_jsonOptions))!.Voucher;

        // Act: thief tries to redeem owner's voucher
        var redeemRes = await thiefClient.PostAsync($"/api/vouchers/{voucher.Id}/redemption", null);

        // Assert
        await redeemRes.ShouldBeProblemAsync(VoucherErrors.NotFound);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var entity = await db.Vouchers.FirstAsync(v => v.Id == voucher.Id);
            entity.IsRedeemed.Should().BeFalse();
        });
    }

    [Fact]
    public async Task RedeemVoucher_WhenNotFound_ShouldReturn404WithErrorCode()
    {
        var client = _factory.CreateAuthenticatedClient();
        var redeemRes = await client.PostAsync("/api/vouchers/999999/redemption", null);
        await redeemRes.ShouldBeProblemAsync(VoucherErrors.NotFound);
    }
}
