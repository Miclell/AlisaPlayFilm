using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using HtmlAgilityPack;

namespace Infrastructure.Services;

public class CaptchaDetectionService : ICaptchaDetectionService
{
    private readonly List<string> _xpathIndicators;
    private readonly List<string> _textIndicators;
    private readonly Dictionary<CaptchaType, List<string>> _typeSpecificIndicators;

    public CaptchaDetectionService()
    {
        _xpathIndicators = [];
        _textIndicators = [];
        _typeSpecificIndicators = new Dictionary<CaptchaType, List<string>>();
        
        InitializeDefaultIndicators();
    }

    private void InitializeDefaultIndicators()
    {
        // ReCaptcha
        var recaptchaIndicators = new[]
        {
            "//div[contains(@class, 'g-recaptcha')]",
            "//div[contains(@class, 'recaptcha')]",
            "//iframe[contains(@src, 'google.com/recaptcha')]",
            "//script[contains(@src, 'google.com/recaptcha')]",
            "//div[@data-sitekey]",
            "//*[contains(@class, 'recaptcha-challenge')]"
        };
        _typeSpecificIndicators[CaptchaType.ReCaptcha] = recaptchaIndicators.ToList();
        _xpathIndicators.AddRange(recaptchaIndicators);

        // hCaptcha
        var hcaptchaIndicators = new[]
        {
            "//div[contains(@class, 'h-captcha')]",
            "//iframe[contains(@src, 'hcaptcha.com')]",
            "//script[contains(@src, 'hcaptcha.com')]",
            "//div[contains(@class, 'hcaptcha')]"
        };
        _typeSpecificIndicators[CaptchaType.HCaptcha] = hcaptchaIndicators.ToList();
        _xpathIndicators.AddRange(hcaptchaIndicators);

        // Yandex SmartCaptcha
        var yandexIndicators = new[]
        {
            // CheckboxCaptcha
            "//div[contains(@class, 'CheckboxCaptcha')]",
            "//form[contains(@id, 'checkbox-captcha')]",
            "//div[contains(@class, 'CheckboxCaptcha-')]",
            "//input[contains(@class, 'CheckboxCaptcha-Button')]",
            "//form[contains(@id, 'checkbox-captcha-form')]",
    
            // Общие индикаторы Яндекс капчи
            "//a[contains(text(), 'SmartCaptcha by Yandex Cloud')]",
            "//a[contains(@href, 'yandex.cloud/ru/services/smartcaptcha')]",
    
            // Текстовые индикаторы
            "//*[contains(text(), 'Подтвердите, что запросы отправляли вы, а не робот')]",
            "//*[contains(text(), 'Я не робот')]",
            "//*[contains(text(), 'Нажмите, чтобы продолжить')]"
        };
        _typeSpecificIndicators[CaptchaType.YandexSmartCaptcha] = yandexIndicators.ToList();
        _xpathIndicators.AddRange(yandexIndicators);

        // Cloudflare Turnstile
        var turnstileIndicators = new[]
        {
            "//div[contains(@class, 'cf-turnstile')]",
            "//script[contains(@src, 'challenges.cloudflare.com/turnstile')]",
            "//iframe[contains(@src, 'challenges.cloudflare.com')]"
        };
        _typeSpecificIndicators[CaptchaType.CloudflareTurnstile] = turnstileIndicators.ToList();
        _xpathIndicators.AddRange(turnstileIndicators);

        // Общие индикаторы капчи
        var generalIndicators = new[]
        {
            "//iframe[contains(@src, 'captcha')]",
            "//img[contains(@src, 'captcha')]",
            "//input[contains(@name, 'captcha')]",
            "//div[contains(@id, 'captcha')]",
            "//div[contains(@class, 'captcha')]",
            "//canvas[contains(@id, 'captcha')]",
            "//script[contains(., 'captcha')]",
            "//meta[contains(@content, 'captcha')]",
            "//link[contains(@href, 'captcha')]",
            
            // Audio captcha
            "//audio[contains(@src, 'captcha')]",
            "//a[contains(@href, 'audio.captcha')]",
            
            // Поле ввода капчи
            "//input[contains(@placeholder, 'captcha')]",
            "//input[contains(@alt, 'captcha')]"
        };
        _xpathIndicators.AddRange(generalIndicators);

        // Текстовые индикаторы (мультиязычные)
        var textIndicators = new[]
        {
            // Русский
            "капча", "каптча", "каптча", "я не робот", "подтвердите что вы не робот",
            "введите код", "введите символы", "защита от роботов", "проверка безопасности",
            
            // English
            "captcha", "i'm not a robot", "i am not a robot", "verify you are human",
            "type the characters", "enter the code", "security check", "robot check",
            
            // Другие языки
            "验证码", "キャプチャ", "captcha", "كابتشا"
        };
        _textIndicators.AddRange(textIndicators);
    }

    public bool HasCaptcha(HtmlDocument htmlDocument)
    {
        return DetectCaptcha(htmlDocument).HasCaptcha;
    }

    public bool HasCaptcha(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return HasCaptcha(doc);
    }

    public CaptchaDetectionResult DetectCaptcha(HtmlDocument htmlDocument)
    {
        var result = new CaptchaDetectionResult();
        
        if (htmlDocument.DocumentNode == null)
            return result;

        var detectedSelectors = new List<string>();
        var typeHits = new Dictionary<CaptchaType, int>();

        // Проверка XPath селекторов
        foreach (var selector in _xpathIndicators.Distinct())
        {
            try
            {
                var elements = htmlDocument.DocumentNode.SelectNodes(selector);
                if (elements is not { Count: > 0 }) continue;
                detectedSelectors.Add(selector);
                    
                // Определение типа капчи
                foreach (var kvp in _typeSpecificIndicators)
                {
                    if (!kvp.Value.Contains(selector)) continue;
                    
                    typeHits.TryAdd(kvp.Key, 0);
                    typeHits[kvp.Key]++;
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки при необходимости
                System.Diagnostics.Debug.WriteLine($"Error processing selector {selector}: {ex.Message}");
            }
        }

        // Проверка текстовых индикаторов
        var htmlText = htmlDocument.DocumentNode.InnerText.ToLowerInvariant();
        foreach (var text in _textIndicators.Distinct())
        {
            if (htmlText.Contains(text.ToLowerInvariant()))
            {
                detectedSelectors.Add($"Text: {text}");
            }
        }

        // Проверка мета-тегов и скриптов
        CheckMetaTags(htmlDocument, detectedSelectors);
        CheckScripts(htmlDocument, detectedSelectors, typeHits);

        result.DetectedIndicators = detectedSelectors;
        result.HasCaptcha = detectedSelectors.Count > 0;
        
        if (result.HasCaptcha)
        {
            result.CaptchaType = DetermineCaptchaType(typeHits, detectedSelectors);
            result.ConfidenceLevel = CalculateConfidence(detectedSelectors.Count, typeHits);
            result.AdditionalInfo = $"Found {detectedSelectors.Count} indicators";
        }

        return result;
    }

    public CaptchaDetectionResult DetectCaptcha(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return DetectCaptcha(doc);
    }

    private static void CheckMetaTags(HtmlDocument htmlDocument, List<string> detectedSelectors)
    {
        var metaTags = htmlDocument.DocumentNode.SelectNodes("//meta[contains(@name, 'captcha') or contains(@content, 'captcha')]");
        if (metaTags != null)
        {
            detectedSelectors.AddRange(metaTags.Select(meta => $"Meta: {meta.OuterHtml}"));
        }
    }

    private static void CheckScripts(HtmlDocument htmlDocument, List<string> detectedSelectors, Dictionary<CaptchaType, int> typeHits)
    {
        var scripts = htmlDocument.DocumentNode.SelectNodes("//script");
        if (scripts == null) return;

        foreach (var script in scripts)
        {
            var scriptContent = script.InnerHtml + script.GetAttributeValue("src", "");
            
            var captchaPatterns = new Dictionary<string, CaptchaType>
            {
                { "recaptcha", CaptchaType.ReCaptcha },
                { "hcaptcha", CaptchaType.HCaptcha },
                { "smartcaptcha", CaptchaType.YandexSmartCaptcha },
                { "turnstile", CaptchaType.CloudflareTurnstile },
                { "captcha", CaptchaType.Unknown }
            };

            foreach (var pattern in captchaPatterns)
            {
                if (!scriptContent.ToLowerInvariant().Contains(pattern.Key)) continue;
                detectedSelectors.Add($"Script: {pattern.Key}");
                    
                typeHits.TryAdd(pattern.Value, 0);
                typeHits[pattern.Value]++;
            }
        }
    }

    private static CaptchaType? DetermineCaptchaType(Dictionary<CaptchaType, int> typeHits, List<string> detectedSelectors)
    {
        if (typeHits.Any())
        {
            return typeHits.OrderByDescending(x => x.Value).First().Key;
        }

        // Эвристическое определение по обнаруженным селекторам
        if (detectedSelectors.Any(s => s.Contains("g-recaptcha")))
            return CaptchaType.ReCaptcha;
        if (detectedSelectors.Any(s => s.Contains("h-captcha")))
            return CaptchaType.HCaptcha;
        if (detectedSelectors.Any(s => s.Contains("SmartCaptcha")))
            return CaptchaType.YandexSmartCaptcha;
        if (detectedSelectors.Any(s => s.Contains("cf-turnstile")))
            return CaptchaType.CloudflareTurnstile;
        if (detectedSelectors.Any(s => s.Contains("img") || s.Contains("canvas")))
            return CaptchaType.Image;
        if (detectedSelectors.Any(s => s.Contains("audio")))
            return CaptchaType.Audio;

        return CaptchaType.Unknown;
    }

    private static int CalculateConfidence(int indicatorCount, Dictionary<CaptchaType, int> typeHits)
    {
        var confidence = Math.Min(indicatorCount * 10, 100);
        
        if (typeHits.Any(x => x.Value > 1))
            confidence = Math.Min(confidence + 20, 100);
            
        return confidence;
    }

    public void AddCustomIndicators(IEnumerable<string> xpathSelectors)
    {
        _xpathIndicators.AddRange(xpathSelectors);
    }

    public void AddCustomTextIndicators(IEnumerable<string> texts)
    {
        _textIndicators.AddRange(texts);
    }
}