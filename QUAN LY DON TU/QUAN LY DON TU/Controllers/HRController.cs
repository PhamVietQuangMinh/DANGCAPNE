using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.Filters;
using DANGCAPNE.ViewModels;
using DANGCAPNE.Models.HR;
using DANGCAPNE.Models.Organization;
using DANGCAPNE.Models.Requests;

namespace DANGCAPNE.Controllers
{
    [Route("HR/[action]")]
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Route("~/HR")]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            if (!roles.Contains("HR") && !roles.Contains("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            var threeMonthsAgo = DateTime.Now.AddMonths(-3);

            // Fetch all requests with related data in one query
            var allRequests = await _context.Requests
                .Include(r => r.Requester).ThenInclude(u => u!.Department)
                .Include(r => r.FormTemplate)
                .Where(r => r.TenantId == tenantId)
                .OrderByDescending(r => r.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            // Pre-filter for status buckets using in-memory (small dataset after SQL filter)
            var pendingList = new List<Request>();
            var inProgressList = new List<Request>();
            var approvedList = new List<Request>();
            var rejectedList = new List<Request>();

            foreach (var r in allRequests)
            {
                switch (r.Status)
                {
                    case "Pending": pendingList.Add(r); break;
                    case "InProgress":
                    case "RequestEdit": inProgressList.Add(r); break;
                    case "Approved": approvedList.Add(r); break;
                    case "Rejected":
                    case "Cancelled": rejectedList.Add(r); break;
                }
            }

            var model = new HRDashboardViewModel
            {
                AllRequests = allRequests,
                PendingRequests = pendingList,
                InProgressRequests = inProgressList,
                ApprovedRequests = approvedList,
                RejectedRequests = rejectedList,
                Employees = await _context.Users.Include(u => u.Department).Where(u => u.TenantId == tenantId && u.Status == "Active").AsNoTracking().ToListAsync(),
                LeaveBalances = await _context.LeaveBalances.Include(lb => lb.User).Include(lb => lb.LeaveType)
                    .Where(lb => lb.TenantId == tenantId && lb.Year == DateTime.Now.Year).AsNoTracking().ToListAsync(),
            };

            // Anomaly detection - use SQL grouping instead of loading all rows
            var anomalies = await DetectAnomalies(tenantId, threeMonthsAgo);
            model.Anomalies = anomalies;

            // Dept stats - use SQL aggregation
            var deptStatsRaw = await _context.Requests
                .Include(r => r.Requester).ThenInclude(u => u!.Department)
                .Where(r => r.TenantId == tenantId && r.Requester != null && r.Requester.Department != null)
                .GroupBy(r => r.Requester!.Department!.Name)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .AsNoTracking()
                .ToListAsync();
            model.DepartmentStats = deptStatsRaw.ToDictionary(x => x.Dept, x => x.Count);

            return View(model);
        }

        public async Task<IActionResult> LeaveManagement()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var balances = await _context.LeaveBalances
                .Include(lb => lb.User).ThenInclude(u => u!.Department)
                .Include(lb => lb.LeaveType)
                .Where(lb => lb.TenantId == tenantId && lb.Year == DateTime.Now.Year)
                .ToListAsync();

            return View(balances);
        }

        public async Task<IActionResult> Timekeeping(DateTime? startDate, DateTime? endDate, int? employeeId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var start = startDate ?? DateTime.Now.AddDays(-DateTime.Now.Day + 1);
            var end = endDate ?? DateTime.Now;

            var query = _context.Timesheets
                .Include(t => t.User).ThenInclude(u => u!.Department)
                .Where(t => t.TenantId == tenantId && t.Date >= start && t.Date <= end);

            if (employeeId.HasValue)
                query = query.Where(t => t.UserId == employeeId);

            var model = new TimekeepingViewModel
            {
                Timesheets = await query.OrderByDescending(t => t.Date).ToListAsync(),
                Shifts = await _context.Shifts.Where(s => s.TenantId == tenantId).ToListAsync(),
                StartDate = start,
                EndDate = end,
                UserId = employeeId,
                Employees = await _context.Users.Where(u => u.TenantId == tenantId && u.Status == "Active").ToListAsync()
            };

            return View(model);
        }

        [PermissionAuthorize("checklist.manage")]
        public async Task<IActionResult> EnterpriseChecklist(string mode = "onboarding", int? userId = null)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries);
            if (!roles.Contains("HR") && !roles.Contains("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            mode = string.Equals(mode, "offboarding", StringComparison.OrdinalIgnoreCase) ? "offboarding" : "onboarding";

            var employees = await _context.Users
                .Where(u => u.TenantId == tenantId && u.Status == "Active")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var model = new EnterpriseChecklistViewModel
            {
                Mode = mode,
                SelectedUserId = userId,
                Employees = employees,
                Roles = await _context.Roles.OrderBy(r => r.Name).ToListAsync(),
                OnboardingTemplates = await _context.OnboardingTaskTemplates
                    .Include(t => t.DefaultAssigneeRole)
                    .Where(t => t.TenantId == tenantId)
                    .OrderBy(t => t.Name)
                    .ToListAsync(),
                OffboardingTemplates = await _context.OffboardingTaskTemplates
                    .Include(t => t.DefaultAssigneeRole)
                    .Where(t => t.TenantId == tenantId)
                    .OrderBy(t => t.Name)
                    .ToListAsync(),
                OnboardingTasks = await _context.OnboardingTasks
                    .Include(t => t.Template)
                    .Include(t => t.User)
                    .Include(t => t.AssignedTo)
                    .Where(t => userId == null || t.UserId == userId.Value)
                    .OrderBy(t => t.Status)
                    .ThenBy(t => t.DueDate)
                    .ToListAsync(),
                OffboardingTasks = await _context.OffboardingTasks
                    .Include(t => t.Template)
                    .Include(t => t.User)
                    .Include(t => t.AssignedTo)
                    .Where(t => userId == null || t.UserId == userId.Value)
                    .OrderBy(t => t.Status)
                    .ThenBy(t => t.DueDate)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorize("checklist.manage")]
        public async Task<IActionResult> CreateChecklistTemplate(EnterpriseChecklistViewModel model)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries);
            if (!roles.Contains("HR") && !roles.Contains("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(model.TemplateName))
            {
                TempData["Error"] = "Vui lòng nhập tên checklist mẫu.";
                return RedirectToAction("EnterpriseChecklist", new { mode = model.Mode });
            }

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            if (string.Equals(model.Mode, "offboarding", StringComparison.OrdinalIgnoreCase))
            {
                _context.OffboardingTaskTemplates.Add(new OffboardingTaskTemplate
                {
                    TenantId = tenantId,
                    Name = model.TemplateName.Trim(),
                    Description = model.TemplateDescription?.Trim(),
                    DefaultDueDays = model.DefaultDueDays <= 0 ? 7 : model.DefaultDueDays,
                    DefaultAssigneeRoleId = model.DefaultAssigneeRoleId
                });
            }
            else
            {
                _context.OnboardingTaskTemplates.Add(new OnboardingTaskTemplate
                {
                    TenantId = tenantId,
                    Name = model.TemplateName.Trim(),
                    Description = model.TemplateDescription?.Trim(),
                    DefaultDueDays = model.DefaultDueDays <= 0 ? 7 : model.DefaultDueDays,
                    DefaultAssigneeRoleId = model.DefaultAssigneeRoleId
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã tạo checklist mẫu.";
            return RedirectToAction("EnterpriseChecklist", new { mode = model.Mode });
        }

        [HttpPost]
        [PermissionAuthorize("checklist.manage")]
        public async Task<IActionResult> GenerateChecklist(string mode, int selectedUserId)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries);
            if (!roles.Contains("HR") && !roles.Contains("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            mode = string.Equals(mode, "offboarding", StringComparison.OrdinalIgnoreCase) ? "offboarding" : "onboarding";

            var employee = await _context.Users.FirstOrDefaultAsync(u => u.Id == selectedUserId && u.TenantId == tenantId);
            if (employee == null)
            {
                TempData["Error"] = "Không tìm thấy nhân viên cần tạo checklist.";
                return RedirectToAction("EnterpriseChecklist", new { mode });
            }

            if (mode == "offboarding")
            {
                var templates = await _context.OffboardingTaskTemplates.Where(t => t.TenantId == tenantId).ToListAsync();
                foreach (var template in templates)
                {
                    var exists = await _context.OffboardingTasks.AnyAsync(t => t.TemplateId == template.Id && t.UserId == selectedUserId && t.Status != "Cancelled");
                    if (exists) continue;

                    _context.OffboardingTasks.Add(new OffboardingTask
                    {
                        TemplateId = template.Id,
                        UserId = selectedUserId,
                        AssignedToUserId = await ResolveDefaultAssigneeUserIdAsync(template.DefaultAssigneeRoleId, tenantId),
                        DueDate = DateTime.Today.AddDays(template.DefaultDueDays <= 0 ? 7 : template.DefaultDueDays),
                        Status = "Open"
                    });
                }
            }
            else
            {
                var templates = await _context.OnboardingTaskTemplates.Where(t => t.TenantId == tenantId).ToListAsync();
                foreach (var template in templates)
                {
                    var exists = await _context.OnboardingTasks.AnyAsync(t => t.TemplateId == template.Id && t.UserId == selectedUserId && t.Status != "Cancelled");
                    if (exists) continue;

                    _context.OnboardingTasks.Add(new OnboardingTask
                    {
                        TemplateId = template.Id,
                        UserId = selectedUserId,
                        AssignedToUserId = await ResolveDefaultAssigneeUserIdAsync(template.DefaultAssigneeRoleId, tenantId),
                        DueDate = DateTime.Today.AddDays(template.DefaultDueDays <= 0 ? 7 : template.DefaultDueDays),
                        Status = "Open"
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã sinh checklist {mode} cho {employee.FullName}.";
            return RedirectToAction("EnterpriseChecklist", new { mode, userId = selectedUserId });
        }

        [HttpPost]
        [PermissionAuthorize("checklist.manage")]
        public async Task<IActionResult> UpdateChecklistTask(string mode, int id, string action)
        {
            var currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return RedirectToAction("Login", "Account");

            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries);
            if (!roles.Contains("HR") && !roles.Contains("Admin"))
                return RedirectToAction("AccessDenied", "Account");

            mode = string.Equals(mode, "offboarding", StringComparison.OrdinalIgnoreCase) ? "offboarding" : "onboarding";
            var markDone = string.Equals(action, "complete", StringComparison.OrdinalIgnoreCase);

            if (mode == "offboarding")
            {
                var task = await _context.OffboardingTasks.FindAsync(id);
                if (task != null)
                {
                    task.Status = markDone ? "Completed" : "Open";
                    task.CompletedAt = markDone ? DateTime.UtcNow : null;
                    await _context.SaveChangesAsync();
                    return RedirectToAction("EnterpriseChecklist", new { mode, userId = task.UserId });
                }
            }
            else
            {
                var task = await _context.OnboardingTasks.FindAsync(id);
                if (task != null)
                {
                    task.Status = markDone ? "Completed" : "Open";
                    task.CompletedAt = markDone ? DateTime.UtcNow : null;
                    await _context.SaveChangesAsync();
                    return RedirectToAction("EnterpriseChecklist", new { mode, userId = task.UserId });
                }
            }

            TempData["Error"] = "Không tìm thấy task cần cập nhật.";
            return RedirectToAction("EnterpriseChecklist", new { mode });
        }

        private async Task<List<AnomalyAlert>> DetectAnomalies(int tenantId, DateTime threeMonthsAgo)
        {
            var anomalies = new List<AnomalyAlert>();

            // Detect Monday sick leave pattern via SQL grouping
            var mondaySickLeaves = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.FormTemplate)
                .Where(r => r.TenantId == tenantId &&
                    r.FormTemplate!.Category == "Leave" &&
                    r.CreatedAt >= threeMonthsAgo)
                .AsNoTracking()
                .ToListAsync();

            var mondayGrouped = mondaySickLeaves
                .Where(r => r.CreatedAt.DayOfWeek == DayOfWeek.Monday)
                .GroupBy(r => new { r.RequesterId, r.Requester!.FullName })
                .Where(g => g.Count() >= 3)
                .Select(g => new { g.Key.RequesterId, g.Key.FullName, Count = g.Count() })
                .ToList();

            foreach (var item in mondayGrouped)
            {
                anomalies.Add(new AnomalyAlert
                {
                    EmployeeName = item.FullName,
                    AlertType = "Nghỉ thứ Hai",
                    Description = $"Đã xin nghỉ {item.Count} lần vào ngày thứ Hai trong 3 tháng qua",
                    Severity = "Warning"
                });
            }

            // Detect high OT department via SQL aggregation
            var otByDept = await _context.Requests
                .Include(r => r.Requester).ThenInclude(u => u!.Department)
                .Where(r => r.TenantId == tenantId &&
                    r.FormTemplate!.Category == "OT" &&
                    r.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                .AsNoTracking()
                .ToListAsync();

            var highOtDepts = otByDept
                .GroupBy(r => r.Requester!.Department!.Name)
                .Where(g => g.Count() > 10)
                .ToList();

            foreach (var item in highOtDepts)
            {
                anomalies.Add(new AnomalyAlert
                {
                    EmployeeName = item.Key,
                    AlertType = "OT cao bất thường",
                    Description = $"Phòng {item.Key} có {item.Count()} đơn OT trong tháng này",
                    Severity = "Critical"
                });
            }

            return anomalies;
        }

        private async Task<int?> ResolveDefaultAssigneeUserIdAsync(int? roleId, int tenantId)
        {
            if (!roleId.HasValue)
            {
                return null;
            }

            return await _context.UserRoles
                .Where(ur => ur.RoleId == roleId.Value && ur.User!.TenantId == tenantId && ur.User.Status == "Active")
                .OrderBy(ur => ur.User!.FullName)
                .Select(ur => (int?)ur.UserId)
                .FirstOrDefaultAsync();
        }
    }
}
