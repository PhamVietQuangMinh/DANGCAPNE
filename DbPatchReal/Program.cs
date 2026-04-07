using System.Text.Json;
using Npgsql;

var appSettingsPath = @"C:\Users\Tien Dat\DANGCAPNE\QUAN LY DON TU\QUAN LY DON TU\appsettings.json";
var json = await File.ReadAllTextAsync(appSettingsPath);
using var doc = JsonDocument.Parse(json);
var connectionString = doc.RootElement
    .GetProperty("ConnectionStrings")
    .GetProperty("DefaultConnection")
    .GetString() ?? throw new InvalidOperationException("Missing connection string.");

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

await using var findLeaveType = new NpgsqlCommand("SELECT \"Id\" FROM \"LeaveTypes\" WHERE \"TenantId\" = 1 AND \"Code\" = 'AL' LIMIT 1;", conn);
var leaveTypeId = Convert.ToInt32(await findLeaveType.ExecuteScalarAsync());

await using var findBalance = new NpgsqlCommand(
    "SELECT \"Id\" FROM \"LeaveBalances\" WHERE \"TenantId\" = 1 AND \"UserId\" = 4 AND \"LeaveTypeId\" = @leaveTypeId AND \"Year\" = 2025 LIMIT 1;",
    conn);
findBalance.Parameters.AddWithValue("leaveTypeId", leaveTypeId);
var existingId = await findBalance.ExecuteScalarAsync();

if (existingId == null || existingId == DBNull.Value)
{
    await using var insert = new NpgsqlCommand(
        "INSERT INTO \"LeaveBalances\" (\"TenantId\", \"UserId\", \"LeaveTypeId\", \"Year\", \"TotalEntitled\", \"Used\", \"CarriedOver\", \"SeniorityBonus\", \"CompensatoryDays\", \"UpdatedAt\") VALUES (1, 4, @leaveTypeId, 2025, 12, 9, 0, 0, 0, NOW());",
        conn);
    insert.Parameters.AddWithValue("leaveTypeId", leaveTypeId);
    await insert.ExecuteNonQueryAsync();
}
else
{
    await using var update = new NpgsqlCommand(
        "UPDATE \"LeaveBalances\" SET \"TotalEntitled\" = 12, \"Used\" = 9, \"CarriedOver\" = 0, \"SeniorityBonus\" = 0, \"CompensatoryDays\" = 0, \"UpdatedAt\" = NOW() WHERE \"Id\" = @id;",
        conn);
    update.Parameters.AddWithValue("id", Convert.ToInt32(existingId));
    await update.ExecuteNonQueryAsync();
}

await using var verify = new NpgsqlCommand(
    "SELECT (\"TotalEntitled\" + \"CarriedOver\" + \"SeniorityBonus\" + \"CompensatoryDays\" - \"Used\") AS remaining FROM \"LeaveBalances\" WHERE \"TenantId\" = 1 AND \"UserId\" = 4 AND \"LeaveTypeId\" = @leaveTypeId AND \"Year\" = 2025 LIMIT 1;",
    conn);
verify.Parameters.AddWithValue("leaveTypeId", leaveTypeId);
var remaining = await verify.ExecuteScalarAsync();
Console.WriteLine($"Updated real DB: UserId=4, Year=2025, Remaining={remaining}");
