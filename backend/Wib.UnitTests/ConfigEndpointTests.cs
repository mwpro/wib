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
                    ["JwtAuth:Authority"] = "https://test-wib.eu.auth0.com/",
                    ["JwtAuth:ClientId"] = "test-client-id-123",
                    ["JwtAuth:Audience"] = "https://wib-api.example.com",
                    ["JwtAuth:BypassAuth"] = "true"
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
    public async Task GetConfig_ShouldReturnJwtAuthSettingsAndTestMode()
    {
        // Arrange
        var client = _factory.CreateClient();

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
