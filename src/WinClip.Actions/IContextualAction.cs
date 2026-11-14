namespace WinClip.Actions;

/// <summary>
/// Optional interface for actions that should only appear in the popup
/// when the selected text matches certain criteria.
/// Actions that do NOT implement this interface are always shown.
/// </summary>
public interface IContextualAction
{
    /// <summary>
    /// Returns true if this action is relevant for the given selected text.
    /// The popup uses this to filter out irrelevant actions.
    /// </summary>
    bool IsRelevantFor(string selectedText);
}
