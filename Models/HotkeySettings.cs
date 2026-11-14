using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WinClip.Models;

/// <summary>
/// Settings for global hotkey bindings that trigger commands.
/// Implements INotifyPropertyChanged for data binding support.
/// </summary>
public class HotkeySettings : INotifyPropertyChanged
{
    private ObservableCollection<HotkeyBinding> _hotkeys = new();

    /// <summary>
    /// Gets or sets the collection of hotkey bindings.
    /// </summary>
    public ObservableCollection<HotkeyBinding> Hotkeys
    {
        get => _hotkeys;
        set
        {
            if (_hotkeys != value)
            {
                _hotkeys = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the HotkeySettings class with default hotkeys.
    /// </summary>
    public HotkeySettings()
    {
        // Start empty — user configures hotkeys in settings
        Hotkeys = new ObservableCollection<HotkeyBinding>();
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
