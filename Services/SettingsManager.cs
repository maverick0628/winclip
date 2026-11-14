using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WinClip.UI;

namespace WinClip.Services;

/// <summary>
/// Predefined action visibility profiles. Users select a preset or customize individually.
/// </summary>
public enum ActionProfile
{
    Essentials,
    Developer,
    Writer,
    All,
    Custom
}

/// <summary>
/// Manages persistent user settings stored as JSON in %APPDATA%\WinClip\settings.json.
/// Thread-safe — all reads/writes go through a lock. Changes are flushed to disk immediately.
/// </summary>
public sealed class SettingsManager
{
    private readonly string _filePath;
    private readonly ILogger<SettingsManager>? _logger;
    private readonly object _lock = new();
    private UserSettings _settings;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SettingsManager(ILogger<SettingsManager>? logger = null)
    {
        _logger = logger;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WinClip");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "settings.json");
        _settings = Load();
    }

    /// <summary>
    /// Gets a snapshot of the current settings. Mutations to the returned object have no effect;
    /// use the individual setters on SettingsManager instead.
    /// </summary>
    public UserSettings Current
    {
        get { lock (_lock) { return _settings.Clone(); } }
    }

    // ── Individual property accessors (read + write-through) ──────────────

    public bool SoundEnabled
    {
        get { lock (_lock) { return _settings.SoundEnabled; } }
        set { Mutate(s => s.SoundEnabled = value); }
    }

    public bool Paused
    {
        get { lock (_lock) { return _settings.Paused; } }
        set { Mutate(s => s.Paused = value); }
    }

    public bool StartWithWindows
    {
        get { lock (_lock) { return _settings.StartWithWindows; } }
        set { Mutate(s => s.StartWithWindows = value); }
    }

    public List<string> ExcludedProcesses
    {
        get { lock (_lock) { return new List<string>(_settings.ExcludedProcesses); } }
        set { Mutate(s => s.ExcludedProcesses = new List<string>(value)); }
    }

    public void AddExcludedProcess(string processName)
    {
        Mutate(s =>
        {
            var lower = processName.ToLowerInvariant();
            if (!s.ExcludedProcesses.Contains(lower))
                s.ExcludedProcesses.Add(lower);
        });
    }

    public void RemoveExcludedProcess(string processName)
    {
        Mutate(s => s.ExcludedProcesses.Remove(processName.ToLowerInvariant()));
    }

    public PopupThemeKind Theme
    {
        get { lock (_lock) { return _settings.Theme; } }
        set { Mutate(s => s.Theme = value); }
    }

    public bool AutoCopyOnDragSelect
    {
        get { lock (_lock) { return _settings.AutoCopyOnDragSelect; } }
        set { Mutate(s => s.AutoCopyOnDragSelect = value); }
    }

    public bool RequireTextCursor
    {
        get { lock (_lock) { return _settings.RequireTextCursor; } }
        set { Mutate(s => s.RequireTextCursor = value); }
    }

    // ── Action profile ──────────────────────────────────────────────────

    public ActionProfile ActiveProfile
    {
        get { lock (_lock) { return _settings.ActiveProfile; } }
        set { Mutate(s => s.ActiveProfile = value); }
    }

    public List<string> EnabledActions
    {
        get { lock (_lock) { return new List<string>(_settings.EnabledActions); } }
        set { Mutate(s => s.EnabledActions = new List<string>(value)); }
    }

    // ── AI configuration ────────────────────────────────────────────────

    public string? AIProvider
    {
        get { lock (_lock) { return _settings.AIProvider; } }
        set { Mutate(s => s.AIProvider = value); }
    }

    public string? AIApiKey
    {
        get { lock (_lock) { return _settings.AIApiKey; } }
        set { Mutate(s => s.AIApiKey = value); }
    }

    public string? AIEndpoint
    {
        get { lock (_lock) { return _settings.AIEndpoint; } }
        set { Mutate(s => s.AIEndpoint = value); }
    }

    public string? AIModel
    {
        get { lock (_lock) { return _settings.AIModel; } }
        set { Mutate(s => s.AIModel = value); }
    }

    /// <summary>
    /// Global hotkey string (e.g., "Ctrl+Shift+C"). Null/empty means no hotkey.
    /// </summary>
    public string? GlobalHotkey
    {
        get { lock (_lock) { return _settings.GlobalHotkey; } }
        set { Mutate(s => s.GlobalHotkey = value); }
    }

    /// <summary>
    /// Hotkey that opens the clipboard history picker (e.g., "Win+Alt+V").
    /// Null/empty disables the picker hotkey.
    /// </summary>
    public string? HistoryPickerHotkey
    {
        get { lock (_lock) { return _settings.HistoryPickerHotkey; } }
        set { Mutate(s => s.HistoryPickerHotkey = value); }
    }

    /// <summary>Maximum number of entries retained in the clipboard history ring buffer.</summary>
    public int HistoryMaxEntries
    {
        get { lock (_lock) { return _settings.HistoryMaxEntries; } }
        set { Mutate(s => s.HistoryMaxEntries = value); }
    }

    /// <summary>When true, the history file is wiped on application exit.</summary>
    public bool ClearHistoryOnExit
    {
        get { lock (_lock) { return _settings.ClearHistoryOnExit; } }
        set { Mutate(s => s.ClearHistoryOnExit = value); }
    }

    /// <summary>Snapshot of currently pinned action IDs (in display order).</summary>
    public IReadOnlyList<string> GetPinnedActionIds()
    {
        lock (_lock) return _settings.PinnedActionIds.ToList().AsReadOnly();
    }

    /// <summary>
    /// Toggles whether the given action is pinned. Returns true if now pinned.
    /// Capped at <see cref="UserSettings.MaxPinnedActions"/> — pinning past the
    /// cap drops the oldest pinned entry.
    /// </summary>
    public bool TogglePinnedAction(string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName)) return false;
        bool nowPinned = false;
        Mutate(s =>
        {
            if (s.PinnedActionIds.Remove(actionName))
            {
                nowPinned = false;
            }
            else
            {
                s.PinnedActionIds.Insert(0, actionName);
                while (s.PinnedActionIds.Count > UserSettings.MaxPinnedActions)
                    s.PinnedActionIds.RemoveAt(s.PinnedActionIds.Count - 1);
                nowPinned = true;
            }
        });
        return nowPinned;
    }

    /// <summary>Snapshot of action invocation counts.</summary>
    public IReadOnlyDictionary<string, int> GetActionInvocationCounts()
    {
        lock (_lock) return new Dictionary<string, int>(_settings.ActionInvocationCounts);
    }

    /// <summary>Increment the invocation counter for an action by 1. Persists.</summary>
    public void IncrementActionUsage(string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName)) return;
        Mutate(s =>
        {
            s.ActionInvocationCounts.TryGetValue(actionName, out var count);
            s.ActionInvocationCounts[actionName] = count + 1;
        });
    }

    /// <summary>Opt-in flag for anonymous Plausible telemetry (default false).</summary>
    public bool EnableUsageTelemetry
    {
        get { lock (_lock) { return _settings.EnableUsageTelemetry; } }
        set { Mutate(s => s.EnableUsageTelemetry = value); }
    }

    /// <summary>Optional override for the Plausible site domain.</summary>
    public string? TelemetryDomain
    {
        get { lock (_lock) { return _settings.TelemetryDomain; } }
        set { Mutate(s => s.TelemetryDomain = value); }
    }

    /// <summary>Snapshot of currently configured action chains.</summary>
    public IReadOnlyList<WinClip.Actions.Chains.ActionChain> GetActionChains()
    {
        lock (_lock) return _settings.ActionChains.ToList().AsReadOnly();
    }

    /// <summary>
    /// Writes the current AI settings to appsettings.json so IConfiguration picks them up.
    /// Called from the Settings UI after the user changes AI configuration.
    /// </summary>
    public void SyncAISettingsToAppSettings()
    {
        // Snapshot AI settings under the lock to avoid reading partially-updated state
        string? provider, apiKey, endpoint, model;
        lock (_lock)
        {
            provider = _settings.AIProvider;
            apiKey = _settings.AIApiKey;
            endpoint = _settings.AIEndpoint;
            model = _settings.AIModel;
        }

        try
        {
            var appSettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            Dictionary<string, object?>? root;

            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                root = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            else
            {
                root = new Dictionary<string, object?>();
            }

            root ??= new Dictionary<string, object?>();

            // Build the AI section based on the selected provider
            var aiSection = new Dictionary<string, object?>();

            if (!string.IsNullOrWhiteSpace(provider))
                aiSection["Provider"] = provider;

            if (string.Equals(provider, "ollama", StringComparison.OrdinalIgnoreCase))
            {
                var ollama = new Dictionary<string, string?>();
                if (!string.IsNullOrWhiteSpace(endpoint))
                    ollama["Endpoint"] = endpoint;
                if (!string.IsNullOrWhiteSpace(model))
                    ollama["Model"] = model;
                aiSection["Ollama"] = ollama;
            }
            else if (string.Equals(provider, "lmstudio", StringComparison.OrdinalIgnoreCase))
            {
                var lmStudio = new Dictionary<string, string?>();
                if (!string.IsNullOrWhiteSpace(endpoint))
                    lmStudio["Endpoint"] = endpoint;
                if (!string.IsNullOrWhiteSpace(apiKey))
                    lmStudio["ApiKey"] = apiKey;
                if (!string.IsNullOrWhiteSpace(model))
                    lmStudio["Model"] = model;
                aiSection["LMStudio"] = lmStudio;
            }
            else if (string.Equals(provider, "claude", StringComparison.OrdinalIgnoreCase))
            {
                var claude = new Dictionary<string, string?>();
                if (!string.IsNullOrWhiteSpace(apiKey))
                    claude["ApiKey"] = apiKey;
                if (!string.IsNullOrWhiteSpace(model))
                    claude["Model"] = model;
                aiSection["Claude"] = claude;
            }
            else if (string.Equals(provider, "openai", StringComparison.OrdinalIgnoreCase))
            {
                var openai = new Dictionary<string, string?>();
                if (!string.IsNullOrWhiteSpace(apiKey))
                    openai["ApiKey"] = apiKey;
                if (!string.IsNullOrWhiteSpace(model))
                    openai["Model"] = model;
                aiSection["OpenAI"] = openai;
            }

            root["AI"] = aiSection;

            var output = System.Text.Json.JsonSerializer.Serialize(root, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(appSettingsPath, output);

            _logger?.LogInformation("AI settings synced to appsettings.json");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to sync AI settings to appsettings.json");
        }
    }

    // ── Internals ──────────────────────────────────────────────────────────

    private void Mutate(Action<UserSettings> action)
    {
        lock (_lock)
        {
            action(_settings);
            Save(_settings);
        }
    }

    private UserSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var loaded = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    _logger?.LogInformation("Settings loaded from {Path}", _filePath);
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load settings from {Path}, trying backup.", _filePath);

            // Try loading from backup
            var backupPath = _filePath + ".backup";
            try
            {
                if (File.Exists(backupPath))
                {
                    var json = File.ReadAllText(backupPath);
                    var loaded = JsonSerializer.Deserialize<UserSettings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        _logger?.LogInformation("Settings restored from backup {Path}", backupPath);
                        return loaded;
                    }
                }
            }
            catch (Exception backupEx)
            {
                _logger?.LogWarning(backupEx, "Backup settings also failed at {Path}.", backupPath);
            }
        }

        _logger?.LogInformation("Using default settings.");
        return new UserSettings();
    }

    /// <summary>Writes settings to disk. Returns false if the write failed.</summary>
    private bool Save(UserSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            var tempPath = _filePath + ".tmp";

            // Write to temp file first
            File.WriteAllText(tempPath, json);

            // Atomic replace — if this fails, the original file is untouched
            File.Move(tempPath, _filePath, overwrite: true);

            // Keep a backup copy after successful write (best-effort)
            try { File.Copy(_filePath, _filePath + ".backup", overwrite: true); }
            catch { /* Backup is best-effort */ }

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save settings to {Path}.", _filePath);
            return false;
        }
    }
}

/// <summary>
/// Serializable settings model. Add new properties with sensible defaults.
/// </summary>
public sealed class UserSettings
{
    public bool SoundEnabled { get; set; } = true;
    public bool Paused { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public List<string> ExcludedProcesses { get; set; } = new();

    /// <summary>
    /// The popup theme. Defaults to System (auto-detect from Windows).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PopupThemeKind Theme { get; set; } = PopupThemeKind.System;

    // ── Action profile ─────────────────────────────────────────────

    /// <summary>The active action visibility profile.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ActionProfile ActiveProfile { get; set; } = ActionProfile.Essentials;

    /// <summary>List of action names currently enabled in the popup.</summary>
    public List<string> EnabledActions { get; set; } = new();

    /// <summary>When true, drag-selecting text auto-copies it to the clipboard.</summary>
    public bool AutoCopyOnDragSelect { get; set; } = false;

    /// <summary>
    /// When true, a gesture only pops if the cursor fits text: an I-beam for a drag,
    /// an I-beam or arrow for a double-click. App-specific cursors never block.
    /// </summary>
    public bool RequireTextCursor { get; set; } = true;

    // ── AI configuration ─────────────────────────────────────────────

    /// <summary>AI provider: "openai", "claude", "lmstudio", or null (auto-detect).</summary>
    public string? AIProvider { get; set; }

    /// <summary>API key for the selected AI provider (OpenAI or Claude). Encrypted on disk via DPAPI.</summary>
    [JsonConverter(typeof(DpapiStringConverter))]
    public string? AIApiKey { get; set; }

    /// <summary>Endpoint URL (required for LM Studio, optional for others).</summary>
    public string? AIEndpoint { get; set; }

    /// <summary>Model name override (e.g., "gpt-4", "claude-3-5-sonnet-20241022").</summary>
    public string? AIModel { get; set; }

    /// <summary>
    /// Global hotkey combination string (e.g., "Ctrl+Shift+C"). Null or empty means no hotkey registered.
    /// </summary>
    public string? GlobalHotkey { get; set; }

    // ── Clipboard history picker (v2.1.0) ────────────────────────────

    /// <summary>
    /// Hotkey that opens the clipboard history picker (default: "Win+Alt+V" — avoids
    /// clobbering the built-in Win+V). Null or empty disables the picker hotkey.
    /// </summary>
    public string? HistoryPickerHotkey { get; set; } = "Win+Alt+V";

    /// <summary>Maximum entries retained in the in-memory + on-disk clipboard history.</summary>
    public int HistoryMaxEntries { get; set; } = 100;

    /// <summary>When true, the persisted clipboard history file is wiped on app exit.</summary>
    public bool ClearHistoryOnExit { get; set; } = false;

    // ── Action UX (v2.2.0) ───────────────────────────────────────────

    /// <summary>
    /// Names of actions pinned to the popup top row. Rendered as icon-only buttons
    /// before the "more ▸" flyout. Capped at <see cref="MaxPinnedActions"/>.
    /// </summary>
    public List<string> PinnedActionIds { get; set; } = new();

    /// <summary>
    /// Local invocation counts per action name. Drives flyout sort order
    /// (after a usage threshold) and is purely on-device — no telemetry.
    /// </summary>
    public Dictionary<string, int> ActionInvocationCounts { get; set; } = new();

    /// <summary>Maximum number of pinned actions that fit in the popup row.</summary>
    public const int MaxPinnedActions = 3;

    /// <summary>Usage count below which the flyout uses default ordering.</summary>
    public const int FlyoutUsageSortThreshold = 3;

    // ── Telemetry (opt-in) ───────────────────────────────────────────

    /// <summary>
    /// When true, anonymous action-invocation events are sent to Plausible Analytics.
    /// Default false. Never sends clipboard content, selected text, or any identifier —
    /// only the action name + app version. See <see cref="TelemetryClient"/>.
    /// </summary>
    public bool EnableUsageTelemetry { get; set; } = false;

    /// <summary>
    /// Optional override for the Plausible site domain. Leave null to use the default.
    /// </summary>
    public string? TelemetryDomain { get; set; }

    // ── Action chains (v3.x) ─────────────────────────────────────────

    /// <summary>
    /// User-defined named sequences of actions. Each chain is exposed in the popup
    /// flyout like any other action; running it applies its steps in order, feeding
    /// each step's clipboard output to the next step's input.
    /// </summary>
    public List<WinClip.Actions.Chains.ActionChain> ActionChains { get; set; } = new();

    public UserSettings Clone() => new()
    {
        SoundEnabled = SoundEnabled,
        Paused = Paused,
        StartWithWindows = StartWithWindows,
        ExcludedProcesses = new List<string>(ExcludedProcesses),
        Theme = Theme,
        ActiveProfile = ActiveProfile,
        EnabledActions = new List<string>(EnabledActions),
        AutoCopyOnDragSelect = AutoCopyOnDragSelect,
        RequireTextCursor = RequireTextCursor,
        AIProvider = AIProvider,
        AIApiKey = AIApiKey,
        AIEndpoint = AIEndpoint,
        AIModel = AIModel,
        GlobalHotkey = GlobalHotkey,
        HistoryPickerHotkey = HistoryPickerHotkey,
        HistoryMaxEntries = HistoryMaxEntries,
        ClearHistoryOnExit = ClearHistoryOnExit,
        PinnedActionIds = new List<string>(PinnedActionIds),
        ActionInvocationCounts = new Dictionary<string, int>(ActionInvocationCounts),
        EnableUsageTelemetry = EnableUsageTelemetry,
        TelemetryDomain = TelemetryDomain,
        ActionChains = ActionChains
            .Select(c => new WinClip.Actions.Chains.ActionChain
            {
                Name = c.Name,
                Description = c.Description,
                ActionIds = new List<string>(c.ActionIds)
            })
            .ToList()
    };
}
