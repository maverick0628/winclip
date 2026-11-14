using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WinClip.Data.Repositories
{
    /// <summary>
    /// Entity Framework implementation of IClipboardRepository with pagination support
    /// </summary>
    public class ClipboardRepository : IClipboardRepository
    {
        // Configuration constants for pagination
        private const int DEFAULT_PAGE_SIZE = 50;
        private const int MAX_PAGE_SIZE = 200;
        private const int MIN_PAGE_SIZE = 1;
        private const int DEFAULT_PAGE_NUMBER = 1;

        private readonly ClipboardDbContext _context;

        public ClipboardRepository(ClipboardDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves all clipboard entries with pagination, ordered by timestamp descending
        /// </summary>
        public async Task<IEnumerable<ClipboardEntry>> GetAllAsync(int pageNumber = DEFAULT_PAGE_NUMBER, int pageSize = DEFAULT_PAGE_SIZE)
        {
            // Validate pagination parameters
            ValidatePaginationParameters(pageNumber, pageSize);

            return await _context.ClipboardEntries
                .OrderByDescending(e => e.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Searches clipboard entries by text with pagination
        /// </summary>
        public async Task<IEnumerable<ClipboardEntry>> SearchAsync(string searchText, int pageNumber = DEFAULT_PAGE_NUMBER, int pageSize = DEFAULT_PAGE_SIZE)
        {
            // Validate pagination parameters
            ValidatePaginationParameters(pageNumber, pageSize);

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return await GetAllAsync(pageNumber, pageSize);
            }

            return await _context.ClipboardEntries
                .Where(e => e.Content.Contains(searchText))
                .OrderByDescending(e => e.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Adds a new clipboard entry
        /// </summary>
        public async Task<ClipboardEntry> AddAsync(ClipboardEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            _context.ClipboardEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        /// <summary>
        /// Gets a clipboard entry by ID
        /// </summary>
        public async Task<ClipboardEntry> GetByIdAsync(int id)
        {
            return await _context.ClipboardEntries.FindAsync(id);
        }

        /// <summary>
        /// Deletes a clipboard entry
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var entry = await _context.ClipboardEntries.FindAsync(id);
            if (entry == null)
            {
                return false;
            }

            _context.ClipboardEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Deletes all clipboard entries
        /// </summary>
        public async Task<int> DeleteAllAsync()
        {
            var entries = await _context.ClipboardEntries.ToListAsync();
            var count = entries.Count;
            _context.ClipboardEntries.RemoveRange(entries);
            await _context.SaveChangesAsync();
            return count;
        }

        /// <summary>
        /// Gets the total count of clipboard entries
        /// </summary>
        public async Task<int> GetCountAsync()
        {
            return await _context.ClipboardEntries.CountAsync();
        }

        /// <summary>
        /// Validates pagination parameters
        /// </summary>
        private void ValidatePaginationParameters(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");
            }

            if (pageSize < MIN_PAGE_SIZE)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"Page size must be at least {MIN_PAGE_SIZE}.");
            }

            if (pageSize > MAX_PAGE_SIZE)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"Page size cannot exceed {MAX_PAGE_SIZE}.");
            }
        }
    }

    /// <summary>
    /// Entity Framework DbContext for clipboard data
    /// </summary>
    public class ClipboardDbContext : DbContext
    {
        public ClipboardDbContext(DbContextOptions<ClipboardDbContext> options)
            : base(options)
        {
        }

        public DbSet<ClipboardEntry> ClipboardEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ClipboardEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.ContentType).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.HasIndex(e => e.Timestamp);
            });
        }
    }
}
