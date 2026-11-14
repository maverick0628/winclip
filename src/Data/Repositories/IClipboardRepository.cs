using System.Collections.Generic;
using System.Threading.Tasks;

namespace WinClip.Data.Repositories
{
    /// <summary>
    /// Repository interface for clipboard entry operations
    /// </summary>
    public interface IClipboardRepository
    {
        /// <summary>
        /// Retrieves all clipboard entries with optional pagination
        /// </summary>
        /// <param name="pageNumber">Page number (1-based). Default is 1.</param>
        /// <param name="pageSize">Number of items per page. Default is 50.</param>
        /// <returns>Collection of clipboard entries for the specified page</returns>
        Task<IEnumerable<ClipboardEntry>> GetAllAsync(int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Searches clipboard entries by text with optional pagination
        /// </summary>
        /// <param name="searchText">Text to search for</param>
        /// <param name="pageNumber">Page number (1-based). Default is 1.</param>
        /// <param name="pageSize">Number of items per page. Default is 50.</param>
        /// <returns>Collection of matching clipboard entries for the specified page</returns>
        Task<IEnumerable<ClipboardEntry>> SearchAsync(string searchText, int pageNumber = 1, int pageSize = 50);

        /// <summary>
        /// Adds a new clipboard entry
        /// </summary>
        /// <param name="entry">The clipboard entry to add</param>
        /// <returns>The added clipboard entry</returns>
        Task<ClipboardEntry> AddAsync(ClipboardEntry entry);

        /// <summary>
        /// Gets a clipboard entry by ID
        /// </summary>
        /// <param name="id">The entry ID</param>
        /// <returns>The clipboard entry if found, null otherwise</returns>
        Task<ClipboardEntry> GetByIdAsync(int id);

        /// <summary>
        /// Deletes a clipboard entry
        /// </summary>
        /// <param name="id">The entry ID to delete</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Deletes all clipboard entries
        /// </summary>
        /// <returns>Number of entries deleted</returns>
        Task<int> DeleteAllAsync();

        /// <summary>
        /// Gets the total count of clipboard entries
        /// </summary>
        /// <returns>Total number of entries</returns>
        Task<int> GetCountAsync();
    }

    /// <summary>
    /// Represents a clipboard entry
    /// </summary>
    public class ClipboardEntry
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
        public System.DateTime Timestamp { get; set; }
        public string Source { get; set; }
        public bool IsSensitive { get; set; }
    }
}
