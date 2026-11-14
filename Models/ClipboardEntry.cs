using System;

namespace WinClip.Models
{
    /// <summary>
    /// Represents a clipboard history entry
    /// </summary>
    public class ClipboardEntry
    {
        /// <summary>
        /// Unique identifier for the clipboard entry
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The text content of the clipboard entry
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the entry was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// SHA256 hash of the content for deduplication
        /// </summary>
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether this entry is marked as a favorite
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// The type of data stored in this clipboard entry
        /// </summary>
        public ClipboardDataType DataType { get; set; }

        public ClipboardEntry()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            IsFavorite = false;
            DataType = ClipboardDataType.Text;
        }
    }

    /// <summary>
    /// Enumeration of supported clipboard data types
    /// </summary>
    public enum ClipboardDataType
    {
        /// <summary>
        /// Plain text content
        /// </summary>
        Text = 0,

        /// <summary>
        /// Image content
        /// </summary>
        Image = 1,

        /// <summary>
        /// File content
        /// </summary>
        File = 2
    }
}
