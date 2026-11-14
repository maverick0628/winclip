using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinClip.Actions;
using WinClip.Actions.AI;
using WinClip.Core.Services;
using WinClip.Extensions;
using WinClip.Integrations.Blip;
using WinClip.Services;

namespace WinClip;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Register WPF Application
                services.AddSingleton<App>();

                // Register Selection Monitor Service
                services.AddSingleton<ISelectionMonitorService, SelectionMonitorService>();

                // Register Action system
                services.AddSingleton<IActionRegistry, ActionRegistry>();

                // Register Extension / Plugin system
                services.AddSingleton<PluginLoader>();
                services.AddSingleton<PluginManager>();

                // Register Update service
                services.AddSingleton<IUpdateService, UpdateService>();

                // Register Settings manager (persistent JSON settings)
                services.AddSingleton<SettingsManager>();

                // Register Global hotkey service
                services.AddSingleton<GlobalHotkeyService>();

                // Local AI provider detector (Ollama / LM Studio reachability probe)
                services.AddSingleton<LocalAIProviderDetector>();

                // Opt-in Plausible telemetry (gated by SettingsManager.EnableUsageTelemetry)
                services.AddSingleton<TelemetryClient>();

                // Register Browser service for actions that open URLs
                services.AddSingleton<IBrowserService, BrowserService>();

                // Register Blip integration services
                services.AddSingleton<IBlipDetector, BlipDetector>();
                services.AddSingleton<IBlipCommunicator, BlipCommunicator>();

                // Register clipboard history ring buffer.
                // Capacity is read from SettingsManager (default 100 in v2.1+).
                var clipboardHistoryPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WinClip", "clipboard-history.json");
                services.AddSingleton(sp =>
                {
                    var settings = sp.GetRequiredService<SettingsManager>();
                    var capacity = settings.HistoryMaxEntries > 0 ? settings.HistoryMaxEntries : 100;
                    return new ClipboardHistory(capacity, clipboardHistoryPath);
                });

                // Register pinned-history store (persistent JSON; survives ring-buffer eviction).
                var pinnedHistoryPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WinClip", "pinned-history.json");
                services.AddSingleton(new PinnedHistoryStore(pinnedHistoryPath));

                // Register snippet store (persistent JSON at %APPDATA%\WinClip\snippets.json)
                var snippetsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WinClip", "snippets.json");
                services.AddSingleton(new SnippetStore(snippetsPath));

                // Register AI service — picks provider based on configuration.
                // Priority on auto-detect: Ollama (local) → LM Studio (local) → Claude → OpenAI.
                services.AddSingleton<IAIService>(sp =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var provider = config["AI:Provider"]?.ToLowerInvariant();

                    if (provider == "ollama") return new OllamaService(config);
                    if (provider == "lmstudio") return new LMStudioService(config);
                    if (provider == "claude") return new ClaudeService(config);
                    if (provider == "openai") return new OpenAIService(config);

                    // Auto-detect: prefer the explicitly-configured local provider, then API-key providers
                    if (!string.IsNullOrWhiteSpace(config["AI:Ollama:Endpoint"]))
                        return new OllamaService(config);
                    if (!string.IsNullOrWhiteSpace(config["AI:LMStudio:Endpoint"]))
                        return new LMStudioService(config);
                    if (!string.IsNullOrWhiteSpace(config["AI:Claude:ApiKey"]))
                        return new ClaudeService(config);
                    return new OpenAIService(config);
                });

                // Configure logging — file logger is essential for tray apps (no console)
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.AddDebug();
                    builder.AddProvider(new FileLoggerProvider());
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        // Sync any saved AI settings into appsettings.json so IConfiguration picks them up
        var settingsManager = host.Services.GetRequiredService<SettingsManager>();
        if (!string.IsNullOrWhiteSpace(settingsManager.AIProvider))
        {
            settingsManager.SyncAISettingsToAppSettings();

            // Force IConfiguration to reload from disk after the sync write,
            // otherwise the in-memory config is stale and IAIService gets defaults.
            var configRoot = host.Services.GetRequiredService<IConfiguration>()
                as Microsoft.Extensions.Configuration.IConfigurationRoot;
            configRoot?.Reload();
        }

        // Register built-in actions via PluginManager
        var pluginManager = host.Services.GetRequiredService<PluginManager>();
        var browserService = host.Services.GetRequiredService<IBrowserService>();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        // Load user extensions from %APPDATA%\WinClip\extensions.
        // This keeps extension discovery active in normal app startup.
        try
        {
            var extensionsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinClip", "extensions");
            pluginManager.LoadExtensionsFrom(extensionsDirectory);
            logger.LogInformation("Extension discovery completed from {ExtensionsDirectory}", extensionsDirectory);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Extension discovery failed; continuing with built-in actions only");
        }

        pluginManager.RegisterBuiltInAction(new CopyAction());
        pluginManager.RegisterBuiltInAction(new WebSearchAction(browserService, configuration));
        pluginManager.RegisterBuiltInAction(new DictionaryAction(browserService));
        pluginManager.RegisterBuiltInAction(new WordCountAction());
        pluginManager.RegisterBuiltInAction(new EmailAction());

        // Text transform actions
        pluginManager.RegisterBuiltInAction(new TextTransformAction(TransformKind.Uppercase));
        pluginManager.RegisterBuiltInAction(new TextTransformAction(TransformKind.Lowercase));
        pluginManager.RegisterBuiltInAction(new TextTransformAction(TransformKind.TitleCase));
        pluginManager.RegisterBuiltInAction(new TextTransformAction(TransformKind.PascalCase));
        pluginManager.RegisterBuiltInAction(new TextTransformAction(TransformKind.CamelCase));
        pluginManager.RegisterBuiltInAction(new TextTransformAction(TransformKind.SnakeCase));
        pluginManager.RegisterBuiltInAction(new TextTransformAction(TransformKind.Trim));

        // URL & encoding utility actions
        pluginManager.RegisterBuiltInAction(new UrlEncodeAction());
        pluginManager.RegisterBuiltInAction(new UrlDecodeAction());
        pluginManager.RegisterBuiltInAction(new OpenUrlAction());
        pluginManager.RegisterBuiltInAction(new Base64EncodeAction());
        pluginManager.RegisterBuiltInAction(new Base64DecodeAction());

        // JSON utility actions (contextual — only appear for JSON-like text)
        pluginManager.RegisterBuiltInAction(new JsonFormatAction(JsonFormatMode.Prettify));
        pluginManager.RegisterBuiltInAction(new JsonFormatAction(JsonFormatMode.Minify));

        // Line manipulation actions (contextual — only appear for multi-line text)
        pluginManager.RegisterBuiltInAction(new LineSortAction(LineSortMode.SortAscending));
        pluginManager.RegisterBuiltInAction(new LineSortAction(LineSortMode.SortDescending));
        pluginManager.RegisterBuiltInAction(new LineSortAction(LineSortMode.RemoveDuplicates));
        pluginManager.RegisterBuiltInAction(new LineSortAction(LineSortMode.Reverse));

        // Smart extraction (contextual — only appears when emails/URLs/phones/IPs detected)
        pluginManager.RegisterBuiltInAction(new SmartExtractAction());

        // Hash generation actions
        pluginManager.RegisterBuiltInAction(new HashAction(HashAlgorithmKind.MD5));
        pluginManager.RegisterBuiltInAction(new HashAction(HashAlgorithmKind.SHA1));
        pluginManager.RegisterBuiltInAction(new HashAction(HashAlgorithmKind.SHA256));

        // Timestamp conversion (contextual — detects Unix epochs and date strings)
        pluginManager.RegisterBuiltInAction(new TimestampConvertAction());

        // HTML strip (contextual — only appears when HTML tags detected)
        pluginManager.RegisterBuiltInAction(new HtmlStripAction());

        // Slugify (convert text to URL-friendly slug)
        pluginManager.RegisterBuiltInAction(new SlugifyAction());

        // Number base conversion (contextual — detects hex 0x, binary 0b, decimal)
        pluginManager.RegisterBuiltInAction(new NumberBaseConvertAction());

        // Color conversion (contextual — detects hex, rgb(), hsl() colors)
        pluginManager.RegisterBuiltInAction(new ColorConvertAction());

        // CSV ↔ JSON conversion (contextual — detects CSV or JSON arrays)
        pluginManager.RegisterBuiltInAction(new CsvJsonConvertAction());

        // Regex replace (first line: pattern;;replacement, rest: target text)
        pluginManager.RegisterBuiltInAction(new RegexReplaceAction());

        // Text statistics (words, sentences, paragraphs, reading time, etc.)
        pluginManager.RegisterBuiltInAction(new TextStatsAction());

        // Markdown strip (contextual — detects markdown syntax)
        pluginManager.RegisterBuiltInAction(new MarkdownStripAction());

        // Line utility actions (contextual — only appear for multi-line text)
        pluginManager.RegisterBuiltInAction(new AddLineNumbersAction());
        pluginManager.RegisterBuiltInAction(new RemoveBlankLinesAction());

        // Wrap/unwrap lines
        pluginManager.RegisterBuiltInAction(new WrapLinesAction(WrapMode.Wrap));
        pluginManager.RegisterBuiltInAction(new WrapLinesAction(WrapMode.Unwrap));

        // Markdown table (contextual — detects CSV/TSV multi-line data)
        pluginManager.RegisterBuiltInAction(new MarkdownTableAction());

        // QR code (opens QR generator in browser)
        pluginManager.RegisterBuiltInAction(new QrCodeAction(browserService));

        // Translate (opens Google Translate with selected text)
        pluginManager.RegisterBuiltInAction(new TranslateAction(browserService, configuration));

        // Clipboard history (saves copied text to in-memory ring buffer)
        var clipboardHistory = host.Services.GetRequiredService<ClipboardHistory>();
        pluginManager.RegisterBuiltInAction(new ClipboardHistoryAction(clipboardHistory));

        // Snippet manager (save selected text as a reusable snippet)
        var snippetStore = host.Services.GetRequiredService<SnippetStore>();
        pluginManager.RegisterBuiltInAction(new SnippetAction(snippetStore));

        // Share to Twitter/X (opens compose with selected text)
        pluginManager.RegisterBuiltInAction(new ShareToTwitterAction());

        // AI-powered actions (hidden when no API key is configured)
        var aiService = host.Services.GetRequiredService<IAIService>();
        pluginManager.RegisterBuiltInAction(new GrammarCheckAction(aiService, configuration));
        pluginManager.RegisterBuiltInAction(new SummarizeAction(aiService, configuration));
        pluginManager.RegisterBuiltInAction(new RephraseAction(aiService, configuration));
        pluginManager.RegisterBuiltInAction(new ExpandAction(aiService, configuration));
        pluginManager.RegisterBuiltInAction(new CustomPromptAction(aiService, "Improve the clarity and tone of this text", configuration));

        // Blip integration (only visible when Blip is installed)
        var blipDetector = host.Services.GetRequiredService<IBlipDetector>();
        var blipCommunicator = host.Services.GetRequiredService<IBlipCommunicator>();
        pluginManager.RegisterBuiltInAction(new BlipAction(blipDetector, blipCommunicator));

        // User-defined action chains — each chain is registered as an IAction so
        // the existing popup/flyout machinery just works. The resolver lambda
        // looks up chain steps from the action registry at execute time.
        var actionRegistry = host.Services.GetRequiredService<IActionRegistry>();
        IAction? ResolveStep(string name) => actionRegistry.GetAction(name);
        foreach (var chain in settingsManager.GetActionChains())
        {
            if (string.IsNullOrWhiteSpace(chain.Name) || chain.ActionIds.Count == 0)
                continue;
            try
            {
                pluginManager.RegisterBuiltInAction(
                    new WinClip.Actions.Chains.ChainAction(chain, ResolveStep));
                logger.LogInformation("Registered action chain: {Chain} ({Steps} steps)",
                    chain.Name, chain.ActionIds.Count);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to register chain {Chain}; skipping", chain.Name);
            }
        }

        var app = host.Services.GetRequiredService<App>();
        app.Run();
    }
}
