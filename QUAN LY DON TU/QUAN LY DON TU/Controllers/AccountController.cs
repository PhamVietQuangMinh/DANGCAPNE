using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.ViewModels;
using DANGCAPNE.Models.Organization;
using DANGCAPNE.Models.Timekeeping;
using Microsoft.AspNetCore.SignalR;
using DANGCAPNE.Hubs;
using DANGCAPNE.Security;
using Npgsql;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DANGCAPNE.Models.SystemModels;
namespace DANGCAPNE.Controllers
{
    public class AccountController : Controller
    {
        private const string RememberMeCookieName = "DANGCAPNE_REMEMBER";
        private const int RememberMeDays = 30;
        private readonly ApplicationDbContext _context;
        private readonly Services.IFileService _fileService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IWebHostEnvironment _env;

        public AccountController(ApplicationDbContext context, Services.IFileService fileService, IHubContext<NotificationHub> hubContext, IWebHostEnvironment env)
        {
            _context = context;
            _fileService = fileService;
            _hubContext = hubContext;
            _env = env;
        }

                [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToHomeBySessionRole();

            if (await TryLoginFromRememberCookieAsync())
                return RedirectToHomeBySessionRole();

            ViewBag.DetectedIp = NormalizeIp(HttpContext.Connection.RemoteIpAddress?.ToString()) ?? "Khong xac dinh";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var remoteIp = NormalizeIp(HttpContext.Connection.RemoteIpAddress?.ToString());
            if (string.IsNullOrWhiteSpace(remoteIp))
            {
                await WriteAuthAuditLog(null, model.EmployeeCodeOrEmail, "LoginFailed", false, "Remote IP unavailable");
                ViewBag.Error = "Khong xac dinh duoc IP thiet bi.";
                return View(model);
            }

            // ── Strict Wifi / Internal Network Check ────────────────────────────
            // 1. Check if the credential belongs to a privileged user (Admin/IT) who can skip checks
            var tempUser = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.EmployeeCode == model.EmployeeCodeOrEmail.Trim());

            bool isPrivileged = false;
            if (tempUser != null)
            {
                var roleNames = tempUser.UserRoles.Select(ur => ur.Role?.Name ?? "").ToHashSet(StringComparer.OrdinalIgnoreCase);
                isPrivileged = roleNames.Contains("IT") || roleNames.Contains("ITManager") || roleNames.Contains("Admin");
            }

            if (!isPrivileged)
            {
                var tenantId = tempUser?.TenantId ?? 1;
                bool whitelistHasEntries = await _context.AllowedIps.AnyAsync(a => a.TenantId == tenantId && a.IsActive);

                if (whitelistHasEntries)
                {
                    // Strict mode: Only manually added IPs are allowed
                    bool ipAllowed = await _context.AllowedIps.AnyAsync(a => a.TenantId == tenantId && a.IsActive && a.IpAddress == remoteIp);
                    if (!ipAllowed)
                    {
                        await WriteAuthAuditLog(tempUser?.Id, model.EmployeeCodeOrEmail, "LoginFailed", false, $"Wifi not whitelisted: {remoteIp}");
                        ViewBag.Error = $"Wifi nay chua duoc phong IT cap quyen truy cap (IP: {remoteIp}).";
                        return View(model);
                    }
                }
                else
                {
                    // Open mode / Fallback: Only allow company internal network ranges
                    if (!IsInternalNetwork(HttpContext.Connection.RemoteIpAddress))
                    {
                        await WriteAuthAuditLog(tempUser?.Id, model.EmployeeCodeOrEmail, "LoginFailed", false, "Outside intranet and no whitelist");
                        ViewBag.Error = "Ban dang o ngoai mang noi bo cong ty. Vui long ket noi Wifi van phong.";
                        return View(model);
                    }
                }
            }
            // ────────────────────────────────────────────────────────────────────

            if (string.IsNullOrWhiteSpace(model.EmployeeCodeOrEmail))
            {
                await WriteAuthAuditLog(null, model.EmployeeCodeOrEmail, "LoginFailed", false, "Missing employee code");
                ViewBag.Error = "Vui long nhap ma nhan vien.";
                return View(model);
            }

            // Re-fetch user with full details if privileged check passed
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.EmployeeCode == model.EmployeeCodeOrEmail.Trim());

            if (user == null)
            {
                await WriteAuthAuditLog(null, model.EmployeeCodeOrEmail, "LoginFailed", false, "Invalid employee code");
                ViewBag.Error = "Ma nhan vien khong ton tai.";
                return View(model);
            }

            if (user.Status == "PendingApproval")
            {
                // Email approval flow removed: auto-activate legacy pending accounts.
                user.Status = "Active";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            if (user.Status == "Rejected")
            {
                await WriteAuthAuditLog(user.Id, user.Email, "LoginFailed", false, "Registration rejected");
                ViewBag.Error = "Tai khoan da bi tu choi.";
                return View(model);
            }

            if (user.Status != "Active")
            {
                await WriteAuthAuditLog(user.Id, user.Email, "LoginFailed", false, "Inactive account");
                ViewBag.Error = "Tai khoan dang bi khoa hoac khong kha dung.";
                return View(model);
            }

            // ────────────────────────────────────────────────────────────────────

            // Note: manual IP input validation and first-time registration removed per user request.
            // Access is still restricted by internal network check and whitelist.

            HttpContext.Session.SetInt32("PendingUserId", user.Id);
            HttpContext.Session.SetString("PendingRememberMe", model.RememberMe ? "1" : "0");
            await WriteAuthAuditLog(user.Id, user.Email, "LoginSuccess", true, "Employee code accepted, awaiting biometric verification");

            var demoSkipBiometric = string.Equals(Environment.GetEnvironmentVariable("DEMO_SKIP_BIOMETRIC"), "1", StringComparison.OrdinalIgnoreCase);
            if (demoSkipBiometric && string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("FinalizeLogin");
            }

            if (!user.IsBiometricEnrolled)
            {
                return RedirectToAction("EnrollBiometrics");
            }

            return RedirectToAction("VerifyBiometrics");
        }

        [HttpGet]
        public async Task<IActionResult> RegisterEmail()
        {
            var model = new RegisterEmailViewModel
            {
                Departments = await _context.Departments.Where(d => d.Id != 1).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RegisterEmail(RegisterEmailViewModel model)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký hoặc đang chờ duyệt.");
                model.Departments = await _context.Departments.Where(d => d.Id != 1).ToListAsync();
                return View(model);
            }

            var tenantId = 1; // Default tenant
            var user = new User
            {
                TenantId = tenantId,
                FullName = model.FullName,
                Email = model.Email,
                DepartmentId = model.DepartmentId,
                Status = "Active",
                PasswordHash = HashPassword(model.Password),
                EmployeeCode = "TEMP",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(user.EmployeeCode) || user.EmployeeCode == "TEMP" || user.EmployeeCode == "PENDING")
            {
                user.EmployeeCode = $"EMP{user.Id:D4}";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            await WriteAuthAuditLog(user.Id, user.Email, "RegisterSubmitted", true, "New account registered (auto-activated)");

            TempData["Success"] = $"Đăng ký thành công! Mã nhân viên của bạn là: {user.EmployeeCode}";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var email = HttpContext.Session.GetString("Email");
            if (userId.HasValue)
            {
                await MarkCurrentSessionOfflineAsync(userId.Value);
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    user.RememberMeTokenHash = null;
                    user.RememberMeExpiresAt = null;
                    await _context.SaveChangesAsync();
                }
            }

            _ = WriteAuthAuditLog(userId, email, "Logout", true, "User logged out");
            HttpContext.Session.Clear();
            Response.Cookies.Delete(RememberMeCookieName);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = _context.Users
                .Include(u => u.Department)
                .Include(u => u.Branch)
                .Include(u => u.JobTitle)
                .Include(u => u.Position)
                .FirstOrDefault(u => u.Id == userId);

            var leaveBalances = _context.LeaveBalances
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.UserId == userId && lb.Year == DateTime.Now.Year)
                .ToList();

            var attendanceHistory = _context.Timesheets
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(10)
                .ToList();

            var model = new ProfileViewModel
            {
                User = user,
                LeaveBalances = leaveBalances,
                AttendanceHistory = attendanceHistory,
                TwoFactorEnabled = user?.TwoFactorEnabled ?? false
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Signature()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var profile = await _context.DigitalSignatureProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId.Value && p.IsActive);

            var model = new SignatureProfileViewModel
            {
                SignatureName = profile?.SignatureName
                    ?? (HttpContext.Session.GetString("FullName") ?? "Chữ ký điện tử"),
                ExistingSignatureImageUrl = profile?.SignatureImageUrl
            };

            ViewData["Title"] = "Chữ ký điện tử";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> HandSign()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var profile = await _context.DigitalSignatureProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId.Value && p.IsActive);

            var model = new SignatureProfileViewModel
            {
                SignatureName = profile?.SignatureName
                    ?? (HttpContext.Session.GetString("FullName") ?? "Chữ ký điện tử"),
                ExistingSignatureImageUrl = profile?.SignatureImageUrl
            };

            ViewData["Title"] = "Tự ký bằng tay";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSignature()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var profile = await _context.DigitalSignatureProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId.Value && p.IsActive);

            if (profile == null || string.IsNullOrWhiteSpace(profile.SignatureImageUrl))
            {
                TempData["Error"] = "Bạn chưa có chữ ký để tải về.";
                return RedirectToAction(nameof(HandSign));
            }

            var normalizedPath = profile.SignatureImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_env.WebRootPath, normalizedPath);
            if (!System.IO.File.Exists(physicalPath))
            {
                TempData["Error"] = "Không tìm thấy file chữ ký trên hệ thống.";
                return RedirectToAction(nameof(HandSign));
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            var fileName = $"chu-ky-{userId.Value}.png";
            return File(bytes, "image/png", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signature(SignatureProfileViewModel model, IFormFile? signatureFile)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            if (string.IsNullOrWhiteSpace(model.SignatureName))
            {
                ModelState.AddModelError(nameof(model.SignatureName), "Vui lòng nhập tên chữ ký.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Title"] = "Chữ ký điện tử";
                return View(model);
            }

            byte[]? signatureBytes = null;
            string fileExt = ".png";

            if (signatureFile != null && signatureFile.Length > 0)
            {
                if (signatureFile.Length > 500_000)
                {
                    ModelState.AddModelError(string.Empty, "File chữ ký quá lớn (tối đa 500KB).");
                    ViewData["Title"] = "Chữ ký điện tử";
                    return View(model);
                }

                fileExt = Path.GetExtension(signatureFile.FileName);
                if (string.IsNullOrWhiteSpace(fileExt)) fileExt = ".png";
                fileExt = fileExt.ToLowerInvariant();
                if (fileExt != ".png" && fileExt != ".jpg" && fileExt != ".jpeg")
                {
                    ModelState.AddModelError(string.Empty, "Chỉ hỗ trợ ảnh chữ ký dạng PNG/JPG.");
                    ViewData["Title"] = "Chữ ký điện tử";
                    return View(model);
                }

                using var ms = new MemoryStream();
                await signatureFile.CopyToAsync(ms);
                signatureBytes = ms.ToArray();
            }
            else if (!string.IsNullOrWhiteSpace(model.SignatureDataUrl))
            {
                var dataUrl = model.SignatureDataUrl.Trim();
                const string prefix = "data:image/png;base64,";
                const string prefixJpg = "data:image/jpeg;base64,";
                const string prefixJpg2 = "data:image/jpg;base64,";

                string? base64 = null;
                if (dataUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    base64 = dataUrl[prefix.Length..];
                    fileExt = ".png";
                }
                else if (dataUrl.StartsWith(prefixJpg, StringComparison.OrdinalIgnoreCase))
                {
                    base64 = dataUrl[prefixJpg.Length..];
                    fileExt = ".jpg";
                }
                else if (dataUrl.StartsWith(prefixJpg2, StringComparison.OrdinalIgnoreCase))
                {
                    base64 = dataUrl[prefixJpg2.Length..];
                    fileExt = ".jpg";
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Dữ liệu chữ ký không hợp lệ.");
                    ViewData["Title"] = "Chữ ký điện tử";
                    return View(model);
                }

                try
                {
                    signatureBytes = Convert.FromBase64String(base64);
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "Không thể đọc dữ liệu chữ ký.");
                    ViewData["Title"] = "Chữ ký điện tử";
                    return View(model);
                }

                if (signatureBytes.Length > 500_000)
                {
                    ModelState.AddModelError(string.Empty, "Chữ ký quá lớn (tối đa 500KB).");
                    ViewData["Title"] = "Chữ ký điện tử";
                    return View(model);
                }
            }

            var existing = await _context.DigitalSignatureProfiles
                .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId.Value && p.IsActive);

            string? signatureUrl = existing?.SignatureImageUrl;
            if (signatureBytes != null)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", tenantId.ToString(), "signatures");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"sig_user_{userId.Value}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExt}";
                var physicalPath = Path.Combine(uploadsDir, fileName);
                await System.IO.File.WriteAllBytesAsync(physicalPath, signatureBytes);
                signatureUrl = $"/uploads/{tenantId}/signatures/{fileName}";

                if (!string.IsNullOrWhiteSpace(existing?.SignatureImageUrl))
                {
                    try { _fileService.DeleteFile(existing.SignatureImageUrl); } catch { }
                }
            }

            if (existing == null)
            {
                existing = new DigitalSignatureProfile
                {
                    TenantId = tenantId,
                    UserId = userId.Value,
                    ProviderName = signatureFile != null ? "Upload" : "HandDrawn",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.DigitalSignatureProfiles.Add(existing);
            }
            else if (string.IsNullOrWhiteSpace(existing.ProviderName))
            {
                existing.ProviderName = signatureFile != null ? "Upload" : "HandDrawn";
            }

            existing.SignatureName = model.SignatureName.Trim();
            existing.SignatureImageUrl = signatureUrl;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật chữ ký điện tử.";
            return RedirectToAction(nameof(Signature));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToAction("Login");

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (model.NewPassword != model.ConfirmPassword)
                {
                    TempData["Error"] = "Mật khẩu xác nhận không khớp.";
                    return RedirectToAction("Profile");
                }
                user.PasswordHash = HashPassword(model.NewPassword);
                _context.PasswordHistories.Add(new DANGCAPNE.Models.Security.PasswordHistory
                {
                    TenantId = user.TenantId,
                    UserId = user.Id,
                    PasswordHash = user.PasswordHash,
                    ChangedByUserId = user.Id,
                    ChangeSource = "SelfService"
                });
            }

            user.Phone = model.User?.Phone ?? user.Phone;
            user.TwoFactorEnabled = model.TwoFactorEnabled;
            user.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            {
                _context.ChangeTracker.Clear();
                await SchemaPatchRunner.EnsureExtendedSchemaAsync(_context);

                var reloadedUser = await _context.Users.FindAsync(userId);
                if (reloadedUser == null) return RedirectToAction("Login");

                reloadedUser.Phone = model.User?.Phone ?? reloadedUser.Phone;
                reloadedUser.TwoFactorEnabled = model.TwoFactorEnabled;
                reloadedUser.UpdatedAt = DateTime.Now;

                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    reloadedUser.PasswordHash = HashPassword(model.NewPassword);
                    _context.PasswordHistories.Add(new DANGCAPNE.Models.Security.PasswordHistory
                    {
                        TenantId = reloadedUser.TenantId,
                        UserId = reloadedUser.Id,
                        PasswordHash = reloadedUser.PasswordHash,
                        ChangedByUserId = reloadedUser.Id,
                        ChangeSource = "SelfService"
                    });
                }

                await _context.SaveChangesAsync();
                user = reloadedUser;
            }
            TempData["Success"] = "Cập nhật thông tin thành công!";
            HttpContext.Session.SetString("FullName", user.FullName);

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Phiên làm việc hết hạn." });

            if (file == null || file.Length == 0) return Json(new { success = false, message = "Vui lòng chọn ảnh." });

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return Json(new { success = false, message = "Người dùng không tồn tại." });

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    _fileService.DeleteFile(user.AvatarUrl);
                }

                // Lưu ảnh mới
                var fileUrl = await _fileService.SaveFileAsync(file, "avatars");
                user.AvatarUrl = fileUrl;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                
                // Cập nhật session
                HttpContext.Session.SetString("Avatar", fileUrl);

                return Json(new { success = true, url = fileUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tải ảnh: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "DANGCAPNE_SALT"));
            return Convert.ToBase64String(bytes);
        }

        private async Task WriteAuthAuditLog(int? userId, string? email, string action, bool isSuccess, string? details = null)
        {
            try
            {
                var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
                await SaveAuthAuditLogInternalAsync(new DANGCAPNE.Models.Security.AuthAuditLog
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Email = email ?? string.Empty,
                    Action = action,
                    IsSuccess = isSuccess,
                    Details = details,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString()
                });
            }
            catch
            {
                // Ignore audit log errors in authentication flow.
            }
        }

        private async Task SaveAuthAuditLogInternalAsync(DANGCAPNE.Models.Security.AuthAuditLog log)
        {
            try
            {
                _context.AuthAuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            {
                _context.ChangeTracker.Clear();
                await SchemaPatchRunner.EnsureExtendedSchemaAsync(_context);
                _context.AuthAuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }

        [HttpGet]
        public async Task<IActionResult> EnrollBiometrics()
        {
            var userId = HttpContext.Session.GetInt32("PendingUserId");
            if (userId == null) return RedirectToAction("Login");
            
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.IsBiometricEnrolled) return RedirectToAction("Login");

            return View(user);
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> SaveBiometrics([FromBody] BiometricSaveRequest req)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("PendingUserId");
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // 1:N Duplicate Check - Ensure this face isn't already registered
            var otherUsers = await _context.Users
                .Where(u => u.IsBiometricEnrolled && u.Id != user.Id && !string.IsNullOrEmpty(u.FaceDescriptorFront))
                .Select(u => new { u.Id, u.FullName, u.Email, u.FaceDescriptorFront, u.FaceDescriptorLeft, u.FaceDescriptorRight })
                .ToListAsync();

            var newFront = System.Text.Json.JsonSerializer.Deserialize<float[]>(req.Front ?? "[]");
            var newLeft = System.Text.Json.JsonSerializer.Deserialize<float[]>(req.Left ?? "[]");
            var newRight = System.Text.Json.JsonSerializer.Deserialize<float[]>(req.Right ?? "[]");

            var newDescriptors = new List<float[]>();
            if (newFront != null && newFront.Length > 0) newDescriptors.Add(newFront);
            if (newLeft != null && newLeft.Length > 0) newDescriptors.Add(newLeft);
            if (newRight != null && newRight.Length > 0) newDescriptors.Add(newRight);

            if (newDescriptors.Count > 0)
            {
                foreach (var other in otherUsers)
                {
                    var otherDescriptors = new List<float[]>();

                    var otherFront = System.Text.Json.JsonSerializer.Deserialize<float[]>(other.FaceDescriptorFront ?? "[]");
                    if (otherFront != null && otherFront.Length > 0) otherDescriptors.Add(otherFront);

                    var otherLeft = System.Text.Json.JsonSerializer.Deserialize<float[]>(other.FaceDescriptorLeft ?? "[]");
                    if (otherLeft != null && otherLeft.Length > 0) otherDescriptors.Add(otherLeft);

                    var otherRight = System.Text.Json.JsonSerializer.Deserialize<float[]>(other.FaceDescriptorRight ?? "[]");
                    if (otherRight != null && otherRight.Length > 0) otherDescriptors.Add(otherRight);

                    if (otherDescriptors.Count == 0) continue;

                    float best = 99f;
                    foreach (var nd in newDescriptors)
                    {
                        foreach (var od in otherDescriptors)
                        {
                            float dist = CalculateEuclideanDistance(nd, od);
                            if (dist < best) best = dist;
                        }
                    }

                    Console.WriteLine($"[FaceEnroll] New vs ID {other.Id} ({other.FullName}) bestDistance: {best:F4}");

                    // Enrollment duplicate threshold: intentionally strict to prevent cross-account confusion later.
                    if (best < 0.45f)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Lỗi bảo mật: Khuôn mặt này đã được đăng ký bởi tài khoản: {other.FullName} ({other.Email})!"
                        });
                    }
                }
            }

            user.FaceDescriptorFront = req.Front;
            user.FaceDescriptorLeft = req.Left;
            user.FaceDescriptorRight = req.Right;
            user.PortraitImage = req.Portrait;
            user.AvatarUrl = req.Portrait ?? string.Empty; // Set as account avatar as requested
            user.TrustedDeviceId = Guid.NewGuid().ToString();
            user.IsBiometricEnrolled = true;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            
            // Set cookie for Trust Device
            var cookieOptions = new CookieOptions { Expires = DateTime.Now.AddYears(1), HttpOnly = true };
            Response.Cookies.Append($"TrustDevice_{user.Id}", user.TrustedDeviceId, cookieOptions);

                return Ok(new { success = true });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = "Lỗi hệ thống khi lưu dữ liệu: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetBiometrics(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FaceDescriptorFront = null;
            user.FaceDescriptorLeft = null;
            user.FaceDescriptorRight = null;
            user.IsBiometricEnrolled = false;
            user.PortraitImage = null;
            user.AvatarUrl = string.Empty;
            user.TrustedDeviceId = null;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = $"Đã xóa dữ liệu sinh trắc của {user.FullName}" });
        }

        [HttpGet]
        public async Task<IActionResult> GetFaceDistances()
        {
            var users = await _context.Users
                .Where(u => u.IsBiometricEnrolled && !string.IsNullOrEmpty(u.FaceDescriptorFront))
                .Select(u => new { u.Id, u.FullName, u.FaceDescriptorFront })
                .ToListAsync();

            var results = new List<string>();
            for (int i = 0; i < users.Count; i++)
            {
                for (int j = i + 1; j < users.Count; j++)
                {
                    try {
                        var d1 = System.Text.Json.JsonSerializer.Deserialize<float[]>(users[i].FaceDescriptorFront ?? "[]");
                        var d2 = System.Text.Json.JsonSerializer.Deserialize<float[]>(users[j].FaceDescriptorFront ?? "[]");
                        float dist = CalculateEuclideanDistance(d1!, d2!);
                        results.Add($"{users[i].FullName} vs {users[j].FullName}: {dist}");
                    } catch {
                        results.Add($"Error calculating distance between UID {users[i].Id} and UID {users[j].Id}");
                    }
                }
            }
            return Ok(results);
        }

        [HttpPost]
        public async Task<IActionResult> CheckUniqueness([FromBody] UniquenessRequest req)
        {
            var userId = HttpContext.Session.GetInt32("PendingUserId");
            if (userId == null) return Unauthorized();

            var newFront = System.Text.Json.JsonSerializer.Deserialize<float[]>(req.Front ?? "[]");
            if (newFront == null || newFront.Length == 0) 
                return BadRequest(new { success = false, message = "Dữ liệu khuôn mặt không hợp lệ." });

            var otherUsers = await _context.Users
                .Where(u => u.IsBiometricEnrolled && u.Id != userId && !string.IsNullOrEmpty(u.FaceDescriptorFront))
                .Select(u => new { u.Id, u.FaceDescriptorFront })
                .ToListAsync();

            foreach (var other in otherUsers)
            {
                var otherFront = System.Text.Json.JsonSerializer.Deserialize<float[]>(other.FaceDescriptorFront ?? "[]");
                if (otherFront == null || otherFront.Length == 0) continue;

                float dist = CalculateEuclideanDistance(newFront, otherFront);
                Console.WriteLine($"[FaceUniqueness] New vs ID {other.Id}. Distance: {dist}");
                
                // Ngưỡng độc nhất rất nghiêm ngặt: < 0.38 mới coi là trùng lặp.
                if (dist < 0.38)
                {
                    var matchUser = await _context.Users.FindAsync(other.Id);
                    return Ok(new { success = false, message = $"Lỗi bảo mật: Khuôn mặt này đã được đăng ký bởi tài khoản: {matchUser?.FullName} ({matchUser?.Email})!" });
                }
            }

            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> VerifyBiometrics()
        {
            var userId = HttpContext.Session.GetInt32("PendingUserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsBiometricEnrolled) return RedirectToAction("Login");
            
            var trustCookie = Request.Cookies[$"TrustDevice_{user.Id}"];
            ViewBag.IsTrustedDevice = (!string.IsNullOrEmpty(trustCookie) && trustCookie == user.TrustedDeviceId);

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyFaceMatch([FromBody] FaceMatchRequest req)
        {
            var userId = HttpContext.Session.GetInt32("PendingUserId");
            if (userId == null) return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsBiometricEnrolled) return NotFound();

            try {
                var live = System.Text.Json.JsonSerializer.Deserialize<float[]>(req.LiveDescriptor ?? "[]");
                if (live == null || live.Length == 0)
                    return BadRequest(new { success = false, message = "Dữ liệu sinh trắc không hợp lệ." });

                List<float[]> GetDescriptors(string? raw)
                {
                    var list = new List<float[]>();
                    if (string.IsNullOrWhiteSpace(raw)) return list;
                    try
                    {
                        var parsed = System.Text.Json.JsonSerializer.Deserialize<float[]>(raw);
                        if (parsed != null && parsed.Length == live.Length)
                            list.Add(parsed);
                    }
                    catch { }
                    return list;
                }

                var userDescriptors = new List<float[]>();
                userDescriptors.AddRange(GetDescriptors(user.FaceDescriptorFront));
                userDescriptors.AddRange(GetDescriptors(user.FaceDescriptorLeft));
                userDescriptors.AddRange(GetDescriptors(user.FaceDescriptorRight));

                if (userDescriptors.Count == 0)
                    return BadRequest(new { success = false, message = "Tài khoản chưa có dữ liệu sinh trắc học hợp lệ." });

                float bestClaimedDistance = 99f;
                foreach (var descriptor in userDescriptors)
                {
                    var dist = CalculateEuclideanDistance(live, descriptor);
                    if (dist < bestClaimedDistance) bestClaimedDistance = dist;
                }

                var isStaffOnly = !user.UserRoles.Any(ur => ur.Role != null && 
                    (ur.Role.Name == "Admin" || ur.Role.Name == "Manager" || ur.Role.Name == "HR" || ur.Role.Name == "Accountant"));

                float acceptThreshold = isStaffOnly ? 0.55f : 0.42f;
                const float strongMatchThreshold = 0.38f;
                const float otherStrongThreshold = 0.38f;
                const float betterMatchMargin = 0.06f;

                float bestOtherDistance = 99f;
                int? bestOtherUserId = null;
                string? bestOtherUserName = null;

                var otherUsers = await _context.Users
                    .Where(u => u.IsBiometricEnrolled && u.Id != user.Id)
                    .Select(u => new { u.Id, u.FullName, u.FaceDescriptorFront, u.FaceDescriptorLeft, u.FaceDescriptorRight })
                    .ToListAsync();

                foreach (var other in otherUsers)
                {
                    var otherDescriptors = new List<float[]>();
                    otherDescriptors.AddRange(GetDescriptors(other.FaceDescriptorFront));
                    otherDescriptors.AddRange(GetDescriptors(other.FaceDescriptorLeft));
                    otherDescriptors.AddRange(GetDescriptors(other.FaceDescriptorRight));
                    if (otherDescriptors.Count == 0) continue;

                    foreach (var descriptor in otherDescriptors)
                    {
                        var dist = CalculateEuclideanDistance(live, descriptor);
                        if (dist < bestOtherDistance)
                        {
                            bestOtherDistance = dist;
                            bestOtherUserId = other.Id;
                            bestOtherUserName = other.FullName;
                        }
                    }
                }

                Console.WriteLine($"[FaceVerify] User {user.Id} ({user.FullName}) - bestClaimed: {bestClaimedDistance:F4}, bestOther: {bestOtherDistance:F4} (uid={bestOtherUserId})");

                if (bestClaimedDistance > acceptThreshold)
                    return BadRequest(new { success = false, message = $"Khuôn mặt không khớp! (Độ lệch: {bestClaimedDistance:F2}). Vui lòng thử lại." });

                var claimedStrong = bestClaimedDistance <= strongMatchThreshold;
                var otherStrong = bestOtherDistance <= otherStrongThreshold;
                var otherClearlyBetter = bestOtherDistance + betterMatchMargin < bestClaimedDistance;
                if (!claimedStrong && otherStrong && otherClearlyBetter)
                    return BadRequest(new { success = false, message = $"Xác thực thất bại: khuôn mặt này gần với tài khoản khác ({bestOtherUserName ?? "Unknown"})." });

                // AUTO CHECK-IN: If face matches during login, record it as a check-in
                var today = DateTime.Today;
                var existingTimesheet = await _context.Timesheets
                    .FirstOrDefaultAsync(t => t.UserId == user.Id && t.Date == today);

                if (existingTimesheet == null)
                {
                    var now = DateTime.Now;
                    var targetCheckIn = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);

                    _context.Timesheets.Add(new DANGCAPNE.Models.Timekeeping.Timesheet
                    {
                        TenantId = user.TenantId,
                        UserId = user.Id,
                        Date = today,
                        CheckIn = now,
                        Source = "FaceRecognition",
                        Status = now > targetCheckIn ? "Late" : "Present",
                        UpdatedAt = now
                    });
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, distance = bestClaimedDistance, checkInDone = (existingTimesheet == null) });
            } catch {
                return BadRequest(new { success = false, message = "Lỗi xử lý dữ liệu AI." });
            }
        }

        private float CalculateEuclideanDistance(float[] p1, float[] p2)
        {
            if (p1.Length != p2.Length) return 99f;
            float sum = 0;
            for (int i = 0; i < p1.Length; i++)
            {
                sum += (p1[i] - p2[i]) * (p1[i] - p2[i]);
            }
            return (float)Math.Sqrt(sum);
        }

                [HttpGet]
        public async Task<IActionResult> FinalizeLogin()
        {
            var userId = HttpContext.Session.GetInt32("PendingUserId");
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return RedirectToAction("Login");

            ApplyUserSession(user);

            if (HttpContext.Session.GetString("PendingRememberMe") == "1")
            {
                SetRememberMeCookie(user);
            }
            else
            {
                user.RememberMeTokenHash = null;
                user.RememberMeExpiresAt = null;
                Response.Cookies.Delete(RememberMeCookieName);
            }

            await _context.SaveChangesAsync();
            await TrackOnlineSessionAsync(user);

            HttpContext.Session.Remove("PendingUserId");
            HttpContext.Session.Remove("PendingRememberMe");

            return RedirectToHomeBySessionRole();
        }

        private IActionResult RedirectToHomeBySessionRole()
        {
            var primaryRole = HttpContext.Session.GetString("PrimaryRole");
            if (string.Equals(primaryRole, "Employee", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Attendance");
            }

            return RedirectToAction("Index", "Home");
        }

        private void ApplyUserSession(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetInt32("TenantId", user.TenantId);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Avatar", user.AvatarUrl ?? "");
            HttpContext.Session.SetString("EmployeeCode", user.EmployeeCode);
            HttpContext.Session.SetString("Department", user.Department?.Name ?? "");

            var roles = user.UserRoles.Select(ur => ur.Role?.Name ?? "").Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
            var primaryRole = roles.FirstOrDefault() ?? "Employee";

            var isChiefAccountant =
                roles.Contains("ChiefAccountant") ||
                string.Equals(user.Position?.Name, "Kế toán trưởng", StringComparison.OrdinalIgnoreCase);

            var isAccountantStaff = roles.Contains("AccountantStaff");
            var isAccountant =
                roles.Contains("Accountant") ||
                isChiefAccountant ||
                isAccountantStaff ||
                string.Equals(user.Department?.Code, "ACC", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.Department?.Name, "Phòng Kế toán", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.Position?.Name, "Kế toán trưởng", StringComparison.OrdinalIgnoreCase) ||
                user.Email.Contains("accountant", StringComparison.OrdinalIgnoreCase);

            if (isAccountant)
            {
                primaryRole = "Accountant";
            }

            var permissions = PermissionCatalog.ResolvePermissions(user, roles, primaryRole);

            HttpContext.Session.SetString("Roles", string.Join(",", roles));
            HttpContext.Session.SetString("PrimaryRole", primaryRole);
            HttpContext.Session.SetString("Permissions", string.Join(",", permissions));
            HttpContext.Session.SetString("IsChiefAccountant", isChiefAccountant ? "1" : "0");
            HttpContext.Session.SetString("IsAccountantStaff", isAccountantStaff ? "1" : "0");
        }

        private static string? NormalizeIp(string? ipText)
        {
            if (string.IsNullOrWhiteSpace(ipText))
            {
                return null;
            }

            if (!System.Net.IPAddress.TryParse(ipText.Trim(), out var ip))
            {
                return null;
            }

            // Treat IPv4/IPv6 loopback as the same value for first-time IP registration.
            if (System.Net.IPAddress.IsLoopback(ip))
            {
                return "127.0.0.1";
            }

            return ip.MapToIPv6().IsIPv4MappedToIPv6
                ? ip.MapToIPv4().ToString()
                : ip.ToString();
        }

        private static bool IsInternalNetwork(System.Net.IPAddress? ip)
        {
            if (ip == null)
                return false;

            if (System.Net.IPAddress.IsLoopback(ip))
                return true;

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                if (bytes.Length == 4)
                {
                    if (bytes[0] == 10)
                        return true;
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                        return true;
                    if (bytes[0] == 192 && bytes[1] == 168)
                        return true;
                }
            }

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                var text = ip.ToString();
                if (text.StartsWith("fc", StringComparison.OrdinalIgnoreCase) || text.StartsWith("fd", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string HashRememberToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{token}|DANGCAPNE_REMEMBER"));
            return Convert.ToBase64String(bytes);
        }

        private void SetRememberMeCookie(User user)
        {
            var token = Guid.NewGuid().ToString("N");
            user.RememberMeTokenHash = HashRememberToken(token);
            user.RememberMeExpiresAt = DateTime.UtcNow.AddDays(RememberMeDays);

            Response.Cookies.Append(RememberMeCookieName, $"{user.Id}.{token}", new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(RememberMeDays),
                IsEssential = true
            });
        }

        private async Task<bool> TryLoginFromRememberCookieAsync()
        {
            if (!IsInternalNetwork(HttpContext.Connection.RemoteIpAddress))
            {
                return false;
            }

            if (!Request.Cookies.TryGetValue(RememberMeCookieName, out var rememberValue) || string.IsNullOrWhiteSpace(rememberValue))
            {
                return false;
            }

            var parts = rememberValue.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var userId))
            {
                Response.Cookies.Delete(RememberMeCookieName);
                return false;
            }

            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId && u.Status == "Active");

            if (user == null || string.IsNullOrWhiteSpace(user.RememberMeTokenHash) || !user.RememberMeExpiresAt.HasValue || user.RememberMeExpiresAt < DateTime.UtcNow)
            {
                Response.Cookies.Delete(RememberMeCookieName);
                return false;
            }

            var computedHash = HashRememberToken(parts[1]);
            if (!string.Equals(user.RememberMeTokenHash, computedHash, StringComparison.Ordinal))
            {
                Response.Cookies.Delete(RememberMeCookieName);
                return false;
            }

            ApplyUserSession(user);
            await TrackOnlineSessionAsync(user);
            return true;
        }

        private async Task TrackOnlineSessionAsync(User user)
        {
            try
            {
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers.UserAgent.ToString();

                var existing = await _context.EmployeeOnlineSessions
                    .Where(s => s.UserId == user.Id && s.Status == "Online")
                    .OrderByDescending(s => s.LastSeenAt)
                    .FirstOrDefaultAsync();

                if (existing == null)
                {
                    _context.EmployeeOnlineSessions.Add(new EmployeeOnlineSession
                    {
                        TenantId = user.TenantId,
                        UserId = user.Id,
                        SessionToken = HttpContext.Session.Id,
                        LoginAt = DateTime.UtcNow,
                        LastSeenAt = DateTime.UtcNow,
                        Status = "Online",
                        LastIpAddress = remoteIp,
                        DeviceName = string.IsNullOrWhiteSpace(userAgent) ? "Unknown" : userAgent[..Math.Min(250, userAgent.Length)]
                    });
                }
                else
                {
                    existing.LastSeenAt = DateTime.UtcNow;
                    existing.LastIpAddress = remoteIp;
                    existing.DeviceName = string.IsNullOrWhiteSpace(userAgent) ? existing.DeviceName : userAgent[..Math.Min(250, userAgent.Length)];
                    existing.Status = "Online";
                }

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Keep auth flow resilient even if session tracking fails.
            }
        }

        private async Task MarkCurrentSessionOfflineAsync(int userId)
        {
            try
            {
                var currentToken = HttpContext.Session.Id;
                var session = await _context.EmployeeOnlineSessions
                    .Where(s => s.UserId == userId && s.Status == "Online")
                    .OrderByDescending(s => s.LastSeenAt)
                    .FirstOrDefaultAsync(s => s.SessionToken == currentToken)
                    ?? await _context.EmployeeOnlineSessions
                        .Where(s => s.UserId == userId && s.Status == "Online")
                        .OrderByDescending(s => s.LastSeenAt)
                        .FirstOrDefaultAsync();

                if (session != null)
                {
                    session.Status = "Offline";
                    session.LogoutAt = DateTime.UtcNow;
                    session.LastSeenAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                // Ignore to avoid blocking logout.
            }
        }
    }

    public class BiometricSaveRequest
    {
        public string? Front { get; set; }
        public string? Left { get; set; }
        public string? Right { get; set; }
        public string? Portrait { get; set; }
    }

    public class UniquenessRequest
    {
        public string? Front { get; set; }
    }

    public class FaceMatchRequest
    {
        public string? LiveDescriptor { get; set; }
    }
}


