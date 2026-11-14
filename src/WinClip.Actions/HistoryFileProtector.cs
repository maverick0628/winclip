using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace WinClip.Actions;

/// <summary>
/// DPAPI-backed file encryption for clipboard-history and pinned-history JSON files.
///
/// Uses <see cref="DataProtectionScope.CurrentUser"/> so the ciphertext is bound to the
/// logged-in Windows account: copying clipboard-history.json to another user/machine
/// renders it unreadable.
///
/// File format on disk:
///   - First line: literal magic header <c>WCEH1</c> (WinClip Encrypted History v1)
///   - Remainder: base64-encoded DPAPI ciphertext
///
/// Files without the magic header are treated as legacy plaintext JSON and read as-is;
/// they get rewritten encrypted on the next save (transparent upgrade).
/// </summary>
internal static class HistoryFileProtector
{
    private const string MagicHeader = "WCEH1\n";
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WinClip.History.v1");

    /// <summary>
    /// Reads and decrypts the file. Returns null if the file does not exist.
    /// Returns plaintext unchanged for legacy (unencrypted) files. Returns null if
    /// decryption fails (e.g., copied between users) so callers can fall back to empty.
    /// </summary>
    public static string? ReadOrNull(string path)
    {
        if (!File.Exists(path)) return null;

        try
        {
            var contents = File.ReadAllText(path);
            if (string.IsNullOrEmpty(contents)) return contents;

            if (!contents.StartsWith(MagicHeader, StringComparison.Ordinal))
                return contents; // legacy plaintext — return as-is

            if (!OperatingSystem.IsWindows()) return null;

            var base64 = contents[MagicHeader.Length..];
            var bytes = Convert.FromBase64String(base64);
            return UnprotectWindows(bytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Encrypts <paramref name="plainJson"/> and writes it atomically.
    /// On non-Windows hosts (CI, tests on Linux/Mac) writes plaintext as a fallback.
    /// </summary>
    public static void Write(string path, string plainJson)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        if (!OperatingSystem.IsWindows())
        {
            File.WriteAllText(path, plainJson);
            return;
        }

        var protectedBytes = ProtectWindows(Encoding.UTF8.GetBytes(plainJson));
        var output = MagicHeader + Convert.ToBase64String(protectedBytes);

        // Atomic write: temp file + Move-with-overwrite so a crash mid-write
        // never leaves the user with a half-encrypted history file.
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, output);
        File.Move(tempPath, path, overwrite: true);
    }

    [SupportedOSPlatform("windows")]
    private static byte[] ProtectWindows(byte[] data) =>
        ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);

    [SupportedOSPlatform("windows")]
    private static string UnprotectWindows(byte[] data) =>
        Encoding.UTF8.GetString(ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser));
}
