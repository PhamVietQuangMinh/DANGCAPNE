using DANGCAPNE.Data;
using DANGCAPNE.Models.Security;
using DANGCAPNE.Models.Timekeeping;
using DANGCAPNE.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DANGCAPNE.Controllers
{
    public class ITController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ITController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsITOrAdmin()
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            return roles.Contains("IT", StringComparer.OrdinalIgnoreCase)
                || roles.Contains("ITManager", StringComparer.OrdinalIgnoreCase)
                || roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        }

        private IActionResult? DenyIfNotIT()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!IsITOrAdmin())
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var from = DateTime.UtcNow.AddDays(-7);

            var recentAuthLogs = await _context.AuthAuditLogs
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .ToListAsync();

            var onlineSessions = await _context.EmployeeOnlineSessions
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.TenantId == tenantId && s.Status == "Online")
                .OrderByDescending(s => s.LastSeenAt)
                .Take(20)
                .ToListAsync();

            var pendingWhitelist = await _context.Users
                .AsNoTracking()
                .Where(u => u.TenantId == tenantId && u.Status == "PendingApproval")
                .OrderByDescending(u => u.CreatedAt)
                .Take(20)
                .ToListAsync();

            var suspiciousLogins = await _context.AuthAuditLogs
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId
                    && x.CreatedAt >= from
                    && x.Action == "LoginFailed")
                .GroupBy(x => x.IpAddress)
                .Select(g => new
                {
                    Ip = g.Key,
                    FailedCount = g.Count()
                })
                .OrderByDescending(x => x.FailedCount)
                .Take(10)
                .ToListAsync();

            var model = new ITDashboardViewModel
            {
                RecentAuthLogs = recentAuthLogs,
                PendingWhitelist = pendingWhitelist,
                OnlineSessions = onlineSessions,
                AssetIncidents = suspiciousLogins.Cast<object>().ToList()
            };

            ViewBag.TotalFailedWeek = recentAuthLogs.Count(x => x.Action == "LoginFailed");
            ViewBag.TotalSuccessWeek = recentAuthLogs.Count(x => x.Action == "LoginSuccess");
            ViewBag.ActiveSessionCount = onlineSessions.Count;
            ViewBag.SuspiciousIpCount = suspiciousLogins.Count(x => x.FailedCount >= 3);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
            if (user == null) return NotFound();

            user.Status = "Active";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Da duyet tai khoan {user.FullName}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(int id)
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
            if (user == null) return NotFound();

            user.Status = "Rejected";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Da tu choi tai khoan {user.FullName}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceLogout(int sessionId)
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var session = await _context.EmployeeOnlineSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId);

            if (session == null) return NotFound();

            session.Status = "Offline";
            session.LogoutAt = DateTime.UtcNow;
            session.LastSeenAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Da ket thuc phien dang nhap.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AllowedIPs()
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var list = await _context.AllowedIps
                .Where(a => a.TenantId == tenantId)
                .Include(a => a.AddedByUser)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewData["Title"] = "Quan ly Wifi cong ty";
            ViewData["DetectedIp"] = NormalizeRemoteIp(HttpContext.Connection.RemoteIpAddress?.ToString());
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string ipAddress, string label)
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var userId = HttpContext.Session.GetInt32("UserId");

            ipAddress = ipAddress?.Trim() ?? string.Empty;
            label = label?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                TempData["Error"] = "Dia chi Wifi khong duoc de trong.";
                return RedirectToAction(nameof(AllowedIPs));
            }

            if (!System.Net.IPAddress.TryParse(ipAddress, out _))
            {
                TempData["Error"] = $"Dia chi Wifi '{ipAddress}' khong hop le.";
                return RedirectToAction(nameof(AllowedIPs));
            }

            var exists = await _context.AllowedIps.AnyAsync(a => a.TenantId == tenantId && a.IpAddress == ipAddress);
            if (exists)
            {
                TempData["Error"] = $"Wifi '{ipAddress}' da ton tai.";
                return RedirectToAction(nameof(AllowedIPs));
            }

            _context.AllowedIps.Add(new AllowedIp
            {
                TenantId = tenantId,
                IpAddress = ipAddress,
                Label = label,
                AddedByUserId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Da them Wifi '{ipAddress}'.";
            return RedirectToAction(nameof(AllowedIPs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var entry = await _context.AllowedIps.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId);
            if (entry == null) return NotFound();

            entry.IsActive = !entry.IsActive;
            entry.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = entry.IsActive ? "Da bat Wifi." : "Da tat Wifi.";
            return RedirectToAction(nameof(AllowedIPs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var entry = await _context.AllowedIps.FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId);
            if (entry == null) return NotFound();

            _context.AllowedIps.Remove(entry);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Da xoa Wifi khoi danh sach.";
            return RedirectToAction(nameof(AllowedIPs));
        }

        private static string NormalizeRemoteIp(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "N/A";
            if (System.Net.IPAddress.TryParse(raw.Trim(), out var ip))
            {
                return System.Net.IPAddress.IsLoopback(ip) ? "127.0.0.1" : ip.ToString();
            }

            return raw;
        }
    }
}
