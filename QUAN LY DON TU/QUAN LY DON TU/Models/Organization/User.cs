using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DANGCAPNE.Models.Organization
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public int TenantId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        public string AvatarUrl { get; set; } = string.Empty;

        [MaxLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        public int? DepartmentId { get; set; }
        public int? BranchId { get; set; }
        public int? JobTitleId { get; set; }
        public int? PositionId { get; set; }

        public DateTime HireDate { get; set; } = DateTime.Now;
        public DateTime? TerminationDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseSalary { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryCoefficient { get; set; } = 1;

        public int StandardWorkDays { get; set; } = 26;

        [Column(TypeName = "decimal(18,2)")]
        public decimal StandardWorkHoursPerDay { get; set; } = 8;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OvertimeHourlyMultiplier { get; set; } = 1.5m;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LatePenaltyPerMinute { get; set; } = 2000;

        [Column(TypeName = "decimal(18,2)")]
        public decimal FixedAllowance { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherIncome { get; set; } = 0;

        [MaxLength(50)]
        public string Status { get; set; } = "Active"; // Active, Inactive, OnLeave, Terminated

        [MaxLength(50)]
        public string TimeZone { get; set; } = "SE Asia Standard Time";

        [MaxLength(10)]
        public string Locale { get; set; } = "vi-VN";

        public bool TwoFactorEnabled { get; set; } = false;
        [MaxLength(256)]
        public string? PinHash { get; set; }

        public bool IsBiometricEnrolled { get; set; } = false;
        public string? FaceDescriptorFront { get; set; }
        public string? FaceDescriptorLeft { get; set; }
        public string? FaceDescriptorRight { get; set; }
        public string? PortraitImage { get; set; }
        public string? TrustedDeviceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("TenantId")]
        public virtual SystemModels.Tenant? Tenant { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
        [ForeignKey("BranchId")]
        public virtual Branch? Branch { get; set; }
        [ForeignKey("JobTitleId")]
        public virtual JobTitle? JobTitle { get; set; }
        [ForeignKey("PositionId")]
        public virtual Position? Position { get; set; }

        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<UserManager> ManagedBy { get; set; } = new List<UserManager>();
        public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
    }
}
