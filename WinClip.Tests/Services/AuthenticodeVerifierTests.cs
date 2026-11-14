using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Xml.Linq;
using WinClip.Services;
using Xunit;
using Xunit.Abstractions;

namespace WinClip.Tests.Services;

public class AuthenticodeVerifierTests
{
    private const string Installer211Sha256 = "6842545787F27D18E7A5E30674EE5D78E339BF6F214B48C2D030C41A6EFA6793";
    private const string Installer220Sha256 = "730F24B89F251663CD09C9DF8A13774A253AE27961E5F22FC8B2C3C8767DBDF8";
    private const string ReleaseInstallerVariable = "WINCLIP_RELEASE_INSTALLER";

    private readonly ITestOutputHelper _output;

    public AuthenticodeVerifierTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void IsTrusted_RejectsAnUnsignedFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"winclip-unsigned-{Guid.NewGuid():N}.exe");
        File.WriteAllBytes(path, RandomNumberGenerator.GetBytes(4096));
        try
        {
            Assert.False(AuthenticodeVerifier.IsTrusted(path, out var reason));
            Assert.Contains("Authenticode verification failed", reason);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // coreclr.dll carries a valid Microsoft signature, so WinVerifyTrust passes and
    // only the pin can refuse it.
    [Fact]
    public void IsTrusted_RejectsAValidSignatureFromAnotherSigner()
    {
        Assert.False(AuthenticodeVerifier.IsTrusted(SignedRuntimeFile(), out var reason));
        Assert.DoesNotContain("Authenticode verification failed", reason);
        _output.WriteLine(reason);
    }

    [Fact]
    public void IsTrusted_RejectsASignedFileThatWasModified()
    {
        var path = Path.Combine(Path.GetTempPath(), $"winclip-tampered-{Guid.NewGuid():N}.dll");
        var bytes = File.ReadAllBytes(SignedRuntimeFile());
        bytes[bytes.Length / 3] ^= 0xFF;
        File.WriteAllBytes(path, bytes);
        try
        {
            Assert.False(AuthenticodeVerifier.IsTrusted(path, out var reason));
            Assert.Contains("Authenticode verification failed", reason);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Real installers signed by Trusted Signing, so the pin is exercised end to end:
    // v2.1.1 was issued from AOC CA 03 in May 2026 and v2.2.0 from EOC CA 03 in
    // September. Both leaves expired three days after signing, and the timestamp keeps
    // the signatures valid. build.yml downloads them from the GitHub releases and sets
    // the variables when the download succeeds. Without a variable the case is skipped.
    [Theory]
    [InlineData("WINCLIP_SIGNED_INSTALLER", Installer211Sha256)]
    [InlineData("WINCLIP_SIGNED_INSTALLER_2_2_0", Installer220Sha256)]
    public void IsTrusted_AcceptsAReleasedInstallerAfterItsCertificateExpired(string variable, string expectedSha256)
    {
        var path = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(path))
        {
            _output.WriteLine($"Skipped: {variable} is not set (the released installer was not available to download).");
            return;
        }

        Assert.True(File.Exists(path), $"missing {path}");
        Assert.Equal(expectedSha256, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));

        Assert.True(AuthenticodeVerifier.IsTrusted(path, out var reason), reason);
        _output.WriteLine(reason);
    }

    // release.yml runs this against each installer it has just signed, before the feed
    // goes live, so a release every installed copy would refuse never ships. The step
    // looks for the PIN-ACCEPTED line in the results, so a skipped run can't pass it.
    [Fact]
    public void IsTrusted_AcceptsTheInstallerThisReleaseSigned()
    {
        var path = Environment.GetEnvironmentVariable(ReleaseInstallerVariable);
        if (string.IsNullOrWhiteSpace(path))
        {
            _output.WriteLine($"Skipped: {ReleaseInstallerVariable} is set only by release.yml.");
            return;
        }

        Assert.True(File.Exists(path), $"missing {path}");
        Assert.True(AuthenticodeVerifier.IsTrusted(path, out var reason), reason);
        _output.WriteLine($"PIN-ACCEPTED {Path.GetFileName(path)}: {reason}");
    }

    [Fact]
    public void Pin_AcceptsTheExactReleaseIdentity()
    {
        var (signer, chain) = SignedChain();

        Assert.True(UpdateSignerPin.Matches(signer, chain, out var reason), reason);
    }

    // The subject string Windows shows for the real certificate, and the Publisher
    // string the MSIX files use, must parse to exactly the pinned attributes.
    [Fact]
    public void Pin_AcceptsTheSubjectAsWindowsRendersIt()
    {
        var (signer, chain) = SignedChain(subject: new X500DistinguishedName(UpdateSignerPin.SubjectDistinguishedName));

        Assert.True(UpdateSignerPin.Matches(signer, chain, out var reason), reason);
    }

    [Theory]
    [InlineData("CN=DST Digital, O=DST Digital, L=Atlanta, S=Georgia, C=US")]
    [InlineData("CN=Duncan Smith, O=Duncan Smith, L=Atlanta, S=Georgia, C=US")]
    [InlineData("CN=\"DST Digital, LLC Updates\", O=\"DST Digital, LLC\", L=Atlanta, S=Georgia, C=US")]
    [InlineData("CN=\"Not DST Digital, LLC\", O=\"DST Digital, LLC\", L=Atlanta, S=Georgia, C=US")]
    [InlineData("CN=\"dst digital, llc\", O=\"DST Digital, LLC\", L=Atlanta, S=Georgia, C=US")]
    [InlineData("CN=\"DST Digital, LLC\", O=\"DST Digital, LLC\", L=Savannah, S=Georgia, C=US")]
    [InlineData("CN=\"DST Digital, LLC\", OU=Updates, O=\"DST Digital, LLC\", L=Atlanta, S=Georgia, C=US")]
    [InlineData("CN=\"DST Digital, LLC\", CN=Someone Else, O=\"DST Digital, LLC\", L=Atlanta, S=Georgia, C=US")]
    [InlineData("CN=\"DST Digital, LLC\", O=\"DST Digital, LLC\", S=Georgia, C=US")]
    [InlineData("CN=\"DST Digital, LLC\", O=\"DST Digital, LLC\", L=Atlanta, S=Georgia")]
    public void Pin_RefusesAnyOtherSubject(string subject)
    {
        var (signer, chain) = SignedChain(subject: new X500DistinguishedName(subject));

        Assert.False(UpdateSignerPin.Matches(signer, chain, out var reason));
        Assert.Contains("subject", reason);
    }

    [Fact]
    public void Pin_RefusesAWrongDurableIdentityEku()
    {
        var (signer, chain) = SignedChain(ekus:
        [
            UpdateSignerPin.CodeSigningEku,
            "1.3.6.1.4.1.311.97.1.0",
            "1.3.6.1.4.1.311.97.168319635.88729184.535197288.375628415",
        ]);

        Assert.False(UpdateSignerPin.Matches(signer, chain, out var reason));
        Assert.Contains("certificate-profile EKU", reason);
    }

    [Fact]
    public void Pin_RefusesAMissingCodeSigningEku()
    {
        var (signer, chain) = SignedChain(ekus: ["1.3.6.1.4.1.311.97.1.0", UpdateSignerPin.DurableIdentityEku]);

        Assert.False(UpdateSignerPin.Matches(signer, chain, out var reason));
        Assert.Contains("Code Signing", reason);
    }

    [Fact]
    public void Pin_RefusesACertificateWithNoEkus()
    {
        var (signer, chain) = SignedChain(ekus: []);

        Assert.False(UpdateSignerPin.Matches(signer, chain, out _));
    }

    [Theory]
    [InlineData("Microsoft ID Verified CS EOC CA 03", "Example Corporation")]
    [InlineData("Example Code Signing CA", MicrosoftCorporation)]
    [InlineData("Microsoft ID Verified CS EOC CA Evil", MicrosoftCorporation)]
    [InlineData("Microsoft ID Verified CS EOC CA ", MicrosoftCorporation)]
    [InlineData("Microsoft ID Verified CS AOC CA 03a", MicrosoftCorporation)]
    [InlineData("Microsoft ID Verified CS XYZ CA 03", MicrosoftCorporation)]
    [InlineData("Microsoft ID Verified CS AOC CA 03", "Example Corporation")]
    public void Pin_RefusesANonMicrosoftIssuer(string commonName, string organization)
    {
        var issuer = Name((Cn, commonName), (O, organization), (C, "US"));
        var (signer, chain) = SignedChain(issuingCa: issuer);

        Assert.False(UpdateSignerPin.Matches(signer, chain, out var reason));
        Assert.Contains("issuer", reason);
    }

    [Theory]
    [InlineData("Microsoft ID Verified CS EOC CA 01")]
    [InlineData("Microsoft ID Verified CS EOC CA 02")]
    [InlineData("Microsoft ID Verified CS EOC CA 04")]
    [InlineData("Microsoft ID Verified CS EOC CA 10")]
    [InlineData("Microsoft ID Verified CS AOC CA 03")]
    [InlineData("Microsoft ID Verified CS AOC CA 04")]
    public void Pin_AcceptsARotatedIssuingCa(string commonName)
    {
        var issuer = Name((Cn, commonName), (O, MicrosoftCorporation), (C, "US"));
        var (signer, chain) = SignedChain(issuingCa: issuer);

        Assert.True(UpdateSignerPin.Matches(signer, chain, out var reason), reason);
    }

    [Fact]
    public void Pin_RefusesAChainWithoutThePolicyCa()
    {
        var (signer, chain) = SignedChain();

        Assert.False(UpdateSignerPin.Matches(signer, [chain[0], chain[1], chain[3]], out var reason));
        Assert.Contains("chain", reason);
    }

    [Fact]
    public void Pin_RefusesAChainThroughAnotherPolicyCa()
    {
        var pca = Name((Cn, "Microsoft Code Signing PCA 2011"), (O, MicrosoftCorporation), (C, "US"));
        var (signer, chain) = SignedChain(policyCa: pca);

        Assert.False(UpdateSignerPin.Matches(signer, chain, out var reason));
        Assert.Contains(UpdateSignerPin.PolicyCaCommonName, reason);
    }

    [Fact]
    public void Pin_RefusesAChainEndingAtAnotherRoot()
    {
        var root = Name((Cn, "Microsoft Root Certificate Authority 2011"), (O, MicrosoftCorporation), (C, "US"));
        var (signer, chain) = SignedChain(root: root);

        Assert.False(UpdateSignerPin.Matches(signer, chain, out var reason));
        Assert.Contains(UpdateSignerPin.RootCommonName, reason);
    }

    [Fact]
    public void Pin_RefusesARootWithTheRightNameAndAnotherSerial()
    {
        var (signer, chain) = SignedChain(rootSerial: "01");

        Assert.False(UpdateSignerPin.Matches(signer, chain, out var reason));
        Assert.Contains("serial", reason);
    }

    [Fact]
    public void Pin_RefusesAChainBuiltForAnotherCertificate()
    {
        var (signer, _) = SignedChain();
        var (_, otherChain) = SignedChain();

        Assert.False(UpdateSignerPin.Matches(signer, otherChain, out var reason));
        Assert.Contains("does not start at the signer", reason);
    }

    [Fact]
    public void Pin_RefusesAnEmptyChain()
    {
        var (signer, _) = SignedChain();

        Assert.False(UpdateSignerPin.Matches(signer, [], out _));
    }

    [Fact]
    public void AppInstallerPublisher_IsTheSigningSubject()
    {
        var path = Path.Combine(FindRepoRoot(), "WinClip.Package", "winclip.appinstaller.template.xml");
        XNamespace ns = "http://schemas.microsoft.com/appx/appinstaller/2018";
        var bundle = XDocument.Load(path).Root?.Element(ns + "MainBundle");

        Assert.NotNull(bundle);
        Assert.Equal(UpdateSignerPin.SubjectDistinguishedName, (string?)bundle.Attribute("Publisher"));
    }

    [Fact]
    public void AppSettings_CarriesNoSignerPins()
    {
        var appSettings = Path.Combine(FindRepoRoot(), "appsettings.json");
        using var doc = JsonDocument.Parse(
            File.ReadAllText(appSettings),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var update = doc.RootElement.GetProperty("Update");

        Assert.False(update.TryGetProperty("TrustedSignerSubjects", out _));
        Assert.False(update.TryGetProperty("TrustedSignerThumbprints", out _));
        Assert.False(update.TryGetProperty("FeedUrl", out _));
    }

    private const string Cn = UpdateSignerPin.CommonNameOid;
    private const string O = UpdateSignerPin.OrganizationOid;
    private const string L = UpdateSignerPin.LocalityOid;
    private const string St = UpdateSignerPin.StateOid;
    private const string C = UpdateSignerPin.CountryOid;
    private const string MicrosoftCorporation = UpdateSignerPin.MicrosoftOrganization;

    private static readonly string[] ReleaseEkus =
    [
        "1.3.6.1.4.1.311.97.1.0",
        UpdateSignerPin.CodeSigningEku,
        UpdateSignerPin.DurableIdentityEku,
    ];

    private static readonly X500DistinguishedName ReleaseSubject = Name(
        (Cn, "DST Digital, LLC"), (O, "DST Digital, LLC"), (L, "Atlanta"), (St, "Georgia"), (C, "US"));

    private static readonly X500DistinguishedName IssuingCa03 = Name(
        (Cn, "Microsoft ID Verified CS EOC CA 03"), (O, MicrosoftCorporation), (C, "US"));

    private static readonly X500DistinguishedName PolicyCa = Name(
        (Cn, UpdateSignerPin.PolicyCaCommonName), (O, MicrosoftCorporation), (C, "US"));

    private static readonly X500DistinguishedName Root = Name(
        (Cn, UpdateSignerPin.RootCommonName), (O, MicrosoftCorporation), (C, "US"));

    /// <summary>
    /// Builds signer, issuing CA, PCA and root certificates with the given names. The
    /// signatures between them are not real; the pin checks names, EKUs and serials only,
    /// and WinVerifyTrust plus X509Chain check the signatures in production.
    /// </summary>
    private static (X509Certificate2 Signer, X509Certificate2[] Chain) SignedChain(
        X500DistinguishedName? subject = null,
        string[]? ekus = null,
        X500DistinguishedName? issuingCa = null,
        X500DistinguishedName? policyCa = null,
        X500DistinguishedName? root = null,
        string rootSerial = UpdateSignerPin.RootSerialNumber)
    {
        var eoc = issuingCa ?? IssuingCa03;
        var pca = policyCa ?? PolicyCa;
        var rootName = root ?? Root;

        var signer = Issue(subject ?? ReleaseSubject, eoc, "0A", ekus ?? ReleaseEkus);
        var eocCert = Issue(eoc, pca, "0B", []);
        var pcaCert = Issue(pca, rootName, "0C", []);
        var rootCert = Issue(rootName, rootName, rootSerial, []);

        return (signer, new[] { signer, eocCert, pcaCert, rootCert });
    }

    private static X509Certificate2 Issue(
        X500DistinguishedName subject, X500DistinguishedName issuer, string serialHex, string[] ekus)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var issuerKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256);
        if (ekus.Length > 0)
        {
            var oids = new OidCollection();
            foreach (var eku in ekus)
                oids.Add(new Oid(eku));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(oids, critical: false));
        }

        var now = DateTimeOffset.UtcNow;
        return request.Create(
            issuer,
            X509SignatureGenerator.CreateForECDsa(issuerKey),
            now.AddDays(-1),
            now.AddDays(2),
            Convert.FromHexString(serialHex));
    }

    /// <summary>Attributes in display order (CN first), encoded root-first like real certificates.</summary>
    private static X500DistinguishedName Name(params (string Oid, string Value)[] attributes)
    {
        var builder = new X500DistinguishedNameBuilder();
        for (var i = attributes.Length - 1; i >= 0; i--)
        {
            var (oid, value) = attributes[i];
            if (oid == C)
                builder.AddCountryOrRegion(value);
            else
                builder.Add(oid, value);
        }

        return builder.Build();
    }

    private static string SignedRuntimeFile()
    {
        var path = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "coreclr.dll");
        Assert.True(File.Exists(path), $"missing {path}");
        return path;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WinClip.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("WinClip.sln not found above " + AppContext.BaseDirectory);
    }
}
