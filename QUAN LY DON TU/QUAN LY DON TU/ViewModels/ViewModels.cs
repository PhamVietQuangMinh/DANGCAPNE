using DANGCAPNE.Models.Organization;
using DANGCAPNE.Models.Requests;
using DANGCAPNE.Models.Workflow;
using DANGCAPNE.Models.Timekeeping;
using DANGCAPNE.Models.SystemModels;

namespace DANGCAPNE.ViewModels
{
    public class LoginViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class DashboardViewModel
    {
        public User? CurrentUser { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int TotalPendingRequests { get; set; }
        public int TotalApprovedRequests { get; set; }
        public int TotalRejectedRequests { get; set; }
        public int TotalMyRequests { get; set; }
        public int UnreadNotifications { get; set; }
        public double LeaveBalance { get; set; }
        public List<Request> RecentRequests { get; set; } = new();
        public List<Request> PendingApprovals { get; set; } = new();
        public List<Notification> RecentNotifications { get; set; } = new();
        // Chart data
        public Dictionary<string, int> RequestsByMonth { get; set; } = new();
        public Dictionary<string, int> RequestsByDepartment { get; set; } = new();
        public Dictionary<string, int> RequestsByStatus { get; set; } = new();
        public Dictionary<string, int> RequestsByType { get; set; } = new();
        // Additional stats
        public int TotalEmployees { get; set; }
        public int OnLeaveToday { get; set; }
        public int OvertimeThisMonth { get; set; }
        public bool IsAttendanceDone { get; set; } // Cả 2 lượt đã hoàn tất
        public bool IsCheckInDone { get; set; }    // Chỉ mới Check-in, chưa Check-out
        public string? CheckInTime { get; set; }   // Giờ vào để hiển thị
        public List<LeaveBalanceSummary> TeamLeaveBalances { get; set; } = new();
        // Employee page additions
        public List<Timesheet> RecentTimesheets { get; set; } = new();
        public List<Timesheet> CalendarTimesheets { get; set; } = new();
        public List<UserShift> UpcomingShifts { get; set; } = new();
        public List<Shift> Shifts { get; set; } = new();
        public List<User> Colleagues { get; set; } = new();
        public List<ShiftSwapRequest> ShiftSwapRequests { get; set; } = new();
    }

    public class LeaveBalanceSummary
    {
        public string EmployeeName { get; set; } = string.Empty;
        public double TotalEntitled { get; set; }
        public double Used { get; set; }
        public double Remaining { get; set; }
    }

    public class RequestListViewModel
    {
        public List<Request> Requests { get; set; } = new();
        public string? StatusFilter { get; set; }
        public string? TypeFilter { get; set; }
        public string? SearchQuery { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public List<FormTemplate> FormTemplates { get; set; } = new();
    }

        public class AnnualLeaveBucketViewModel
    {
        public int Year { get; set; }
        public double TotalEntitled { get; set; }
        public double Used { get; set; }
        public double CarryOver { get; set; }
        public double Remaining { get; set; }
    }

    public class OvertimePlanItemViewModel
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string RequestCode { get; set; } = string.Empty;
        public DateTime WorkDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class RequestCreateViewModel
    {
        public FormTemplate? FormTemplate { get; set; }
        public List<FormField> Fields { get; set; } = new();
        public Dictionary<string, string> FormData { get; set; } = new();
        public string? Title { get; set; }
        public string Priority { get; set; } = "Normal";
        public string? FormError { get; set; }
        public List<AnnualLeaveBucketViewModel> AnnualLeaveBuckets { get; set; } = new();
        public List<OvertimePlanItemViewModel> OvertimePlans { get; set; } = new();
    }

    public class RequestDetailViewModel
    {
        public Request? Request { get; set; }
        public List<RequestApproval> ApprovalHistory { get; set; } = new();
        public Dictionary<int, ApprovalSlaViewModel> ApprovalSla { get; set; } = new();
        public List<RequestComment> Comments { get; set; } = new();
        public List<RequestAuditLog> AuditLogs { get; set; } = new();
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
        public bool RequiresPin { get; set; }
        public int? CurrentApprovalId { get; set; }
        public Dictionary<string, string> FormData { get; set; } = new();
        public List<FormField> FormFields { get; set; } = new();
    }

    public class ApprovalListViewModel
    {
        public List<RequestApproval> PendingApprovals { get; set; } = new();
        public List<RequestApproval> ProcessedApprovals { get; set; } = new();
        public Dictionary<int, ApprovalSlaViewModel> ApprovalSla { get; set; } = new();
        public string? StatusFilter { get; set; }
        public int TotalPending { get; set; }
    }

    public class ApprovalSlaViewModel
    {
        public int ApprovalId { get; set; }
        public DateTime? DueAt { get; set; }
        public bool IsOverdue { get; set; }
        public bool IsBreached { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string RemainingText { get; set; } = string.Empty;
    }

    public class ApprovalActionViewModel
    {
        public int ApprovalId { get; set; }
        public int RequestId { get; set; }
        public string Action { get; set; } = string.Empty; // Approve, Reject, RequestEdit
        public string? Comments { get; set; }
        public string? Pin { get; set; }
    }

    public class BulkApprovalViewModel
    {
        public List<int> ApprovalIds { get; set; } = new();
        public string Action { get; set; } = "Approve";
        public string? Comments { get; set; }
        public string? Pin { get; set; }
    }

    public class HRDashboardViewModel
    {
        public List<Request> AllRequests { get; set; } = new();
        public List<Request> PendingRequests { get; set; } = new();
        public List<Request> InProgressRequests { get; set; } = new();
        public List<Request> ApprovedRequests { get; set; } = new();
        public List<Request> RejectedRequests { get; set; } = new();
        public List<User> Employees { get; set; } = new();
        public List<LeaveBalance> LeaveBalances { get; set; } = new();
        public Dictionary<string, int> DepartmentStats { get; set; } = new();
        // Anomaly flags
        public List<AnomalyAlert> Anomalies { get; set; } = new();
    }

    public class AnomalyAlert
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Warning"; // Info, Warning, Critical
    }

    public class AdminViewModel
    {
        public List<User> Users { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public List<FormTemplate> FormTemplates { get; set; } = new();
        public List<WorkflowDef> Workflows { get; set; } = new();
        public List<Branch> Branches { get; set; } = new();
        public string ActiveTab { get; set; } = "users";
        // HR Management tabs
        public List<Shift> Shifts { get; set; } = new();
        public List<ShiftSwapRequest> ShiftSwapRequests { get; set; } = new();
        public List<Timesheet> Timesheets { get; set; } = new();
        public string? EmployeeSearch { get; set; }
        public int? EmployeeDepartmentId { get; set; }
        public string? AttendanceSearch { get; set; }
        public int? AttendanceDepartmentId { get; set; }
        public DateTime? AttendanceFromDate { get; set; }
        public DateTime? AttendanceToDate { get; set; }
        public DateTime? RequestStatsFromDate { get; set; }
        public DateTime? RequestStatsToDate { get; set; }
        public int TotalRequestsInRange { get; set; }
        public int PendingRequestsInRange { get; set; }
        public int ApprovedRequestsInRange { get; set; }
        public int RejectedRequestsInRange { get; set; }
        public int InProgressRequestsInRange { get; set; }
        public int CancelledRequestsInRange { get; set; }
        public Dictionary<string, int> TopRequestTypes { get; set; } = new();
        public Dictionary<string, int> RequestStatusStats { get; set; } = new();
        public Dictionary<string, int> RequestsByDay { get; set; } = new();
    }

    public class AdminEmployeeDetailViewModel
    {
        public User Employee { get; set; } = new();
        public List<Timesheet> RecentTimesheets { get; set; } = new();
        public List<ShiftSwapRequest> RecentShiftRequests { get; set; } = new();
    }

    public class AdminShiftRequestEditViewModel
    {
        public ShiftSwapRequest ShiftRequest { get; set; } = new();
        public List<User> Employees { get; set; } = new();
        public List<Shift> Shifts { get; set; } = new();
    }

    public class UserEditViewModel
    {
        public User? User { get; set; }
        public List<Role> AllRoles { get; set; } = new();
        public List<int> SelectedRoleIds { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<Branch> Branches { get; set; } = new();
        public List<JobTitle> JobTitles { get; set; } = new();
        public List<Position> Positions { get; set; } = new();
        public int? ManagerId { get; set; }
        public List<User> PotentialManagers { get; set; } = new();
    }

    public class FormBuilderViewModel
    {
        public FormTemplate? FormTemplate { get; set; }
        public List<FormField> Fields { get; set; } = new();
        public List<WorkflowDef> Workflows { get; set; } = new();
    }

    public class TimekeepingViewModel
    {
        public List<Timesheet> Timesheets { get; set; } = new();
        public List<DailyAttendance> Attendances { get; set; } = new();
        public List<Shift> Shifts { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? UserId { get; set; }
        public User? SelectedUser { get; set; }
        public List<User> Employees { get; set; } = new();
    }

    public class NotificationListViewModel
    {
        public List<Notification> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }

    public class DelegationViewModel
    {
        public List<Delegation> ActiveDelegations { get; set; } = new();
        public List<User> PotentialDelegates { get; set; } = new();
        public int? DelegateId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Reason { get; set; }
    }

    public class ProfileViewModel
    {
        public User? User { get; set; }
        public List<LeaveBalance> LeaveBalances { get; set; } = new();
        public List<Timesheet> AttendanceHistory { get; set; } = new(); // Thêm lịch sử chấm công
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }

    public class ReportViewModel
    {
        public string ReportType { get; set; } = "leave"; // leave, attendance, overtime, expense
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);
        public DateTime EndDate { get; set; } = DateTime.Now;
        public int? DepartmentId { get; set; }
        public List<Department> Departments { get; set; } = new();
        public object? ReportData { get; set; }
    }

    public class RegisterEmailViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public List<Department> Departments { get; set; } = new();
    }

    public class WhitelistViewModel
    {
        public List<User> PendingUsers { get; set; } = new();
        public List<User> ApprovedUsers { get; set; } = new();
    }
}
