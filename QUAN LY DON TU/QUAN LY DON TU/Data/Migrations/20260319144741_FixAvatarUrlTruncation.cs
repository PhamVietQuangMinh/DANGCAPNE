using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DANGCAPNE.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAvatarUrlTruncation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(4120));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(4143));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(4149));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(4153));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(4157));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(4161));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(7721));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(7749));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(7756));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(7762));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(7769));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(7775));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8273));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8283));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8289));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8293));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8297));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 6,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8301));

            migrationBuilder.UpdateData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8460));

            migrationBuilder.UpdateData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8470));

            migrationBuilder.UpdateData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8476));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9221));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9229));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9234));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8542));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8645));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8666));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(8677));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9310));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 2,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9315));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 3,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9318));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 4,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9321));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 5,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9324));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 6,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9327));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(3902));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(3934));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(3940));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(3945));

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(3359));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 1,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6634));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 2,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6640));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 3,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6644));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 4,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6646));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 5,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6649));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 6,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6652));

            migrationBuilder.UpdateData(
                table: "UserPermissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "GrantedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(9395));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6530));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 2,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6535));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 3,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6539));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 4,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6542));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 5,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6545));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 6,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6547));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 7,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6550));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 8,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6553));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 9,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6556));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6248), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6246), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6249) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6300), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6298), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6301) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6314), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6313), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6315) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6326), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6325), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6327) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6335), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6334), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6336) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6357), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6356), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6358) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6412), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6383), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6413) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6423), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6422), new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(6424) });

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(7473));

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(7498));

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 21, 47, 38, 534, DateTimeKind.Local).AddTicks(7503));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1120));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1125));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1127));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1129));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1131));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1132));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5195));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5201));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5204));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5206));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5209));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5211));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5434));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5439));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5441));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5443));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5445));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 6,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5447));

            migrationBuilder.UpdateData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5522));

            migrationBuilder.UpdateData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5527));

            migrationBuilder.UpdateData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5529));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5805));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5809));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5811));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5566));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5578));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5587));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5591));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5839));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 2,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5841));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 3,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5843));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 4,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5844));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 5,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5845));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 6,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5846));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1051));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1055));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1058));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(1059));

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 661, DateTimeKind.Local).AddTicks(794));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 1,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4667));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 2,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4671));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 3,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4673));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 4,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4674));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 5,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4675));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 6,
                column: "StartDate",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4677));

            migrationBuilder.UpdateData(
                table: "UserPermissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "GrantedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5874));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4613));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 2,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4616));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 3,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4617));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 4,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4619));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 5,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4620));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 6,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4621));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 7,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4623));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 8,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4624));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 9,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4625));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4415), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4395), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4415) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4438), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4437), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4438) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4443), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4443), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4444) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4448), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4448), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4449) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4453), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4452), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4453) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4457), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4457), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4458) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4462), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4462), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4463) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4467), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4467), new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4467) });

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5112));

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5117));

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(5119));
        }
    }
}
