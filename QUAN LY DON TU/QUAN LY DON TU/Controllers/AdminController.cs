using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.Filters;
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
            var roles = GetCurrentRoles();
            if (!CanAccessEmployeeDirectory(roles)) return RedirectToAction("AccessDenied", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var isAdmin = roles.Contains("Admin");
            var isIT = roles.Contains("IT") || roles.Contains("ITManager");
            tab = tab?.Trim().ToLowerInvariant() switch
            {
                "timekeeping" => "timekeeping",
                "requeststats" when isAdmin => "requeststats",
                _ => "users"
            };

            if (isIT && !isAdmin && tab != "users")
            {
                tab = "users";
            }

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
            var roles = GetCurrentRoles();
            if (!CanManageEmployeeProfiles(roles))
                return RedirectToAction("AccessDenied", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

            if (user == null)
            {
                return NotFound();
            }

            var manager = await _context.UserManagers
                .Where(um => um.UserId == id && um.IsPrimary && (um.EndDate == null || um.EndDate > DateTime.UtcNow))
                .FirstOrDefaultAsync();

            var model = new UserEditViewModel
            {
                User = user,
                AllRoles = CanManageRoleAssignments(roles)
                    ? await _context.Roles.Where(r => r.TenantId == tenantId).OrderBy(r => r.Name).ToListAsync()
                    : new List<Role>(),
                SelectedRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
                Departments = await _context.Departments.Where(d => d.TenantId == tenantId).ToListAsync(),
                Branches = await _context.Branches.Where(b => b.TenantId == tenantId).ToListAsync(),
                JobTitles = await _context.JobTitles.Where(j => j.TenantId == tenantId).ToListAsync(),
                Positions = await _context.Positions.Where(p => p.TenantId == tenantId).ToListAsync(),
                ManagerId = manager?.ManagerId,
                PotentialManagers = await _context.Users.Where(u => u.TenantId == tenantId && u.Id != id && u.Status == "Active").ToListAsync(),
                CanManageRoles = CanManageRoleAssignments(roles),
                CanAutoGenerateEmployeeCode = CanAutoGenerateEmployeeCode(roles)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeDetails(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var roles = GetCurrentRoles();
            if (!CanAccessEmployeeDirectory(roles))
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
                    .ToListAsync(),
                CanEditEmployee = CanManageEmployeeProfiles(roles),
                CanManageTechnicalAccess = CanManageTechnicalAccess(roles),
                RolesSummary = string.Join(", ", employee.UserRoles
                    .Select(ur => ur.Role?.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()
                    .OrderBy(name => name))
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveUser(UserEditViewModel model, int[]? roleIds)
        {
            var roles = GetCurrentRoles();
            if (!CanManageEmployeeProfiles(roles))
                return RedirectToAction("AccessDenied", "Account");

            if (model.User == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu nhân viên cần lưu.";
                return RedirectToAction("Index", new { tab = "users" });
            }

            var inputUser = model.User;
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var defaultBaseSalary = ResolveDefaultBaseSalary(inputUser.DepartmentId, inputUser.JobTitleId, inputUser.PositionId);
            var normalizedEmployeeCode = NormalizeEmployeeCode(inputUser.EmployeeCode);
            var autoGeneratedCode = false;
            if (string.IsNullOrWhiteSpace(normalizedEmployeeCode) || string.Equals(normalizedEmployeeCode, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                normalizedEmployeeCode = await GenerateNextEmployeeCodeAsync(tenantId, inputUser.DepartmentId, inputUser.Id > 0 ? inputUser.Id : null);
                autoGeneratedCode = true;
            }

            User user;
            if (inputUser.Id > 0)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == inputUser.Id && u.TenantId == tenantId) ?? new();
                if (user.Id <= 0)
                {
                    return NotFound();
                }
                user.FullName = inputUser.FullName;
                user.Email = inputUser.Email;
                user.EmployeeCode = normalizedEmployeeCode;
                user.DepartmentId = inputUser.DepartmentId;
                user.BranchId = inputUser.BranchId;
                user.JobTitleId = inputUser.JobTitleId;
                user.PositionId = inputUser.PositionId;
                user.HireDate = inputUser.HireDate;
                user.BaseSalary = inputUser.BaseSalary > 0 ? inputUser.BaseSalary : defaultBaseSalary;
                user.SalaryCoefficient = inputUser.SalaryCoefficient;
                user.StandardWorkDays = inputUser.StandardWorkDays;
                user.StandardWorkHoursPerDay = inputUser.StandardWorkHoursPerDay;
                user.OvertimeHourlyMultiplier = inputUser.OvertimeHourlyMultiplier;
                user.LatePenaltyPerMinute = inputUser.LatePenaltyPerMinute;
                user.FixedAllowance = inputUser.FixedAllowance;
                user.OtherIncome = inputUser.OtherIncome;
                user.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                user = new User
                {
                    TenantId = tenantId,
                    FullName = inputUser.FullName ?? "",
                    Email = inputUser.Email ?? "",
                    EmployeeCode = normalizedEmployeeCode,
                    DepartmentId = inputUser.DepartmentId,
                    BranchId = inputUser.BranchId,
                    JobTitleId = inputUser.JobTitleId,
                    PositionId = inputUser.PositionId,
                    HireDate = inputUser.HireDate,
                    BaseSalary = inputUser.BaseSalary > 0 ? inputUser.BaseSalary : defaultBaseSalary,
                    SalaryCoefficient = inputUser.SalaryCoefficient,
                    StandardWorkDays = inputUser.StandardWorkDays,
                    StandardWorkHoursPerDay = inputUser.StandardWorkHoursPerDay,
                    OvertimeHourlyMultiplier = inputUser.OvertimeHourlyMultiplier,
                    LatePenaltyPerMinute = inputUser.LatePenaltyPerMinute,
                    FixedAllowance = inputUser.FixedAllowance,
                    OtherIncome = inputUser.OtherIncome,
                    PasswordHash = HashPassword("Default@123"),
                    Status = "Active"
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            if (CanManageRoleAssignments(roles))
            {
                await SyncUserRolesAsync(user.Id, tenantId, roleIds);
            }
            else if (inputUser.Id <= 0)
            {
                await EnsureDefaultEmployeeRoleAsync(user.Id, tenantId);
            }

            if (!await _context.UserRoles.AnyAsync(ur => ur.UserId == user.Id))
            {
                await EnsureDefaultEmployeeRoleAsync(user.Id, tenantId);
            }

            // Update manager
            if (model.ManagerId.HasValue)
            {
                var existingMgr = await _context.UserManagers.Where(um => um.UserId == user.Id && um.IsPrimary).ToListAsync();
                _context.UserManagers.RemoveRange(existingMgr);
                _context.UserManagers.Add(new UserManager { UserId = user.Id, ManagerId = model.ManagerId.Value, IsPrimary = true });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = autoGeneratedCode
                ? $"Lưu thông tin nhân viên thành công. Hệ thống đã cấp mã nhân viên {normalizedEmployeeCode}."
                : "Lưu thông tin nhân viên thành công!";
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

        private string[] GetCurrentRoles() =>
            (HttpContext.Session.GetString("Roles") ?? string.Empty)
                .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        private static bool ContainsAnyRole(string[] roles, params string[] allowedRoles) =>
            allowedRoles.Any(role => roles.Contains(role, StringComparer.OrdinalIgnoreCase));

        private bool CanAccessEmployeeDirectory(string[]? roles = null) =>
            ContainsAnyRole(roles ?? GetCurrentRoles(), "Admin", "HR", "Manager", "IT", "ITManager");

        private bool CanManageEmployeeProfiles(string[]? roles = null) =>
            ContainsAnyRole(roles ?? GetCurrentRoles(), "Admin", "HR", "IT", "ITManager");

        private bool CanManageTechnicalAccess(string[]? roles = null) =>
            ContainsAnyRole(roles ?? GetCurrentRoles(), "Admin", "IT", "ITManager");

        private bool CanManageRoleAssignments(string[]? roles = null) =>
            ContainsAnyRole(roles ?? GetCurrentRoles(), "Admin");

        private bool CanAutoGenerateEmployeeCode(string[]? roles = null) =>
            ContainsAnyRole(roles ?? GetCurrentRoles(), "Admin", "HR", "IT", "ITManager");

        private async Task EnsureDefaultEmployeeRoleAsync(int userId, int tenantId)
        {
            var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == "Employee");
            if (employeeRole != null && !await _context.UserRoles.AnyAsync(ur => ur.UserId == userId && ur.RoleId == employeeRole.Id))
            {
                _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = employeeRole.Id });
            }
        }

        private async Task SyncUserRolesAsync(int userId, int tenantId, int[]? roleIds)
        {
            var targetRoleIds = (roleIds ?? Array.Empty<int>())
                .Distinct()
                .ToList();

            if (targetRoleIds.Count == 0)
            {
                var employeeRoleId = await _context.Roles
                    .Where(r => r.TenantId == tenantId && r.Name == "Employee")
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync();

                if (employeeRoleId > 0)
                {
                    targetRoleIds.Add(employeeRoleId);
                }
            }

            var validRoleIds = await _context.Roles
                .Where(r => r.TenantId == tenantId && targetRoleIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync();

            var existingRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            var rolesToRemove = existingRoles
                .Where(ur => !validRoleIds.Contains(ur.RoleId))
                .ToList();

            if (rolesToRemove.Count > 0)
            {
                _context.UserRoles.RemoveRange(rolesToRemove);
            }

            var existingRoleIds = existingRoles.Select(ur => ur.RoleId).ToHashSet();
            foreach (var roleId in validRoleIds.Where(roleId => !existingRoleIds.Contains(roleId)))
            {
                _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            }
        }

        private async Task<string> GenerateNextEmployeeCodeAsync(int tenantId, int? departmentId, int? excludeUserId = null)
        {
            var prefix = await ResolveEmployeeCodePrefixAsync(tenantId, departmentId);
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            var codes = await query
                .Where(u => !string.IsNullOrWhiteSpace(u.EmployeeCode))
                .Select(u => u.EmployeeCode)
                .ToListAsync();

            var nextNumber = codes
                .Select(code => TryExtractEmployeeCodeNumber(code, prefix))
                .Where(number => number.HasValue)
                .Select(number => number!.Value)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"{prefix}{nextNumber:000}";
        }

        private async Task<string> ResolveEmployeeCodePrefixAsync(int tenantId, int? departmentId)
        {
            if (!departmentId.HasValue)
            {
                return "NV";
            }

            var departmentCode = await _context.Departments
                .AsNoTracking()
                .Where(d => d.TenantId == tenantId && d.Id == departmentId.Value)
                .Select(d => d.Code)
                .FirstOrDefaultAsync();

            return (departmentCode ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "BOD" => "AD",
                "IT" => "IT",
                "HR" => "HR",
                "ACC" => "KT",
                "SALES" => "KD",
                "MKT" => "MKT",
                _ => "NV"
            };
        }

        private static int? TryExtractEmployeeCodeNumber(string? employeeCode, string prefix)
        {
            var normalized = NormalizeEmployeeCode(employeeCode);
            if (string.IsNullOrWhiteSpace(normalized) || !normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var suffix = normalized.Substring(prefix.Length);
            return int.TryParse(suffix, out var number) ? number : null;
        }

        private static string NormalizeEmployeeCode(string? employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                return string.Empty;
            }

            return string.Concat(employeeCode.Where(c => !char.IsWhiteSpace(c))).Trim().ToUpperInvariant();
        }

        [HttpPost]
        [PermissionAuthorize("users.manage")]
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
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"ÄÃ£ vÃ´ hiá»‡u hÃ³a nhÃ¢n viÃªn {user.FullName}.";
            }

            return RedirectToAction("Index", new { tab = "users" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserLock(int id)
        {
            return RedirectToAction("Index", "IT");
        }


        [HttpPost]
        public async Task<IActionResult> SendResetPasswordOtp(int id)
        {
            return RedirectToAction("Index", "IT");
        }


        [HttpPost]
        public async Task<IActionResult> ResetPasswordWithOtp(int id, string otp)
        {
            var roles = GetCurrentRoles();
            if (!CanManageTechnicalAccess(roles))
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
            employee.UpdatedAt = DateTime.UtcNow;

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

        [HttpPost]
        public async Task<IActionResult> ResetRegisteredLoginIp(int id)
        {
            var roles = GetCurrentRoles();
            if (!CanManageTechnicalAccess(roles))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
            if (employee == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên cần reset IP đăng nhập.";
                return RedirectToAction("Index", new { tab = "users" });
            }

            employee.RegisteredLoginIp = null;
            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xoá IP đăng nhập đã lưu của {employee.FullName}. Nhân viên sẽ phải xác nhận IP lại ở lần đăng nhập tiếp theo.";
            return RedirectToAction("EmployeeDetails", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ResetBiometricEnrollment(int id)
        {
            var roles = GetCurrentRoles();
            if (!CanManageTechnicalAccess(roles))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
            if (employee == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên cần reset sinh trắc học.";
                return RedirectToAction("Index", new { tab = "users" });
            }

            employee.IsBiometricEnrolled = false;
            employee.FaceDescriptorFront = null;
            employee.FaceDescriptorLeft = null;
            employee.FaceDescriptorRight = null;
            employee.PortraitImage = null;
            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xoá dữ liệu sinh trắc học của {employee.FullName}. Nhân viên sẽ phải đăng ký lại khuôn mặt.";
            return RedirectToAction("EmployeeDetails", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ClearTrustedDevice(int id)
        {
            var roles = GetCurrentRoles();
            if (!CanManageTechnicalAccess(roles))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
            if (employee == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên cần xoá thiết bị tin cậy.";
                return RedirectToAction("Index", new { tab = "users" });
            }

            employee.TrustedDeviceId = null;
            employee.RegisteredMacAddress = null;
            employee.RememberMeTokenHash = null;
            employee.RememberMeExpiresAt = null;
            employee.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xoá thiết bị tin cậy của {employee.FullName}. Thiết bị sẽ phải xác thực lại ở lần đăng nhập tiếp theo.";
            return RedirectToAction("EmployeeDetails", new { id });
        }

        [HttpGet]
        [PermissionAuthorize("access.provision")]
        public async Task<IActionResult> Whitelist()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

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
        [PermissionAuthorize("access.provision")]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Status == "PendingApproval")
            {
                user.Status = "Active";
                user.UpdatedAt = DateTime.UtcNow;
                user.EmployeeCode = string.IsNullOrWhiteSpace(user.EmployeeCode) || string.Equals(user.EmployeeCode, "PENDING", StringComparison.OrdinalIgnoreCase)
                    ? await GenerateNextEmployeeCodeAsync(user.TenantId, user.DepartmentId, user.Id)
                    : NormalizeEmployeeCode(user.EmployeeCode);
                
                // Gán role mặc định là Employee nếu chưa có
                if (!await _context.UserRoles.AnyAsync(ur => ur.UserId == id))
                {
                    var employeeRole = await _context.Roles.FirstOrDefaultAsync(r => r.TenantId == user.TenantId && r.Name == "Employee");
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

                TempData["Success"] = $"Đã phê duyệt email {user.Email} và cấp mã nhân viên {user.EmployeeCode}!";
            }
            return RedirectToAction("Whitelist");
        }

        [HttpPost]
        [PermissionAuthorize("access.provision")]
        public async Task<IActionResult> RejectUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null && user.Status == "PendingApproval")
            {
                user.Status = "Rejected";
                user.UpdatedAt = DateTime.UtcNow;
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
        [PermissionAuthorize("delegation.manage")]
        public async Task<IActionResult> Delegation()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var today = DateTime.Today;

            var model = new DelegationViewModel
            {
                ActiveDelegations = await _context.Delegations
                    .Include(d => d.Delegator).Include(d => d.Delegate)
                    .Where(d => d.TenantId == tenantId && d.IsActive && d.EndDate >= today)
                    .OrderBy(d => d.StartDate)
                    .ToListAsync(),
                MyDelegations = await _context.Delegations
                    .Include(d => d.Delegator).Include(d => d.Delegate)
                    .Where(d => d.TenantId == tenantId && d.DelegatorId == userId.Value)
                    .OrderByDescending(d => d.CreatedAt)
                    .ToListAsync(),
                DelegatedToMe = await _context.Delegations
                    .Include(d => d.Delegator).Include(d => d.Delegate)
                    .Where(d => d.TenantId == tenantId && d.DelegateId == userId.Value && d.EndDate >= today)
                    .OrderBy(d => d.StartDate)
                    .ToListAsync(),
                PotentialDelegates = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.Id != userId && u.Status == "Active")
                    .OrderBy(u => u.FullName)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorize("delegation.manage")]
        public async Task<IActionResult> CreateDelegation(DelegationViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            if (model.DelegateId.HasValue && model.StartDate.HasValue && model.EndDate.HasValue)
            {
                if (model.DelegateId.Value == userId.Value)
                {
                    TempData["Error"] = "Bạn không thể tự ủy quyền cho chính mình.";
                    return RedirectToAction("Delegation");
                }

                if (model.EndDate.Value.Date < model.StartDate.Value.Date)
                {
                    TempData["Error"] = "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.";
                    return RedirectToAction("Delegation");
                }

                var hasOverlap = await _context.Delegations.AnyAsync(d =>
                    d.TenantId == tenantId &&
                    d.DelegatorId == userId.Value &&
                    d.IsActive &&
                    d.EndDate >= model.StartDate.Value.Date &&
                    d.StartDate <= model.EndDate.Value.Date);

                if (hasOverlap)
                {
                    TempData["Error"] = "Bạn đã có một ủy quyền khác bị trùng thời gian.";
                    return RedirectToAction("Delegation");
                }

                _context.Delegations.Add(new Delegation
                {
                    TenantId = tenantId,
                    DelegatorId = userId.Value,
                    DelegateId = model.DelegateId.Value,
                    StartDate = model.StartDate.Value.Date,
                    EndDate = model.EndDate.Value.Date,
                    Reason = model.Reason ?? "",
                    IsActive = true
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ủy quyền thành công!";
            }

            return RedirectToAction("Delegation");
        }

        [HttpPost]
        [PermissionAuthorize("delegation.manage")]
        public async Task<IActionResult> CancelDelegation(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var delegation = await _context.Delegations.FirstOrDefaultAsync(d => d.Id == id && d.DelegatorId == userId.Value);
            if (delegation == null)
            {
                TempData["Error"] = "Không tìm thấy bản ghi ủy quyền cần hủy.";
                return RedirectToAction("Delegation");
            }

            delegation.IsActive = false;
            delegation.EndDate = DateTime.Today;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã hủy ủy quyền.";
            return RedirectToAction("Delegation");
        }

        [HttpGet]
        [PermissionAuthorize("sla.manage")]
        public async Task<IActionResult> Sla()
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var model = new SlaManagementViewModel
            {
                Configs = await _context.SlaConfigs
                    .Include(s => s.FormTemplate)
                    .Where(s => s.TenantId == tenantId)
                    .OrderBy(s => s.FormTemplateId)
                    .ToListAsync(),
                EscalationRules = await _context.EscalationRules
                    .Include(e => e.SlaConfig).ThenInclude(s => s!.FormTemplate)
                    .Include(e => e.EscalateToUser)
                    .Where(e => e.TenantId == tenantId)
                    .OrderBy(e => e.SlaConfigId)
                    .ToListAsync(),
                FormTemplates = await _context.FormTemplates
                    .Where(f => f.TenantId == tenantId && f.IsActive)
                    .OrderBy(f => f.Name)
                    .ToListAsync(),
                PotentialEscalationTargets = await _context.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .Where(u => u.TenantId == tenantId && u.Status == "Active" &&
                        u.UserRoles.Any(ur => ur.Role!.Name == "Admin" || ur.Role!.Name == "HR" || ur.Role!.Name == "Manager" || ur.Role!.Name == "ITManager"))
                    .OrderBy(u => u.FullName)
                    .ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [PermissionAuthorize("sla.manage")]
        public async Task<IActionResult> SaveSla(SlaManagementViewModel model)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            if (model.ReminderHours <= 0 || model.EscalationHours <= 0 || model.EscalationHours < model.ReminderHours)
            {
                TempData["Error"] = "Giờ nhắc và giờ escalation phải > 0, và giờ escalation >= giờ nhắc.";
                return RedirectToAction("Sla");
            }

            var existing = await _context.SlaConfigs.FirstOrDefaultAsync(s =>
                s.TenantId == tenantId && s.FormTemplateId == model.SelectedFormTemplateId);
            if (existing == null)
            {
                _context.SlaConfigs.Add(new SlaConfig
                {
                    TenantId = tenantId,
                    FormTemplateId = model.SelectedFormTemplateId,
                    ReminderHours = model.ReminderHours,
                    EscalationHours = model.EscalationHours,
                    AutoRemind = model.AutoRemind,
                    AutoEscalate = model.AutoEscalate
                });
            }
            else
            {
                existing.ReminderHours = model.ReminderHours;
                existing.EscalationHours = model.EscalationHours;
                existing.AutoRemind = model.AutoRemind;
                existing.AutoEscalate = model.AutoEscalate;
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã lưu cấu hình SLA.";
            return RedirectToAction("Sla");
        }

        [HttpPost]
        [PermissionAuthorize("sla.manage")]
        public async Task<IActionResult> DeleteSla(int id)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var cfg = await _context.SlaConfigs.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId);
            if (cfg != null)
            {
                var rules = _context.EscalationRules.Where(r => r.SlaConfigId == id);
                _context.EscalationRules.RemoveRange(rules);
                _context.SlaConfigs.Remove(cfg);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xoá cấu hình SLA.";
            }
            return RedirectToAction("Sla");
        }

        [HttpPost]
        [PermissionAuthorize("sla.manage")]
        public async Task<IActionResult> SaveEscalationRule(SlaManagementViewModel model)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            if (!model.SelectedSlaConfigId.HasValue || !model.EscalateToUserId.HasValue || model.EscalateAfterHours <= 0)
            {
                TempData["Error"] = "Vui lòng chọn cấu hình SLA, người nhận escalation và giờ hợp lệ.";
                return RedirectToAction("Sla");
            }

            var configExists = await _context.SlaConfigs.AnyAsync(s => s.Id == model.SelectedSlaConfigId.Value && s.TenantId == tenantId);
            if (!configExists)
            {
                TempData["Error"] = "Cấu hình SLA không tồn tại.";
                return RedirectToAction("Sla");
            }

            _context.EscalationRules.Add(new EscalationRule
            {
                TenantId = tenantId,
                SlaConfigId = model.SelectedSlaConfigId.Value,
                EscalateToUserId = model.EscalateToUserId.Value,
                EscalateAfterHours = model.EscalateAfterHours,
                NotificationMessage = model.NotificationMessage ?? string.Empty
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm rule escalation.";
            return RedirectToAction("Sla");
        }

        [HttpPost]
        [PermissionAuthorize("sla.manage")]
        public async Task<IActionResult> DeleteEscalationRule(int id)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var rule = await _context.EscalationRules.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);
            if (rule != null)
            {
                _context.EscalationRules.Remove(rule);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xoá rule escalation.";
            }
            return RedirectToAction("Sla");
        }

        [HttpGet]
        [PermissionAuthorize("audit.view")]
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
                record.UpdatedAt = DateTime.UtcNow;
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

                record.UpdatedAt = DateTime.UtcNow;
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
            return File(bytes, "text/csv; charset=utf-8", $"timekeeping-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
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
                SentAt = DateTime.UtcNow
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

        [HttpGet]
        public async Task<IActionResult> MigrateFaceDescriptors()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("Admin") && !roles.Contains("IT") && !roles.Contains("ITManager"))
                return Json(new { success = false, message = "Chỉ Admin hoặc IT mới có quyền truy cập." });

            var migrationService = HttpContext.RequestServices.GetService(typeof(DANGCAPNE.Services.IFaceDescriptorMigrationService)) as DANGCAPNE.Services.IFaceDescriptorMigrationService;
            if (migrationService == null)
                return Json(new { success = false, message = "Migration service không khả dụng." });

            try
            {
                var (updated, skipped, message) = await migrationService.MigrateFaceDescriptorsFromSQLiteAsync();
                return Json(new { success = true, updated, skipped, message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdminController] MigrateFaceDescriptors Error: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi migration: {ex.Message}" });
            }
        }
    }
}
