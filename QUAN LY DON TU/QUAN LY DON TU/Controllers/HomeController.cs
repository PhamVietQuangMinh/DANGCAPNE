using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.ViewModels;

namespace DANGCAPNE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");

            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var isAdmin = roles.Contains("Admin");
            var isHR = roles.Contains("HR");
            var isManager = roles.Contains("Manager");

            var requestsQuery = _context.Requests.Where(r => r.TenantId == tenantId);

            var todaySheet = await _context.Timesheets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Date == DateTime.Today);

            var model = new DashboardViewModel
            {
                CurrentUser = user,
                RoleName = HttpContext.Session.GetString("PrimaryRole") ?? "Employee",
                UnreadNotifications = await _context.Notifications
                    .AsNoTracking()
                    .CountAsync(n => n.UserId == userId && !n.IsRead),
                IsAttendanceDone = todaySheet?.CheckOut != null,
                IsCheckInDone = todaySheet?.CheckIn != null && todaySheet?.CheckOut == null,
                CheckInTime = todaySheet?.CheckIn?.ToString("HH:mm")
            };

            if (isAdmin || isHR)
            {
                model.TotalPendingRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.Status == "Pending" || r.Status == "InProgress");
                model.TotalApprovedRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.Status == "Approved");
                model.TotalRejectedRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.Status == "Rejected");
                model.TotalMyRequests = await requestsQuery.AsNoTracking().CountAsync();
                model.TotalEmployees = await _context.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.Status == "Active");
            }
            else
            {
                model.TotalPendingRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.RequesterId == userId && (r.Status == "Pending" || r.Status == "InProgress"));
                model.TotalApprovedRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.RequesterId == userId && r.Status == "Approved");
                model.TotalRejectedRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.RequesterId == userId && r.Status == "Rejected");
                model.TotalMyRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.RequesterId == userId);
            }

            var leaveBalance = await _context.LeaveBalances
                .AsNoTracking()
                .Where(lb => lb.UserId == userId && lb.LeaveTypeId == 1 && lb.Year == DateTime.Now.Year)
                .FirstOrDefaultAsync();
            model.LeaveBalance = leaveBalance?.Remaining ?? 12;

            if (isAdmin || isHR)
            {
                model.RecentRequests = await requestsQuery
                    .Include(r => r.Requester)
                    .Include(r => r.FormTemplate)
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }
            else
            {
                model.RecentRequests = await requestsQuery
                    .Where(r => r.RequesterId == userId)
                    .Include(r => r.FormTemplate)
                    .Include(r => r.Requester)
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }

            if (isManager || isAdmin || isHR)
            {
                model.PendingApprovals = await _context.Requests
                    .Include(r => r.Requester)
                    .Include(r => r.FormTemplate)
                    .AsNoTracking()
                    .Where(r => r.TenantId == tenantId &&
                        r.Approvals.Any(a => a.ApproverId == userId && a.Status == "Pending"))
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }

            model.RecentNotifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlyData = await requestsQuery
                .AsNoTracking()
                .Where(r => r.CreatedAt >= sixMonthsAgo)
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new { Key = g.Key.Year + "-" + g.Key.Month.ToString("D2"), Count = g.Count() })
                .ToListAsync();
            foreach (var m in monthlyData)
                model.RequestsByMonth[m.Key] = m.Count;

            var deptData = await requestsQuery
                .Include(r => r.Requester).ThenInclude(u => u!.Department)
                .AsNoTracking()
                .Where(r => r.Requester != null && r.Requester.Department != null)
                .GroupBy(r => r.Requester!.Department!.Name)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var d in deptData)
                model.RequestsByDepartment[d.Dept] = d.Count;

            var statusData = await requestsQuery
                .AsNoTracking()
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var s in statusData)
                model.RequestsByStatus[s.Status] = s.Count;

            var typeData = await requestsQuery
                .Include(r => r.FormTemplate)
                .AsNoTracking()
                .GroupBy(r => r.FormTemplate!.Name)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var t in typeData)
                model.RequestsByType[t.Type] = t.Count;

            if (isManager || isHR || isAdmin)
            {
                var teamUserIds = await _context.UserManagers
                    .AsNoTracking()
                    .Where(um => um.ManagerId == userId && (um.EndDate == null || um.EndDate > DateTime.Now))
                    .Select(um => um.UserId)
                    .ToListAsync();

                var teamBalances = await _context.LeaveBalances
                    .Include(lb => lb.User)
                    .AsNoTracking()
                    .Where(lb => teamUserIds.Contains(lb.UserId) && lb.LeaveTypeId == 1 && lb.Year == DateTime.Now.Year)
                    .ToListAsync();

                model.TeamLeaveBalances = teamBalances.Select(lb => new LeaveBalanceSummary
                {
                    EmployeeName = lb.User?.FullName ?? "",
                    TotalEntitled = lb.TotalEntitled,
                    Used = lb.Used,
                    Remaining = lb.Remaining
                }).ToList();
            }

            return View(model);
        }
        public async Task<IActionResult> EmployeeRequests()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var model = await BuildEmployeeModel(userId.Value, tenantId, roles);
            ViewData["Title"] = "Quan ly don tu";
            return View("EmployeeRequests", model);
        }

        public async Task<IActionResult> EmployeeTimeline()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var model = await BuildEmployeeModel(userId.Value, tenantId, roles);
            ViewData["Title"] = "Timeline cham cong";
            return View("EmployeeTimeline", model);
        }

        public async Task<IActionResult> EmployeeSchedule()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var model = await BuildEmployeeModel(userId.Value, tenantId, roles);
            ViewData["Title"] = "Lich bieu & ca lam";
            return View("EmployeeSchedule", model);
        }

        public async Task<IActionResult> EmployeeSwap()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var model = await BuildEmployeeModel(userId.Value, tenantId, roles);
            ViewData["Title"] = "Doi ca";
            return View("EmployeeSwap", model);
        }

        private async Task<DashboardViewModel> BuildEmployeeModel(int userId, int tenantId, string[] roles)
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var isAdmin = roles.Contains("Admin");
            var isHR = roles.Contains("HR");
            var isManager = roles.Contains("Manager");

            var requestsQuery = _context.Requests.Where(r => r.TenantId == tenantId);

            var todaySheet = await _context.Timesheets
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Date == DateTime.Today);

            var model = new DashboardViewModel
            {
                CurrentUser = user,
                RoleName = HttpContext.Session.GetString("PrimaryRole") ?? "Employee",
                UnreadNotifications = await _context.Notifications
                    .AsNoTracking()
                    .CountAsync(n => n.UserId == userId && !n.IsRead),
                IsAttendanceDone = todaySheet?.CheckOut != null,
                IsCheckInDone = todaySheet?.CheckIn != null && todaySheet?.CheckOut == null,
                CheckInTime = todaySheet?.CheckIn?.ToString("HH:mm")
            };

            if (isAdmin || isHR)
            {
                model.TotalPendingRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.Status == "Pending" || r.Status == "InProgress");
                model.TotalApprovedRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.Status == "Approved");
                model.TotalRejectedRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.Status == "Rejected");
                model.TotalMyRequests = await requestsQuery.AsNoTracking().CountAsync();
                model.TotalEmployees = await _context.Users.AsNoTracking().CountAsync(u => u.TenantId == tenantId && u.Status == "Active");
            }
            else
            {
                model.TotalPendingRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.RequesterId == userId && (r.Status == "Pending" || r.Status == "InProgress"));
                model.TotalApprovedRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.RequesterId == userId && r.Status == "Approved");
                model.TotalRejectedRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.RequesterId == userId && r.Status == "Rejected");
                model.TotalMyRequests = await requestsQuery.AsNoTracking().CountAsync(r => r.RequesterId == userId);
            }

            var leaveBalance = await _context.LeaveBalances
                .AsNoTracking()
                .Where(lb => lb.UserId == userId && lb.LeaveTypeId == 1 && lb.Year == DateTime.Now.Year)
                .FirstOrDefaultAsync();
            model.LeaveBalance = leaveBalance?.Remaining ?? 12;

            if (isAdmin || isHR)
            {
                model.RecentRequests = await requestsQuery
                    .Include(r => r.Requester)
                    .Include(r => r.FormTemplate)
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }
            else
            {
                model.RecentRequests = await requestsQuery
                    .Where(r => r.RequesterId == userId)
                    .Include(r => r.FormTemplate)
                    .Include(r => r.Requester)
                    .AsNoTracking()
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }

            if (isManager || isAdmin || isHR)
            {
                model.PendingApprovals = await _context.Requests
                    .Include(r => r.Requester)
                    .Include(r => r.FormTemplate)
                    .AsNoTracking()
                    .Where(r => r.TenantId == tenantId &&
                        r.Approvals.Any(a => a.ApproverId == userId && a.Status == "Pending"))
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }

            model.RecentNotifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var monthlyData = await requestsQuery
                .AsNoTracking()
                .Where(r => r.CreatedAt >= sixMonthsAgo)
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new { Key = g.Key.Year + "-" + g.Key.Month.ToString("D2"), Count = g.Count() })
                .ToListAsync();
            foreach (var m in monthlyData)
                model.RequestsByMonth[m.Key] = m.Count;

            var deptData = await requestsQuery
                .Include(r => r.Requester).ThenInclude(u => u!.Department)
                .AsNoTracking()
                .Where(r => r.Requester != null && r.Requester.Department != null)
                .GroupBy(r => r.Requester!.Department!.Name)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var d in deptData)
                model.RequestsByDepartment[d.Dept] = d.Count;

            var statusData = await requestsQuery
                .AsNoTracking()
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var s in statusData)
                model.RequestsByStatus[s.Status] = s.Count;

            var typeData = await requestsQuery
                .Include(r => r.FormTemplate)
                .AsNoTracking()
                .GroupBy(r => r.FormTemplate!.Name)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var t in typeData)
                model.RequestsByType[t.Type] = t.Count;

            if (isManager || isHR || isAdmin)
            {
                var teamUserIds = await _context.UserManagers
                    .AsNoTracking()
                    .Where(um => um.ManagerId == userId && (um.EndDate == null || um.EndDate > DateTime.Now))
                    .Select(um => um.UserId)
                    .ToListAsync();

                var teamBalances = await _context.LeaveBalances
                    .Include(lb => lb.User)
                    .AsNoTracking()
                    .Where(lb => teamUserIds.Contains(lb.UserId) && lb.LeaveTypeId == 1 && lb.Year == DateTime.Now.Year)
                    .ToListAsync();

                model.TeamLeaveBalances = teamBalances.Select(lb => new LeaveBalanceSummary
                {
                    EmployeeName = lb.User?.FullName ?? "",
                    TotalEntitled = lb.TotalEntitled,
                    Used = lb.Used,
                    Remaining = lb.Remaining
                }).ToList();
            }

            model.RecentTimesheets = await _context.Timesheets
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .Take(10)
                .ToListAsync();

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            model.CalendarTimesheets = await _context.Timesheets
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.Date >= monthStart && t.Date <= monthEnd)
                .ToListAsync();

            var scheduleFrom = DateTime.Today;
            var scheduleTo = DateTime.Today.AddDays(14);
            model.UpcomingShifts = await _context.UserShifts
                .Include(us => us.Shift)
                .AsNoTracking()
                .Where(us => us.UserId == userId &&
                            us.EffectiveDate <= scheduleTo &&
                            (us.EndDate == null || us.EndDate >= scheduleFrom))
                .OrderBy(us => us.EffectiveDate)
                .ToListAsync();

            model.Shifts = await _context.Shifts
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            model.Colleagues = await _context.Users
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId && u.Status == "Active" && u.Id != userId)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            model.ShiftSwapRequests = await _context.ShiftSwapRequests
                .Include(r => r.Requester)
                .Include(r => r.TargetUser)
                .AsNoTracking()
                .Where(r => r.RequesterId == userId || r.TargetUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            return model;
        }
    }
}
