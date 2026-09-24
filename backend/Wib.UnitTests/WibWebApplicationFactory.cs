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
                ["JwtAuth:Authority"] = "https://test.eu.auth0.com/",
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

    public async Task<T> ExecuteDbContextAsync<T>(Func<WibDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WibDbContext>();
        return await action(db);
    }
}
