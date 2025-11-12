using System.Diagnostics;
using System.Runtime.InteropServices;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class BrowserService(ILogger<BrowserService> logger) : IBrowserService
{
    public Task OpenUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Opening URL in browser: {Url}", url);

            var processStartInfo = CreateProcessStartInfo(url);
            Process.Start(processStartInfo);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error opening URL in browser: {Url}", url);
            throw;
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = url,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new ProcessStartInfo
            {
                FileName = "open",
                Arguments = url,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        
        throw new PlatformNotSupportedException("Unsupported operating system");
    }
}

