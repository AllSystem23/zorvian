#r "nuget: Npgsql, 8.0.6"

using Npgsql;

var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__ZorvianDb")
    ?? "Host=localhost;Database=zorvian;Username=postgres";
await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();
await using var cmd = new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename", conn);
await using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
    Console.WriteLine(reader.GetString(0));
