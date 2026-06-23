using Npgsql;

var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__ZorvianDb")
    ?? "Host=localhost;Database=zorvian;Username=postgres";
await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();

Console.WriteLine("Removing AddCompanyIdToBaseEntity from history...");
await using (var cmd = new NpgsqlCommand(@"DELETE FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260623215834_AddCompanyIdToBaseEntity'", conn))
{
    var rows = await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"  Rows affected: {rows}");
}

Console.WriteLine("\n=== Remaining migrations ===");
await using (var cmd = new NpgsqlCommand("SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\"", conn))
{
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        Console.WriteLine($"  {reader.GetString(0)}");
}
