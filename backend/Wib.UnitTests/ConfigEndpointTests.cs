using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wib.Api.Data;

namespace Wib.UnitTests;

public class ConfigEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConfigEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth0:Domain"] = "test-wib.eu.auth0.com",
                    ["Auth0:ClientId"] = "test-client-id-123",
                    ["Auth0:Audience"] = "https://wib-api.example.com",
                    ["Testing:BypassAuth"] = "true"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WibDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddDbContext<WibDbContext>(options =>
                {
                    options.UseInMemoryDatabase("ConfigTestDb");
                });
            });
        });
    }

    [Fact]
    public async Task GetConfig_ShouldReturnAuth0SettingsAndTestMode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var auth0 = json.GetProperty("auth0");
        auth0.GetProperty("domain").GetString().Should().Be("test-wib.eu.auth0.com");
        auth0.GetProperty("clientId").GetString().Should().Be("test-client-id-123");
        auth0.GetProperty("audience").GetString().Should().Be("https://wib-api.example.com");

        json.GetProperty("isTestMode").GetBoolean().Should().BeTrue();
    }
}
