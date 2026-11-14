using WinClip.Plugins.Security;

namespace WinClip.Plugins.API;

/// <summary>
/// Implementation of <see cref="IClipboardApi"/> that enforces clipboard permission checks.
/// All clipboard operations require the Clipboard permission to be granted to the plugin.
/// </summary>
public class ClipboardApiImpl : IClipboardApi
{
    private readonly PermissionType _grantedPermissions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardApiImpl"/> class.
    /// </summary>
    /// <param name="grantedPermissions">The permissions that have been granted to the plugin.</param>
    public ClipboardApiImpl(PermissionType grantedPermissions)
    {
        _grantedPermissions = grantedPermissions;
    }

    /// <inheritdoc/>
    public async Task<string?> GetTextAsync()
    {
        // Enforce permission check before accessing clipboard
        PermissionGuard.RequirePermission(_grantedPermissions, PermissionType.Clipboard, "Clipboard Access");

        // Execute clipboard operation asynchronously to avoid blocking
        return await Task.Run(() =>
        {
            try
            {
                // Check if clipboard contains text
                if (System.Windows.Forms.Clipboard.ContainsText())
                {
                    return System.Windows.Forms.Clipboard.GetText();
                }
                return null;
            }
            catch (Exception ex)
            {
                // Wrap clipboard access exceptions with more context
                throw new InvalidOperationException("Failed to access clipboard.", ex);
            }
        });
    }

    /// <inheritdoc/>
    public async Task SetTextAsync(string text)
    {
        // Enforce permission check before modifying clipboard
        PermissionGuard.RequirePermission(_grantedPermissions, PermissionType.Clipboard, "Clipboard Access");

        // Validate input
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text), "Text cannot be null.");
        }

        // Execute clipboard operation asynchronously to avoid blocking
        await Task.Run(() =>
        {
            try
            {
                System.Windows.Forms.Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                // Wrap clipboard access exceptions with more context
                throw new InvalidOperationException("Failed to set clipboard text.", ex);
            }
        });
    }

    /// <inheritdoc/>
    public async Task ClearAsync()
    {
        // Enforce permission check before clearing clipboard
        PermissionGuard.RequirePermission(_grantedPermissions, PermissionType.Clipboard, "Clipboard Access");

        // Execute clipboard operation asynchronously to avoid blocking
        await Task.Run(() =>
        {
            try
            {
                System.Windows.Forms.Clipboard.Clear();
            }
            catch (Exception ex)
            {
                // Wrap clipboard access exceptions with more context
                throw new InvalidOperationException("Failed to clear clipboard.", ex);
            }
        });
    }
}
