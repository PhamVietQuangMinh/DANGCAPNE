using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using DANGCAPNE.Data;
using DANGCAPNE.Models.HR;
using DANGCAPNE.Models.Requests;
using DANGCAPNE.Models.Timekeeping;
using DANGCAPNE.Models.Workflow;
using DANGCAPNE.ViewModels;
using Newtonsoft.Json;

namespace DANGCAPNE.Controllers
{
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly DANGCAPNE.Services.GeminiAIService _aiService;

        public RequestsController(ApplicationDbContext context, IWebHostEnvironment env, DANGCAPNE.Services.GeminiAIService aiService)
        {
            _context = context;
            _env = env;
            _aiService = aiService;
        }

        public async Task<IActionResult> Index(string? status, string? type, string? search, int page = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var isAdminOrHR = roles.Contains("Admin") || roles.Contains("HR");

            var query = _context.Requests
                .Include(r => r.Requester)
                .Include(r => r.FormTemplate)
                .Where(r => r.TenantId == tenantId);

            if (!isAdminOrHR)
                query = query.Where(r => r.RequesterId == userId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(r => r.FormTemplate!.Category == type);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(r => r.Title.Contains(search) || r.RequestCode.Contains(search));

            var pageSize = 10;
            var total = await query.CountAsync();

            var model = new RequestListViewModel
            {
                Requests = await query.OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(),
                StatusFilter = status,
                TypeFilter = type,
                SearchQuery = search,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                PageSize = pageSize,
                FormTemplates = await _context.FormTemplates.Where(f => f.TenantId == tenantId && f.IsActive).ToListAsync()
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int templateId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            await EnsureExtendedTemplatesAsync(tenantId);

            // Self-healing seed for Template 7 (Update Info Request)
            if (templateId == 7)
            {
                var hasUpdateTypeField = await _context.FormFields.AnyAsync(f => f.FormTemplateId == 7 && f.FieldName == "update_type");
                if (!hasUpdateTypeField)
                {
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync($@"
                            INSERT INTO ""FormTemplates"" (""Id"", ""TenantId"", ""Name"", ""Category"", ""Icon"", ""IconColor"", ""WorkflowId"", ""IsActive"", ""Description"", ""RequiresFinancialApproval"", ""CreatedAt"")
                            VALUES (7, {tenantId}, 'Đơn cập nhật thông tin nhân sự', 'Other', 'bi-person-gear', '#6366f1', 3, true, 'Biểu mẫu thay đổi thông tin cá nhân. Vui lòng chọn loại thông tin cần cập nhật.', false, NOW())
                            ON CONFLICT (""Id"") DO UPDATE SET ""Description"" = 'Biểu mẫu thay đổi thông tin cá nhân. Vui lòng chọn loại thông tin cần cập nhật.', ""IsActive"" = true;

                            DELETE FROM ""FormFieldOptions"" WHERE ""FormFieldId"" = 27;
                            DELETE FROM ""FormFields"" WHERE ""FormTemplateId"" = 7;

                            INSERT INTO ""FormFields"" (""Id"", ""FormTemplateId"", ""Label"", ""FieldName"", ""FieldType"", ""IsRequired"", ""DisplayOrder"", ""Width"")
                            VALUES 
                            (27, 7, 'Thông tin muốn thay đổi', 'update_type', 'Dropdown', true, 1, 12),
                            (22, 7, 'Họ và tên mới', 'new_fullname', 'Text', false, 2, 12),
                            (23, 7, 'Số điện thoại mới', 'new_phone', 'Text', false, 3, 12),
                            (26, 7, 'Email mới', 'new_email', 'Text', false, 4, 12),
                            (24, 7, 'Lý do thay đổi', 'reason', 'Textarea', true, 5, 12),
                            (25, 7, 'Minh chứng đính kèm', 'attachment', 'FileUpload', false, 6, 12);

                            INSERT INTO ""FormFieldOptions"" (""Id"", ""FormFieldId"", ""Label"", ""Value"", ""DisplayOrder"")
                            VALUES 
                            (100, 27, 'Họ và tên', 'fullname', 1),
                            (101, 27, 'Số điện thoại', 'phone', 2),
                            (102, 27, 'Email cá nhân', 'email', 3);
                        ");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SeedError] Template 7 seed failed: {ex.Message}");
                    }
                }
            }

            var template = await _context.FormTemplates
                .Include(f => f.Fields.OrderBy(ff => ff.DisplayOrder))
                    .ThenInclude(f => f.Options.OrderBy(o => o.DisplayOrder))
                .FirstOrDefaultAsync(f => f.Id == templateId);

            if (template == null) return NotFound();

            // Check for auto-saved draft
            var draft = await _context.DraftRequests
                .FirstOrDefaultAsync(d => d.UserId == userId.Value && d.FormTemplateId == templateId);

            var model = new RequestCreateViewModel
            {
                FormTemplate = template,
                Fields = template.Fields.ToList(),
                FormData = draft != null ? JsonConvert.DeserializeObject<Dictionary<string, string>>(draft.FormDataJson) ?? new() : new()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(int templateId, IFormCollection form)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var template = await _context.FormTemplates
                .Include(f => f.Workflow).ThenInclude(w => w!.Steps.OrderBy(s => s.StepOrder))
                .Include(f => f.Fields)
                .FirstOrDefaultAsync(f => f.Id == templateId);

            if (template == null) return NotFound();

            // Generate request code
            var today = DateTime.Now;
            var countToday = await _context.Requests.CountAsync(r => r.TenantId == tenantId && r.CreatedAt.Date == today.Date);
            var requestCode = $"REQ-{today:yyyyMMdd}-{(countToday + 1):D3}";

            var title = form["Title"].ToString();
            if (string.IsNullOrEmpty(title))
                title = $"{template.Name} - {HttpContext.Session.GetString("FullName")}";

            var request = new Request
            {
                TenantId = tenantId,
                RequestCode = requestCode,
                FormTemplateId = templateId,
                RequesterId = userId.Value,
                Title = title,
                Status = "Pending",
                CurrentStepOrder = 1,
                Priority = form["Priority"].ToString() ?? "Normal",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            // Save form data
            foreach (var field in template.Fields)
            {
                var val = form[field.FieldName].ToString();
                if (!string.IsNullOrEmpty(val))
                {
                    _context.RequestData.Add(new RequestData
                    {
                        RequestId = request.Id,
                        FieldKey = field.FieldName,
                        FieldValue = val,
                        FieldType = field.FieldType
                    });
                }
            }

            await SyncExtendedBusinessRequestAsync(request, form, tenantId, userId.Value);

            // Handle file uploads
            var aiTasks = new List<Task<string>>();
            foreach (var file in form.Files)
            {
                if (file.Length > 0)
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", tenantId.ToString());
                    Directory.CreateDirectory(uploadsDir);
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    _context.RequestAttachments.Add(new RequestAttachment
                    {
                        RequestId = request.Id,
                        FileName = file.FileName,
                        FilePath = $"/uploads/{tenantId}/{fileName}",
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        UploadedById = userId.Value
                    });
                    
                    // Trigger AI Document Analysis for images
                    if (file.ContentType.StartsWith("image/"))
                    {
                        var task = _aiService.AnalyzeDocumentAsync(filePath, file.ContentType);
                        aiTasks.Add(task);
                    }
                }
            }

            // Create approval steps
            if (template.Workflow != null)
            {
                foreach (var step in template.Workflow.Steps.OrderBy(s => s.StepOrder))
                {
                    int? approverId = null;

                    // Determine approver
                    if (step.ApproverType == "DirectManager")
                    {
                        var mgr = await _context.UserManagers
                            .Where(um => um.UserId == userId && um.IsPrimary && (um.EndDate == null || um.EndDate > DateTime.Now))
                            .FirstOrDefaultAsync();
                        approverId = mgr?.ManagerId;

                        // Check delegation
                        if (approverId != null)
                        {
                            var delegation = await _context.Delegations
                                .Where(d => d.DelegatorId == approverId && d.IsActive && d.StartDate <= DateTime.Now && d.EndDate >= DateTime.Now)
                                .FirstOrDefaultAsync();
                            if (delegation != null)
                                approverId = delegation.DelegateId;
                        }
                    }
                    else if (step.ApproverType == "SpecificUser")
                    {
                        approverId = step.ApproverUserId;
                    }
                    else if (step.ApproverType == "Role")
                    {
                        var roleUser = await _context.UserRoles
                            .Where(ur => ur.RoleId == step.ApproverRoleId)
                            .Select(ur => ur.UserId)
                            .FirstOrDefaultAsync();
                        approverId = roleUser > 0 ? roleUser : null;
                    }

                    // Skip step if applicant holds the role
                    var skipStatus = "Pending";
                    if (step.CanSkipIfApplicant && approverId == userId)
                    {
                        skipStatus = "Skipped";
                    }

                    _context.RequestApprovals.Add(new RequestApproval
                    {
                        RequestId = request.Id,
                        StepOrder = step.StepOrder,
                        StepName = step.Name,
                        ApproverId = approverId,
                        Status = skipStatus,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Audit log
            _context.RequestAuditLogs.Add(new RequestAuditLog
            {
                RequestId = request.Id,
                UserId = userId.Value,
                Action = "Created",
                NewStatus = "Pending",
                Details = $"Tạo {template.Name}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.Now
            });

            // Notification for approver
            var firstApproval = await _context.RequestApprovals
                .Where(ra => ra.RequestId == request.Id && ra.Status == "Pending" && ra.ApproverId != null)
                .OrderBy(ra => ra.StepOrder)
                .FirstOrDefaultAsync();

            if (firstApproval?.ApproverId != null)
            {
                _context.Notifications.Add(new Models.SystemModels.Notification
                {
                    TenantId = tenantId,
                    UserId = firstApproval.ApproverId.Value,
                    Title = "Đơn mới cần duyệt",
                    Message = $"{HttpContext.Session.GetString("FullName")} đã tạo {template.Name}: {title}",
                    Type = "Approval",
                    ActionUrl = $"/Requests/Detail/{request.Id}",
                    RelatedRequestId = request.Id
                });
            }

            // --- CC Admin Users on new request creation ---
            var adminRoleId = await _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync();
            if (adminRoleId > 0)
            {
                var adminUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRoleId && ur.UserId != firstApproval.ApproverId && ur.UserId != userId.Value)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var adminId in adminUserIds)
                {
                    _context.Notifications.Add(new Models.SystemModels.Notification
                    {
                        TenantId = tenantId,
                        UserId = adminId,
                        Title = "Thông báo: Đơn mới được tạo",
                        Message = $"{HttpContext.Session.GetString("FullName")} vừa tạo đơn: {title}",
                        Type = "Info",
                        ActionUrl = $"/Requests/Detail/{request.Id}",
                        RelatedRequestId = request.Id
                    });
                }
            }

            // Delete draft if exists
            var draft = await _context.DraftRequests
                .FirstOrDefaultAsync(d => d.UserId == userId.Value && d.FormTemplateId == templateId);
            if (draft != null) _context.DraftRequests.Remove(draft);

            // Delay AI Processing minimally to not block the main request, though for demo we await
            if (aiTasks.Any())
            {
                var aiResults = await Task.WhenAll(aiTasks);
                foreach (var res in aiResults)
                {
                    if (!string.IsNullOrEmpty(res))
                    {
                        _context.RequestComments.Add(new RequestComment
                        {
                            RequestId = request.Id,
                            UserId = userId.Value, // AI comment runs as the requester impersonation or system user
                            Content = res,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đơn {requestCode} đã được tạo thành công!";
            return RedirectToAction("Detail", new { id = request.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var request = await _context.Requests
                .Include(r => r.FormTemplate)
                .ThenInclude(t => t!.Fields.OrderBy(f => f.DisplayOrder))
                .ThenInclude(f => f.Options)
                .Include(r => r.DataEntries)
                .FirstOrDefaultAsync(r => r.Id == id && r.RequesterId == userId);

            if (request == null || (request.Status != "RequestEdit" && request.Status != "Draft")) 
                return NotFound();

            var model = new RequestCreateViewModel
            {
                FormTemplate = request.FormTemplate,
                Fields = request.FormTemplate?.Fields.ToList() ?? new(),
                Title = request.Title,
                FormData = request.DataEntries.ToDictionary(d => d.FieldKey, d => d.FieldValue ?? "")
            };

            ViewBag.RequestId = request.Id;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, IFormCollection form)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;

            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.Id == id && r.RequesterId == userId);

            if (request == null || (request.Status != "RequestEdit" && request.Status != "Draft")) 
                return NotFound();

            request.Title = form["Title"].ToString() ?? request.Title;
            request.Priority = form["Priority"].ToString() ?? request.Priority;
            request.Status = "Pending";
            request.CurrentStepOrder = 1;

            var oldData = await _context.RequestData.Where(d => d.RequestId == id).ToListAsync();
            _context.RequestData.RemoveRange(oldData);

            foreach (var key in form.Keys)
            {
                if (key != "__RequestVerificationToken" && key != "Title" && key != "Priority" && !form.Files.Any(f => f.Name == key))
                {
                    _context.RequestData.Add(new RequestData
                    {
                        RequestId = request.Id,
                        FieldKey = key,
                        FieldValue = form[key].ToString()
                    });
                }
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            foreach (var file in form.Files)
            {
                if (file.Length > 0)
                {
                    var ext = Path.GetExtension(file.FileName);
                    var newFileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsFolder, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    _context.RequestAttachments.Add(new RequestAttachment
                    {
                        RequestId = request.Id,
                        FileName = file.FileName,
                        FilePath = $"/uploads/{newFileName}",
                        FileSize = file.Length,
                        ContentType = file.ContentType
                    });
                }
            }

            var approvals = await _context.RequestApprovals.Where(a => a.RequestId == id).ToListAsync();
            foreach (var app in approvals)
            {
                if (app.Status != "Pending") {
                    app.Status = "Pending";
                    app.ActionDate = null;
                    app.Comments = null;
                }
            }

            _context.RequestAuditLogs.Add(new RequestAuditLog
            {
                RequestId = request.Id,
                UserId = userId.Value,
                Action = "Resubmitted",
                NewStatus = "Pending",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            // Notify ALL approvers in the workflow (not just the first one)
            var approverIds = approvals
                .Where(a => a.ApproverId != null)
                .Select(a => a.ApproverId!.Value)
                .Distinct()
                .ToList();
            
            var requesterName = HttpContext.Session.GetString("FullName") ?? "Nhân viên";
            foreach (var approverId in approverIds)
            {
                _context.Notifications.Add(new Models.SystemModels.Notification
                {
                    TenantId = tenantId,
                    UserId = approverId,
                    Title = "Đơn đã chỉnh sửa và gửi lại",
                    Message = $"{requesterName} đã chỉnh sửa và gửi lại đơn: {request.Title}",
                    Type = "Approval",
                    ActionUrl = $"/Requests/Detail/{request.Id}",
                    RelatedRequestId = request.Id
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đơn {request.RequestCode} đã được cập nhật và gửi lại!";
            return RedirectToAction("Detail", new { id = request.Id });
        }

        public async Task<IActionResult> Detail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var request = await _context.Requests
                .Include(r => r.Requester).ThenInclude(u => u!.Department)
                .Include(r => r.FormTemplate).ThenInclude(f => f!.Fields.OrderBy(ff => ff.DisplayOrder))
                    .ThenInclude(ff => ff.Options)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            var approvals = await _context.RequestApprovals
                .Include(a => a.Approver)
                .Where(a => a.RequestId == id)
                .OrderBy(a => a.StepOrder)
                .ToListAsync();

            var comments = await _context.RequestComments
                .Include(c => c.User)
                .Where(c => c.RequestId == id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            var auditLogs = await _context.RequestAuditLogs
                .Include(a => a.User)
                .Where(a => a.RequestId == id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var formData = await _context.RequestData
                .Where(rd => rd.RequestId == id)
                .ToDictionaryAsync(rd => rd.FieldKey, rd => rd.FieldValue ?? "");

            var currentApproval = approvals.FirstOrDefault(a => a.Status == "Pending" && a.ApproverId == userId);
            var roles = (HttpContext.Session.GetString("Roles") ?? "").Split(",");
            var isAdmin = roles.Contains("Admin");

            // Admin Super-Override: If there's a pending approval but the current user (Admin) isn't the assigned approver,
            // let the Admin override and approve it anyway.
            if (currentApproval == null && isAdmin)
            {
                currentApproval = approvals.FirstOrDefault(a => a.Status == "Pending");
            }

            // Safety check: if request is already finished, nobody can approve anything, regardless of orphaned pending DB rows
            if (request.Status == "Approved" || request.Status == "Rejected" || request.Status == "Cancelled")
            {
                currentApproval = null;
            }

            var model = new RequestDetailViewModel
            {
                Request = request,
                ApprovalHistory = approvals,
                Comments = comments,
                AuditLogs = auditLogs,
                FormData = formData,
                FormFields = request.FormTemplate?.Fields.ToList() ?? new(),
                CanApprove = currentApproval != null,
                CanReject = currentApproval != null,
                CanEdit = request.RequesterId == userId && (request.Status == "Draft" || request.Status == "RequestEdit"),
                CanCancel = request.RequesterId == userId && request.Status == "Pending",
                RequiresPin = request.FormTemplate?.RequiresFinancialApproval ?? false,
                CurrentApprovalId = currentApproval?.Id
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int requestId, string content)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            _context.RequestComments.Add(new RequestComment
            {
                RequestId = requestId,
                UserId = userId.Value,
                Content = content,
                CreatedAt = DateTime.Now
            });

            _context.RequestAuditLogs.Add(new RequestAuditLog
            {
                RequestId = requestId,
                UserId = userId.Value,
                Action = "Commented",
                Details = content.Length > 200 ? content.Substring(0, 200) + "..." : content,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("Detail", new { id = requestId });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");

            var request = await _context.Requests.FindAsync(id);
            if (request == null || request.RequesterId != userId) return NotFound();

            request.Status = "Cancelled";
            request.UpdatedAt = DateTime.Now;

            _context.RequestAuditLogs.Add(new RequestAuditLog
            {
                RequestId = id,
                UserId = userId.Value,
                Action = "Cancelled",
                OldStatus = request.Status,
                NewStatus = "Cancelled",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đơn đã được hủy.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> SaveDraft([FromBody] dynamic data)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            int templateId = (int)data.templateId;
            string formDataJson = JsonConvert.SerializeObject(data.formData);

            var existing = await _context.DraftRequests
                .FirstOrDefaultAsync(d => d.UserId == userId.Value && d.FormTemplateId == templateId);

            if (existing != null)
            {
                existing.FormDataJson = formDataJson;
                existing.LastSavedAt = DateTime.Now;
            }
            else
            {
                _context.DraftRequests.Add(new DraftRequest
                {
                    TenantId = HttpContext.Session.GetInt32("TenantId") ?? 1,
                    UserId = userId.Value,
                    FormTemplateId = templateId,
                    FormDataJson = formDataJson
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã lưu nháp" });
        }

        public async Task<IActionResult> SelectTemplate()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Account");
            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            await EnsureExtendedTemplatesAsync(tenantId);

            // Self-healing seed for Template 7 (Update Info Request)
            var hasUpdateTypeField = await _context.FormFields.AnyAsync(f => f.FormTemplateId == 7 && f.FieldName == "update_type");
            if (!hasUpdateTypeField)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync($@"
                        INSERT INTO ""FormTemplates"" (""Id"", ""TenantId"", ""Name"", ""Category"", ""Icon"", ""IconColor"", ""WorkflowId"", ""IsActive"", ""Description"", ""RequiresFinancialApproval"", ""CreatedAt"")
                        VALUES (7, {tenantId}, 'Đơn cập nhật thông tin nhân sự', 'Other', 'bi-person-gear', '#6366f1', 3, true, 'Biểu mẫu thay đổi thông tin cá nhân. Vui lòng chọn loại thông tin cần cập nhật.', false, NOW())
                        ON CONFLICT (""Id"") DO UPDATE SET ""Description"" = 'Biểu mẫu thay đổi thông tin cá nhân. Vui lòng chọn loại thông tin cần cập nhật.', ""IsActive"" = true;

                        DELETE FROM ""FormFieldOptions"" WHERE ""FormFieldId"" = 27;
                        DELETE FROM ""FormFields"" WHERE ""FormTemplateId"" = 7;

                        INSERT INTO ""FormFields"" (""Id"", ""FormTemplateId"", ""Label"", ""FieldName"", ""FieldType"", ""IsRequired"", ""DisplayOrder"", ""Width"")
                        VALUES 
                        (27, 7, 'Thông tin muốn thay đổi', 'update_type', 'Dropdown', true, 1, 12),
                        (22, 7, 'Họ và tên mới', 'new_fullname', 'Text', false, 2, 12),
                        (23, 7, 'Số điện thoại mới', 'new_phone', 'Text', false, 3, 12),
                        (26, 7, 'Email mới', 'new_email', 'Text', false, 4, 12),
                        (24, 7, 'Lý do thay đổi', 'reason', 'Textarea', true, 5, 12),
                        (25, 7, 'Minh chứng đính kèm', 'attachment', 'FileUpload', false, 6, 12);

                        INSERT INTO ""FormFieldOptions"" (""Id"", ""FormFieldId"", ""Label"", ""Value"", ""DisplayOrder"")
                        VALUES 
                        (100, 27, 'Họ và tên', 'fullname', 1),
                        (101, 27, 'Số điện thoại', 'phone', 2),
                        (102, 27, 'Email cá nhân', 'email', 3);
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SeedError] Template 7 seed failed: {ex.Message}");
                    if (ex.InnerException != null) Console.WriteLine($"[SeedError] Inner: {ex.InnerException.Message}");
                }
            }

            // Self-healing for Workflow 1 (Basic Flow -> Add Admin)
            var workflow1 = await _context.Workflows.Include(w => w.Steps).FirstOrDefaultAsync(w => w.Id == 1);
            if (workflow1 != null)
            {
                var steps1 = workflow1.Steps.OrderBy(s => s.StepOrder).ToList();
                var hasAdmin1 = steps1.Any(s => s.Name.Contains("Giám đốc") || (s.ApproverType == "SpecificUser" && s.ApproverUserId == 1));
                
                if (!hasAdmin1)
                {
                    _context.WorkflowSteps.RemoveRange(steps1);
                    await _context.SaveChangesAsync();
                    _context.WorkflowSteps.AddRange(
                        new DANGCAPNE.Models.Workflow.WorkflowStep { WorkflowId = 1, Name = "Quản lý trực tiếp duyệt", StepOrder = 1, ApproverType = "DirectManager" },
                        new DANGCAPNE.Models.Workflow.WorkflowStep { WorkflowId = 1, Name = "HR duyệt", StepOrder = 2, ApproverType = "Role", ApproverRoleId = 2 },
                        new DANGCAPNE.Models.Workflow.WorkflowStep { WorkflowId = 1, Name = "Giám đốc duyệt", StepOrder = 3, ApproverType = "SpecificUser", ApproverUserId = 1 }
                    );
                    await _context.SaveChangesAsync();
                    Console.WriteLine("[SelfHealing] Workflow 1 steps updated to include Giám đốc.");
                }
            }

            // Self-healing for Workflow 3 (Update Info Request)
            var workflow3 = await _context.Workflows.Include(w => w.Steps).FirstOrDefaultAsync(w => w.Id == 3);
            if (workflow3 != null)
            {
                var steps = workflow3.Steps.OrderBy(s => s.StepOrder).ToList();
                var hasDirectManager = steps.Any(s => s.Name.Contains("Quản lý trực tiếp"));
                
                if (hasDirectManager || steps.Count > 3)
                {
                    // Clean up and reset Workflow 3 steps
                    _context.WorkflowSteps.RemoveRange(steps);
                    await _context.SaveChangesAsync();

                    _context.WorkflowSteps.AddRange(
                        new DANGCAPNE.Models.Workflow.WorkflowStep { WorkflowId = 3, Name = "Trưởng phòng duyệt", StepOrder = 1, ApproverType = "Role", ApproverRoleId = 3, CanSkipIfApplicant = true },
                        new DANGCAPNE.Models.Workflow.WorkflowStep { WorkflowId = 3, Name = "HR duyệt", StepOrder = 2, ApproverType = "Role", ApproverRoleId = 2 },
                        new DANGCAPNE.Models.Workflow.WorkflowStep { WorkflowId = 3, Name = "Giám đốc duyệt", StepOrder = 3, ApproverType = "SpecificUser", ApproverUserId = 1 }
                    );
                    await _context.SaveChangesAsync();
                    Console.WriteLine("[SelfHealing] Workflow 3 steps reset to: Trưởng phòng -> HR -> Giám đốc");
                }
            }

            var templates = await _context.FormTemplates
                .Where(f => f.TenantId == tenantId && f.IsActive)
                .OrderBy(f => f.Id)
                .ToListAsync();

            return View(templates);
        }

        private async Task EnsureExtendedTemplatesAsync(int tenantId)
        {
            var templates = new[]
            {
                new { Id = 11, Name = "Đơn điều chỉnh công", Category = "Attendance", Icon = "bi-pencil-square", IconColor = "#f59e0b", WorkflowId = 1, RequiresFinancialApproval = false },
                new { Id = 12, Name = "Đơn đi muộn/về sớm", Category = "Attendance", Icon = "bi-alarm", IconColor = "#ef4444", WorkflowId = 1, RequiresFinancialApproval = false },
                new { Id = 13, Name = "Đơn tạm ứng lương", Category = "Expense", Icon = "bi-wallet2", IconColor = "#22c55e", WorkflowId = 2, RequiresFinancialApproval = true }
            };

            foreach (var templateDef in templates)
            {
                var template = await _context.FormTemplates.FirstOrDefaultAsync(f => f.Id == templateDef.Id);
                if (template == null)
                {
                    template = new DANGCAPNE.Models.Workflow.FormTemplate
                    {
                        Id = templateDef.Id,
                        TenantId = tenantId,
                        Name = templateDef.Name,
                        Category = templateDef.Category,
                        Icon = templateDef.Icon,
                        IconColor = templateDef.IconColor,
                        WorkflowId = templateDef.WorkflowId,
                        RequiresFinancialApproval = templateDef.RequiresFinancialApproval,
                        IsActive = true,
                        Description = templateDef.Name,
                        CreatedAt = DateTime.Now
                    };
                    _context.FormTemplates.Add(template);
                }
                else
                {
                    template.IsActive = true;
                    template.WorkflowId = templateDef.WorkflowId;
                    template.RequiresFinancialApproval = templateDef.RequiresFinancialApproval;
                    template.Icon = templateDef.Icon;
                    template.IconColor = templateDef.IconColor;
                    template.Category = templateDef.Category;
                }
            }

            await _context.SaveChangesAsync();

            await EnsureTemplateFieldsAsync(11, new[]
            {
                new TemplateFieldSeed(1001, "Ngày cần điều chỉnh", "attendance_date", "Date", true, 1, 6),
                new TemplateFieldSeed(1002, "Giờ vào đề nghị", "requested_checkin", "Text", false, 2, 3),
                new TemplateFieldSeed(1003, "Giờ ra đề nghị", "requested_checkout", "Text", false, 3, 3),
                new TemplateFieldSeed(1004, "Lý do điều chỉnh", "reason", "Textarea", true, 4, 12),
                new TemplateFieldSeed(1005, "Minh chứng đính kèm", "attachment", "FileUpload", false, 5, 12)
            });

            await EnsureTemplateFieldsAsync(12, new[]
            {
                new TemplateFieldSeed(1011, "Ngày áp dụng", "attendance_date", "Date", true, 1, 6),
                new TemplateFieldSeed(1012, "Loại đăng ký", "request_type", "Dropdown", true, 2, 6),
                new TemplateFieldSeed(1013, "Giờ dự kiến", "expected_time", "Text", true, 3, 6),
                new TemplateFieldSeed(1014, "Giờ thực tế", "actual_time", "Text", true, 4, 6),
                new TemplateFieldSeed(1015, "Lý do", "reason", "Textarea", true, 5, 12)
            }, new[]
            {
                new TemplateOptionSeed(2011, 1012, "Đi muộn", "LateArrival", 1),
                new TemplateOptionSeed(2012, 1012, "Về sớm", "EarlyLeave", 2)
            });

            await EnsureTemplateFieldsAsync(13, new[]
            {
                new TemplateFieldSeed(1021, "Số tiền tạm ứng", "advance_amount", "Number", true, 1, 6),
                new TemplateFieldSeed(1022, "Tháng lương khấu trừ", "payroll_month", "Text", true, 2, 6),
                new TemplateFieldSeed(1023, "Ngày cần nhận", "needed_by_date", "Date", true, 3, 6),
                new TemplateFieldSeed(1024, "Lý do tạm ứng", "reason", "Textarea", true, 4, 12),
                new TemplateFieldSeed(1025, "Tài liệu đính kèm", "attachment", "FileUpload", false, 5, 12)
            });
        }

        private async Task EnsureTemplateFieldsAsync(int templateId, IEnumerable<TemplateFieldSeed> fields, IEnumerable<TemplateOptionSeed>? options = null)
        {
            var existingFields = await _context.FormFields.Where(f => f.FormTemplateId == templateId).ToListAsync();
            foreach (var fieldSeed in fields)
            {
                var field = existingFields.FirstOrDefault(f => f.Id == fieldSeed.Id)
                    ?? existingFields.FirstOrDefault(f => f.FieldName == fieldSeed.FieldName);

                if (field == null)
                {
                    field = new DANGCAPNE.Models.Workflow.FormField
                    {
                        Id = fieldSeed.Id,
                        FormTemplateId = templateId
                    };
                    _context.FormFields.Add(field);
                    existingFields.Add(field);
                }

                field.Label = fieldSeed.Label;
                field.FieldName = fieldSeed.FieldName;
                field.FieldType = fieldSeed.FieldType;
                field.IsRequired = fieldSeed.IsRequired;
                field.DisplayOrder = fieldSeed.DisplayOrder;
                field.Width = fieldSeed.Width;
            }

            if (options != null)
            {
                var fieldIds = existingFields.Select(f => f.Id).ToList();
                var existingOptions = await _context.FormFieldOptions.Where(o => fieldIds.Contains(o.FormFieldId)).ToListAsync();
                foreach (var optionSeed in options)
                {
                    var option = existingOptions.FirstOrDefault(o => o.Id == optionSeed.Id)
                        ?? existingOptions.FirstOrDefault(o => o.FormFieldId == optionSeed.FormFieldId && o.Value == optionSeed.Value);

                    if (option == null)
                    {
                        option = new DANGCAPNE.Models.Workflow.FormFieldOption
                        {
                            Id = optionSeed.Id,
                            FormFieldId = optionSeed.FormFieldId
                        };
                        _context.FormFieldOptions.Add(option);
                        existingOptions.Add(option);
                    }

                    option.Label = optionSeed.Label;
                    option.Value = optionSeed.Value;
                    option.DisplayOrder = optionSeed.DisplayOrder;
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SyncExtendedBusinessRequestAsync(Request request, IFormCollection form, int tenantId, int userId)
        {
            switch (request.FormTemplateId)
            {
                case 11:
                    await UpsertAttendanceAdjustmentRequestAsync(request, form, tenantId, userId);
                    break;
                case 12:
                    await UpsertLateEarlyRequestAsync(request, form, tenantId, userId);
                    break;
                case 13:
                    await UpsertSalaryAdvanceRequestAsync(request, form, tenantId, userId);
                    break;
            }
        }

        private async Task UpsertAttendanceAdjustmentRequestAsync(Request request, IFormCollection form, int tenantId, int userId)
        {
            var attendanceDate = ParseDateOrNull(form["attendance_date"]) ?? DateTime.Today;
            var requestedCheckIn = CombineDateAndTime(attendanceDate, form["requested_checkin"]);
            var requestedCheckOut = CombineDateAndTime(attendanceDate, form["requested_checkout"]);
            var timesheetId = await _context.Timesheets
                .Where(t => t.UserId == userId && t.Date.Date == attendanceDate.Date)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();

            var entity = await _context.AttendanceAdjustmentRequests
                .FirstOrDefaultAsync(x => x.SourceRequestId == request.Id);

            if (entity == null)
            {
                entity = new AttendanceAdjustmentRequest
                {
                    TenantId = tenantId,
                    SourceRequestId = request.Id,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.AttendanceAdjustmentRequests.Add(entity);
            }

            entity.TimesheetId = timesheetId;
            entity.AttendanceDate = attendanceDate.Date;
            entity.RequestedCheckIn = requestedCheckIn;
            entity.RequestedCheckOut = requestedCheckOut;
            entity.Reason = form["reason"].ToString();
            entity.Status = "Pending";
            entity.UpdatedAt = DateTime.Now;
        }

        private async Task UpsertLateEarlyRequestAsync(Request request, IFormCollection form, int tenantId, int userId)
        {
            var attendanceDate = ParseDateOrNull(form["attendance_date"]) ?? DateTime.Today;
            var entity = await _context.LateEarlyRequests
                .FirstOrDefaultAsync(x => x.SourceRequestId == request.Id);

            if (entity == null)
            {
                entity = new LateEarlyRequest
                {
                    TenantId = tenantId,
                    SourceRequestId = request.Id,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.LateEarlyRequests.Add(entity);
            }

            entity.AttendanceDate = attendanceDate.Date;
            entity.RequestType = form["request_type"].ToString();
            entity.ExpectedTime = CombineDateAndTime(attendanceDate, form["expected_time"]) ?? attendanceDate;
            entity.ActualTime = CombineDateAndTime(attendanceDate, form["actual_time"]) ?? attendanceDate;
            entity.Reason = form["reason"].ToString();
            entity.Status = "Pending";
            entity.UpdatedAt = DateTime.Now;
        }

        private async Task UpsertSalaryAdvanceRequestAsync(Request request, IFormCollection form, int tenantId, int userId)
        {
            var entity = await _context.SalaryAdvanceRequests
                .FirstOrDefaultAsync(x => x.SourceRequestId == request.Id);

            if (entity == null)
            {
                entity = new SalaryAdvanceRequest
                {
                    TenantId = tenantId,
                    SourceRequestId = request.Id,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                };
                _context.SalaryAdvanceRequests.Add(entity);
            }

            entity.Amount = ParseDecimalOrDefault(form["advance_amount"]);
            entity.PayrollMonth = form["payroll_month"].ToString();
            entity.NeededByDate = ParseDateOrNull(form["needed_by_date"]) ?? DateTime.Today;
            entity.Reason = form["reason"].ToString();
            entity.Status = "Pending";
            entity.UpdatedAt = DateTime.Now;
        }

        private static DateTime? ParseDateOrNull(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ||
                DateTime.TryParse(raw, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out parsed))
            {
                return parsed;
            }

            return null;
        }

        private static DateTime? CombineDateAndTime(DateTime date, string? rawTime)
        {
            if (string.IsNullOrWhiteSpace(rawTime))
            {
                return null;
            }

            if (TimeSpan.TryParse(rawTime, CultureInfo.InvariantCulture, out var time) ||
                TimeSpan.TryParse(rawTime, CultureInfo.GetCultureInfo("vi-VN"), out time))
            {
                return date.Date.Add(time);
            }

            if (DateTime.TryParse(rawTime, out var parsed))
            {
                return date.Date.Add(parsed.TimeOfDay);
            }

            return null;
        }

        private static decimal ParseDecimalOrDefault(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return 0;
            }

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ||
                decimal.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out parsed))
            {
                return parsed;
            }

            return 0;
        }

        private sealed record TemplateFieldSeed(int Id, string Label, string FieldName, string FieldType, bool IsRequired, int DisplayOrder, int Width);
        private sealed record TemplateOptionSeed(int Id, int FormFieldId, string Label, string Value, int DisplayOrder);

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] AIChatRequest payload)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized(new { text = "Vui lòng đăng nhập trước tiên." });

            var history = payload?.history;
            if (history == null || history.Count == 0) return BadRequest(new { text = "History is empty." });

            var aiResult = await _aiService.GeneralChatAsync(history);

            return Json(new { text = aiResult });
        }
        
        public class AIIntentRequest
        {
            public string? prompt { get; set; }
        }

        public class AIChatRequest
        {
            public List<DANGCAPNE.Services.ChatMessage> history { get; set; } = new();
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeIntent([FromBody] AIIntentRequest payload)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            string? prompt = payload?.prompt;
            if (string.IsNullOrEmpty(prompt)) return BadRequest();

            var tenantId = HttpContext.Session.GetInt32("TenantId") ?? 1;
            
            // Get simplified template info for AI
            var templatesInfo = await _context.FormTemplates
                .Include(f => f.Fields)
                .Where(f => f.TenantId == tenantId && f.IsActive)
                .Select(f => new 
                {
                    Id = f.Id,
                    Name = f.Name,
                    Fields = f.Fields.Select(ff => new { ff.FieldName, ff.Label, ff.FieldType })
                })
                .ToListAsync();

            var templatesJson = JsonConvert.SerializeObject(templatesInfo);
            var aiResult = await _aiService.ParseRequestIntentAsync(prompt, templatesJson);

            return Content(aiResult, "application/json");
        }
    }
}
