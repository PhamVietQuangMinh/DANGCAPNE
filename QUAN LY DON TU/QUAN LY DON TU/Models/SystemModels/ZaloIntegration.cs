using System.ComponentModel.DataAnnotations;

namespace DANGCAPNE.Models.SystemModels
{
    /// <summary>
    /// Lưu số điện thoại Zalo mà nhân viên đăng ký để nhận thông báo ZNS.
    /// Một user chỉ có 1 số Zalo.
    /// </summary>
    public class ZaloSubscriber
    {
        [Key] public int Id { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }

        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;   // Định dạng E.164: +84xxxxxxxxx

        public bool Verified { get; set; }
        public bool Enabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual Organization.User? User { get; set; }
    }

    /// <summary>
    /// Cấu hình ZNS toàn tenant: App ID, Access Token, Template IDs.
    /// Khi IsMockMode = true → gửi mô phỏng + ghi log, không gọi Zalo API thật.
    /// </summary>
    public class ZaloSettings
    {
        [Key] public int Id { get; set; }
        public int TenantId { get; set; }

        [MaxLength(50)] public string? AppId { get; set; }
        [MaxLength(512)] public string? AccessToken { get; set; }
        [MaxLength(512)] public string? RefreshToken { get; set; }
        public DateTime? TokenExpiresAt { get; set; }

        [MaxLength(100)] public string? OaId { get; set; }                 // ID Official Account
        [MaxLength(150)] public string? OaName { get; set; }

        // Template IDs cho từng loại thông báo (do Zalo duyệt)
        [MaxLength(50)] public string? TemplateRequestCreated { get; set; }
        [MaxLength(50)] public string? TemplateRequestApproved { get; set; }
        [MaxLength(50)] public string? TemplateRequestRejected { get; set; }
        [MaxLength(50)] public string? TemplateSlaWarning { get; set; }

        public bool Enabled { get; set; }
        public bool IsMockMode { get; set; } = true;                       // Mặc định chạy mô phỏng

        public bool NotifyOnRequestCreated { get; set; } = true;
        public bool NotifyOnRequestApproved { get; set; } = true;
        public bool NotifyOnRequestRejected { get; set; } = true;
        public bool NotifyOnSlaWarning { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ZaloNotificationLog
    {
        [Key] public long Id { get; set; }
        public int TenantId { get; set; }
        public int? UserId { get; set; }

        [MaxLength(20)] public string? PhoneNumber { get; set; }
        [MaxLength(50)] public string Category { get; set; } = string.Empty;
        [MaxLength(50)] public string? TemplateId { get; set; }
        [MaxLength(50)] public string? MessageId { get; set; }               // ID Zalo trả về
        public string MessagePreview { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool WasMocked { get; set; }                                  // true = mô phỏng
        [MaxLength(500)] public string? ErrorMessage { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
