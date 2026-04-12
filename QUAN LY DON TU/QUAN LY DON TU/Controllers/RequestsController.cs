using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using DANGCAPNE.Data;
using DANGCAPNE.Models.HR;
using DANGCAPNE.Models.Requests;
using DANGCAPNE.Models.Timekeeping;
using DANGCAPNE.Models.Workflow;
using DANGCAPNE.Services;
using DANGCAPNE.ViewModels;
using Newtonsoft.Json;

namespace DANGCAPNE.Controllers
{
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly DANGCAPNE.Services.GeminiAIService _aiService;
        private readonly IApprovalSlaService _slaService;

        public RequestsController(ApplicationDbContext context, IWebHostEnvironment env, DANGCAPNE.Services.GeminiAIService aiService, IApprovalSlaService slaService)
        {
            _context = context;
            _env = env;
            _aiService = aiService;
            _slaService = slaService;
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

            var draft = await _context.DraftRequests
                .FirstOrDefaultAsync(d => d.UserId == userId.Value && d.FormTemplateId == templateId);

            var model = await BuildCreateViewModelAsync(
                template,
                userId.Value,
                tenantId,
                draft != null ? JsonConvert.DeserializeObject<Dictionary<string, string>>(draft.FormDataJson) ?? new() : new());

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
                .Include(f => f.Fields.OrderBy(ff => ff.DisplayOrder))
                    .ThenInclude(ff => ff.Options.OrderBy(o => o.DisplayOrder))
                .FirstOrDefaultAsync(f => f.Id == templateId);

            if (template == null) return NotFound();

            var formData = template.Fields
                .ToDictionary(f => f.FieldName, f => form[f.FieldName].ToString());

            var validationMessage = await ValidateCreateRequestAsync(template, form, userId.Value, tenantId);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                var invalidModel = await BuildCreateViewModelAsync(template, userId.Value, tenantId, formData, form["Title"].ToString(), validationMessage);
                invalidModel.Priority = form["Priority"].ToString() ?? "Normal";
                return View(invalidModel);
            }

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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Requests.Add(request);
            await _context.SaveChangesAsync();

            var leaveDayCount = (double?)null;
            (double previousYearUsage, double currentYearUsage)? annualLeaveAllocation = null;
            if (template.Id == 1)
            {
                var startDate = ParseDateOrNull(form["start_date"]);
                var endDate = ParseDateOrNull(form["end_date"]);
                if (startDate.HasValue && endDate.HasValue)
                {
                    leaveDayCount = await CountWorkingLeaveDaysAsync(tenantId, startDate.Value, endDate.Value);
                }

                if (string.Equals(form["leave_type"].ToString(), "AL", StringComparison.OrdinalIgnoreCase) && leaveDayCount.HasValue)
                {
                    var annualBuckets = await GetAnnualLeaveBucketsAsync(userId.Value, tenantId);
                    annualLeaveAllocation = AllocateAnnualLeave(annualBuckets, leaveDayCount.Value);
                }
            }

            foreach (var field in template.Fields)
            {
                var val = form[field.FieldName].ToString();
                if (template.Id == 1 && field.FieldName == "total_days" && leaveDayCount.HasValue)
                {
                    val = leaveDayCount.Value.ToString("0.##", CultureInfo.InvariantCulture);
                }
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

                    if (file.ContentType.StartsWith("image/"))
                    {
                        aiTasks.Add(_aiService.AnalyzeDocumentAsync(filePath, file.ContentType));
                    }
                }
            }

            var approvalSteps = await BuildApprovalStepsAsync(template, userId.Value, tenantId);
            foreach (var step in approvalSteps)
            {
                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId = request.Id,
                    StepOrder = step.StepOrder,
                    StepName = step.StepName,
                    ApproverId = step.ApproverId,
                    Status = step.ApproverId == userId.Value ? "Skipped" : "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var firstPendingStep = await _context.RequestApprovals
                .Where(ra => ra.RequestId == request.Id && ra.Status == "Pending")
                .OrderBy(ra => ra.StepOrder)
                .FirstOrDefaultAsync();

            if (firstPendingStep == null)
            {
                request.Status = "Approved";
                request.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                request.CurrentStepOrder = firstPendingStep.StepOrder;
            }

            _context.RequestAuditLogs.Add(new RequestAuditLog
            {
                RequestId = request.Id,
                UserId = userId.Value,
                Action = "Created",
                NewStatus = request.Status,
                Details = $"Tạo {template.Name}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            });

            if (firstPendingStep?.ApproverId != null)
            {
                _context.Notifications.Add(new Models.SystemModels.Notification
                {
                    TenantId = tenantId,
                    UserId = firstPendingStep.ApproverId.Value,
                    Title = "Đơn mới cần duyệt",
                    Message = $"{HttpContext.Session.GetString("FullName")} đã tạo {template.Name}: {title}",
                    Type = "Approval",
                    ActionUrl = $"/Requests/Detail/{request.Id}",
                    RelatedRequestId = request.Id
                });
            }

            var adminRoleId = await _context.Roles.Where(r => r.Name == "Admin").Select(r => r.Id).FirstOrDefaultAsync();
            if (adminRoleId > 0)
            {
                var adminUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRoleId && ur.UserId != (firstPendingStep != null ? firstPendingStep.ApproverId : null) && ur.UserId != userId.Value)
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

            var draft = await _context.DraftRequests
                .FirstOrDefaultAsync(d => d.UserId == userId.Value && d.FormTemplateId == templateId);
            if (draft != null) _context.DraftRequests.Remove(draft);

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
                            UserId = userId.Value,
                            Content = res,
                            CreatedAt = DateTime.UtcNow
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
                .Include(r => r.FormTemplate)
                .FirstOrDefaultAsync(r => r.Id == id && r.RequesterId == userId);

            if (request == null || (request.Status != "RequestEdit" && request.Status != "Draft")) 
                return NotFound();

            var template = await _context.FormTemplates
                .Include(f => f.Workflow).ThenInclude(w => w!.Steps.OrderBy(s => s.StepOrder))
                .Include(f => f.Fields.OrderBy(ff => ff.DisplayOrder))
                    .ThenInclude(ff => ff.Options.OrderBy(o => o.DisplayOrder))
                .FirstOrDefaultAsync(f => f.Id == request.FormTemplateId);

            if (template == null)
            {
                return NotFound();
            }

            var submittedFormData = template.Fields
                .ToDictionary(f => f.FieldName, f => form[f.FieldName].ToString());

            var validationMessage = await ValidateCreateRequestAsync(template, form, userId.Value, tenantId);
            if (!string.IsNullOrWhiteSpace(validationMessage))
            {
                var invalidModel = await BuildCreateViewModelAsync(template, userId.Value, tenantId, submittedFormData, form["Title"].ToString(), validationMessage);
                invalidModel.Priority = form["Priority"].ToString() ?? "Normal";
                ViewBag.RequestId = id;
                return View(invalidModel);
            }

            request.Title = form["Title"].ToString() ?? request.Title;
            request.Priority = form["Priority"].ToString() ?? request.Priority;
            request.Status = "Pending";
            request.CurrentStepOrder = 1;
            request.CompletedAt = null;

            var oldData = await _context.RequestData.Where(d => d.RequestId == id).ToListAsync();
            _context.RequestData.RemoveRange(oldData);

            (double previousYearUsage, double currentYearUsage)? annualLeaveAllocation = null;
            if (template.Id == 1)
            {
                var startDate = ParseDateOrNull(form["start_date"]);
                var endDate = ParseDateOrNull(form["end_date"]);
                if (startDate.HasValue && endDate.HasValue && string.Equals(form["leave_type"].ToString(), "AL", StringComparison.OrdinalIgnoreCase))
                {
                    var leaveDayCount = await CountWorkingLeaveDaysAsync(tenantId, startDate.Value, endDate.Value);
                    var annualBuckets = await GetAnnualLeaveBucketsAsync(userId.Value, tenantId);
                    annualLeaveAllocation = AllocateAnnualLeave(annualBuckets, leaveDayCount);
                }
            }

            foreach (var key in form.Keys)
            {
                if (key != "__RequestVerificationToken" && key != "Title" && key != "Priority" && !form.Files.Any(f => f.Name == key))
                {
                    var value = form[key].ToString();
                    if (template.Id == 1 && key == "total_days")
                    {
                        var startDate = ParseDateOrNull(form["start_date"]);
                        var endDate = ParseDateOrNull(form["end_date"]);
                        if (startDate.HasValue && endDate.HasValue)
                        {
                            value = (await CountWorkingLeaveDaysAsync(tenantId, startDate.Value, endDate.Value)).ToString("0.##", CultureInfo.InvariantCulture);
                        }
                    }

                    _context.RequestData.Add(new RequestData
                    {
                        RequestId = request.Id,
                        FieldKey = key,
                        FieldValue = value
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
                        ContentType = file.ContentType,
                        UploadedById = userId.Value
                    });
                }
            }

            if (annualLeaveAllocation.HasValue)
            {
                _context.RequestData.Add(new RequestData
                {
                    RequestId = request.Id,
                    FieldKey = "annual_leave_prev_year_used",
                    FieldValue = annualLeaveAllocation.Value.previousYearUsage.ToString("0.##", CultureInfo.InvariantCulture),
                    FieldType = "Number"
                });
                _context.RequestData.Add(new RequestData
                {
                    RequestId = request.Id,
                    FieldKey = "annual_leave_current_year_used",
                    FieldValue = annualLeaveAllocation.Value.currentYearUsage.ToString("0.##", CultureInfo.InvariantCulture),
                    FieldType = "Number"
                });
            }

            var approvals = await _context.RequestApprovals.Where(a => a.RequestId == id).ToListAsync();
            _context.RequestApprovals.RemoveRange(approvals);

            var approvalSteps = await BuildApprovalStepsAsync(template, userId.Value, tenantId);
            foreach (var step in approvalSteps)
            {
                _context.RequestApprovals.Add(new RequestApproval
                {
                    RequestId = request.Id,
                    StepOrder = step.StepOrder,
                    StepName = step.StepName,
                    ApproverId = step.ApproverId,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }

            request.CurrentStepOrder = approvalSteps.First().StepOrder;

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

            // Safety check: if request is already finished, nobody can approve anything, regardless of orphaned pending DB rows
            if (request.Status == "Approved" || request.Status == "Rejected" || request.Status == "Cancelled")
            {
                currentApproval = null;
            }

            var approvalSla = await _slaService.BuildForRequestAsync(request, approvals, formData);

            var model = new RequestDetailViewModel
            {
                Request = request,
                ApprovalHistory = approvals,
                ApprovalSla = approvalSla,
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
                CreatedAt = DateTime.UtcNow
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
            request.UpdatedAt = DateTime.UtcNow;

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
                existing.LastSavedAt = DateTime.UtcNow;
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
            }            // Self-healing for Workflow 1 (Basic Flow = Trưởng phòng -> HR)
            var workflow1 = await _context.Workflows.Include(w => w.Steps).FirstOrDefaultAsync(w => w.Id == 1);
            if (workflow1 != null)
            {
                var steps1 = workflow1.Steps.OrderBy(s => s.StepOrder).ToList();
                var needsReset1 = steps1.Count != 2 || steps1.Any(s => s.Name.Contains("Giám đốc") || s.Name.Contains("Admin"));

                if (needsReset1)
                {
                    _context.WorkflowSteps.RemoveRange(steps1);
                    await _context.SaveChangesAsync();
                    _context.WorkflowSteps.AddRange(
                        new DANGCAPNE.Models.Workflow.WorkflowStep { WorkflowId = 1, Name = "Trưởng phòng duyệt", StepOrder = 1, ApproverType = "Role", ApproverRoleId = 3 },
                        new DANGCAPNE.Models.Workflow.WorkflowStep { WorkflowId = 1, Name = "HR duyệt", StepOrder = 2, ApproverType = "Role", ApproverRoleId = 2 }
                    );
                    await _context.SaveChangesAsync();
                    Console.WriteLine("[SelfHealing] Workflow 1 reset to: Trưởng phòng -> HR");
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
            // Đơn 11–13 (điều chỉnh công, đi muộn/về sớm, tạm ứng lương) không còn hiển thị trên "Chọn loại đơn".
            // Tắt nếu đã tồn tại trong DB; giữ code xử lý SyncExtendedBusinessRequestAsync cho đơn cũ.
            const int retiredStart = 11;
            const int retiredEnd = 13;
            var retired = await _context.FormTemplates
                .Where(f => f.TenantId == tenantId && f.Id >= retiredStart && f.Id <= retiredEnd)
                .ToListAsync();
            foreach (var t in retired)
                t.IsActive = false;
            if (retired.Count > 0)
                await _context.SaveChangesAsync();
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
                    CreatedAt = DateTime.UtcNow
                };
                _context.AttendanceAdjustmentRequests.Add(entity);
            }

            entity.TimesheetId = timesheetId;
            entity.AttendanceDate = attendanceDate.Date;
            entity.RequestedCheckIn = requestedCheckIn;
            entity.RequestedCheckOut = requestedCheckOut;
            entity.Reason = form["reason"].ToString();
            entity.Status = "Pending";
            entity.UpdatedAt = DateTime.UtcNow;
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
                    CreatedAt = DateTime.UtcNow
                };
                _context.LateEarlyRequests.Add(entity);
            }

            entity.AttendanceDate = attendanceDate.Date;
            entity.RequestType = form["request_type"].ToString();
            entity.ExpectedTime = CombineDateAndTime(attendanceDate, form["expected_time"]) ?? attendanceDate;
            entity.ActualTime = CombineDateAndTime(attendanceDate, form["actual_time"]) ?? attendanceDate;
            entity.Reason = form["reason"].ToString();
            entity.Status = "Pending";
            entity.UpdatedAt = DateTime.UtcNow;
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
                    CreatedAt = DateTime.UtcNow
                };
                _context.SalaryAdvanceRequests.Add(entity);
            }

            entity.Amount = ParseDecimalOrDefault(form["advance_amount"]);
            entity.PayrollMonth = form["payroll_month"].ToString();
            entity.NeededByDate = ParseDateOrNull(form["needed_by_date"]) ?? DateTime.Today;
            entity.Reason = form["reason"].ToString();
            entity.Status = "Pending";
            entity.UpdatedAt = DateTime.UtcNow;
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

        private async Task<RequestCreateViewModel> BuildCreateViewModelAsync(
            FormTemplate template,
            int userId,
            int tenantId,
            Dictionary<string, string>? formData = null,
            string? title = null,
            string? formError = null)
        {
            var model = new RequestCreateViewModel
            {
                FormTemplate = template,
                Fields = template.Fields.OrderBy(f => f.DisplayOrder).ToList(),
                FormData = formData ?? new Dictionary<string, string>(),
                Title = title,
                FormError = formError
            };

            if (template.Id == 1)
            {
                model.AnnualLeaveBuckets = await GetAnnualLeaveBucketsAsync(userId, tenantId);
                model.HolidayDates = await GetHolidayDateTokensAsync(tenantId);
            }

            if (template.Id == 2)
            {
                model.OvertimePlans = await GetOvertimePlanItemsAsync(userId, tenantId);
            }

            return model;
        }

                private async Task<string?> ValidateCreateRequestAsync(FormTemplate template, IFormCollection form, int userId, int tenantId)
        {
            if (template.Id == 1)
            {
                var startDate = ParseDateOrNull(form["start_date"]);
                var endDate = ParseDateOrNull(form["end_date"]);
                var requestedDays = (double)ParseDecimalOrDefault(form["total_days"]);

                if (!startDate.HasValue || !endDate.HasValue)
                {
                    return "Vui lòng chọn đầy đủ từ ngày và đến ngày nghỉ phép.";
                }

                if (endDate.Value.Date < startDate.Value.Date)
                {
                    return "Đến ngày phải lớn hơn hoặc bằng từ ngày.";
                }

                requestedDays = await CountWorkingLeaveDaysAsync(tenantId, startDate.Value, endDate.Value);

                if (requestedDays <= 0)
                {
                    return "Số ngày nghỉ phải lớn hơn 0.";
                }

                if (form.Files.Count == 0)
                {
                    return "Đơn nghỉ phép bắt buộc phải có file đính kèm để cấp trên xác minh khi duyệt.";
                }

                if (string.Equals(form["leave_type"].ToString(), "AL", StringComparison.OrdinalIgnoreCase))
                {
                    var annualBuckets = await GetAnnualLeaveBucketsAsync(userId, tenantId);
                    var totalRemaining = annualBuckets.Sum(b => b.Remaining);
                    if (requestedDays > totalRemaining)
                    {
                        return $"Phép năm không đủ. Tổng quỹ còn lại của năm {DateTime.Now.Year - 1}/{DateTime.Now.Year} chỉ còn {totalRemaining:0.##} ngày.";
                    }
                }
            }

            if (template.Id == 2)
            {
                var otDate = ParseDateOrNull(form["ot_date"]);
                var startAt = otDate.HasValue ? CombineDateAndTime(otDate.Value, form["start_time"]) : null;
                var endAt = otDate.HasValue ? CombineDateAndTime(otDate.Value, form["end_time"]) : null;
                var now = DateTime.Now;

                if (!otDate.HasValue || !startAt.HasValue || !endAt.HasValue)
                {
                    return "Vui lòng nhập đầy đủ ngày OT, giờ bắt đầu và giờ kết thúc.";
                }

                if (endAt <= startAt)
                {
                    return "Giờ kết thúc OT phải lớn hơn giờ bắt đầu.";
                }

                if (startAt <= now)
                {
                    return "Đơn OT phải được tạo trước thời điểm bắt đầu làm thêm.";
                }

                if (otDate.Value.Date == now.Date && startAt.Value < now.AddHours(4))
                {
                    return "OT trong ngày làm việc phải báo trước cho cấp trên ít nhất 4 tiếng.";
                }

                if (otDate.Value.Date > now.Date && startAt.Value < now.AddDays(1))
                {
                    return "OT cho ngày kế tiếp trở đi phải báo trước ít nhất 1 ngày.";
                }
            }

            return null;
        }

        private async Task<List<AnnualLeaveBucketViewModel>> GetAnnualLeaveBucketsAsync(int userId, int tenantId)
        {
            var annualLeaveType = await _context.LeaveTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "AL" && x.IsActive);

            if (annualLeaveType == null)
            {
                return new List<AnnualLeaveBucketViewModel>();
            }

            var currentYear = DateTime.Now.Year;
            var previousYear = currentYear - 1;
            var balances = await _context.LeaveBalances
                .AsNoTracking()
                .Where(lb => lb.UserId == userId && lb.LeaveTypeId == annualLeaveType.Id && (lb.Year == previousYear || lb.Year == currentYear))
                .ToListAsync();

            var previous = balances.FirstOrDefault(lb => lb.Year == previousYear);
            var current = balances.FirstOrDefault(lb => lb.Year == currentYear);
            var previousRemaining = Math.Max(previous?.Remaining ?? 0, 0);
            var carryOverLimit = annualLeaveType.CarryOverMaxDays > 0 ? annualLeaveType.CarryOverMaxDays : previousRemaining;
            var carryOver = annualLeaveType.AllowCarryOver ? Math.Min(previousRemaining, carryOverLimit) : 0;
            var currentEntitled = current?.TotalEntitled ?? Math.Min(DateTime.Now.Month, annualLeaveType.DefaultDaysPerYear);
            var currentCarryOver = current?.CarriedOver > 0 ? current.CarriedOver : carryOver;
            var currentUsed = current?.Used ?? 0;
            var currentRemaining = Math.Max(currentEntitled + currentCarryOver + (current?.SeniorityBonus ?? 0) + (current?.CompensatoryDays ?? 0) - currentUsed, 0);

            return new List<AnnualLeaveBucketViewModel>
            {
                new AnnualLeaveBucketViewModel
                {
                    Year = previousYear,
                    TotalEntitled = previous?.TotalEntitled ?? 0,
                    Used = previous?.Used ?? 0,
                    CarryOver = 0,
                    Remaining = previousRemaining
                },
                new AnnualLeaveBucketViewModel
                {
                    Year = currentYear,
                    TotalEntitled = currentEntitled,
                    Used = currentUsed,
                    CarryOver = currentCarryOver,
                    Remaining = currentRemaining
                }
            };
        }

        private async Task<List<string>> GetHolidayDateTokensAsync(int tenantId)
        {
            var currentYear = DateTime.Now.Year;
            var nextYear = currentYear + 1;

            var holidays = await _context.Holidays
                .AsNoTracking()
                .Where(h => h.TenantId == tenantId && (h.IsRecurring || h.Date.Year == currentYear || h.Date.Year == nextYear))
                .ToListAsync();

            return holidays
                .Select(h => h.IsRecurring
                    ? h.Date.ToString("MM-dd", CultureInfo.InvariantCulture)
                    : h.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .Distinct()
                .ToList();
        }

        private async Task<double> CountWorkingLeaveDaysAsync(int tenantId, DateTime startDate, DateTime endDate)
        {
            if (endDate.Date < startDate.Date)
            {
                return 0;
            }

            var holidaySet = new HashSet<string>(await GetHolidayDateTokensAsync(tenantId), StringComparer.OrdinalIgnoreCase);
            var count = 0d;

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek == DayOfWeek.Sunday)
                {
                    continue;
                }

                var exactToken = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var recurringToken = date.ToString("MM-dd", CultureInfo.InvariantCulture);
                if (holidaySet.Contains(exactToken) || holidaySet.Contains(recurringToken))
                {
                    continue;
                }

                count += 1;
            }

            return count;
        }

        private static (double previousYearUsage, double currentYearUsage) AllocateAnnualLeave(
            IReadOnlyList<AnnualLeaveBucketViewModel> annualBuckets,
            double requestedDays)
        {
            if (annualBuckets.Count == 0 || requestedDays <= 0)
            {
                return (0, 0);
            }

            var previousYearRemaining = annualBuckets[0].Remaining;
            var previousYearUsage = Math.Min(previousYearRemaining, requestedDays);
            var currentYearUsage = Math.Max(0, requestedDays - previousYearUsage);
            return (previousYearUsage, currentYearUsage);
        }

        private async Task<List<OvertimePlanItemViewModel>> GetOvertimePlanItemsAsync(int userId, int tenantId)
        {
            var roleNames = await (from ur in _context.UserRoles
                                   join r in _context.Roles on ur.RoleId equals r.Id
                                   where ur.UserId == userId
                                   select r.Name)
                .ToListAsync();

            var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (currentUser == null)
            {
                return new List<OvertimePlanItemViewModel>();
            }

            var overtimeQuery = _context.Requests
                .AsNoTracking()
                .Include(r => r.Requester)
                .Where(r => r.TenantId == tenantId && r.FormTemplateId == 2);

            if (roleNames.Contains("HR"))
            {
                overtimeQuery = overtimeQuery.Where(r => r.Requester != null);
            }
            else if (roleNames.Contains("Manager"))
            {
                overtimeQuery = overtimeQuery.Where(r => r.Requester != null && r.Requester.DepartmentId == currentUser.DepartmentId);
            }
            else
            {
                overtimeQuery = overtimeQuery.Where(r => r.RequesterId == userId);
            }

            var overtimeRequests = await overtimeQuery
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            var requestIds = overtimeRequests.Select(r => r.Id).ToList();
            var requestData = await _context.RequestData
                .AsNoTracking()
                .Where(d => requestIds.Contains(d.RequestId))
                .ToListAsync();

            return overtimeRequests.Select(r =>
            {
                var data = requestData.Where(d => d.RequestId == r.Id).ToList();
                var workDate = ParseDateOrNull(data.FirstOrDefault(d => d.FieldKey == "ot_date")?.FieldValue)?.Date ?? r.CreatedAt.Date;
                return new OvertimePlanItemViewModel
                {
                    EmployeeName = r.Requester?.FullName ?? "Chưa xác định",
                    RequestCode = r.RequestCode,
                    WorkDate = workDate,
                    StartTime = data.FirstOrDefault(d => d.FieldKey == "start_time")?.FieldValue ?? "--:--",
                    EndTime = data.FirstOrDefault(d => d.FieldKey == "end_time")?.FieldValue ?? "--:--",
                    Status = r.Status
                };
            }).ToList();
        }

        private async Task<List<ApprovalStepDraft>> BuildApprovalStepsAsync(FormTemplate template, int requesterId, int tenantId)
        {
            var steps = new List<ApprovalStepDraft>();
            var directManagerId = await ResolveDirectManagerIdAsync(requesterId);
            var hrUserId = await ResolveRoleUserIdAsync(tenantId, "HR", requesterId);
            var accountantUserId = (int?)null;
            var directorUserId = (int?)null;
            var requiresDirector = false;
            var isSalaryAdvance = template.Id == 13 || (template.Name?.Contains("ứng lương", StringComparison.OrdinalIgnoreCase) ?? false);

            void AddStep(string stepName, int? approverId)
            {
                if (!approverId.HasValue || approverId.Value == requesterId)
                {
                    return;
                }

                if (steps.Any(s => s.ApproverId == approverId.Value))
                {
                    return;
                }

                steps.Add(new ApprovalStepDraft(steps.Count + 1, stepName, approverId.Value));
            }

            AddStep("Trưởng phòng duyệt", directManagerId);

            if (isSalaryAdvance)
            {
                AddStep("Kế toán duyệt", accountantUserId);
                AddStep("HR duyệt", hrUserId);
            }
            else
            {
                AddStep("HR duyệt", hrUserId);
                if (requiresDirector)
                {
                    AddStep("Giám đốc duyệt", directorUserId);
                }
            }

            return steps;
        }

        private async Task<int?> ResolveDirectManagerIdAsync(int requesterId)
        {
            var managerId = await _context.UserManagers
                .Where(um => um.UserId == requesterId && um.IsPrimary && (um.EndDate == null || um.EndDate > DateTime.Now))
                .Select(um => (int?)um.ManagerId)
                .FirstOrDefaultAsync();

            if (!managerId.HasValue)
            {
                return null;
            }

            var delegation = await _context.Delegations
                .Where(d => d.DelegatorId == managerId && d.IsActive && d.StartDate <= DateTime.Now && d.EndDate >= DateTime.Now)
                .Select(d => (int?)d.DelegateId)
                .FirstOrDefaultAsync();

            if (delegation.HasValue && delegation.Value != requesterId)
            {
                return delegation;
            }

            return managerId;
        }

        private async Task<int?> ResolveRoleUserIdAsync(int tenantId, string roleName, int? excludeUserId = null)
        {
            return await (from ur in _context.UserRoles
                          join r in _context.Roles on ur.RoleId equals r.Id
                          join u in _context.Users on ur.UserId equals u.Id
                          where r.TenantId == tenantId && r.Name == roleName && (!excludeUserId.HasValue || u.Id != excludeUserId.Value)
                          orderby u.Id
                          select (int?)u.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<int?> ResolveDirectorUserIdAsync()
        {
            return await _context.Users
                .Where(u => u.DepartmentId == 1 || u.JobTitleId == 1 || u.PositionId == 1)
                .OrderBy(u => u.Id)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<int?> ResolveAccountingUserIdAsync()
        {
            return await _context.Users
                .Where(u => u.DepartmentId == 4 || u.PositionId == 4 || u.Email.Contains("accountant"))
                .OrderBy(u => u.Id)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<string?> ValidateApprovalRoutingAsync(int requesterId, int tenantId)
        {
            var directManagerId = await ResolveDirectManagerIdAsync(requesterId);
            if (!directManagerId.HasValue)
            {
                return "ChÆ°a cáº¥u hÃ¬nh TrÆ°á»Ÿng phÃ²ng trá»±c tiáº¿p cho nhÃ¢n viÃªn nÃ y, nÃªn chÆ°a thá»ƒ gá»­i Ä‘Æ¡n.";
            }

            var hrUserId = await ResolveRoleUserIdAsync(tenantId, "HR", requesterId);
            if (!hrUserId.HasValue)
            {
                return "Há»‡ thá»‘ng chÆ°a cÃ³ ngÆ°á»i duyá»‡t HR kháº£ dá»¥ng, nÃªn chÆ°a thá»ƒ gá»­i Ä‘Æ¡n.";
            }

            return null;
        }

        private sealed record TemplateFieldSeed(int Id, string Label, string FieldName, string FieldType, bool IsRequired, int DisplayOrder, int Width);
        private sealed record TemplateOptionSeed(int Id, int FormFieldId, string Label, string Value, int DisplayOrder);
        private sealed record ApprovalStepDraft(int StepOrder, string StepName, int ApproverId);

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
