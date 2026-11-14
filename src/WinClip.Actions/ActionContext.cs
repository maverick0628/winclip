namespace WinClip.Actions;

/// <summary>
/// Provides context information for action execution.
/// Contains metadata about where the text was selected from.
/// </summary>
public class ActionContext
{
    /// <summary>
    /// Gets or sets the name of the source application where the text was selected.
    /// </summary>
    public string? SourceApp { get; set; }

    /// <summary>
    /// Gets or sets the source URL if the text was selected from a web browser.
    /// </summary>
    public string? SourceUrl { get; set; }
}
