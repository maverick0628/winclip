# Text Transform Plugin

A practical example plugin that demonstrates multiple text transformation actions. This example shows how to work with clipboard text and provide several related actions in a single plugin.

## What This Example Demonstrates

- **Multiple Actions**: How to provide several related actions from one plugin
- **Conditional Enablement**: Only enable when clipboard contains text
- **Text Manipulation**: Various text transformation techniques
- **Error Handling**: Graceful handling of empty or missing text
- **Action Ordering**: Using the `Order` property to control action sequence

## Available Actions

The plugin provides six text transformation actions:
TRANSFORMATION
1. **UPPERCASE** - Converts all text to uppercase
2. **lowercase** - Converts all text to lowercase
3. **Title Case** - Capitalizes the first letter of each word
4. **Reverse** - Reverses the text character by character
5. **Remove Whitespace** - Removes all spaces, tabs, and line breaks
6. **Trim** - Removes leading and trailing whitespace

## Code Overview

### Plugin Structure

```csharp
[Plugin("com.winclip.examples.texttransform", "Text Transform", "1.0.0",
    Description = "Transform text with various formatting options",
    Author = "WinClip Team")]
public class TextTransformPlugin : PluginBase
{
    public override bool ShouldEnable(IClipboardContext context)
    {
        // Only enable when clipboard contains text
        return context.HasText;
    }

    [Action("uppercase", "UPPERCASE", Order = 1)]
    public void ToUpperCase(IActionContext context)
    {
        string? text = context.Clipboard.GetText();
        if (string.IsNullOrEmpty(text))
        {
            context.ShowNotification(
                "Text Transform",
                "No text available to transform",
                NotificationType.Warning);
            return;
        }

        string transformed = text.ToUpperInvariant();
        context.PasteText(transformed);
        context.Log($"Transformed text to uppercase: {text.Length} characters", LogLevel.Info);
    }

    // ... other action methods
}
```

### Key Techniques

#### 1. Conditional Plugin Enablement

```csharp
public override bool ShouldEnable(IClipboardContext context)
{
    return context.HasText;
}
```

The plugin only appears when there's text in the clipboard, preventing unnecessary UI clutter.

#### 2. Multiple Related Actions

Each action method is decorated with `[Action]` attribute and performs a specific transformation:

```csharp
[Action("action-id", "Display Name", Order = 1)]
public void ActionMethod(IActionContext context)
{
    // Action implementation
}
```

The `Order` property determines the sequence in which actions appear.

#### 3. Reading Clipboard Text

```csharp
string? text = context.Clipboard.GetText();
```

Use `IClipboardContext.GetText()` to retrieve the current clipboard text.

#### 4. Error Handling

```csharp
if (string.IsNullOrEmpty(text))
{
    context.ShowNotification(
        "Text Transform",
        "No text available to transform",
        NotificationType.Warning);
    return;
}
```

Always validate that text is available before processing.

#### 5. Pasting Transformed Text

```csharp
context.PasteText(transformed);
```

Use `IActionContext.PasteText()` to paste the transformed text at the cursor position.

#### 6. Logging

```csharp
context.Log($"Transformed text: {text.Length} characters", LogLevel.Info);
```

Log actions for debugging and monitoring purposes.

## Building the Plugin

```bash
cd Examples/TextTransform
dotnet build
```

The compiled plugin will be in `bin/Debug/net8.0/TextTransform.dll`.

## Testing the Plugin

### Option 1: Using the Test Harness

```bash
dotnet run --project ../../WinClip.SDK.TestHarness -- --plugin bin/Debug/net8.0/TextTransform.dll
```

### Option 2: Manual Testing

1. Build the plugin
2. Copy `TextTransform.dll` and `plugin.json` to your WinClip plugins directory
3. Restart WinClip
4. Copy some text (e.g., "hello world")
5. Trigger the clipboard popup
6. Try different transformations:
   - "UPPERCASE" → "HELLO WORLD"
   - "Title Case" → "Hello World"
   - "Reverse" → "dlrow olleh"

## How It Works

1. **Copy Text**: User copies text to clipboard
2. **Trigger Popup**: User triggers WinClip popup
3. **Enablement Check**: Plugin checks `context.HasText` - returns `true`
4. **Show Actions**: All six transformation actions appear
5. **User Selection**: User clicks an action (e.g., "UPPERCASE")
6. **Execute**: Plugin transforms the text and pastes it
7. **Logging**: Plugin logs the transformation for debugging

## Real-World Use Cases

This plugin pattern is useful for:

- **Code Formatting**: Convert variable names (camelCase, PascalCase, snake_case)
- **Data Cleaning**: Trim, normalize whitespace, remove special characters
- **Text Processing**: Encode/decode (Base64, URL encoding, etc.)
- **Language Tools**: Transliteration, character replacement, sanitization
- **Developer Tools**: JSON formatting, XML pretty-printing, minification

## Next Steps

After understanding this example, explore:

- **WebSearch Example**: Shows external integration and URL handling
- **[Plugin API Reference](../../Docs/PluginAPI.md)**: Complete API documentation
- **[Best Practices](../../Docs/BestPractices.md)**: Guidelines for production plugins
- **[Testing Guide](../../Docs/Testing.md)**: How to test your plugins thoroughly

## Customization Ideas

Try extending this plugin with:

- **More Transformations**: camelCase, snake_case, kebab-case
- **Character Encoding**: Base64 encode/decode, URL encoding
- **Text Analysis**: Show character count, word count
- **Smart Transforms**: Only capitalize proper nouns, smart quotes
- **Conditional Actions**: Only show relevant actions based on text content
- **Clipboard Update**: Use `SetClipboard()` instead of `PasteText()` for different workflow

## Key Takeaways

1. **Multiple Actions**: One plugin can provide multiple related actions
2. **Conditional Enablement**: Use `ShouldEnable()` to show plugin only when relevant
3. **Text Operations**: Read with `GetText()`, paste with `PasteText()`
4. **Error Handling**: Always validate input before processing
5. **User Feedback**: Use logging and notifications appropriately
6. **Action Ordering**: Control action sequence with `Order` property

This example demonstrates the versatility of the WinClip plugin system and shows how a single plugin can provide multiple useful operations on the same type of data.

Happy text transforming! 🔤
