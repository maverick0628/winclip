using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace WinClip.Services;

internal enum UpdateFeedStatus
{
    UpdateAvailable,
    UpToDate,
    Malformed,
    UnsupportedSchema,
    NoInstallerForArchitecture,
}

internal sealed record UpdateFeedResult(UpdateFeedStatus Status, UpdateInfo? Update, string Detail);

/// <summary>
/// Reads <c>latest.json</c>, the update feed that <c>release.yml</c> publishes to Cloudflare R2
/// (written by <c>scripts/New-UpdateFeed.ps1</c>). No I/O, so the parsing and version rules
/// can be tested without a network.
/// </summary>
/// <remarks>
/// Schema version 1:
/// <code>
/// {
///   "schemaVersion": 1,
///   "product": "WinClip",
///   "version": "2.2.0",
///   "publishedAt": "2026-09-20T12:00:00.0000000Z",
///   "releaseNotesUrl": "https://duncansmith.tech/apps/winclip/",
///   "installers": {
///     "x64":   { "url": "https://.../WinClip-2.2.0-x64-Setup.exe",   "sha256": "...", "size": 6212776 },
///     "arm64": { "url": "https://.../WinClip-2.2.0-arm64-Setup.exe", "sha256": "...", "size": 4824680 }
///   }
/// }
/// </code>
/// A feed with a higher schemaVersion is ignored rather than misread, so a future format
/// change cannot make an old install act on fields it does not understand.
/// </remarks>
internal static class UpdateFeed
{
    public const int SupportedSchemaVersion = 1;

    public static string CurrentArchitecture =>
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "arm64" : "x64";

    public static UpdateFeedResult Evaluate(string json, Version currentVersion, string architecture)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            return Malformed($"feed is not valid JSON ({ex.Message})");
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return Malformed("feed root is not a JSON object");

            if (!TryGetInt64(root, "schemaVersion", out var schemaVersion))
                return Malformed("schemaVersion is missing");
            if (schemaVersion > SupportedSchemaVersion)
                return new(UpdateFeedStatus.UnsupportedSchema, null,
                    $"feed schemaVersion {schemaVersion} is newer than supported {SupportedSchemaVersion}");
            if (schemaVersion < 1)
                return Malformed($"schemaVersion {schemaVersion} is not valid");

            if (!TryGetString(root, "version", out var versionText) || !TryParseVersion(versionText, out var remoteVersion))
                return Malformed("version is missing or is not MAJOR.MINOR.PATCH");
            versionText = versionText.Trim().TrimStart('v', 'V');

            if (!IsNewer(remoteVersion, currentVersion))
                return new(UpdateFeedStatus.UpToDate, null,
                    $"current version {currentVersion} is up to date (feed: {versionText})");

            if (!root.TryGetProperty("installers", out var installers) || installers.ValueKind != JsonValueKind.Object)
                return Malformed("installers is missing");
            if (!installers.TryGetProperty(architecture, out var installer))
                return new(UpdateFeedStatus.NoInstallerForArchitecture, null,
                    $"feed version {versionText} has no {architecture} installer");
            if (installer.ValueKind != JsonValueKind.Object)
                return Malformed($"{architecture} installer is not an object");

            if (!TryGetString(installer, "url", out var url) || !IsHttpsUrl(url))
                return Malformed($"{architecture} installer url is not an absolute https URL");
            if (!TryGetString(installer, "sha256", out var sha256) || !IsSha256Hex(sha256))
                return Malformed($"{architecture} installer sha256 is not 64 hex characters");

            long? size = TryGetInt64(installer, "size", out var sizeValue) && sizeValue > 0 ? sizeValue : null;

            var releaseNotesUrl = TryGetString(root, "releaseNotesUrl", out var notesUrl) && IsHttpsUrl(notesUrl)
                ? notesUrl
                : AppInfo.ReleasesPage;
            string? releaseNotes = TryGetString(root, "releaseNotes", out var notes) ? notes : null;

            var publishedAt = TryGetString(root, "publishedAt", out var publishedText) &&
                DateTimeOffset.TryParse(publishedText, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var parsedPublishedAt)
                ? parsedPublishedAt.UtcDateTime
                : DateTime.UtcNow;

            return new(UpdateFeedStatus.UpdateAvailable, new UpdateInfo
            {
                Version = versionText,
                DownloadUrl = url,
                ReleaseNotesUrl = releaseNotesUrl,
                ReleaseNotes = releaseNotes,
                SizeBytes = size,
                Sha256 = sha256,
                PublishedAt = publishedAt,
            }, $"update available: {versionText} (current: {currentVersion})");
        }
    }

    /// <summary>
    /// Parses MAJOR.MINOR[.PATCH[.REVISION]] with an optional leading "v". Prerelease
    /// suffixes are rejected: the feed only ever advertises stable releases.
    /// </summary>
    public static bool TryParseVersion(string? raw, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(text, out var parsed))
            return false;

        version = Normalize(parsed);
        return true;
    }

    /// <summary>
    /// True when <paramref name="remote"/> is strictly newer. Missing components count as
    /// zero, so "2.1.1.0" is not newer than "2.1.1". <see cref="Version"/> alone says it is,
    /// which would offer the same update forever.
    /// </summary>
    public static bool IsNewer(Version remote, Version current) => Normalize(remote) > Normalize(current);

    private static Version Normalize(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

    private static bool TryGetString(JsonElement obj, string name, out string value)
    {
        value = string.Empty;
        if (!obj.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            return false;
        value = el.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetInt64(JsonElement obj, string name, out long value)
    {
        value = 0;
        return obj.TryGetProperty(name, out var el)
            && el.ValueKind == JsonValueKind.Number
            && el.TryGetInt64(out value);
    }

    private static bool IsHttpsUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

    private static bool IsSha256Hex(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static UpdateFeedResult Malformed(string detail) => new(UpdateFeedStatus.Malformed, null, detail);
}
