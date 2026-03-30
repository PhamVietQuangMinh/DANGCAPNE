using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DANGCAPNE.Models.Timekeeping
{
    public class AttendanceAdjustmentRequest
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? SourceRequestId { get; set; }
        public int UserId { get; set; }
        public int? TimesheetId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public DateTime? RequestedCheckIn { get; set; }
        public DateTime? RequestedCheckOut { get; set; }
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
        [MaxLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public int? ApprovedByUserId { get; set; }
        public DateTime? ProcessedAt { get; set; }
        [MaxLength(500)]
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
        [ForeignKey("TimesheetId")]
        public virtual Timesheet? Timesheet { get; set; }
        [ForeignKey("ApprovedByUserId")]
        public virtual Organization.User? ApprovedByUser { get; set; }
    }

    public class LateEarlyRequest
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? SourceRequestId { get; set; }
        public int UserId { get; set; }
        public DateTime AttendanceDate { get; set; }
        [MaxLength(30)]
        public string RequestType { get; set; } = "LateArrival"; // LateArrival, EarlyLeave
        public DateTime ExpectedTime { get; set; }
        public DateTime ActualTime { get; set; }
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
        [MaxLength(30)]
        public string Status { get; set; } = "Pending";
        public int? ApprovedByUserId { get; set; }
        public DateTime? ProcessedAt { get; set; }
        [MaxLength(500)]
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
        [ForeignKey("ApprovedByUserId")]
        public virtual Organization.User? ApprovedByUser { get; set; }
    }

    public class ShiftImportBatch
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        public int ImportedByUserId { get; set; }
        [MaxLength(30)]
        public string Status { get; set; } = "Draft";
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        [MaxLength(2000)]
        public string? Summary { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.Now;

        [ForeignKey("ImportedByUserId")]
        public virtual Organization.User? ImportedByUser { get; set; }
    }

    public class AutoShiftPlan
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? DepartmentId { get; set; }
        [MaxLength(30)]
        public string Strategy { get; set; } = "Balanced"; // Balanced, CoverageFirst, CostOptimized
        [MaxLength(30)]
        public string Status { get; set; } = "Draft";
        public int GeneratedByUserId { get; set; }
        [MaxLength(1000)]
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("DepartmentId")]
        public virtual Organization.Department? Department { get; set; }
        [ForeignKey("GeneratedByUserId")]
        public virtual Organization.User? GeneratedByUser { get; set; }
    }

    public class AutoShiftPlanItem
    {
        [Key]
        public int Id { get; set; }
        public int PlanId { get; set; }
        public int UserId { get; set; }
        public int ShiftId { get; set; }
        public DateTime WorkDate { get; set; }
        public double ConfidenceScore { get; set; } = 0;
        [MaxLength(30)]
        public string Source { get; set; } = "Logic"; // Logic, AI, Manual

        [ForeignKey("PlanId")]
        public virtual AutoShiftPlan? Plan { get; set; }
        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }
    }

    public class ShiftTaskAssignment
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int ShiftId { get; set; }
        public DateTime WorkDate { get; set; }
        public int AssignedToUserId { get; set; }
        public int AssignedByUserId { get; set; }
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Description { get; set; }
        [MaxLength(20)]
        public string Priority { get; set; } = "Normal";
        [MaxLength(20)]
        public string Status { get; set; } = "Open"; // Open, InProgress, Done, Cancelled
        public DateTime? DueAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }
        [ForeignKey("AssignedToUserId")]
        public virtual Organization.User? AssignedToUser { get; set; }
        [ForeignKey("AssignedByUserId")]
        public virtual Organization.User? AssignedByUser { get; set; }
    }

    public class EmployeeOnlineSession
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }
        [MaxLength(100)]
        public string SessionToken { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime LoginAt { get; set; } = DateTime.Now;
        public DateTime LastSeenAt { get; set; } = DateTime.Now;
        public DateTime? LogoutAt { get; set; }
        [MaxLength(20)]
        public string Status { get; set; } = "Online"; // Online, Idle, Offline
        [MaxLength(50)]
        public string? LastIpAddress { get; set; }
        [MaxLength(255)]
        public string? DeviceName { get; set; }

        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
    }
}
