using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DANGCAPNE.Models.SystemModels
{
    public class DigitalSignatureProfile
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }
        [Required, MaxLength(100)]
        public string ProviderName { get; set; } = "Image";
        [Required, MaxLength(150)]
        public string SignatureName { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? SignatureImageUrl { get; set; }
        [MaxLength(255)]
        public string? CertificateThumbprint { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
    }

    public class KpiSnapshot
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? UserId { get; set; }
        public int? DepartmentId { get; set; }
        [Required, MaxLength(50)]
        public string MetricCode { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string MetricName { get; set; } = string.Empty;
        [Column(TypeName = "decimal(18,2)")]
        public decimal MetricValue { get; set; }
        [MaxLength(50)]
        public string Category { get; set; } = "General";
        public DateTime SnapshotDate { get; set; } = DateTime.Today;

        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Organization.Department? Department { get; set; }
    }

    public class RequestCategoryReportSnapshot
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        [Required, MaxLength(50)]
        public string Category { get; set; } = string.Empty;
        public int TotalRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int PendingRequests { get; set; }
        public DateTime SnapshotDate { get; set; } = DateTime.Today;
        public int? GeneratedByUserId { get; set; }

        [ForeignKey("GeneratedByUserId")]
        public virtual Organization.User? GeneratedByUser { get; set; }
    }
}
