using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.ViewModels;
using DANGCAPNE.Models.Finance;
using DANGCAPNE.Services;

namespace DANGCAPNE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPayrollPdfService _payrollPdfService;

        public HomeController(ApplicationDbContext context, IPayrollPdfService payrollPdfService)
        {
            _context = context;
            _payrollPdfService = payrollPdfService;
        }

        public async Task<IActionResult> Index(string? payrollMonth = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.JobTitle)
                .Include(u => u.Position)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (IsAccountant(user, roles))
            {
                var selectedPayrollMonth = string.IsNullOrWhiteSpace(payrollMonth)
                    ? DateTime.Today.ToString("yyyy-MM")
                    : payrollMonth.Trim();
                var accountantModel = await BuildAccountantDashboardModel(userId.Value, tenantId, selectedPayrollMonth, user);
                ViewData["Title"] = "Dashboard kế toán";
                return View("AccountantDashboard", accountantModel);
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClosePayroll(string payrollMonth, string? notes)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (!IsAccountant(user, roles))
            {
                TempData["Error"] = "Bạn không có quyền chốt lương.";
                return RedirectToAction(nameof(Index));
            }

            if (!TryParsePayrollMonth(payrollMonth, out var monthStart))
            {
                TempData["Error"] = "Tháng lương không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            if (monthStart > new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1))
            {
                TempData["Error"] = "Không thể chốt lương cho tháng trong tương lai.";
                return RedirectToAction(nameof(Index));
            }

            var monthEnd = monthStart.AddMonths(1);
            var alreadyClosed = await _context.PayrollClosures
                .AsNoTracking()
                .AnyAsync(p => p.TenantId == tenantId && p.PayrollMonth == payrollMonth);

            if (alreadyClosed)
            {
                TempData["Error"] = $"Tháng {payrollMonth} đã được chốt trước đó.";
                return RedirectToAction(nameof(Index));
            }

            var payrollUsers = await _context.Users
                .Include(u => u.Position)
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId && u.Status == "Active")
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var payrollUserIds = payrollUsers.Select(u => u.Id).ToList();
            var monthTimesheets = await _context.Timesheets
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId
                    && payrollUserIds.Contains(t.UserId)
                    && t.Date >= monthStart
                    && t.Date < monthEnd)
                .ToListAsync();

            var attendances = await _context.DailyAttendances
                .AsNoTracking()
                .Where(a => a.TenantId == tenantId
                    && payrollUserIds.Contains(a.UserId)
                    && a.Date >= monthStart
                    && a.Date < monthEnd)
                .ToListAsync();

            var pendingAdvanceCount = await _context.SalaryAdvanceRequests
                .AsNoTracking()
                .CountAsync(r => r.TenantId == tenantId
                    && r.PayrollMonth == payrollMonth
                    && (r.Status == "Pending" || r.Status == "Approved"));

            var closure = new PayrollClosure
            {
                TenantId = tenantId,
                PayrollMonth = payrollMonth,
                ClosedByUserId = userId.Value,
                ClosedAt = DateTime.Now,
                EmployeeCount = payrollUsers.Count,
                TimesheetCount = monthTimesheets.Count,
                TotalWorkHours = Convert.ToDecimal(monthTimesheets.Sum(t => t.WorkHours)),
                TotalOtHours = Convert.ToDecimal(monthTimesheets.Sum(t => t.OtHours)),
                PendingAdvanceCount = pendingAdvanceCount,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };

            _context.PayrollClosures.Add(closure);
            await _context.SaveChangesAsync();

            var approvedAdvances = await _context.SalaryAdvanceRequests
                .AsNoTracking()
                .Where(r => r.TenantId == tenantId
                    && payrollUserIds.Contains(r.UserId)
                    && r.PayrollMonth == payrollMonth
                    && (r.Status == "Approved" || r.Status == "Paid"))
                .ToListAsync();

            var slips = new List<PayrollSlip>();
            foreach (var payrollUser in payrollUsers)
            {
                var userTimesheets = monthTimesheets.Where(t => t.UserId == payrollUser.Id).ToList();
                var userAttendances = attendances.Where(a => a.UserId == payrollUser.Id).ToList();
                var baseMonthlySalary = payrollUser.BaseSalary * payrollUser.SalaryCoefficient;
                var standardWorkDays = payrollUser.StandardWorkDays <= 0 ? 26 : payrollUser.StandardWorkDays;
                var standardHoursPerDay = payrollUser.StandardWorkHoursPerDay <= 0 ? 8 : payrollUser.StandardWorkHoursPerDay;
                var dailyRate = standardWorkDays > 0 ? baseMonthlySalary / standardWorkDays : 0;
                var hourlyRate = standardHoursPerDay > 0 ? dailyRate / standardHoursPerDay : 0;
                var actualWorkHours = Convert.ToDecimal(userTimesheets.Sum(t => t.WorkHours));
                var overtimeHours = Convert.ToDecimal(userTimesheets.Sum(t => t.OtHours));
                var actualWorkingDays = userTimesheets.Count(t => t.WorkHours > 0);
                var lateMinutes = userAttendances.Sum(a => a.LateMinutes);
                var mainSalary = hourlyRate * actualWorkHours;
                var overtimeSalary = hourlyRate * payrollUser.OvertimeHourlyMultiplier * overtimeHours;
                var latePenalty = payrollUser.LatePenaltyPerMinute * lateMinutes;
                var advanceDeduction = approvedAdvances
                    .Where(a => a.UserId == payrollUser.Id)
                    .Sum(a => a.Amount);
                var netSalary = Math.Max(0, mainSalary + overtimeSalary + payrollUser.FixedAllowance + payrollUser.OtherIncome - latePenalty - advanceDeduction);

                slips.Add(new PayrollSlip
                {
                    TenantId = tenantId,
                    PayrollClosureId = closure.Id,
                    UserId = payrollUser.Id,
                    PayrollMonth = payrollMonth,
                    BaseSalary = payrollUser.BaseSalary,
                    SalaryCoefficient = payrollUser.SalaryCoefficient,
                    StandardWorkDays = standardWorkDays,
                    StandardWorkHoursPerDay = standardHoursPerDay,
                    ActualWorkingDays = actualWorkingDays,
                    ActualWorkHours = actualWorkHours,
                    OvertimeHours = overtimeHours,
                    LateMinutes = lateMinutes,
                    HourlyRate = hourlyRate,
                    MainSalary = mainSalary,
                    OvertimeSalary = overtimeSalary,
                    FixedAllowance = payrollUser.FixedAllowance,
                    OtherIncome = payrollUser.OtherIncome,
                    LatePenalty = latePenalty,
                    AdvanceDeduction = advanceDeduction,
                    NetSalary = netSalary,
                    CreatedAt = DateTime.Now
                });
            }

            _context.PayrollSlips.AddRange(slips);
            _context.Notifications.Add(new Models.SystemModels.Notification
            {
                TenantId = tenantId,
                UserId = userId.Value,
                Title = $"Đã chốt lương tháng {payrollMonth}",
                Message = $"Bảng lương tháng {payrollMonth} đã được chốt với {closure.EmployeeCount} nhân sự và {closure.TotalOtHours:N1} giờ OT.",
                Type = "System",
                ActionUrl = Url.Action(nameof(Index), "Home")
            });
            await _context.SaveChangesAsync();

            foreach (var slip in slips)
            {
                await _payrollPdfService.GeneratePayrollSlipPdfAsync(slip.Id);
            }

            TempData["Success"] = $"Đã chốt lương tháng {payrollMonth} thành công.";
            return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> PayrollRecords(string? payrollMonth = null, int? selectedUserId = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.Position)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (!IsAccountant(user, roles))
            {
                return RedirectToAction(nameof(Index));
            }

            var selectedPayrollMonth = string.IsNullOrWhiteSpace(payrollMonth)
                ? DateTime.Today.ToString("yyyy-MM")
                : payrollMonth.Trim();

            if (!TryParsePayrollMonth(selectedPayrollMonth, out _))
            {
                selectedPayrollMonth = DateTime.Today.ToString("yyyy-MM");
            }

            var slips = await _context.PayrollSlips
                .Include(s => s.User).ThenInclude(u => u!.Department)
                .Include(s => s.User).ThenInclude(u => u!.Position)
                .Include(s => s.PayrollClosure)
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.PayrollMonth == selectedPayrollMonth)
                .OrderByDescending(s => s.NetSalary)
                .ThenBy(s => s.User!.FullName)
                .ToListAsync();

            var selectedSlip = selectedUserId.HasValue
                ? slips.FirstOrDefault(s => s.UserId == selectedUserId.Value)
                : slips.FirstOrDefault();

            var closures = await _context.PayrollClosures
                .Include(c => c.ClosedByUser)
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .OrderByDescending(c => c.ClosedAt)
                .Take(12)
                .ToListAsync();

            var model = new PayrollRecordsViewModel
            {
                SelectedPayrollMonth = selectedPayrollMonth,
                PayrollSlips = slips,
                PayrollClosures = closures,
                TotalNetSalary = slips.Sum(s => s.NetSalary),
                TotalAdvanceDeduction = slips.Sum(s => s.AdvanceDeduction),
                TotalLatePenalty = slips.Sum(s => s.LatePenalty),
                SelectedPayrollSlip = selectedSlip
            };

            ViewData["Title"] = "Hồ sơ lương";
            return View("PayrollRecords", model);
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

        private bool IsAccountant(DANGCAPNE.Models.Organization.User? user, string[] roles)
        {
            return roles.Contains("Accountant") ||
                   string.Equals(HttpContext.Session.GetString("PrimaryRole"), "Accountant", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(user?.Department?.Code, "ACC", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(user?.Department?.Name, "Phòng Kế toán", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(user?.Position?.Name, "Kế toán trưởng", StringComparison.OrdinalIgnoreCase) ||
                   (user?.Email?.Contains("accountant", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private bool TryParsePayrollMonth(string? payrollMonth, out DateTime monthStart)
        {
            return DateTime.TryParseExact(
                $"{payrollMonth}-01",
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out monthStart);
        }

        private async Task<AccountantDashboardViewModel> BuildAccountantDashboardModel(
            int userId,
            int tenantId,
            string selectedPayrollMonth,
            DANGCAPNE.Models.Organization.User? currentUser)
        {
            if (!TryParsePayrollMonth(selectedPayrollMonth, out var monthStart))
            {
                monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                selectedPayrollMonth = monthStart.ToString("yyyy-MM");
            }

            var monthEnd = monthStart.AddMonths(1);
            var payrollUsers = await _context.Users
                .Include(u => u.Department)
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId && u.Status == "Active" && u.DepartmentId != 1)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var payrollUserIds = payrollUsers.Select(u => u.Id).ToList();
            var monthTimesheets = await _context.Timesheets
                .AsNoTracking()
                .Where(t => t.TenantId == tenantId
                    && payrollUserIds.Contains(t.UserId)
                    && t.Date >= monthStart
                    && t.Date < monthEnd)
                .ToListAsync();

            var pendingApprovals = await _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.FormTemplate)
                .AsNoTracking()
                .Where(r => r.TenantId == tenantId &&
                    r.Approvals.Any(a => a.ApproverId == userId && a.Status == "Pending"))
                .OrderByDescending(r => r.CreatedAt)
                .Take(8)
                .ToListAsync();

            var salaryAdvances = await _context.SalaryAdvanceRequests
                .Include(r => r.User)
                .AsNoTracking()
                .Where(r => r.TenantId == tenantId
                    && r.PayrollMonth == selectedPayrollMonth
                    && (r.Status == "Pending" || r.Status == "Approved"))
                .OrderByDescending(r => r.CreatedAt)
                .Take(8)
                .ToListAsync();

            var recentClosures = await _context.PayrollClosures
                .Include(p => p.ClosedByUser)
                .AsNoTracking()
                .Where(p => p.TenantId == tenantId)
                .OrderByDescending(p => p.ClosedAt)
                .Take(6)
                .ToListAsync();

            var recentPayrollSlips = await _context.PayrollSlips
                .Include(s => s.User)
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.PayrollMonth == selectedPayrollMonth)
                .OrderByDescending(s => s.NetSalary)
                .Take(8)
                .ToListAsync();

            var groupedTimesheets = monthTimesheets
                .GroupBy(t => t.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        WorkingDays = g.Count(x => x.WorkHours > 0),
                        WorkHours = Convert.ToDecimal(g.Sum(x => x.WorkHours)),
                        OtHours = Convert.ToDecimal(g.Sum(x => x.OtHours)),
                        LateDays = g.Count(x => x.Status == "Late"),
                        AbsentDays = g.Count(x => x.Status == "Absent")
                    });

            return new AccountantDashboardViewModel
            {
                CurrentUser = currentUser,
                RoleName = "Accountant",
                UnreadNotifications = await _context.Notifications
                    .AsNoTracking()
                    .CountAsync(n => n.UserId == userId && !n.IsRead),
                SelectedPayrollMonth = selectedPayrollMonth,
                IsMonthClosed = recentClosures.Any(c => c.PayrollMonth == selectedPayrollMonth),
                TotalPayrollEmployees = payrollUsers.Count,
                TotalTimesheets = monthTimesheets.Count,
                TotalWorkHours = Convert.ToDecimal(monthTimesheets.Sum(t => t.WorkHours)),
                TotalOtHours = Convert.ToDecimal(monthTimesheets.Sum(t => t.OtHours)),
                PendingFinanceApprovals = pendingApprovals.Count,
                PendingSalaryAdvanceCount = salaryAdvances.Count,
                LateAttendanceCount = monthTimesheets.Count(t => t.Status == "Late"),
                AbsentAttendanceCount = monthTimesheets.Count(t => t.Status == "Absent"),
                PendingApprovals = pendingApprovals,
                SalaryAdvances = salaryAdvances,
                RecentClosures = recentClosures,
                RecentPayrollSlips = recentPayrollSlips,
                PayrollEmployees = payrollUsers
                    .Select(u =>
                    {
                        groupedTimesheets.TryGetValue(u.Id, out var summary);
                        return new PayrollEmployeeSummaryViewModel
                        {
                            UserId = u.Id,
                            EmployeeName = u.FullName,
                            DepartmentName = u.Department?.Name ?? "--",
                            WorkingDays = summary?.WorkingDays ?? 0,
                            WorkHours = summary?.WorkHours ?? 0,
                            OtHours = summary?.OtHours ?? 0,
                            LateDays = summary?.LateDays ?? 0,
                            AbsentDays = summary?.AbsentDays ?? 0
                        };
                    })
                    .OrderByDescending(x => x.WorkHours)
                    .ThenBy(x => x.EmployeeName)
                    .Take(10)
                    .ToList()
            };
        }
    }
}
