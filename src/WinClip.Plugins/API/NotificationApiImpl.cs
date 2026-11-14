using WinClip.Plugins.Security;

namespace WinClip.Plugins.API;

/// <summary>
/// Implementation of <see cref="INotificationApi"/> that enforces notification permission
/// checks and forwards approved notifications to a host-supplied
/// <see cref="INotificationDispatcher"/>.
/// </summary>
public class NotificationApiImpl : INotificationApi
{
    private readonly PermissionType _grantedPermissions;
    private readonly INotificationDispatcher _dispatcher;

    /// <summary>
    /// Initializes the API with no UI surface — notifications are silently dispatched to
    /// <see cref="NullNotificationDispatcher"/>. Use the two-argument overload in production.
    /// </summary>
    public NotificationApiImpl(PermissionType grantedPermissions)
        : this(grantedPermissions, NullNotificationDispatcher.Instance)
    {
    }

    /// <summary>
    /// Initializes the API with the given permissions and a host-provided dispatcher.
    /// </summary>
    public NotificationApiImpl(PermissionType grantedPermissions, INotificationDispatcher dispatcher)
    {
        _grantedPermissions = grantedPermissions;
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc/>
    public Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Info)
    {
        PermissionGuard.RequirePermission(_grantedPermissions, PermissionType.Notifications, "Notification Display");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentNullException(nameof(title), "Notification title cannot be null or empty.");
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message), "Notification message cannot be null or empty.");
        if (!Enum.IsDefined(typeof(NotificationType), type))
            throw new ArgumentException($"Invalid notification type: {type}", nameof(type));

        try
        {
            _dispatcher.Dispatch(title, message, type);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(new InvalidOperationException(
                $"Failed to display notification with title '{title}'.", ex));
        }
    }
}
