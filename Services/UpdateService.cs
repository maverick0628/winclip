using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WinClip.Native;

namespace WinClip.Services;

/// <summary>
/// Checks the update feed on Cloudflare R2 (<see cref="AppInfo.UpdateUrl"/>), downloads the
/// installer, and launches it. Designed to work with the Inno Setup installer produced by
/// <c>release.yml</c>, which publishes the installers and the feed together.
/// </summary>
/// <remarks>
/// Every failure to check is quiet: an unreachable host, a 404 before the first feed is
/// published, or a feed this build cannot read all return null and log, never throw.
/// </remarks>
public sealed class UpdateService : IUpdateService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateService> _logger;
    private readonly string _updateUrl;
    private readonly bool _autoCheckEnabled;
    private readonly Func<bool> _isPackaged;

    /// <param name="httpClient">
    /// Defaults to a new client. Tests pass one backed by a fake handler.
    /// </param>
    /// <param name="isPackaged">
    /// Defaults to asking Windows whether the process has package identity.
    /// </param>
    public UpdateService(
        ILogger<UpdateService> logger,
        IConfiguration configuration,
        HttpClient? httpClient = null,
        Func<bool>? isPackaged = null)
    {
        _logger = logger;
        _isPackaged = isPackaged ?? HasPackageIdentity;

#if DEBUG
        // Update:FeedUrl lets a Debug build poll a test feed.
        var configuredUrl = configuration["Update:FeedUrl"];
        _updateUrl = string.IsNullOrWhiteSpace(configuredUrl) ? AppInfo.UpdateUrl : configuredUrl;
#else
        // Release builds poll the compiled-in feed and never read Update:FeedUrl.
        // appsettings.json, environment variables and command-line args all feed
        // IConfiguration, so none of them can point an installed copy elsewhere.
        _updateUrl = AppInfo.UpdateUrl;
#endif

        var autoCheckStr = configuration["Update:AutoCheck"];
        _autoCheckEnabled = string.IsNullOrEmpty(autoCheckStr) || !bool.TryParse(autoCheckStr, out var parsed) || parsed;

        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(AppInfo.ProductName, AppInfo.DisplayVersion));
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public bool AutoCheckEnabled => _autoCheckEnabled;

    internal string FeedUrl => _updateUrl;

    /// <summary>True only in Debug builds, the one place Update:FeedUrl is read.</summary>
    internal static bool FeedUrlOverrideAllowed =>
#if DEBUG
        true;
#else
        false;
#endif

    /// <inheritdoc />
    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        // Store and MSIX installs are updated by Windows. The feed only offers the Inno
        // Setup installer, which would install a second copy beside the packaged one.
        if (_isPackaged())
        {
            _logger.LogInformation("Packaged install, so Windows handles updates. Skipping the update feed");
            return null;
        }

        _logger.LogInformation("Checking for updates at {Url}", _updateUrl);

        try
        {
            using var response = await _httpClient.GetAsync(_updateUrl, cancellationToken);

            // 404 is the expected answer until the first release publishes latest.json,
            // so it is not worth more than a debug line on every check.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Update feed returned 404 (nothing published yet)");
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Update feed returned HTTP {Status}", (int)response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = UpdateFeed.Evaluate(json, AppInfo.GetCurrentVersion(), UpdateFeed.CurrentArchitecture);

            switch (result.Status)
            {
                case UpdateFeedStatus.UpdateAvailable:
                    _logger.LogInformation("Update feed: {Detail}", result.Detail);
                    return result.Update;
                case UpdateFeedStatus.UpToDate:
                    _logger.LogInformation("Update feed: {Detail}", result.Detail);
                    return null;
                default:
                    _logger.LogWarning("Ignoring update feed ({Status}): {Detail}", result.Status, result.Detail);
                    return null;
            }
        }
        catch (OperationCanceledException)
        {
            // Covers both a caller cancelling and HttpClient's own timeout.
            _logger.LogDebug("Update check cancelled or timed out");
            return null;
        }
        catch (HttpRequestException ex)
        {
            // DNS failure, refused connection, TLS error, offline.
            _logger.LogWarning("Could not reach the update feed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error checking for updates");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading update {Version} from {Url}", update.Version, update.DownloadUrl);

        var tempDir = Path.Combine(Path.GetTempPath(), "WinClip", "updates");
        Directory.CreateDirectory(tempDir);

        var safeVersion = SanitizeVersionForFileName(update.Version);
        var fileName = Path.GetFileName($"WinClipSetup-{safeVersion}.exe");
        var filePath = Path.GetFullPath(Path.Combine(tempDir, fileName));
        var expectedBaseDir = Path.GetFullPath(tempDir + Path.DirectorySeparatorChar);
        if (!filePath.StartsWith(expectedBaseDir, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Computed update path is outside the updates directory.");

        // If already downloaded (e.g. retry), skip
        if (File.Exists(filePath))
        {
            var existingLength = new FileInfo(filePath).Length;
            if (update.SizeBytes.HasValue && existingLength == update.SizeBytes.Value &&
                (string.IsNullOrEmpty(update.Sha256) || FileMatchesSha256(filePath, update.Sha256)))
            {
                _logger.LogInformation("Installer already downloaded at {Path}", filePath);
                progress?.Report(1.0);
                return filePath;
            }

            File.Delete(filePath);
        }

        using var response = await _httpClient.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? update.SizeBytes ?? -1;

        long bytesRead = 0;
        await using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
        {
            var buffer = new byte[81920];
            int read;

            while ((read = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                bytesRead += read;

                if (totalBytes > 0)
                    progress?.Report((double)bytesRead / totalBytes);
            }
        }

        // The feed and the installers are separate objects on R2. A hash mismatch means a
        // partial upload, a stale edge copy or tampering, and none of those should launch.
        if (!string.IsNullOrEmpty(update.Sha256) && !FileMatchesSha256(filePath, update.Sha256))
        {
            File.Delete(filePath);
            throw new InvalidOperationException(
                $"Downloaded installer for {update.Version} does not match the SHA-256 in the update feed.");
        }

        progress?.Report(1.0);
        _logger.LogInformation("Update downloaded to {Path} ({Bytes} bytes)", filePath, bytesRead);
        return filePath;
    }

    /// <inheritdoc />
    public void ApplyUpdate(string installerPath)
    {
        if (string.IsNullOrWhiteSpace(installerPath))
            throw new ArgumentException("Installer path cannot be empty.", nameof(installerPath));
        if (!File.Exists(installerPath))
            throw new FileNotFoundException("Installer file not found.", installerPath);
        if (!string.Equals(Path.GetExtension(installerPath), ".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Update installer must be an .exe file.");

        if (!AuthenticodeVerifier.IsTrusted(installerPath, out var signatureReason))
            throw new InvalidOperationException($"Refusing to launch unsigned or untrusted installer: {signatureReason}");

        _logger.LogInformation("Launching installer: {Path}", installerPath);

        // /SILENT = show progress bar but no questions
        // /CLOSEAPPLICATIONS = let Inno kill WinClip if still running
        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT /CLOSEAPPLICATIONS",
            UseShellExecute = true,
        });

        // Request the WPF app to shut down so the installer can replace files
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            System.Windows.Application.Current.Shutdown();
        });
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    internal static bool HasPackageIdentity()
    {
        try
        {
            var length = 0;
            return NativeMethods.GetCurrentPackageFullName(ref length, IntPtr.Zero)
                != NativeMethods.APPMODEL_ERROR_NO_PACKAGE;
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
            return false;
        }
    }

    internal static bool FileMatchesSha256(string path, string expectedHex)
    {
        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        return string.Equals(actual, expectedHex, StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeVersionForFileName(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return "unknown";

        // Keep a conservative allowlist to avoid path traversal or shell surprises.
        var cleaned = Regex.Replace(version, "[^0-9A-Za-z._-]", "_");
        return string.IsNullOrWhiteSpace(cleaned) ? "unknown" : cleaned;
    }
}
