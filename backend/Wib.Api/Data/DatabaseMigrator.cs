using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

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
            MigrateWithCompletionOutcomeBackfill();
            _context.Database.Migrate();
            _logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            _logger.LogInformation("Non-relational database provider detected ({ProviderName}). Ensuring database is created.", _context.Database.ProviderName);
            _context.Database.EnsureCreated();
        }
    }

    // TEMPORARY: remove together with CompletionOutcomeBackfill. See ADR 0005.
    private void MigrateWithCompletionOutcomeBackfill()
    {
        var pending = _context.Database.GetPendingMigrations().ToList();
        if (!pending.Any(m => m.EndsWith("_MakeCompletionOutcomeRequired")))
        {
            return;
        }

        if (pending.Contains(CompletionOutcomeBackfill.RunAfterMigration))
        {
            _context.GetService<IMigrator>().Migrate(CompletionOutcomeBackfill.RunAfterMigration);
        }

        var updated = CompletionOutcomeBackfill.Run(_context);
        _logger.LogInformation("Backfilled outcomes for {Count} chore completions.", updated);
    }
}
