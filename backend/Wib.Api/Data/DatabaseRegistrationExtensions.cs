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

        services.AddSingleton(serviceProvider => new DatabaseServerVersion(
            connectionString,
            serviceProvider.GetRequiredService<ILogger<DatabaseServerVersion>>()));

        services.AddDbContext<WibDbContext>((serviceProvider, options) =>
        {
            options.UseMySql(
                connectionString,
                serviceProvider.GetRequiredService<DatabaseServerVersion>().Get(),
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
}
