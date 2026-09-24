using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wib.Api.Data;
using Wib.Api.Data.Entities;

namespace Wib.UnitTests;

public class AuthenticationAndProvisioningTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthenticationAndProvisioningTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "AuthTestDb_" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Testing:BypassAuth"] = "true",
                    ["JwtAuth:Authority"] = "https://test.eu.auth0.com/",
                    ["JwtAuth:Domain"] = "test.eu.auth0.com",
                    ["JwtAuth:ClientId"] = "test-client",
                    ["JwtAuth:Audience"] = "https://api.test"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WibDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                var inMemoryProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<WibDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName)
                           .UseInternalServiceProvider(inMemoryProvider);
                });
            });
        });
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
        request.Headers.Add("X-Test-User-Email", "alice@example.com");
        request.Headers.Add("X-Test-User-Picture", "https://example.com/alice.png");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("auth0UserId").GetString().Should().Be("auth0|test-alice");
        json.GetProperty("name").GetString().Should().Be("Alice Test");
        json.GetProperty("email").GetString().Should().Be("alice@example.com");
        json.GetProperty("picture").GetString().Should().Be("https://example.com/alice.png");
        json.GetProperty("walletBalance").GetInt32().Should().Be(0);
        json.GetProperty("earnedPoints").GetInt32().Should().Be(0);

        // Verify directly in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
        var member = await db.Members.FirstOrDefaultAsync(m => m.Auth0UserId == "auth0|test-alice");
        member.Should().NotBeNull();
        member!.Name.Should().Be("Alice Test");
        member.Email.Should().Be("alice@example.com");
        member.Picture.Should().Be("https://example.com/alice.png");
    }

    [Fact]
    public async Task JitProvisioning_WhenMemberExists_ShouldUpdateProfileWithoutResettingPoints()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Seed existing member with points and balance
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
            db.Members.Add(new Member
            {
                Auth0UserId = "auth0|test-bob",
                Name = "Bob Old Name",
                Email = "bob.old@example.com",
                Picture = "https://example.com/bob_old.png",
                WalletBalance = 50,
                EarnedPoints = 120,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            });
            await db.SaveChangesAsync();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/members/me");
        request.Headers.Add("X-Test-Sub", "auth0|test-bob");
        request.Headers.Add("X-Test-User-Name", "Bob New Name");
        request.Headers.Add("X-Test-User-Email", "bob.new@example.com");
        request.Headers.Add("X-Test-User-Picture", "https://example.com/bob_new.png");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("name").GetString().Should().Be("Bob New Name");
        json.GetProperty("email").GetString().Should().Be("bob.new@example.com");
        json.GetProperty("picture").GetString().Should().Be("https://example.com/bob_new.png");
        json.GetProperty("walletBalance").GetInt32().Should().Be(50);
        json.GetProperty("earnedPoints").GetInt32().Should().Be(120);

        // Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
            var member = await db.Members.FirstOrDefaultAsync(m => m.Auth0UserId == "auth0|test-bob");
            member.Should().NotBeNull();
            member!.Name.Should().Be("Bob New Name");
            member.WalletBalance.Should().Be(50);
            member.EarnedPoints.Should().Be(120);
            member.UpdatedAt.Should().NotBeNull();
        }
    }
}
