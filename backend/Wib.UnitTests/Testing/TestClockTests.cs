using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Wib.Api.Testing;

namespace Wib.UnitTests.Testing;

public class TestClockTests : IClassFixture<WibWebApplicationFactory>
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(5);

    private readonly WibWebApplicationFactory _factory;

    public TestClockTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void GetUtcNow_ByDefault_ShouldFollowSystemTime()
    {
        var clock = new TestClock();

        clock.GetUtcNow().Should().BeCloseTo(DateTimeOffset.UtcNow, Tolerance);
    }

    [Fact]
    public void SetUtcNow_ShouldMoveTimeAndKeepItFlowing()
    {
        // Arrange
        var clock = new TestClock();
        var target = DateTimeOffset.UtcNow.AddDays(-186);

        // Act
        clock.SetUtcNow(target);

        // Assert
        var first = clock.GetUtcNow();
        first.Should().BeCloseTo(target, Tolerance);
        clock.GetUtcNow().Should().BeOnOrAfter(first);
    }

    [Fact]
    public void Reset_ShouldReturnToSystemTime()
    {
        // Arrange
        var clock = new TestClock();
        clock.SetUtcNow(DateTimeOffset.UtcNow.AddDays(30));

        // Act
        clock.Reset();

        // Assert
        clock.GetUtcNow().Should().BeCloseTo(DateTimeOffset.UtcNow, Tolerance);
    }

    [Fact]
    public void AddWibClock_InDevelopment_ShouldRegisterTestClockAsTimeProvider()
    {
        // Act
        using var provider = new ServiceCollection()
            .AddWibClock(new FakeHostEnvironment(Environments.Development))
            .BuildServiceProvider();

        // Assert
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(provider.GetRequiredService<TestClock>());
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void AddWibClock_OutsideDevelopment_ShouldRegisterSystemTimeOnly(string environmentName)
    {
        // Act
        using var provider = new ServiceCollection()
            .AddWibClock(new FakeHostEnvironment(environmentName))
            .BuildServiceProvider();

        // Assert
        provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
        provider.GetService<TestClock>().Should().BeNull();
    }

    [Fact]
    public void ClockEndpoints_OutsideDevelopment_ShouldNotBeMapped()
    {
        // Act
        var endpointNames = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .Select(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName)
            .ToList();

        // Assert
        endpointNames.Should().Contain("GetChores");
        endpointNames.Should().NotContain(["SetTestClock", "ResetTestClock"]);
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Wib.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
