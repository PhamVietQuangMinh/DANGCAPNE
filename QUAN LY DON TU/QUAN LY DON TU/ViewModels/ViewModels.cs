using DANGCAPNE.Models.Organization;
using DANGCAPNE.Models.Requests;
using DANGCAPNE.Models.Workflow;
using DANGCAPNE.Models.Timekeeping;
using DANGCAPNE.Models.SystemModels;
using DANGCAPNE.Models.Finance;
using DANGCAPNE.Models.HR;
using DANGCAPNE.Models.Compliance;

namespace DANGCAPNE.ViewModels
{
    public class LoginViewModel
    {
        public string EmployeeCodeOrEmail { get; set; } = string.Empty;

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

    public class AccountantDashboardViewModel
    {
        public User? CurrentUser { get; set; }
        public string RoleName { get; set; } = "Accountant";
        public bool IsChiefAccountant { get; set; }
        public int UnreadNotifications { get; set; }
        public string SelectedPayrollMonth { get; set; } = string.Empty;
        public bool IsMonthClosed { get; set; }
        public int TotalPayrollEmployees { get; set; }
        public int TotalTimesheets { get; set; }
        public decimal TotalWorkHours { get; set; }
        public decimal TotalOtHours { get; set; }
        public int PendingFinanceApprovals { get; set; }
        public int PendingSalaryAdvanceCount { get; set; }
        public int LateAttendanceCount { get; set; }
        public int AbsentAttendanceCount { get; set; }
        public List<Request> PendingApprovals { get; set; } = new();
        public List<SalaryAdvanceRequest> SalaryAdvances { get; set; } = new();
        public List<PayrollEmployeeSummaryViewModel> PayrollEmployees { get; set; } = new();
        public List<PayrollClosure> RecentClosures { get; set; } = new();
        public List<PayrollSlip> RecentPayrollSlips { get; set; } = new();
        public decimal TotalCashIn { get; set; }
        public decimal TotalCashOut { get; set; }
        public decimal CashOnHand { get; set; }
        public decimal BankBalance { get; set; }
        public decimal TotalReceivableDueSoon { get; set; }
        public decimal TotalPayableDueSoon { get; set; }
        public int DueReceivableCount { get; set; }
        public int DuePayableCount { get; set; }
        public List<CashTransactionViewModel> RecentCashTransactions { get; set; } = new();
        public List<InvoiceSummaryViewModel> DueInvoices { get; set; } = new();
        public List<InvoiceSummaryViewModel> ReceivableInvoices { get; set; } = new();
        public List<InvoiceSummaryViewModel> PayableInvoices { get; set; } = new();
        public List<AccountingDocumentViewModel> AccountingDocuments { get; set; } = new();
        public FinancialReportSnapshotViewModel ProfitAndLoss { get; set; } = new();
        public FinancialReportSnapshotViewModel CashFlow { get; set; } = new();
        public BalanceSheetSnapshotViewModel BalanceSheet { get; set; } = new();
    }

    public class CashTransactionViewModel
    {
        public int Id { get; set; }
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CounterpartyName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ReferenceCode { get; set; }
    }

    public class InvoiceSummaryViewModel
    {
        public int Id { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public string InvoiceType { get; set; } = string.Empty;
        public string CounterpartyName { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal OutstandingAmount { get; set; }
        public DateTime DueDate { get; set; }
        public int AgingDays { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool OverCreditLimit { get; set; }
    }

    public class AccountingDocumentViewModel
    {
        public int Id { get; set; }
        public string DocumentNo { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
        public decimal Amount { get; set; }
        public string? PdfPath { get; set; }
        public string? XmlPath { get; set; }
    }

    public class FinancialReportSnapshotViewModel
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetAmount { get; set; }
        public List<FinancialReportLineViewModel> Lines { get; set; } = new();
    }

    public class FinancialReportLineViewModel
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class BalanceSheetSnapshotViewModel
    {
        public decimal CashAndBank { get; set; }
        public decimal Receivables { get; set; }
        public decimal Payables { get; set; }
        public decimal NetPosition { get; set; }
    }

    public class PayrollEmployeeSummaryViewModel
    {
        public int UserId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int WorkingDays { get; set; }
        public decimal WorkHours { get; set; }
        public decimal OtHours { get; set; }
        public int LateDays { get; set; }
        public int AbsentDays { get; set; }
    }

    public class PayrollRecordsViewModel
    {
        public string SelectedPayrollMonth { get; set; } = string.Empty;
        public List<PayrollSlip> PayrollSlips { get; set; } = new();
        public List<PayrollClosure> PayrollClosures { get; set; } = new();
        public decimal TotalNetSalary { get; set; }
        public decimal TotalAdvanceDeduction { get; set; }
        public decimal TotalLatePenalty { get; set; }
        public PayrollSlip? SelectedPayrollSlip { get; set; }
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
        public int? DepartmentIdFilter { get; set; }
        public DateTime? FromDateFilter { get; set; }
        public DateTime? ToDateFilter { get; set; }
        public string? PriorityFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public List<FormTemplate> FormTemplates { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
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

    public class SignatureProfileViewModel
    {
        public string SignatureName { get; set; } = string.Empty;
        public string? ExistingSignatureImageUrl { get; set; }
        public string? SignatureDataUrl { get; set; }
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
        public List<string> HolidayDates { get; set; } = new();
    }

    public class RequestDetailViewModel
    {
        public Request? Request { get; set; }
        public List<RequestApproval> ApprovalHistory { get; set; } = new();
        public Dictionary<int, ApprovalSlaViewModel> ApprovalSla { get; set; } = new();
        public List<RequestComment> Comments { get; set; } = new();
        public List<RequestAuditLog> AuditLogs { get; set; } = new();
        public List<RequestTimelineEventViewModel> Timeline { get; set; } = new();
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
        public bool RequiresPin { get; set; }
        public int? CurrentApprovalId { get; set; }
        public Dictionary<string, string> FormData { get; set; } = new();
        public List<FormField> FormFields { get; set; } = new();
        public DANGCAPNE.Services.RequestRiskAssessment? RiskAssessment { get; set; }
    }

    public class RequestTimelineEventViewModel
    {
        public DateTime At { get; set; }
        public string Kind { get; set; } = string.Empty; // Created, Approved, Rejected, Comment, Status
        public string Title { get; set; } = string.Empty;
        public string? ActorName { get; set; }
        public string? Details { get; set; }
    }

    public class RequestVerifyViewModel
    {
        public Request? Request { get; set; }
        public List<RequestApproval> Approvals { get; set; } = new();
        public bool CanOpenInSystem { get; set; }
    }

    public class ApprovalListViewModel
    {
        public List<RequestApproval> PendingApprovals { get; set; } = new();
        public List<RequestApproval> ProcessedApprovals { get; set; } = new();
        public Dictionary<int, ApprovalSlaViewModel> ApprovalSla { get; set; } = new();
        public string? StatusFilter { get; set; }
        public int TotalPending { get; set; }
        public List<int> DelegatedApprovalIds { get; set; } = new();
        public Dictionary<int, string> DelegatedFromNames { get; set; } = new();
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

        // HR image report metrics
        public Dictionary<string, int> EmployeeDepartmentStats { get; set; } = new();
        public int TotalLeaveMinutesUsed { get; set; }
        public int TotalOvertimeMinutesThisMonth { get; set; }
        public int HealthScore { get; set; } = 10; // 1..10
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
        public bool CanEditEmployee { get; set; }
        public bool CanManageTechnicalAccess { get; set; }
        public string RolesSummary { get; set; } = string.Empty;
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
        public bool CanManageRoles { get; set; }
        public bool CanAutoGenerateEmployeeCode { get; set; }
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
        public List<Delegation> MyDelegations { get; set; } = new();
        public List<Delegation> DelegatedToMe { get; set; } = new();
    }

    public class ZaloSettingsViewModel
    {
        public int Id { get; set; }
        public string? AppId { get; set; }
        public string? AccessToken { get; set; }
        public string? OaId { get; set; }
        public string? OaName { get; set; }
        public string? TemplateRequestCreated { get; set; }
        public string? TemplateRequestApproved { get; set; }
        public string? TemplateRequestRejected { get; set; }
        public string? TemplateSlaWarning { get; set; }
        public bool Enabled { get; set; }
        public bool IsMockMode { get; set; } = true;
        public bool NotifyOnRequestCreated { get; set; } = true;
        public bool NotifyOnRequestApproved { get; set; } = true;
        public bool NotifyOnRequestRejected { get; set; } = true;
        public bool NotifyOnSlaWarning { get; set; } = true;

        public string? TestPhone { get; set; }

        public List<DANGCAPNE.Models.SystemModels.ZaloSubscriber> Subscribers { get; set; } = new();
        public List<DANGCAPNE.Models.SystemModels.ZaloNotificationLog> RecentLogs { get; set; } = new();
        public int TotalSent { get; set; }
        public int TotalMocked { get; set; }
        public int TotalFailed { get; set; }
    }

    public class ApproverKpiRow
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Department { get; set; }
        public int TotalHandled { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Pending { get; set; }
        public double AvgHours { get; set; }
        public double RejectionRate { get; set; }
        public int OverdueCount { get; set; }
    }

    public class ApproverKpiViewModel
    {
        public List<ApproverKpiRow> Rows { get; set; } = new();
        public DateTime FromDate { get; set; } = DateTime.Today.AddMonths(-1);
        public DateTime ToDate { get; set; } = DateTime.Today;
    }

    public class SlaManagementViewModel
    {
        public List<SlaConfig> Configs { get; set; } = new();
        public List<EscalationRule> EscalationRules { get; set; } = new();
        public List<FormTemplate> FormTemplates { get; set; } = new();
        public List<User> PotentialEscalationTargets { get; set; } = new();
        public int? SelectedFormTemplateId { get; set; }
        public int ReminderHours { get; set; } = 24;
        public int EscalationHours { get; set; } = 48;
        public bool AutoRemind { get; set; } = true;
        public bool AutoEscalate { get; set; } = true;
        public int? EscalateToUserId { get; set; }
        public int EscalateAfterHours { get; set; } = 48;
        public string? NotificationMessage { get; set; }
        public int? SelectedSlaConfigId { get; set; }
    }

    public class PolicyPortalViewModel
    {
        public List<PolicyDocument> ActivePolicies { get; set; } = new();
        public List<PolicyAcknowledgement> MyAcknowledgements { get; set; } = new();
        public List<PolicyDocument> RecentPolicies { get; set; } = new();
        public bool CanManage { get; set; }
        public string? Title { get; set; }
        public string? Version { get; set; }
        public string? FileUrl { get; set; }
        public Dictionary<int, int> AcknowledgedCounts { get; set; } = new();
        public Dictionary<int, int> PendingCounts { get; set; } = new();
    }

    public class EnterpriseChecklistViewModel
    {
        public string Mode { get; set; } = "onboarding";
        public int? SelectedUserId { get; set; }
        public List<User> Employees { get; set; } = new();
        public List<OnboardingTaskTemplate> OnboardingTemplates { get; set; } = new();
        public List<OffboardingTaskTemplate> OffboardingTemplates { get; set; } = new();
        public List<OnboardingTask> OnboardingTasks { get; set; } = new();
        public List<OffboardingTask> OffboardingTasks { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public string? TemplateName { get; set; }
        public string? TemplateDescription { get; set; }
        public int DefaultDueDays { get; set; } = 7;
        public int? DefaultAssigneeRoleId { get; set; }
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
