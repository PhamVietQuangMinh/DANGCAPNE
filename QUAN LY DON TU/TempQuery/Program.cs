using Npgsql;
using System;

var connString = "Host=aws-1-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.elnboidlskzruxwvbiks;Password=SQL30042008@@XYZ;SSL Mode=Require;Trust Server Certificate=true;Timeout=30;CommandTimeout=60;Max Auto Prepare=0;";

await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

var checkCmd = new NpgsqlCommand("SELECT \"CreatedAt\", \"Email\", \"Action\", \"Details\", \"IpAddress\" FROM \"AuthAuditLogs\" ORDER BY \"CreatedAt\" DESC LIMIT 10", conn);
await using var reader = await checkCmd.ExecuteReaderAsync();

Console.WriteLine("Recent Auth Logs:");
while (await reader.ReadAsync())
{
    var time = reader.GetDateTime(0);
    var email = reader.IsDBNull(1) ? "N/A" : reader.GetString(1);
    var action = reader.GetString(2);
    var details = reader.IsDBNull(3) ? "N/A" : reader.GetString(3);
    var ip = reader.IsDBNull(4) ? "N/A" : reader.GetString(4);
    Console.WriteLine($"- Time: {time}, User: {email}, Action: {action}, IP: {ip}, Details: {details}");
}
