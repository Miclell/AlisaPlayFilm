using System.Runtime.InteropServices;

namespace Tray.App.Configuration;

internal static class UserConfigStore
{
    private const string AppFolderName = "AlisaPlayFilm";

    public static string EnsureDefaultConfig(string fileName)
    {
        var template = EmbeddedConfigTemplates.GetTemplate(fileName);
        return EnsureFile(fileName, template);
    }

    public static string EnsureEnvironmentConfig(string fileName)
    {
        if (EmbeddedConfigTemplates.TryGetTemplate(fileName, out var template))
            return EnsureFile(fileName, template);

        return GetPath(fileName);
    }

    public static string GetPath(string fileName)
    {
        var directory = GetDirectory();
        return Path.Combine(directory, fileName);
    }

    private static string EnsureFile(string fileName, string content)
    {
        var path = GetPath(fileName);
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        if (!File.Exists(path))
            File.WriteAllText(path, content);

        return path;
    }

    private static string GetDirectory()
    {
        var baseDir = GetBaseDirectory();
        var target = Path.Combine(baseDir, AppFolderName);
        Directory.CreateDirectory(target);
        return target;
    }

    private static string GetBaseDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdgConfig))
            return xdgConfig;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".config");
    }
}