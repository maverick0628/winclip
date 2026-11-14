namespace WinClip.Models;

/// <summary>
/// Represents a clipboard history item stored in the database.
/// Contains the clipboard content, timestamp, and pin status.
/// </summary>
public sealed class ClipboardItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardItem"/> class.
    /// </summary>
    /// <param name="content">The clipboard text content.</param>
    public ClipboardItem(string content)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        CreatedAt = DateTime.UtcNow;
        IsPinned = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardItem"/> class for Entity Framework.
    /// </summary>
    /// <remarks>
    /// This constructor is required by Entity Framework Core for materialization.
    /// </remarks>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    private ClipboardItem()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Gets or sets the unique identifier for this clipboard item.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the clipboard text content.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets the UTC timestamp when this item was added to the clipboard history.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this item is pinned.
    /// Pinned items are not removed when the history exceeds the 20-item limit.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Gets a preview of the clipboard content suitable for display in the UI.
    /// Returns the first 100 characters with newlines replaced by spaces.
    /// </summary>
    public string ContentPreview
    {
        get
        {
            if (string.IsNullOrEmpty(Content))
            {
                return string.Empty;
            }

            // Replace newlines and carriage returns with spaces for single-line preview
            var preview = Content.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

            // Trim to 100 characters and add ellipsis if truncated
            const int maxLength = 100;
            if (preview.Length > maxLength)
            {
                return preview[..maxLength] + "...";
            }

            return preview;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this item has content.
    /// </summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Returns a string representation of the clipboard item.
    /// </summary>
    /// <returns>A string containing the timestamp, pin status, and content preview.</returns>
    public override string ToString()
    {
        var pinIndicator = IsPinned ? "[PINNED] " : string.Empty;
        return $"{pinIndicator}ClipboardItem (ID: {Id}) - {CreatedAt:yyyy-MM-dd HH:mm:ss.fff} UTC - {ContentPreview}";
    }
}
