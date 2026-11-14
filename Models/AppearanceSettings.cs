using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinClip.Models;

/// <summary>
/// Appearance settings for the application including theme, colors, and visual preferences.
/// Implements INotifyPropertyChanged for data binding support.
/// </summary>
public class AppearanceSettings : INotifyPropertyChanged
{
    private string _theme = "Light";
    private string _accentColor = "#0078D4";
    private double _fontSize = 12.0;
    private double _windowOpacity = 1.0;

    /// <summary>
    /// Gets or sets the application theme.
    /// Valid values: "Light", "Dark", "Auto"
    /// </summary>
    public string Theme
    {
        get => _theme;
        set
        {
            if (_theme != value)
            {
                _theme = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the accent color in hex format.
    /// Example: "#0078D4" for default Windows blue
    /// </summary>
    public string AccentColor
    {
        get => _accentColor;
        set
        {
            if (_accentColor != value)
            {
                _accentColor = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the base font size for the application.
    /// Valid range: 8.0-24.0
    /// </summary>
    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize != value)
            {
                _fontSize = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the window opacity level.
    /// Valid range: 0.5-1.0 (50%-100%)
    /// </summary>
    public double WindowOpacity
    {
        get => _windowOpacity;
        set
        {
            if (_windowOpacity != value)
            {
                _windowOpacity = value;
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
