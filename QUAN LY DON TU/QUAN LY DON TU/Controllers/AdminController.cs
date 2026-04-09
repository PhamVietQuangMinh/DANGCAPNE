using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.Models.Organization;
using DANGCAPNE.Models.Workflow;
using DANGCAPNE.Models.Timekeeping;
using DANGCAPNE.ViewModels;
using Microsoft.AspNetCore.SignalR;
using DANGCAPNE.Hubs;
using System.Text;
using DANGCAPNE.Models.SystemModels;
namespace DANGCAPNE.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AdminController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index(
            string tab = "users",
            string? employeeSearch = null,
            int? employeeDepartmentId = null,
            string? attendanceSearch = null,
            int? attendanceDepartmentId = null,
            DateTime? attendanceFromDate = null,
            DateTime? attendanceToDate = null,
            DateTime? requestStatsFromDate = null,
            DateTime? requestStatsToDate = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager")) return RedirectToAction("AccessDenied", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var isAdmin = roles.Contains("Admin");
            tab = tab?.Trim().ToLowerInvariant() switch
            {
                "timekeeping" => "timekeeping",
                "requeststats" when isAdmin => "requeststats",
                _ => "users"
            };

            var normalizedEmployeeSearch = employeeSearch?.Trim();
            var normalizedAttendanceSearch = attendanceSearch?.Trim();
            var fromDate = attendanceFromDate?.Date ?? DateTime.Today.AddDays(-30);
            var toDate = attendanceToDate?.Date ?? DateTime.Today;
            var requestFrom = requestStatsFromDate?.Date ?? DateTime.Today.AddDays(-30);
            var requestTo = requestStatsToDate?.Date ?? DateTime.Today;
            if (requestTo < requestFrom)
            {
                (requestFrom, requestTo) = (requestTo, requestFrom);
            }

            var usersQuery = _context.Users
                .Include(u => u.Department)
                .Include(u => u.Branch)
                .Include(u => u.JobTitle)
                .Include(u => u.Position)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Where(u => u.TenantId == tenantId);

            if (!string.IsNullOrWhiteSpace(normalizedEmployeeSearch))
            {
                usersQuery = usersQuery.Where(u =>
                    u.FullName.Contains(normalizedEmployeeSearch) ||
                    u.Email.Contains(normalizedEmployeeSearch) ||
                    u.EmployeeCode.Contains(normalizedEmployeeSearch));
            }

            if (employeeDepartmentId.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.DepartmentId == employeeDepartmentId.Value);
            }

            var timesheetQuery = _context.Timesheets
                .Include(t => t.User).ThenInclude(u => u!.Department)
                .Where(t => t.User!.TenantId == tenantId && t.Date >= fromDate && t.Date <= toDate);

            if (!string.IsNullOrWhiteSpace(normalizedAttendanceSearch))
            {
                timesheetQuery = timesheetQuery.Where(t =>
                    t.User!.FullName.Contains(normalizedAttendanceSearch) ||
                    t.User.EmployeeCode.Contains(normalizedAttendanceSearch) ||
                    t.User.Email.Contains(normalizedAttendanceSearch));
            }

            if (attendanceDepartmentId.HasValue)
            {
                timesheetQuery = timesheetQuery.Where(t => t.User!.DepartmentId == attendanceDepartmentId.Value);
            }

            var requestStatsQuery = _context.Requests
                .Include(r => r.FormTemplate)
                .Where(r => r.TenantId == tenantId &&
                            r.CreatedAt.Date >= requestFrom &&
                            r.CreatedAt.Date <= requestTo);

            var topRequestTypes = new Dictionary<string, int>();
            var requestStatusStats = new Dictionary<string, int>();
            var requestsByDay = new Dictionary<string, int>();
            var totalRequestsInRange = 0;

            if (isAdmin)
            {
                totalRequestsInRange = await requestStatsQuery.CountAsync();

                topRequestTypes = await requestStatsQuery
                    .GroupBy(r => r.FormTemplate != null && !string.IsNullOrWhiteSpace(r.FormTemplate.Name)
                        ? r.FormTemplate.Name
                        : "Không xác định")
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Name)
                    .Take(5)
                    .ToDictionaryAsync(x => x.Name, x => x.Count);

                requestStatusStats = await requestStatsQuery
                    .GroupBy(r => r.Status ?? "Unknown")
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToDictionaryAsync(x => x.Status, x => x.Count);

                requestsByDay = await requestStatsQuery
                    .GroupBy(r => r.CreatedAt.Date)
                    .Select(g => new { Day = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Day)
                    .ToDictionaryAsync(x => x.Day.ToString("dd/MM"), x => x.Count);
            }

            var model = new AdminViewModel
            {
                Users = await usersQuery.OrderBy(u => u.FullName).ToListAsync(),
                Departments = await _context.Departments.Where(d => d.TenantId == tenantId).ToListAsync(),
                Roles = await _context.Roles.Where(r => r.TenantId == tenantId).ToListAsync(),
                FormTemplates = await _context.FormTemplates.Include(f => f.Workflow).Where(f => f.TenantId == tenantId).ToListAsync(),
                Workflows = await _context.Workflows.Include(w => w.Steps).Where(w => w.TenantId == tenantId).ToListAsync(),
                Branches = await _context.Branches.Where(b => b.TenantId == tenantId).ToListAsync(),
                ActiveTab = tab,
                // HR tabs
                Shifts = await _context.Shifts.Where(s => s.TenantId == tenantId).OrderBy(s => s.Name).ToListAsync(),
                ShiftSwapRequests = await _context.ShiftSwapRequests
                    .Include(r => r.Requester)
                    .Include(r => r.TargetUser)
                    .Where(r => r.TenantId == tenantId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(200)
                    .ToListAsync(),
                Timesheets = await timesheetQuery
                    .OrderByDescending(t => t.Date)
                    .Take(200)
                    .ToListAsync(),
                EmployeeSearch = normalizedEmployeeSearch,
                EmployeeDepartmentId = employeeDepartmentId,
                AttendanceSearch = normalizedAttendanceSearch,
                AttendanceDepartmentId = attendanceDepartmentId,
                AttendanceFromDate = fromDate,
                AttendanceToDate = toDate,
                RequestStatsFromDate = requestFrom,
                RequestStatsToDate = requestTo,
                TotalRequestsInRange = totalRequestsInRange,
                PendingRequestsInRange = requestStatusStats.TryGetValue("Pending", out var pendingCount) ? pendingCount : 0,
                ApprovedRequestsInRange = requestStatusStats.TryGetValue("Approved", out var approvedCount) ? approvedCount : 0,
                RejectedRequestsInRange = requestStatusStats.TryGetValue("Rejected", out var rejectedCount) ? rejectedCount : 0,
                InProgressRequestsInRange = requestStatusStats.TryGetValue("InProgress", out var inProgressCount) ? inProgressCount : 0,
                CancelledRequestsInRange = requestStatusStats.TryGetValue("Cancelled", out var cancelledCount) ? cancelledCount : 0,
                TopRequestTypes = topRequestTypes,
                RequestStatusStats = requestStatusStats,
                RequestsByDay = requestsByDay
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);

            var manager = await _context.UserManagers
                .Where(um => um.UserId == id && um.IsPrimary && (um.EndDate == null || um.EndDate > DateTime.Now))
                .FirstOrDefaultAsync();

            var model = new UserEditViewModel
            {
                User = user,
                AllRoles = await _context.Roles.Where(r => r.TenantId == tenantId).ToListAsync(),
                SelectedRoleIds = user?.UserRoles.Select(ur => ur.RoleId).ToList() ?? new(),
                Departments = await _context.Departments.Where(d => d.TenantId == tenantId).ToListAsync(),
                Branches = await _context.Branches.Where(b => b.TenantId == tenantId).ToListAsync(),
                JobTitles = await _context.JobTitles.Where(j => j.TenantId == tenantId).ToListAsync(),
                Positions = await _context.Positions.Where(p => p.TenantId == tenantId).ToListAsync(),
                ManagerId = manager?.ManagerId,
                PotentialManagers = await _context.Users.Where(u => u.TenantId == tenantId && u.Id != id && u.Status == "Active").ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var employee = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Branch)
                .Include(u => u.JobTitle)
                .Include(u => u.Position)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

            if (employee == null) return NotFound();

            var model = new AdminEmployeeDetailViewModel
            {
                Employee = employee,
                RecentTimesheets = await _context.Timesheets
                    .Where(t => t.UserId == id)
                    .OrderByDescending(t => t.Date)
                    .Take(10)
                    .ToListAsync(),
                RecentShiftRequests = await _context.ShiftSwapRequests
                    .Include(r => r.Requester)
                    .Include(r => r.TargetUser)
                    .Where(r => r.TenantId == tenantId && (r.RequesterId == id || r.TargetUserId == id))
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveUser(UserEditViewModel model, int[]? roleIds)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var defaultBaseSalary = ResolveDefaultBaseSalary(model.User?.DepartmentId, model.User?.JobTitleId, model.User?.PositionId);

            User user;
            if (model.User?.Id > 0)
            {
                user = await _context.Users.FindAsync(model.User.Id) ?? new();
                user.FullName = model.User.FullName;
                user.Email = model.User.Email;
                user.EmployeeCode = model.User.EmployeeCode;
                user.DepartmentId = model.User.DepartmentId;
                user.BranchId = model.User.BranchId;
                user.JobTitleId = model.User.JobTitleId;
                user.PositionId = model.User.PositionId;
                user.HireDate = model.User.HireDate;
                user.BaseSalary = model.User.BaseSalary > 0 ? model.User.BaseSalary : defaultBaseSalary;
                user.SalaryCoefficient = model.User.SalaryCoefficient;
                user.StandardWorkDays = model.User.StandardWorkDays;
                user.StandardWorkHoursPerDay = model.User.StandardWorkHoursPerDay;
                user.OvertimeHourlyMultiplier = model.User.OvertimeHourlyMultiplier;
                user.LatePenaltyPerMinute = model.User.LatePenaltyPerMinute;
                user.FixedAllowance = model.User.FixedAllowance;
                user.OtherIncome = model.User.OtherIncome;
                user.UpdatedAt = DateTime.Now;
            }
            else
            {
                user = new User
                {
                    TenantId = tenantId,
                    FullName = model.User?.FullName ?? "",
                    Email = model.User?.Email ?? "",
                    EmployeeCode = model.User?.EmployeeCode ?? "",
                    DepartmentId = model.User?.DepartmentId,
                    BranchId = model.User?.BranchId,
                    JobTitleId = model.User?.JobTitleId,
                    PositionId = model.User?.PositionId,
                    HireDate = model.User?.HireDate ?? DateTime.Now,
                    BaseSalary = (model.User?.BaseSalary ?? 0) > 0 ? model.User!.BaseSalary : defaultBaseSalary,
                    SalaryCoefficient = model.User?.SalaryCoefficient ?? 1,
                    StandardWorkDays = model.User?.StandardWorkDays ?? 26,
                    StandardWorkHoursPerDay = model.User?.StandardWorkHoursPerDay ?? 8,
                    OvertimeHourlyMultiplier = model.User?.OvertimeHourlyMultiplier ?? 1.5m,
                    LatePenaltyPerMinute = model.User?.LatePenaltyPerMinute ?? 2000,
                    FixedAllowance = model.User?.FixedAllowance ?? 0,
                    OtherIncome = model.User?.OtherIncome ?? 0,
                    PasswordHash = HashPassword("Default@123"),
                    Status = "Active"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            if (model.User?.Id <= 0)
            {
                var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == "Employee");
                if (employeeRole != null && !await _context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == employeeRole.Id))
                {
                    _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = employeeRole.Id });
                }
            }

            // Update manager
            if (model.ManagerId.HasValue)
            {
                var existingMgr = await _context.UserManagers.Where(um => um.UserId == user.Id && um.IsPrimary).ToListAsync();
                _context.UserManagers.RemoveRange(existingMgr);
                _context.UserManagers.Add(new UserManager { UserId = user.Id, ManagerId = model.ManagerId.Value, IsPrimary = true });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Lưu thông tin nhân viên thành công!";
            return RedirectToAction("Index", new { tab = "users" });
        }

        private decimal ResolveDefaultBaseSalary(int? departmentId, int? jobTitleId, int? positionId)
        {
            if (departmentId == 1 || jobTitleId == 1 || positionId == 1)
            {
                return 80_000_000m;
            }

            if (departmentId == 4 || positionId == 4)
            {
                return 17_000_000m;
            }

            if (departmentId == 3)
            {
                return 15_000_000m;
            }

            if (jobTitleId == 3 || positionId == 2 || positionId == 3 || positionId == 5)
            {
                return 25_000_000m;
            }

            return 10_000_000m;
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
            if (user != null)
            {
                user.Status = "Inactive";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"ÄÃ£ vÃ´ hiá»‡u hÃ³a nhÃ¢n viÃªn {user.FullName}.";
            }

            return RedirectToAction("Index", new { tab = "users" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserLock(int id)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản nhân viên.";
                return RedirectToAction("Index", new { tab = "users" });
            }

            user.Status = string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase)
                ? "Locked"
                : "Active";
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = string.Equals(user.Status, "Locked", StringComparison.OrdinalIgnoreCase)
                ? $"Đã khóa tạm thời tài khoản {user.FullName}."
                : $"Đã mở khóa tài khoản {user.FullName}.";

            return RedirectToAction("EmployeeDetails", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> SendResetPasswordOtp(int id)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var employee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

            if (employee == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên cần cấp lại mật khẩu.";
                return RedirectToAction("Index", new { tab = "users" });
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            HttpContext.Session.SetString(GetResetOtpKey(id), otp);
            HttpContext.Session.SetString(GetResetOtpExpiryKey(id), DateTime.UtcNow.AddMinutes(10).ToString("O"));

            await QueueEmailLog(
                tenantId,
                employee.Email,
                $"[DANGCAPNE] Mã OTP đặt lại mật khẩu cho {employee.FullName}",
                "Queued");

            _context.Notifications.Add(new Notification
            {
                TenantId = tenantId,
                UserId = employee.Id,
                Title = "Mã OTP đặt lại mật khẩu",
                Message = $"Mã OTP của bạn là {otp}. Mã có hiệu lực trong 10 phút.",
                Type = "Info",
                ActionUrl = $"/Admin/EmployeeDetails/{employee.Id}"
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã tạo OTP cho {employee.FullName}. Mã OTP là {otp} và đã được ghi nhận vào hệ thống email demo.";
            return RedirectToAction("EmployeeDetails", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPasswordWithOtp(int id, string otp)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var employee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

            if (employee == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên cần cấp lại mật khẩu.";
                return RedirectToAction("Index", new { tab = "users" });
            }

            var expectedOtp = HttpContext.Session.GetString(GetResetOtpKey(id));
            var expiresAtRaw = HttpContext.Session.GetString(GetResetOtpExpiryKey(id));

            if (string.IsNullOrWhiteSpace(expectedOtp) || string.IsNullOrWhiteSpace(expiresAtRaw) || !DateTime.TryParse(expiresAtRaw, out var expiresAt))
            {
                TempData["Error"] = "OTP không tồn tại hoặc đã hết hạn. Vui lòng tạo mã mới.";
                return RedirectToAction("EmployeeDetails", new { id });
            }

            if (DateTime.UtcNow > expiresAt.ToUniversalTime())
            {
                ClearResetOtp(id);
                TempData["Error"] = "OTP đã hết hạn. Vui lòng tạo mã mới.";
                return RedirectToAction("EmployeeDetails", new { id });
            }

            if (!string.Equals(expectedOtp, otp?.Trim(), StringComparison.Ordinal))
            {
                TempData["Error"] = "OTP không đúng. Vui lòng kiểm tra lại.";
                return RedirectToAction("EmployeeDetails", new { id });
            }

            var newPassword = GenerateRandomPassword();
            employee.PasswordHash = HashPassword(newPassword);
            employee.UpdatedAt = DateTime.Now;

            ClearResetOtp(id);

            await QueueEmailLog(
                tenantId,
                employee.Email,
                $"[DANGCAPNE] Mật khẩu mới cho {employee.FullName}",
                "Queued");

            _context.Notifications.Add(new Notification
            {
                TenantId = tenantId,
                UserId = employee.Id,
                Title = "Mật khẩu đã được đặt lại",
                Message = "Tài khoản của bạn vừa được admin đặt lại mật khẩu. Vui lòng kiểm tra email nội bộ để nhận mật khẩu mới.",
                Type = "Info",
                ActionUrl = "/Account/Login"
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã đặt lại mật khẩu cho {employee.FullName}. Mật khẩu tạm thời: {newPassword}";
            return RedirectToAction("EmployeeDetails", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Whitelist()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager")) return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var model = new WhitelistViewModel
            {
                PendingUsers = await _context.Users
                    .Include(u => u.Department)
                    .Where(u => u.TenantId == tenantId && u.Status == "PendingApproval")
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync(),
                ApprovedUsers = await _context.Users
                    .Include(u => u.Department)
                    .Where(u => u.TenantId == tenantId && u.Status == "Active")
                    .OrderByDescending(u => u.UpdatedAt)
                    .Take(100)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager")) return RedirectToAction("AccessDenied", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Status == "PendingApproval")
            {
                user.Status = "Active";
                user.UpdatedAt = DateTime.Now;
                
                // Gán role mặc định là Employee nếu chưa có
                if (!await _context.UserRoles.AnyAsync(ur => ur.UserId == id))
                {
                    var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Employee");
                    if (employeeRole != null)
                    {
                        _context.UserRoles.Add(new UserRole { UserId = id, RoleId = employeeRole.Id });
                    }
                }

                await _context.SaveChangesAsync();

                // Notify User
                _context.Notifications.Add(new Models.SystemModels.Notification
                {
                    TenantId = user.TenantId,
                    UserId = user.Id,
                    Title = "Tài khoản đã được duyệt",
                    Message = "Tài khoản đăng nhập của bạn đã được quản trị viên phê duyệt thành công.",
                    Type = "Info",
                    ActionUrl = "/Account/Profile"
                });
                await _context.SaveChangesAsync();
                
                await _hubContext.Clients.Group($"user_{user.Id}").SendAsync("ReceiveNotification", new
                {
                    title = "Tài khoản đã được duyệt",
                    message = "Tài khoản của bạn đã được duyệt.",
                    type = "Info",
                    actionUrl = "/Account/Profile"
                });

                TempData["Success"] = $"Đã phê duyệt email {user.Email}!";
            }
            return RedirectToAction("Whitelist");
        }

        [HttpPost]
        public async Task<IActionResult> RejectUser(int id)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager")) return RedirectToAction("AccessDenied", "Account");

            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Status == "PendingApproval")
            {
                user.Status = "Rejected";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Notify User
                _context.Notifications.Add(new Models.SystemModels.Notification
                {
                    TenantId = user.TenantId,
                    UserId = user.Id,
                    Title = "Đăng ký bị từ chối",
                    Message = "Rất tiếc, yêu cầu tạo tài khoản của bạn đã bị từ chối.",
                    Type = "Info"
                });
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group($"user_{user.Id}").SendAsync("ReceiveNotification", new
                {
                    title = "Đăng ký bị từ chối",
                    message = "Yêu cầu đăng ký tài khoản của bạn đã bị từ chối.",
                    type = "Info"
                });

                TempData["Error"] = $"Đã từ chối email {user.Email}!";
            }
            return RedirectToAction("Whitelist");
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(string name, string code, int? managerId)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            _context.Departments.Add(new Department
            {
                TenantId = tenantId,
                Name = name,
                Code = code,
                ManagerId = managerId
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Tạo phòng ban thành công!";
            return RedirectToAction("Index", new { tab = "departments" });
        }

        [HttpGet]
        public async Task<IActionResult> FormBuilder(int? id)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var model = new FormBuilderViewModel
            {
                FormTemplate = id.HasValue
                    ? await _context.FormTemplates
                        .Include(f => f.Fields.OrderBy(ff => ff.DisplayOrder))
                            .ThenInclude(ff => ff.Options)
                        .FirstOrDefaultAsync(f => f.Id == id)
                    : new FormTemplate { TenantId = tenantId },
                Workflows = await _context.Workflows.Where(w => w.TenantId == tenantId).ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveFormTemplate(FormTemplate template, string fieldsJson)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            template.TenantId = tenantId;

            if (template.Id > 0)
            {
                var existing = await _context.FormTemplates.FindAsync(template.Id);
                if (existing != null)
                {
                    existing.Name = template.Name;
                    existing.Description = template.Description;
                    existing.Category = template.Category;
                    existing.Icon = template.Icon;
                    existing.WorkflowId = template.WorkflowId;
                    existing.RequiresFinancialApproval = template.RequiresFinancialApproval;
                    existing.IsActive = template.IsActive;
                }
            }
            else
            {
                _context.FormTemplates.Add(template);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Lưu biểu mẫu thành công!";
            return RedirectToAction("Index", new { tab = "forms" });
        }

        [HttpGet]
        public async Task<IActionResult> Delegation()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var model = new DelegationViewModel
            {
                ActiveDelegations = await _context.Delegations
                    .Include(d => d.Delegator).Include(d => d.Delegate)
                    .Where(d => d.TenantId == tenantId && d.IsActive)
                    .ToListAsync(),
                PotentialDelegates = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.Id != userId && u.Status == "Active")
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDelegation(DelegationViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            if (model.DelegateId.HasValue && model.StartDate.HasValue && model.EndDate.HasValue)
            {
                _context.Delegations.Add(new Delegation
                {
                    TenantId = tenantId,
                    DelegatorId = userId.Value,
                    DelegateId = model.DelegateId.Value,
                    StartDate = model.StartDate.Value,
                    EndDate = model.EndDate.Value,
                    Reason = model.Reason ?? "",
                    IsActive = true
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ủy quyền thành công!";
            }

            return RedirectToAction("Delegation");
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogs(int? requestId, int page = 1)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var query = _context.RequestAuditLogs
                .Include(a => a.User)
                .Include(a => a.Request)
                .Where(a => a.Request!.TenantId == tenantId);

            if (requestId.HasValue)
                query = query.Where(a => a.RequestId == requestId);

            var logs = await query.OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * 50).Take(50)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.RequestId = requestId;
            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> SystemErrors(int page = 1)
        {
            var errors = await _context.SystemErrors
                .OrderByDescending(e => e.OccurredAt)
                .Skip((page - 1) * 50).Take(50)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            return View(errors);
        }

        // ==================== CA LÀM VIỆC ====================
        [HttpPost]
        public async Task<IActionResult> CreateShift(string name, string code, string startTime, string endTime, int gracePeriodMinutes = 15)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR")) return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            _context.Shifts.Add(new Shift
            {
                TenantId = tenantId,
                Name = name,
                Code = code,
                StartTime = TimeSpan.Parse(startTime),
                EndTime = TimeSpan.Parse(endTime),
                GracePeriodMinutes = gracePeriodMinutes,
                IsActive = true
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã tạo ca làm việc '{name}'!";
            return RedirectToAction("Index", new { tab = "shifts" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteShift(int id)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR")) return RedirectToAction("AccessDenied", "Account");

            var shift = await _context.Shifts.FindAsync(id);
            if (shift != null)
            {
                shift.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã vô hiệu hóa ca '{shift.Name}'!";
            }
            return RedirectToAction("Index", new { tab = "shifts" });
        }

        // ==================== DUYỆT CA ====================
        [HttpPost]
        public async Task<IActionResult> ApproveShiftSwap(int id)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager")) return RedirectToAction("AccessDenied", "Account");

            var request = await _context.ShiftSwapRequests.FindAsync(id);
            if (request != null && request.Status == "Pending" || request?.Status == "ApprovedByTarget")
            {
                request!.Status = "Approved";
                request.ApprovedByManagerId = HttpContext.Session.GetInt32("UserId");
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã duyệt đơn đổi ca!";
            }
            return RedirectToAction("Index", new { tab = "users" });
        }

        [HttpPost]
        public async Task<IActionResult> RejectShiftSwap(int id)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager")) return RedirectToAction("AccessDenied", "Account");

            var request = await _context.ShiftSwapRequests.FindAsync(id);
            if (request != null)
            {
                request.Status = "RejectedByManager";
                await _context.SaveChangesAsync();
                TempData["Error"] = "Đã từ chối đơn đổi ca.";
            }
            return RedirectToAction("Index", new { tab = "users" });
        }

        // ==================== CHẤM CÔNG ====================
        [HttpGet]
        public async Task<IActionResult> ShiftRequestDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var request = await _context.ShiftSwapRequests
                .Include(r => r.Requester)
                .Include(r => r.TargetUser)
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

            if (request == null) return NotFound();
            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> EditShiftRequest(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var request = await _context.ShiftSwapRequests
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

            if (request == null) return NotFound();

            var model = new AdminShiftRequestEditViewModel
            {
                ShiftRequest = request,
                Employees = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.Status == "Active")
                    .OrderBy(u => u.FullName)
                    .ToListAsync(),
                Shifts = await _context.Shifts
                    .Where(s => s.TenantId == tenantId && s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateShiftRequest(AdminShiftRequestEditViewModel model)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var request = await _context.ShiftSwapRequests
                .FirstOrDefaultAsync(r => r.Id == model.ShiftRequest.Id && r.TenantId == tenantId);

            if (request == null) return NotFound();

            request.TargetUserId = model.ShiftRequest.TargetUserId;
            request.RequesterShiftId = model.ShiftRequest.RequesterShiftId;
            request.RequesterDate = model.ShiftRequest.RequesterDate;
            request.TargetShiftId = model.ShiftRequest.TargetShiftId;
            request.TargetDate = model.ShiftRequest.TargetDate;
            request.Reason = model.ShiftRequest.Reason;
            request.Status = model.ShiftRequest.Status;

            await _context.SaveChangesAsync();
            TempData["Success"] = "ÄÃ£ cáº­p nháº­t Ä‘Æ¡n ca.";
            return RedirectToAction("Index", new { tab = "shiftrequests" });
        }

        [HttpPost]
        public async Task<IActionResult> ManualUpdateTimekeeping(int id, string? checkIn, string? checkOut, string? status, double? workHours, string? notes)
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR")) return RedirectToAction("AccessDenied", "Account");

            var record = await _context.Timesheets.FindAsync(id);
            if (record != null)
            {
                if (DateTime.TryParse(checkIn, out var ci)) record.CheckIn = ci;
                if (DateTime.TryParse(checkOut, out var co)) record.CheckOut = co;

                if (workHours.HasValue && workHours.Value > 0)
                {
                    // Manual override
                    record.WorkHours = Math.Round(workHours.Value, 2);
                }
                else if (record.CheckIn.HasValue && record.CheckOut.HasValue)
                {
                    // Auto-calculate
                    var hours = (record.CheckOut.Value - record.CheckIn.Value).TotalHours;
                    record.WorkHours = Math.Round(Math.Max(hours, 0), 2);
                }

                if (!string.IsNullOrEmpty(status)) record.Status = status;
                if (notes != null) record.Notes = notes;
                record.Source = "Manual";
                record.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật chấm công thủ công!";
            }
            return RedirectToAction("Index", new { tab = "timekeeping" });
        }

        [HttpPost]
        public async Task<IActionResult> NormalizeCheckInTimes()
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR")) return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var records = await _context.Timesheets
                .Where(t => t.TenantId == tenantId && t.CheckIn.HasValue)
                .ToListAsync();

            var updatedCount = 0;
            foreach (var record in records)
            {
                var normalizedCheckIn = record.Date.Date.AddHours(8);
                if (record.CheckIn != normalizedCheckIn)
                {
                    record.CheckIn = normalizedCheckIn;
                    updatedCount++;
                }

                if (record.CheckOut.HasValue)
                {
                    var hours = (record.CheckOut.Value - normalizedCheckIn).TotalHours;
                    record.WorkHours = Math.Round(Math.Max(hours, 0), 2);
                }

                record.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã chuẩn hóa {updatedCount} bản ghi giờ vào về 08:00.";
            return RedirectToAction("Index", new { tab = "timekeeping" });
        }

        [HttpGet]
        public async Task<IActionResult> TimekeepingDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR") && !roles.Contains("Manager"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var record = await _context.Timesheets
                .Include(t => t.User).ThenInclude(u => u!.Department)
                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);

            if (record == null) return NotFound();
            return View(record);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("HR"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var timesheets = await _context.Timesheets
                .Include(t => t.User).ThenInclude(u => u!.Department)
                .Where(t => t.TenantId == tenantId)
                .OrderByDescending(t => t.Date)
                .Take(500)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("RecordId,EmployeeName,Department,Date,CheckIn,CheckOut,TotalHours,Status,Source,Notes");
            foreach (var item in timesheets)
            {
                csv.AppendLine(string.Join(",",
                    item.Id,
                    EscapeCsv(item.User?.FullName),
                    EscapeCsv(item.User?.Department?.Name),
                    item.Date.ToString("yyyy-MM-dd"),
                    item.CheckIn?.ToString("HH:mm") ?? "",
                    item.CheckOut?.ToString("HH:mm") ?? "",
                    item.WorkHours.ToString("0.##"),
                    EscapeCsv(item.Status),
                    EscapeCsv(item.Source),
                    EscapeCsv(item.Notes)));
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"timekeeping-report-{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "DANGCAPNE_SALT"));
            return Convert.ToBase64String(bytes);
        }

        private async Task QueueEmailLog(int tenantId, string toEmail, string subject, string status)
        {
            _context.EmailLogs.Add(new EmailLog
            {
                TenantId = tenantId,
                ToEmail = toEmail,
                Subject = subject,
                Status = status,
                SentAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
            return new string(Enumerable.Range(0, 10)
                .Select(_ => chars[Random.Shared.Next(chars.Length)])
                .ToArray());
        }

        private static string GetResetOtpKey(int userId) => $"AdminResetOtp:{userId}";

        private static string GetResetOtpExpiryKey(int userId) => $"AdminResetOtpExpiry:{userId}";

        private void ClearResetOtp(int userId)
        {
            HttpContext.Session.Remove(GetResetOtpKey(userId));
            HttpContext.Session.Remove(GetResetOtpExpiryKey(userId));
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var normalized = value.Replace("\"", "\"\"");
            return $"\"{normalized}\"";
        }
    }
}
