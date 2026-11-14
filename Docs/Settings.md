# WinClip Settings Reference

## settings.json

**Location:** `%APPDATA%\WinClip\settings.json`

Thread-safe JSON managed by `SettingsManager`. All writes are atomic (write to `.tmp`, then `File.Move`). A `.backup` copy is maintained after each successful write.

### Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `soundEnabled` | `bool` | `true` | Play pop sound on clipboard capture |
| `paused` | `bool` | `false` | Temporarily disable popup |
| `startWithWindows` | `bool` | `false` | Launch on Windows startup |
| `excludedProcesses` | `string[]` | `[]` | Process names to ignore (lowercase) |
| `theme` | `string` | `"System"` | Popup theme: `System`, `Light`, `Dark`, `Nord`, `Solarized`, `Dracula`, `Monokai`, `GruvboxDark`, `CatppuccinMocha`, `TokyoNight`, `OneDark`, `Everforest`, `RosePine` |
| `activeProfile` | `string` | `"Essentials"` | Action visibility preset: `Essentials`, `Developer`, `Writer`, `All`, `Custom` |
| `enabledActions` | `string[]` | `[]` | Action names enabled when using `Custom` profile |
| `autoCopyOnDragSelect` | `bool` | `false` | Auto-copy text on drag selection |
| `aiProvider` | `string?` | `null` | AI provider: `"openai"`, `"claude"`, `"lmstudio"` |
| `aiApiKey` | `string?` | `null` | API key for the selected provider |
| `aiEndpoint` | `string?` | `null` | Endpoint URL (required for LM Studio) |
| `aiModel` | `string?` | `null` | Model name override |

### Security Note

API keys are stored in plaintext. On Windows, `%APPDATA%` is user-only readable by default, but the file is not encrypted. Users should be aware that any process running as their user can read the settings file.

---

## snippets.json

**Location:** `%APPDATA%\WinClip\snippets.json`

Managed by `SnippetStore`. Stores user-defined text snippets as a flat JSON object.

```json
{
  "greeting": "Hello, thanks for reaching out!",
  "sig": "Best regards,\nAlex"
}
```

Key = snippet name, value = snippet text. Atomic writes via the same temp-file pattern.

---

## appsettings.json

**Location:** Application directory (next to `WinClip.exe`)

Standard .NET `IConfiguration` file. AI settings from `settings.json` are synced here by `SettingsManager.SyncAISettingsToAppSettings()` so the DI-registered AI services can read them via `IConfiguration`.

```json
{
  "AI": {
    "Provider": "openai",
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4"
    }
  }
}
```

Provider-specific sections: `OpenAI`, `Claude`, `LMStudio`. Only the active provider's section is populated. See `Docs/AISetup.md` for provider configuration details.
