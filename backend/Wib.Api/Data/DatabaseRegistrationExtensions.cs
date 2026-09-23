using Microsoft.EntityFrameworkCore;

namespace Wib.Api.Data;

public static class DatabaseRegistrationExtensions
{
    public static IServiceCollection AddWibDatabase(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<WibDbContext>(options =>
        {
            var version = ServerVersion.AutoDetect(connectionString);

            options.UseMySql(
                connectionString,
                version,
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
