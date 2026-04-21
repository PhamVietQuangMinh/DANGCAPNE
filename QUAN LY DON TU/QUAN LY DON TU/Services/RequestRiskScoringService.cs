using DANGCAPNE.Data;
using DANGCAPNE.Models.Requests;
using Microsoft.EntityFrameworkCore;

namespace DANGCAPNE.Services
{
    public sealed class RequestRiskAssessment
    {
        public int Score { get; set; } // 0..100
        public string Level { get; set; } = "Low"; // Low, Medium, High, Critical
        public string Recommendation { get; set; } = "Nên duyệt";
        public List<RiskReason> Reasons { get; set; } = new();
        public int HistoricalApprovalRate { get; set; } // % đơn đã duyệt trong 90 ngày
    }

    public sealed class RiskReason
    {
        public string Icon { get; set; } = "bi-info-circle";
        public string Severity { get; set; } = "info"; // info, warning, danger, success
        public string Text { get; set; } = string.Empty;
    }

    public interface IRequestRiskScoringService
    {
        Task<RequestRiskAssessment> EvaluateAsync(int requestId, CancellationToken cancellationToken = default);
    }

    public class RequestRiskScoringService : IRequestRiskScoringService
    {
        private readonly ApplicationDbContext _context;

        public RequestRiskScoringService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RequestRiskAssessment> EvaluateAsync(int requestId, CancellationToken cancellationToken = default)
        {
            var request = await _context.Requests
                .Include(r => r.FormTemplate)
                .Include(r => r.Requester)
                .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

            var assessment = new RequestRiskAssessment();
            if (request == null) return assessment;

            var score = 0; // 0 = an toàn, càng cao càng rủi ro
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // 1. Lịch sử nhân viên trong 90 ngày
            var requesterHistory = await _context.Requests
                .AsNoTracking()
                .Where(r => r.RequesterId == request.RequesterId && r.CreatedAt >= ninetyDaysAgo && r.Id != requestId)
                .Select(r => new { r.Status, r.FormTemplateId, r.CreatedAt })
                .ToListAsync(cancellationToken);

            var totalRecent = requesterHistory.Count;
            var rejected = requesterHistory.Count(h => h.Status == "Rejected");
            var approved = requesterHistory.Count(h => h.Status == "Approved");
            assessment.HistoricalApprovalRate = totalRecent == 0 ? 100 : (int)Math.Round(approved * 100.0 / Math.Max(totalRecent, 1));

            if (rejected >= 3)
            {
                score += 25;
                assessment.Reasons.Add(new RiskReason { Icon = "bi-x-octagon", Severity = "danger", Text = $"Nhân viên có {rejected} đơn bị từ chối trong 90 ngày gần đây" });
            }
            else if (rejected >= 1)
            {
                score += 10;
                assessment.Reasons.Add(new RiskReason { Icon = "bi-exclamation-triangle", Severity = "warning", Text = $"Nhân viên có {rejected} đơn bị từ chối trong 90 ngày" });
            }
            else if (totalRecent >= 3 && approved == totalRecent)
            {
                assessment.Reasons.Add(new RiskReason { Icon = "bi-patch-check", Severity = "success", Text = $"Lịch sử tốt: {approved}/{totalRecent} đơn đã duyệt trong 90 ngày" });
            }

            // 2. Tần suất đơn cùng loại trong 30 ngày (dấu hiệu lạm dụng)
            var sameTypeIn30Days = requesterHistory.Count(h => h.FormTemplateId == request.FormTemplateId && h.CreatedAt >= thirtyDaysAgo);
            if (sameTypeIn30Days >= 5)
            {
                score += 30;
                assessment.Reasons.Add(new RiskReason { Icon = "bi-repeat", Severity = "danger", Text = $"Đã gửi {sameTypeIn30Days + 1} đơn cùng loại trong 30 ngày — bất thường" });
            }
            else if (sameTypeIn30Days >= 3)
            {
                score += 15;
                assessment.Reasons.Add(new RiskReason { Icon = "bi-repeat", Severity = "warning", Text = $"Đã gửi {sameTypeIn30Days + 1} đơn cùng loại trong 30 ngày" });
            }

            // 3. Mức độ ưu tiên
            if (string.Equals(request.Priority, "Urgent", StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
                assessment.Reasons.Add(new RiskReason { Icon = "bi-fire", Severity = "warning", Text = "Đơn khẩn cấp — cần xem xét kỹ trước khi duyệt" });
            }

            // 4. Đơn tài chính
            if (request.FormTemplate?.RequiresFinancialApproval == true)
            {
                score += 15;
                assessment.Reasons.Add(new RiskReason { Icon = "bi-shield-lock", Severity = "warning", Text = "Đơn có tác động tài chính — yêu cầu PIN duyệt" });
            }

            // 5. Giá trị bất thường (cho đơn công tác phí / tạm ứng)
            var amountData = await _context.RequestData
                .AsNoTracking()
                .Where(d => d.RequestId == requestId && (d.FieldKey == "amount" || d.FieldKey == "total_amount" || d.FieldKey == "advance_amount"))
                .FirstOrDefaultAsync(cancellationToken);

            if (amountData != null && decimal.TryParse(amountData.FieldValue, out var amount))
            {
                if (amount >= 50_000_000m)
                {
                    score += 20;
                    assessment.Reasons.Add(new RiskReason { Icon = "bi-cash-stack", Severity = "danger", Text = $"Giá trị đơn ≥ 50 triệu ({amount:N0} VND) — cần xem xét kỹ" });
                }
                else if (amount >= 10_000_000m)
                {
                    score += 8;
                    assessment.Reasons.Add(new RiskReason { Icon = "bi-cash", Severity = "warning", Text = $"Giá trị đơn {amount:N0} VND — lớn hơn trung bình" });
                }
            }

            // 6. Đơn gửi ngoài giờ hành chính
            var hour = request.CreatedAt.AddHours(7).Hour; // VN timezone
            if (hour < 6 || hour >= 22)
            {
                score += 5;
                assessment.Reasons.Add(new RiskReason { Icon = "bi-moon", Severity = "info", Text = $"Đơn được gửi lúc {hour}h — ngoài giờ hành chính" });
            }

            // 7. Nhân viên mới (chưa đủ 30 ngày vào làm)
            if (request.Requester != null && request.Requester.CreatedAt >= thirtyDaysAgo)
            {
                score += 10;
                assessment.Reasons.Add(new RiskReason { Icon = "bi-person-plus", Severity = "warning", Text = "Nhân viên mới vào (< 30 ngày) — cần xác minh kỹ" });
            }

            // 8. Không có attachment cho đơn cần bằng chứng
            var hasAttachment = await _context.RequestAttachments.AnyAsync(a => a.RequestId == requestId, cancellationToken);
            var category = request.FormTemplate?.Category ?? string.Empty;
            if (!hasAttachment && (category.Equals("Expense", StringComparison.OrdinalIgnoreCase) ||
                                     category.Equals("Leave", StringComparison.OrdinalIgnoreCase)))
            {
                score += 10;
                assessment.Reasons.Add(new RiskReason { Icon = "bi-paperclip", Severity = "warning", Text = "Thiếu chứng từ đính kèm cho loại đơn này" });
            }

            // Nếu hoàn toàn sạch, thêm 1 dòng tích cực
            if (assessment.Reasons.Count == 0)
            {
                assessment.Reasons.Add(new RiskReason { Icon = "bi-check2-circle", Severity = "success", Text = "Đơn không có dấu hiệu rủi ro bất thường" });
            }

            // Tổng hợp
            assessment.Score = Math.Clamp(score, 0, 100);
            assessment.Level = assessment.Score switch
            {
                >= 60 => "Critical",
                >= 40 => "High",
                >= 20 => "Medium",
                _ => "Low"
            };
            assessment.Recommendation = assessment.Level switch
            {
                "Critical" => "⛔ Cân nhắc kỹ trước khi duyệt — có nhiều yếu tố rủi ro",
                "High" => "⚠️ Cần xem xét cẩn thận và có thể yêu cầu bổ sung thông tin",
                "Medium" => "ℹ️ Có vài điểm cần lưu ý, nhưng không nghiêm trọng",
                _ => "✅ Đơn an toàn, có thể duyệt"
            };

            return assessment;
        }
    }
}
