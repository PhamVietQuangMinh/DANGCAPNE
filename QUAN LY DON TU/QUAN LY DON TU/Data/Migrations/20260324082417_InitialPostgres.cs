using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DANGCAPNE.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    AllowedRadius = table.Column<double>(type: "double precision", nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Candidates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ShiftId = table.Column<int>(type: "integer", nullable: true),
                    ActualCheckIn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ActualCheckOut = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    IsLate = table.Column<bool>(type: "boolean", nullable: false),
                    LateMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsEarlyLeave = table.Column<bool>(type: "boolean", nullable: false),
                    EarlyLeaveMinutes = table.Column<int>(type: "integer", nullable: false),
                    EffectiveHours = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HasApprovedLeave = table.Column<bool>(type: "boolean", nullable: false),
                    HasApprovedOutside = table.Column<bool>(type: "boolean", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyAttendances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ToEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RelatedRequestId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    RequiresReceipt = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    Country = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveAccruals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    Days = table.Column<double>(type: "double precision", nullable: false),
                    AccrualType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccrualDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    RelatedRequestId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveAccruals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefaultDaysPerYear = table.Column<double>(type: "double precision", nullable: false),
                    AllowCarryOver = table.Column<bool>(type: "boolean", nullable: false),
                    CarryOverMaxDays = table.Column<int>(type: "integer", nullable: false),
                    CarryOverExpiryMonth = table.Column<int>(type: "integer", nullable: false),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    AllowNegativeBalance = table.Column<bool>(type: "boolean", nullable: false),
                    IconColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OvertimeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Multiplier = table.Column<double>(type: "double precision", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvertimeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BreakStartTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    BreakEndTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    GracePeriodMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemErrors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubDomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Plan = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingCourses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingCourses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLocationConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: false),
                    WifiName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WifiBssid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QrCodeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AllowedLatitude = table.Column<double>(type: "double precision", nullable: true),
                    AllowedLongitude = table.Column<double>(type: "double precision", nullable: true),
                    AllowedRadiusMeters = table.Column<int>(type: "integer", nullable: false),
                    RequirePhoto = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLocationConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceLocationConfigs_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    FromCurrencyId = table.Column<int>(type: "integer", nullable: false),
                    ToCurrencyId = table.Column<int>(type: "integer", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_FromCurrencyId",
                        column: x => x.FromCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_ToCurrencyId",
                        column: x => x.ToCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OffboardingTaskTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DefaultDueDays = table.Column<int>(type: "integer", nullable: false),
                    DefaultAssigneeRoleId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffboardingTaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffboardingTaskTemplates_Roles_DefaultAssigneeRoleId",
                        column: x => x.DefaultAssigneeRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OnboardingTaskTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DefaultDueDays = table.Column<int>(type: "integer", nullable: false),
                    DefaultAssigneeRoleId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingTaskTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingTaskTemplates_Roles_DefaultAssigneeRoleId",
                        column: x => x.DefaultAssigneeRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    PermissionId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Plan = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastPaymentDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantConfigs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IconColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WorkflowId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresFinancialApproval = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormTemplates_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    ApproverType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ApproverUserId = table.Column<int>(type: "integer", nullable: true),
                    ApproverRoleId = table.Column<int>(type: "integer", nullable: true),
                    CanSkipIfApplicant = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FormTemplateId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FieldType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Placeholder = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValidationRules = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DefaultValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFields_FormTemplates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlaConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    FormTemplateId = table.Column<int>(type: "integer", nullable: true),
                    ReminderHours = table.Column<int>(type: "integer", nullable: false),
                    EscalationHours = table.Column<int>(type: "integer", nullable: false),
                    AutoRemind = table.Column<bool>(type: "boolean", nullable: false),
                    AutoEscalate = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaConfigs_FormTemplates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowStepId = table.Column<int>(type: "integer", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Operator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NextStepId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowConditions_WorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "WorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStepApprovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowStepId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    RoleId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStepApprovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowStepApprovers_WorkflowSteps_WorkflowStepId",
                        column: x => x.WorkflowStepId,
                        principalTable: "WorkflowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FormFieldOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FormFieldId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFieldOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFieldOptions_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssetIncidents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<int>(type: "integer", nullable: false),
                    ReportedByUserId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    AssetCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AssignedToUserId = table.Column<int>(type: "integer", nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_AssetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "AssetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BonusRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateId = table.Column<int>(type: "integer", nullable: false),
                    JobRequisitionId = table.Column<int>(type: "integer", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateApplications_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfferLetters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateApplicationId = table.Column<int>(type: "integer", nullable: false),
                    OfferedSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferLetters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferLetters_CandidateApplications_CandidateApplicationId",
                        column: x => x.CandidateApplicationId,
                        principalTable: "CandidateApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    PickupLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DriverName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CertificationRenewals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CertificationId = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificationRenewals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Certifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IssuedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Certifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Delegations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    DelegatorId = table.Column<int>(type: "integer", nullable: false),
                    DelegateId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Delegations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ParentDepartmentId = table.Column<int>(type: "integer", nullable: true),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AvatarUrl = table.Column<string>(type: "text", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: true),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    JobTitleId = table.Column<int>(type: "integer", nullable: true),
                    PositionId = table.Column<int>(type: "integer", nullable: true),
                    HireDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PinHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsBiometricEnrolled = table.Column<bool>(type: "boolean", nullable: false),
                    FaceDescriptorFront = table.Column<string>(type: "text", nullable: true),
                    FaceDescriptorLeft = table.Column<string>(type: "text", nullable: true),
                    FaceDescriptorRight = table.Column<string>(type: "text", nullable: true),
                    PortraitImage = table.Column<string>(type: "text", nullable: true),
                    TrustedDeviceId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DraftRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FormTemplateId = table.Column<int>(type: "integer", nullable: false),
                    FormDataJson = table.Column<string>(type: "text", nullable: false),
                    LastSavedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DraftRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DraftRequests_FormTemplates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DraftRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DocumentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EscalationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    SlaConfigId = table.Column<int>(type: "integer", nullable: false),
                    EscalateToUserId = table.Column<int>(type: "integer", nullable: false),
                    EscalateAfterHours = table.Column<int>(type: "integer", nullable: false),
                    NotificationMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationRules_SlaConfigs_SlaConfigId",
                        column: x => x.SlaConfigId,
                        principalTable: "SlaConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EscalationRules_Users_EscalateToUserId",
                        column: x => x.EscalateToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterviewSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateApplicationId = table.Column<int>(type: "integer", nullable: false),
                    InterviewerId = table.Column<int>(type: "integer", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewSchedules_CandidateApplications_CandidateApplicati~",
                        column: x => x.CandidateApplicationId,
                        principalTable: "CandidateApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterviewSchedules_Users_InterviewerId",
                        column: x => x.InterviewerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobRequisitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: true),
                    JobTitleId = table.Column<int>(type: "integer", nullable: true),
                    Headcount = table.Column<int>(type: "integer", nullable: false),
                    BudgetMin = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BudgetMax = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRequisitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobRequisitions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobRequisitions_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobRequisitions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    TotalEntitled = table.Column<double>(type: "double precision", nullable: false),
                    Used = table.Column<double>(type: "double precision", nullable: false),
                    CarriedOver = table.Column<double>(type: "double precision", nullable: false),
                    SeniorityBonus = table.Column<double>(type: "double precision", nullable: false),
                    CompensatoryDays = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MealRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    MealType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MealRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MealRegistrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RelatedRequestId = table.Column<int>(type: "integer", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OffboardingTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedToUserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffboardingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OffboardingTasks_OffboardingTaskTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "OffboardingTaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OffboardingTasks_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OffboardingTasks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedToUserId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_OnboardingTaskTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "OnboardingTaskTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnboardingTasks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CycleId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceGoals_PerformanceCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "PerformanceCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerformanceGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CycleId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ReviewerId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceReviews_PerformanceCycles_CycleId",
                        column: x => x.CycleId,
                        principalTable: "PerformanceCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerformanceReviews_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PerformanceReviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PolicyAcknowledgements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PolicyDocumentId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyAcknowledgements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyAcknowledgements_PolicyDocuments_PolicyDocumentId",
                        column: x => x.PolicyDocumentId,
                        principalTable: "PolicyDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PolicyAcknowledgements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ManagerId = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Budget = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OtCost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    RequestCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FormTemplateId = table.Column<int>(type: "integer", nullable: false),
                    RequesterId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentStepOrder = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requests_FormTemplates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "FormTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requests_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryAdjustmentRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ProposedSalary = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryAdjustmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryAdjustmentRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalaryAdjustmentRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShiftSwapRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    RequesterId = table.Column<int>(type: "integer", nullable: false),
                    TargetUserId = table.Column<int>(type: "integer", nullable: false),
                    RequesterShiftId = table.Column<int>(type: "integer", nullable: false),
                    RequesterDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TargetShiftId = table.Column<int>(type: "integer", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ApprovedByManagerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SocialInsurances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    InsuranceNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SalaryBasis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SocialInsurances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialInsurances_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LeaderId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Users_LeaderId",
                        column: x => x.LeaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Timesheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CheckIn = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CheckOut = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GpsLatitude = table.Column<double>(type: "double precision", nullable: true),
                    GpsLongitude = table.Column<double>(type: "double precision", nullable: true),
                    WifiName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WifiBssid = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    QrCodeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WorkHours = table.Column<double>(type: "double precision", nullable: false),
                    OtHours = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsValidGps = table.Column<bool>(type: "boolean", nullable: false),
                    IsValidWifi = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Timesheets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingEnrollments_TrainingCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "TrainingCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingEnrollments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UniformRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniformRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UniformRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserManagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ManagerId = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserManagers_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserManagers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PermissionId = table.Column<int>(type: "integer", nullable: false),
                    GrantedByUserId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserShifts_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserShifts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobRequisitionApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobRequisitionId = table.Column<int>(type: "integer", nullable: false),
                    ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Comments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRequisitionApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobRequisitionApprovals_JobRequisitions_JobRequisitionId",
                        column: x => x.JobRequisitionId,
                        principalTable: "JobRequisitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobRequisitionApprovals_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PerformanceReviewItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReviewId = table.Column<int>(type: "integer", nullable: false),
                    GoalId = table.Column<int>(type: "integer", nullable: true),
                    Score = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceReviewItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceReviewItems_PerformanceGoals_GoalId",
                        column: x => x.GoalId,
                        principalTable: "PerformanceGoals",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PerformanceReviewItems_PerformanceReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "PerformanceReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    StepName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApproverId = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ActionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    VerifiedByPin = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestApprovals_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestApprovals_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UploadedById = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestAttachments_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Details = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestAuditLogs_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ParentCommentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestComments_RequestComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "RequestComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RequestComments_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    FieldKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FieldValue = table.Column<string>(type: "text", nullable: true),
                    FieldType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestData_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestFollowers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestFollowers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestFollowers_Requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "Requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AssetCategories",
                columns: new[] { "Id", "Description", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, "Máy tính xách tay", "Laptop", 1 },
                    { 2, "Màn hình máy tính", "Màn hình", 1 },
                    { 3, "Điện thoại công ty", "Điện thoại", 1 },
                    { 4, "Bàn ghế văn phòng", "Bàn ghế", 1 }
                });

            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "Id", "Address", "AllowedRadius", "IsActive", "Latitude", "Longitude", "Name", "TenantId", "TimeZone" },
                values: new object[,]
                {
                    { 1, "123 Nguyễn Huệ, Quận 1, TP.HCM", 200.0, true, 10.776899999999999, 106.7009, "Trụ sở chính - TP.HCM", 1, "SE Asia Standard Time" },
                    { 2, "456 Hoàn Kiếm, Hà Nội", 200.0, true, 21.028500000000001, 105.85420000000001, "Chi nhánh Hà Nội", 1, "SE Asia Standard Time" }
                });

            migrationBuilder.InsertData(
                table: "Candidates",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "Phone", "Source", "TenantId" },
                values: new object[] { 1, new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "candidate@demo.com", "Tran Thi Candidate", "0909000001", "Referral", 1 });

            migrationBuilder.InsertData(
                table: "Currencies",
                columns: new[] { "Id", "Code", "IsDefault", "Name", "Symbol" },
                values: new object[,]
                {
                    { 1, "VND", true, "Việt Nam Đồng", "₫" },
                    { 2, "USD", false, "US Dollar", "$" },
                    { 3, "EUR", false, "Euro", "€" },
                    { 4, "JPY", false, "Japanese Yen", "¥" }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "CreatedAt", "EnglishName", "IsActive", "ManagerId", "Name", "ParentDepartmentId", "TenantId" },
                values: new object[,]
                {
                    { 1, "BOD", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3620), null, true, null, "Ban Giám đốc", null, 1 },
                    { 2, "IT", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3623), null, true, null, "Phòng Công nghệ Thông tin", null, 1 },
                    { 3, "HR", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3624), null, true, null, "Phòng Nhân sự", null, 1 },
                    { 4, "ACC", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3625), null, true, null, "Phòng Kế toán", null, 1 },
                    { 5, "SALES", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3626), null, true, null, "Phòng Kinh doanh", null, 1 },
                    { 6, "MKT", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3627), null, true, null, "Phòng Marketing", null, 1 }
                });

            migrationBuilder.InsertData(
                table: "EmailTemplates",
                columns: new[] { "Id", "BodyHtml", "IsActive", "Name", "Subject", "TenantId" },
                values: new object[,]
                {
                    { 1, "<h2>Xin chào {{ApproverName}},</h2><p>Bạn có một đơn mới cần xử lý từ <strong>{{RequesterName}}</strong>.</p><p>Loại đơn: {{FormName}}</p><p>Mã đơn: {{RequestCode}}</p><a href='{{ActionUrl}}'>Xem chi tiết</a>", true, "NewRequest", "[{{CompanyName}}] Đơn mới cần duyệt: {{RequestCode}}", 1 },
                    { 2, "<h2>Xin chào {{RequesterName}},</h2><p>Đơn <strong>{{RequestCode}}</strong> của bạn đã được <strong>phê duyệt</strong> bởi {{ApproverName}}.</p>", true, "Approved", "[{{CompanyName}}] Đơn {{RequestCode}} đã được duyệt", 1 },
                    { 3, "<h2>Xin chào {{RequesterName}},</h2><p>Đơn <strong>{{RequestCode}}</strong> của bạn đã bị <strong>từ chối</strong> bởi {{ApproverName}}.</p><p>Lý do: {{Comments}}</p>", true, "Rejected", "[{{CompanyName}}] Đơn {{RequestCode}} bị từ chối", 1 },
                    { 4, "<h2>Xin chào {{ApproverName}},</h2><p>Đơn <strong>{{RequestCode}}</strong> đã chờ duyệt hơn {{Hours}} giờ. Vui lòng xử lý sớm.</p>", true, "Reminder", "[{{CompanyName}}] Nhắc nhở: Đơn {{RequestCode}} chưa được xử lý", 1 }
                });

            migrationBuilder.InsertData(
                table: "ExpenseCategories",
                columns: new[] { "Id", "Code", "IsActive", "MaxAmount", "Name", "RequiresReceipt", "TenantId" },
                values: new object[,]
                {
                    { 1, "TAXI", true, null, "Tiền taxi/xe", true, 1 },
                    { 2, "HOTEL", true, null, "Tiền khách sạn", true, 1 },
                    { 3, "MEAL", true, 500000m, "Tiền ăn uống", false, 1 },
                    { 4, "OTHER", true, null, "Chi phí khác", true, 1 }
                });

            migrationBuilder.InsertData(
                table: "Holidays",
                columns: new[] { "Id", "Country", "Date", "IsRecurring", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, "VN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Tết Dương lịch", 1 },
                    { 2, "VN", new DateTime(2026, 4, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Giỗ Tổ Hùng Vương", 1 },
                    { 3, "VN", new DateTime(2026, 4, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Ngày Thống nhất", 1 },
                    { 4, "VN", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Quốc tế Lao động", 1 },
                    { 5, "VN", new DateTime(2026, 9, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Quốc khánh", 1 }
                });

            migrationBuilder.InsertData(
                table: "JobTitles",
                columns: new[] { "Id", "IsActive", "Level", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, true, 5, "Giám đốc", 1 },
                    { 2, true, 4, "Phó Giám đốc", 1 },
                    { 3, true, 3, "Trưởng phòng", 1 },
                    { 4, true, 3, "Phó phòng", 1 },
                    { 5, true, 2, "Chuyên viên", 1 },
                    { 6, true, 1, "Nhân viên", 1 },
                    { 7, true, 0, "Thực tập sinh", 1 }
                });

            migrationBuilder.InsertData(
                table: "LeaveTypes",
                columns: new[] { "Id", "AllowCarryOver", "AllowNegativeBalance", "CarryOverExpiryMonth", "CarryOverMaxDays", "Code", "DefaultDaysPerYear", "IconColor", "IsActive", "IsPaid", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, true, false, 3, 5, "AL", 12.0, "#10b981", true, true, "Phép năm", 1 },
                    { 2, false, false, 3, 0, "SL", 30.0, "#f59e0b", true, true, "Nghỉ ốm", 1 },
                    { 3, false, false, 3, 0, "ML", 180.0, "#ec4899", true, true, "Nghỉ thai sản", 1 },
                    { 4, false, true, 3, 0, "UL", 365.0, "#6b7280", true, false, "Nghỉ không lương", 1 },
                    { 5, false, false, 3, 0, "CO", 0.0, "#3b82f6", true, true, "Nghỉ bù (Comp Off)", 1 }
                });

            migrationBuilder.InsertData(
                table: "OvertimeRates",
                columns: new[] { "Id", "Description", "IsActive", "Multiplier", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, "OT ngày thường x1.5", true, 1.5, "Ngày thường", 1 },
                    { 2, "OT cuối tuần x2.0", true, 2.0, "Cuối tuần", 1 },
                    { 3, "OT ngày lễ x3.0", true, 3.0, "Ngày lễ", 1 }
                });

            migrationBuilder.InsertData(
                table: "PerformanceCycles",
                columns: new[] { "Id", "CreatedAt", "EndDate", "Name", "StartDate", "Status", "TenantId" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 6, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), "2026 H1 Review", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Open", 1 });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, "REQUEST_CREATE", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8472), "Create request", true, "Create Request", 1 },
                    { 2, "REQUEST_APPROVE", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8474), "Approve request", true, "Approve Request", 1 },
                    { 3, "SYSTEM_ADMIN", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8475), "System administration", true, "System Admin", 1 }
                });

            migrationBuilder.InsertData(
                table: "PolicyDocuments",
                columns: new[] { "Id", "FileUrl", "IsActive", "PublishedAt", "TenantId", "Title", "Version" },
                values: new object[] { 1, "/docs/handbook.pdf", true, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Employee Handbook", "1.0" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3572), "Quản trị viên hệ thống", "Admin", 1 },
                    { 2, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3576), "Hành chính Nhân sự", "HR", 1 },
                    { 3, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3577), "Quản lý", "Manager", 1 },
                    { 4, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3578), "Nhân viên", "Employee", 1 }
                });

            migrationBuilder.InsertData(
                table: "Shifts",
                columns: new[] { "Id", "BreakEndTime", "BreakStartTime", "Code", "EndTime", "GracePeriodMinutes", "IsActive", "Name", "StartTime", "TenantId" },
                values: new object[,]
                {
                    { 1, new TimeSpan(0, 13, 0, 0, 0), new TimeSpan(0, 12, 0, 0, 0), "HC", new TimeSpan(0, 17, 0, 0, 0), 15, true, "Ca hành chính", new TimeSpan(0, 8, 0, 0, 0), 1 },
                    { 2, null, null, "S", new TimeSpan(0, 14, 0, 0, 0), 10, true, "Ca sáng", new TimeSpan(0, 6, 0, 0, 0), 1 },
                    { 3, null, null, "C", new TimeSpan(0, 22, 0, 0, 0), 10, true, "Ca chiều", new TimeSpan(0, 14, 0, 0, 0), 1 },
                    { 4, null, null, "D", new TimeSpan(0, 6, 0, 0, 0), 10, true, "Ca đêm", new TimeSpan(0, 22, 0, 0, 0), 1 }
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "CompanyName", "CreatedAt", "ExpiresAt", "IsActive", "LogoUrl", "MaxUsers", "Plan", "PrimaryColor", "SecondaryColor", "SubDomain" },
                values: new object[] { 1, "DANGCAPNE Corporation", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(3403), null, true, "", 500, "Enterprise", "#6366f1", "#8b5cf6", "dangcapne" });

            migrationBuilder.InsertData(
                table: "TrainingCourses",
                columns: new[] { "Id", "Cost", "EndDate", "IsActive", "Name", "Provider", "StartDate", "TenantId" },
                values: new object[] { 1, 500000m, new DateTime(2026, 3, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), true, "Advanced Excel", "Internal", new DateTime(2026, 3, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 1 });

            migrationBuilder.InsertData(
                table: "Workflows",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8068), "Quản lý trực tiếp -> HR", true, "Luồng duyệt cơ bản", 1 },
                    { 2, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8070), "Quản lý -> Kế toán -> Giám đốc", true, "Luồng duyệt tài chính", 1 },
                    { 3, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8071), "Quản lý -> Trưởng phòng -> HR -> Giám đốc", true, "Luồng duyệt vượt cấp", 1 }
                });

            migrationBuilder.InsertData(
                table: "Assets",
                columns: new[] { "Id", "AssetCode", "AssignedDate", "AssignedToUserId", "CategoryId", "Name", "PurchaseDate", "PurchasePrice", "SerialNumber", "Status", "TenantId" },
                values: new object[] { 1, "AST-001", null, null, 1, "Laptop Dell", new DateTime(2025, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 15000000m, null, "Available", 1 });

            migrationBuilder.InsertData(
                table: "FormTemplates",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "EnglishName", "Icon", "IconColor", "IsActive", "Name", "RequiresFinancialApproval", "TenantId", "WorkflowId" },
                values: new object[,]
                {
                    { 1, "Leave", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8119), "", null, "bi-calendar-x", "#10b981", true, "Đơn xin nghỉ phép", false, 1, 1 },
                    { 2, "OT", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8122), "", null, "bi-clock-history", "#f59e0b", true, "Đơn làm thêm giờ (OT)", false, 1, 1 },
                    { 3, "Travel", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8124), "", null, "bi-airplane", "#3b82f6", true, "Đơn đi công tác", false, 1, 1 },
                    { 4, "Expense", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8125), "", null, "bi-cash-stack", "#ef4444", true, "Đơn tạm ứng chi phí", true, 1, 2 },
                    { 5, "Equipment", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8127), "", null, "bi-laptop", "#8b5cf6", true, "Đơn yêu cầu cấp phát thiết bị", false, 1, 1 },
                    { 6, "Leave", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8128), "", null, "bi-box-arrow-right", "#dc2626", true, "Đơn xin nghỉ việc", false, 1, 3 }
                });

            migrationBuilder.InsertData(
                table: "OffboardingTaskTemplates",
                columns: new[] { "Id", "DefaultAssigneeRoleId", "DefaultDueDays", "Description", "Name", "TenantId" },
                values: new object[] { 1, 2, 2, "Collect laptop and badge", "Return Assets", 1 });

            migrationBuilder.InsertData(
                table: "OnboardingTaskTemplates",
                columns: new[] { "Id", "DefaultAssigneeRoleId", "DefaultDueDays", "Description", "Name", "TenantId" },
                values: new object[] { 1, 2, 3, "Prepare laptop and account", "Laptop Setup", 1 });

            migrationBuilder.InsertData(
                table: "Positions",
                columns: new[] { "Id", "DepartmentId", "IsActive", "Name", "TenantId" },
                values: new object[,]
                {
                    { 1, 1, true, "Giám đốc điều hành", 1 },
                    { 2, 2, true, "Trưởng phòng IT", 1 },
                    { 3, 3, true, "Trưởng phòng HR", 1 },
                    { 4, 4, true, "Kế toán trưởng", 1 },
                    { 5, 5, true, "Trưởng phòng KD", 1 }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "AssignedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8494), 1, 4 },
                    { 2, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8495), 2, 3 },
                    { 3, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8496), 1, 1 },
                    { 4, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8496), 2, 1 },
                    { 5, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8497), 3, 1 },
                    { 6, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8498), 2, 2 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "BranchId", "CreatedAt", "DepartmentId", "Email", "EmployeeCode", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "FullName", "HireDate", "IsBiometricEnrolled", "JobTitleId", "Locale", "PasswordHash", "Phone", "PinHash", "PortraitImage", "PositionId", "Status", "TenantId", "TerminationDate", "TimeZone", "TrustedDeviceId", "TwoFactorEnabled", "UpdatedAt" },
                values: new object[,]
                {
                    { 4, "", 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7731), 2, "employee@company.com", "NV004", null, null, null, "Phạm Thị Employee", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7731), false, 6, "vi-VN", "HSEqbOoWNtVcp7r8Ous+JPgWx7cfiZ9kKGR02yw1Vk8=", "0901234570", null, null, null, "Active", 1, null, "SE Asia Standard Time", null, false, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7732) },
                    { 5, "", 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7734), 2, "dev@company.com", "NV005", null, null, null, "Hoàng Văn Dev", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7734), false, 5, "vi-VN", "HSEqbOoWNtVcp7r8Ous+JPgWx7cfiZ9kKGR02yw1Vk8=", "0901234571", null, null, null, "Active", 1, null, "SE Asia Standard Time", null, false, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7734) },
                    { 7, "", 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7739), 5, "sales@company.com", "NV007", null, null, null, "Đỗ Văn Sales", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7739), false, 6, "vi-VN", "HSEqbOoWNtVcp7r8Ous+JPgWx7cfiZ9kKGR02yw1Vk8=", "0901234573", null, null, null, "Active", 1, null, "SE Asia Standard Time", null, false, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7740) },
                    { 8, "", 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7742), 6, "marketing@company.com", "NV008", null, null, null, "Ngô Thị Marketing", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7742), false, 6, "vi-VN", "HSEqbOoWNtVcp7r8Ous+JPgWx7cfiZ9kKGR02yw1Vk8=", "0901234574", null, null, null, "Active", 1, null, "SE Asia Standard Time", null, false, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7742) }
                });

            migrationBuilder.InsertData(
                table: "WorkflowSteps",
                columns: new[] { "Id", "ApproverRoleId", "ApproverType", "ApproverUserId", "CanSkipIfApplicant", "IsActive", "Name", "StepOrder", "WorkflowId" },
                values: new object[,]
                {
                    { 1, null, "DirectManager", null, false, true, "Quản lý trực tiếp duyệt", 1, 1 },
                    { 2, 2, "Role", null, false, true, "HR duyệt", 2, 1 },
                    { 3, null, "DirectManager", null, false, true, "Quản lý trực tiếp duyệt", 1, 2 },
                    { 4, null, "SpecificUser", 6, false, true, "Kế toán trưởng duyệt", 2, 2 },
                    { 5, null, "SpecificUser", 1, false, true, "Giám đốc duyệt", 3, 2 },
                    { 6, null, "DirectManager", null, true, true, "Quản lý trực tiếp duyệt", 1, 3 },
                    { 7, 3, "Role", null, true, true, "Trưởng phòng duyệt", 2, 3 },
                    { 8, 2, "Role", null, false, true, "HR duyệt", 3, 3 },
                    { 9, null, "SpecificUser", 1, false, true, "Giám đốc duyệt", 4, 3 }
                });

            migrationBuilder.InsertData(
                table: "AssetAssignments",
                columns: new[] { "Id", "AssetId", "AssignedAt", "ReturnedAt", "Status", "UserId" },
                values: new object[] { 1, 1, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, "Assigned", 4 });

            migrationBuilder.InsertData(
                table: "AssetIncidents",
                columns: new[] { "Id", "AssetId", "Description", "ReportedAt", "ReportedByUserId", "Status", "Type" },
                values: new object[] { 1, 1, "Screen cracked", new DateTime(2026, 3, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), 4, "Open", "Damage" });

            migrationBuilder.InsertData(
                table: "CarBookings",
                columns: new[] { "Id", "Destination", "DriverName", "EndTime", "PickupLocation", "StartTime", "Status", "TenantId", "UserId" },
                values: new object[] { 1, "Client Site", "Nguyen Driver", new DateTime(2026, 3, 12, 17, 0, 0, 0, DateTimeKind.Unspecified), "Office", new DateTime(2026, 3, 12, 8, 0, 0, 0, DateTimeKind.Unspecified), "Pending", 1, 5 });

            migrationBuilder.InsertData(
                table: "Certifications",
                columns: new[] { "Id", "ExpiryDate", "IssuedDate", "Name", "Status", "TenantId", "UserId" },
                values: new object[] { 1, new DateTime(2027, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Safety Basics", "Active", 1, 4 });

            migrationBuilder.InsertData(
                table: "FormFields",
                columns: new[] { "Id", "DefaultValue", "DisplayOrder", "FieldName", "FieldType", "FormTemplateId", "IsRequired", "Label", "Placeholder", "ValidationRules", "Width" },
                values: new object[,]
                {
                    { 1, null, 1, "leave_type", "Dropdown", 1, true, "Loại nghỉ phép", null, null, 6 },
                    { 2, null, 2, "start_date", "Date", 1, true, "Từ ngày", null, null, 6 },
                    { 3, null, 3, "end_date", "Date", 1, true, "Đến ngày", null, null, 6 },
                    { 4, null, 4, "total_days", "Number", 1, true, "Số ngày nghỉ", null, null, 6 },
                    { 5, null, 5, "reason", "Textarea", 1, true, "Lý do", null, null, 12 },
                    { 6, null, 6, "attachment", "FileUpload", 1, false, "File đính kèm", null, null, 12 },
                    { 7, null, 1, "ot_date", "Date", 2, true, "Ngày làm thêm", null, null, 6 },
                    { 8, null, 2, "start_time", "Text", 2, true, "Giờ bắt đầu", "HH:mm", null, 6 },
                    { 9, null, 3, "end_time", "Text", 2, true, "Giờ kết thúc", "HH:mm", null, 6 },
                    { 10, null, 4, "project", "Dropdown", 2, true, "Dự án", null, null, 6 },
                    { 11, null, 5, "reason", "Textarea", 2, true, "Lý do làm thêm", null, null, 12 },
                    { 12, null, 1, "destination", "Text", 3, true, "Điểm đến", null, null, 6 },
                    { 13, null, 2, "start_date", "Date", 3, true, "Từ ngày", null, null, 6 },
                    { 14, null, 3, "end_date", "Date", 3, true, "Đến ngày", null, null, 6 },
                    { 15, null, 4, "purpose", "Textarea", 3, true, "Mục đích", null, null, 12 },
                    { 16, null, 1, "amount", "Number", 4, true, "Số tiền tạm ứng", null, null, 6 },
                    { 17, null, 2, "currency", "Dropdown", 4, true, "Loại tiền", null, null, 6 },
                    { 18, null, 3, "purpose", "Textarea", 4, true, "Mục đích", null, null, 12 },
                    { 19, null, 4, "receipt", "FileUpload", 4, false, "Hóa đơn đính kèm", null, null, 12 },
                    { 20, null, 1, "equipment_type", "Dropdown", 5, true, "Loại thiết bị", null, null, 6 },
                    { 21, null, 2, "reason", "Textarea", 5, true, "Lý do cần cấp phát", null, null, 12 }
                });

            migrationBuilder.InsertData(
                table: "LeaveBalances",
                columns: new[] { "Id", "CarriedOver", "CompensatoryDays", "LeaveTypeId", "SeniorityBonus", "TenantId", "TotalEntitled", "UpdatedAt", "Used", "UserId", "Year" },
                values: new object[,]
                {
                    { 1, 0.0, 0.0, 1, 0.0, 1, 12.0, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8253), 3.0, 4, 2026 },
                    { 2, 0.0, 0.0, 2, 0.0, 1, 30.0, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8256), 1.0, 4, 2026 },
                    { 3, 0.0, 0.0, 1, 0.0, 1, 12.0, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8258), 2.0, 5, 2026 },
                    { 4, 0.0, 0.0, 2, 0.0, 1, 30.0, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8259), 0.0, 5, 2026 },
                    { 5, 0.0, 0.0, 1, 0.0, 1, 12.0, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8260), 5.0, 7, 2026 },
                    { 6, 0.0, 0.0, 1, 0.0, 1, 12.0, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8261), 1.0, 8, 2026 }
                });

            migrationBuilder.InsertData(
                table: "MealRegistrations",
                columns: new[] { "Id", "Date", "MealType", "Notes", "TenantId", "UserId" },
                values: new object[] { 1, new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Overtime", "Vegetarian", 1, 4 });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "ActionUrl", "CreatedAt", "IsRead", "Message", "ReadAt", "RelatedRequestId", "TenantId", "Title", "Type", "UserId" },
                values: new object[] { 2, null, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8304), false, "Chào mừng bạn đến với hệ thống quản lý đơn từ DANGCAPNE", null, null, 1, "Chào mừng!", "Info", 4 });

            migrationBuilder.InsertData(
                table: "PerformanceGoals",
                columns: new[] { "Id", "CycleId", "Status", "Title", "UserId", "Weight" },
                values: new object[] { 1, 1, "Active", "Deliver projects on time", 4, 1.0m });

            migrationBuilder.InsertData(
                table: "PolicyAcknowledgements",
                columns: new[] { "Id", "AcknowledgedAt", "PolicyDocumentId", "Status", "UserId" },
                values: new object[] { 1, new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Acknowledged", 4 });

            migrationBuilder.InsertData(
                table: "Requests",
                columns: new[] { "Id", "CompletedAt", "CreatedAt", "CurrentStepOrder", "FormTemplateId", "Priority", "RequestCode", "RequesterId", "Status", "TenantId", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 1, "Normal", "REQ-20260305-001", 4, "Pending", 1, "Xin nghỉ phép năm 3 ngày", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8326) },
                    { 2, new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 2, "Normal", "REQ-20260307-001", 5, "Approved", 1, "Làm thêm giờ dự án ERP", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8333) },
                    { 3, null, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, 4, "High", "REQ-20260310-001", 7, "InProgress", 1, "Tạm ứng đi công tác Đà Nẵng", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8338) },
                    { 4, new DateTime(2026, 3, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 1, "Normal", "REQ-20260311-001", 8, "Rejected", 1, "Xin nghỉ phép 1 ngày", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8340) }
                });

            migrationBuilder.InsertData(
                table: "SlaConfigs",
                columns: new[] { "Id", "AutoEscalate", "AutoRemind", "EscalationHours", "FormTemplateId", "ReminderHours", "TenantId" },
                values: new object[,]
                {
                    { 1, true, true, 48, 1, 24, 1 },
                    { 2, true, true, 24, 4, 12, 1 }
                });

            migrationBuilder.InsertData(
                table: "TrainingEnrollments",
                columns: new[] { "Id", "CompletedAt", "CourseId", "EnrolledAt", "Status", "UserId" },
                values: new object[] { 1, null, 1, new DateTime(2026, 3, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "Enrolled", 4 });

            migrationBuilder.InsertData(
                table: "UniformRequests",
                columns: new[] { "Id", "Quantity", "RequestedAt", "Size", "Status", "TenantId", "UserId" },
                values: new object[] { 1, 2, new DateTime(2026, 3, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), "L", "Pending", 1, 7 });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "AssignedAt", "RoleId", "UserId" },
                values: new object[,]
                {
                    { 4, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7802), 4, 4 },
                    { 5, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7803), 4, 5 },
                    { 7, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7804), 4, 7 },
                    { 8, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7805), 4, 8 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AvatarUrl", "BranchId", "CreatedAt", "DepartmentId", "Email", "EmployeeCode", "FaceDescriptorFront", "FaceDescriptorLeft", "FaceDescriptorRight", "FullName", "HireDate", "IsBiometricEnrolled", "JobTitleId", "Locale", "PasswordHash", "Phone", "PinHash", "PortraitImage", "PositionId", "Status", "TenantId", "TerminationDate", "TimeZone", "TrustedDeviceId", "TwoFactorEnabled", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "", 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7716), 1, "admin@company.com", "NV001", null, null, null, "Nguyễn Văn Admin", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7710), false, 1, "vi-VN", "HSEqbOoWNtVcp7r8Ous+JPgWx7cfiZ9kKGR02yw1Vk8=", "0901234567", null, null, 1, "Active", 1, null, "SE Asia Standard Time", null, false, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7716) },
                    { 2, "", 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7725), 3, "hr@company.com", "NV002", null, null, null, "Trần Thị HR", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7725), false, 3, "vi-VN", "HSEqbOoWNtVcp7r8Ous+JPgWx7cfiZ9kKGR02yw1Vk8=", "0901234568", null, null, 3, "Active", 1, null, "SE Asia Standard Time", null, false, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7726) },
                    { 3, "", 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7728), 2, "manager@company.com", "NV003", null, null, null, "Lê Văn Manager", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7728), false, 3, "vi-VN", "HSEqbOoWNtVcp7r8Ous+JPgWx7cfiZ9kKGR02yw1Vk8=", "0901234569", null, null, 2, "Active", 1, null, "SE Asia Standard Time", null, false, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7729) },
                    { 6, "", 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7737), 4, "accountant@company.com", "NV006", null, null, null, "Vũ Thị Kế Toán", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7736), false, 3, "vi-VN", "HSEqbOoWNtVcp7r8Ous+JPgWx7cfiZ9kKGR02yw1Vk8=", "0901234572", null, null, 4, "Active", 1, null, "SE Asia Standard Time", null, false, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7737) }
                });

            migrationBuilder.InsertData(
                table: "BonusRequests",
                columns: new[] { "Id", "Amount", "CreatedAt", "Reason", "RequestedByUserId", "Status", "TenantId", "Type", "UserId" },
                values: new object[] { 1, 2000000m, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "Project delivery", 3, "Pending", 1, "Spot", 5 });

            migrationBuilder.InsertData(
                table: "CertificationRenewals",
                columns: new[] { "Id", "ApprovedAt", "ApprovedByUserId", "CertificationId", "RequestedAt", "Status" },
                values: new object[] { 1, null, 2, 1, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Pending" });

            migrationBuilder.InsertData(
                table: "FormFieldOptions",
                columns: new[] { "Id", "DisplayOrder", "FormFieldId", "Label", "Value" },
                values: new object[,]
                {
                    { 1, 1, 1, "Phép năm", "AL" },
                    { 2, 2, 1, "Nghỉ ốm", "SL" },
                    { 3, 3, 1, "Nghỉ thai sản", "ML" },
                    { 4, 4, 1, "Nghỉ không lương", "UL" },
                    { 5, 5, 1, "Nghỉ bù", "CO" },
                    { 6, 1, 10, "Dự án ERP", "PRJ-001" },
                    { 7, 2, 10, "Dự án Website", "PRJ-002" },
                    { 8, 1, 17, "VNĐ", "VND" },
                    { 9, 2, 17, "USD", "USD" },
                    { 10, 1, 20, "Laptop", "LAPTOP" },
                    { 11, 2, 20, "Màn hình", "MONITOR" },
                    { 12, 3, 20, "Điện thoại", "PHONE" },
                    { 13, 4, 20, "Khác", "OTHER" }
                });

            migrationBuilder.InsertData(
                table: "JobRequisitions",
                columns: new[] { "Id", "BudgetMax", "BudgetMin", "CreatedAt", "CreatedByUserId", "DepartmentId", "Headcount", "JobTitleId", "Status", "TenantId", "Title" },
                values: new object[] { 1, 12000000m, 8000000m, new DateTime(2026, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, 3, 1, 5, "Pending", 1, "HR Specialist" });

            migrationBuilder.InsertData(
                table: "Notifications",
                columns: new[] { "Id", "ActionUrl", "CreatedAt", "IsRead", "Message", "ReadAt", "RelatedRequestId", "TenantId", "Title", "Type", "UserId" },
                values: new object[,]
                {
                    { 1, "/Approvals", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8301), false, "Phạm Thị Employee đã tạo đơn xin nghỉ phép", null, null, 1, "Đơn mới cần duyệt", "Approval", 3 },
                    { 3, "/HR", new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8305), false, "Có 5 đơn mới cần HR xử lý trong tuần này", null, null, 1, "Báo cáo tuần", "Info", 2 }
                });

            migrationBuilder.InsertData(
                table: "OffboardingTasks",
                columns: new[] { "Id", "AssignedToUserId", "CompletedAt", "DueDate", "Status", "TemplateId", "UserId" },
                values: new object[] { 1, 2, null, new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Open", 1, 8 });

            migrationBuilder.InsertData(
                table: "OnboardingTasks",
                columns: new[] { "Id", "AssignedToUserId", "CompletedAt", "DueDate", "Status", "TemplateId", "UserId" },
                values: new object[] { 1, 2, null, new DateTime(2026, 3, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), "Open", 1, 4 });

            migrationBuilder.InsertData(
                table: "PerformanceReviews",
                columns: new[] { "Id", "CycleId", "ReviewerId", "Score", "Status", "SubmittedAt", "UserId" },
                values: new object[] { 1, 1, 3, null, "Draft", null, 4 });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Budget", "Code", "EndDate", "IsActive", "ManagerId", "Name", "OtCost", "StartDate", "Status", "TenantId" },
                values: new object[,]
                {
                    { 1, 500000000m, "PRJ-001", null, true, 3, "Dự án ERP", 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Active", 1 },
                    { 2, 200000000m, "PRJ-002", null, true, 3, "Dự án Website", 0m, new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Active", 1 }
                });

            migrationBuilder.InsertData(
                table: "RequestApprovals",
                columns: new[] { "Id", "ActionDate", "ApproverId", "Comments", "CreatedAt", "IpAddress", "RequestId", "Status", "StepName", "StepOrder", "VerifiedByPin" },
                values: new object[,]
                {
                    { 1, null, 3, null, new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 1, "Pending", "Quản lý trực tiếp duyệt", 1, false },
                    { 2, new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, null, new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2, "Approved", "Quản lý trực tiếp duyệt", 1, false },
                    { 3, new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, null, new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 2, "Approved", "HR duyệt", 2, false },
                    { 4, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, null, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 3, "Approved", "Quản lý trực tiếp duyệt", 1, false },
                    { 5, null, 6, null, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 3, "Pending", "Kế toán trưởng duyệt", 2, false },
                    { 6, new DateTime(2026, 3, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Phòng đang có nhiều người nghỉ, vui lòng chọn ngày khác", new DateTime(2026, 3, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), null, 4, "Rejected", "Quản lý trực tiếp duyệt", 1, false }
                });

            migrationBuilder.InsertData(
                table: "RequestAuditLogs",
                columns: new[] { "Id", "Action", "CreatedAt", "Details", "IpAddress", "NewStatus", "OldStatus", "RequestId", "UserAgent", "UserId" },
                values: new object[,]
                {
                    { 1, "Created", new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), "Tạo đơn xin nghỉ phép", null, "Pending", null, 1, null, 4 },
                    { 2, "Created", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Pending", null, 2, null, 5 },
                    { 3, "Approved", new DateTime(2026, 3, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "InProgress", "Pending", 2, null, 3 },
                    { 4, "Approved", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Approved", "InProgress", 2, null, 2 }
                });

            migrationBuilder.InsertData(
                table: "RequestData",
                columns: new[] { "Id", "CreatedAt", "FieldKey", "FieldType", "FieldValue", "RequestId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8365), "leave_type", null, "AL", 1 },
                    { 2, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8367), "start_date", null, "2026-03-15", 1 },
                    { 3, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8368), "end_date", null, "2026-03-17", 1 },
                    { 4, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8369), "total_days", null, "3", 1 },
                    { 5, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8370), "reason", null, "Nghỉ phép cá nhân để đi du lịch", 1 },
                    { 6, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8370), "ot_date", null, "2026-03-08", 2 },
                    { 7, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8371), "start_time", null, "18:00", 2 },
                    { 8, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8372), "end_time", null, "21:00", 2 },
                    { 9, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8373), "project", null, "PRJ-001", 2 },
                    { 10, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8374), "reason", null, "Deploy module thanh toán", 2 },
                    { 11, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8374), "amount", null, "15000000", 3 },
                    { 12, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8375), "currency", null, "VND", 3 },
                    { 13, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8376), "purpose", null, "Công tác gặp khách hàng tại Đà Nẵng", 3 }
                });

            migrationBuilder.InsertData(
                table: "SalaryAdjustmentRequests",
                columns: new[] { "Id", "CreatedAt", "EffectiveDate", "ProposedSalary", "Reason", "RequestedByUserId", "Status", "TenantId", "UserId" },
                values: new object[] { 1, new DateTime(2026, 3, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 12000000m, "High performance", 3, "Pending", 1, 4 });

            migrationBuilder.InsertData(
                table: "UserManagers",
                columns: new[] { "Id", "EndDate", "IsPrimary", "ManagerId", "StartDate", "UserId" },
                values: new object[,]
                {
                    { 1, null, true, 3, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7829), 4 },
                    { 2, null, true, 3, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7831), 5 },
                    { 3, null, true, 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7832), 3 },
                    { 4, null, true, 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7833), 2 },
                    { 5, null, true, 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7834), 7 },
                    { 6, null, true, 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7834), 8 }
                });

            migrationBuilder.InsertData(
                table: "UserPermissions",
                columns: new[] { "Id", "GrantedAt", "GrantedByUserId", "IsActive", "PermissionId", "UserId" },
                values: new object[] { 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(8515), 1, true, 2, 2 });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "Id", "AssignedAt", "RoleId", "UserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7798), 1, 1 },
                    { 2, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7801), 2, 2 },
                    { 3, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7801), 3, 3 },
                    { 6, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7804), 4, 6 },
                    { 9, new DateTime(2026, 3, 24, 15, 24, 16, 618, DateTimeKind.Local).AddTicks(7806), 3, 1 }
                });

            migrationBuilder.InsertData(
                table: "CandidateApplications",
                columns: new[] { "Id", "AppliedAt", "CandidateId", "JobRequisitionId", "Status" },
                values: new object[] { 1, new DateTime(2026, 3, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, 1, "Applied" });

            migrationBuilder.InsertData(
                table: "JobRequisitionApprovals",
                columns: new[] { "Id", "ActionDate", "ApproverId", "Comments", "JobRequisitionId", "Status" },
                values: new object[] { 1, new DateTime(2026, 3, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Approved for hiring", 1, "Approved" });

            migrationBuilder.InsertData(
                table: "PerformanceReviewItems",
                columns: new[] { "Id", "Comment", "GoalId", "ReviewId", "Score" },
                values: new object[] { 1, "Good performance", 1, 1, 4.0m });

            migrationBuilder.InsertData(
                table: "InterviewSchedules",
                columns: new[] { "Id", "CandidateApplicationId", "InterviewerId", "Location", "Notes", "ScheduledAt", "Status" },
                values: new object[] { 1, 1, 3, "Meeting Room 1", null, new DateTime(2026, 3, 5, 9, 0, 0, 0, DateTimeKind.Unspecified), "Scheduled" });

            migrationBuilder.InsertData(
                table: "OfferLetters",
                columns: new[] { "Id", "CandidateApplicationId", "OfferedSalary", "SentAt", "StartDate", "Status" },
                values: new object[] { 1, 1, 10000000m, new DateTime(2026, 3, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sent" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_AssetId",
                table: "AssetAssignments",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetAssignments_UserId",
                table: "AssetAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetIncidents_AssetId",
                table: "AssetIncidents",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetIncidents_ReportedByUserId",
                table: "AssetIncidents",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssignedToUserId",
                table: "Assets",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CategoryId",
                table: "Assets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLocationConfigs_BranchId",
                table: "AttendanceLocationConfigs",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusRequests_RequestedByUserId",
                table: "BonusRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusRequests_UserId",
                table: "BonusRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateApplications_CandidateId",
                table: "CandidateApplications",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateApplications_JobRequisitionId",
                table: "CandidateApplications",
                column: "JobRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CarBookings_UserId",
                table: "CarBookings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRenewals_ApprovedByUserId",
                table: "CertificationRenewals",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificationRenewals_CertificationId",
                table: "CertificationRenewals",
                column: "CertificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Certifications_UserId",
                table: "Certifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegations_DelegateId",
                table: "Delegations",
                column: "DelegateId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegations_DelegatorId",
                table: "Delegations",
                column: "DelegatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerId",
                table: "Departments",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId",
                table: "Departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftRequests_FormTemplateId",
                table: "DraftRequests",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DraftRequests_UserId",
                table: "DraftRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_UserId",
                table: "EmployeeDocuments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_EscalateToUserId",
                table: "EscalationRules",
                column: "EscalateToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EscalationRules_SlaConfigId",
                table: "EscalationRules",
                column: "SlaConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_FromCurrencyId",
                table: "ExchangeRates",
                column: "FromCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_ToCurrencyId",
                table: "ExchangeRates",
                column: "ToCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFieldOptions_FormFieldId",
                table: "FormFieldOptions",
                column: "FormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_FormTemplateId",
                table: "FormFields",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FormTemplates_WorkflowId",
                table: "FormTemplates",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_CandidateApplicationId",
                table: "InterviewSchedules",
                column: "CandidateApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_InterviewerId",
                table: "InterviewSchedules",
                column: "InterviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequisitionApprovals_ApproverId",
                table: "JobRequisitionApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequisitionApprovals_JobRequisitionId",
                table: "JobRequisitionApprovals",
                column: "JobRequisitionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequisitions_CreatedByUserId",
                table: "JobRequisitions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequisitions_DepartmentId",
                table: "JobRequisitions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRequisitions_JobTitleId",
                table: "JobRequisitions",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_LeaveTypeId",
                table: "LeaveBalances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_UserId_Year",
                table: "LeaveBalances",
                columns: new[] { "UserId", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_MealRegistrations_UserId",
                table: "MealRegistrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingTasks_AssignedToUserId",
                table: "OffboardingTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingTasks_TemplateId",
                table: "OffboardingTasks",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingTasks_UserId",
                table: "OffboardingTasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingTaskTemplates_DefaultAssigneeRoleId",
                table: "OffboardingTaskTemplates",
                column: "DefaultAssigneeRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferLetters_CandidateApplicationId",
                table: "OfferLetters",
                column: "CandidateApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_AssignedToUserId",
                table: "OnboardingTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_TemplateId",
                table: "OnboardingTasks",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTasks_UserId",
                table: "OnboardingTasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingTaskTemplates_DefaultAssigneeRoleId",
                table: "OnboardingTaskTemplates",
                column: "DefaultAssigneeRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceGoals_CycleId",
                table: "PerformanceGoals",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceGoals_UserId",
                table: "PerformanceGoals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviewItems_GoalId",
                table: "PerformanceReviewItems",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviewItems_ReviewId",
                table: "PerformanceReviewItems",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviews_CycleId",
                table: "PerformanceReviews",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviews_ReviewerId",
                table: "PerformanceReviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviews_UserId",
                table: "PerformanceReviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyAcknowledgements_PolicyDocumentId",
                table: "PolicyAcknowledgements",
                column: "PolicyDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyAcknowledgements_UserId",
                table: "PolicyAcknowledgements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_DepartmentId",
                table: "Positions",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ManagerId",
                table: "Projects",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestApprovals_ApproverId",
                table: "RequestApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestApprovals_RequestId",
                table: "RequestApprovals",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAttachments_RequestId",
                table: "RequestAttachments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAuditLogs_RequestId",
                table: "RequestAuditLogs",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestAuditLogs_UserId",
                table: "RequestAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestComments_ParentCommentId",
                table: "RequestComments",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestComments_RequestId",
                table: "RequestComments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestComments_UserId",
                table: "RequestComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestData_RequestId",
                table: "RequestData",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestFollowers_RequestId",
                table: "RequestFollowers",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_FormTemplateId",
                table: "Requests",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequesterId",
                table: "Requests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_Status",
                table: "Requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_TenantId",
                table: "Requests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdjustmentRequests_RequestedByUserId",
                table: "SalaryAdjustmentRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdjustmentRequests_UserId",
                table: "SalaryAdjustmentRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_RequesterId",
                table: "ShiftSwapRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_TargetUserId",
                table: "ShiftSwapRequests",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaConfigs_FormTemplateId",
                table: "SlaConfigs",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialInsurances_UserId",
                table: "SocialInsurances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId",
                table: "TeamMembers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_LeaderId",
                table: "Teams",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantConfigs_TenantId",
                table: "TenantConfigs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_UserId_Date",
                table: "Timesheets",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEnrollments_CourseId",
                table: "TrainingEnrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEnrollments_UserId",
                table: "TrainingEnrollments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UniformRequests_UserId",
                table: "UniformRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserManagers_ManagerId",
                table: "UserManagers",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserManagers_UserId",
                table: "UserManagers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_GrantedByUserId",
                table: "UserPermissions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_PermissionId",
                table: "UserPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_UserId",
                table: "UserPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Users_JobTitleId",
                table: "Users",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PositionId",
                table: "Users",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserShifts_ShiftId",
                table: "UserShifts",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_UserShifts_UserId",
                table: "UserShifts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConditions_WorkflowStepId",
                table: "WorkflowConditions",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowStepApprovers_WorkflowStepId",
                table: "WorkflowStepApprovers",
                column: "WorkflowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowId",
                table: "WorkflowSteps",
                column: "WorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetAssignments_Assets_AssetId",
                table: "AssetAssignments",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetAssignments_Users_UserId",
                table: "AssetAssignments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetIncidents_Assets_AssetId",
                table: "AssetIncidents",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetIncidents_Users_ReportedByUserId",
                table: "AssetIncidents",
                column: "ReportedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Users_AssignedToUserId",
                table: "Assets",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BonusRequests_Users_RequestedByUserId",
                table: "BonusRequests",
                column: "RequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BonusRequests_Users_UserId",
                table: "BonusRequests",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateApplications_JobRequisitions_JobRequisitionId",
                table: "CandidateApplications",
                column: "JobRequisitionId",
                principalTable: "JobRequisitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CarBookings_Users_UserId",
                table: "CarBookings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificationRenewals_Certifications_CertificationId",
                table: "CertificationRenewals",
                column: "CertificationId",
                principalTable: "Certifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificationRenewals_Users_ApprovedByUserId",
                table: "CertificationRenewals",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Certifications_Users_UserId",
                table: "Certifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Delegations_Users_DelegateId",
                table: "Delegations",
                column: "DelegateId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Delegations_Users_DelegatorId",
                table: "Delegations",
                column: "DelegatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_ManagerId",
                table: "Departments",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_ManagerId",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "AssetAssignments");

            migrationBuilder.DropTable(
                name: "AssetIncidents");

            migrationBuilder.DropTable(
                name: "AttendanceLocationConfigs");

            migrationBuilder.DropTable(
                name: "BonusRequests");

            migrationBuilder.DropTable(
                name: "CarBookings");

            migrationBuilder.DropTable(
                name: "CertificationRenewals");

            migrationBuilder.DropTable(
                name: "DailyAttendances");

            migrationBuilder.DropTable(
                name: "Delegations");

            migrationBuilder.DropTable(
                name: "DraftRequests");

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "EmployeeDocuments");

            migrationBuilder.DropTable(
                name: "EscalationRules");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");

            migrationBuilder.DropTable(
                name: "FormFieldOptions");

            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "InterviewSchedules");

            migrationBuilder.DropTable(
                name: "JobRequisitionApprovals");

            migrationBuilder.DropTable(
                name: "LeaveAccruals");

            migrationBuilder.DropTable(
                name: "LeaveBalances");

            migrationBuilder.DropTable(
                name: "MealRegistrations");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OffboardingTasks");

            migrationBuilder.DropTable(
                name: "OfferLetters");

            migrationBuilder.DropTable(
                name: "OnboardingTasks");

            migrationBuilder.DropTable(
                name: "OvertimeRates");

            migrationBuilder.DropTable(
                name: "PerformanceReviewItems");

            migrationBuilder.DropTable(
                name: "PolicyAcknowledgements");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "RequestApprovals");

            migrationBuilder.DropTable(
                name: "RequestAttachments");

            migrationBuilder.DropTable(
                name: "RequestAuditLogs");

            migrationBuilder.DropTable(
                name: "RequestComments");

            migrationBuilder.DropTable(
                name: "RequestData");

            migrationBuilder.DropTable(
                name: "RequestFollowers");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SalaryAdjustmentRequests");

            migrationBuilder.DropTable(
                name: "ShiftSwapRequests");

            migrationBuilder.DropTable(
                name: "SocialInsurances");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "SystemErrors");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "TenantConfigs");

            migrationBuilder.DropTable(
                name: "Timesheets");

            migrationBuilder.DropTable(
                name: "TrainingEnrollments");

            migrationBuilder.DropTable(
                name: "UniformRequests");

            migrationBuilder.DropTable(
                name: "UserManagers");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserShifts");

            migrationBuilder.DropTable(
                name: "WorkflowConditions");

            migrationBuilder.DropTable(
                name: "WorkflowStepApprovers");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Certifications");

            migrationBuilder.DropTable(
                name: "SlaConfigs");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "FormFields");

            migrationBuilder.DropTable(
                name: "LeaveTypes");

            migrationBuilder.DropTable(
                name: "OffboardingTaskTemplates");

            migrationBuilder.DropTable(
                name: "CandidateApplications");

            migrationBuilder.DropTable(
                name: "OnboardingTaskTemplates");

            migrationBuilder.DropTable(
                name: "PerformanceGoals");

            migrationBuilder.DropTable(
                name: "PerformanceReviews");

            migrationBuilder.DropTable(
                name: "PolicyDocuments");

            migrationBuilder.DropTable(
                name: "Requests");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "TrainingCourses");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "WorkflowSteps");

            migrationBuilder.DropTable(
                name: "AssetCategories");

            migrationBuilder.DropTable(
                name: "Candidates");

            migrationBuilder.DropTable(
                name: "JobRequisitions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "PerformanceCycles");

            migrationBuilder.DropTable(
                name: "FormTemplates");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "JobTitles");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
