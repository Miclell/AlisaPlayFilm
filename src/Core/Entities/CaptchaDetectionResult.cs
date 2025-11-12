using Core.Enums;

namespace Core.Entities;

public class CaptchaDetectionResult
{
    public bool HasCaptcha { get; set; }
    public List<string> DetectedIndicators { get; set; } = [];
    public CaptchaType? CaptchaType { get; set; }
    public int ConfidenceLevel { get; set; } // 0-100
    public string? AdditionalInfo { get; set; }
}