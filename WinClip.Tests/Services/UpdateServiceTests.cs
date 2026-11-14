using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WinClip.Services;
using Xunit;

namespace WinClip.Tests.Services;

public class UpdateServiceTests
{
    private static IConfiguration BuildConfig(string? feedUrl = null, bool autoCheck = true)
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Update:AutoCheck"]).Returns(autoCheck.ToString());
        mockConfig.Setup(c => c["Update:FeedUrl"]).Returns(feedUrl);
        return mockConfig.Object;
    }

    [Fact]
    public void Constructor_UsesDefaultFeedUrl_WhenNotConfigured()
    {
        var config = BuildConfig();
        var logger = Mock.Of<ILogger<UpdateService>>();

        using var service = new UpdateService(logger, config);
        // No exception = construction succeeded with defaults
        Assert.True(service.AutoCheckEnabled);
    }

    [Fact]
    public void AutoCheckEnabled_ReflectsConfiguration()
    {
        var config = BuildConfig(autoCheck: false);
        var logger = Mock.Of<ILogger<UpdateService>>();

        using var service = new UpdateService(logger, config);
        Assert.False(service.AutoCheckEnabled);
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenNetworkFails()
    {
        // A fake handler rather than a dead URL: Release builds ignore Update:FeedUrl.
        using var service = CreateService(_ =>
            throw new HttpRequestException("Connection refused.", new SocketException(10061)));

        Assert.Null(await service.CheckForUpdateAsync());
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenCancelled()
    {
        var config = BuildConfig(feedUrl: AppInfo.UpdateUrl);
        var logger = Mock.Of<ILogger<UpdateService>>();

        using var service = new UpdateService(logger, config);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await service.CheckForUpdateAsync(cts.Token);
        Assert.Null(result);
    }

    [Fact]
    public void ApplyUpdate_RequiresValidPath()
    {
        var config = BuildConfig();
        var logger = Mock.Of<ILogger<UpdateService>>();

        using var service = new UpdateService(logger, config);
        Assert.ThrowsAny<Exception>(() => service.ApplyUpdate("nonexistent.exe"));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var config = BuildConfig();
        var logger = Mock.Of<ILogger<UpdateService>>();

        var service = new UpdateService(logger, config);
        service.Dispose();
        service.Dispose(); // Should not throw
    }

    [Fact]
    public async Task CheckForUpdateAsync_PollsTheR2Feed_NotTheGitHubApi()
    {
        Uri? requested = null;
        using var service = CreateService(req =>
        {
            requested = req.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        await service.CheckForUpdateAsync();

        Assert.Equal(AppInfo.UpdateUrl, requested?.ToString());
        Assert.DoesNotContain("api.github.com", AppInfo.UpdateUrl);
    }

    [Fact]
    public void Constructor_IgnoresBlankFeedUrlOverride()
    {
        using var service = new UpdateService(Mock.Of<ILogger<UpdateService>>(), BuildConfig(feedUrl: "  "));

        Assert.Equal(AppInfo.UpdateUrl, service.FeedUrl);
    }

    [Fact]
    public void Constructor_HonoursFeedUrlOverride_OnlyInDebugBuilds()
    {
        const string testFeed = "https://example.invalid/latest.json";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Update:FeedUrl"] = testFeed })
            .Build();

        using var service = new UpdateService(Mock.Of<ILogger<UpdateService>>(), config);

        Assert.Equal(UpdateService.FeedUrlOverrideAllowed ? testFeed : AppInfo.UpdateUrl, service.FeedUrl);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenFeedIsNotServed(HttpStatusCode status)
    {
        using var service = CreateService(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent("<html>error</html>"),
        });

        Assert.Null(await service.CheckForUpdateAsync());
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenDnsLookupFails()
    {
        using var service = CreateService(_ =>
            throw new HttpRequestException("No such host is known.", new SocketException(11001)));

        Assert.Null(await service.CheckForUpdateAsync());
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenRequestTimesOut()
    {
        using var service = CreateService(_ => throw new TaskCanceledException("timeout"));

        Assert.Null(await service.CheckForUpdateAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("<!DOCTYPE html><html></html>")]
    [InlineData("{\"tag_name\": \"v9.9.9\", \"assets\": []}")]
    [InlineData("{\"schemaVersion\": 1, \"version\": \"999.0.0\"}")]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenFeedIsMalformed(string body)
    {
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body),
        });

        Assert.Null(await service.CheckForUpdateAsync());
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsNull_WhenFeedIsNotNewer()
    {
        var feed = UpdateFeedTests.Feed(version: "0.0.1");
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(feed),
        });

        Assert.Null(await service.CheckForUpdateAsync());
    }

    [Fact]
    public async Task CheckForUpdateAsync_ReturnsUpdate_WhenFeedAdvertisesNewerVersion()
    {
        var feed = UpdateFeedTests.Feed(version: "999.0.0");
        using var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(feed),
        });

        var update = await service.CheckForUpdateAsync();

        Assert.NotNull(update);
        Assert.Equal("999.0.0", update.Version);
        Assert.StartsWith("https://", update.DownloadUrl);
        Assert.NotNull(update.Sha256);
    }

    [Fact]
    public async Task CheckForUpdateAsync_SkipsTheFeed_ForPackagedInstalls()
    {
        var requests = 0;
        var feed = UpdateFeedTests.Feed(version: "999.0.0");
        var httpClient = new HttpClient(new FakeFeedHandler(_ =>
        {
            requests++;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(feed) };
        }));
        using var service = new UpdateService(
            Mock.Of<ILogger<UpdateService>>(), BuildConfig(), httpClient, isPackaged: () => true);

        Assert.Null(await service.CheckForUpdateAsync());
        Assert.Equal(0, requests);
    }

    [Fact]
    public void HasPackageIdentity_IsFalse_ForTheUnpackagedTestHost()
    {
        Assert.False(UpdateService.HasPackageIdentity());
    }

    private static UpdateService CreateService(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var httpClient = new HttpClient(new FakeFeedHandler(respond)) { Timeout = TimeSpan.FromSeconds(5) };
        return new UpdateService(Mock.Of<ILogger<UpdateService>>(), BuildConfig(), httpClient, isPackaged: () => false);
    }

    private sealed class FakeFeedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

        public FakeFeedHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_respond(request));
        }
    }
}

public class UpdateInfoTests
{
    [Fact]
    public void UpdateInfo_StoresAllProperties()
    {
        var info = new UpdateInfo
        {
            Version = "2.0.0",
            DownloadUrl = "https://example.com/setup.exe",
            ReleaseNotesUrl = "https://example.com/releases/v2.0.0",
            ReleaseNotes = "New features",
            SizeBytes = 12345678,
            PublishedAt = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc),
        };

        Assert.Equal("2.0.0", info.Version);
        Assert.Equal("https://example.com/setup.exe", info.DownloadUrl);
        Assert.Equal("https://example.com/releases/v2.0.0", info.ReleaseNotesUrl);
        Assert.Equal("New features", info.ReleaseNotes);
        Assert.Equal(12345678, info.SizeBytes);
        Assert.Equal(2025, info.PublishedAt.Year);
    }

    [Fact]
    public void UpdateInfo_AllowsNullOptionalProperties()
    {
        var info = new UpdateInfo
        {
            Version = "1.1.0",
            DownloadUrl = "https://example.com/setup.exe",
            ReleaseNotesUrl = "https://example.com/releases",
        };

        Assert.Null(info.ReleaseNotes);
        Assert.Null(info.SizeBytes);
    }
}
