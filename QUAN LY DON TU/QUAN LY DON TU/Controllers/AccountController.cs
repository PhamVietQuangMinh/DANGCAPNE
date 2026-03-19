using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.ViewModels;
using DANGCAPNE.Models.Organization;
using Microsoft.AspNetCore.SignalR;
using DANGCAPNE.Hubs;
namespace DANGCAPNE.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.IFileService _fileService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AccountController(ApplicationDbContext context, Services.IFileService fileService, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _fileService = fileService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin đăng nhập.";
                return View(model);
            }

            var passwordHash = HashPassword(model.Password);
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordHash == passwordHash);

            if (user == null)
            {
                ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
                return View(model);
            }

            if (user.Status == "PendingApproval")
            {
                ViewBag.Error = "Tài khoản của bạn đang chờ quản lý phê duyệt. Vui lòng quay lại sau.";
                return View(model);
            }

            if (user.Status == "Rejected")
            {
                ViewBag.Error = "Yêu cầu đăng ký của bạn đã bị từ chối. Vui lòng liên hệ bộ phận nhân sự.";
                return View(model);
            }

            if (user.Status != "Active")
            {
                ViewBag.Error = "Tài khoản của bạn hiện đang bị khóa hoặc không khả dụng.";
                return View(model);
            }

            // Biometric Interception
            HttpContext.Session.SetInt32("PendingUserId", user.Id);

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
                Status = "PendingApproval",
                PasswordHash = HashPassword(model.Password),
                EmployeeCode = "PENDING",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Notify HR, Admin, and Managers
            var hrAdmins = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.Name == "Admin" || ur.Role.Name == "HR" || ur.Role.Name == "Manager")
                .Select(ur => ur.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var hrId in hrAdmins)
            {
                _context.Notifications.Add(new Models.SystemModels.Notification
                {
                    TenantId = tenantId,
                    UserId = hrId,
                    Title = "Yêu cầu đăng ký mới",
                    Message = $"{model.FullName} ({model.Email}) vừa đăng ký tài khoản mới.",
                    Type = "Info",
                    ActionUrl = "/Admin/Whitelist"
                });

                await _hubContext.Clients.Group($"user_{hrId}").SendAsync("ReceiveNotification", new
                {
                    title = "Yêu cầu đăng ký mới",
                    message = $"{model.FullName} ({model.Email}) vừa đăng ký tài khoản.",
                    type = "Info",
                    actionUrl = "/Admin/Whitelist"
                });
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "Yêu cầu của bạn đã được gửi thành công! Vui lòng chờ quản lý phê duyệt email trước khi đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
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
            }

            user.Phone = model.User?.Phone ?? user.Phone;
            user.TwoFactorEnabled = model.TwoFactorEnabled;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
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
                .Select(u => new { u.Id, u.FaceDescriptorFront })
                .ToListAsync();

            var newFront = System.Text.Json.JsonSerializer.Deserialize<float[]>(req.Front ?? "[]");
            if (newFront != null && newFront.Length > 0)
            {
                foreach (var other in otherUsers)
                {
                    var otherFront = System.Text.Json.JsonSerializer.Deserialize<float[]>(other.FaceDescriptorFront ?? "[]");
                    if (otherFront == null || otherFront.Length == 0) continue;

                    float dist = CalculateEuclideanDistance(newFront, otherFront);
                    if (dist < 0.55) // threshold for matching same person
                    {
                        return BadRequest(new { success = false, message = "Lỗi bảo mật: Khuôn mặt này đã được đăng ký bởi một tài khoản khác trong hệ thống!" });
                    }
                }
            }

            user.FaceDescriptorFront = req.Front;
            user.FaceDescriptorLeft = req.Left;
            user.FaceDescriptorRight = req.Right;
            user.PortraitImage = req.Portrait;
            user.AvatarUrl = req.Portrait; // Set as account avatar as requested
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
                if (dist < 0.55)
                {
                    return Ok(new { success = false, message = "Lỗi bảo mật: Khuôn mặt này đã được đăng ký bởi một tài khoản khác trong hệ thống!" });
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

            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsBiometricEnrolled) return NotFound();

            try {
                var live = System.Text.Json.JsonSerializer.Deserialize<float[]>(req.LiveDescriptor ?? "[]");
                var front = System.Text.Json.JsonSerializer.Deserialize<float[]>(user.FaceDescriptorFront ?? "[]");
                var left = System.Text.Json.JsonSerializer.Deserialize<float[]>(user.FaceDescriptorLeft ?? "[]");
                var right = System.Text.Json.JsonSerializer.Deserialize<float[]>(user.FaceDescriptorRight ?? "[]");

                if (live == null || live.Length == 0 || front == null || front.Length == 0)
                    return BadRequest(new { success = false, message = "Dữ liệu sinh trắc không hợp lệ." });

                // Tính khoảng cách Euclidean
                float distFront = CalculateEuclideanDistance(live, front);
                float distLeft = CalculateEuclideanDistance(live, left);
                float distRight = CalculateEuclideanDistance(live, right);

                float minDistance = Math.Min(distFront, Math.Min(distLeft, distRight));

                // Ngưỡng tiêu chuẩn của face-api.js là 0.6. Dưới 0.6 là cùng một người.
                if (minDistance > 0.55) 
                    return BadRequest(new { success = false, message = "Khuôn mặt không khớp! Vui lòng thử lại." });

                return Ok(new { success = true, distance = minDistance });
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
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (user == null) return RedirectToAction("Login");

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetInt32("TenantId", user.TenantId);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Avatar", user.AvatarUrl ?? "");
            HttpContext.Session.SetString("EmployeeCode", user.EmployeeCode);
            HttpContext.Session.SetString("Department", user.Department?.Name ?? "");

            var roles = user.UserRoles.Select(ur => ur.Role?.Name ?? "").ToList();
            HttpContext.Session.SetString("Roles", string.Join(",", roles));
            HttpContext.Session.SetString("PrimaryRole", roles.FirstOrDefault() ?? "Employee");

            HttpContext.Session.Remove("PendingUserId");

            return RedirectToAction("Index", "Home");
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
