using DANGCAPNE.Services;
using Xunit;

namespace DANGCAPNE.Tests;

public class AttendanceRiskScoringServiceTests
{
    [Fact]
    public void Evaluate_ShouldReturnLowRisk_WhenAllSignalsValid()
    {
        var service = new AttendanceRiskScoringService();
        var result = service.Evaluate(new AttendanceRiskInput
        {
            InternalNetwork = true,
            BiometricRequired = true,
            FaceMatched = true,
            WifiRequired = true,
            WifiMatched = true,
            QrRequired = true,
            QrMatched = true,
            GpsRequired = true,
            GpsMatched = true,
            HasPhoto = true
        });

        Assert.Equal("Low", result.Level);
        Assert.Equal(100, result.Score);
        Assert.False(result.NeedsManualReview);
    }

    [Fact]
    public void Evaluate_ShouldReturnCriticalRisk_WhenCoreSignalsFail()
    {
        var service = new AttendanceRiskScoringService();
        var result = service.Evaluate(new AttendanceRiskInput
        {
            InternalNetwork = false,
            BiometricRequired = true,
            FaceMatched = false,
            WifiRequired = true,
            WifiMatched = false,
            QrRequired = false,
            GpsRequired = true,
            GpsMatched = false,
            HasPhoto = false
        });

        Assert.Equal("Critical", result.Level);
        Assert.True(result.NeedsManualReview);
        Assert.True(result.Score < 50);
    }
}
