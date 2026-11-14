using System;

namespace WinClip.Services;

/// <summary>
/// Service interface for managing the Windows system tray icon.
/// Provides tray icon presence, status indication, and context menu for application control.
/// </summary>
public interface ITrayIconService : IDisposable
{
    /// <summary>
    /// Initializes the system tray icon and displays it in the Windows taskbar notification area.
    /// Sets up the context menu, tooltip, and event handlers for user interaction.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the tray icon is already initialized.</exception>
    void Initialize();

    /// <summary>
    /// Updates the tray icon to reflect the current application status.
    /// Changes the icon visual state between enabled and disabled representations.
    /// </summary>
    /// <param name="enabled">True to show the enabled icon state; false to show the disabled icon state.</param>
    void UpdateStatus(bool enabled);
}
