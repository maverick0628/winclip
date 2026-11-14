# Testing and Debugging WinClip Plugins

This guide covers strategies, tools, and techniques for testing and debugging your WinClip plugins effectively.

## Table of Contents

- [Testing Strategies](#testing-strategies)
- [Local Testing with Test Harness](#local-testing-with-test-harness)
- [Debugging Techniques](#debugging-techniques)
- [Integration Testing](#integration-testing)
- [Performance Testing](#performance-testing)
- [Troubleshooting Common Issues](#troubleshooting-common-issues)
- [Best Practices](#best-practices)

> **📘 SDK Developers**: For complete end-to-end SDK workflow testing (packaging, template installation, plugin creation), see the [End-to-End Testing Guide](./EndToEndTesting.md).

---

## Testing Strategies

### Test-Driven Development

Consider writing tests before implementing your plugin features:

1. **Define expected behavior** - What should your action do?
2. **Write test cases** - Create test scenarios
3. **Implement the feature** - Write the plugin code
4. **Verify with tests** - Ensure it works as expected

### Testing Pyramid

Layer your testing approach:

```
        /\
       /  \    Manual Testing (in WinClip)
      /____\
     /      \  Integration Tests (with Test Harness)
    /________\
   /          \ Unit Tests (for complex logic)
  /____________\
```

- **Unit Tests**: Test individual methods and logic
- **Integration Tests**: Test plugin actions with mock contexts
- **Manual Tests**: Test in actual WinClip application

---

## Local Testing with Test Harness

The WinClip SDK includes a test harness for rapid testing without installing plugins into WinClip.

> **📖 Detailed Guide:** For comprehensive test harness documentation including all commands, debugging setup, and troubleshooting, see the **[Test Harness README](../WinClip.SDK.TestHarness/README.md)**.

### Basic Usage

```bash
# Build your plugin
dotnet build

# Run test harness
dotnet run --project path/to/WinClip.SDK.TestHarness \
    --plugin ./bin/Debug/net8.0/MyPlugin.dll
```

### Interactive Testing

The test harness provides an interactive menu:

```
WinClip Plugin Test Harness
===========================
Loaded Plugin: My Text Transform (1.0.0)

Available Actions:
  1. uppercase - Convert to UPPERCASE
  2. lowercase - Convert to lowercase
  3. title-case - Title Case

Enter action number (or 'q' to quit): 1
Enter test clipboard content: hello world
---
Action Result: HELLO WORLD
---
```

### Test Harness Features

#### Custom Clipboard Data

Set up specific test scenarios:

```bash
# Test with text
dotnet run --project WinClip.SDK.TestHarness \
    --plugin ./bin/Debug/net8.0/MyPlugin.dll \
    --text "sample text content"

# Test with files
dotnet run --project WinClip.SDK.TestHarness \
    --plugin ./bin/Debug/net8.0/MyPlugin.dll \
    --files "C:\file1.txt,C:\file2.txt"

# Test with image
dotnet run --project WinClip.SDK.TestHarness \
    --plugin ./bin/Debug/net8.0/MyPlugin.dll \
    --image "C:\test.png"
```

#### Action Validation

The test harness validates:
- Plugin loads correctly
- All actions are discovered
- Action method signatures are correct
- Metadata is valid
- Exceptions are caught and reported

#### Output Inspection

View detailed action execution:

```bash
dotnet run --project WinClip.SDK.TestHarness \
    --plugin ./bin/Debug/net8.0/MyPlugin.dll \
    --verbose
```

Verbose mode shows:
- Plugin initialization steps
- ShouldEnable() evaluation
- Action execution details
- Context method calls
- Exceptions and stack traces
- Performance metrics

---

## Debugging Techniques

### Using Visual Studio

#### Debugging the Test Harness

1. **Open Test Harness Project** in Visual Studio
2. **Set Command Line Arguments**:
   - Right-click project → Properties → Debug
   - Add arguments: `--plugin C:\path\to\MyPlugin.dll`
3. **Set Breakpoints** in your plugin code
4. **Start Debugging** (F5)

#### Debugging Within WinClip

1. **Build your plugin** in Debug configuration
2. **Copy to plugins directory**: `%APPDATA%\WinClip\Plugins\`
3. **Attach debugger to WinClip**:
   - Debug → Attach to Process
   - Select `WinClip.exe`
4. **Set breakpoints** in your plugin code
5. **Trigger plugin** by copying text

### Using Visual Studio Code

#### Launch Configuration

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug Plugin with Test Harness",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/../WinClip.SDK.TestHarness/bin/Debug/net8.0/WinClip.SDK.TestHarness.dll",
      "args": [
        "--plugin",
        "${workspaceFolder}/bin/Debug/net8.0/MyPlugin.dll",
        "--verbose"
      ],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "console": "integratedTerminal"
    }
  ]
}
```

Press F5 to start debugging.

### Logging

Use `IActionContext.Log()` for debugging output:

```csharp
[Action("process-data", "Process Data")]
public void ProcessData(IActionContext context)
{
    context.Log("ProcessData started", LogLevel.Debug);

    var text = context.Clipboard.GetText();
    context.Log($"Retrieved text: {text.Length} characters", LogLevel.Info);

    try
    {
        // Process text
        var result = ComplexProcessing(text);
        context.Log($"Processing successful: {result.Length} items", LogLevel.Info);

        context.PasteText(result);
    }
    catch (Exception ex)
    {
        context.Log($"Error processing data: {ex.Message}", LogLevel.Error);
        context.ShowNotification("Error", ex.Message, NotificationType.Error);
    }
}
```

Log levels:
- **Debug**: Detailed debugging information
- **Info**: General informational messages
- **Warning**: Warning messages for potential issues
- **Error**: Error messages for failures

### Diagnostic Actions

Add temporary diagnostic actions during development:

```csharp
#if DEBUG
[Action("debug-info", "Debug: Show Context Info")]
public void ShowDebugInfo(IActionContext context)
{
    var info = new System.Text.StringBuilder();
    info.AppendLine($"Has Text: {context.Clipboard.HasText}");
    info.AppendLine($"Has Image: {context.Clipboard.HasImage}");
    info.AppendLine($"Has Files: {context.Clipboard.HasFiles}");
    info.AppendLine($"Source: {context.Clipboard.SourceApplication}");
    info.AppendLine($"Modified: {context.Clipboard.LastModified}");

    if (context.Clipboard.HasText)
    {
        var text = context.Clipboard.GetText();
        info.AppendLine($"Text Length: {text.Length}");
        info.AppendLine($"First 50 chars: {text.Substring(0, Math.Min(50, text.Length))}");
    }

    context.ShowNotification("Debug Info", info.ToString(), NotificationType.Info);
}
#endif
```

### Exception Handling

Always wrap action code in try-catch blocks:

```csharp
[Action("risky-action", "Risky Action")]
public void RiskyAction(IActionContext context)
{
    try
    {
        // Your action code
        var result = PerformRiskyOperation(context);
        context.ShowNotification("Success", "Operation completed", NotificationType.Success);
    }
    catch (ArgumentException ex)
    {
        context.Log($"Invalid argument: {ex.Message}", LogLevel.Error);
        context.ShowNotification("Invalid Input", ex.Message, NotificationType.Warning);
    }
    catch (Exception ex)
    {
        context.Log($"Unexpected error: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
        context.ShowNotification("Error", "An unexpected error occurred", NotificationType.Error);
    }
}
```

---

## Integration Testing

### Writing Integration Tests

Create a test project for your plugin:

```bash
dotnet new xunit -n MyPlugin.Tests
cd MyPlugin.Tests
dotnet add reference ../MyPlugin/MyPlugin.csproj
dotnet add package WinClip.SDK
```

#### Mock Context Implementation

Create mock implementations for testing:

```csharp
using WinClip.SDK;

public class MockClipboardContext : IClipboardContext
{
    public bool HasText { get; set; }
    public bool HasImage { get; set; }
    public bool HasFiles { get; set; }
    public string SourceApplication { get; set; } = "TestApp";
    public DateTime LastModified { get; set; } = DateTime.Now;

    private string _text;
    private string[] _files;

    public MockClipboardContext(string text = null, string[] files = null)
    {
        _text = text;
        _files = files;
        HasText = text != null;
        HasFiles = files != null && files.Length > 0;
    }

    public string GetText() => _text;
    public string[] GetFiles() => _files;
    public byte[] GetImage() => null;
    public object GetData(string format) => null;
    public bool ContainsFormat(string format) => false;
}

public class MockActionContext : IActionContext
{
    public IClipboardContext Clipboard { get; set; }
    public List<string> LogMessages { get; } = new List<string>();
    public List<string> Notifications { get; } = new List<string>();
    public string PastedText { get; private set; }
    public string ClipboardText { get; private set; }
    public string OpenedUrl { get; private set; }

    public void Log(string message, LogLevel level)
    {
        LogMessages.Add($"[{level}] {message}");
    }

    public void ShowNotification(string title, string message, NotificationType type)
    {
        Notifications.Add($"{title}: {message}");
    }

    public void PasteText(string text)
    {
        PastedText = text;
    }

    public void SetClipboard(string text)
    {
        ClipboardText = text;
    }

    public void OpenUrl(string url)
    {
        OpenedUrl = url;
    }

    public void DismissPopup() { }

    public Task<ProcessResult> ExecuteCommandAsync(string command, string arguments)
    {
        return Task.FromResult(new ProcessResult { ExitCode = 0 });
    }

    public string GetPluginDataDirectory(string pluginId)
    {
        return Path.Combine(Path.GetTempPath(), "WinClipTest", pluginId);
    }
}
```

#### Example Test Cases

```csharp
using Xunit;
using MyPlugin;

public class TextTransformPluginTests
{
    [Fact]
    public void Uppercase_TransformsTextCorrectly()
    {
        // Arrange
        var plugin = new TextTransformPlugin();
        var clipboard = new MockClipboardContext(text: "hello world");
        var context = new MockActionContext { Clipboard = clipboard };

        // Act
        plugin.Uppercase(context);

        // Assert
        Assert.Equal("HELLO WORLD", context.PastedText);
    }

    [Fact]
    public void ShouldEnable_ReturnsTrueForText()
    {
        // Arrange
        var plugin = new TextTransformPlugin();
        var clipboard = new MockClipboardContext(text: "sample");

        // Act
        var result = plugin.ShouldEnable(clipboard);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldEnable_ReturnsFalseForEmptyText()
    {
        // Arrange
        var plugin = new TextTransformPlugin();
        var clipboard = new MockClipboardContext(text: "");

        // Act
        var result = plugin.ShouldEnable(clipboard);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("hello", "HELLO")]
    [InlineData("WORLD", "WORLD")]
    [InlineData("MiXeD", "MIXED")]
    public void Uppercase_HandlesVariousInputs(string input, string expected)
    {
        // Arrange
        var plugin = new TextTransformPlugin();
        var clipboard = new MockClipboardContext(text: input);
        var context = new MockActionContext { Clipboard = clipboard };

        // Act
        plugin.Uppercase(context);

        // Assert
        Assert.Equal(expected, context.PastedText);
    }
}
```

#### Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~Uppercase_TransformsTextCorrectly"

# Run tests with code coverage
dotnet test /p:CollectCoverage=true
```

---

## Performance Testing

### Measuring Action Performance

Test harness includes performance metrics:

```bash
dotnet run --project WinClip.SDK.TestHarness \
    --plugin ./bin/Debug/net8.0/MyPlugin.dll \
    --benchmark
```

Output shows:
```
Action: uppercase
  Execution Time: 1.23ms
  Memory Allocated: 456 bytes

Action: lowercase
  Execution Time: 0.98ms
  Memory Allocated: 412 bytes
```

### Performance Best Practices

#### 1. Avoid Slow Operations in ShouldEnable

```csharp
// BAD - Slow network call
public override bool ShouldEnable(IClipboardContext context)
{
    var text = context.GetText();
    return IsValidUrl(text) && CheckUrlReachable(text); // Slow!
}

// GOOD - Fast local check only
public override bool ShouldEnable(IClipboardContext context)
{
    var text = context.GetText();
    return IsValidUrl(text); // Fast regex check
}
```

#### 2. Lazy Load Resources

```csharp
private HttpClient _httpClient;
private HttpClient HttpClient => _httpClient ??= new HttpClient();

[Action("fetch-data", "Fetch Data")]
public void FetchData(IActionContext context)
{
    // HttpClient only created when needed
    var result = await HttpClient.GetStringAsync(url);
}
```

#### 3. Cache Expensive Computations

```csharp
private Dictionary<string, string> _cache = new Dictionary<string, string>();

[Action("process", "Process Text")]
public void ProcessText(IActionContext context)
{
    var text = context.Clipboard.GetText();

    if (!_cache.TryGetValue(text, out var result))
    {
        result = ExpensiveOperation(text);
        _cache[text] = result;
    }

    context.PasteText(result);
}
```

#### 4. Use Asynchronous Operations

```csharp
[Action("download", "Download Content")]
public async void DownloadContent(IActionContext context)
{
    var url = context.Clipboard.GetText();

    try
    {
        var content = await HttpClient.GetStringAsync(url);
        context.SetClipboard(content);
        context.ShowNotification("Success", "Content downloaded", NotificationType.Success);
    }
    catch (Exception ex)
    {
        context.ShowNotification("Error", ex.Message, NotificationType.Error);
    }
}
```

### Memory Profiling

Use Visual Studio Diagnostic Tools:

1. **Start Performance Profiler**: Debug → Performance Profiler
2. **Select .NET Object Allocation Tracking**
3. **Run Test Harness** with your plugin
4. **Analyze Memory Usage** and identify leaks

---

## Troubleshooting Common Issues

### Plugin Not Loading

#### Symptom
Plugin doesn't appear in WinClip or test harness fails to load it.

#### Possible Causes & Solutions

**Missing Plugin Attribute**
```csharp
// Wrong - No attribute
public class MyPlugin : PluginBase { }

// Correct
[Plugin("my-plugin", "My Plugin", "1.0.0")]
public class MyPlugin : PluginBase { }
```

**Incorrect Target Framework**
```xml
<!-- Wrong - Not compatible -->
<TargetFramework>net8.0</TargetFramework>

<!-- Correct - Compatible with WinClip -->
<TargetFramework>netstandard2.0</TargetFramework>
```

**Missing SDK Reference**
```xml
<ItemGroup>
  <PackageReference Include="WinClip.SDK" Version="1.0.0" />
</ItemGroup>
```

**Check Logs**
```bash
# View WinClip logs
type %APPDATA%\WinClip\Logs\winclip.log
```

### Actions Not Appearing

#### Symptom
Plugin loads but actions don't show in menu.

#### Possible Causes & Solutions

**Missing Action Attribute**
```csharp
// Wrong
public void MyAction(IActionContext context) { }

// Correct
[Action("my-action", "My Action")]
public void MyAction(IActionContext context) { }
```

**Incorrect Method Signature**
```csharp
// Wrong - Wrong parameter type
[Action("test", "Test")]
public void Test(string data) { }

// Wrong - Returns value
[Action("test", "Test")]
public string Test(IActionContext context) { return ""; }

// Correct
[Action("test", "Test")]
public void Test(IActionContext context) { }
```

**ShouldEnable Returns False**
```csharp
public override bool ShouldEnable(IClipboardContext context)
{
    context.Log($"HasText: {context.HasText}", LogLevel.Debug);
    return context.HasText;
}
```

### Runtime Exceptions

#### Symptom
Action throws exception when executed.

#### Common Exceptions

**NullReferenceException**
```csharp
// Wrong - No null check
var text = context.Clipboard.GetText();
var upper = text.ToUpper(); // NullReferenceException if text is null

// Correct
var text = context.Clipboard.GetText();
if (!string.IsNullOrEmpty(text))
{
    var upper = text.ToUpper();
    context.PasteText(upper);
}
```

**FileNotFoundException**
```csharp
// Wrong - No validation
var files = context.Clipboard.GetFiles();
var content = File.ReadAllText(files[0]); // May not exist

// Correct
var files = context.Clipboard.GetFiles();
if (files != null && files.Length > 0 && File.Exists(files[0]))
{
    var content = File.ReadAllText(files[0]);
    // Process content
}
```

**TimeoutException**
```csharp
// Wrong - No timeout
var response = await httpClient.GetAsync(url);

// Correct
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    var response = await httpClient.GetAsync(url, cts.Token);
}
catch (OperationCanceledException)
{
    context.ShowNotification("Timeout", "Request took too long", NotificationType.Warning);
}
```

### Build Errors

#### CS0246: Type or namespace name not found

**Solution**: Add WinClip.SDK reference
```bash
dotnet add package WinClip.SDK
```

#### CS0246: Type 'PluginBase' not found

**Solution**: Add using directive
```csharp
using WinClip.SDK;
```

#### CS1061: Does not contain a definition

**Solution**: Check SDK version compatibility
```bash
dotnet list package
dotnet add package WinClip.SDK --version 1.0.0
```

### Debugging Checklist

When troubleshooting issues, work through this checklist:

- [ ] Plugin builds without errors
- [ ] Plugin has `[Plugin]` attribute with valid metadata
- [ ] All actions have `[Action]` attribute
- [ ] Action methods have correct signature: `void Method(IActionContext context)`
- [ ] `ShouldEnable()` returns true for test clipboard content
- [ ] No null reference exceptions in action code
- [ ] All external dependencies are available
- [ ] Plugin DLL is in correct location
- [ ] WinClip logs show no loading errors
- [ ] Test harness can load and run plugin

---

## Best Practices

### Test Early and Often

- Test each action as you write it
- Use test harness for rapid iteration
- Write integration tests for complex logic
- Test edge cases and error conditions

### Use Logging Strategically

```csharp
public override void Initialize(IClipboardContext context)
{
    context.Log($"Plugin {Name} v{Version} initializing", LogLevel.Info);

    try
    {
        LoadConfiguration();
        context.Log("Configuration loaded successfully", LogLevel.Debug);
    }
    catch (Exception ex)
    {
        context.Log($"Configuration load failed: {ex.Message}", LogLevel.Error);
    }
}
```

### Handle All Error Cases

```csharp
[Action("process", "Process Data")]
public void ProcessData(IActionContext context)
{
    try
    {
        var text = context.Clipboard.GetText();

        if (string.IsNullOrWhiteSpace(text))
        {
            context.ShowNotification("Warning", "No text to process", NotificationType.Warning);
            return;
        }

        var result = ProcessText(text);

        if (result == null)
        {
            context.ShowNotification("Warning", "Processing returned no result", NotificationType.Warning);
            return;
        }

        context.PasteText(result);
    }
    catch (Exception ex)
    {
        context.Log($"Error: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
        context.ShowNotification("Error", "Failed to process data", NotificationType.Error);
    }
}
```

### Test with Real-World Data

- Use actual clipboard content from your workflow
- Test with different text encodings
- Test with large files
- Test with empty/null data
- Test with malformed data

### Automate Testing

Create a test script:

```bash
#!/bin/bash
# test-plugin.sh

echo "Building plugin..."
dotnet build

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

echo "Running tests..."
dotnet test

if [ $? -ne 0 ]; then
    echo "Tests failed!"
    exit 1
fi

echo "Testing with harness..."
dotnet run --project ../WinClip.SDK.TestHarness \
    --plugin ./bin/Debug/net8.0/MyPlugin.dll \
    --text "test content" \
    --auto-test-all

if [ $? -ne 0 ]; then
    echo "Test harness failed!"
    exit 1
fi

echo "All tests passed!"
```

### Continuous Integration

Set up CI/CD pipeline (example with GitHub Actions):

```yaml
name: Test Plugin

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v2

    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Test
      run: dotnet test --no-build --verbosity normal
```

---

## Next Steps

- **[Plugin API Reference](./PluginAPI.md)** - Complete API documentation
- **[Best Practices](./BestPractices.md)** - Professional plugin development tips
- **[Publishing Guide](./Publishing.md)** - Share your plugin with others
- **[Example Plugins](../Examples/)** - Study working examples

---

**Happy Testing!** 🧪✨

Thorough testing ensures your plugin provides a great experience for all WinClip users.
