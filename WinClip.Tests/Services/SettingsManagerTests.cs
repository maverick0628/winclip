using WinClip.Services;
using Xunit;

namespace WinClip.Tests.Services;

// SettingsManagerTests writes to the real
// %APPDATA%\WinClip\settings.json because Environment.GetFolderPath(ApplicationData)
// ignores the APPDATA env var on Windows (SHGetFolderPath goes straight to the
// user profile). Running them in parallel causes atomic-move collisions that
// can corrupt the file and make the loader fall back to defaults. Keeping them
// in one xUnit collection serializes execution and keeps the suite stable.
[Collection("SettingsRealAppData")]
public class SettingsManagerTests
{
    [Fact]
    public void PersistsSettingsAcrossInstances()
    {
        var tempAppData = Path.Combine(Path.GetTempPath(), $"winclip-settings-{Guid.NewGuid():N}");
        var originalAppData = Environment.GetEnvironmentVariable("APPDATA");
        Directory.CreateDirectory(tempAppData);

        try
        {
            Environment.SetEnvironmentVariable("APPDATA", tempAppData);

            var first = new SettingsManager();
            first.SoundEnabled = false;
            first.AutoCopyOnDragSelect = true;
            first.RequireTextCursor = false;
            first.AIProvider = "openai";
            first.EnabledActions = new List<string> { "Copy", "Hash SHA256" };

            var second = new SettingsManager();
            Assert.False(second.SoundEnabled);
            Assert.True(second.AutoCopyOnDragSelect);
            Assert.False(second.RequireTextCursor);
            Assert.Equal("openai", second.AIProvider);
            Assert.Contains("Hash SHA256", second.EnabledActions);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
            if (Directory.Exists(tempAppData))
                Directory.Delete(tempAppData, recursive: true);
        }
    }

    [Fact]
    public void LegacyLicenceAndTrialFields_AreIgnoredWhenReadingOldSettingsFiles()
    {
        const string oldFile = """
            {
              "soundEnabled": false,
              "trialStartDate": "2026-01-01T00:00:00Z",
              "lastSeenUtc": "2026-02-01T00:00:00Z",
              "licenseKey": "dpapi:AAAA",
              "deviceId": "abc123"
            }
            """;

        var loaded = System.Text.Json.JsonSerializer.Deserialize<UserSettings>(
            oldFile,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

        Assert.NotNull(loaded);
        Assert.False(loaded!.SoundEnabled);
    }

    [Fact]
    public void UsageTelemetry_IsOffByDefault()
    {
        Assert.False(new UserSettings().EnableUsageTelemetry);
    }

    [Fact]
    public void UsageTelemetry_PersistsAcrossInstances()
    {
        var tempAppData = Path.Combine(Path.GetTempPath(), $"winclip-settings-{Guid.NewGuid():N}");
        var originalAppData = Environment.GetEnvironmentVariable("APPDATA");
        Directory.CreateDirectory(tempAppData);

        try
        {
            Environment.SetEnvironmentVariable("APPDATA", tempAppData);

            var first = new SettingsManager();
            first.EnableUsageTelemetry = true;
            Assert.True(new SettingsManager().EnableUsageTelemetry);

            first.EnableUsageTelemetry = false;
            Assert.False(new SettingsManager().EnableUsageTelemetry);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
            if (Directory.Exists(tempAppData))
                Directory.Delete(tempAppData, recursive: true);
        }
    }

    [Fact]
    public void ConcurrentMutations_DoNotThrowAndKeepSettingsReadable()
    {
        var tempAppData = Path.Combine(Path.GetTempPath(), $"winclip-settings-{Guid.NewGuid():N}");
        var originalAppData = Environment.GetEnvironmentVariable("APPDATA");
        Directory.CreateDirectory(tempAppData);

        try
        {
            Environment.SetEnvironmentVariable("APPDATA", tempAppData);
            var manager = new SettingsManager();

            var tasks = Enumerable.Range(0, 20).Select(i => Task.Run(() =>
            {
                manager.SoundEnabled = i % 2 == 0;
                manager.AddExcludedProcess($"proc{i}");
                _ = manager.Current;
            })).ToArray();

            var ex = Record.Exception(() => Task.WaitAll(tasks));
            Assert.Null(ex);
            Assert.NotEmpty(manager.ExcludedProcesses);
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
            if (Directory.Exists(tempAppData))
                Directory.Delete(tempAppData, recursive: true);
        }
    }
}
