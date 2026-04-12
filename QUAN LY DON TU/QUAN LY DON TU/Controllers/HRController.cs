using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.ViewModels;
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
    }
}
