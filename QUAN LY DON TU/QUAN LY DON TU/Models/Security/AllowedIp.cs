namespace DANGCAPNE.Models.Security
{
    public class AllowedIp
    {
        public int Id { get; set; }
        public int TenantId { get; set; }

        /// <summary>Địa chỉ IP được phép đăng nhập (IPv4, đã chuẩn hóa)</summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>Nhãn mô tả thiết bị, ví dụ: "Máy chấm công tầng 1"</summary>
        public string Label { get; set; } = string.Empty;

        public int? AddedByUserId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Organization.User? AddedByUser { get; set; }
    }
}
