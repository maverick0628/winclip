using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinClip.Services;

/// <summary>
/// DPAPI-based encryption helper for sensitive settings (API keys).
///
/// Uses <see cref="DataProtectionScope.CurrentUser"/> so encrypted blobs are bound to the
/// logged-in Windows account on the current machine. If the settings.json file is copied
/// off-box or read by another user, the ciphertext is unrecoverable.
///
/// Encrypted values are prefixed with <c>"dpapi:"</c> so we can:
///   1. Tell encrypted strings apart from legacy plaintext (migration from pre-2.x builds).
///   2. Avoid double-encrypting if the same string is round-tripped.
/// </summary>
internal static class SecretProtector
{
    private const string Prefix = "dpapi:";

    // Extra entropy bound to this app, mixed into every Protect/Unprotect call.
    // This means ciphertext from WinClip can't be decrypted by other apps running as the same user.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WinClip.Settings.v1");

    /// <summary>
    /// Encrypts a plain string with DPAPI and returns a <c>"dpapi:"</c>-prefixed base64 blob.
    /// Null, empty, or already-encrypted input is returned unchanged.
    /// </summary>
    public static string? Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        // Already encrypted — don't double-wrap.
        if (plaintext.StartsWith(Prefix, StringComparison.Ordinal))
            return plaintext;

        try
        {
            if (!OperatingSystem.IsWindows())
                return plaintext; // DPAPI is Windows-only; fall back to plaintext on other OSes.

            var bytes = Encoding.UTF8.GetBytes(plaintext);
            var protectedBytes = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(protectedBytes);
        }
        catch
        {
            // If protection fails for any reason (rare), store plaintext rather than losing the value.
            return plaintext;
        }
    }

    /// <summary>
    /// Decrypts a previously-protected string. Values without the <c>"dpapi:"</c> prefix are
    /// returned unchanged so legacy plaintext settings still load (and will be re-saved
    /// encrypted on the next write).
    /// </summary>
    public static string? Unprotect(string? storedValue)
    {
        if (string.IsNullOrEmpty(storedValue))
            return storedValue;

        if (!storedValue.StartsWith(Prefix, StringComparison.Ordinal))
            return storedValue; // Legacy plaintext — return as-is.

        try
        {
            if (!OperatingSystem.IsWindows())
                return null; // Can't decrypt a DPAPI blob on a non-Windows host.

            var base64 = storedValue[Prefix.Length..];
            var protectedBytes = Convert.FromBase64String(base64);
            var bytes = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // Decryption failed — user profile migrated, machine key rotated, or tampering.
            // Returning null forces the user to re-enter the secret rather than crashing on load.
            return null;
        }
    }
}

/// <summary>
/// JsonConverter that transparently DPAPI-protects string properties on serialize and
/// unprotects on deserialize. Attach with <c>[JsonConverter(typeof(DpapiStringConverter))]</c>
/// on any <c>string?</c> property that holds a secret.
/// </summary>
internal sealed class DpapiStringConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        return SecretProtector.Unprotect(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(SecretProtector.Protect(value));
    }
}
