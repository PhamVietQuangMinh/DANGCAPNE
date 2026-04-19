using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.Models.Timekeeping;
using DANGCAPNE.ViewModels;

namespace DANGCAPNE.Controllers
{
    public class ShiftController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShiftController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsEmployeeOnlySession()
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",", StringSplitOptions.RemoveEmptyEntries);
            // "Đổi ca" chỉ cho user thuần Employee (không kèm các quyền đặc quyền).
            var privileged = new[] { "Admin", "HR", "Manager", "IT", "ITManager", "Accountant", "ChiefAccountant", "AccountantStaff" };
            var hasPrivileged = roles.Any(r => privileged.Contains(r, StringComparer.OrdinalIgnoreCase));
            var hasEmployee = roles.Any(r => string.Equals(r, "Employee", StringComparison.OrdinalIgnoreCase));
            return hasEmployee && !hasPrivileged;
        }

        private async Task<bool> IsEmployeeOnlyUserAsync(int userId, int tenantId)
        {
            var roleNames = await _context.UserRoles
                .Include(ur => ur.Role)
                .AsNoTracking()
                .Where(ur => ur.UserId == userId && ur.Role != null && ur.Role.TenantId == tenantId)
                .Select(ur => ur.Role!.Name)
                .ToListAsync();

            var privileged = new[] { "Admin", "HR", "Manager", "IT", "ITManager", "Accountant", "ChiefAccountant", "AccountantStaff" };
            var hasPrivileged = roleNames.Any(r => privileged.Contains(r, StringComparer.OrdinalIgnoreCase));
            var hasEmployee = roleNames.Any(r => string.Equals(r, "Employee", StringComparison.OrdinalIgnoreCase));
            return hasEmployee && !hasPrivileged;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? year = null, int? month = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var now = DateTime.Today;
            var selectedYear = year ?? now.Year;
            var selectedMonth = month ?? now.Month;
            if (selectedMonth < 1) selectedMonth = 1;
            if (selectedMonth > 12) selectedMonth = 12;

            var monthStart = new DateTime(selectedYear, selectedMonth, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var user = await _context.Users
                .Include(u => u.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            var schedules = await _context.UserShifts
                .Include(us => us.Shift)
                .AsNoTracking()
                .Where(us => us.UserId == userId.Value &&
                             us.EffectiveDate.Date <= monthEnd &&
                             (us.EndDate == null || us.EndDate.Value.Date >= monthStart))
                .OrderBy(us => us.EffectiveDate)
                .ToListAsync();

            var timesheets = await _context.Timesheets
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId && t.UserId == userId.Value && t.Date >= monthStart && t.Date <= monthEnd)
                .ToListAsync();

            var timesheetMap = timesheets.ToDictionary(t => t.Date.Date, t => t);

            static (UserShift? match, DANGCAPNE.Models.Timekeeping.Shift? shift) ResolveShiftForDate(List<UserShift> items, DateTime date)
            {
                var match = items
                    .Where(x => x.EffectiveDate.Date <= date.Date && (x.EndDate == null || x.EndDate.Value.Date >= date.Date))
                    .OrderByDescending(x => x.EffectiveDate)
                    .FirstOrDefault();
                return (match, match?.Shift);
            }

            var days = new List<ShiftCalendarDayViewModel>();
            var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
            for (var d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(monthStart.Year, monthStart.Month, d);
                var (_, shift) = ResolveShiftForDate(schedules, date);

                timesheetMap.TryGetValue(date.Date, out var ts);

                days.Add(new ShiftCalendarDayViewModel
                {
                    Date = date,
                    IsToday = date.Date == now,
                    ShiftName = shift?.Name,
                    ShiftStart = shift?.StartTime,
                    ShiftEnd = shift?.EndTime,
                    AttendanceStatus = ts?.Status,
                    CheckIn = ts?.CheckIn,
                    CheckOut = ts?.CheckOut
                });
            }

            var workingDays = timesheets.Count(t => t.WorkHours > 0);
            var otHours = timesheets.Sum(t => t.OtHours);

            var prev = monthStart.AddMonths(-1);
            var next = monthStart.AddMonths(1);

            var model = new ShiftCalendarViewModel
            {
                UserFullName = user?.FullName ?? "Nhân viên",
                EmployeeCode = user?.EmployeeCode ?? "--",
                DepartmentName = user?.Department?.Name,
                MonthStart = monthStart,
                Days = days,
                WorkingDays = workingDays,
                OtHours = otHours,
                PrevYear = prev.Year,
                PrevMonth = prev.Month,
                NextYear = next.Year,
                NextMonth = next.Month
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSwapRequest(int targetUserId, int requesterShiftId, DateTime requesterDate, int targetShiftId, DateTime targetDate, string reason)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            return Json(new { success = false, message = "Chức năng trao đổi/đổi ca làm đã được tắt." });

            // (Disabled)
        }

        [HttpPost]
        public async Task<IActionResult> RespondToSwap(int requestId, bool accept)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });
            return Json(new { success = false, message = "Chức năng trao đổi/đổi ca làm đã được tắt." });
        }

        [HttpPost]
        public async Task<IActionResult> ManagerApproveSwap(int requestId, bool approve)
        {
            return Json(new { success = false, message = "Chức năng trao đổi/đổi ca làm đã được tắt." });
        }
    }
}
