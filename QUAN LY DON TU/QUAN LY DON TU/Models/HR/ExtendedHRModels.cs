using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DANGCAPNE.Models.HR
{
    public class SalaryAdvanceRequest
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? SourceRequestId { get; set; }
        public int UserId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
        public DateTime NeededByDate { get; set; }
        [MaxLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Paid
        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        [MaxLength(20)]
        public string PayrollMonth { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
        [ForeignKey("ApprovedByUserId")]
        public virtual Organization.User? ApprovedByUser { get; set; }
    }

    public class InsuranceImportBatch
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;
        public int ImportedByUserId { get; set; }
        [MaxLength(30)]
        public string Status { get; set; } = "Draft"; // Draft, Processing, Completed, Failed
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        [MaxLength(2000)]
        public string? Summary { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.Now;

        [ForeignKey("ImportedByUserId")]
        public virtual Organization.User? ImportedByUser { get; set; }
    }
}
