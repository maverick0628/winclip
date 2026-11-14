using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace WinClip.Models;

/// <summary>
/// Represents a hotkey binding that maps a key combination to a command.
/// Implements INotifyPropertyChanged for data binding support.
/// </summary>
public class HotkeyBinding : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _command = string.Empty;
    private Key _key = Key.None;
    private ModifierKeys _modifiers = ModifierKeys.None;
    private bool _enabled = true;

    /// <summary>
    /// Gets or sets the display name of the hotkey binding.
    /// </summary>
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the command to execute when the hotkey is pressed.
    /// </summary>
    public string Command
    {
        get => _command;
        set
        {
            if (_command != value)
            {
                _command = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the key for the hotkey combination.
    /// </summary>
    public Key Key
    {
        get => _key;
        set
        {
            if (_key != value)
            {
                _key = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    /// <summary>
    /// Gets or sets the modifier keys (Ctrl, Alt, Shift, Windows) for the hotkey combination.
    /// </summary>
    public ModifierKeys Modifiers
    {
        get => _modifiers;
        set
        {
            if (_modifiers != value)
            {
                _modifiers = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the hotkey binding is enabled.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the display text for the hotkey combination (e.g., "Ctrl+Shift+C").
    /// </summary>
    public string DisplayText
    {
        get
        {
            if (Key == Key.None)
                return string.Empty;

            var parts = new List<string>();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");
            if (Modifiers.HasFlag(ModifierKeys.Windows))
                parts.Add("Win");

            parts.Add(Key.ToString());

            return string.Join("+", parts);
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
