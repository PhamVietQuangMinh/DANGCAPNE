namespace DANGCAPNE.Services
{
    public sealed class AttendanceRiskInput
    {
        public bool InternalNetwork { get; set; }
        public bool BiometricRequired { get; set; }
        public bool FaceMatched { get; set; }
        public bool WifiRequired { get; set; }
        public bool WifiMatched { get; set; }
        public bool QrRequired { get; set; }
        public bool QrMatched { get; set; }
        public bool GpsRequired { get; set; }
        public bool GpsMatched { get; set; }
        public bool HasPhoto { get; set; }
    }

    public sealed class AttendanceRiskResult
    {
        public int Score { get; set; }
        public string Level { get; set; } = "Low";
        public bool NeedsManualReview { get; set; }
        public List<string> Reasons { get; set; } = new();
    }

    public interface IAttendanceRiskScoringService
    {
        AttendanceRiskResult Evaluate(AttendanceRiskInput input);
    }

    public class AttendanceRiskScoringService : IAttendanceRiskScoringService
    {
        public AttendanceRiskResult Evaluate(AttendanceRiskInput input)
        {
            var score = 100;
            var reasons = new List<string>();

            if (!input.InternalNetwork)
            {
                score -= 60;
                reasons.Add("Outside internal network");
            }

            if (input.BiometricRequired && !input.FaceMatched)
            {
                score -= 35;
                reasons.Add("Face mismatch");
            }

            if (input.WifiRequired && !input.WifiMatched)
            {
                score -= 20;
                reasons.Add("Wifi mismatch");
            }

            if (input.QrRequired && !input.QrMatched)
            {
                score -= 15;
                reasons.Add("QR mismatch");
            }

            if (input.GpsRequired && !input.GpsMatched)
            {
                score -= 20;
                reasons.Add("GPS out of range");
            }

            if (!input.HasPhoto)
            {
                score -= 10;
                reasons.Add("No check-in photo");
            }

            score = Math.Clamp(score, 0, 100);
            var level = score >= 85 ? "Low" : score >= 70 ? "Medium" : score >= 50 ? "High" : "Critical";
            var needsReview = score < 70 || reasons.Any(r => r.Contains("mismatch", StringComparison.OrdinalIgnoreCase));

            return new AttendanceRiskResult
            {
                Score = score,
                Level = level,
                NeedsManualReview = needsReview,
                Reasons = reasons
            };
        }
    }
}
