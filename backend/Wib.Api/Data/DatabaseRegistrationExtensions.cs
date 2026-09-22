using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Wib.Api.Data;

public static class DatabaseRegistrationExtensions
{
    public static readonly ServerVersion DefaultServerVersion = ServerVersion.Parse("12.3.2-mariadb");

    public static IServiceCollection AddWibDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Server=localhost;Database=wib;User=root;Password=secret;";

        services.AddDbContext<WibDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                DefaultServerVersion,
                mysqlOptions =>
                {
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                });
        });

        services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();

        return services;
    }

    public static IHost ApplyDatabaseMigrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
        migrator.Migrate();
        return host;
    }
}
