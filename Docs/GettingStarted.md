# Getting Started with WinClip Plugin Development

Welcome to WinClip plugin development! This guide will walk you through creating your first plugin from installation to a working example.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Creating Your First Plugin](#creating-your-first-plugin)
- [Understanding Plugin Structure](#understanding-plugin-structure)
- [Building Your Plugin](#building-your-plugin)
- [Testing Your Plugin](#testing-your-plugin)
- [Next Steps](#next-steps)

## Prerequisites

Before you begin developing WinClip plugins, ensure you have:

- **.NET SDK 8.0 or later** - [Download here](https://dotnet.microsoft.com/download)
- **Visual Studio 2022** or **Visual Studio Code** (recommended)
- **WinClip** installed on your Windows machine
- Basic knowledge of C# programming

Verify your .NET installation:

```bash
dotnet --version
```

## Installation

### 1. Install the WinClip SDK

Install the WinClip.SDK NuGet package:

```bash
dotnet add package WinClip.SDK
```

Or add it to your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="WinClip.SDK" Version="1.0.0" />
</ItemGroup>
```

### 2. Install the Plugin Template (Optional)

For faster development, install the WinClip plugin template:

```bash
dotnet new install WinClip.Plugin.Template
```

## Creating Your First Plugin

Let's create a simple "Hello World" plugin that displays a greeting when you select text.

### Option 1: Using the Template

If you installed the template:

```bash
dotnet new winclip-plugin -n MyFirstPlugin
cd MyFirstPlugin
```

### Option 2: Manual Setup

1. Create a new class library project:

```bash
dotnet new classlib -n MyFirstPlugin
cd MyFirstPlugin
```

2. Add the WinClip.SDK package:

```bash
dotnet add package WinClip.SDK
```

3. Replace the contents of `Class1.cs` with your plugin code (see below).

### Writing Your Plugin Code

Create or modify the plugin class with this code:

```csharp
using WinClip.SDK;

namespace MyFirstPlugin
{
    [Plugin("my-first-plugin", "My First Plugin", "1.0.0",
        Description = "A simple greeting plugin",
        Author = "Your Name")]
    public class HelloWorldPlugin : PluginBase
    {
        public override bool ShouldEnable(IClipboardContext context)
        {
            // Enable only when clipboard contains text
            return context.HasText;
        }

        [Action("say-hello", "Say Hello")]
        public void SayHello(IActionContext context)
        {
            var text = context.Clipboard.GetText();
            context.ShowNotification(
                "Hello!",
                $"You selected: {text}",
                NotificationType.Info
            );
        }

        [Action("uppercase", "Convert to UPPERCASE", Order = 1)]
        public void ConvertToUppercase(IActionContext context)
        {
            var text = context.Clipboard.GetText();
            if (!string.IsNullOrEmpty(text))
            {
                context.PasteText(text.ToUpper());
            }
        }
    }
}
```

## Understanding Plugin Structure

Let's break down the key components:

### 1. Plugin Attribute

The `[Plugin]` attribute defines your plugin's metadata:

```csharp
[Plugin("my-first-plugin", "My First Plugin", "1.0.0",
    Description = "A simple greeting plugin",
    Author = "Your Name")]
```

- **First parameter**: Unique plugin ID (use lowercase with hyphens)
- **Second parameter**: Display name shown in WinClip
- **Third parameter**: Version number (use semantic versioning)
- **Description**: What your plugin does
- **Author**: Your name or organization

### 2. Plugin Base Class

Inherit from `PluginBase` to get automatic action discovery:

```csharp
public class HelloWorldPlugin : PluginBase
```

The `PluginBase` class provides:
- Automatic metadata extraction from attributes
- Automatic action discovery via reflection
- Built-in action execution handling

### 3. ShouldEnable Method

Control when your plugin is available:

```csharp
public override bool ShouldEnable(IClipboardContext context)
{
    return context.HasText;
}
```

This method determines whether your plugin should be active based on clipboard content. Common checks:
- `context.HasText` - Clipboard has text
- `context.HasImage` - Clipboard has an image
- `context.HasFiles` - Clipboard has files
- Custom validation of clipboard content

### 4. Action Methods

Define actions users can perform:

```csharp
[Action("say-hello", "Say Hello")]
public void SayHello(IActionContext context)
{
    // Your action logic here
}
```

**Action Attribute Parameters:**
- **First parameter**: Unique action ID within the plugin
- **Second parameter**: Display name shown in menu

**Optional Properties:**
- `Order = 1` - Sort order in menu (lower numbers first)
- `Icon = "icon-name"` - Icon to display
- `Shortcut = "Ctrl+H"` - Keyboard shortcut hint
- `IsEnabled = false` - Disable action conditionally

**Action Method Requirements:**
- Must return `void`
- Must take exactly one parameter of type `IActionContext`
- Can be public or private

## Building Your Plugin

Build your plugin using the .NET CLI:

```bash
dotnet build
```

This creates a DLL file in `bin/Debug/net8.0/` (or your target framework).

For a release build:

```bash
dotnet build -c Release
```

Your compiled plugin will be at `bin/Release/net8.0/MyFirstPlugin.dll`.

## Testing Your Plugin

### Using the WinClip Test Harness

The SDK includes a test harness for local testing without installing to WinClip:

```bash
# Build your plugin first
dotnet build

# Run the test harness
dotnet run --project path/to/WinClip.SDK.TestHarness \
    --plugin ./bin/Debug/net8.0/MyFirstPlugin.dll
```

The test harness will:
1. Load your plugin
2. Display available actions
3. Let you test actions with sample clipboard data
4. Show any errors or exceptions

### Installing in WinClip

To test in the actual WinClip application:

1. Build your plugin in Release mode
2. Copy the DLL to WinClip's plugin directory:
   ```
   %APPDATA%\WinClip\Plugins\
   ```
3. Restart WinClip or reload plugins
4. Copy some text to trigger the plugin
5. Your plugin actions should appear in the WinClip menu

## Working with Clipboard Context

The `IClipboardContext` interface provides access to clipboard data:

```csharp
// Check what's on the clipboard
if (context.HasText)
{
    string text = context.GetText();
}

if (context.HasFiles)
{
    string[] files = context.GetFiles();
}

// Check for custom formats
if (context.ContainsFormat("HTML Format"))
{
    var html = context.GetData("HTML Format");
}

// Get metadata
string sourceApp = context.SourceApplication;
DateTime modified = context.LastModified;
```

## Working with Action Context

The `IActionContext` interface provides operations for your actions:

```csharp
public void MyAction(IActionContext context)
{
    // Access clipboard
    var text = context.Clipboard.GetText();

    // Modify and paste back
    context.PasteText(text.ToUpper());

    // Or set clipboard without pasting
    context.SetClipboard("New clipboard content");

    // Open URLs
    context.OpenUrl("https://example.com");

    // Show notifications
    context.ShowNotification("Success", "Action completed!", NotificationType.Success);

    // Execute commands
    var result = await context.ExecuteCommandAsync("notepad.exe", "myfile.txt");

    // Log for debugging
    context.Log("Action executed successfully", LogLevel.Info);

    // Store plugin data
    string dataDir = context.GetPluginDataDirectory("my-first-plugin");

    // Close the popup
    context.DismissPopup();
}
```

## Common Patterns

### Text Transformation Plugin

```csharp
[Plugin("text-transform", "Text Transform", "1.0.0")]
public class TextTransformPlugin : PluginBase
{
    public override bool ShouldEnable(IClipboardContext context)
    {
        return context.HasText && !string.IsNullOrWhiteSpace(context.GetText());
    }

    [Action("uppercase", "UPPERCASE", Order = 1)]
    public void Uppercase(IActionContext context)
    {
        var text = context.Clipboard.GetText();
        context.PasteText(text.ToUpper());
    }

    [Action("lowercase", "lowercase", Order = 2)]
    public void Lowercase(IActionContext context)
    {
        var text = context.Clipboard.GetText();
        context.PasteText(text.ToLower());
    }

    [Action("title-case", "Title Case", Order = 3)]
    public void TitleCase(IActionContext context)
    {
        var text = context.Clipboard.GetText();
        var titleCase = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
        context.PasteText(titleCase);
    }
}
```

### Web Search Plugin

```csharp
[Plugin("web-search", "Web Search", "1.0.0")]
public class WebSearchPlugin : PluginBase
{
    public override bool ShouldEnable(IClipboardContext context)
    {
        return context.HasText;
    }

    [Action("google", "Search Google", Icon = "google")]
    public void SearchGoogle(IActionContext context)
    {
        var query = Uri.EscapeDataString(context.Clipboard.GetText());
        context.OpenUrl($"https://www.google.com/search?q={query}");
    }

    [Action("stackoverflow", "Search Stack Overflow", Icon = "stackoverflow")]
    public void SearchStackOverflow(IActionContext context)
    {
        var query = Uri.EscapeDataString(context.Clipboard.GetText());
        context.OpenUrl($"https://stackoverflow.com/search?q={query}");
    }
}
```

### File Operations Plugin

```csharp
[Plugin("file-ops", "File Operations", "1.0.0")]
public class FileOperationsPlugin : PluginBase
{
    public override bool ShouldEnable(IClipboardContext context)
    {
        return context.HasFiles;
    }

    [Action("open-location", "Open File Location")]
    public void OpenFileLocation(IActionContext context)
    {
        var files = context.Clipboard.GetFiles();
        if (files != null && files.Length > 0)
        {
            var directory = System.IO.Path.GetDirectoryName(files[0]);
            context.ExecuteCommandAsync("explorer.exe", $"\"{directory}\"");
        }
    }
}
```

## Next Steps

Now that you have a working plugin, explore these topics:

- **[Plugin API Reference](./PluginAPI.md)** - Complete API documentation
- **[Testing and Debugging](./Testing.md)** - Advanced testing techniques
- **[Best Practices](./BestPractices.md)** - Tips for professional plugins
- **[Publishing Guide](./Publishing.md)** - Share your plugin with others
- **[Example Plugins](../Examples/)** - Learn from working examples

## Troubleshooting

### Plugin Not Loading

- Verify your plugin DLL is in the correct directory
- Check that you're targeting .NET Standard 2.0 or compatible framework
- Ensure the `[Plugin]` attribute is present on your class
- Check WinClip logs for loading errors

### Actions Not Appearing

- Verify action methods have the `[Action]` attribute
- Check that action methods have the correct signature: `void Method(IActionContext context)`
- Ensure `ShouldEnable()` returns `true` for the current clipboard content
- Verify action `IsEnabled` property is `true`

### Build Errors

- Ensure WinClip.SDK package is installed
- Verify you're using .NET SDK 8.0 or later
- Check for namespace conflicts

## Getting Help

- **GitHub Issues** - Report bugs or request features
- **Documentation** - Browse the complete docs in the `Docs/` folder
- **Examples** - Study working examples in `Examples/` folder
- **Community** - Join the WinClip developer community

---

**Congratulations!** You've created your first WinClip plugin. Happy coding! 🎉
