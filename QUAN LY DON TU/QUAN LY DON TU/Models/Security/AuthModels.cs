using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DANGCAPNE.Models.Security
{
    public class AuthAuditLog
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? UserId { get; set; }
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string Action { get; set; } = string.Empty; // LoginSuccess, LoginFailed, Logout, PasswordChanged, ProfileUpdated
        public bool IsSuccess { get; set; } = true;
        [MaxLength(500)]
        public string? Details { get; set; }
        [MaxLength(50)]
        public string? IpAddress { get; set; }
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
    }

    public class PasswordHistory
    {
        [Key]
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }
        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;
        public int? ChangedByUserId { get; set; }
        [MaxLength(50)]
        public string ChangeSource { get; set; } = "SelfService"; // SelfService, AdminReset, Enrollment
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual Organization.User? User { get; set; }
        [ForeignKey("ChangedByUserId")]
        public virtual Organization.User? ChangedByUser { get; set; }
    }
}
