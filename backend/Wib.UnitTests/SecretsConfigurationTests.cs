using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Wib.UnitTests;

public class SecretsConfigurationTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;

    public SecretsConfigurationTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Configuration_ShouldIncludeDockerSecretSource()
    {
        var configuration = (IConfigurationRoot)_factory.Services.GetRequiredService<IConfiguration>();
        var jsonProviders = configuration.Providers.OfType<JsonConfigurationProvider>().ToList();

        jsonProviders.Should().Contain(p => 
            p.Source.Path != null &&
            p.Source.Path.EndsWith("appsettings.secret.json") && 
            p.Source.Optional);
    }
}
