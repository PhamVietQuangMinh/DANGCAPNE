using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.Hubs;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
// Add services to the container.
builder.Services.AddControllersWithViews();

// PostgreSQL Supabase connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null)));

// Session for authentication
builder.Services.AddHttpClient<DANGCAPNE.Services.GeminiAIService>();
builder.Services.AddScoped<DANGCAPNE.Services.IFileService, DANGCAPNE.Services.FileService>();
builder.Services.AddScoped<DANGCAPNE.Services.IApprovedRequestPdfService, DANGCAPNE.Services.ApprovedRequestPdfService>();
builder.Services.AddScoped<DANGCAPNE.Services.IPayrollPdfService, DANGCAPNE.Services.PayrollPdfService>();
builder.Services.AddScoped<DANGCAPNE.Services.IFaceDescriptorMigrationService, DANGCAPNE.Services.FaceDescriptorMigrationService>();
builder.Services.AddScoped<DANGCAPNE.Services.IEmailNotificationService, DANGCAPNE.Services.EmailNotificationService>();
builder.Services.AddScoped<DANGCAPNE.Services.IAttendanceRiskScoringService, DANGCAPNE.Services.AttendanceRiskScoringService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "DANGCAPNE_Session";
});

builder.Services.AddHttpContextAccessor();

// SignalR for real-time
builder.Services.AddSignalR();

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Migration may fail when using connection pooler (e.g. on school network)
        // This is safe to ignore if schema is already up-to-date
        Console.WriteLine($"[Startup] Migration skipped: {ex.Message}");
    }

    // Skip extended schema patch for SQLite (PostgreSQL syntax not compatible)
    if (db.Database.ProviderName?.Contains("Npgsql") == true)
    {
        try
        {
            await SchemaPatchRunner.EnsureExtendedSchemaAsync(db);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Startup] Extended schema patch skipped: {ex.Message}");
            System.IO.File.WriteAllText("patch_error.txt", ex.ToString());
        }
    }

    // Seed demo accounts (works across providers). Runs by default in Development,
    // or when explicitly enabled via SEED_DEMO_ACCOUNTS=1.
    if (app.Environment.IsDevelopment() ||
        string.Equals(Environment.GetEnvironmentVariable("SEED_DEMO_ACCOUNTS"), "1", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            await SchemaPatchRunner.EnsureDemoAccountsAsync(db);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Startup] Demo accounts seed skipped: {ex.Message}");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");

app.Run();
