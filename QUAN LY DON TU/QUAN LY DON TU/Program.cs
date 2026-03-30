using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.Hubs;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
// Add services to the container.
builder.Services.AddControllersWithViews();

// SQL Server connection
// PostgreSQL (Supabase) connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session for authentication
builder.Services.AddHttpClient<DANGCAPNE.Services.GeminiAIService>();
builder.Services.AddScoped<DANGCAPNE.Services.IFileService, DANGCAPNE.Services.FileService>();
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

    try
    {
        await SchemaPatchRunner.EnsureExtendedSchemaAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Startup] Extended schema patch skipped: {ex.Message}");
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
