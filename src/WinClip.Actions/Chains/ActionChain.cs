namespace WinClip.Actions.Chains;

/// <summary>
/// User-defined named sequence of action IDs applied in order to selected text.
/// Each step's clipboard output feeds the next step's input.
/// </summary>
public sealed class ActionChain
{
    /// <summary>Display name shown in the popup flyout.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description shown as the action tooltip.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Names (matching <see cref="IAction.Name"/>) of the actions to run in order.
    /// First action sees the selected text; subsequent actions see the previous step's clipboard output.
    /// </summary>
    public List<string> ActionIds { get; set; } = new();
}
