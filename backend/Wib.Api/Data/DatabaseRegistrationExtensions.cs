using Microsoft.EntityFrameworkCore;

namespace Wib.Api.Data;

public static class DatabaseRegistrationExtensions
{
    private static readonly ServerVersion FallbackServerVersion = ServerVersion.Parse("12.3.0-mariadb");
    private static ServerVersion? _detectedServerVersion;

    public static IServiceCollection AddWibDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<WibDbContext>(options =>
        {
            options.UseMySql(
                connectionString,
                GetServerVersion(connectionString),
                mysqlOptions =>
                {
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(2),
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

    private static ServerVersion GetServerVersion(string connectionString)
    {
        if (_detectedServerVersion is not null)
        {
            return _detectedServerVersion;
        }

        try
        {
            return _detectedServerVersion = ServerVersion.AutoDetect(connectionString);
        }
        catch (Exception)
        {
            return FallbackServerVersion;
        }
    }
}
