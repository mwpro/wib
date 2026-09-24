using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Wib.UnitTests;

public class ConfigEndpointTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;

    public ConfigEndpointTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetConfig_ShouldReturnJwtAuthSettingsAndTestMode()
    {
        // Arrange
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtAuth:Authority"] = "https://test-wib.eu.auth0.com/",
                    ["JwtAuth:ClientId"] = "test-client-id-123",
                    ["JwtAuth:Audience"] = "https://wib-api.example.com",
                    ["JwtAuth:BypassAuth"] = "true"
                });
            });
        });
        var client = customFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/config");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var jwtAuth = json.GetProperty("jwtAuth");
        jwtAuth.GetProperty("authority").GetString().Should().Be("https://test-wib.eu.auth0.com/");
        jwtAuth.GetProperty("domain").GetString().Should().Be("test-wib.eu.auth0.com");
        jwtAuth.GetProperty("clientId").GetString().Should().Be("test-client-id-123");
        jwtAuth.GetProperty("audience").GetString().Should().Be("https://wib-api.example.com");

        json.GetProperty("isTestMode").GetBoolean().Should().BeTrue();
    }
}
