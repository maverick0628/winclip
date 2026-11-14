using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinClip.Models;

/// <summary>
/// Represents a single action that can be triggered by clipboard events or hotkeys.
/// Implements INotifyPropertyChanged for data binding support.
/// </summary>
public class ActionItem : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _trigger = string.Empty;
    private string _command = string.Empty;
    private bool _enabled = true;

    /// <summary>
    /// Gets or sets the name of the action.
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
    /// Gets or sets the trigger type for the action (e.g., "ClipboardCopy", "ClipboardPaste", "TextPattern").
    /// </summary>
    public string Trigger
    {
        get => _trigger;
        set
        {
            if (_trigger != value)
            {
                _trigger = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the command to execute when the action is triggered.
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
    /// Gets or sets whether the action is enabled.
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
