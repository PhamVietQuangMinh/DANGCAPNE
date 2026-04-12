using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.Models.Security;

namespace DANGCAPNE.Controllers
{
    public class ITController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ITController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Guard helper ────────────────────────────────────────────────────────
        private bool IsITOrAdmin()
        {
            var roles = (HttpContext.Session.GetString("Roles") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            return roles.Contains("IT", StringComparer.OrdinalIgnoreCase)
                || roles.Contains("ITManager", StringComparer.OrdinalIgnoreCase)
                || roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
        }

        private IActionResult DenyIfNotIT()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
                return RedirectToAction("Login", "Account");
            if (!IsITOrAdmin())
                return RedirectToAction("AccessDenied", "Account");
            return null!;
        }

        // ── GET /IT/AllowedIPs ──────────────────────────────────────────────────
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

            ViewData["Title"] = "Quản lý Wifi công ty";
            ViewData["DetectedIp"] = NormalizeRemoteIp(HttpContext.Connection.RemoteIpAddress?.ToString());
            return View(list);
        }

        // ── POST /IT/AllowedIPs/Add ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string ipAddress, string label)
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var userId   = HttpContext.Session.GetInt32("UserId");

            ipAddress = ipAddress?.Trim() ?? "";
            label     = label?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                TempData["Error"] = "Địa chỉ Wifi không được để trống.";
                return RedirectToAction("AllowedIPs");
            }

            // Basic IPv4 format validation
            if (!System.Net.IPAddress.TryParse(ipAddress, out _))
            {
                TempData["Error"] = $"Địa chỉ Wifi '{ipAddress}' không hợp lệ.";
                return RedirectToAction("AllowedIPs");
            }

            // Duplicate check
            bool exists = await _context.AllowedIps
                .AnyAsync(a => a.TenantId == tenantId && a.IpAddress == ipAddress);
            if (exists)
            {
                TempData["Error"] = $"Wifi '{ipAddress}' đã được thêm vào danh sách trước đó.";
                return RedirectToAction("AllowedIPs");
            }

            var entry = new AllowedIp
            {
                TenantId      = tenantId,
                IpAddress     = ipAddress,
                Label         = label,
                AddedByUserId = userId,
                IsActive      = true,
                CreatedAt     = DateTime.UtcNow,
                UpdatedAt     = DateTime.UtcNow
            };
            _context.AllowedIps.Add(entry);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã thêm Wifi '{ipAddress}' ({label}) vào danh sách cho phép.";
            return RedirectToAction("AllowedIPs");
        }

        // ── POST /IT/AllowedIPs/Toggle/5 ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var entry = await _context.AllowedIps
                .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId);

            if (entry == null) return NotFound();

            entry.IsActive  = !entry.IsActive;
            entry.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var state = entry.IsActive ? "bật" : "tắt";
            TempData["Success"] = $"Đã {state} Wifi '{entry.IpAddress}'.";
            return RedirectToAction("AllowedIPs");
        }

        // ── POST /IT/AllowedIPs/Delete/5 ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var deny = DenyIfNotIT();
            if (deny != null) return deny;

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var entry = await _context.AllowedIps
                .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId);

            if (entry == null) return NotFound();

            _context.AllowedIps.Remove(entry);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa Wifi '{entry.IpAddress}' khỏi danh sách.";
            return RedirectToAction("AllowedIPs");
        }

        // ── Helper ──────────────────────────────────────────────────────────────
        private static string NormalizeRemoteIp(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "N/A";
            if (System.Net.IPAddress.TryParse(raw.Trim(), out var ip))
                return System.Net.IPAddress.IsLoopback(ip) ? "127.0.0.1" : ip.ToString();
            return raw;
        }
    }
}
