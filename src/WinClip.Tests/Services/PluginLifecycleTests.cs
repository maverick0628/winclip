using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WinClip.Actions;
using WinClip.Core.Services;
using WinClip.Extensions;
using Xunit;

namespace WinClip.Tests.Services;

public class PluginLifecycleTests
{
    [Fact]
    public void LoadExtensionsFrom_MissingDirectory_DoesNotThrow()
    {
        var manager = CreatePluginManager();
        var missingDir = Path.Combine(Path.GetTempPath(), $"winclip-missing-{Guid.NewGuid():N}");

        var ex = Record.Exception(() => manager.LoadExtensionsFrom(missingDir));

        Assert.Null(ex);
        Assert.Empty(manager.Extensions);
    }

    [Fact]
    public void LoadExtensionsFrom_ValidManifestButMissingAssembly_TracksLoadError()
    {
        var manager = CreatePluginManager();
        var root = Path.Combine(Path.GetTempPath(), $"winclip-ext-{Guid.NewGuid():N}");
        var extDir = Path.Combine(root, "sample.extension");
        Directory.CreateDirectory(extDir);

        try
        {
            File.WriteAllText(Path.Combine(extDir, "plugin.json"), """
{
  "id": "sample.extension",
  "name": "Sample Extension",
  "version": "1.0.0",
  "author": "Tests",
  "entryAssembly": "Sample.Extension.dll",
  "actions": [
    { "typeName": "Sample.Extension.Action", "name": "Sample Action", "icon": "default" }
  ]
}
""");

            manager.LoadExtensionsFrom(root);

            Assert.Single(manager.Extensions);
            var loaded = manager.Extensions[0];
            Assert.Equal("sample.extension", loaded.Manifest.Id);
            Assert.False(loaded.IsLoaded);
            Assert.NotNull(loaded.LoadError);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RegisterBuiltInAction_AddsActionToRegistry()
    {
        var registry = new ActionRegistry();
        var manager = CreatePluginManager(registry);

        manager.RegisterBuiltInAction(new TestAction("Sample BuiltIn Action"));

        Assert.Contains(registry.GetAllActions(), a => a.Name == "Sample BuiltIn Action");
    }

    private static PluginManager CreatePluginManager(IActionRegistry? registry = null)
    {
        var loggerFactory = LoggerFactory.Create(builder => { });
        var loaderLogger = loggerFactory.CreateLogger<PluginLoader>();
        var managerLogger = loggerFactory.CreateLogger<PluginManager>();

        return new PluginManager(
            new PluginLoader(loaderLogger),
            registry ?? new ActionRegistry(),
            managerLogger,
            loggerFactory,
            new ConfigurationBuilder().Build());
    }

    private sealed class TestAction : IAction
    {
        public TestAction(string name) => Name = name;

        public string Name { get; }
        public string Description => "Test action";
        public string IconName => "Test";

        public bool IsAvailable() => true;

        public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null) =>
            Task.FromResult(ActionResult.SuccessResult("ok"));
    }
}
