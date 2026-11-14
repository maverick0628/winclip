using System.Reflection;

namespace WinClip;

/// <summary>
/// Provides application-level constants for version, product name, and update configuration.
/// Version is read from the assembly metadata set in WinClip.csproj.
/// </summary>
public static class AppInfo
{
    public static readonly string Version =
        typeof(AppInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "1.0.0";

    /// <summary>
    /// Human-readable version without the git hash suffix (e.g. "1.0.0" instead of "1.0.0+bff63ed...").
    /// </summary>
    public static readonly string DisplayVersion =
        Version.IndexOf('+') is var i and >= 0 ? Version[..i] : Version;

    public const string ProductName = "WinClip";

    // The update feed every installed copy polls. It is served from Cloudflare R2 rather
    // than the GitHub releases API. release.yml reads this line to decide where it publishes, so change the URL here
    // and nowhere else. R2 setup notes are at the bottom of .github/workflows/release.yml.
    public const string UpdateUrl = "https://winclip-updates.duncansmith.tech/latest.json";

    // Public page users can open for downloads and release notes.
    public const string ReleasesPage = "https://duncansmith.tech/apps/winclip/";

    /// <summary>
    /// Parses the current version as a <see cref="System.Version"/> for comparison.
    /// Strips any SemVer suffix (e.g. "-beta") before parsing.
    /// </summary>
    public static System.Version GetCurrentVersion()
    {
        var ver = Version;
        var plusIdx = ver.IndexOf('+');
        if (plusIdx >= 0) ver = ver[..plusIdx];
        var dashIdx = ver.IndexOf('-');
        if (dashIdx >= 0) ver = ver[..dashIdx];
        return System.Version.TryParse(ver, out var parsed) ? parsed : new System.Version(1, 0, 0);
    }
}
