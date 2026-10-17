using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wib.Api.Data;

namespace Wib.UnitTests;

public class UtcDateTimeConventionTests : IClassFixture<WibWebApplicationFactory>
{
    private readonly WibWebApplicationFactory _factory;

    public UtcDateTimeConventionTests(WibWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // MySQL drops DateTimeKind, so without the converter timestamps read back as Unspecified
    // and serialize without the trailing "Z" that the frontend relies on to parse them as UTC.
    [Fact]
    public void EveryDateTimeProperty_ShouldBeReadBackAsUtc()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();

        var dateTimeProperties = db.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
            .ToList();

        dateTimeProperties.Should().NotBeEmpty();
        dateTimeProperties.Should().OnlyContain(
            p => p.GetValueConverter() is UtcDateTimeConverter || p.GetValueConverter() is NullableUtcDateTimeConverter,
            "every timestamp must be materialized with DateTimeKind.Utc");
    }
}
