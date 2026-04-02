---
name: attendance-timekeeping
description: Quy trình chấm công — CheckIn/CheckOut Timesheet, ca làm Shift, phân ca UserShift, đổi ca Swap. Dùng khi chỉnh AttendanceController, ShiftController hoặc tính công.
---

# Attendance & Timekeeping — DANGCAPNE

## Bảng chính

| Bảng | Vai trò |
|------|---------|
| `Timesheets` | Bản ghi CheckIn/CheckOut ngày |
| `Shifts` | Định nghĩa ca làm (tên, giờ vào/ra) |
| `UserShifts` | Phân ca cho từng nhân viên |
| `ShiftSwapRequests` | Yêu cầu đổi ca |

## Timesheet (CheckIn/CheckOut)

```csharp
// CheckIn
var sheet = await _context.Timesheets
    .FirstOrDefaultAsync(t => t.UserId == userId && t.Date == DateTime.Today);

if (sheet == null)
{
    sheet = new Timesheet
    {
        UserId = userId,
        TenantId = tenantId,
        Date = DateTime.Today,
        CheckIn = DateTime.Now
    };
    _context.Timesheets.Add(sheet);
}
else
{
    sheet.CheckIn = DateTime.Now;
}
await _context.SaveChangesAsync();

// CheckOut
sheet.CheckOut = DateTime.Now;
sheet.WorkingHours = (sheet.CheckOut - sheet.CheckIn).TotalHours;
await _context.SaveChangesAsync();
```

## Ca làm (Shift)

```csharp
// Shift mặc định
var shift = await _context.Shifts
    .AsNoTracking()
    .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IsDefault);

// Gán ca cho nhân viên
_context.UserShifts.Add(new UserShift
{
    UserId = userId,
    ShiftId = shift.Id,
    EffectiveDate = startDate,
    EndDate = endDate
});
```

## Đổi ca (Swap)

```csharp
var swap = new ShiftSwapRequest
{
    RequesterId = userId,
    TargetUserId = targetUserId,
    OriginalShiftId = originalShift.Id,
    TargetShiftId = targetShift.Id,
    Reason = reason,
    Status = "Pending",
    CreatedAt = DateTime.Now
};
_context.ShiftSwapRequests.Add(swap);
```

## Tính công tháng

```csharp
var monthStart = new DateTime(year, month, 1);
var monthEnd = monthStart.AddMonths(1).AddDays(-1);

var sheets = await _context.Timesheets
    .AsNoTracking()
    .Where(t => t.UserId == userId && t.Date >= monthStart && t.Date <= monthEnd)
    .ToListAsync();

var totalHours = sheets
    .Where(s => s.CheckOut != null)
    .Sum(s => s.WorkingHours ?? 0);
```
