using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.ViewModels;
using DANGCAPNE.Models.Finance;
using DANGCAPNE.Services;
using System.Text;

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
                var isChiefAccountant = IsChiefAccountant(user, roles);
                var selectedPayrollMonth = string.IsNullOrWhiteSpace(payrollMonth)
                    ? DateTime.Today.ToString("yyyy-MM")
                    : payrollMonth.Trim();
                var accountantModel = await BuildAccountantDashboardModel(userId.Value, tenantId, selectedPayrollMonth, user, isChiefAccountant);
                ViewData["Title"] = "Dashboard kế toán";
                return View("AccountantDashboard", accountantModel);
            }

            var isAdmin = roles.Contains("Admin");
            var isHR = roles.Contains("HR");
            var isManager = roles.Contains("Manager") || roles.Contains("ITManager");

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

            if (!IsChiefAccountant(user, roles))
            {
                TempData["Error"] = "Chức năng này chỉ dành cho Kế toán trưởng.";
                return RedirectToAction(nameof(Index), new { payrollMonth });
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

            var privileged = new[] { "Admin", "HR", "Manager", "IT", "ITManager", "Accountant", "ChiefAccountant", "AccountantStaff" };
            var hasPrivileged = roles.Any(r => privileged.Contains(r, StringComparer.OrdinalIgnoreCase));
            var hasEmployee = roles.Any(r => string.Equals(r, "Employee", StringComparison.OrdinalIgnoreCase));
            var isEmployeeOnly = hasEmployee && !hasPrivileged;
            if (!isEmployeeOnly)
            {
                TempData["Error"] = "Chức năng đổi ca chỉ dành cho nhân viên.";
                return RedirectToAction(nameof(Index));
            }

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

            if (!IsChiefAccountant(user, roles))
            {
                TempData["Error"] = "Chức năng này chỉ dành cho Kế toán trưởng.";
                return RedirectToAction(nameof(Index), new { payrollMonth });
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCashTransaction(string payrollMonth, string transactionType, string channel, DateTime transactionDate, decimal amount, string content, string counterpartyName, string counterpartyType, string? referenceCode)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var user = await _context.Users.Include(u => u.Department).Include(u => u.Position).FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (!IsAccountant(user, roles)) return RedirectToAction(nameof(Index));

            var isChiefAccountant = IsChiefAccountant(user, roles);
            _context.CashTransactions.Add(new CashTransaction
            {
                TenantId = tenantId,
                TransactionType = string.IsNullOrWhiteSpace(transactionType) ? "Expense" : transactionType.Trim(),
                Channel = string.IsNullOrWhiteSpace(channel) ? "Cash" : channel.Trim(),
                TransactionDate = transactionDate,
                Amount = amount,
                Content = content?.Trim() ?? string.Empty,
                CounterpartyName = counterpartyName?.Trim() ?? string.Empty,
                CounterpartyType = counterpartyType?.Trim() ?? "Other",
                ReferenceCode = string.IsNullOrWhiteSpace(referenceCode) ? null : referenceCode.Trim(),
                Status = isChiefAccountant ? "Approved" : "Pending",
                CreatedByUserId = userId.Value,
                ApprovedByUserId = isChiefAccountant ? userId.Value : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã ghi nhận phiếu thu/chi.";
            return RedirectToAction(nameof(Index), new { payrollMonth });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvoiceRecord(string payrollMonth, string invoiceType, string invoiceNo, string counterpartyName, string taxCode, DateTime invoiceDate, DateTime dueDate, decimal amount, decimal paidAmount, decimal creditLimit, string? notes)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var user = await _context.Users.Include(u => u.Department).Include(u => u.Position).FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (!IsAccountant(user, roles)) return RedirectToAction(nameof(Index));

            var outstanding = Math.Max(amount - paidAmount, 0);
            _context.InvoiceRecords.Add(new InvoiceRecord
            {
                TenantId = tenantId,
                InvoiceType = string.IsNullOrWhiteSpace(invoiceType) ? "Receivable" : invoiceType.Trim(),
                InvoiceNo = invoiceNo.Trim(),
                CounterpartyName = counterpartyName?.Trim() ?? string.Empty,
                TaxCode = taxCode?.Trim() ?? string.Empty,
                InvoiceDate = invoiceDate,
                DueDate = dueDate,
                Amount = amount,
                PaidAmount = paidAmount,
                CreditLimit = creditLimit,
                Status = outstanding <= 0 ? "Paid" : paidAmount > 0 ? "Partial" : (dueDate.Date < DateTime.Today ? "Overdue" : "Open"),
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã lưu hóa đơn/công nợ.";
            return RedirectToAction(nameof(Index), new { payrollMonth });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccountingDocument(string payrollMonth, string documentType, string documentNo, string vendorName, string taxCode, DateTime documentDate, decimal amount, string? pdfPath, string? xmlPath)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var user = await _context.Users.Include(u => u.Department).Include(u => u.Position).FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (!IsAccountant(user, roles)) return RedirectToAction(nameof(Index));
            if (!IsChiefAccountant(user, roles))
            {
                TempData["Error"] = "Chức năng này chỉ dành cho Kế toán trưởng.";
                return RedirectToAction(nameof(Index), new { payrollMonth });
            }

            _context.AccountingDocuments.Add(new AccountingDocument
            {
                TenantId = tenantId,
                DocumentType = string.IsNullOrWhiteSpace(documentType) ? "InvoiceIn" : documentType.Trim(),
                DocumentNo = documentNo.Trim(),
                VendorName = vendorName?.Trim() ?? string.Empty,
                TaxCode = taxCode?.Trim() ?? string.Empty,
                DocumentDate = documentDate,
                Amount = amount,
                PdfPath = string.IsNullOrWhiteSpace(pdfPath) ? null : pdfPath.Trim(),
                XmlPath = string.IsNullOrWhiteSpace(xmlPath) ? null : xmlPath.Trim(),
                UploadedByUserId = userId.Value,
                UploadedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã lưu chứng từ kế toán.";
            return RedirectToAction(nameof(Index), new { payrollMonth });
        }

        [HttpGet]
        public async Task<IActionResult> ExportFinanceReport(string reportType, string? payrollMonth = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var user = await _context.Users.Include(u => u.Department).Include(u => u.Position).AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (!IsAccountant(user, roles)) return RedirectToAction(nameof(Index));
            if (!IsChiefAccountant(user, roles))
            {
                TempData["Error"] = "Chức năng này chỉ dành cho Kế toán trưởng.";
                return RedirectToAction(nameof(Index), new { payrollMonth });
            }

            var selectedPayrollMonth = string.IsNullOrWhiteSpace(payrollMonth) ? DateTime.Today.ToString("yyyy-MM") : payrollMonth.Trim();
            if (!TryParsePayrollMonth(selectedPayrollMonth, out var monthStart))
            {
                monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                selectedPayrollMonth = monthStart.ToString("yyyy-MM");
            }
            var monthEnd = monthStart.AddMonths(1);

            var transactions = await _context.CashTransactions.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.TransactionDate >= monthStart && x.TransactionDate < monthEnd)
                .OrderByDescending(x => x.TransactionDate)
                .ToListAsync();
            var invoices = await _context.InvoiceRecords.AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.DueDate)
                .ToListAsync();

            var csv = new StringBuilder();
            var safeType = (reportType ?? "pnl").Trim().ToLowerInvariant();
            if (safeType == "cashflow")
            {
                csv.AppendLine("Ngay,Loai,Kenh,SoTien,DoiTuong,NoiDung,TrangThai");
                foreach (var item in transactions)
                {
                    csv.AppendLine($"{item.TransactionDate:dd/MM/yyyy},{item.TransactionType},{item.Channel},{item.Amount:0.##},{EscapeCsv(item.CounterpartyName)},{EscapeCsv(item.Content)},{item.Status}");
                }
            }
            else if (safeType == "balancesheet")
            {
                var receivables = invoices.Where(x => x.InvoiceType == "Receivable").Sum(x => Math.Max(x.Amount - x.PaidAmount, 0));
                var payables = invoices.Where(x => x.InvoiceType == "Payable").Sum(x => Math.Max(x.Amount - x.PaidAmount, 0));
                var cash = transactions.Where(x => x.Channel == "Cash").Sum(x => x.TransactionType == "Income" ? x.Amount : -x.Amount);
                var bank = transactions.Where(x => x.Channel == "Bank").Sum(x => x.TransactionType == "Income" ? x.Amount : -x.Amount);
                csv.AppendLine("ChiTieu,SoTien");
                csv.AppendLine($"Tien mat,{cash:0.##}");
                csv.AppendLine($"Tien gui ngan hang,{bank:0.##}");
                csv.AppendLine($"Phai thu,{receivables:0.##}");
                csv.AppendLine($"Phai tra,{payables:0.##}");
                csv.AppendLine($"Vi the rong,{(cash + bank + receivables - payables):0.##}");
            }
            else
            {
                var income = transactions.Where(x => x.TransactionType == "Income").Sum(x => x.Amount);
                var expense = transactions.Where(x => x.TransactionType == "Expense").Sum(x => x.Amount);
                csv.AppendLine("ChiTieu,SoTien");
                csv.AppendLine($"Tong thu,{income:0.##}");
                csv.AppendLine($"Tong chi,{expense:0.##}");
                csv.AppendLine($"Loi nhuan thuan,{(income - expense):0.##}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"{safeType}-{selectedPayrollMonth}.csv");
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
            var isManager = roles.Contains("Manager") || roles.Contains("ITManager");

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

            var colleaguesRaw = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId && u.Status == "Active" && u.Id != userId)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            static bool IsEmployeeOnlyUser(DANGCAPNE.Models.Organization.User u)
            {
                var roleNames = u.UserRoles
                    .Select(ur => ur.Role?.Name ?? string.Empty)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var privileged = new[] { "Admin", "HR", "Manager", "IT", "ITManager", "Accountant", "ChiefAccountant", "AccountantStaff" };
                var hasPrivileged = privileged.Any(roleNames.Contains);
                var hasEmployee = roleNames.Contains("Employee");
                if (hasPrivileged || !hasEmployee)
                {
                    return false;
                }

                return true;
            }

            model.Colleagues = colleaguesRaw.Where(IsEmployeeOnlyUser).ToList();

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
                   roles.Contains("ChiefAccountant") ||
                   roles.Contains("AccountantStaff") ||
                   string.Equals(HttpContext.Session.GetString("PrimaryRole"), "Accountant", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(user?.Department?.Code, "ACC", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(user?.Department?.Name, "Phòng Kế toán", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(user?.Position?.Name, "Kế toán trưởng", StringComparison.OrdinalIgnoreCase) ||
                   (user?.Email?.Contains("accountant", StringComparison.OrdinalIgnoreCase) ?? false);
        }

        private static bool IsChiefAccountant(DANGCAPNE.Models.Organization.User? user, string[] roles)
        {
            return roles.Contains("ChiefAccountant") ||
                   string.Equals(user?.Position?.Name, "Kế toán trưởng", StringComparison.OrdinalIgnoreCase);
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
            DANGCAPNE.Models.Organization.User? currentUser,
            bool isChiefAccountant)
        {
            if (!TryParsePayrollMonth(selectedPayrollMonth, out var monthStart))
            {
                monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                selectedPayrollMonth = monthStart.ToString("yyyy-MM");
            }

            var monthEnd = monthStart.AddMonths(1);
            var payrollUsers = new List<DANGCAPNE.Models.Organization.User>();
            var monthTimesheets = new List<DANGCAPNE.Models.Timekeeping.Timesheet>();
            if (isChiefAccountant)
            {
                payrollUsers = await _context.Users
                    .Include(u => u.Department)
                    .AsNoTracking()
                    .Where(u => u.TenantId == tenantId && u.Status == "Active" && u.DepartmentId != 1)
                    .OrderBy(u => u.FullName)
                    .ToListAsync();

                var payrollUserIds = payrollUsers.Select(u => u.Id).ToList();
                monthTimesheets = await _context.Timesheets
                    .AsNoTracking()
                    .Where(t => t.TenantId == tenantId
                        && payrollUserIds.Contains(t.UserId)
                        && t.Date >= monthStart
                        && t.Date < monthEnd)
                    .ToListAsync();
            }

            var pendingApprovals = new List<DANGCAPNE.Models.Requests.Request>();
            if (isChiefAccountant)
            {
                pendingApprovals = await _context.Requests
                    .Include(r => r.Requester)
                    .Include(r => r.FormTemplate)
                    .AsNoTracking()
                    .Where(r => r.TenantId == tenantId &&
                        r.Approvals.Any(a => a.ApproverId == userId && a.Status == "Pending"))
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(8)
                    .ToListAsync();
            }

            var salaryAdvances = new List<DANGCAPNE.Models.HR.SalaryAdvanceRequest>();
            if (isChiefAccountant)
            {
                salaryAdvances = await _context.SalaryAdvanceRequests
                    .Include(r => r.User)
                    .AsNoTracking()
                    .Where(r => r.TenantId == tenantId
                        && r.PayrollMonth == selectedPayrollMonth
                        && (r.Status == "Pending" || r.Status == "Approved"))
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(8)
                    .ToListAsync();
            }

            var recentClosures = new List<PayrollClosure>();
            var recentPayrollSlips = new List<PayrollSlip>();
            if (isChiefAccountant)
            {
                recentClosures = await _context.PayrollClosures
                    .Include(p => p.ClosedByUser)
                    .AsNoTracking()
                    .Where(p => p.TenantId == tenantId)
                    .OrderByDescending(p => p.ClosedAt)
                    .Take(6)
                    .ToListAsync();

                recentPayrollSlips = await _context.PayrollSlips
                    .Include(s => s.User)
                    .AsNoTracking()
                    .Where(s => s.TenantId == tenantId && s.PayrollMonth == selectedPayrollMonth)
                    .OrderByDescending(s => s.NetSalary)
                    .Take(8)
                    .ToListAsync();
            }

            var cashTransactions = await _context.CashTransactions
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.TransactionDate >= monthStart && x.TransactionDate < monthEnd)
                .OrderByDescending(x => x.TransactionDate)
                .Take(8)
                .ToListAsync();

            var invoiceRecords = await _context.InvoiceRecords
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.DueDate)
                .ToListAsync();

            var accountingDocuments = await _context.AccountingDocuments
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.DocumentDate)
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

            var dueInvoices = invoiceRecords
                .Where(x => x.DueDate.Date >= DateTime.Today && x.DueDate.Date <= DateTime.Today.AddDays(5))
                .Select(x => new InvoiceSummaryViewModel
                {
                    Id = x.Id,
                    InvoiceNo = x.InvoiceNo,
                    InvoiceType = x.InvoiceType,
                    CounterpartyName = x.CounterpartyName,
                    TaxCode = x.TaxCode,
                    Amount = x.Amount,
                    PaidAmount = x.PaidAmount,
                    OutstandingAmount = Math.Max(x.Amount - x.PaidAmount, 0),
                    DueDate = x.DueDate,
                    AgingDays = Math.Max((DateTime.Today - x.InvoiceDate.Date).Days, 0),
                    Status = x.Status,
                    OverCreditLimit = x.InvoiceType == "Receivable" && x.CreditLimit > 0 && Math.Max(x.Amount - x.PaidAmount, 0) > x.CreditLimit
                })
                .ToList();

            var receivableInvoices = invoiceRecords
                .Where(x => x.InvoiceType == "Receivable")
                .Select(x => new InvoiceSummaryViewModel
                {
                    Id = x.Id,
                    InvoiceNo = x.InvoiceNo,
                    InvoiceType = x.InvoiceType,
                    CounterpartyName = x.CounterpartyName,
                    TaxCode = x.TaxCode,
                    Amount = x.Amount,
                    PaidAmount = x.PaidAmount,
                    OutstandingAmount = Math.Max(x.Amount - x.PaidAmount, 0),
                    DueDate = x.DueDate,
                    AgingDays = Math.Max((DateTime.Today - x.InvoiceDate.Date).Days, 0),
                    Status = x.Status,
                    OverCreditLimit = x.CreditLimit > 0 && Math.Max(x.Amount - x.PaidAmount, 0) > x.CreditLimit
                })
                .OrderByDescending(x => x.OutstandingAmount)
                .Take(8)
                .ToList();

            var payableInvoices = invoiceRecords
                .Where(x => x.InvoiceType == "Payable")
                .Select(x => new InvoiceSummaryViewModel
                {
                    Id = x.Id,
                    InvoiceNo = x.InvoiceNo,
                    InvoiceType = x.InvoiceType,
                    CounterpartyName = x.CounterpartyName,
                    TaxCode = x.TaxCode,
                    Amount = x.Amount,
                    PaidAmount = x.PaidAmount,
                    OutstandingAmount = Math.Max(x.Amount - x.PaidAmount, 0),
                    DueDate = x.DueDate,
                    AgingDays = Math.Max((DateTime.Today - x.InvoiceDate.Date).Days, 0),
                    Status = x.Status,
                    OverCreditLimit = false
                })
                .OrderByDescending(x => x.OutstandingAmount)
                .Take(8)
                .ToList();

            var totalCashIn = cashTransactions.Where(x => x.TransactionType == "Income").Sum(x => x.Amount);
            var totalCashOut = cashTransactions.Where(x => x.TransactionType == "Expense").Sum(x => x.Amount);
            var cashOnHand = cashTransactions.Where(x => x.Channel == "Cash").Sum(x => x.TransactionType == "Income" ? x.Amount : -x.Amount);
            var bankBalance = cashTransactions.Where(x => x.Channel == "Bank").Sum(x => x.TransactionType == "Income" ? x.Amount : -x.Amount);
            var receivableOutstanding = receivableInvoices.Sum(x => x.OutstandingAmount);
            var payableOutstanding = payableInvoices.Sum(x => x.OutstandingAmount);

            var reportLines = cashTransactions
                .GroupBy(x => x.TransactionType == "Income" ? "Thu tiền" : "Chi tiền")
                .Select(g => new FinancialReportLineViewModel { Label = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToList();

            return new AccountantDashboardViewModel
            {
                CurrentUser = currentUser,
                RoleName = "Accountant",
                IsChiefAccountant = isChiefAccountant,
                UnreadNotifications = await _context.Notifications
                    .AsNoTracking()
                    .CountAsync(n => n.UserId == userId && !n.IsRead),
                SelectedPayrollMonth = selectedPayrollMonth,
                IsMonthClosed = isChiefAccountant && recentClosures.Any(c => c.PayrollMonth == selectedPayrollMonth),
                TotalPayrollEmployees = isChiefAccountant ? payrollUsers.Count : 0,
                TotalTimesheets = isChiefAccountant ? monthTimesheets.Count : 0,
                TotalWorkHours = isChiefAccountant ? Convert.ToDecimal(monthTimesheets.Sum(t => t.WorkHours)) : 0,
                TotalOtHours = isChiefAccountant ? Convert.ToDecimal(monthTimesheets.Sum(t => t.OtHours)) : 0,
                PendingFinanceApprovals = pendingApprovals.Count,
                PendingSalaryAdvanceCount = isChiefAccountant ? salaryAdvances.Count : 0,
                LateAttendanceCount = isChiefAccountant ? monthTimesheets.Count(t => t.Status == "Late") : 0,
                AbsentAttendanceCount = isChiefAccountant ? monthTimesheets.Count(t => t.Status == "Absent") : 0,
                PendingApprovals = pendingApprovals,
                SalaryAdvances = salaryAdvances,
                RecentClosures = recentClosures,
                RecentPayrollSlips = recentPayrollSlips,
                TotalCashIn = totalCashIn,
                TotalCashOut = totalCashOut,
                CashOnHand = cashOnHand,
                BankBalance = bankBalance,
                TotalReceivableDueSoon = dueInvoices.Where(x => x.InvoiceType == "Receivable").Sum(x => x.OutstandingAmount),
                TotalPayableDueSoon = dueInvoices.Where(x => x.InvoiceType == "Payable").Sum(x => x.OutstandingAmount),
                DueReceivableCount = dueInvoices.Count(x => x.InvoiceType == "Receivable"),
                DuePayableCount = dueInvoices.Count(x => x.InvoiceType == "Payable"),
                RecentCashTransactions = cashTransactions.Select(x => new CashTransactionViewModel
                {
                    Id = x.Id,
                    TransactionDate = x.TransactionDate,
                    Type = x.TransactionType,
                    Channel = x.Channel,
                    Amount = x.Amount,
                    CounterpartyName = x.CounterpartyName,
                    Content = x.Content,
                    Status = x.Status,
                    ReferenceCode = x.ReferenceCode
                }).ToList(),
                DueInvoices = dueInvoices,
                ReceivableInvoices = receivableInvoices,
                PayableInvoices = payableInvoices,
                AccountingDocuments = accountingDocuments.Select(x => new AccountingDocumentViewModel
                {
                    Id = x.Id,
                    DocumentNo = x.DocumentNo,
                    DocumentType = x.DocumentType,
                    TaxCode = x.TaxCode,
                    VendorName = x.VendorName,
                    DocumentDate = x.DocumentDate,
                    Amount = x.Amount,
                    PdfPath = x.PdfPath,
                    XmlPath = x.XmlPath
                }).ToList(),
                ProfitAndLoss = new FinancialReportSnapshotViewModel
                {
                    TotalIncome = isChiefAccountant ? totalCashIn : 0,
                    TotalExpense = isChiefAccountant ? totalCashOut : 0,
                    NetAmount = isChiefAccountant ? (totalCashIn - totalCashOut) : 0,
                    Lines = isChiefAccountant ? reportLines : new List<FinancialReportLineViewModel>()
                },
                CashFlow = new FinancialReportSnapshotViewModel
                {
                    TotalIncome = isChiefAccountant ? totalCashIn : 0,
                    TotalExpense = isChiefAccountant ? totalCashOut : 0,
                    NetAmount = isChiefAccountant ? (totalCashIn - totalCashOut) : 0,
                    Lines = isChiefAccountant
                        ? cashTransactions.Select(x => new FinancialReportLineViewModel
                        {
                            Label = $"{x.TransactionDate:dd/MM} - {x.CounterpartyName}",
                            Amount = x.TransactionType == "Income" ? x.Amount : -x.Amount
                        }).ToList()
                        : new List<FinancialReportLineViewModel>()
                },
                BalanceSheet = new BalanceSheetSnapshotViewModel
                {
                    CashAndBank = isChiefAccountant ? (cashOnHand + bankBalance) : 0,
                    Receivables = isChiefAccountant ? receivableOutstanding : 0,
                    Payables = isChiefAccountant ? payableOutstanding : 0,
                    NetPosition = isChiefAccountant ? (cashOnHand + bankBalance + receivableOutstanding - payableOutstanding) : 0
                },
                PayrollEmployees = isChiefAccountant
                    ? payrollUsers
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
                    : new List<PayrollEmployeeSummaryViewModel>()
            };
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Replace("\"", "\"\"");
            return normalized.Contains(',') ? $"\"{normalized}\"" : normalized;
        }
    }
}
