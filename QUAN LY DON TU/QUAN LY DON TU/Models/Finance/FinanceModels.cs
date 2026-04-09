using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DANGCAPNE.Models.Finance
{
    public class Project
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        public int? ManagerId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [MaxLength(30)]
        public string Status { get; set; } = "Active"; // Active, Completed, OnHold
        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; } = 0;
        [Column(TypeName = "decimal(18,2)")]
        public decimal OtCost { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        [ForeignKey("ManagerId")]
        public virtual Organization.User? Manager { get; set; }
    }

    public class ExpenseCategory
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Taxi, Hotel, Meals, Others
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxAmount { get; set; }
        public bool RequiresReceipt { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class Currency
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(3)]
        public string Code { get; set; } = "VND";
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(5)]
        public string Symbol { get; set; } = "₫";
        public bool IsDefault { get; set; } = false;
    }

    public class ExchangeRate
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int FromCurrencyId { get; set; }
        public int ToCurrencyId { get; set; }
        [Column(TypeName = "decimal(18,6)")]
        public decimal Rate { get; set; }
        public DateTime EffectiveDate { get; set; }
        [MaxLength(100)]
        public string Source { get; set; } = "Manual"; // Manual, Vietcombank, API
        [ForeignKey("FromCurrencyId")]
        public virtual Currency? FromCurrency { get; set; }
        [ForeignKey("ToCurrencyId")]
        public virtual Currency? ToCurrency { get; set; }
    }

    public class AssetCategory
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Laptop, Monitor, Phone, Desk
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
    }

    public class Asset
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        [Required, MaxLength(50)]
        public string AssetCode { get; set; } = string.Empty;
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        [MaxLength(30)]
        public string Status { get; set; } = "Available"; // Available, Assigned, Maintenance, Retired
        public int? AssignedToUserId { get; set; }
        public DateTime? AssignedDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; } = 0;
        public DateTime PurchaseDate { get; set; }
        [MaxLength(100)]
        public string? SerialNumber { get; set; }
        [ForeignKey("CategoryId")]
        public virtual AssetCategory? Category { get; set; }
        [ForeignKey("AssignedToUserId")]
        public virtual Organization.User? AssignedToUser { get; set; }
    }

    public class PayrollClosure
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        [Required, MaxLength(20)]
        public string PayrollMonth { get; set; } = string.Empty; // yyyy-MM
        public int ClosedByUserId { get; set; }
        public DateTime ClosedAt { get; set; } = DateTime.Now;
        public int EmployeeCount { get; set; }
        public int TimesheetCount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalWorkHours { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalOtHours { get; set; }
        public int PendingAdvanceCount { get; set; }
        [MaxLength(1000)]
        public string? Notes { get; set; }

        [ForeignKey("ClosedByUserId")]
        public virtual Organization.User? ClosedByUser { get; set; }
    }

    public class PayrollSlip
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int PayrollClosureId { get; set; }
        public int UserId { get; set; }
        [Required, MaxLength(20)]
        public string PayrollMonth { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalary { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryCoefficient { get; set; }
        public int StandardWorkDays { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal StandardWorkHoursPerDay { get; set; }
        public int ActualWorkingDays { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualWorkHours { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal OvertimeHours { get; set; }
        public int LateMinutes { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MainSalary { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal OvertimeSalary { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedAllowance { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherIncome { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal LatePenalty { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvanceDeduction { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetSalary { get; set; }
        [MaxLength(500)]
        public string? PdfPath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("PayrollClosureId")]
        public virtual PayrollClosure? PayrollClosure { get; set; }
        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
    }
}
