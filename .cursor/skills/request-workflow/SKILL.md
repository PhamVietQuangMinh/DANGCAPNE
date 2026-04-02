---
name: request-workflow
description: Workflow quy trình đơn từ — tạo, phê duyệt, duyệt/cancel/reject, gửi notification. Dùng khi tạo mới RequestController, ApprovalController, hoặc chỉnh luồng phê duyệt.
---

# Request Workflow — DANGCAPNE

## Luồng chính

```
Tạo đơn → Pending → (QP duyệt) → Approved/Rejected
                         ↓
                    Yêu cầu sửa → RequestEdit → Pending
```

## Bảng chính

| Bảng | Vai trò |
|------|---------|
| `Requests` | Đơn từ gốc |
| `RequestApprovals` | Duyệt theo từng cấp |
| `RequestData` | Dữ liệu form JSON/dynamic |
| `RequestAuditLog` | Lịch sử thay đổi |
| `FormTemplates` | Template biểu mẫu |
| `Notifications` | Thông báo cho người liên quan |

## Tạo đơn mới

```csharp
var request = new Request
{
    TenantId = tenantId,
    RequesterId = userId,
    FormTemplateId = templateId,
    Status = "Pending",
    CreatedAt = DateTime.Now
};
_context.Requests.Add(request);
await _context.SaveChangesAsync();

// Tạo approval cho mỗi cấp phê duyệt
foreach (var step in approvalSteps)
{
    _context.RequestApprovals.Add(new RequestApproval
    {
        RequestId = request.Id,
        ApproverId = step.ApproverId,
        StepOrder = step.Order,
        Status = "Pending"
    });
}
```

## Phê duyệt / Từ chối

```csharp
// Approve
var approval = await _context.RequestApprovals
    .FirstOrDefaultAsync(a => a.RequestId == requestId && a.ApproverId == userId);

approval.Status = "Approved";
approval.ApprovedAt = DateTime.Now;

// Kiểm tra nếu là cấp cuối
request.Status = "Approved";

// Cancel (người tạo hủy)
if (request.RequesterId == userId && request.Status != "Approved")
    request.Status = "Cancelled";

// Reject
approval.Status = "Rejected";
request.Status = "Rejected";
```

## Gửi Notification

```csharp
_context.Notifications.Add(new Notification
{
    UserId = recipientId,
    Title = "Đơn được duyệt",
    Message = $"Đơn #{request.Id} đã được duyệt.",
    Type = "Approval",
    IsRead = false,
    CreatedAt = DateTime.Now
});
```

## Audit Log

```csharp
_context.RequestAuditLogs.Add(new RequestAuditLog
{
    RequestId = request.Id,
    Action = "StatusChanged",
    OldValue = oldStatus,
    NewValue = newStatus,
    ChangedBy = userId,
    ChangedAt = DateTime.Now
});
```
