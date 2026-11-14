using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WinClip.Extensions;

/// <summary>
/// Context provided to extensions during initialization.
/// Contains services the extension can use without coupling to the host.
/// </summary>
public sealed class ExtensionContext
{
    /// <summary>Logger scoped to the extension.</summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Per-extension data directory for storing persistent state.
    /// Created automatically. Path: %APPDATA%/WinClip/extensions/{id}/data/
    /// </summary>
    public string DataDirectory { get; }

    /// <summary>Application configuration (read-only).</summary>
    public IConfiguration Configuration { get; }

    public ExtensionContext(ILogger logger, string dataDirectory, IConfiguration configuration)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        DataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
}
