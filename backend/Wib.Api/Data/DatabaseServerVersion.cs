using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Wib.Api.Data;

public sealed class DatabaseServerVersion
{
    private static readonly ServerVersion FallbackServerVersion = ServerVersion.Parse("12.3.0-mariadb");

    private readonly string _connectionString;
    private readonly ILogger<DatabaseServerVersion> _logger;
    private ServerVersion? _detectedServerVersion;

    public DatabaseServerVersion(string connectionString, ILogger<DatabaseServerVersion> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public ServerVersion Get()
    {
        if (_detectedServerVersion is not null)
        {
            return _detectedServerVersion;
        }

        try
        {
            return _detectedServerVersion = ServerVersion.AutoDetect(_connectionString);
        }
        catch (MySqlException ex)
        {
            _logger.LogWarning(ex, "Could not detect database server version, using {FallbackVersion} until the database is reachable", FallbackServerVersion);
            return FallbackServerVersion;
        }
    }
}
