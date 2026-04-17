using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DANGCAPNE.Data;
using DANGCAPNE.Filters;
using DANGCAPNE.Models.Compliance;
using DANGCAPNE.ViewModels;

namespace DANGCAPNE.Controllers
{
    [PermissionAuthorize("policy.view")]
    public class ComplianceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComplianceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? string.Empty)
                .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var canManage = roles.Contains("Admin") || roles.Contains("HR");

            var activePolicies = await _context.PolicyDocuments
                .Where(p => p.TenantId == tenantId && p.IsActive)
                .OrderByDescending(p => p.PublishedAt)
                .ToListAsync();

            var existingAcknowledgements = await _context.PolicyAcknowledgements
                .Include(a => a.PolicyDocument)
                .Where(a => a.UserId == userId.Value)
                .ToListAsync();

            var existingPolicyIds = existingAcknowledgements.Select(a => a.PolicyDocumentId).ToHashSet();
            foreach (var policy in activePolicies.Where(p => !existingPolicyIds.Contains(p.Id)))
            {
                existingAcknowledgements.Add(new PolicyAcknowledgement
                {
                    PolicyDocumentId = policy.Id,
                    UserId = userId.Value,
                    Status = "Pending"
                });
            }

            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
                existingAcknowledgements = await _context.PolicyAcknowledgements
                    .Include(a => a.PolicyDocument)
                    .Where(a => a.UserId == userId.Value)
                    .ToListAsync();
            }

            existingAcknowledgements = existingAcknowledgements
                .Where(a => a.PolicyDocument != null)
                .ToList();

            var acknowledgedCounts = await _context.PolicyAcknowledgements
                .Where(a => activePolicies.Select(p => p.Id).Contains(a.PolicyDocumentId) && a.Status == "Acknowledged")
                .GroupBy(a => a.PolicyDocumentId)
                .Select(g => new { PolicyDocumentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PolicyDocumentId, x => x.Count);

            var pendingCounts = await _context.PolicyAcknowledgements
                .Where(a => activePolicies.Select(p => p.Id).Contains(a.PolicyDocumentId) && a.Status != "Acknowledged")
                .GroupBy(a => a.PolicyDocumentId)
                .Select(g => new { PolicyDocumentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PolicyDocumentId, x => x.Count);

            var model = new PolicyPortalViewModel
            {
                ActivePolicies = activePolicies,
                MyAcknowledgements = existingAcknowledgements
                    .OrderByDescending(a => a.PolicyDocument?.PublishedAt ?? DateTime.MinValue)
                    .ToList(),
                RecentPolicies = activePolicies.Take(10).ToList(),
                CanManage = canManage,
                AcknowledgedCounts = acknowledgedCounts,
                PendingCounts = pendingCounts
            };

            return View(model);
        }

        [HttpPost]
        [PermissionAuthorize("policy.manage")]
        public async Task<IActionResult> Publish(PolicyPortalViewModel model)
        {
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.FileUrl))
            {
                TempData["Error"] = "Vui lòng nhập tiêu đề và liên kết/chỗ lưu tài liệu.";
                return RedirectToAction("Index");
            }

            var policy = new PolicyDocument
            {
                TenantId = tenantId,
                Title = model.Title.Trim(),
                Version = string.IsNullOrWhiteSpace(model.Version) ? "1.0" : model.Version.Trim(),
                FileUrl = model.FileUrl.Trim(),
                PublishedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.PolicyDocuments.Add(policy);
            await _context.SaveChangesAsync();

            var userIds = await _context.Users
                .Where(u => u.TenantId == tenantId && u.Status == "Active")
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var userId in userIds)
            {
                _context.PolicyAcknowledgements.Add(new PolicyAcknowledgement
                {
                    PolicyDocumentId = policy.Id,
                    UserId = userId,
                    Status = "Pending"
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã đăng chính sách nội bộ mới.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Acknowledge(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var acknowledgement = await _context.PolicyAcknowledgements
                .FirstOrDefaultAsync(a => a.PolicyDocumentId == id && a.UserId == userId.Value);

            if (acknowledgement == null)
            {
                acknowledgement = new PolicyAcknowledgement
                {
                    PolicyDocumentId = id,
                    UserId = userId.Value,
                    Status = "Acknowledged",
                    AcknowledgedAt = DateTime.UtcNow
                };
                _context.PolicyAcknowledgements.Add(acknowledgement);
            }
            else
            {
                acknowledgement.Status = "Acknowledged";
                acknowledgement.AcknowledgedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã ghi nhận bạn đã đọc chính sách.";
            return RedirectToAction("Index");
        }
    }
}
