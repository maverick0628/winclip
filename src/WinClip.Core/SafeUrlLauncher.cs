using System.Diagnostics;

namespace WinClip.Core;

/// <summary>
/// Opens URLs with strict scheme validation to prevent unsafe protocol execution.
/// </summary>
public static class SafeUrlLauncher
{
    public static bool IsSafeHttpUrl(string? url, out string? normalizedUrl)
    {
        normalizedUrl = null;
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        normalizedUrl = uri.AbsoluteUri;
        return true;
    }

    public static bool TryOpenHttpUrl(string? url)
    {
        if (!IsSafeHttpUrl(url, out var normalizedUrl) || normalizedUrl == null)
            return false;

        Process.Start(new ProcessStartInfo
        {
            FileName = normalizedUrl,
            UseShellExecute = true
        });

        return true;
    }
}
