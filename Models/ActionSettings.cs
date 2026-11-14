using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinClip.Models;

/// <summary>
/// Settings for custom actions that can be triggered by clipboard events or patterns.
/// Implements INotifyPropertyChanged for data binding support.
/// </summary>
public class ActionSettings : INotifyPropertyChanged
{
    private ObservableCollection<ActionItem> _actions = new();

    /// <summary>
    /// Gets or sets the collection of custom actions.
    /// </summary>
    public ObservableCollection<ActionItem> Actions
    {
        get => _actions;
        set
        {
            if (_actions != value)
            {
                _actions = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the ActionSettings class with default actions.
    /// </summary>
    public ActionSettings()
    {
        // Initialize with default actions as examples
        Actions = new ObservableCollection<ActionItem>
        {
            new ActionItem
            {
                Name = "Open URL in Browser",
                Trigger = "TextPattern",
                Command = "https?://.*",
                Enabled = true
            },
            new ActionItem
            {
                Name = "Format Phone Number",
                Trigger = "TextPattern",
                Command = @"\d{3}-\d{3}-\d{4}",
                Enabled = true
            }
        };
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
