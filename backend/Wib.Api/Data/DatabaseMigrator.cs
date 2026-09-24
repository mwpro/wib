using Microsoft.EntityFrameworkCore;

namespace Wib.Api.Data;

public interface IDatabaseMigrator
{
    void Migrate();
}

public class DatabaseMigrator : IDatabaseMigrator
{
    private readonly WibDbContext _context;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(WibDbContext context, ILogger<DatabaseMigrator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public void Migrate()
    {
        if (_context.Database.IsRelational())
        {
            _logger.LogInformation("Applying database migrations for WibDbContext...");
            _context.Database.Migrate();
            _logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            _logger.LogInformation("Non-relational database provider detected ({ProviderName}). Ensuring database is created.", _context.Database.ProviderName);
            _context.Database.EnsureCreated();
        }
    }
}
