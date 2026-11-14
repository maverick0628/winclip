using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinClip.Models;

/// <summary>
/// Main application settings container that holds all category settings.
/// Implements INotifyPropertyChanged for data binding support.
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    private GeneralSettings _generalSettings = new();
    private ActionSettings _actionSettings = new();
    private HotkeySettings _hotkeySettings = new();
    private AppearanceSettings _appearanceSettings = new();

    /// <summary>
    /// Gets or sets the general application settings.
    /// </summary>
    public GeneralSettings GeneralSettings
    {
        get => _generalSettings;
        set
        {
            if (_generalSettings != value)
            {
                _generalSettings = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the action settings.
    /// </summary>
    public ActionSettings ActionSettings
    {
        get => _actionSettings;
        set
        {
            if (_actionSettings != value)
            {
                _actionSettings = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the hotkey settings.
    /// </summary>
    public HotkeySettings HotkeySettings
    {
        get => _hotkeySettings;
        set
        {
            if (_hotkeySettings != value)
            {
                _hotkeySettings = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the appearance settings.
    /// </summary>
    public AppearanceSettings AppearanceSettings
    {
        get => _appearanceSettings;
        set
        {
            if (_appearanceSettings != value)
            {
                _appearanceSettings = value;
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

    /// <summary>
    /// Creates a deep copy of the current settings.
    /// </summary>
    /// <returns>A new AppSettings instance with copied values.</returns>
    public AppSettings Clone()
    {
        var cloned = new AppSettings
        {
            GeneralSettings = new GeneralSettings
            {
                StartWithWindows = GeneralSettings.StartWithWindows,
                RunAtStartup = GeneralSettings.RunAtStartup,
                ShowNotifications = GeneralSettings.ShowNotifications,
                ClipboardHistorySize = GeneralSettings.ClipboardHistorySize,
                MinimizeToTray = GeneralSettings.MinimizeToTray,
                AutoSaveInterval = GeneralSettings.AutoSaveInterval
            },
            ActionSettings = new ActionSettings()
        };

        // Deep copy action items
        foreach (var action in ActionSettings.Actions)
        {
            cloned.ActionSettings.Actions.Add(new ActionItem
            {
                Name = action.Name,
                Trigger = action.Trigger,
                Command = action.Command,
                Enabled = action.Enabled
            });
        }

        // Deep copy hotkey bindings
        cloned.HotkeySettings = new HotkeySettings();
        foreach (var hotkey in HotkeySettings.Hotkeys)
        {
            cloned.HotkeySettings.Hotkeys.Add(new HotkeyBinding
            {
                Name = hotkey.Name,
                Command = hotkey.Command,
                Key = hotkey.Key,
                Modifiers = hotkey.Modifiers,
                Enabled = hotkey.Enabled
            });
        }

        // Copy appearance settings
        cloned.AppearanceSettings = new AppearanceSettings
        {
            Theme = AppearanceSettings.Theme,
            AccentColor = AppearanceSettings.AccentColor,
            FontSize = AppearanceSettings.FontSize,
            WindowOpacity = AppearanceSettings.WindowOpacity
        };

        return cloned;
    }

    /// <summary>
    /// Creates a new AppSettings instance with default values.
    /// </summary>
    /// <returns>A new AppSettings instance with default settings.</returns>
    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            GeneralSettings = new GeneralSettings(),
            ActionSettings = new ActionSettings(),
            HotkeySettings = new HotkeySettings(),
            AppearanceSettings = new AppearanceSettings()
        };
    }
}
