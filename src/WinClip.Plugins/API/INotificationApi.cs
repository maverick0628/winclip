namespace WinClip.Plugins.API;

/// <summary>
/// Provides notification display functionality to plugins.
/// Requires Notifications permission to use.
/// </summary>
public interface INotificationApi
{
    /// <summary>
    /// Displays a notification to the user.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The message content of the notification.</param>
    /// <param name="type">The type of notification (info, warning, error).</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the plugin lacks Notifications permission.</exception>
    Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Info);
}

/// <summary>
/// Defines the types of notifications that can be displayed.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Informational notification.
    /// </summary>
    Info,

    /// <summary>
    /// Warning notification.
    /// </summary>
    Warning,

    /// <summary>
    /// Error notification.
    /// </summary>
    Error,

    /// <summary>
    /// Success notification.
    /// </summary>
    Success
}
