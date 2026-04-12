using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using DANGCAPNE.Data;
using DANGCAPNE.Hubs;
using DANGCAPNE.Models.HR;
using DANGCAPNE.Models.Requests;
using DANGCAPNE.Models.Timekeeping;
using DANGCAPNE.Models.Workflow;
using DANGCAPNE.Services;
using DANGCAPNE.ViewModels;

namespace DANGCAPNE.Controllers
{
    public class ApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IApprovalSlaService _slaService;
        private readonly IApprovedRequestPdfService _pdfService;

        public ApprovalsController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, IApprovalSlaService slaService, IApprovedRequestPdfService pdfService)
        {
            _context = context;
            _hubContext = hubContext;
            _slaService = slaService;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var pending = await _context.RequestApprovals
                .Include(a => a.Request).ThenInclude(r => r!.Requester)
                .Include(a => a.Request).ThenInclude(r => r!.FormTemplate)
                .Include(a => a.Approver)
                .Where(a => a.ApproverId == userId && a.Status == "Pending")
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var processed = await _context.RequestApprovals
                .Include(a => a.Request).ThenInclude(r => r!.Requester)
                .Include(a => a.Request).ThenInclude(r => r!.FormTemplate)
                .Include(a => a.Approver)
                .Where(a => a.ApproverId == userId && a.Status != "Pending")
                .OrderByDescending(a => a.ActionDate)
                .Take(50)
                .ToListAsync();

            var model = new ApprovalListViewModel
            {
                PendingApprovals = pending,
                ProcessedApprovals = processed,
                ApprovalSla = await _slaService.BuildForApprovalsAsync(pending.Concat(processed).ToList()),
                StatusFilter = status,
                TotalPending = pending.Count
            };

            return View(model);
        }

                [HttpPost]
        public async Task<IActionResult> ProcessApproval(ApprovalActionViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var approval = await _context.RequestApprovals
                .Include(a => a.Request).ThenInclude(r => r!.FormTemplate)
                .FirstOrDefaultAsync(a => a.Id == model.ApprovalId && a.ApproverId == userId);

            if (approval == null) return NotFound();

            if (approval.Request?.FormTemplate?.RequiresFinancialApproval == true && model.Action == "Approve")
            {
                if (string.IsNullOrEmpty(model.Pin) || model.Pin != "1234")
                {
                    TempData["Error"] = "Mã PIN không chính xác. Vui lòng thử lại.";
                    return RedirectToAction("Detail", "Requests", new { id = approval.RequestId });
                }
                approval.VerifiedByPin = true;
            }

            var request = approval.Request!;
            var oldStatus = request.Status;
            var finalApproved = false;

            if (model.Action == "Approve")
            {
                approval.Status = "Approved";
                approval.ActionDate = DateTime.UtcNow;
                approval.Comments = model.Comments;
                approval.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var nextApproval = await _context.RequestApprovals
                    .Where(a => a.RequestId == request.Id && a.StepOrder > approval.StepOrder && a.Status == "Pending")
                    .OrderBy(a => a.StepOrder)
                    .FirstOrDefaultAsync();

                while (nextApproval != null && nextApproval.Status == "Skipped")
                {
                    nextApproval = await _context.RequestApprovals
                        .Where(a => a.RequestId == request.Id && a.StepOrder > nextApproval.StepOrder && a.Status == "Pending")
                        .OrderBy(a => a.StepOrder)
                        .FirstOrDefaultAsync();
                }

                if (nextApproval != null)
                {
                    request.Status = "InProgress";
                    request.CurrentStepOrder = nextApproval.StepOrder;

                    if (nextApproval.ApproverId != null)
                    {
                        _context.Notifications.Add(new Models.SystemModels.Notification
                        {
                            TenantId = tenantId,
                            UserId = nextApproval.ApproverId.Value,
                            Title = "Đơn cần duyệt",
                            Message = $"Đơn {request.RequestCode} đã được duyệt bước trước và chuyển đến bạn",
                            Type = "Approval",
                            ActionUrl = $"/Requests/Detail/{request.Id}",
                            RelatedRequestId = request.Id
                        });

                        await _hubContext.Clients.Group($"user_{nextApproval.ApproverId}")
                            .SendAsync("ReceiveNotification", new
                            {
                                title = "Đơn cần duyệt",
                                message = $"Đơn {request.RequestCode} cần bạn xử lý",
                                type = "Approval",
                                actionUrl = $"/Requests/Detail/{request.Id}"
                            });
                    }

                    _context.Notifications.Add(new Models.SystemModels.Notification
                    {
                        TenantId = tenantId,
                        UserId = request.RequesterId,
                        Title = "Đơn đang được xử lý",
                        Message = $"Đơn {request.RequestCode} đã được duyệt bước {approval.StepName ?? approval.StepOrder.ToString()} và đang chờ bước tiếp theo",
                        Type = "Info",
                        ActionUrl = $"/Requests/Detail/{request.Id}",
                        RelatedRequestId = request.Id
                    });
                }
                else
                {
                    request.Status = "Approved";
                    request.CompletedAt = DateTime.UtcNow;
                    finalApproved = true;
                }

                if (request.Status == "Approved")
                {
                    var templateName = request.FormTemplate?.Name ?? "";
                    if (request.FormTemplateId == 7 || templateName.Contains("cập nhật thông tin"))
                    {
                        var dataEntries = await _context.RequestData
                            .Where(rd => rd.RequestId == request.Id)
                            .ToListAsync();

                        var requester = await _context.Users.FindAsync(request.RequesterId);
                        if (requester != null)
                        {
                            var newFullName = dataEntries.FirstOrDefault(d => (d.FieldKey == "new_fullname" || d.FieldKey == "fullName"))?.FieldValue;
                            var newPhone = dataEntries.FirstOrDefault(d => (d.FieldKey == "new_phone" || d.FieldKey == "phone"))?.FieldValue;
                            var newEmail = dataEntries.FirstOrDefault(d => (d.FieldKey == "new_email" || d.FieldKey == "email"))?.FieldValue;

                            if (!string.IsNullOrWhiteSpace(newFullName)) requester.FullName = newFullName.Trim();
                            if (!string.IsNullOrWhiteSpace(newPhone)) requester.Phone = newPhone.Trim();
                            if (!string.IsNullOrWhiteSpace(newEmail)) requester.Email = newEmail.Trim();

                            _context.Users.Update(requester);
                        }
                    }

                    _context.Notifications.Add(new Models.SystemModels.Notification
                    {
                        TenantId = tenantId,
                        UserId = request.RequesterId,
                        Title = "Đơn đã được duyệt ✓",
                        Message = $"Đơn {request.RequestCode} - {request.Title} đã được phê duyệt hoàn tất",
                        Type = "Info",
                        ActionUrl = $"/Requests/Detail/{request.Id}",
                        RelatedRequestId = request.Id
                    });

                    await _hubContext.Clients.Group($"user_{request.RequesterId}")
                        .SendAsync("RequestStatusChanged", new
                        {
                            requestCode = request.RequestCode,
                            newStatus = "Approved"
                        });
                }
            }
            else if (model.Action == "Reject")
            {
                approval.Status = "Rejected";
                approval.ActionDate = DateTime.UtcNow;
                approval.Comments = model.Comments;
                approval.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                request.Status = "Rejected";
                request.CompletedAt = DateTime.UtcNow;

                _context.Notifications.Add(new Models.SystemModels.Notification
                {
                    TenantId = tenantId,
                    UserId = request.RequesterId,
                    Title = "Đơn bị từ chối ✗",
                    Message = $"Đơn {request.RequestCode} bị từ chối: {model.Comments}",
                    Type = "Info",
                    ActionUrl = $"/Requests/Detail/{request.Id}",
                    RelatedRequestId = request.Id
                });

                await _hubContext.Clients.Group($"user_{request.RequesterId}")
                    .SendAsync("RequestStatusChanged", new
                    {
                        requestCode = request.RequestCode,
                        newStatus = "Rejected"
                    });
            }
            else if (model.Action == "RequestEdit")
            {
                approval.Status = "Pending";
                request.Status = "RequestEdit";

                _context.Notifications.Add(new Models.SystemModels.Notification
                {
                    TenantId = tenantId,
                    UserId = request.RequesterId,
                    Title = "Yêu cầu chỉnh sửa đơn",
                    Message = $"Đơn {request.RequestCode} cần chỉnh sửa: {model.Comments}",
                    Type = "Info",
                    ActionUrl = $"/Requests/Detail/{request.Id}",
                    RelatedRequestId = request.Id
                });
            }

            request.UpdatedAt = DateTime.UtcNow;

            _context.RequestAuditLogs.Add(new RequestAuditLog
            {
                RequestId = request.Id,
                UserId = userId.Value,
                Action = model.Action,
                OldStatus = oldStatus,
                NewStatus = request.Status,
                Details = model.Comments,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            var adminRoleId = await _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync();
            if (adminRoleId > 0)
            {
                var nextApproverId = request.Status == "InProgress"
                    ? await _context.RequestApprovals.Where(a => a.RequestId == request.Id && a.Status == "Pending").OrderBy(a => a.StepOrder).Select(a => a.ApproverId).FirstOrDefaultAsync()
                    : null;

                var adminUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRoleId && ur.UserId != userId.Value && ur.UserId != request.RequesterId && ur.UserId != nextApproverId)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                var actionMsg = model.Action switch
                {
                    "Approve" => "đã duyệt",
                    "Reject" => "đã từ chối",
                    "RequestEdit" => "yêu cầu chỉnh sửa",
                    _ => "đã xử lý"
                };

                foreach (var adminId in adminUserIds)
                {
                    _context.Notifications.Add(new Models.SystemModels.Notification
                    {
                        TenantId = tenantId,
                        UserId = adminId,
                        Title = $"Thông báo: Đơn {actionMsg}",
                        Message = $"{HttpContext.Session.GetString("FullName")} {actionMsg} đơn: {request.RequestCode}",
                        Type = "Info",
                        ActionUrl = $"/Requests/Detail/{request.Id}",
                        RelatedRequestId = request.Id
                    });
                }
            }

            await SyncExtendedBusinessRequestStatusAsync(request.Id, request.Status, userId.Value, model.Comments);
            await _context.SaveChangesAsync();
            if (finalApproved)
            {
                await _pdfService.GenerateApprovedPdfAsync(request.Id, userId.Value);
            }

            TempData["Success"] = model.Action switch
            {
                "Approve" => "Đơn đã được phê duyệt thành công!",
                "Reject" => "Đơn đã bị từ chối.",
                "RequestEdit" => "Đã yêu cầu chỉnh sửa đơn.",
                _ => "Thao tác thành công."
            };

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> BulkApproval(BulkApprovalViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            int processed = 0;
            var approvedRequestIds = new List<int>();
            foreach (var approvalId in model.ApprovalIds)
            {
                var approval = await _context.RequestApprovals
                    .Include(a => a.Request)
                    .FirstOrDefaultAsync(a => a.Id == approvalId && a.ApproverId == userId && a.Status == "Pending");

                if (approval == null) continue;

                approval.Status = model.Action == "Approve" ? "Approved" : "Rejected";
                approval.ActionDate = DateTime.UtcNow;
                approval.Comments = model.Comments;
                approval.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                var request = approval.Request!;

                if (model.Action == "Approve")
                {
                    var hasMoreSteps = await _context.RequestApprovals
                        .AnyAsync(a => a.RequestId == request.Id && a.StepOrder > approval.StepOrder && a.Status == "Pending");

                    request.Status = hasMoreSteps ? "InProgress" : "Approved";
                    if (!hasMoreSteps) request.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    request.Status = "Rejected";
                    request.CompletedAt = DateTime.UtcNow;

                    // Notify requester
                    _context.Notifications.Add(new Models.SystemModels.Notification
                    {
                        TenantId = tenantId,
                        UserId = request.RequesterId,
                        Title = "Đơn bị từ chối ✗",
                        Message = $"Đơn {request.RequestCode} bị từ chối (Duyệt hàng loạt)",
                        Type = "Info",
                        ActionUrl = $"/Requests/Detail/{request.Id}",
                        RelatedRequestId = request.Id
                    });
                }

                // If approved and moved to next step, notify next person
                if (model.Action == "Approve" && request.Status == "InProgress")
                {
                    var nextApproval = await _context.RequestApprovals
                        .Where(a => a.RequestId == request.Id && a.StepOrder > approval.StepOrder && a.Status == "Pending")
                        .OrderBy(a => a.StepOrder)
                        .FirstOrDefaultAsync();

                    if (nextApproval?.ApproverId != null)
                    {
                        _context.Notifications.Add(new Models.SystemModels.Notification
                        {
                            TenantId = tenantId,
                            UserId = nextApproval.ApproverId.Value,
                            Title = "Đơn cần duyệt",
                            Message = $"Đơn {request.RequestCode} chuyển đến bạn (Duyệt hàng loạt)",
                            Type = "Approval",
                            ActionUrl = $"/Requests/Detail/{request.Id}",
                            RelatedRequestId = request.Id
                        });

                        await _hubContext.Clients.Group($"user_{nextApproval.ApproverId}")
                            .SendAsync("ReceiveNotification", new
                            {
                                title = "Đơn cần duyệt",
                                message = $"Đơn {request.RequestCode} cần bạn xử lý",
                                type = "Approval",
                                actionUrl = $"/Requests/Detail/{request.Id}"
                            });
                    }
                }
                else if (model.Action == "Approve" && request.Status == "Approved")
                {
                    approvedRequestIds.Add(request.Id);

                    // Full approval
                    _context.Notifications.Add(new Models.SystemModels.Notification
                    {
                        TenantId = tenantId,
                        UserId = request.RequesterId,
                        Title = "Đơn đã được duyệt ✓",
                        Message = $"Đơn {request.RequestCode} đã được phê duyệt hoàn tất (Duyệt hàng loạt)",
                        Type = "Info",
                        ActionUrl = $"/Requests/Detail/{request.Id}",
                        RelatedRequestId = request.Id
                    });
                }

                request.UpdatedAt = DateTime.UtcNow;

                _context.RequestAuditLogs.Add(new RequestAuditLog
                {
                    RequestId = request.Id,
                    UserId = userId.Value,
                    Action = $"Bulk{model.Action}",
                    NewStatus = request.Status,
                    Details = model.Comments,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });

                // --- CC Admin Users on bulk action ---
                var adminRoleId = await _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync();
                if (adminRoleId > 0)
                {
                    var nextApproverId = request.Status == "InProgress" ? 
                        await _context.RequestApprovals.Where(a => a.RequestId == request.Id && a.Status == "Pending").OrderBy(a => a.StepOrder).Select(a => a.ApproverId).FirstOrDefaultAsync() 
                        : null;

                    var adminUserIds = await _context.UserRoles
                        .Where(ur => ur.RoleId == adminRoleId && ur.UserId != userId.Value && ur.UserId != request.RequesterId && ur.UserId != nextApproverId)
                        .Select(ur => ur.UserId)
                        .ToListAsync();
                    
                    string actionMsg = model.Action switch {
                        "Approve" => "đã duyệt hàng loạt",
                        "Reject" => "đã từ chối hàng loạt",
                        _ => "đã xử lý hàng loạt"
                    };

                    foreach (var adminId in adminUserIds)
                    {
                        _context.Notifications.Add(new Models.SystemModels.Notification
                        {
                            TenantId = tenantId,
                            UserId = adminId,
                            Title = $"Thông báo: Đơn {actionMsg}",
                            Message = $"{HttpContext.Session.GetString("FullName")} {actionMsg} đơn: {request.RequestCode}",
                            Type = "Info",
                            ActionUrl = $"/Requests/Detail/{request.Id}",
                            RelatedRequestId = request.Id
                        });
                    }
                }

                await SyncExtendedBusinessRequestStatusAsync(request.Id, request.Status, userId.Value, model.Comments);

                processed++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xử lý {processed} đơn thành công!";
            foreach (var requestId in approvedRequestIds.Distinct())
            {
                await _pdfService.GenerateApprovedPdfAsync(requestId, userId.Value);
            }

            return RedirectToAction("Index");
        }

        private async Task SyncExtendedBusinessRequestStatusAsync(int sourceRequestId, string status, int actionUserId, string? comments)
        {
            var adjustment = await _context.AttendanceAdjustmentRequests
                .FirstOrDefaultAsync(x => x.SourceRequestId == sourceRequestId);
            if (adjustment != null)
            {
                adjustment.Status = MapStructuredStatus(status);
                adjustment.Notes = comments;
                adjustment.UpdatedAt = DateTime.UtcNow;
                if (status == "Approved" || status == "Rejected")
                {
                    adjustment.ApprovedByUserId = actionUserId;
                    adjustment.ProcessedAt = DateTime.UtcNow;
                }
            }

            var lateEarly = await _context.LateEarlyRequests
                .FirstOrDefaultAsync(x => x.SourceRequestId == sourceRequestId);
            if (lateEarly != null)
            {
                lateEarly.Status = MapStructuredStatus(status);
                lateEarly.Notes = comments;
                lateEarly.UpdatedAt = DateTime.UtcNow;
                if (status == "Approved" || status == "Rejected")
                {
                    lateEarly.ApprovedByUserId = actionUserId;
                    lateEarly.ProcessedAt = DateTime.UtcNow;
                }
            }

            var advance = await _context.SalaryAdvanceRequests
                .FirstOrDefaultAsync(x => x.SourceRequestId == sourceRequestId);
            if (advance != null)
            {
                advance.Status = status == "Approved" ? "Approved" : status == "Rejected" ? "Rejected" : "Pending";
                advance.Notes = comments;
                advance.UpdatedAt = DateTime.UtcNow;
                if (status == "Approved" || status == "Rejected")
                {
                    advance.ApprovedByUserId = actionUserId;
                    advance.ApprovedAt = DateTime.UtcNow;
                }
            }
        }

        private static string MapStructuredStatus(string requestStatus)
        {
            return requestStatus switch
            {
                "Approved" => "Approved",
                "Rejected" => "Rejected",
                _ => "Pending"
            };
        }
    }
}
