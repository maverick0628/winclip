namespace WinClip.Extensions;

/// <summary>
/// Contract for a WinClip extension.
/// Extensions are loaded from plugin directories and provide one or more IAction implementations.
/// </summary>
public interface IExtension : IDisposable
{
    /// <summary>
    /// Unique identifier for this extension (reverse-domain, e.g. "com.winclip.texttransform").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Called once after the extension assembly is loaded.
    /// Use this to set up state, read configuration, etc.
    /// </summary>
    Task InitializeAsync(ExtensionContext context);
}
