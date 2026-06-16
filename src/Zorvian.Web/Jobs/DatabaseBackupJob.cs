using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace Zorvian.Web.Jobs;

public sealed class DatabaseBackupJob
{
    private readonly ILogger<DatabaseBackupJob> _logger;
    private readonly string _connectionString;
    private static readonly string BackupDir = Path.Combine(Path.GetTempPath(), "zorvian-backups");

    public DatabaseBackupJob(IConfiguration config, ILogger<DatabaseBackupJob> logger)
    {
        _logger = logger;
        _connectionString = config.GetConnectionString("ZorvianDb") ?? "";
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Starting database backup");

        if (!IsPgDumpAvailable())
        {
            _logger.LogWarning("pg_dump not found — skipping backup (Render provides automatic backups)");
            return;
        }

        try
        {
            Directory.CreateDirectory(BackupDir);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var outputFile = Path.Combine(BackupDir, $"zorvian-backup-{timestamp}.sql");

            var parsed = ParseConnectionString(_connectionString);
            if (parsed is null)
            {
                _logger.LogWarning("Cannot parse connection string for backup — skipping");
                return;
            }

            var (host, port, db, user, password) = parsed.Value;

            var psi = new ProcessStartInfo
            {
                FileName = "pg_dump",
                ArgumentList =
                {
                    $"--host={host}",
                    $"--port={port}",
                    $"--dbname={db}",
                    $"--username={user}",
                    "--format=custom",
                    "--verbose",
                    $"--file={outputFile}",
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            psi.EnvironmentVariables["PGPASSWORD"] = password;

            using var process = Process.Start(psi);
            if (process is null)
            {
                _logger.LogError("Failed to start pg_dump process");
                return;
            }

            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("pg_dump failed (exit {ExitCode}): {Error}", process.ExitCode, stderr);
                return;
            }

            var fileInfo = new FileInfo(outputFile);
            _logger.LogInformation("Backup completed: {File} ({Size} bytes)", outputFile, fileInfo.Length);

            // Cleanup old backups (keep last 7)
            var existing = Directory.GetFiles(BackupDir, "zorvian-backup-*.sql")
                .OrderByDescending(f => f)
                .ToList();

            if (existing.Count > 7)
            {
                foreach (var old in existing.Skip(7))
                {
                    File.Delete(old);
                    _logger.LogInformation("Deleted old backup: {File}", old);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup failed");
        }
    }

    private static bool IsPgDumpAvailable()
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "pg_dump",
                ArgumentList = { "--version" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (proc is null) return false;
            proc.WaitForExit(3000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static (string host, string port, string db, string user, string password)? ParseConnectionString(string cs)
    {
        try
        {
            var parts = cs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string host = "", port = "5432", db = "", user = "", password = "";

            foreach (var part in parts)
            {
                var eq = part.IndexOf('=', StringComparison.Ordinal);
                if (eq < 0) continue;

                var key = part[..eq].Trim().ToLowerInvariant();
                var val = part[(eq + 1)..].Trim();

                switch (key)
                {
                    case "host": host = val; break;
                    case "port": port = val; break;
                    case "database": db = val; break;
                    case "username": user = val; break;
                    case "password": password = val; break;
                }
            }

            return string.IsNullOrEmpty(host) ? null : (host, port, db, user, password);
        }
        catch
        {
            return null;
        }
    }
}
