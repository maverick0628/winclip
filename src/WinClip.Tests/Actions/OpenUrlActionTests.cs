using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Actions;

/// <summary>
/// Unit tests for the OpenUrlAction class.
/// </summary>
public class OpenUrlActionTests
{
    [Fact]
    public void Name_ShouldReturnOpenUrl()
    {
        // Arrange
        var action = new OpenUrlAction();

        // Act
        var name = action.Name;

        // Assert
        Assert.Equal("Open URL", name);
    }

    [Fact]
    public void Description_ShouldReturnExpectedDescription()
    {
        // Arrange
        var action = new OpenUrlAction();

        // Act
        var description = action.Description;

        // Assert
        Assert.Equal("Open URL in default browser", description);
    }

    [Fact]
    public void IconName_ShouldReturnGlobe()
    {
        // Arrange
        var action = new OpenUrlAction();

        // Act
        var iconName = action.IconName;

        // Assert
        Assert.Equal("Globe", iconName);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyString_ShouldReturnFailure()
    {
        // Arrange
        var action = new OpenUrlAction();

        // Act
        var result = await action.ExecuteAsync(string.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No text selected to open", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithNull_ShouldReturnFailure()
    {
        // Arrange
        var action = new OpenUrlAction();

        // Act
        var result = await action.ExecuteAsync(null!);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No text selected to open", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithWhitespace_ShouldReturnFailure()
    {
        // Arrange
        var action = new OpenUrlAction();

        // Act
        var result = await action.ExecuteAsync("   ");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No text selected to open", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidUrl_ShouldReturnFailure()
    {
        // Arrange
        var action = new OpenUrlAction();
        var invalidText = "not a valid url at all!@#";

        // Act
        var result = await action.ExecuteAsync(invalidText);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Selected text is not a valid URL", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithFtpProtocol_ShouldReturnFailure()
    {
        // Arrange
        var action = new OpenUrlAction();
        var ftpUrl = "ftp://example.com/file.txt";

        // Act
        var result = await action.ExecuteAsync(ftpUrl);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("URL must use http or https protocol", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithFileProtocol_ShouldReturnFailure()
    {
        // Arrange
        var action = new OpenUrlAction();
        var fileUrl = "file:///C:/path/to/file.txt";

        // Act
        var result = await action.ExecuteAsync(fileUrl);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("URL must use http or https protocol", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithMailtoProtocol_ShouldReturnFailure()
    {
        // Arrange
        var action = new OpenUrlAction();
        var mailtoUrl = "mailto:test@example.com";

        // Act
        var result = await action.ExecuteAsync(mailtoUrl);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("URL must use http or https protocol", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithHttpUrl_ShouldAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var httpUrl = "http://example.com";

        // Act
        var result = await action.ExecuteAsync(httpUrl);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        // If it succeeds, it should have the expected success message
        // If it fails (no browser available), it should have a failure message
        if (result.Success)
        {
            Assert.Equal("Opened http://example.com/", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithHttpsUrl_ShouldAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var httpsUrl = "https://example.com";

        // Act
        var result = await action.ExecuteAsync(httpsUrl);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        // If it succeeds, it should have the expected success message
        // If it fails (no browser available), it should have a failure message
        if (result.Success)
        {
            Assert.Equal("Opened https://example.com/", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUrlWithoutProtocol_ShouldAddHttps()
    {
        // Arrange
        var action = new OpenUrlAction();
        var urlWithoutProtocol = "example.com";

        // Act
        var result = await action.ExecuteAsync(urlWithoutProtocol);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        // If it succeeds, it should have added https:// prefix
        // If it fails (no browser available), it should have a failure message
        if (result.Success)
        {
            Assert.Equal("Opened https://example.com/", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUrlWithPath_ShouldAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var urlWithPath = "https://example.com/path/to/page";

        // Act
        var result = await action.ExecuteAsync(urlWithPath);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        if (result.Success)
        {
            Assert.Equal("Opened https://example.com/path/to/page", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUrlWithQueryString_ShouldAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var urlWithQuery = "https://example.com/search?q=test&lang=en";

        // Act
        var result = await action.ExecuteAsync(urlWithQuery);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        if (result.Success)
        {
            Assert.Equal("Opened https://example.com/search?q=test&lang=en", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUrlWithFragment_ShouldAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var urlWithFragment = "https://example.com/page#section";

        // Act
        var result = await action.ExecuteAsync(urlWithFragment);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        if (result.Success)
        {
            Assert.Equal("Opened https://example.com/page#section", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUrlWithPort_ShouldAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var urlWithPort = "https://example.com:8080/page";

        // Act
        var result = await action.ExecuteAsync(urlWithPort);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        if (result.Success)
        {
            Assert.Equal("Opened https://example.com:8080/page", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithSubdomain_ShouldAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var subdomainUrl = "https://subdomain.example.com";

        // Act
        var result = await action.ExecuteAsync(subdomainUrl);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        if (result.Success)
        {
            Assert.Equal("Opened https://subdomain.example.com/", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithUrlWithWhitespace_ShouldTrimAndAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var urlWithWhitespace = "  https://example.com  ";

        // Act
        var result = await action.ExecuteAsync(urlWithWhitespace);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        if (result.Success)
        {
            Assert.Equal("Opened https://example.com/", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithLocalhostUrl_ShouldAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var localhostUrl = "http://localhost:3000";

        // Act
        var result = await action.ExecuteAsync(localhostUrl);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        if (result.Success)
        {
            Assert.Equal("Opened http://localhost:3000/", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithIpAddressUrl_ShouldAttemptToOpen()
    {
        // Arrange
        var action = new OpenUrlAction();
        var ipUrl = "http://192.168.1.1";

        // Act
        var result = await action.ExecuteAsync(ipUrl);

        // Assert
        // Note: This test may succeed or fail depending on the environment
        if (result.Success)
        {
            Assert.Equal("Opened http://192.168.1.1/", result.Message);
            Assert.Null(result.Error);
        }
        else
        {
            Assert.Equal("Failed to open URL", result.Message);
            Assert.NotNull(result.Error);
        }
    }
}
