using System.Text.Json;
using WinClip.Services;

namespace WinClip.Tests.Services;

public class UpdateFeedTests
{
    private const string X64Sha = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    private const string Arm64Sha = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private static readonly Version Current = new(2, 1, 1);

    internal static string Feed(
        string version = "2.2.0",
        int schemaVersion = 1,
        string x64Url = "https://winclip-updates.duncansmith.tech/WinClip-2.2.0-x64-Setup.exe",
        string x64Sha = X64Sha,
        bool includeArm64 = true,
        string? releaseNotesUrl = "https://duncansmith.tech/apps/winclip/")
    {
        var installers = new Dictionary<string, object>
        {
            ["x64"] = new { url = x64Url, sha256 = x64Sha, size = 6212776 },
        };
        if (includeArm64)
        {
            installers["arm64"] = new
            {
                url = "https://winclip-updates.duncansmith.tech/WinClip-2.2.0-arm64-Setup.exe",
                sha256 = Arm64Sha,
                size = 4824680,
            };
        }

        var feed = new Dictionary<string, object>
        {
            ["schemaVersion"] = schemaVersion,
            ["product"] = "WinClip",
            ["version"] = version,
            ["publishedAt"] = "2026-09-20T12:00:00.0000000Z",
            ["installers"] = installers,
        };
        if (releaseNotesUrl is not null)
            feed["releaseNotesUrl"] = releaseNotesUrl;

        return JsonSerializer.Serialize(feed);
    }

    [Fact]
    public void Evaluate_ReturnsUpdate_WhenFeedVersionIsNewer()
    {
        var result = UpdateFeed.Evaluate(Feed(), Current, "x64");

        Assert.Equal(UpdateFeedStatus.UpdateAvailable, result.Status);
        var update = Assert.IsType<UpdateInfo>(result.Update);
        Assert.Equal("2.2.0", update.Version);
        Assert.Equal("https://winclip-updates.duncansmith.tech/WinClip-2.2.0-x64-Setup.exe", update.DownloadUrl);
        Assert.Equal(X64Sha, update.Sha256);
        Assert.Equal(6212776, update.SizeBytes);
        Assert.Equal("https://duncansmith.tech/apps/winclip/", update.ReleaseNotesUrl);
        Assert.Equal(new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc), update.PublishedAt);
        Assert.Equal(DateTimeKind.Utc, update.PublishedAt.Kind);
    }

    [Fact]
    public void Evaluate_PicksTheInstallerForTheRunningArchitecture()
    {
        var result = UpdateFeed.Evaluate(Feed(), Current, "arm64");

        Assert.Equal(UpdateFeedStatus.UpdateAvailable, result.Status);
        Assert.Equal("https://winclip-updates.duncansmith.tech/WinClip-2.2.0-arm64-Setup.exe", result.Update!.DownloadUrl);
        Assert.Equal(Arm64Sha, result.Update.Sha256);
        Assert.Equal(4824680, result.Update.SizeBytes);
    }

    [Theory]
    [InlineData("2.1.1")]
    [InlineData("2.1.0")]
    [InlineData("1.9.9")]
    [InlineData("2.1.1.0")]
    [InlineData("v2.1.1")]
    public void Evaluate_ReportsUpToDate_WhenFeedIsNotNewer(string feedVersion)
    {
        var result = UpdateFeed.Evaluate(Feed(version: feedVersion), Current, "x64");

        Assert.Equal(UpdateFeedStatus.UpToDate, result.Status);
        Assert.Null(result.Update);
    }

    [Fact]
    public void Evaluate_DoesNotInspectInstallers_WhenAlreadyUpToDate()
    {
        var json = Feed(version: "2.1.1", x64Url: "not a url");

        Assert.Equal(UpdateFeedStatus.UpToDate, UpdateFeed.Evaluate(json, Current, "x64").Status);
    }

    [Fact]
    public void Evaluate_ReportsNoInstaller_WhenArchitectureIsMissing()
    {
        var result = UpdateFeed.Evaluate(Feed(includeArm64: false), Current, "arm64");

        Assert.Equal(UpdateFeedStatus.NoInstallerForArchitecture, result.Status);
        Assert.Null(result.Update);
    }

    [Fact]
    public void Evaluate_IgnoresFeed_WhenSchemaIsNewerThanThisBuildUnderstands()
    {
        var result = UpdateFeed.Evaluate(Feed(schemaVersion: 2), Current, "x64");

        Assert.Equal(UpdateFeedStatus.UnsupportedSchema, result.Status);
        Assert.Null(result.Update);
    }

    [Fact]
    public void Evaluate_FallsBackToPublicPage_WhenReleaseNotesUrlIsMissing()
    {
        var result = UpdateFeed.Evaluate(Feed(releaseNotesUrl: null), Current, "x64");

        Assert.Equal(AppInfo.ReleasesPage, result.Update!.ReleaseNotesUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html><body>Not Found</body></html>")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\": \"1\", \"version\": \"2.2.0\"}")]
    [InlineData("{\"schemaVersion\": 1}")]
    [InlineData("{\"schemaVersion\": 1, \"version\": 220}")]
    [InlineData("{\"schemaVersion\": 1, \"version\": \"2.2.0\"}")]
    [InlineData("{\"schemaVersion\": 1, \"version\": \"2.2.0\", \"installers\": []}")]
    [InlineData("{\"schemaVersion\": 1, \"version\": \"2.2.0\", \"installers\": {\"x64\": \"https://example.com/a.exe\"}}")]
    [InlineData("{\"schemaVersion\": 0, \"version\": \"2.2.0\"}")]
    public void Evaluate_ReportsMalformed_ForUnusableFeeds(string json)
    {
        var result = UpdateFeed.Evaluate(json, Current, "x64");

        Assert.Equal(UpdateFeedStatus.Malformed, result.Status);
        Assert.Null(result.Update);
    }

    [Theory]
    [InlineData("2.2.0-beta.1")]
    [InlineData("latest")]
    [InlineData("2")]
    [InlineData("")]
    public void Evaluate_ReportsMalformed_WhenVersionIsNotAStableRelease(string feedVersion)
    {
        var result = UpdateFeed.Evaluate(Feed(version: feedVersion), Current, "x64");

        Assert.Equal(UpdateFeedStatus.Malformed, result.Status);
    }

    [Theory]
    [InlineData("http://winclip-updates.duncansmith.tech/WinClip-2.2.0-x64-Setup.exe")]
    [InlineData("/WinClip-2.2.0-x64-Setup.exe")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    public void Evaluate_ReportsMalformed_WhenInstallerUrlIsNotHttps(string url)
    {
        var result = UpdateFeed.Evaluate(Feed(x64Url: url), Current, "x64");

        Assert.Equal(UpdateFeedStatus.Malformed, result.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc123")]
    [InlineData("ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    public void Evaluate_ReportsMalformed_WhenSha256IsInvalid(string sha)
    {
        var result = UpdateFeed.Evaluate(Feed(x64Sha: sha), Current, "x64");

        Assert.Equal(UpdateFeedStatus.Malformed, result.Status);
    }

    [Theory]
    [InlineData("2.1.2", "2.1.1", true)]
    [InlineData("2.2.0", "2.1.9", true)]
    [InlineData("3.0.0", "2.99.99", true)]
    [InlineData("2.10.0", "2.9.0", true)]
    [InlineData("2.1.1", "2.1.1", false)]
    [InlineData("2.1.0", "2.1.1", false)]
    [InlineData("2.1.1.0", "2.1.1", false)]
    [InlineData("2.1.1", "2.1.1.0", false)]
    [InlineData("2.1", "2.1.0", false)]
    [InlineData("2.1.1.1", "2.1.1", true)]
    public void IsNewer_ComparesComponentsNumericallyAndTreatsMissingAsZero(string remote, string current, bool expected)
    {
        Assert.True(UpdateFeed.TryParseVersion(remote, out var r));
        Assert.True(UpdateFeed.TryParseVersion(current, out var c));

        Assert.Equal(expected, UpdateFeed.IsNewer(r, c));
    }

    [Theory]
    [InlineData("2.1.1", true)]
    [InlineData("v2.1.1", true)]
    [InlineData("V2.1.1", true)]
    [InlineData(" 2.1.1 ", true)]
    [InlineData("2.1.1-rc.1", false)]
    [InlineData("2", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParseVersion_AcceptsOnlyNumericVersions(string? raw, bool expected)
    {
        Assert.Equal(expected, UpdateFeed.TryParseVersion(raw, out _));
    }

    [Fact]
    public void CurrentArchitecture_IsOneTheFeedPublishes()
    {
        Assert.Contains(UpdateFeed.CurrentArchitecture, new[] { "x64", "arm64" });
    }

    [Fact]
    public void FileMatchesSha256_ComparesCaseInsensitively()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "abc");
            const string sha = "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";

            Assert.True(UpdateService.FileMatchesSha256(path, sha));
            Assert.True(UpdateService.FileMatchesSha256(path, sha.ToLowerInvariant()));
            Assert.False(UpdateService.FileMatchesSha256(path, X64Sha));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
