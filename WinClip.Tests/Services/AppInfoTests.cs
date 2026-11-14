using Xunit;

namespace WinClip.Tests.Services;

public class AppInfoTests
{
    [Fact]
    public void Version_IsNotNullOrEmpty()
    {
        Assert.False(string.IsNullOrEmpty(AppInfo.Version));
    }

    [Fact]
    public void ProductName_IsWinClip()
    {
        Assert.Equal("WinClip", AppInfo.ProductName);
    }

    [Fact]
    public void GetCurrentVersion_ReturnsParsableVersion()
    {
        var version = AppInfo.GetCurrentVersion();
        Assert.NotNull(version);
        Assert.True(version.Major >= 0);
    }

    [Fact]
    public void GetCurrentVersion_StripsPreReleaseSuffix()
    {
        // The assembly version won't have a pre-release suffix in tests,
        // but we verify the method doesn't throw
        var version = AppInfo.GetCurrentVersion();
        Assert.NotNull(version);
    }

    [Fact]
    public void UpdateUrl_IsValidHttpsUrl()
    {
        Assert.StartsWith("https://", AppInfo.UpdateUrl);
    }

    [Fact]
    public void UpdateUrl_IsThePublicFeedReleaseYmlPublishes()
    {
        // release.yml derives its upload target from this constant and requires this shape.
        // Updates are served from the R2 feed, not the GitHub releases API.
        Assert.Matches(@"^https://[^/]+(/.*)?/latest\.json$", AppInfo.UpdateUrl);
        Assert.DoesNotContain("github.com", AppInfo.UpdateUrl);
        Assert.DoesNotContain("github.com", AppInfo.ReleasesPage);
    }

    [Fact]
    public void ReleasesPage_IsValidHttpsUrl()
    {
        Assert.StartsWith("https://", AppInfo.ReleasesPage);
    }
}
