using System;
using Npgsql;

var connString = "Host=db.elnboidlskzruxwvbiks.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=SQL30042008@@XYZ;SSL Mode=Require;Trust Server Certificate=true";

await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

const string sql = """
UPDATE "Timesheets"
SET "CheckIn" = date_trunc('day', "CheckIn") + interval '8 hours',
    "WorkHours" = CASE
        WHEN "CheckOut" IS NOT NULL THEN ROUND(GREATEST(EXTRACT(EPOCH FROM ("CheckOut" - (date_trunc('day', "CheckIn") + interval '8 hours'))) / 3600.0, 0)::numeric, 2)
        ELSE "WorkHours"
    END,
    "UpdatedAt" = NOW()
WHERE "CheckIn" IS NOT NULL
  AND "TenantId" = 1;
""";

await using var cmd = new NpgsqlCommand(sql, conn);
var rows = await cmd.ExecuteNonQueryAsync();
Console.WriteLine($"UPDATED_ROWS={rows}");
