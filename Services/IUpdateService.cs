namespace WinClip.Services;

/// <summary>
/// Service that checks for application updates and downloads the installer.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks whether a newer version is available.
    /// Returns the update info if available, or null if the current version is up to date.
    /// </summary>
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the installer for the given update to a temporary file.
    /// Returns the path to the downloaded installer.
    /// </summary>
    Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Launches the downloaded installer and requests the application to shut down.
    /// </summary>
    void ApplyUpdate(string installerPath);
}

/// <summary>
/// Describes an available update from the remote release feed.
/// </summary>
public sealed class UpdateInfo
{
    public required string Version { get; init; }
    public required string DownloadUrl { get; init; }
    public required string ReleaseNotesUrl { get; init; }
    public string? ReleaseNotes { get; init; }
    public long? SizeBytes { get; init; }
    /// <summary>Hex SHA-256 of the installer, checked after download when present.</summary>
    public string? Sha256 { get; init; }
    public DateTime PublishedAt { get; init; }
}
