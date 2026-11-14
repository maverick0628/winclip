namespace WinClip.Actions;

/// <summary>
/// Defines the contract for all WinClip actions.
/// Actions are operations that can be performed on selected text.
/// </summary>
public interface IAction
{
    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    string IconName { get; }

    /// <summary>
    /// Determines whether the action is available for use.
    /// This allows actions to be conditionally shown based on runtime conditions.
    /// </summary>
    /// <returns>True if the action is available, false otherwise.</returns>
    bool IsAvailable();

    /// <summary>
    /// Executes the action asynchronously on the selected text.
    /// </summary>
    /// <param name="selectedText">The text that was selected by the user.</param>
    /// <param name="context">Optional context information about where the text was selected from.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the action execution.</returns>
    Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null);
}
