using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wib.Api.Data;
using Wib.Api.Data.Entities;
using Xunit;

namespace Wib.UnitTests;

public class AuthenticationAndProvisioningTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;

    public AuthenticationAndProvisioningTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutHeaders_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/members/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithSyntheticHeader_ShouldAuthenticateAndJitProvisionMember()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/members/me");
        request.Headers.Add("X-Test-Sub", "auth0|test-alice");
        request.Headers.Add("X-Test-User-Name", "Alice Test");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("externalSubjectId").GetString().Should().Be("auth0|test-alice");
        json.GetProperty("name").GetString().Should().Be("Alice Test");
        json.GetProperty("walletBalance").GetInt32().Should().Be(0);

        // Verify directly in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
        var member = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == "auth0|test-alice");
        member.Should().NotBeNull();
        member!.Name.Should().Be("Alice Test");
        member.WalletBalance.Should().Be(0);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithUrlEncodedPolishCharacters_ShouldAuthenticateAndCorrectlyDecodeName()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/members/me");
        request.Headers.Add("X-Test-Sub", Uri.EscapeDataString("auth0|test-użytkownik"));
        request.Headers.Add("X-Test-User-Name", Uri.EscapeDataString("Test Użytkownik"));

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("externalSubjectId").GetString().Should().Be("auth0|test-użytkownik");
        json.GetProperty("name").GetString().Should().Be("Test Użytkownik");
    }

    [Fact]
    public async Task JitProvisioning_WhenMemberExists_ShouldUpdateProfileWithoutResettingPoints()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Seed existing member with balance
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
            db.Members.Add(new Member
            {
                ExternalSubjectId = "auth0|test-bob",
                Name = "Bob Old Name",
                WalletBalance = 50,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            });
            await db.SaveChangesAsync();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/members/me");
        request.Headers.Add("X-Test-Sub", "auth0|test-bob");
        request.Headers.Add("X-Test-User-Name", "Bob New Name");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("externalSubjectId").GetString().Should().Be("auth0|test-bob");
        json.GetProperty("name").GetString().Should().Be("Bob New Name");
        json.GetProperty("walletBalance").GetInt32().Should().Be(50);

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
            var member = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == "auth0|test-bob");
            member.Should().NotBeNull();
            member!.Name.Should().Be("Bob New Name");
            member.WalletBalance.Should().Be(50);
            member.UpdatedAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task AuthenticatedRequest_ToEndpointNotRequiringMember_ShouldNotTriggerJitProvisioning()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("X-Test-Sub", "auth0|test-lazy-user");
        request.Headers.Add("X-Test-User-Name", "Lazy User");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify member was NOT provisioned because endpoint did not access current member
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
        var member = await db.Members.FirstOrDefaultAsync(m => m.ExternalSubjectId == "auth0|test-lazy-user");
        member.Should().BeNull();
    }
}
