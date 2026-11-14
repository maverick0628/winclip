namespace WinClip.Integrations.Blip;

/// <summary>
/// Data transfer object for sending note creation requests to Blip.
/// Contains the note content and optional metadata about the source.
/// </summary>
public class BlipNoteRequest
{
    /// <summary>
    /// Gets or sets the text content of the note.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the source application where the text was selected.
    /// </summary>
    public string? SourceApp { get; set; }

    /// <summary>
    /// Gets or sets the source URL if the text was selected from a web browser or document.
    /// </summary>
    public string? SourceUrl { get; set; }
}
