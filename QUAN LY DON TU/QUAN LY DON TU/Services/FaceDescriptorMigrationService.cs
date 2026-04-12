using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;

namespace DANGCAPNE.Services
{
    public interface IFaceDescriptorMigrationService
    {
        Task<(int Updated, int Skipped, string Message)> MigrateFaceDescriptorsFromSQLiteAsync();
    }

    public class FaceDescriptorMigrationService : IFaceDescriptorMigrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FaceDescriptorMigrationService> _logger;

        public FaceDescriptorMigrationService(ApplicationDbContext context, ILogger<FaceDescriptorMigrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<(int Updated, int Skipped, string Message)> MigrateFaceDescriptorsFromSQLiteAsync()
        {
            int updated = 0;
            int skipped = 0;
            var messages = new List<string>();

            string sqliteDbPath = Path.Combine(Directory.GetCurrentDirectory(), "app.db");
            
            if (!File.Exists(sqliteDbPath))
            {
                return (0, 0, $"❌ SQLite database not found at {sqliteDbPath}");
            }

            messages.Add($"📂 Found SQLite database: {sqliteDbPath}");

            try
            {
                var sqliteUsers = new Dictionary<int, (string FullName, bool IsBiometricEnrolled, string FrontDesc, string LeftDesc, string RightDesc)>();
                
                // Create temporary SQLite DbContext
                var sqliteOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlite($"Data Source={sqliteDbPath}")
                    .Options;
                
                using (var sqliteContext = new ApplicationDbContext(sqliteOptions))
                {
                    var sqliteUserData = await sqliteContext.Users
                        .Where(u => u.IsBiometricEnrolled && !string.IsNullOrEmpty(u.FaceDescriptorFront))
                        .Select(u => new 
                        { 
                            u.Id, 
                            u.FullName, 
                            u.IsBiometricEnrolled, 
                            u.FaceDescriptorFront, 
                            u.FaceDescriptorLeft, 
                            u.FaceDescriptorRight 
                        })
                        .ToListAsync();

                    foreach (var user in sqliteUserData)
                    {
                        sqliteUsers[user.Id] = (user.FullName, user.IsBiometricEnrolled, user.FaceDescriptorFront, user.FaceDescriptorLeft, user.FaceDescriptorRight);
                        messages.Add($"✓ Found SQLite User #{user.Id}: {user.FullName}");
                    }
                }
                
                messages.Add($"\n📊 Total users in SQLite with biometric: {sqliteUsers.Count}\n");

                // Update PostgreSQL (Supabase)
                foreach (var kvp in sqliteUsers)
                {
                    int userId = kvp.Key;
                    var (fullName, isBiometric, frontDesc, leftDesc, rightDesc) = kvp.Value;
                    
                    var pgUser = await _context.Users.FindAsync(userId);
                    if (pgUser == null)
                    {
                        messages.Add($"⚠️  User #{userId} ({fullName}) not found in PostgreSQL - SKIPPED");
                        skipped++;
                        continue;
                    }
                    
                    if (!string.IsNullOrEmpty(pgUser.FaceDescriptorFront) && pgUser.FaceDescriptorFront.Length > 100)
                    {
                        messages.Add($"✓ User #{userId} ({fullName}) already has valid face descriptors - SKIPPED");
                        skipped++;
                        continue;
                    }
                    
                    pgUser.FaceDescriptorFront = frontDesc;
                    pgUser.FaceDescriptorLeft = leftDesc;
                    pgUser.FaceDescriptorRight = rightDesc;
                    pgUser.IsBiometricEnrolled = isBiometric;
                    pgUser.UpdatedAt = DateTime.UtcNow;
                    
                    _context.Users.Update(pgUser);
                    updated++;
                    messages.Add($"✓ Updated User #{userId} ({fullName}) with face descriptors");
                }
                
                if (updated > 0)
                {
                    await _context.SaveChangesAsync();
                    messages.Add($"\n✅ Successfully migrated {updated} users with face descriptors!");
                }
                else if (skipped > 0)
                {
                    messages.Add($"\n⚠️  No users needed updating. {skipped} already have valid descriptors.");
                }
                else
                {
                    messages.Add($"\n⚠️  No users to migrate from SQLite.");
                }
            }
            catch (Exception ex)
            {
                messages.Add($"❌ Error during migration: {ex.Message}");
                _logger.LogError($"[FaceMigration Error] {ex.GetType().Name}: {ex.Message}");
                _logger.LogError(ex.StackTrace);
            }

            string resultMessage = string.Join("\n", messages);
            return (updated, skipped, resultMessage);
        }
    }
}
