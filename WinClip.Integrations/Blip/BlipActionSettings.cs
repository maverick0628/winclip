namespace WinClip.Integrations.Blip;

/// <summary>
/// Configuration settings for the Blip integration.
/// Controls what metadata is included when sending notes to Blip.
/// </summary>
public class BlipActionSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to include the source application name
    /// when sending notes to Blip.
    /// </summary>
    /// <value>
    /// True to include the source application name in note requests; otherwise, false.
    /// Default is true.
    /// </value>
    public bool IncludeSourceApp { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include the source URL
    /// when sending notes to Blip from a web browser.
    /// </summary>
    /// <value>
    /// True to include the source URL in note requests; otherwise, false.
    /// Default is true.
    /// </value>
    public bool IncludeSourceUrl { get; set; } = true;
}
