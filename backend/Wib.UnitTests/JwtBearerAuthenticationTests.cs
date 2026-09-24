using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Wib.Api.Data;

namespace Wib.UnitTests;

public class JwtBearerAuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly RsaSecurityKey _securityKey;
    private const string Issuer = "https://test-auth0.eu.auth0.com/";
    private const string Audience = "https://wib-api.example.com";

    public JwtBearerAuthenticationTests(WebApplicationFactory<Program> factory)
    {
        var rsa = RSA.Create(2048);
        _securityKey = new RsaSecurityKey(rsa);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Testing:BypassAuth"] = "false", // Real JWT Bearer mode!
                    ["JwtAuth:Authority"] = Issuer,
                    ["JwtAuth:Audience"] = Audience
                });
            });

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = Issuer,
                        ValidateAudience = true,
                        ValidAudience = Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = _securityKey,
                        ValidateLifetime = true
                    };
                });
            });
        });
    }

    private string GenerateJwtToken(string sub, string name, string email, string picture)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", sub),
                new Claim("name", name),
                new Claim("email", email),
                new Claim("picture", picture)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.RsaSha256)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task RequestWithInvalidJwt_ShouldReturnUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token-string");

        // Act
        var response = await client.GetAsync("/api/members/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestWithValidJwt_ShouldAuthenticateAndAutoProvisionMember()
    {
        // Arrange
        var client = _factory.CreateClient();
        var uniqueSub = "auth0|jwt-user-" + Guid.NewGuid();
        var token = GenerateJwtToken(uniqueSub, "JWT Auto Provisioned", "jwt-provisioned@example.com", "https://example.com/avatar.jpg");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/members/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("auth0UserId").GetString().Should().Be(uniqueSub);
        json.GetProperty("name").GetString().Should().Be("JWT Auto Provisioned");
        json.GetProperty("email").GetString().Should().Be("jwt-provisioned@example.com");
        json.GetProperty("picture").GetString().Should().Be("https://example.com/avatar.jpg");

        // Verify record in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
        var member = await db.Members.FirstOrDefaultAsync(m => m.Auth0UserId == uniqueSub);
        member.Should().NotBeNull();
        member!.Name.Should().Be("JWT Auto Provisioned");
        member.Email.Should().Be("jwt-provisioned@example.com");
        member.Picture.Should().Be("https://example.com/avatar.jpg");
    }
}
