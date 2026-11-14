namespace WinClip.Services;

/// <summary>
/// Maps each <see cref="ActionProfile"/> to its default set of enabled action names.
/// Action names must match the <c>IAction.Name</c> values registered in Program.cs.
/// </summary>
public static class ActionProfilePresets
{
    /// <summary>
    /// Returns the default enabled action names for a profile.
    /// For <see cref="ActionProfile.All"/> and <see cref="ActionProfile.Custom"/>,
    /// returns an empty list — the caller should handle these cases.
    /// </summary>
    public static IReadOnlyList<string> GetPresetActions(ActionProfile profile) => profile switch
    {
        ActionProfile.Essentials => Essentials,
        ActionProfile.Developer => Developer,
        ActionProfile.Writer => Writer,
        _ => Essentials,
    };

    private static readonly List<string> Essentials = new()
    {
        "Copy",
        "UPPERCASE",
        "lowercase",
        "Title Case",
        "Trim",
        "Web Search",
        "Word Count",
        "Define",
        "Clipboard History",
        "Save Snippet",
    };

    private static readonly List<string> Developer = new()
    {
        // Essentials
        "Copy", "UPPERCASE", "lowercase", "Title Case", "Trim",
        "Web Search", "Word Count", "Define",
        // Case transforms
        "PascalCase", "camelCase", "snake_case",
        // Encoding & URLs
        "URL Encode", "URL Decode", "Open URL",
        "Base64 Encode", "Base64 Decode",
        // Data formats
        "JSON Prettify", "JSON Minify",
        "CSV \u2194 JSON", "Markdown Table",
        // Lines
        "Sort Lines A\u2192Z", "Sort Lines Z\u2192A",
        "Remove Duplicate Lines", "Reverse Lines",
        "Add Line Numbers", "Remove Blank Lines",
        // Hashing & conversion
        "Hash MD5", "Hash SHA1", "Hash SHA256",
        "Timestamp Convert", "Number Base Convert", "Color Convert",
        // Text processing
        "Strip HTML", "Slugify", "Regex Replace",
        "Smart Extract",
        // Productivity
        "Clipboard History", "Snippet Manager",
        // AI (visible only when configured)
        "Check Grammar", "Summarize", "Rephrase", "Expand", "Custom Prompt",
        // Integration
        "Send to Blip",
    };

    private static readonly List<string> Writer = new()
    {
        // Essentials
        "Copy", "UPPERCASE", "lowercase", "Title Case", "Trim",
        "Web Search", "Word Count", "Define",
        // Writing tools
        "Text Statistics", "Strip Markdown",
        "Sort Lines A\u2192Z", "Remove Duplicate Lines",
        "Wrap Lines (80 cols)", "Unwrap to Single Line",
        "Remove Blank Lines",
        // Sharing & output
        "Translate", "Email", "QR Code",
        "Share to Twitter/X", "Smart Extract",
        // Productivity
        "Clipboard History", "Snippet Manager",
        // AI (visible only when configured)
        "Check Grammar", "Summarize", "Rephrase", "Expand", "Custom Prompt",
    };
}
