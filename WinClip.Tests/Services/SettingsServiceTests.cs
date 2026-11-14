using System;
using System.IO;
using System.Threading.Tasks;
using WinClip.Models;
using WinClip.Services;
using Xunit;
using System.Windows.Input;

namespace WinClip.Tests.Services;

/// <summary>
/// Unit tests for the SettingsService class.
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _testSettingsPath;
    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        // Create a temporary file path for testing
        _testSettingsPath = Path.Combine(Path.GetTempPath(), $"winclip_test_{Guid.NewGuid()}.json");
        _settingsService = new SettingsService(_testSettingsPath);
    }

    public void Dispose()
    {
        // Clean up test file after each test
        if (File.Exists(_testSettingsPath))
        {
            File.Delete(_testSettingsPath);
        }

        // Clean up directory if empty
        var directory = Path.GetDirectoryName(_testSettingsPath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            try
            {
                if (Directory.GetFiles(directory).Length == 0)
                {
                    Directory.Delete(directory);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        // Act
        var settings = await _settingsService.LoadSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.GeneralSettings);
        Assert.NotNull(settings.ActionSettings);
        Assert.NotNull(settings.HotkeySettings);
        Assert.NotNull(settings.AppearanceSettings);
    }

    [Fact]
    public async Task SaveSettingsAsync_CreatesFileWithSettings()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        settings.GeneralSettings.ShowNotifications = false;
        settings.GeneralSettings.ClipboardHistorySize = 200;

        // Act
        await _settingsService.SaveSettingsAsync(settings);

        // Assert
        Assert.True(File.Exists(_testSettingsPath));
    }

    [Fact]
    public async Task SaveAndLoadSettings_PreservesAllSettings()
    {
        // Arrange
        var originalSettings = AppSettings.CreateDefault();
        originalSettings.GeneralSettings.ShowNotifications = false;
        originalSettings.GeneralSettings.ClipboardHistorySize = 200;
        originalSettings.GeneralSettings.MinimizeToTray = true;
        originalSettings.AppearanceSettings.Theme = "Dark";
        originalSettings.AppearanceSettings.FontSize = 14.0;

        // Act
        await _settingsService.SaveSettingsAsync(originalSettings);
        var loadedSettings = await _settingsService.LoadSettingsAsync();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.Equal(originalSettings.GeneralSettings.ShowNotifications, loadedSettings.GeneralSettings.ShowNotifications);
        Assert.Equal(originalSettings.GeneralSettings.ClipboardHistorySize, loadedSettings.GeneralSettings.ClipboardHistorySize);
        Assert.Equal(originalSettings.GeneralSettings.MinimizeToTray, loadedSettings.GeneralSettings.MinimizeToTray);
        Assert.Equal(originalSettings.AppearanceSettings.Theme, loadedSettings.AppearanceSettings.Theme);
        Assert.Equal(originalSettings.AppearanceSettings.FontSize, loadedSettings.AppearanceSettings.FontSize);
    }

    [Fact]
    public async Task SaveSettingsAsync_WithNullSettings_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _settingsService.SaveSettingsAsync(null!));
    }

    [Fact]
    public async Task LoadSettingsAsync_WithInvalidJson_ReturnsDefaultSettings()
    {
        // Arrange - Write invalid JSON to the file
        var directory = Path.GetDirectoryName(_testSettingsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        await File.WriteAllTextAsync(_testSettingsPath, "{ invalid json }");

        // Act
        var settings = await _settingsService.LoadSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.GeneralSettings);
    }

    [Fact]
    public void GetSettingsPath_ReturnsCorrectPath()
    {
        // Act
        var path = _settingsService.GetSettingsPath();

        // Assert
        Assert.Equal(_testSettingsPath, path);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_CreatesDefaultSettingsFile()
    {
        // Arrange - First save some custom settings
        var customSettings = AppSettings.CreateDefault();
        customSettings.GeneralSettings.ClipboardHistorySize = 500;
        await _settingsService.SaveSettingsAsync(customSettings);

        // Act
        await _settingsService.ResetToDefaultsAsync();

        // Assert
        var loadedSettings = await _settingsService.LoadSettingsAsync();
        Assert.NotNull(loadedSettings);
        Assert.Equal(100, loadedSettings.GeneralSettings.ClipboardHistorySize); // Default value
    }

    [Fact]
    public async Task SaveSettingsAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var nestedPath = Path.Combine(Path.GetTempPath(), $"winclip_test_{Guid.NewGuid()}", "nested", "settings.json");
        var nestedService = new SettingsService(nestedPath);
        var settings = AppSettings.CreateDefault();

        try
        {
            // Act
            await nestedService.SaveSettingsAsync(settings);

            // Assert
            Assert.True(File.Exists(nestedPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(nestedPath))
            {
                File.Delete(nestedPath);
            }
            var directory = Path.GetDirectoryName(nestedPath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public async Task SaveAndLoadSettings_PreservesActionItems()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        settings.ActionSettings.Actions.Clear();
        settings.ActionSettings.Actions.Add(new ActionItem
        {
            Name = "Test Action",
            Trigger = "keyword",
            Command = "test command",
            Enabled = true
        });

        // Act
        await _settingsService.SaveSettingsAsync(settings);
        var loadedSettings = await _settingsService.LoadSettingsAsync();

        // Assert
        Assert.Single(loadedSettings.ActionSettings.Actions);
        Assert.Equal("Test Action", loadedSettings.ActionSettings.Actions[0].Name);
        Assert.Equal("keyword", loadedSettings.ActionSettings.Actions[0].Trigger);
        Assert.Equal("test command", loadedSettings.ActionSettings.Actions[0].Command);
        Assert.True(loadedSettings.ActionSettings.Actions[0].Enabled);
    }

    [Fact]
    public async Task SaveAndLoadSettings_PreservesHotkeyBindings()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        settings.HotkeySettings.Hotkeys.Clear();
        settings.HotkeySettings.Hotkeys.Add(new HotkeyBinding
        {
            Name = "Test Hotkey",
            Command = "TestCommand",
            Key = Key.C,
            Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
            Enabled = true
        });

        // Act
        await _settingsService.SaveSettingsAsync(settings);
        var loadedSettings = await _settingsService.LoadSettingsAsync();

        // Assert
        Assert.Single(loadedSettings.HotkeySettings.Hotkeys);
        Assert.Equal("Test Hotkey", loadedSettings.HotkeySettings.Hotkeys[0].Name);
        Assert.Equal("TestCommand", loadedSettings.HotkeySettings.Hotkeys[0].Command);
        Assert.Equal(Key.C, loadedSettings.HotkeySettings.Hotkeys[0].Key);
        Assert.Equal(ModifierKeys.Control | ModifierKeys.Shift, loadedSettings.HotkeySettings.Hotkeys[0].Modifiers);
        Assert.True(loadedSettings.HotkeySettings.Hotkeys[0].Enabled);
    }

    [Fact]
    public async Task ExportSettingsAsync_WithValidPath_ExportsSettingsSuccessfully()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        settings.GeneralSettings.ShowNotifications = false;
        settings.GeneralSettings.ClipboardHistorySize = 250;
        settings.AppearanceSettings.Theme = "Dark";
        await _settingsService.SaveSettingsAsync(settings);

        var exportPath = Path.Combine(Path.GetTempPath(), $"winclip_export_{Guid.NewGuid()}.json");

        try
        {
            // Act
            await _settingsService.ExportSettingsAsync(exportPath);

            // Assert
            Assert.True(File.Exists(exportPath));

            // Verify exported content matches original settings
            var exportedJson = await File.ReadAllTextAsync(exportPath);
            Assert.Contains("\"ClipboardHistorySize\": 250", exportedJson);
            Assert.Contains("\"ShowNotifications\": false", exportedJson);
            Assert.Contains("\"Theme\": \"Dark\"", exportedJson);
        }
        finally
        {
            // Cleanup
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }
        }
    }

    [Fact]
    public async Task ExportSettingsAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _settingsService.ExportSettingsAsync(null!));
    }

    [Fact]
    public async Task ExportSettingsAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _settingsService.ExportSettingsAsync(string.Empty));
    }

    [Fact]
    public async Task ExportSettingsAsync_WithWhitespacePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _settingsService.ExportSettingsAsync("   "));
    }

    [Fact]
    public async Task ExportSettingsAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var settings = AppSettings.CreateDefault();
        await _settingsService.SaveSettingsAsync(settings);

        var nestedExportPath = Path.Combine(Path.GetTempPath(), $"winclip_export_{Guid.NewGuid()}", "nested", "export.json");

        try
        {
            // Act
            await _settingsService.ExportSettingsAsync(nestedExportPath);

            // Assert
            Assert.True(File.Exists(nestedExportPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(nestedExportPath))
            {
                File.Delete(nestedExportPath);
            }
            var directory = Path.GetDirectoryName(nestedExportPath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public async Task ImportSettingsAsync_WithValidFile_ImportsSettingsSuccessfully()
    {
        // Arrange
        var originalSettings = AppSettings.CreateDefault();
        originalSettings.GeneralSettings.ShowNotifications = false;
        originalSettings.GeneralSettings.ClipboardHistorySize = 300;
        originalSettings.AppearanceSettings.Theme = "Dark";
        originalSettings.AppearanceSettings.FontSize = 16.0;

        var importPath = Path.Combine(Path.GetTempPath(), $"winclip_import_{Guid.NewGuid()}.json");

        try
        {
            // Save settings to import file
            var json = System.Text.Json.JsonSerializer.Serialize(originalSettings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });
            await File.WriteAllTextAsync(importPath, json);

            // Act
            var importedSettings = await _settingsService.ImportSettingsAsync(importPath);

            // Assert
            Assert.NotNull(importedSettings);
            Assert.Equal(originalSettings.GeneralSettings.ShowNotifications, importedSettings.GeneralSettings.ShowNotifications);
            Assert.Equal(originalSettings.GeneralSettings.ClipboardHistorySize, importedSettings.GeneralSettings.ClipboardHistorySize);
            Assert.Equal(originalSettings.AppearanceSettings.Theme, importedSettings.AppearanceSettings.Theme);
            Assert.Equal(originalSettings.AppearanceSettings.FontSize, importedSettings.AppearanceSettings.FontSize);
        }
        finally
        {
            // Cleanup
            if (File.Exists(importPath))
            {
                File.Delete(importPath);
            }
        }
    }

    [Fact]
    public async Task ImportSettingsAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _settingsService.ImportSettingsAsync(null!));
    }

    [Fact]
    public async Task ImportSettingsAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _settingsService.ImportSettingsAsync(string.Empty));
    }

    [Fact]
    public async Task ImportSettingsAsync_WithWhitespacePath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _settingsService.ImportSettingsAsync("   "));
    }

    [Fact]
    public async Task ImportSettingsAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"winclip_nonexistent_{Guid.NewGuid()}.json");

        // Act
        var settings = await _settingsService.ImportSettingsAsync(nonExistentPath);

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.GeneralSettings);
        Assert.NotNull(settings.ActionSettings);
        Assert.NotNull(settings.HotkeySettings);
        Assert.NotNull(settings.AppearanceSettings);
        // Verify it's default settings
        Assert.Equal(100, settings.GeneralSettings.ClipboardHistorySize); // Default value
    }

    [Fact]
    public async Task ImportSettingsAsync_WithInvalidJson_ReturnsDefaultSettings()
    {
        // Arrange
        var importPath = Path.Combine(Path.GetTempPath(), $"winclip_import_{Guid.NewGuid()}.json");

        try
        {
            // Write invalid JSON to the file
            await File.WriteAllTextAsync(importPath, "{ invalid json content }");

            // Act
            var settings = await _settingsService.ImportSettingsAsync(importPath);

            // Assert
            Assert.NotNull(settings);
            Assert.NotNull(settings.GeneralSettings);
            Assert.NotNull(settings.ActionSettings);
            Assert.NotNull(settings.HotkeySettings);
            Assert.NotNull(settings.AppearanceSettings);
            // Verify it's default settings
            Assert.Equal(100, settings.GeneralSettings.ClipboardHistorySize); // Default value
        }
        finally
        {
            // Cleanup
            if (File.Exists(importPath))
            {
                File.Delete(importPath);
            }
        }
    }

    [Fact]
    public async Task ImportSettingsAsync_WithMalformedJson_ReturnsDefaultSettings()
    {
        // Arrange
        var importPath = Path.Combine(Path.GetTempPath(), $"winclip_import_{Guid.NewGuid()}.json");

        try
        {
            // Write malformed JSON to the file
            await File.WriteAllTextAsync(importPath, "{\"GeneralSettings\": {\"ClipboardHistorySize\": \"not a number\"}}");

            // Act
            var settings = await _settingsService.ImportSettingsAsync(importPath);

            // Assert
            Assert.NotNull(settings);
            Assert.NotNull(settings.GeneralSettings);
            // Verify it's default settings (should handle deserialization error gracefully)
        }
        finally
        {
            // Cleanup
            if (File.Exists(importPath))
            {
                File.Delete(importPath);
            }
        }
    }

    [Fact]
    public async Task ImportSettingsAsync_WithEmptyJson_ReturnsDefaultSettings()
    {
        // Arrange
        var importPath = Path.Combine(Path.GetTempPath(), $"winclip_import_{Guid.NewGuid()}.json");

        try
        {
            // Write empty JSON to the file
            await File.WriteAllTextAsync(importPath, "{}");

            // Act
            var settings = await _settingsService.ImportSettingsAsync(importPath);

            // Assert
            Assert.NotNull(settings);
            Assert.NotNull(settings.GeneralSettings);
        }
        finally
        {
            // Cleanup
            if (File.Exists(importPath))
            {
                File.Delete(importPath);
            }
        }
    }

    [Fact]
    public async Task ImportSettingsAsync_PreservesComplexSettings()
    {
        // Arrange
        var originalSettings = AppSettings.CreateDefault();

        // Customize various settings
        originalSettings.GeneralSettings.ShowNotifications = false;
        originalSettings.GeneralSettings.ClipboardHistorySize = 350;

        originalSettings.ActionSettings.Actions.Clear();
        originalSettings.ActionSettings.Actions.Add(new ActionItem
        {
            Name = "Imported Action",
            Trigger = "import-keyword",
            Command = "import command",
            Enabled = true
        });

        originalSettings.HotkeySettings.Hotkeys.Clear();
        originalSettings.HotkeySettings.Hotkeys.Add(new HotkeyBinding
        {
            Name = "Imported Hotkey",
            Command = "ImportCommand",
            Key = Key.I,
            Modifiers = ModifierKeys.Control | ModifierKeys.Alt,
            Enabled = true
        });

        var importPath = Path.Combine(Path.GetTempPath(), $"winclip_import_{Guid.NewGuid()}.json");

        try
        {
            // Save settings to import file
            var json = System.Text.Json.JsonSerializer.Serialize(originalSettings, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });
            await File.WriteAllTextAsync(importPath, json);

            // Act
            var importedSettings = await _settingsService.ImportSettingsAsync(importPath);

            // Assert
            Assert.NotNull(importedSettings);
            Assert.Equal(350, importedSettings.GeneralSettings.ClipboardHistorySize);
            Assert.False(importedSettings.GeneralSettings.ShowNotifications);

            Assert.Single(importedSettings.ActionSettings.Actions);
            Assert.Equal("Imported Action", importedSettings.ActionSettings.Actions[0].Name);
            Assert.Equal("import-keyword", importedSettings.ActionSettings.Actions[0].Trigger);

            Assert.Single(importedSettings.HotkeySettings.Hotkeys);
            Assert.Equal("Imported Hotkey", importedSettings.HotkeySettings.Hotkeys[0].Name);
            Assert.Equal(ModifierKeys.Control | ModifierKeys.Alt, importedSettings.HotkeySettings.Hotkeys[0].Modifiers);
        }
        finally
        {
            // Cleanup
            if (File.Exists(importPath))
            {
                File.Delete(importPath);
            }
        }
    }

    [Fact]
    public async Task ExportAndImportSettings_RoundTrip_PreservesAllSettings()
    {
        // Arrange
        var originalSettings = AppSettings.CreateDefault();

        // Customize general settings
        originalSettings.GeneralSettings.ShowNotifications = false;
        originalSettings.GeneralSettings.ClipboardHistorySize = 400;
        originalSettings.GeneralSettings.MinimizeToTray = true;

        // Customize appearance settings
        originalSettings.AppearanceSettings.Theme = "Dark";
        originalSettings.AppearanceSettings.FontSize = 18.0;

        // Customize action settings
        originalSettings.ActionSettings.Actions.Clear();
        originalSettings.ActionSettings.Actions.Add(new ActionItem
        {
            Name = "RoundTrip Action",
            Trigger = "roundtrip-keyword",
            Command = "roundtrip command",
            Enabled = true
        });
        originalSettings.ActionSettings.Actions.Add(new ActionItem
        {
            Name = "Second Action",
            Trigger = "second-keyword",
            Command = "second command",
            Enabled = false
        });

        // Customize hotkey settings
        originalSettings.HotkeySettings.Hotkeys.Clear();
        originalSettings.HotkeySettings.Hotkeys.Add(new HotkeyBinding
        {
            Name = "RoundTrip Hotkey",
            Command = "RoundTripCommand",
            Key = Key.R,
            Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
            Enabled = true
        });
        originalSettings.HotkeySettings.Hotkeys.Add(new HotkeyBinding
        {
            Name = "Second Hotkey",
            Command = "SecondCommand",
            Key = Key.S,
            Modifiers = ModifierKeys.Alt,
            Enabled = false
        });

        // Save original settings
        await _settingsService.SaveSettingsAsync(originalSettings);

        var exportPath = Path.Combine(Path.GetTempPath(), $"winclip_roundtrip_{Guid.NewGuid()}.json");

        try
        {
            // Act - Export settings
            await _settingsService.ExportSettingsAsync(exportPath);

            // Act - Import settings from the exported file
            var importedSettings = await _settingsService.ImportSettingsAsync(exportPath);

            // Assert - Verify general settings
            Assert.NotNull(importedSettings);
            Assert.Equal(originalSettings.GeneralSettings.ShowNotifications, importedSettings.GeneralSettings.ShowNotifications);
            Assert.Equal(originalSettings.GeneralSettings.ClipboardHistorySize, importedSettings.GeneralSettings.ClipboardHistorySize);
            Assert.Equal(originalSettings.GeneralSettings.MinimizeToTray, importedSettings.GeneralSettings.MinimizeToTray);

            // Assert - Verify appearance settings
            Assert.Equal(originalSettings.AppearanceSettings.Theme, importedSettings.AppearanceSettings.Theme);
            Assert.Equal(originalSettings.AppearanceSettings.FontSize, importedSettings.AppearanceSettings.FontSize);

            // Assert - Verify action settings
            Assert.Equal(2, importedSettings.ActionSettings.Actions.Count);
            Assert.Equal("RoundTrip Action", importedSettings.ActionSettings.Actions[0].Name);
            Assert.Equal("roundtrip-keyword", importedSettings.ActionSettings.Actions[0].Trigger);
            Assert.Equal("roundtrip command", importedSettings.ActionSettings.Actions[0].Command);
            Assert.True(importedSettings.ActionSettings.Actions[0].Enabled);
            Assert.Equal("Second Action", importedSettings.ActionSettings.Actions[1].Name);
            Assert.Equal("second-keyword", importedSettings.ActionSettings.Actions[1].Trigger);
            Assert.Equal("second command", importedSettings.ActionSettings.Actions[1].Command);
            Assert.False(importedSettings.ActionSettings.Actions[1].Enabled);

            // Assert - Verify hotkey settings
            Assert.Equal(2, importedSettings.HotkeySettings.Hotkeys.Count);
            Assert.Equal("RoundTrip Hotkey", importedSettings.HotkeySettings.Hotkeys[0].Name);
            Assert.Equal("RoundTripCommand", importedSettings.HotkeySettings.Hotkeys[0].Command);
            Assert.Equal(Key.R, importedSettings.HotkeySettings.Hotkeys[0].Key);
            Assert.Equal(ModifierKeys.Control | ModifierKeys.Shift, importedSettings.HotkeySettings.Hotkeys[0].Modifiers);
            Assert.True(importedSettings.HotkeySettings.Hotkeys[0].Enabled);
            Assert.Equal("Second Hotkey", importedSettings.HotkeySettings.Hotkeys[1].Name);
            Assert.Equal("SecondCommand", importedSettings.HotkeySettings.Hotkeys[1].Command);
            Assert.Equal(Key.S, importedSettings.HotkeySettings.Hotkeys[1].Key);
            Assert.Equal(ModifierKeys.Alt, importedSettings.HotkeySettings.Hotkeys[1].Modifiers);
            Assert.False(importedSettings.HotkeySettings.Hotkeys[1].Enabled);
        }
        finally
        {
            // Cleanup
            if (File.Exists(exportPath))
            {
                File.Delete(exportPath);
            }
        }
    }
}
