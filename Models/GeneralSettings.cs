using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinClip.Models;

/// <summary>
/// General application settings including startup behavior, notifications, and clipboard configuration.
/// Implements INotifyPropertyChanged for data binding support.
/// </summary>
public class GeneralSettings : INotifyPropertyChanged
{
    private bool _startWithWindows = false;
    private bool _runAtStartup = false;
    private bool _showNotifications = true;
    private int _clipboardHistorySize = 100;
    private bool _minimizeToTray = true;
    private int _autoSaveInterval = 30;

    /// <summary>
    /// Gets or sets whether the application should start with Windows.
    /// </summary>
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (_startWithWindows != value)
            {
                _startWithWindows = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the application should run at startup.
    /// </summary>
    public bool RunAtStartup
    {
        get => _runAtStartup;
        set
        {
            if (_runAtStartup != value)
            {
                _runAtStartup = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to show system notifications.
    /// </summary>
    public bool ShowNotifications
    {
        get => _showNotifications;
        set
        {
            if (_showNotifications != value)
            {
                _showNotifications = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of items to keep in clipboard history.
    /// Valid range: 10-1000
    /// </summary>
    public int ClipboardHistorySize
    {
        get => _clipboardHistorySize;
        set
        {
            if (_clipboardHistorySize != value)
            {
                _clipboardHistorySize = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the application should minimize to system tray instead of taskbar.
    /// </summary>
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set
        {
            if (_minimizeToTray != value)
            {
                _minimizeToTray = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the auto-save interval in seconds.
    /// Valid range: 10-300 (10 seconds to 5 minutes)
    /// </summary>
    public int AutoSaveInterval
    {
        get => _autoSaveInterval;
        set
        {
            if (_autoSaveInterval != value)
            {
                _autoSaveInterval = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Event raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
