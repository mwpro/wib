using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wib.Api.Data;

namespace Wib.UnitTests;

public class WibWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "WibTestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtAuth:Authority"] = "https://identity.test.example/",
                ["JwtAuth:ClientId"] = "test-client",
                ["JwtAuth:Audience"] = "https://api.test",
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

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<WibDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName)
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                       .UseInternalServiceProvider(inMemoryProvider);
            });
            services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
        });
    }

    public async Task ExecuteDbContextAsync(Func<WibDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
        await action(db);
    }

    /// <summary>
    /// Subject id of the member that <see cref="CreateAuthenticatedClient"/> authenticates as by default.
    /// </summary>
    public const string DefaultTestSub = "test-user";

    /// <summary>
    /// A client authenticated through the test auth handler; the member is provisioned on first request.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string sub = DefaultTestSub, string name = "Test User")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", sub);
        client.DefaultRequestHeaders.Add("X-Test-User-Name", name);
        return client;
    }

    /// <summary>
    /// Adds and saves the entities, returning them with database-generated ids filled in.
    /// </summary>
    public async Task<T> SeedAsync<T>(T entity) where T : class => (await SeedAsync([entity]))[0];

    /// <inheritdoc cref="SeedAsync{T}(T)"/>
    public async Task<T[]> SeedAsync<T>(params T[] entities) where T : class
    {
        await ExecuteDbContextAsync(async db =>
        {
            db.AddRange(entities);
            await db.SaveChangesAsync();
        });
        return entities;
    }
}
