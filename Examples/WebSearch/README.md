# Web Search Plugin

A plugin demonstrating external integration by launching web searches in the default browser. This example shows how to interact with external applications and services from your WinClip plugin.

## What This Example Demonstrates

- **External Integration**: Opening URLs in the default browser
- **Multiple Search Engines**: Supporting different search providers
- **URL Encoding**: Properly encoding search queries for URLs
- **Error Handling**: Gracefully handling browser launch failures
- **Conditional Enablement**: Only enabled when text is in the clipboard

## Code Overview

### Plugin Class

```csharp
[Plugin("com.winclip.examples.websearch", "Web Search", "1.0.0",
    Description = "Search the web using various search engines",
    Author = "WinClip Team")]
public class WebSearchPlugin : PluginBase
{
    public override bool ShouldEnable(IClipboardContext context)
    {
        // Only enable when clipboard contains text
        return context.HasText;
    }

    [Action("search-google", "Search Google", Order = 1)]
    public void SearchGoogle(IActionContext context)
    {
        string? text = context.Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            context.ShowNotification(
                "Web Search",
                "No text available to search",
                NotificationType.Warning);
            return;
        }

        string encodedQuery = Uri.EscapeDataString(text);
        string url = $"https://www.google.com/search?q={encodedQuery}";
        OpenUrl(url, context);
    }

    // Additional search engine methods...
}
```

### Key Components

1. **Conditional Enablement**: Plugin only activates when text is in clipboard
2. **URL Encoding**: Uses `Uri.EscapeDataString()` to safely encode search queries
3. **Process.Start()**: Launches the default browser with the search URL
4. **Error Handling**: Try-catch blocks to handle browser launch failures
5. **Multiple Actions**: Six different search engines (Google, Bing, DuckDuckGo, Stack Overflow, GitHub, Wikipedia)

## Supported Search Engines

- **Google**: General web search
- **Bing**: Microsoft's search engine
- **DuckDuckGo**: Privacy-focused search
- **Stack Overflow**: Developer Q&A search
- **GitHub**: Repository and code search
- **Wikipedia**: Encyclopedia search

## Building the Plugin

```bash
cd Examples/WebSearch
dotnet build
```

The compiled plugin will be in `bin/Debug/net8.0/WebSearch.dll`.

## Testing the Plugin

### Option 1: Using the Test Harness

```bash
dotnet run --project ../../WinClip.SDK.TestHarness -- --plugin bin/Debug/net8.0/WebSearch.dll
```

### Option 2: Manual Installation

1. Build the plugin
2. Copy `WebSearch.dll` and `plugin.json` to your WinClip plugins directory
3. Restart WinClip
4. Copy some text to your clipboard (e.g., "WinClip clipboard manager")
5. Trigger the clipboard popup
6. Look for "Web Search" plugin actions
7. Click any search engine action to open the search in your browser

## How It Works

1. **Text Selection**: User copies text to clipboard
2. **Plugin Activation**: `ShouldEnable()` returns `true` because clipboard has text
3. **Action Selection**: User selects a search engine action
4. **URL Construction**: Search URL is built with encoded query parameter
5. **Browser Launch**: `Process.Start()` opens the URL in the default browser
6. **Feedback**: User sees a success notification

## Implementation Details

### URL Encoding

The plugin uses `Uri.EscapeDataString()` to properly encode search queries:

```csharp
string encodedQuery = Uri.EscapeDataString(text);
string url = $"https://www.google.com/search?q={encodedQuery}";
```

This ensures special characters (spaces, symbols, unicode) are properly handled in URLs.

### Opening URLs

The `OpenUrl()` helper method uses `Process.Start()` with `UseShellExecute = true`:

```csharp
Process.Start(new ProcessStartInfo
{
    FileName = url,
    UseShellExecute = true
});
```

This launches the URL in the user's default browser, regardless of which browser they use.

### Error Handling

The plugin includes try-catch blocks to handle failures:

```csharp
try
{
    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
    context.ShowNotification("Web Search", "Search opened in browser", NotificationType.Success);
}
catch (Exception ex)
{
    context.Log($"Failed to open URL: {ex.Message}", LogLevel.Error);
    context.ShowNotification("Web Search", $"Failed to open browser: {ex.Message}", NotificationType.Error);
}
```

## Patterns Demonstrated

### 1. External Process Integration

Shows how to launch external applications:

```csharp
Process.Start(new ProcessStartInfo
{
    FileName = url,
    UseShellExecute = true
});
```

### 2. Query Parameter Encoding

Demonstrates proper URL encoding:

```csharp
string encodedQuery = Uri.EscapeDataString(text);
```

### 3. Multiple Related Actions

Groups related functionality (different search engines) in one plugin with ordered actions:

```csharp
[Action("search-google", "Search Google", Order = 1)]
[Action("search-bing", "Search Bing", Order = 2)]
// etc...
```

### 4. Defensive Programming

Validates input before processing:

```csharp
if (string.IsNullOrWhiteSpace(text))
{
    context.ShowNotification("Web Search", "No text available to search", NotificationType.Warning);
    return;
}
```

## Customization Ideas

Try extending this plugin to:

- Add more search engines (YouTube, Amazon, Twitter/X, etc.)
- Add language-specific Wikipedia searches
- Support custom search engine configuration
- Add keyboard shortcuts for favorite search engines
- Detect URLs in clipboard and offer "Open in Browser" action
- Add domain-specific searches (e.g., search on current website)
- Support site-specific searches (e.g., `site:stackoverflow.com {query}`)

## Real-World Use Cases

1. **Quick Research**: Highlight text and quickly search without copying/pasting
2. **Code Lookup**: Search error messages on Stack Overflow
3. **Package Discovery**: Search GitHub for libraries and tools
4. **Documentation**: Search Wikipedia for technical concepts
5. **Multi-Engine Comparison**: Quickly compare search results across engines

## Security Considerations

This plugin demonstrates safe URL handling:

- Uses `Uri.EscapeDataString()` to prevent injection attacks
- Validates input before processing
- Uses `UseShellExecute = true` for proper browser launching
- Handles exceptions gracefully

When building similar plugins:
- Always encode user input before including in URLs
- Validate and sanitize clipboard data
- Handle network/browser failures gracefully
- Log errors for debugging

## Next Steps

After understanding this example, check out:

- **[Getting Started Guide](../../Docs/GettingStarted.md)**: Complete tutorial on plugin development
- **[Plugin API Reference](../../Docs/PluginAPI.md)**: Detailed API documentation
- **[Best Practices](../../Docs/BestPractices.md)**: Professional development guidelines

## Key Takeaways

1. Plugins can integrate with external applications using `Process.Start()`
2. Always encode user input when constructing URLs
3. Use `UseShellExecute = true` to open URLs in the default browser
4. Provide multiple related actions for better user experience
5. Handle errors gracefully and provide user feedback
6. Validate clipboard data before processing

Happy plugin development! 🚀
