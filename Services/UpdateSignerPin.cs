using System.Collections.Frozen;
using System.Security.Cryptography.X509Certificates;

namespace WinClip.Services;

/// <summary>
/// The one signing identity an update installer may carry. Compiled in on purpose: it
/// must never come from appsettings.json, environment variables or the command line,
/// because anything that can edit those could otherwise approve its own installer.
///
/// <para>Release builds are signed through Azure Trusted Signing. Its leaf certificates
/// last about three days, so the leaf thumbprint is never pinned. What stays fixed is the
/// subject (DST Digital's validated identity), the durable-identity EKU unique to WinClip's
/// certificate profile, and Microsoft's chain above the leaf.</para>
///
/// <para>Changing any of these is a breaking change for installed copies: the release that
/// moves the pin must itself be signed by the old identity, or no client will accept it.</para>
/// </summary>
internal static class UpdateSignerPin
{
    internal const string CommonNameOid = "2.5.4.3";
    internal const string OrganizationOid = "2.5.4.10";
    internal const string LocalityOid = "2.5.4.7";
    internal const string StateOid = "2.5.4.8";
    internal const string CountryOid = "2.5.4.6";

    /// <summary>
    /// The signer subject as Windows renders it, and the exact Publisher string an MSIX
    /// manifest or .appinstaller needs for this certificate. Matching never compares this
    /// string; it compares the parsed attributes in <see cref="SignerSubject"/>.
    /// </summary>
    internal const string SubjectDistinguishedName =
        "CN=\"DST Digital, LLC\", O=\"DST Digital, LLC\", L=Atlanta, S=Georgia, C=US";

    internal const string CodeSigningEku = "1.3.6.1.5.5.7.3.3";

    /// <summary>
    /// Trusted Signing's per-profile EKU. It survives leaf rotation and no other
    /// certificate profile carries it, so it is what ties a leaf to WinClip's signing account.
    /// </summary>
    internal const string DurableIdentityEku = "1.3.6.1.4.1.311.97.168319635.88729184.535197288.375628414";

    internal const string MicrosoftOrganization = "Microsoft Corporation";
    internal const string MicrosoftCountry = "US";

    /// <summary>
    /// Trusted Signing issues leaves from two families of CA, and Microsoft rotates the
    /// number within each. The same certificate profile signed v2.1.1 from AOC CA 03 in
    /// May 2026 and v2.2.0 from EOC CA 03 in September. Only these prefixes are pinned,
    /// and the rest of the CN must be a number.
    /// </summary>
    internal static readonly string[] IssuingCaCommonNamePrefixes =
    [
        "Microsoft ID Verified CS AOC CA ",
        "Microsoft ID Verified CS EOC CA ",
    ];

    internal const string PolicyCaCommonName = "Microsoft ID Verified Code Signing PCA 2021";
    internal const string RootCommonName = "Microsoft Identity Verification Root Certificate Authority 2020";
    internal const string RootSerialNumber = "5498D2D1D45B1995481379C811C08799";

    /// <summary>Signer, issuing CA, PCA 2021, root.</summary>
    internal const int ChainLength = 4;

    internal static readonly FrozenDictionary<string, string> SignerSubject = new Dictionary<string, string>
    {
        [CommonNameOid] = "DST Digital, LLC",
        [OrganizationOid] = "DST Digital, LLC",
        [LocalityOid] = "Atlanta",
        [StateOid] = "Georgia",
        [CountryOid] = "US",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, string> PolicyCaSubject = MicrosoftName(PolicyCaCommonName);
    private static readonly FrozenDictionary<string, string> RootSubject = MicrosoftName(RootCommonName);

    /// <summary>
    /// Decides whether a signer and the chain Windows built for it are the pinned release
    /// identity. Call it only after WinVerifyTrust has accepted the file: this checks who
    /// signed, not whether the signature is valid. <paramref name="chain"/> runs from the
    /// signer (index 0) to the root. Any certificate or name that cannot be read refuses.
    /// </summary>
    internal static bool Matches(X509Certificate2 signer, IReadOnlyList<X509Certificate2> chain, out string reason)
    {
        try
        {
            return MatchesCore(signer, chain, out reason);
        }
        catch (Exception ex)
        {
            reason = $"Could not read the signer's certificates: {ex.Message}";
            return false;
        }
    }

    private static bool MatchesCore(X509Certificate2 signer, IReadOnlyList<X509Certificate2> chain, out string reason)
    {
        if (!NameIs(signer.SubjectName, SignerSubject))
            return Refuse($"subject is {signer.Subject}", out reason);

        if (!IsIssuingCa(signer.IssuerName))
            return Refuse($"issuer is {signer.Issuer}", out reason);

        var ekus = EnhancedKeyUsages(signer);
        if (!ekus.Contains(CodeSigningEku))
            return Refuse("the certificate has no Code Signing EKU", out reason);
        if (!ekus.Contains(DurableIdentityEku))
            return Refuse("the certificate lacks the pinned certificate-profile EKU", out reason);

        if (chain.Count != ChainLength)
            return Refuse($"the chain has {chain.Count} certificates, expected {ChainLength}", out reason);

        if (!chain[0].RawData.SequenceEqual(signer.RawData))
            return Refuse("the chain does not start at the signer", out reason);

        for (var i = 0; i < chain.Count - 1; i++)
        {
            if (!SameName(chain[i].IssuerName, chain[i + 1].SubjectName))
                return Refuse($"chain link {i} is not issued by link {i + 1}", out reason);
        }

        if (!IsIssuingCa(chain[1].SubjectName))
            return Refuse($"the issuing CA is {chain[1].Subject}", out reason);

        if (!NameIs(chain[2].SubjectName, PolicyCaSubject))
            return Refuse($"the chain passes through {chain[2].Subject}, not {PolicyCaCommonName}", out reason);

        var root = chain[3];
        if (!NameIs(root.SubjectName, RootSubject) || !SameName(root.SubjectName, root.IssuerName))
            return Refuse($"the chain ends at {root.Subject}, not {RootCommonName}", out reason);

        if (!string.Equals(root.SerialNumber, RootSerialNumber, StringComparison.OrdinalIgnoreCase))
            return Refuse($"the root's serial number is {root.SerialNumber}", out reason);

        reason = "Valid Authenticode signature from the pinned update signer.";
        return true;
    }

    /// <summary>
    /// True when the name has exactly the expected attributes, each in its own
    /// single-valued RDN, with values equal byte for byte. Order is not compared.
    /// </summary>
    internal static bool NameIs(X500DistinguishedName name, IReadOnlyDictionary<string, string> expected)
    {
        var actual = ReadAttributes(name);
        if (actual is null || actual.Count != expected.Count)
            return false;

        foreach (var (oid, value) in expected)
        {
            if (!actual.TryGetValue(oid, out var actualValue) || !string.Equals(actualValue, value, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    internal static bool IsIssuingCa(X500DistinguishedName name)
    {
        var actual = ReadAttributes(name);
        if (actual is null || actual.Count != 3)
            return false;

        if (!actual.TryGetValue(OrganizationOid, out var organization) || organization != MicrosoftOrganization)
            return false;
        if (!actual.TryGetValue(CountryOid, out var country) || country != MicrosoftCountry)
            return false;
        if (!actual.TryGetValue(CommonNameOid, out var commonName))
            return false;

        foreach (var prefix in IssuingCaCommonNamePrefixes)
        {
            if (!commonName.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var number = commonName[prefix.Length..];
            return number.Length > 0 && number.All(char.IsAsciiDigit);
        }

        return false;
    }

    /// <summary>
    /// Reads a name into OID to value. Returns null for a multi-valued RDN, a repeated
    /// attribute or a value that is not a string, so callers refuse rather than guess.
    /// </summary>
    private static Dictionary<string, string>? ReadAttributes(X500DistinguishedName name)
    {
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rdn in name.EnumerateRelativeDistinguishedNames())
        {
            if (rdn.HasMultipleElements)
                return null;

            var oid = rdn.GetSingleElementType().Value;
            var value = rdn.GetSingleElementValue();
            if (oid is null || value is null || !attributes.TryAdd(oid, value))
                return null;
        }

        return attributes;
    }

    private static bool SameName(X500DistinguishedName a, X500DistinguishedName b) =>
        a.RawData.SequenceEqual(b.RawData);

    private static HashSet<string> EnhancedKeyUsages(X509Certificate2 certificate)
    {
        var oids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var extension in certificate.Extensions)
        {
            if (extension is not X509EnhancedKeyUsageExtension eku)
                continue;

            foreach (var oid in eku.EnhancedKeyUsages)
            {
                if (oid.Value is not null)
                    oids.Add(oid.Value);
            }
        }

        return oids;
    }

    private static FrozenDictionary<string, string> MicrosoftName(string commonName) =>
        new Dictionary<string, string>
        {
            [CommonNameOid] = commonName,
            [OrganizationOid] = MicrosoftOrganization,
            [CountryOid] = MicrosoftCountry,
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static bool Refuse(string why, out string reason)
    {
        reason = $"Signer does not match the pinned update identity: {why}.";
        return false;
    }
}
