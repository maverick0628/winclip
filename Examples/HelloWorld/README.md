# Hello World Plugin

The simplest possible WinClip plugin that demonstrates the basic plugin structure. This is the perfect starting point for learning plugin development.

## What This Example Demonstrates

- **Minimal Plugin Structure**: Shows the bare minimum code needed to create a working plugin
- **Basic Plugin Metadata**: How to use the `[Plugin]` attribute to define plugin information
- **Single Action**: A simple action that displays a notification
- **Always-Enabled Plugin**: Plugin is available regardless of clipboard content

## Code Overview

### Plugin Class

```csharp
[Plugin("com.winclip.examples.helloworld", "Hello World", "1.0.0",
    Description = "A simple example plugin that greets the user",
    Author = "WinClip Team")]
public class HelloWorldPlugin : PluginBase
{
    public override bool ShouldEnable(IClipboardContext context)
    {
        // Always enabled
        return true;
    }

    [Action("say-hello", "Say Hello", Order = 1)]
    public void SayHello(IActionContext context)
    {
        context.ShowNotification(
            "Hello World",
            "Hello from WinClip! 👋",
            NotificationType.Success);
    }
}
```

### Key Components

1. **Plugin Attribute**: Defines plugin metadata (ID, name, version, description, author)
2. **PluginBase**: Base class that provides automatic action discovery
3. **ShouldEnable**: Determines when the plugin is available (always, in this case)
4. **Action Method**: Marked with `[Action]` attribute, shows a notification

## Building the Plugin

```bash
cd Examples/HelloWorld
dotnet build
```

The compiled plugin will be in `bin/Debug/net8.0/HelloWorld.dll`.

## Testing the Plugin

### Option 1: Using the Test Harness

```bash
dotnet run --project ../../WinClip.SDK.TestHarness -- --plugin bin/Debug/net8.0/HelloWorld.dll
```

### Option 2: Manual Installation

1. Build the plugin
2. Copy `HelloWorld.dll` and `plugin.json` to your WinClip plugins directory
3. Restart WinClip
4. Trigger the clipboard popup
5. Look for "Hello World" plugin and click "Say Hello"

## How It Works

1. **Plugin Discovery**: WinClip finds the plugin by loading `HelloWorld.dll`
2. **Metadata Loading**: The `[Plugin]` attribute provides plugin information
3. **Enablement Check**: `ShouldEnable()` is called - returns `true` (always enabled)
4. **Action Discovery**: The `[Action]` attribute marks the `SayHello` method as an action
5. **Execution**: When user clicks "Say Hello", the method runs and shows a notification

## Next Steps

After understanding this example, check out:

- **TextTransform Example**: Shows multiple actions and text manipulation
- **WebSearch Example**: Demonstrates external integration (opening URLs)
- **[Getting Started Guide](../../Docs/GettingStarted.md)**: Complete tutorial on plugin development
- **[Plugin API Reference](../../Docs/PluginAPI.md)**: Detailed API documentation

## Key Takeaways

1. Plugins inherit from `PluginBase` for automatic action discovery
2. The `[Plugin]` attribute defines plugin metadata
3. Methods marked with `[Action]` become available actions
4. `ShouldEnable()` controls when the plugin is active
5. `IActionContext` provides access to clipboard and system operations

## Customization Ideas

Try modifying this plugin to:

- Show a different message
- Add more actions
- Only enable when text is in the clipboard: `return context.HasText;`
- Display the clipboard text in the notification
- Add a custom icon to the action

Happy plugin development! 🚀
