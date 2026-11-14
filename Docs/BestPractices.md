# WinClip Plugin Development Best Practices

This guide covers best practices, patterns, and recommendations for creating professional, high-quality WinClip plugins that users will love.

## Table of Contents

- [Plugin Design Principles](#plugin-design-principles)
- [Code Organization](#code-organization)
- [Performance Optimization](#performance-optimization)
- [Error Handling](#error-handling)
- [User Experience](#user-experience)
- [Security](#security)
- [Testing](#testing)
- [Documentation](#documentation)
- [Maintenance](#maintenance)
- [Common Pitfalls](#common-pitfalls)

---

## Plugin Design Principles

### Single Responsibility

Each plugin should focus on one clear purpose:

**Good Examples:**
- **Text Transformer**: Text case conversions (uppercase, lowercase, title case)
- **URL Utilities**: URL parsing, encoding, validation
- **Developer Tools**: JSON formatting, Base64 encoding/decoding

**Bad Examples:**
- **Swiss Army Knife**: Text formatting + file operations + web search + calculations
  - Too broad, hard to maintain, confusing for users

### Keep It Simple

Start with core functionality, add features incrementally:

```csharp
// ❌ Bad - Too complex for v1.0.0
[Plugin("text-tools", "Text Tools", "1.0.0")]
public class TextToolsPlugin : PluginBase
{
    // 50+ actions for every possible text transformation
}

// ✅ Good - Focused v1.0.0, expand later
[Plugin("text-tools", "Text Tools", "1.0.0")]
public class TextToolsPlugin : PluginBase
{
    [Action("uppercase", "UPPERCASE")]
    public void Uppercase(IActionContext context) { /* ... */ }

    [Action("lowercase", "lowercase")]
    public void Lowercase(IActionContext context) { /* ... */ }

    [Action("title-case", "Title Case")]
    public void TitleCase(IActionContext context) { /* ... */ }
}
```

### Be Predictable

Actions should do what users expect from their names:

```csharp
// ✅ Good - Clear, predictable behavior
[Action("copy-url", "Copy URL")]
public void CopyUrl(IActionContext context)
{
    var url = ExtractUrl(context.Clipboard.GetText());
    context.SetClipboard(url);
    context.ShowNotification("Copied", "URL copied to clipboard");
}

// ❌ Bad - Surprising side effect
[Action("copy-url", "Copy URL")]
public void CopyUrl(IActionContext context)
{
    var url = ExtractUrl(context.Clipboard.GetText());
    context.SetClipboard(url);
    context.OpenUrl(url); // Unexpected! Opens browser too
}
```

### Fail Gracefully

Never crash WinClip. Always handle edge cases:

```csharp
// ✅ Good - Defensive programming
[Action("parse-json", "Parse JSON")]
public void ParseJson(IActionContext context)
{
    try
    {
        var text = context.Clipboard.GetText();

        if (string.IsNullOrWhiteSpace(text))
        {
            context.ShowNotification("No Data", "Clipboard is empty", NotificationType.Warning);
            return;
        }

        var json = JsonSerializer.Deserialize<object>(text);
        // Process json...
    }
    catch (JsonException ex)
    {
        context.ShowNotification("Invalid JSON", ex.Message, NotificationType.Error);
        context.Log($"JSON parsing failed: {ex.Message}", LogLevel.Error);
    }
    catch (Exception ex)
    {
        context.ShowNotification("Error", "Failed to process JSON", NotificationType.Error);
        context.Log($"Unexpected error: {ex}", LogLevel.Error);
    }
}
```

---

## Code Organization

### Project Structure

Organize code logically for maintainability:

```
MyPlugin/
├── MyPlugin.csproj
├── README.md
├── plugin.json
│
├── Core/
│   ├── MyPlugin.cs           # Main plugin class
│   └── PluginConfiguration.cs
│
├── Actions/
│   ├── TextActions.cs        # Related actions grouped
│   ├── FileActions.cs
│   └── WebActions.cs
│
├── Services/
│   ├── ITextProcessor.cs     # Service interfaces
│   └── TextProcessor.cs      # Service implementations
│
├── Models/
│   └── ProcessResult.cs      # Data models
│
└── Utilities/
    └── ValidationHelper.cs    # Shared utilities
```

### Separate Concerns

Don't put all code in action methods:

```csharp
// ❌ Bad - Business logic in action method
[Action("format-text", "Format Text")]
public void FormatText(IActionContext context)
{
    var text = context.Clipboard.GetText();

    // 100 lines of complex formatting logic...
    var formatted = text.Replace(...).Replace(...); // etc.

    context.PasteText(formatted);
}

// ✅ Good - Separated concerns
[Action("format-text", "Format Text")]
public void FormatText(IActionContext context)
{
    var text = context.Clipboard.GetText();
    var formatted = _textFormatter.Format(text);

    if (formatted != null)
    {
        context.PasteText(formatted);
        context.ShowNotification("Success", "Text formatted", NotificationType.Success);
    }
    else
    {
        context.ShowNotification("Error", "Failed to format text", NotificationType.Error);
    }
}

// Business logic in separate class
public class TextFormatter
{
    public string Format(string text)
    {
        // Complex formatting logic here
        // Easily testable without WinClip context
    }
}
```

### Use Dependency Injection

Make code testable and maintainable:

```csharp
[Plugin("web-search", "Web Search", "1.0.0")]
public class WebSearchPlugin : PluginBase
{
    private readonly IHttpClient _httpClient;
    private readonly ISearchEngine _searchEngine;

    // Constructor injection for testing
    public WebSearchPlugin(IHttpClient httpClient = null, ISearchEngine searchEngine = null)
    {
        _httpClient = httpClient ?? new DefaultHttpClient();
        _searchEngine = searchEngine ?? new GoogleSearchEngine();
    }

    [Action("search", "Search")]
    public void Search(IActionContext context)
    {
        var query = context.Clipboard.GetText();
        var url = _searchEngine.BuildSearchUrl(query);
        context.OpenUrl(url);
    }
}

// Easy to test with mocks
[Fact]
public void Search_BuildsCorrectUrl()
{
    var mockSearchEngine = new MockSearchEngine();
    var plugin = new WebSearchPlugin(searchEngine: mockSearchEngine);

    // Test without actual WinClip context
}
```

---

## Performance Optimization

### Fast ShouldEnable

`ShouldEnable()` is called frequently - keep it fast:

```csharp
// ❌ Bad - Slow operations
public override bool ShouldEnable(IClipboardContext context)
{
    var text = context.GetText();

    // Network call - VERY SLOW!
    return IsUrlReachable(text);
}

// ✅ Good - Fast local check
public override bool ShouldEnable(IClipboardContext context)
{
    var text = context.GetText();

    // Quick regex check - FAST
    return !string.IsNullOrWhiteSpace(text) &&
           Uri.IsWellFormedUriString(text, UriKind.Absolute);
}
```

**Guidelines:**
- **< 1ms**: Ideal
- **< 10ms**: Acceptable
- **> 50ms**: Too slow, will make UI sluggish

### Lazy Initialization

Initialize expensive resources only when needed:

```csharp
// ❌ Bad - Initialized even if never used
public class MyPlugin : PluginBase
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly Dictionary<string, string> _cache = LoadCache(); // Expensive!
}

// ✅ Good - Lazy initialization
public class MyPlugin : PluginBase
{
    private HttpClient _httpClient;
    private Dictionary<string, string> _cache;

    private HttpClient HttpClient => _httpClient ??= new HttpClient();

    private Dictionary<string, string> Cache => _cache ??= LoadCache();
}
```

### Cache Expensive Operations

Cache results that don't change:

```csharp
private readonly Dictionary<string, string> _processCache = new Dictionary<string, string>();

[Action("process", "Process Text")]
public void ProcessText(IActionContext context)
{
    var text = context.Clipboard.GetText();

    // Check cache first
    if (_processCache.TryGetValue(text, out var cached))
    {
        context.PasteText(cached);
        return;
    }

    // Expensive operation
    var result = ExpensiveProcessing(text);

    // Cache for next time
    _processCache[text] = result;

    context.PasteText(result);
}
```

**Note**: Be mindful of memory. Consider cache size limits:

```csharp
private readonly LruCache<string, string> _cache = new LruCache<string, string>(maxSize: 100);
```

### Asynchronous Operations

Use async for I/O operations:

```csharp
// ❌ Bad - Blocks UI thread
[Action("fetch", "Fetch Content")]
public void FetchContent(IActionContext context)
{
    var url = context.Clipboard.GetText();
    var content = httpClient.GetStringAsync(url).Result; // Blocks!
    context.SetClipboard(content);
}

// ✅ Good - Asynchronous
[Action("fetch", "Fetch Content")]
public async void FetchContent(IActionContext context)
{
    var url = context.Clipboard.GetText();

    try
    {
        var content = await httpClient.GetStringAsync(url);
        context.SetClipboard(content);
        context.ShowNotification("Success", "Content fetched", NotificationType.Success);
    }
    catch (Exception ex)
    {
        context.ShowNotification("Error", "Failed to fetch content", NotificationType.Error);
    }
}
```

### Avoid Memory Leaks

Clean up resources properly:

```csharp
public class MyPlugin : PluginBase, IDisposable
{
    private HttpClient _httpClient;
    private Timer _timer;

    public void Dispose()
    {
        _httpClient?.Dispose();
        _timer?.Dispose();
    }
}

// Or use 'using' statements
[Action("download", "Download")]
public async void Download(IActionContext context)
{
    using var client = new HttpClient();
    var content = await client.GetStringAsync(url);
    // Client automatically disposed
}
```

---

## Error Handling

### Wrap All Actions

Every action should have error handling:

```csharp
[Action("my-action", "My Action")]
public void MyAction(IActionContext context)
{
    try
    {
        // Your action logic here
        var result = DoWork(context);
        context.ShowNotification("Success", "Action completed", NotificationType.Success);
    }
    catch (ArgumentException ex)
    {
        // Handle specific exceptions
        context.ShowNotification("Invalid Input", ex.Message, NotificationType.Warning);
        context.Log($"Invalid argument: {ex.Message}", LogLevel.Warning);
    }
    catch (Exception ex)
    {
        // Catch-all for unexpected errors
        context.ShowNotification("Error", "Action failed", NotificationType.Error);
        context.Log($"Unexpected error in MyAction: {ex}", LogLevel.Error);
    }
}
```

### Validate Input

Always validate clipboard data:

```csharp
[Action("open-file", "Open File")]
public void OpenFile(IActionContext context)
{
    var files = context.Clipboard.GetFiles();

    // Validate files exist
    if (files == null || files.Length == 0)
    {
        context.ShowNotification("No Files", "Clipboard contains no files", NotificationType.Warning);
        return;
    }

    var file = files[0];

    // Validate file exists
    if (!File.Exists(file))
    {
        context.ShowNotification("File Not Found", $"File does not exist: {file}", NotificationType.Error);
        return;
    }

    // Validate file type if needed
    var extension = Path.GetExtension(file).ToLower();
    if (!_supportedExtensions.Contains(extension))
    {
        context.ShowNotification("Unsupported File", $"File type {extension} is not supported", NotificationType.Warning);
        return;
    }

    // Safe to proceed
    OpenFileInApp(file);
}
```

### Provide Helpful Error Messages

Make errors actionable:

```csharp
// ❌ Bad - Vague error
context.ShowNotification("Error", "Something went wrong", NotificationType.Error);

// ✅ Good - Specific and actionable
context.ShowNotification(
    "Network Error",
    "Failed to connect to API. Check your internet connection.",
    NotificationType.Error
);

// ✅ Even better - With recovery suggestion
context.ShowNotification(
    "JSON Parse Error",
    "Invalid JSON on line 5. Make sure all strings are quoted.",
    NotificationType.Error
);
```

### Log Appropriately

Use appropriate log levels:

```csharp
public void MyAction(IActionContext context)
{
    context.Log("Action started", LogLevel.Debug); // Detailed debugging

    try
    {
        var result = ProcessData();
        context.Log($"Processed {result.Count} items", LogLevel.Info); // Normal info

        if (result.HasWarnings)
        {
            context.Log("Some items had warnings", LogLevel.Warning); // Potential issues
        }
    }
    catch (Exception ex)
    {
        context.Log($"Critical error: {ex}", LogLevel.Error); // Errors
    }
}
```

---

## User Experience

### Clear Action Names

Use descriptive, action-oriented names:

```csharp
// ❌ Bad - Unclear
[Action("proc", "Proc")]              // What does it do?
[Action("do-it", "Do It")]            // Do what?
[Action("handler", "Handler")]        // Not action-oriented

// ✅ Good - Clear and descriptive
[Action("uppercase", "Convert to UPPERCASE")]
[Action("open-browser", "Open in Browser")]
[Action("copy-filename", "Copy Filename")]
```

### Logical Action Order

Order actions logically:

```csharp
// ✅ Good - Related actions grouped, most common first
[Action("uppercase", "UPPERCASE", Order = 1)]
[Action("lowercase", "lowercase", Order = 2)]
[Action("title-case", "Title Case", Order = 3)]
[Action("capitalize", "Capitalize First Letter", Order = 4)]
[Action("reverse", "Reverse Text", Order = 10)]  // Less common
[Action("scramble", "Scramble Text", Order = 11)] // Least common
```

### Informative Notifications

Always notify users of results:

```csharp
// ✅ Good - Informs user of result
[Action("shorten-url", "Shorten URL")]
public async void ShortenUrl(IActionContext context)
{
    var url = context.Clipboard.GetText();

    context.ShowNotification("Processing", "Shortening URL...", NotificationType.Info);

    try
    {
        var shortened = await _urlShortener.ShortenAsync(url);
        context.SetClipboard(shortened);
        context.ShowNotification("Success", $"URL shortened: {shortened}", NotificationType.Success);
    }
    catch (Exception ex)
    {
        context.ShowNotification("Error", "Failed to shorten URL", NotificationType.Error);
    }
}
```

### Respect User Expectations

Don't surprise users:

```csharp
// ❌ Bad - Unexpected modification
[Action("copy-text", "Copy Text")]
public void CopyText(IActionContext context)
{
    var text = context.Clipboard.GetText();
    text = text.ToUpper(); // Unexpected transformation!
    context.SetClipboard(text);
}

// ✅ Good - Does what name suggests
[Action("copy-uppercase", "Copy as UPPERCASE")]
public void CopyUppercase(IActionContext context)
{
    var text = context.Clipboard.GetText();
    context.SetClipboard(text.ToUpper());
}
```

### Auto-dismiss When Appropriate

Close popup for quick actions:

```csharp
[Action("quick-copy", "Quick Copy")]
public void QuickCopy(IActionContext context)
{
    var text = ProcessText(context.Clipboard.GetText());
    context.SetClipboard(text);
    context.ShowNotification("Copied", "Text processed and copied");
    context.DismissPopup(); // Close menu automatically
}

// But keep open for actions that need context
[Action("show-info", "Show Information")]
public void ShowInfo(IActionContext context)
{
    var info = GetDetailedInfo(context);
    context.ShowNotification("Information", info, NotificationType.Info);
    // Don't dismiss - user may want to read the info
}
```

---

## Security

### Validate All Input

Never trust clipboard data:

```csharp
[Action("execute-command", "Execute Command")]
public void ExecuteCommand(IActionContext context)
{
    var command = context.Clipboard.GetText();

    // ❌ DANGEROUS - No validation
    context.ExecuteCommandAsync(command);

    // ✅ Safe - Whitelist approach
    var allowedCommands = new[] { "notepad", "calc", "explorer" };
    var commandName = command.Split(' ')[0].ToLower();

    if (!allowedCommands.Contains(commandName))
    {
        context.ShowNotification("Security Error", "Command not allowed", NotificationType.Error);
        return;
    }

    context.ExecuteCommandAsync(command);
}
```

### Escape Special Characters

Always escape user input in URLs and commands:

```csharp
[Action("search", "Search Google")]
public void SearchGoogle(IActionContext context)
{
    var query = context.Clipboard.GetText();

    // ✅ Good - Properly escaped
    var encoded = Uri.EscapeDataString(query);
    context.OpenUrl($"https://www.google.com/search?q={encoded}");

    // ❌ Bad - Not escaped, vulnerable to injection
    // context.OpenUrl($"https://www.google.com/search?q={query}");
}
```

### Handle Sensitive Data Carefully

Be careful with credentials and personal information:

```csharp
// ❌ Bad - Logs sensitive data
public void ProcessCredential(IActionContext context)
{
    var password = context.Clipboard.GetText();
    context.Log($"Processing password: {password}", LogLevel.Debug); // LEAKED!
}

// ✅ Good - No logging of sensitive data
public void ProcessCredential(IActionContext context)
{
    var password = context.Clipboard.GetText();
    context.Log("Processing credential", LogLevel.Debug); // Safe

    // Process without logging actual value
    var hash = ComputeHash(password);
}
```

### Audit Dependencies

Review all third-party libraries:

```csharp
// Before adding NuGet package
// 1. Check package reputation (downloads, stars)
// 2. Review source code if possible
// 3. Check for known vulnerabilities
// 4. Keep dependencies minimal
// 5. Update regularly
```

### Network Requests

Be transparent about network activity:

```csharp
[Plugin("weather", "Weather Lookup", "1.0.0",
    Description = "Looks up weather information online. Requires internet connection.")]
public class WeatherPlugin : PluginBase
{
    [Action("get-weather", "Get Weather")]
    public async void GetWeather(IActionContext context)
    {
        var location = context.Clipboard.GetText();

        // Inform user of network request
        context.ShowNotification("Fetching", "Getting weather data...", NotificationType.Info);

        try
        {
            var weather = await _weatherApi.GetWeatherAsync(location);
            context.ShowNotification("Weather", weather.Summary, NotificationType.Success);
        }
        catch (HttpRequestException)
        {
            context.ShowNotification("Network Error", "Could not reach weather service", NotificationType.Error);
        }
    }
}
```

---

## Testing

### Unit Test Business Logic

Test core functionality without WinClip context:

```csharp
public class TextFormatterTests
{
    [Fact]
    public void Format_ConvertsToUppercase()
    {
        var formatter = new TextFormatter();
        var result = formatter.ToUppercase("hello");
        Assert.Equal("HELLO", result);
    }

    [Theory]
    [InlineData("hello world", "Hello World")]
    [InlineData("HELLO WORLD", "Hello World")]
    [InlineData("hElLo WoRlD", "Hello World")]
    public void Format_ConvertsTitleCase(string input, string expected)
    {
        var formatter = new TextFormatter();
        var result = formatter.ToTitleCase(input);
        Assert.Equal(expected, result);
    }
}
```

### Integration Test Actions

Test actions with mock contexts:

```csharp
[Fact]
public void Uppercase_PastesUppercaseText()
{
    // Arrange
    var plugin = new TextTransformPlugin();
    var clipboard = new MockClipboardContext(text: "hello");
    var context = new MockActionContext { Clipboard = clipboard };

    // Act
    plugin.Uppercase(context);

    // Assert
    Assert.Equal("HELLO", context.PastedText);
}
```

### Test Edge Cases

Always test boundary conditions:

```csharp
[Theory]
[InlineData(null)]           // Null input
[InlineData("")]             // Empty string
[InlineData("   ")]          // Whitespace only
[InlineData("a")]            // Single character
[InlineData("verylongstringwithoutspaces...")]  // Very long string
public void Action_HandlesEdgeCases(string input)
{
    var plugin = new MyPlugin();
    var clipboard = new MockClipboardContext(text: input);
    var context = new MockActionContext { Clipboard = clipboard };

    // Should not throw
    plugin.MyAction(context);

    // Should show appropriate notification or handle gracefully
    Assert.NotEmpty(context.Notifications);
}
```

### Performance Tests

Test that actions are fast enough:

```csharp
[Fact]
public void ShouldEnable_CompletesQuickly()
{
    var plugin = new MyPlugin();
    var clipboard = new MockClipboardContext(text: "test");

    var stopwatch = Stopwatch.StartNew();
    var result = plugin.ShouldEnable(clipboard);
    stopwatch.Stop();

    Assert.True(stopwatch.ElapsedMilliseconds < 10,
        $"ShouldEnable took {stopwatch.ElapsedMilliseconds}ms (should be < 10ms)");
}
```

---

## Documentation

### XML Documentation Comments

Document all public APIs:

```csharp
/// <summary>
/// Transforms the clipboard text to uppercase.
/// </summary>
/// <param name="context">The action context providing clipboard access.</param>
/// <remarks>
/// This action converts all characters in the clipboard text to uppercase
/// and pastes the result back into the active application.
/// </remarks>
/// <example>
/// Input: "hello world"
/// Output: "HELLO WORLD"
/// </example>
[Action("uppercase", "Convert to UPPERCASE")]
public void Uppercase(IActionContext context)
{
    // Implementation
}
```

### README.md

Every plugin needs a comprehensive README:

```markdown
# My Plugin

Brief description of what the plugin does.

## Features

- List key features
- What makes it useful
- What problems it solves

## Installation

Step-by-step installation instructions.

## Usage

Clear examples of how to use each action.

## Configuration

If applicable, explain settings.

## Requirements

- Dependencies
- System requirements
- WinClip version

## Known Issues

List any known limitations or bugs.

## Contributing

How others can contribute.

## License

License information.
```

### Inline Comments

Comment complex logic:

```csharp
// ✅ Good comments
public string ProcessText(string text)
{
    // Remove zero-width characters that can cause display issues
    text = Regex.Replace(text, @"\p{Cf}", "");

    // Normalize Unicode to NFC form to prevent duplicate characters
    text = text.Normalize(NormalizationForm.FormC);

    // Replace multiple spaces with single space, preserving intentional formatting
    text = Regex.Replace(text, @" {2,}", " ");

    return text;
}
```

### CHANGELOG.md

Maintain version history:

```markdown
# Changelog

## [1.2.0] - 2024-01-20

### Added
- New "reverse text" action
- Configuration dialog for custom settings

### Changed
- Improved performance of text processing by 50%
- Updated UI icons

### Fixed
- Fixed crash when clipboard contains null
- Fixed encoding issue with Unicode characters

## [1.1.0] - 2024-01-10
...
```

---

## Maintenance

### Respond to Issues Quickly

- **Critical bugs**: Fix within 24-48 hours
- **Regular bugs**: Fix within 1 week
- **Feature requests**: Acknowledge within 1 week

### Keep Dependencies Updated

```bash
# Regularly update NuGet packages
dotnet list package --outdated
dotnet add package PackageName --version NewVersion
```

### Monitor Performance

Track performance over time:

```csharp
#if DEBUG
private readonly Stopwatch _stopwatch = new Stopwatch();

[Action("my-action", "My Action")]
public void MyAction(IActionContext context)
{
    _stopwatch.Restart();

    // Action logic

    _stopwatch.Stop();
    context.Log($"Action took {_stopwatch.ElapsedMilliseconds}ms", LogLevel.Debug);
}
#endif
```

### Plan for Deprecation

When removing features:

1. **Mark as deprecated** in version N
2. **Log warnings** when used
3. **Update documentation** with migration path
4. **Remove** in version N+1 (major version bump)

```csharp
[Obsolete("Use NewAction instead. This action will be removed in v2.0.0")]
[Action("old-action", "Old Action (Deprecated)")]
public void OldAction(IActionContext context)
{
    context.Log("Warning: OldAction is deprecated, use NewAction instead", LogLevel.Warning);
    // Implementation
}
```

---

## Common Pitfalls

### Pitfall 1: Blocking UI Thread

**Problem**: Long-running operations freeze WinClip

```csharp
// ❌ Bad
[Action("download", "Download")]
public void Download(IActionContext context)
{
    var content = httpClient.GetStringAsync(url).Result; // Blocks!
}

// ✅ Good
[Action("download", "Download")]
public async void Download(IActionContext context)
{
    var content = await httpClient.GetStringAsync(url);
}
```

### Pitfall 2: Memory Leaks

**Problem**: Not disposing resources

```csharp
// ❌ Bad
private void ProcessFile(string path)
{
    var stream = File.OpenRead(path); // Never disposed
    // Process stream
}

// ✅ Good
private void ProcessFile(string path)
{
    using var stream = File.OpenRead(path);
    // Process stream
} // Automatically disposed
```

### Pitfall 3: Hardcoded Paths

**Problem**: Plugin breaks on different systems

```csharp
// ❌ Bad
var configPath = @"C:\Users\John\AppData\WinClip\config.json";

// ✅ Good
var dataDir = context.GetPluginDataDirectory("my-plugin");
var configPath = Path.Combine(dataDir, "config.json");
```

### Pitfall 4: No Error Handling

**Problem**: Exceptions crash WinClip

```csharp
// ❌ Bad
[Action("parse", "Parse Data")]
public void Parse(IActionContext context)
{
    var json = JsonSerializer.Deserialize<Data>(text); // Can throw!
}

// ✅ Good
[Action("parse", "Parse Data")]
public void Parse(IActionContext context)
{
    try
    {
        var json = JsonSerializer.Deserialize<Data>(text);
    }
    catch (JsonException ex)
    {
        context.ShowNotification("Error", "Invalid JSON", NotificationType.Error);
    }
}
```

### Pitfall 5: Console Output

**Problem**: Console.WriteLine doesn't work in WinClip

```csharp
// ❌ Bad
Console.WriteLine("Action executed"); // Nobody sees this

// ✅ Good
context.Log("Action executed", LogLevel.Info);
```

### Pitfall 6: Modifying Clipboard Unexpectedly

**Problem**: Users lose their clipboard data

```csharp
// ❌ Bad - "Open URL" shouldn't modify clipboard
[Action("open-url", "Open URL")]
public void OpenUrl(IActionContext context)
{
    var url = context.Clipboard.GetText();
    context.SetClipboard(""); // Why clear?!
    context.OpenUrl(url);
}

// ✅ Good - Only opens URL
[Action("open-url", "Open URL")]
public void OpenUrl(IActionContext context)
{
    var url = context.Clipboard.GetText();
    context.OpenUrl(url);
    context.DismissPopup();
}
```

### Pitfall 7: Not Testing Edge Cases

**Problem**: Plugin crashes with unexpected input

```csharp
// ❌ Bad - No validation
public void ProcessUrl(IActionContext context)
{
    var text = context.Clipboard.GetText();
    var uri = new Uri(text); // Crashes if not valid URL
}

// ✅ Good - Validates first
public void ProcessUrl(IActionContext context)
{
    var text = context.Clipboard.GetText();

    if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
    {
        context.ShowNotification("Invalid URL", "Clipboard does not contain a valid URL", NotificationType.Warning);
        return;
    }

    // Process uri safely
}
```

### Pitfall 8: Poor Action Names

**Problem**: Users don't understand what actions do

```csharp
// ❌ Bad - Unclear names
[Action("proc", "Proc")]
[Action("do-thing", "Do Thing")]
[Action("util1", "Util 1")]

// ✅ Good - Clear, descriptive names
[Action("format-json", "Format JSON")]
[Action("encode-base64", "Encode as Base64")]
[Action("copy-without-formatting", "Copy Without Formatting")]
```

---

## Summary

**Key Takeaways:**

1. **Keep it simple** - Focus on one thing, do it well
2. **Be fast** - Optimize ShouldEnable and actions
3. **Handle errors** - Never crash WinClip
4. **Validate input** - Never trust clipboard data
5. **Inform users** - Clear names and notifications
6. **Test thoroughly** - Unit tests, edge cases, performance
7. **Document well** - README, comments, CHANGELOG
8. **Maintain actively** - Fix bugs quickly, keep updated

---

## Next Steps

- **[Testing Guide](./Testing.md)** - Learn testing strategies
- **[Publishing Guide](./Publishing.md)** - Share your plugin
- **[Plugin API Reference](./PluginAPI.md)** - Complete API documentation
- **[Example Plugins](../Examples/)** - Study working examples

---

**Build Quality Plugins!** 🌟

Following these best practices will help you create plugins that users love and that stand the test of time.
