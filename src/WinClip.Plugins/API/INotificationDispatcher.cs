namespace WinClip.Plugins.API;

/// <summary>
/// Abstraction for the host-supplied notification surface.
/// Implementations forward plugin notifications to the user-visible UI
/// (tray balloon, toast, etc.). Decouples <see cref="NotificationApiImpl"/>
/// from any specific WPF/WinForms type so it can stay in the Plugins layer.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Displays a notification to the user.
    /// Implementations must be thread-safe and non-blocking.
    /// </summary>
    void Dispatch(string title, string message, NotificationType type);
}

/// <summary>
/// No-op dispatcher for tests, headless runs, or when no UI host is available.
/// Notifications are written to <see cref="System.Diagnostics.Debug"/> only.
/// </summary>
public sealed class NullNotificationDispatcher : INotificationDispatcher
{
    /// <summary>Shared singleton instance.</summary>
    public static readonly NullNotificationDispatcher Instance = new();

    /// <inheritdoc/>
    public void Dispatch(string title, string message, NotificationType type)
    {
        System.Diagnostics.Debug.WriteLine($"[Notification:{type}] {title}: {message}");
    }
}
