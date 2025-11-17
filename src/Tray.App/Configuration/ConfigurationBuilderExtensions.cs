using System.Text;
using Microsoft.Extensions.Configuration;

namespace Tray.App.Configuration;

internal static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddEmbeddedJson(
        this IConfigurationBuilder builder,
        string fileName,
        bool optional = false)
    {
        if (EmbeddedConfigTemplates.TryGetTemplate(fileName, out var template))
        {
            var bytes = Encoding.UTF8.GetBytes(template);
            var stream = new MemoryStream(bytes);
            builder.AddJsonStream(stream);
        }
        else if (!optional)
        {
            throw new InvalidOperationException($"Embedded config '{fileName}' not found.");
        }

        return builder;
    }
}