using System.Reflection;
using System.Text;

namespace Tray.App.Configuration;

internal static class EmbeddedConfigTemplates
{
    private const string ResourcePrefix = "Tray.App.Config.";
    private static readonly Assembly Assembly = typeof(EmbeddedConfigTemplates).Assembly;

    public static bool TryGetTemplate(string fileName, out string template)
    {
        var resourceName = BuildResourceName(fileName);
        using var stream = Assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            template = string.Empty;
            return false;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        template = reader.ReadToEnd();
        return true;
    }

    public static string GetTemplate(string fileName)
    {
        if (!TryGetTemplate(fileName, out var template))
            throw new InvalidOperationException($"Embedded config '{fileName}' not found.");

        return template;
    }

    private static string BuildResourceName(string fileName)
    {
        var normalized = fileName
            .Replace('\\', '/')
            .Replace('/', '.');
        return $"{ResourcePrefix}{normalized}";
    }
}