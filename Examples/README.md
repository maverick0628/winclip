# WinClip Plugin Examples

Welcome to the WinClip plugin examples! These examples demonstrate the core concepts and capabilities of the WinClip Plugin SDK, providing practical, runnable code that you can learn from and build upon.

## 📚 Learning Path

We recommend exploring the examples in this order:

1. **HelloWorld** - Start here for the absolute basics
2. **TextTransform** - Build on basics with multiple actions and text handling
3. **WebSearch** - Learn external integration and real-world patterns

Each example builds on concepts from the previous ones, creating a smooth learning curve from beginner to intermediate plugin development.

## 🚀 Quick Start

### Running the Examples

Each example can be built and tested independently:

```bash
# Build an example
cd Examples/HelloWorld
dotnet build

# Test with the SDK Test Harness
dotnet run --project ../../WinClip.SDK.TestHarness -- --plugin bin/Debug/net8.0/HelloWorld.dll
```

### Prerequisites

- .NET 8.0 SDK or later
- Windows 10/11
- Basic C# knowledge

## 📋 Example Overview

### 1. Hello World

**Path**: `Examples/HelloWorld/`
**Complexity**: Beginner
**Lines of Code**: ~30

The simplest possible WinClip plugin - perfect for your first plugin.

**What You'll Learn:**
- Basic plugin structure and inheritance
- Plugin metadata with `[Plugin]` attribute
- Creating a single action with `[Action]` attribute
- Always-enabled plugins with `ShouldEnable()`
- Showing notifications to users

**Key Concepts:**
- PluginBase class
- Automatic action discovery
- IActionContext interface
- NotificationType enum

**Use This Pattern For:**
- Utilities that work regardless of clipboard content
- Simple notification-based actions
- Quick greeting or status plugins

[View HelloWorld Example →](HelloWorld/README.md)

---

### 2. Text Transform

**Path**: `Examples/TextTransform/`
**Complexity**: Intermediate
**Lines of Code**: ~120

A practical plugin with multiple text transformation actions.

**What You'll Learn:**
- Multiple actions in a single plugin
- Conditional plugin enablement (text-only)
- Reading and manipulating clipboard text
- Error handling and validation
- Action ordering with `Order` property
- Using the logging API

**Available Actions:**
- UPPERCASE - Convert text to uppercase
- lowercase - Convert text to lowercase
- Title Case - Capitalize first letter of each word
- Reverse - Reverse text character by character
- Remove Whitespace - Strip all whitespace
- Trim - Remove leading/trailing whitespace

**Key Concepts:**
- `context.HasText` for conditional enablement
- `context.Clipboard.GetText()` for reading clipboard
- `context.PasteText()` for inserting transformed text
- Input validation patterns
- User feedback with notifications and logging

**Use This Pattern For:**
- Text processing utilities
- Code formatters and converters
- Data transformation tools
- Character encoding/decoding
- String manipulation utilities

[View TextTransform Example →](TextTransform/README.md)

---

### 3. Web Search

**Path**: `Examples/WebSearch/`
**Complexity**: Intermediate
**Lines of Code**: ~150

Demonstrates external integration by launching web searches.

**What You'll Learn:**
- Opening URLs in the default browser
- URL encoding and query parameters
- Process.Start() for external applications
- Supporting multiple service providers
- Safe URL construction
- Exception handling for external processes

**Supported Search Engines:**
- Google - General web search
- Bing - Microsoft's search engine
- DuckDuckGo - Privacy-focused search
- Stack Overflow - Developer Q&A
- GitHub - Code and repository search
- Wikipedia - Encyclopedia lookup

**Key Concepts:**
- `Uri.EscapeDataString()` for safe URL encoding
- `Process.Start()` with `UseShellExecute = true`
- Multiple related actions pattern
- Defensive programming practices
- Security considerations for user input

**Use This Pattern For:**
- Web service integrations
- Opening external applications
- URL-based workflows
- API integrations (REST endpoints)
- Launching tools with clipboard data

[View WebSearch Example →](WebSearch/README.md)

## 🎯 Common Patterns Across Examples

### Plugin Structure

All examples follow this basic structure:

```csharp
[Plugin("com.winclip.examples.{name}", "{Display Name}", "1.0.0",
    Description = "Description of what the plugin does",
    Author = "Your Name")]
public class YourPlugin : PluginBase
{
    // Control when plugin is available
    public override bool ShouldEnable(IClipboardContext context)
    {
        return true; // or conditional logic
    }

    // Define actions
    [Action("action-id", "Action Name", Order = 1)]
    public void ActionMethod(IActionContext context)
    {
        // Action implementation
    }
}
```

### Common API Patterns

#### Reading Clipboard
```csharp
string? text = context.Clipboard.GetText();
if (string.IsNullOrEmpty(text))
{
    context.ShowNotification("Plugin", "No text available", NotificationType.Warning);
    return;
}
```

#### Pasting Text
```csharp
context.PasteText(transformedText);
```

#### Notifications
```csharp
context.ShowNotification(
    "Title",
    "Message",
    NotificationType.Success); // or Warning, Error, Info
```

#### Logging
```csharp
context.Log($"Action completed: {details}", LogLevel.Info);
```

## 🛠️ Building Your Own Plugin

After studying these examples, you're ready to build your own plugin!

### Next Steps

1. **Choose a Template**: Start with the example closest to your use case
2. **Read the Documentation**: Review the complete [Getting Started Guide](../Docs/GettingStarted.md)
3. **Study the API**: Check the [Plugin API Reference](../Docs/PluginAPI.md)
4. **Follow Best Practices**: Read the [Best Practices Guide](../Docs/BestPractices.md)
5. **Test Thoroughly**: Use the [Testing Guide](../Docs/Testing.md)
6. **Publish**: Follow the [Publishing Guide](../Docs/Publishing.md)

### Plugin Ideas to Try

**Beginner:**
- Clipboard history manager
- Text counter (characters, words, lines)
- Quick emoji picker
- Unit converter

**Intermediate:**
- QR code generator
- Image optimizer
- File uploader
- Translation service integration

**Advanced:**
- OCR (image to text)
- AI-powered text completion
- Custom shortcut expander
- Cloud sync integration

## 📖 Additional Resources

### Documentation
- [Getting Started Guide](../Docs/GettingStarted.md) - Complete tutorial for beginners
- [Plugin API Reference](../Docs/PluginAPI.md) - Detailed API documentation
- [Best Practices](../Docs/BestPractices.md) - Professional development guidelines
- [Testing Guide](../Docs/Testing.md) - Testing strategies and tools
- [Publishing Guide](../Docs/Publishing.md) - How to distribute your plugin

### SDK Components
- **WinClip.SDK** - Core SDK with plugin interfaces
- **WinClip.SDK.TestHarness** - Testing and debugging tool
- **Plugin Template** - Quick start project template

## 🤝 Contributing

Have an idea for a new example? We welcome contributions!

Good example candidates:
- Demonstrate a pattern not covered by existing examples
- Show integration with popular services or APIs
- Solve a common use case in a clear, educational way
- Are well-documented and easy to understand

## 💡 Tips for Learning

1. **Run Before Reading**: Build and test each example first to see it in action
2. **Experiment**: Modify the examples to see what changes
3. **Read Comments**: Examples include detailed code comments
4. **Check Logs**: Use the test harness to see logging output
5. **Ask Questions**: Join the WinClip community for support

## 🔍 Understanding Plugin Lifecycle

Understanding when methods are called helps demystify plugin development:

```
1. Plugin Load
   ├─ WinClip discovers plugin DLL
   ├─ Reads [Plugin] attribute metadata
   └─ Instantiates plugin class

2. Clipboard Change Event
   ├─ User copies something
   ├─ WinClip creates IClipboardContext
   └─ Calls ShouldEnable() for each plugin

3. Popup Display
   ├─ User triggers WinClip popup
   ├─ Enabled plugins shown
   └─ Actions discovered via [Action] attributes

4. Action Execution
   ├─ User clicks an action
   ├─ WinClip creates IActionContext
   └─ Calls action method
```

## 🎓 From Examples to Production

These examples are educational but can be extended for production use:

### What Examples Include:
✅ Core functionality
✅ Basic error handling
✅ Clear, educational code
✅ Essential documentation

### What Production Plugins Need:
- Configuration/settings UI
- Comprehensive error recovery
- Performance optimization
- Telemetry and analytics
- Update mechanisms
- Localization support
- Accessibility features

See the [Best Practices Guide](../Docs/BestPractices.md) for details on building production-ready plugins.

## 🐛 Troubleshooting

### Example Won't Build
```bash
# Restore dependencies
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

### Example Won't Run in Test Harness
```bash
# Check the plugin DLL path is correct
ls bin/Debug/net8.0/*.dll

# Run with verbose logging
dotnet run --project ../../WinClip.SDK.TestHarness -- --plugin bin/Debug/net8.0/YourPlugin.dll --verbose
```

### Plugin Not Showing Actions
- Check that `ShouldEnable()` returns `true`
- Verify clipboard has expected content (text for TextTransform/WebSearch)
- Ensure action methods are public and have `[Action]` attribute

## 📊 Comparison Table

| Feature | HelloWorld | TextTransform | WebSearch |
|---------|-----------|---------------|-----------|
| **Difficulty** | Beginner | Intermediate | Intermediate |
| **Actions** | 1 | 6 | 6 |
| **Enablement** | Always | Text only | Text only |
| **External Deps** | None | None | Browser |
| **Error Handling** | Basic | Comprehensive | Comprehensive |
| **Logging** | Minimal | Yes | Yes |
| **Best For** | Learning basics | Text processing | External integration |

## 🚀 Ready to Start?

Pick an example that matches your learning style:

- **Visual Learner?** Start with HelloWorld and see it work immediately
- **Hands-On Learner?** Jump into TextTransform and modify the transformations
- **Goal-Oriented?** Start with the example closest to what you want to build

Happy plugin development! 🎉

---

**Need Help?**
- [Report an issue](https://duncansmith.tech/apps/winclip/support)
- [Join the community](https://duncansmith.tech/apps/winclip/community)
- [Read the docs](../Docs/README.md)
