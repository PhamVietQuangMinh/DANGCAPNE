using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DANGCAPNE.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdvBiometricAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FaceDescriptor",
                table: "Users",
                newName: "TrustedDeviceId");

            migrationBuilder.AddColumn<string>(
                name: "FaceDescriptorFront",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaceDescriptorLeft",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaceDescriptorRight",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBiometricEnrolled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PortraitImage",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

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
                columns: new[] { "CreatedAt", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "HireDate", "IsBiometricEnrolled", "PortraitImage", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4415), null, null, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4395), false, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4415) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "HireDate", "IsBiometricEnrolled", "PortraitImage", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4438), null, null, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4437), false, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4438) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "HireDate", "IsBiometricEnrolled", "PortraitImage", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4443), null, null, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4443), false, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4444) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "HireDate", "IsBiometricEnrolled", "PortraitImage", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4448), null, null, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4448), false, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4449) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "HireDate", "IsBiometricEnrolled", "PortraitImage", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4453), null, null, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4452), false, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4453) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "HireDate", "IsBiometricEnrolled", "PortraitImage", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4457), null, null, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4457), false, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4458) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "HireDate", "IsBiometricEnrolled", "PortraitImage", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4462), null, null, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4462), false, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4463) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "HireDate", "IsBiometricEnrolled", "PortraitImage", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4467), null, null, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4467), false, null, new DateTime(2026, 3, 19, 18, 48, 33, 679, DateTimeKind.Local).AddTicks(4467) });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceDescriptorFront",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FaceDescriptorLeft",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FaceDescriptorRight",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsBiometricEnrolled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PortraitImage",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "TrustedDeviceId",
                table: "Users",
                newName: "FaceDescriptor");

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8886));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8889));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8891));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8892));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8893));

            migrationBuilder.UpdateData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8894));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2877));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2880));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2882));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2884));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2885));

            migrationBuilder.UpdateData(
                table: "FormTemplates",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2887));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3027));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3036));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3037));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3039));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 5,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3040));

            migrationBuilder.UpdateData(
                table: "LeaveBalances",
                keyColumn: "Id",
                keyValue: 6,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3041));

            migrationBuilder.UpdateData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3091));

            migrationBuilder.UpdateData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3094));

            migrationBuilder.UpdateData(
                table: "Notifications",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3095));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3271));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3274));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3275));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 1,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3116));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 2,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3124));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 3,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3130));

            migrationBuilder.UpdateData(
                table: "Requests",
                keyColumn: "Id",
                keyValue: 4,
                column: "UpdatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3132));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3294));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 2,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3295));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 3,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3296));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 4,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3296));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 5,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3301));

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 6,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3301));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8820));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8826));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8828));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8829));

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 626, DateTimeKind.Local).AddTicks(8552));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 1,
                column: "StartDate",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2561));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 2,
                column: "StartDate",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2563));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 3,
                column: "StartDate",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2564));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 4,
                column: "StartDate",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2565));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 5,
                column: "StartDate",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2566));

            migrationBuilder.UpdateData(
                table: "UserManagers",
                keyColumn: "Id",
                keyValue: 6,
                column: "StartDate",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2566));

            migrationBuilder.UpdateData(
                table: "UserPermissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "GrantedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(3316));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 1,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2520));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 2,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2522));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 3,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2523));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 4,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2524));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 5,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2534));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 6,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2535));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 7,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2535));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 8,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2536));

            migrationBuilder.UpdateData(
                table: "UserRoles",
                keyColumn: "Id",
                keyValue: 9,
                column: "AssignedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2537));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2440), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2436), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2441) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2449), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2449), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2450) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2453), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2452), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2453) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2455), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2455), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2455) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2457), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2457), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2458) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2460), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2459), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2460) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2462), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2462), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2462) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "HireDate", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2464), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2464), new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2465) });

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2812));

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2816));

            migrationBuilder.UpdateData(
                table: "Workflows",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 3, 18, 8, 24, 23, 627, DateTimeKind.Local).AddTicks(2818));
        }
    }
}
