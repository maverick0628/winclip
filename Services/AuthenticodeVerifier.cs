using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WinClip.Native;

namespace WinClip.Services;

/// <summary>
/// Decides whether a downloaded installer is safe to launch.
///
/// Windows checks the Authenticode signature first (WinVerifyTrust). That
/// confirms the file still matches its signature and judges the certificate at
/// the signature's timestamp, not today. Release builds are signed through
/// Azure Trusted Signing, whose certificates last three days, so comparing
/// against today would refuse every installer three days after it was built.
/// The signer and its chain then have to match <see cref="UpdateSignerPin"/>,
/// which is compiled in and cannot be changed by configuration.
/// </summary>
internal static class AuthenticodeVerifier
{
    internal static bool IsTrusted(string filePath, out string reason)
    {
        try
        {
            var status = VerifyEmbeddedSignature(filePath);
            if (status != 0)
            {
                reason = $"Authenticode verification failed (0x{status:X8}).";
                return false;
            }

            using var signer = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
            var embedded = ReadEmbeddedCertificates(filePath);
            try
            {
                using var chain = new X509Chain();
                // WinVerifyTrust has already judged trust at the timestamp. This build only
                // recovers the path, so an expired three-day leaf must not fail it.
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.VerificationFlags =
                    X509VerificationFlags.IgnoreNotTimeValid | X509VerificationFlags.IgnoreNotTimeNested;
                chain.ChainPolicy.ExtraStore.AddRange(embedded);

                if (!chain.Build(signer))
                {
                    var statuses = string.Join(", ", chain.ChainStatus.Select(s => s.Status));
                    reason = $"Could not build the signer's certificate chain ({statuses}).";
                    return false;
                }

                var elements = new List<X509Certificate2>();
                foreach (var element in chain.ChainElements)
                    elements.Add(element.Certificate);

                try
                {
                    return UpdateSignerPin.Matches(signer, elements, out reason);
                }
                finally
                {
                    foreach (var certificate in elements)
                        certificate.Dispose();
                }
            }
            finally
            {
                foreach (var certificate in embedded)
                    certificate.Dispose();
            }
        }
        catch (Exception ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// The certificates carried inside the signature: the leaf and Microsoft's
    /// intermediates. If Windows can't read them, the chain build falls back to
    /// downloading intermediates from the leaf's AIA URLs, and refuses if that fails too.
    /// </summary>
    private static X509Certificate2Collection ReadEmbeddedCertificates(string filePath)
    {
        var certificates = new X509Certificate2Collection();
        try
        {
            certificates.Import(filePath);
        }
        catch (CryptographicException)
        {
            // Build without them; see the summary.
        }

        return certificates;
    }

    /// <summary>
    /// Returns 0 when the file carries a valid embedded Authenticode signature
    /// that chains to a trusted root, otherwise the WinVerifyTrust error code.
    /// Revocation is not checked, matching the previous behaviour.
    /// </summary>
    internal static int VerifyEmbeddedSignature(string filePath)
    {
        var filePathPtr = Marshal.StringToHGlobalUni(filePath);
        var fileInfoPtr = IntPtr.Zero;
        var trustDataPtr = IntPtr.Zero;
        try
        {
            var fileInfo = new NativeMethods.WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf<NativeMethods.WINTRUST_FILE_INFO>(),
                pcwszFilePath = filePathPtr,
            };
            fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.WINTRUST_FILE_INFO>());
            Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

            var trustData = new NativeMethods.WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<NativeMethods.WINTRUST_DATA>(),
                dwUIChoice = NativeMethods.WTD_UI_NONE,
                fdwRevocationChecks = NativeMethods.WTD_REVOKE_NONE,
                dwUnionChoice = NativeMethods.WTD_CHOICE_FILE,
                pFile = fileInfoPtr,
                dwStateAction = NativeMethods.WTD_STATEACTION_VERIFY,
                dwProvFlags = NativeMethods.WTD_REVOCATION_CHECK_NONE,
            };
            trustDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.WINTRUST_DATA>());
            Marshal.StructureToPtr(trustData, trustDataPtr, false);

            var action = NativeMethods.WINTRUST_ACTION_GENERIC_VERIFY_V2;
            var noUserWindow = new IntPtr(-1);
            var status = NativeMethods.WinVerifyTrust(noUserWindow, ref action, trustDataPtr);

            trustData = Marshal.PtrToStructure<NativeMethods.WINTRUST_DATA>(trustDataPtr);
            trustData.dwStateAction = NativeMethods.WTD_STATEACTION_CLOSE;
            Marshal.StructureToPtr(trustData, trustDataPtr, false);
            NativeMethods.WinVerifyTrust(noUserWindow, ref action, trustDataPtr);

            return status;
        }
        finally
        {
            if (trustDataPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(trustDataPtr);
            if (fileInfoPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(fileInfoPtr);
            Marshal.FreeHGlobal(filePathPtr);
        }
    }
}
