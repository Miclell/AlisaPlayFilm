using Core.Entities;
using HtmlAgilityPack;

namespace Core.Interfaces;

public interface ICaptchaDetectionService
{
    bool HasCaptcha(HtmlDocument htmlDocument);
    bool HasCaptcha(string html);
    CaptchaDetectionResult DetectCaptcha(HtmlDocument htmlDocument);
    CaptchaDetectionResult DetectCaptcha(string html);
    void AddCustomIndicators(IEnumerable<string> xpathSelectors);
    void AddCustomTextIndicators(IEnumerable<string> texts);
}