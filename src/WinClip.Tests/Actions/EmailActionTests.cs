using System.Diagnostics;
using Moq;
using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Actions;

/// <summary>
/// Unit tests for the EmailAction class.
///
/// The process-launch behaviour is exercised through a mocked <see cref="IProcessLauncher"/>
/// so the encoding, URL construction and success/failure mapping run on every machine —
/// including headless CI. Only the single genuine end-to-end test that shell-executes a
/// real mailto: URI is marked [SkipInCI], because GitHub Actions runners have no registered
/// mailto: protocol handler and no interactive desktop to open one.
/// </summary>
public class EmailActionTests
{
    [Fact]
    public void Name_ShouldReturnEmail()
    {
        // Arrange
        var action = new EmailAction();

        // Act
        var name = action.Name;

        // Assert
        Assert.Equal("Email", name);
    }

    [Fact]
    public void Description_ShouldReturnExpectedDescription()
    {
        // Arrange
        var action = new EmailAction();

        // Act
        var description = action.Description;

        // Assert
        Assert.Equal("Compose email with selected text", description);
    }

    [Fact]
    public void IconName_ShouldReturnEmail()
    {
        // Arrange
        var action = new EmailAction();

        // Act
        var iconName = action.IconName;

        // Assert
        Assert.Equal("Email", iconName);
    }

    [Fact]
    public void Constructor_WithNullLauncher_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new EmailAction(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyString_ShouldReturnFailure()
    {
        // Arrange
        var launcher = new Mock<IProcessLauncher>();
        var action = new EmailAction(launcher.Object);

        // Act
        var result = await action.ExecuteAsync(string.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No text selected to email", result.Message);
        Assert.Null(result.Error);
        launcher.Verify(l => l.Start(It.IsAny<ProcessStartInfo>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithNull_ShouldReturnFailure()
    {
        // Arrange
        var launcher = new Mock<IProcessLauncher>();
        var action = new EmailAction(launcher.Object);

        // Act
        var result = await action.ExecuteAsync(null!);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No text selected to email", result.Message);
        Assert.Null(result.Error);
        launcher.Verify(l => l.Start(It.IsAny<ProcessStartInfo>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithWhitespace_ShouldReturnFailure()
    {
        // Arrange
        var launcher = new Mock<IProcessLauncher>();
        var action = new EmailAction(launcher.Object);

        // Act
        var result = await action.ExecuteAsync("   ");

        // Assert — whitespace is rejected by IsNullOrWhiteSpace
        Assert.False(result.Success);
        Assert.Equal("No text selected to email", result.Message);
        launcher.Verify(l => l.Start(It.IsAny<ProcessStartInfo>()), Times.Never);
    }

    // --- Process-launch logic, verified via a mock launcher (runs in CI) ---

    [Theory]
    [InlineData("Test email content")]
    [InlineData("Line 1\nLine 2\nLine 3")]
    [InlineData("Special: !@#$%^&*()_+-={}[]|\\:\";<>?,./~`")]
    [InlineData("Unicode: ✓ ✔ ✘ © ® ™")]
    public async Task ExecuteAsync_WithValidText_LaunchesEncodedMailtoAndReturnsSuccess(string text)
    {
        // Arrange
        ProcessStartInfo? captured = null;
        var launcher = new Mock<IProcessLauncher>();
        launcher
            .Setup(l => l.Start(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(psi => captured = psi);
        var action = new EmailAction(launcher.Object);

        // Act
        var result = await action.ExecuteAsync(text);

        // Assert — result is success
        Assert.True(result.Success);
        Assert.Equal("Email client opened", result.Message);
        Assert.Null(result.Error);

        // Assert — a single shell-execute of a correctly-encoded mailto: URL
        launcher.Verify(l => l.Start(It.IsAny<ProcessStartInfo>()), Times.Once);
        Assert.NotNull(captured);
        Assert.True(captured!.UseShellExecute);
        Assert.StartsWith("mailto:?body=", captured.FileName);

        // The body must round-trip back to the original text (real encoding check,
        // not a re-implementation of EscapeDataString).
        var body = captured.FileName["mailto:?body=".Length..];
        Assert.Equal(text, Uri.UnescapeDataString(body));
    }

    [Fact]
    public async Task ExecuteAsync_WithLongText_LaunchesMailtoAndReturnsSuccess()
    {
        // Arrange
        var text = new string('A', 10000); // 10KB of text
        ProcessStartInfo? captured = null;
        var launcher = new Mock<IProcessLauncher>();
        launcher
            .Setup(l => l.Start(It.IsAny<ProcessStartInfo>()))
            .Callback<ProcessStartInfo>(psi => captured = psi);
        var action = new EmailAction(launcher.Object);

        // Act
        var result = await action.ExecuteAsync(text);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Email client opened", result.Message);
        Assert.Null(result.Error);
        Assert.NotNull(captured);
        var body = captured!.FileName["mailto:?body=".Length..];
        Assert.Equal(text, Uri.UnescapeDataString(body));
    }

    [Fact]
    public async Task ExecuteAsync_WhenLauncherThrows_ReturnsFailureAndPreservesException()
    {
        // Arrange — this reproduces what happens on a machine with no mailto handler
        // (e.g. a headless CI runner): the shell-execute throws and the action must
        // degrade gracefully rather than bubbling the exception.
        var boom = new InvalidOperationException("No application is associated with mailto:");
        var launcher = new Mock<IProcessLauncher>();
        launcher
            .Setup(l => l.Start(It.IsAny<ProcessStartInfo>()))
            .Throws(boom);
        var action = new EmailAction(launcher.Object);

        // Act
        var result = await action.ExecuteAsync("Hello");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Failed to open email client", result.Message);
        Assert.Same(boom, result.Error);
    }

    // --- Genuine end-to-end launch: needs a real mailto handler / GUI, so skipped in CI ---

    [SkipInCI]
    public async Task ExecuteAsync_WithRealLauncher_OpensDefaultMailClient()
    {
        // Exercises the real ProcessLauncher (Process.Start) against the OS shell.
        // Requires a registered mailto: protocol handler, which headless CI runners
        // (GitHub Actions windows-latest) do not have — hence [SkipInCI]. On a developer
        // machine with a mail client this is a true end-to-end smoke test.
        var action = new EmailAction();

        var result = await action.ExecuteAsync("Integration smoke test");

        Assert.True(result.Success);
        Assert.Equal("Email client opened", result.Message);
        Assert.Null(result.Error);
    }
}
