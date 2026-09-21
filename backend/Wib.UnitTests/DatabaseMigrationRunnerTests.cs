using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wib.Api.Data;
using Xunit;

namespace Wib.UnitTests;

public class DatabaseMigrationRunnerTests
{
    [Fact]
    public void AddWibDatabase_ShouldRegisterWibDbContextAndMigrator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=wib;User=root;Password=secret;"
            })
            .Build();

        // Act
        services.AddWibDatabase(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<WibDbContext>().Should().NotBeNull();
        provider.GetService<IDatabaseMigrator>().Should().NotBeNull();
    }

    [Fact]
    public void DatabaseMigrator_WhenUsingInMemoryDatabase_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<WibDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        var provider = services.BuildServiceProvider();
        var migrator = provider.GetRequiredService<IDatabaseMigrator>();

        // Act & Assert
        var act = () => migrator.Migrate();
        act.Should().NotThrow();
    }
}
