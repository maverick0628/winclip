# WinClip Plugin API Reference

**Version:** 1.0.0
**API Version:** 1.0.0

This document provides a comprehensive reference for the WinClip Plugin API, including all interfaces, classes, permissions, and best practices for plugin development.

---

## Table of Contents

1. [Overview](#overview)
2. [Core Interfaces](#core-interfaces)
   - [IPlugin](#iplugin)
   - [IPluginAction](#ipluginaction)
3. [Plugin Context](#plugin-context)
4. [Action Context](#action-context)
5. [Plugin Manifest](#plugin-manifest)
6. [API Interfaces](#api-interfaces)
   - [IClipboardApi](#iclipboardapi)
   - [IStorageApi](#istorageapi)
   - [INotificationApi](#inotificationapi)
7. [Permissions](#permissions)
8. [Logging](#logging)
9. [Lifecycle](#lifecycle)
10. [Best Practices](#best-practices)

---

## Overview

The WinClip Plugin API allows developers to extend WinClip's functionality by creating custom actions that appear in the popup menu. Plugins run in a sandboxed environment with permission-based access to system resources.

### Key Features

- **Action System**: Add custom actions to the WinClip popup
- **Sandboxed Execution**: Plugins run in isolation for security
- **Permission Model**: Fine-grained control over resource access
- **Rich APIs**: Access clipboard, storage, and notification systems
- **No Limits**: Install unlimited plugins (unlike SnipDo)

---

## Core Interfaces

### IPlugin

The main plugin interface that all plugins must implement.

```csharp
namespace WinClip.Plugins;

public interface IPlugin : IDisposable
{
    PluginMetadata Metadata { get; }
    bool Load(PluginContext context);
    Task InitializeAsync();
    IEnumerable<IPluginAction> GetActions();
    Task UnloadAsync();
}
```

#### Properties

##### `PluginMetadata Metadata`
- **Type**: `PluginMetadata`
- **Description**: Metadata describing this plugin (name, version, author, etc.)
- **Required**: Yes

#### Methods

##### `bool Load(PluginContext context)`
- **Description**: Called when the plugin is first loaded into memory. Validate environment and dependencies here.
- **Parameters**:
  - `context` (PluginContext): The plugin context providing access to WinClip APIs
- **Returns**: `true` if the plugin loaded successfully; `false` otherwise
- **Called**: Once when the plugin is first loaded

##### `Task InitializeAsync()`
- **Description**: Called to initialize the plugin after loading. Set up state and prepare to handle action requests.
- **Returns**: A task that completes when initialization is finished
- **Called**: Once after `Load()` succeeds

##### `IEnumerable<IPluginAction> GetActions()`
- **Description**: Returns the collection of actions provided by this plugin
- **Returns**: Collection of plugin actions that will appear in the WinClip popup
- **Called**: Multiple times (when popup opens, when plugins are refreshed)

##### `Task UnloadAsync()`
- **Description**: Called when the plugin is being unloaded. Clean up resources and cancel ongoing operations.
- **Returns**: A task that completes when cleanup is finished
- **Called**: Once when the plugin is disabled or uninstalled

---

### IPluginAction

Defines a single action that can be executed from the WinClip popup.

```csharp
namespace WinClip.Plugins;

public interface IPluginAction
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string? IconPath { get; }
    string? Category { get; }
    IEnumerable<string> Keywords { get; }
    string? KeyboardShortcut { get; }

    bool IsApplicable(ActionContext context);
    Task ExecuteAsync(ActionContext context, CancellationToken cancellationToken = default);

    bool SupportsBatchExecution { get; }
    ExecutionTime EstimatedExecutionTime { get; }
}
```

#### Properties

##### `string Id`
- **Type**: `string`
- **Description**: Unique identifier for this action within the plugin
- **Required**: Yes
- **Example**: `"reverse-text"`, `"translate-to-spanish"`

##### `string Name`
- **Type**: `string`
- **Description**: Display name shown in the WinClip popup
- **Required**: Yes
- **Guidelines**: Keep concise (2-4 words)
- **Example**: `"Reverse Text"`, `"Translate to Spanish"`

##### `string Description`
- **Type**: `string`
- **Description**: Brief description of what the action does
- **Required**: Yes
- **Usage**: Shown as tooltip or in details panel
- **Example**: `"Reverses the order of characters in the selected text"`

##### `string? IconPath`
- **Type**: `string?` (nullable)
- **Description**: Path to icon file relative to plugin directory
- **Required**: No (default icon used if null)
- **Supported Formats**: PNG, ICO, SVG
- **Example**: `"icons/reverse.png"`

##### `string? Category`
- **Type**: `string?` (nullable)
- **Description**: Category for organizing actions in the UI
- **Required**: No
- **Common Categories**: `"Text"`, `"Image"`, `"Web"`, `"Development"`, `"Formatting"`

##### `IEnumerable<string> Keywords`
- **Type**: `IEnumerable<string>`
- **Description**: Keywords/tags for search discoverability
- **Required**: No (can be empty)
- **Example**: `["flip", "backwards", "mirror"]`

##### `string? KeyboardShortcut`
- **Type**: `string?` (nullable)
- **Description**: Keyboard shortcut hint (informational only)
- **Required**: No
- **Format**: Human-readable (e.g., `"Ctrl+Shift+R"`)

##### `bool SupportsBatchExecution`
- **Type**: `bool`
- **Description**: Whether this action can process multiple items at once
- **Required**: Yes
- **Default**: `false`

##### `ExecutionTime EstimatedExecutionTime`
- **Type**: `ExecutionTime` enum
- **Description**: Expected execution time category
- **Required**: Yes
- **Values**:
  - `Instant` - Less than 100ms
  - `Fast` - 100ms to 1 second
  - `Moderate` - 1 to 5 seconds
  - `Slow` - 5+ seconds (progress indicator shown)

#### Methods

##### `bool IsApplicable(ActionContext context)`
- **Description**: Determines if this action should be shown for the given input
- **Parameters**:
  - `context` (ActionContext): Current input context with clipboard/selection data
- **Returns**: `true` to show the action; `false` to hide it
- **Example**:
  ```csharp
  public bool IsApplicable(ActionContext context)
  {
      // Only show for text input
      return !string.IsNullOrEmpty(context.Text);
  }
  ```

##### `Task ExecuteAsync(ActionContext context, CancellationToken cancellationToken)`
- **Description**: Executes the action with the provided input
- **Parameters**:
  - `context` (ActionContext): Input data and APIs
  - `cancellationToken` (CancellationToken): Token for cancellation monitoring
- **Returns**: Task representing the asynchronous execution
- **Called**: When user selects this action from the popup
- **Important**: Always respect the cancellation token for long-running operations

---

## Plugin Context

The `PluginContext` class provides the execution environment and API access for plugins.

```csharp
namespace WinClip.Plugins;

public class PluginContext
{
    // Metadata and Directories
    public PluginMetadata PluginMetadata { get; init; }
    public string PluginDirectory { get; init; }
    public string DataDirectory { get; init; }

    // API Access
    public string ApiVersion { get; init; }
    public IReadOnlyCollection<string> GrantedPermissions { get; init; }
    public IPluginLogger? Logger { get; init; }
    public IDictionary<string, object> Configuration { get; init; }
    public CancellationToken UnloadToken { get; init; }

    // Permission Checking
    public bool HasPermission(string permission);
    public void RequirePermission(string permission);

    // Configuration
    public T GetConfigValue<T>(string key, T defaultValue = default);
    public void SetConfigValue(string key, object value);

    // Path Resolution
    public string ResolvePluginPath(string relativePath);
    public string ResolveDataPath(string relativePath);
}
```

### Properties

#### `PluginMetadata PluginMetadata`
Metadata for the plugin that owns this context.

#### `string PluginDirectory`
Absolute path to the plugin's installation directory (contains assemblies, resources).

#### `string DataDirectory`
Absolute path to the plugin's data directory (for persistent storage).

#### `string ApiVersion`
WinClip API version currently running (e.g., `"1.0.0"`).

#### `IReadOnlyCollection<string> GrantedPermissions`
Collection of permissions granted to this plugin by the user.

#### `IPluginLogger? Logger`
Logger instance for writing diagnostic information. May be null if logging is disabled.

#### `IDictionary<string, object> Configuration`
Configuration dictionary for storing plugin settings.

#### `CancellationToken UnloadToken`
Cancellation token that triggers when the plugin is being unloaded. Monitor this in long-running operations.

### Methods

#### `bool HasPermission(string permission)`
Checks if the plugin has a specific permission.

**Parameters:**
- `permission` (string): Permission name (e.g., `"clipboard"`, `"storage"`)

**Returns:** `true` if granted; `false` otherwise

**Example:**
```csharp
if (context.HasPermission("network"))
{
    // Make network requests
}
```

#### `void RequirePermission(string permission)`
Ensures the plugin has a permission, throwing if not granted.

**Parameters:**
- `permission` (string): Required permission name

**Throws:** `UnauthorizedAccessException` if permission not granted

**Example:**
```csharp
context.RequirePermission("clipboard");
// Proceed with clipboard operations
```

#### `T GetConfigValue<T>(string key, T defaultValue)`
Retrieves a configuration value.

**Parameters:**
- `key` (string): Configuration key
- `defaultValue` (T): Default value if key not found

**Returns:** Configuration value or default

**Example:**
```csharp
var apiKey = context.GetConfigValue<string>("apiKey", "");
var maxRetries = context.GetConfigValue<int>("maxRetries", 3);
```

#### `void SetConfigValue(string key, object value)`
Stores a configuration value.

**Parameters:**
- `key` (string): Configuration key
- `value` (object): Value to store

**Example:**
```csharp
context.SetConfigValue("apiKey", "sk_12345");
context.SetConfigValue("maxRetries", 5);
```

#### `string ResolvePluginPath(string relativePath)`
Resolves a path relative to the plugin directory.

**Parameters:**
- `relativePath` (string): Relative path

**Returns:** Absolute path

**Example:**
```csharp
var iconPath = context.ResolvePluginPath("icons/logo.png");
```

#### `string ResolveDataPath(string relativePath)`
Resolves a path relative to the plugin's data directory. Creates the data directory if it doesn't exist.

**Parameters:**
- `relativePath` (string): Relative path

**Returns:** Absolute path

**Example:**
```csharp
var cachePath = context.ResolveDataPath("cache/translations.json");
```

---

## Action Context

The `ActionContext` class contains input data and APIs provided to actions during execution.

```csharp
namespace WinClip.Plugins;

public class ActionContext
{
    // Input Data
    public string? Text { get; init; }
    public string? Html { get; init; }
    public IReadOnlyList<string>? Files { get; init; }
    public byte[]? ImageData { get; init; }

    // API Access
    public PluginContext PluginContext { get; init; }

    // Metadata
    public Dictionary<string, object> Metadata { get; init; }
}
```

### Properties

#### `string? Text`
Selected or clipboard text content, if available.

#### `string? Html`
HTML content, if available.

#### `IReadOnlyList<string>? Files`
File paths, if files are in the clipboard.

#### `byte[]? ImageData`
Image data (PNG format), if an image is in the clipboard.

#### `PluginContext PluginContext`
Plugin context providing access to WinClip APIs.

#### `Dictionary<string, object> Metadata`
Additional metadata about the input source.

---

## Plugin Manifest

The `manifest.json` file describes a plugin's metadata, requirements, and permissions.

### Schema

```json
{
  "id": "com.example.myplugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "A brief description of what the plugin does",
  "website": "https://github.com/username/my-plugin",
  "iconPath": "icon.png",
  "minApiVersion": "1.0.0",
  "permissions": [
    "clipboard",
    "storage"
  ],
  "tags": [
    "text",
    "formatting"
  ],
  "entryAssembly": "MyPlugin.dll",
  "pluginType": "MyPlugin.MyPluginClass"
}
```

### Required Fields

- **`id`** (string): Unique identifier in reverse-domain notation (e.g., `"com.example.myplugin"`)
- **`name`** (string): Human-readable plugin name
- **`version`** (string): Semantic version (e.g., `"1.0.0"`)
- **`author`** (string): Author or organization name

### Optional Fields

- **`description`** (string): Brief description of plugin functionality
- **`website`** (string): URL to plugin website or repository
- **`iconPath`** (string): Path to icon file relative to plugin directory
- **`minApiVersion`** (string): Minimum WinClip API version required
- **`permissions`** (array): List of required permissions (see [Permissions](#permissions))
- **`tags`** (array): Tags/categories for discovery
- **`entryAssembly`** (string): DLL filename (e.g., `"MyPlugin.dll"`)
- **`pluginType`** (string): Fully qualified type name of plugin class

### Validation Rules

1. **ID Format**: Must use reverse-domain notation
2. **Version Format**: Must be semantic versioning (major.minor.patch)
3. **Permissions**: Must be valid permission names
4. **Entry Assembly**: Must exist in plugin directory
5. **Plugin Type**: Must implement `IPlugin` interface

---

## API Interfaces

### IClipboardApi

Access clipboard operations. **Requires `clipboard` permission.**

```csharp
namespace WinClip.Plugins.API;

public interface IClipboardApi
{
    // Text Operations
    string? GetText();
    void SetText(string text);

    // HTML Operations
    string? GetHtml();
    void SetHtml(string html, string? plainTextFallback = null);

    // File Operations
    IReadOnlyList<string> GetFiles();
    void SetFiles(IEnumerable<string> filePaths);

    // Image Operations
    byte[]? GetImage();
    void SetImage(byte[] imageData);

    // Format Checking
    bool ContainsFormat(string format);
    IReadOnlyList<string> GetAvailableFormats();
    void Clear();

    // Change Monitoring
    IDisposable OnClipboardChanged(Action callback);
}
```

#### Methods

##### Text Operations

**`string? GetText()`**
- Gets clipboard text content
- Returns `null` if clipboard doesn't contain text

**`void SetText(string text)`**
- Sets plain text to clipboard
- Throws `ArgumentNullException` if text is null

##### HTML Operations

**`string? GetHtml()`**
- Gets clipboard HTML content
- Returns `null` if clipboard doesn't contain HTML

**`void SetHtml(string html, string? plainTextFallback = null)`**
- Sets HTML to clipboard with optional plain-text fallback
- Throws `ArgumentNullException` if html is null

##### File Operations

**`IReadOnlyList<string> GetFiles()`**
- Gets file paths from clipboard
- Returns empty list if clipboard doesn't contain files

**`void SetFiles(IEnumerable<string> filePaths)`**
- Sets file paths to clipboard
- Throws `ArgumentNullException` if filePaths is null
- Throws `ArgumentException` if filePaths is empty or contains invalid paths

##### Image Operations

**`byte[]? GetImage()`**
- Gets image data from clipboard (PNG format)
- Returns `null` if clipboard doesn't contain an image

**`void SetImage(byte[] imageData)`**
- Sets image data to clipboard (PNG format)
- Throws `ArgumentNullException` if imageData is null
- Throws `ArgumentException` if imageData is empty or invalid

##### Format Checking

**`bool ContainsFormat(string format)`**
- Checks if clipboard contains a specific format
- Format examples: `"Text"`, `"HTML"`, `"Files"`, `"Image"`

**`IReadOnlyList<string> GetAvailableFormats()`**
- Gets all available formats in clipboard
- Returns list of format names

**`void Clear()`**
- Clears all content from clipboard

##### Change Monitoring

**`IDisposable OnClipboardChanged(Action callback)`**
- Registers callback for clipboard changes
- Callback invoked on background thread
- Returns disposable to unregister callback
- **Example:**
  ```csharp
  var subscription = clipboard.OnClipboardChanged(() => {
      Logger?.Info("Clipboard changed!");
  });
  // Later: subscription.Dispose();
  ```

---

### IStorageApi

Persistent storage for plugin data. **Requires `storage` permission.**

```csharp
namespace WinClip.Plugins.API;

public interface IStorageApi
{
    // Key-Value Storage
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string key, T? defaultValue = default, CancellationToken cancellationToken = default);
    Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAllKeysAsync(CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);

    // File Operations
    Task<string> ReadFileAsync(string fileName, CancellationToken cancellationToken = default);
    Task WriteFileAsync(string fileName, string content, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string fileName, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string fileName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ListFilesAsync(string? searchPattern = null, CancellationToken cancellationToken = default);
}
```

#### Key-Value Storage

**`Task SetAsync<T>(string key, T value, CancellationToken cancellationToken)`**
- Stores value with JSON serialization
- Throws `ArgumentNullException` if key is null or empty

**`Task<T?> GetAsync<T>(string key, T? defaultValue, CancellationToken cancellationToken)`**
- Retrieves value with JSON deserialization
- Returns `defaultValue` if key doesn't exist
- Throws `ArgumentNullException` if key is null or empty

**`Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken)`**
- Checks if key exists
- Throws `ArgumentNullException` if key is null or empty

**`Task<bool> RemoveAsync(string key, CancellationToken cancellationToken)`**
- Removes value by key
- Returns `true` if removed; `false` if key didn't exist
- Throws `ArgumentNullException` if key is null or empty

**`Task<IReadOnlyList<string>> GetAllKeysAsync(CancellationToken cancellationToken)`**
- Returns all storage keys

**`Task ClearAsync(CancellationToken cancellationToken)`**
- Clears all stored data (cannot be undone)

#### File Operations

All file operations are relative to the plugin's data directory for security.

**`Task<string> ReadFileAsync(string fileName, CancellationToken cancellationToken)`**
- Reads file contents as string
- Throws `ArgumentNullException` if fileName is null or empty
- Throws `FileNotFoundException` if file doesn't exist

**`Task WriteFileAsync(string fileName, string content, CancellationToken cancellationToken)`**
- Writes content to file (overwrites if exists)
- Throws `ArgumentNullException` if fileName or content is null

**`Task<bool> DeleteFileAsync(string fileName, CancellationToken cancellationToken)`**
- Deletes file
- Returns `true` if deleted; `false` if file didn't exist
- Throws `ArgumentNullException` if fileName is null or empty

**`Task<bool> FileExistsAsync(string fileName, CancellationToken cancellationToken)`**
- Checks if file exists
- Throws `ArgumentNullException` if fileName is null or empty

**`Task<IReadOnlyList<string>> ListFilesAsync(string? searchPattern, CancellationToken cancellationToken)`**
- Lists files in data directory
- Optional search pattern (e.g., `"*.json"`)
- Returns all files if pattern is null

---

### INotificationApi

Display notifications to users. **Requires `notifications` permission.**

```csharp
namespace WinClip.Plugins.API;

public interface INotificationApi
{
    // Simple Notifications
    Task ShowAsync(string title, string message, CancellationToken cancellationToken = default);

    // Typed Notifications
    Task<INotificationHandle> ShowAsync(
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken = default);

    // Notifications with Actions
    Task<INotificationHandle> ShowAsync(
        string title,
        string message,
        NotificationType type,
        IEnumerable<NotificationAction> actions,
        CancellationToken cancellationToken = default);

    // Progress Notifications
    Task<INotificationHandle> ShowProgressAsync(
        string title,
        string message,
        CancellationToken cancellationToken = default);
}

public interface INotificationHandle
{
    string Id { get; }
    Task UpdateMessageAsync(string message, CancellationToken cancellationToken = default);
    Task UpdateProgressAsync(int progress, string? message = null, CancellationToken cancellationToken = default);
    Task DismissAsync(CancellationToken cancellationToken = default);
}
```

#### Notification Types

```csharp
public enum NotificationType
{
    Info,       // Informational (neutral/blue)
    Success,    // Success (green)
    Warning,    // Warning (yellow/orange)
    Error       // Error (red)
}
```

#### Methods

**`Task ShowAsync(string title, string message, CancellationToken cancellationToken)`**
- Shows simple informational notification
- Throws `ArgumentNullException` if title or message is null

**`Task<INotificationHandle> ShowAsync(string title, string message, NotificationType type, CancellationToken cancellationToken)`**
- Shows typed notification
- Returns handle for updates/dismissal
- Throws `ArgumentNullException` if title or message is null

**`Task<INotificationHandle> ShowAsync(string title, string message, NotificationType type, IEnumerable<NotificationAction> actions, CancellationToken cancellationToken)`**
- Shows notification with action buttons
- Returns handle for updates/dismissal
- Throws `ArgumentNullException` if title, message, or actions is null

**`Task<INotificationHandle> ShowProgressAsync(string title, string message, CancellationToken cancellationToken)`**
- Shows progress notification
- Returns handle for progress updates
- Throws `ArgumentNullException` if title or message is null

#### Notification Handle

**`string Id`**
- Unique identifier for the notification

**`Task UpdateMessageAsync(string message, CancellationToken cancellationToken)`**
- Updates notification message

**`Task UpdateProgressAsync(int progress, string? message, CancellationToken cancellationToken)`**
- Updates progress percentage (0-100)
- Optional status message
- Throws `ArgumentOutOfRangeException` if progress not 0-100

**`Task DismissAsync(CancellationToken cancellationToken)`**
- Dismisses the notification

#### Notification Actions

```csharp
public class NotificationAction
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required Action Callback { get; init; }
    public bool DismissOnClick { get; init; } = true;
}
```

**Example:**
```csharp
var actions = new[]
{
    new NotificationAction
    {
        Id = "open",
        Label = "Open",
        Callback = () => OpenFile(),
        DismissOnClick = true
    },
    new NotificationAction
    {
        Id = "cancel",
        Label = "Cancel",
        Callback = () => CancelOperation(),
        DismissOnClick = true
    }
};

await notifications.ShowAsync("Operation Complete", "File processed successfully", NotificationType.Success, actions);
```

---

## Permissions

Plugins must declare required permissions in their manifest. Users approve permissions during installation.

### Available Permissions

| Permission | String Value | Description | Risk Level |
|------------|-------------|-------------|------------|
| **Clipboard** | `"clipboard"` | Read/write clipboard beyond action context | Low |
| **Storage** | `"storage"` | Persistent key-value and file storage | Low |
| **Notifications** | `"notifications"` | Display notifications to user | Low |
| **FileSystem** | `"filesystem"` | Access files outside plugin directory | Medium |
| **Network** | `"network"` | Make network requests to external services | Medium |
| **Process** | `"process"` | Launch external processes/applications | High |

### Permission Checking

Always check permissions before using restricted APIs:

```csharp
// Method 1: HasPermission (conditional)
if (context.HasPermission("clipboard"))
{
    var text = clipboard.GetText();
}

// Method 2: RequirePermission (exception-based)
try
{
    context.RequirePermission("network");
    // Make network requests
}
catch (UnauthorizedAccessException ex)
{
    Logger?.Error("Network permission not granted", ex);
}
```

### Declaring Permissions

In `manifest.json`:

```json
{
  "permissions": [
    "clipboard",
    "storage",
    "notifications"
  ]
}
```

### Permission Best Practices

1. **Minimal Permissions**: Only request what you need
2. **Explain Usage**: Document why each permission is needed
3. **Check Before Use**: Always check permissions before API calls
4. **Graceful Degradation**: Provide fallback behavior if permissions denied
5. **Security**: Higher-risk permissions require stronger justification

---

## Logging

Plugins can write diagnostic information using `IPluginLogger`.

```csharp
public interface IPluginLogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
}
```

### Log Levels

- **Debug**: Detailed diagnostic information (disabled in production)
- **Info**: Informational messages about normal operations
- **Warning**: Potentially problematic situations
- **Error**: Error conditions with optional exception

### Usage Example

```csharp
public bool Load(PluginContext context)
{
    context.Logger?.Info($"Loading {context.PluginMetadata.Name} v{context.PluginMetadata.Version}");

    try
    {
        // Load resources
        context.Logger?.Debug("Loading plugin resources");
        return true;
    }
    catch (Exception ex)
    {
        context.Logger?.Error("Failed to load plugin", ex);
        return false;
    }
}
```

---

## Lifecycle

Understanding the plugin lifecycle is crucial for proper resource management.

### Lifecycle States

1. **Discovered**: Plugin found in plugins directory
2. **Loaded**: Assembly loaded, `Load()` called
3. **Initialized**: `InitializeAsync()` completed
4. **Active**: Plugin enabled, actions available
5. **Disabled**: Plugin disabled, actions hidden
6. **Unloading**: `UnloadAsync()` called
7. **Unloaded**: Plugin removed from memory

### Lifecycle Diagram

```
[Discovered] → [Loaded] → [Initialized] → [Active]
                                              ↓
                                          [Disabled] ⟺ [Active]
                                              ↓
                                          [Unloading] → [Unloaded]
```

### Lifecycle Methods

#### Load Phase

**`bool Load(PluginContext context)`**
- **Purpose**: Validate environment, store context
- **Duration**: Should be fast (< 100ms)
- **Return**: `false` to abort loading
- **Example**:
  ```csharp
  private PluginContext _context;

  public bool Load(PluginContext context)
  {
      _context = context;
      _context.Logger?.Info("Plugin loaded");

      // Validate dependencies
      if (!ValidateDependencies())
      {
          _context.Logger?.Error("Missing required dependencies");
          return false;
      }

      return true;
  }
  ```

#### Initialize Phase

**`Task InitializeAsync()`**
- **Purpose**: Set up state, load resources
- **Duration**: Can be longer (async operations allowed)
- **Example**:
  ```csharp
  public async Task InitializeAsync()
  {
      _context.Logger?.Info("Initializing plugin");

      // Load configuration
      _apiKey = _context.GetConfigValue<string>("apiKey", "");

      // Initialize resources
      await LoadResourcesAsync();

      _context.Logger?.Info("Plugin initialized");
  }
  ```

#### Active Phase

**`IEnumerable<IPluginAction> GetActions()`**
- **Purpose**: Provide actions to WinClip
- **Called**: Multiple times
- **Important**: Return the same action instances, don't create new ones each call
- **Example**:
  ```csharp
  private readonly List<IPluginAction> _actions;

  public IEnumerable<IPluginAction> GetActions()
  {
      return _actions;
  }
  ```

#### Unload Phase

**`Task UnloadAsync()`**
- **Purpose**: Clean up resources, cancel operations
- **Important**: Check `UnloadToken` in long-running operations
- **Example**:
  ```csharp
  public async Task UnloadAsync()
  {
      _context.Logger?.Info("Unloading plugin");

      // Cancel ongoing operations
      _cancellationTokenSource?.Cancel();

      // Clean up resources
      _httpClient?.Dispose();

      // Save state
      await SaveStateAsync();

      _context.Logger?.Info("Plugin unloaded");
  }
  ```

#### Dispose

**`void Dispose()`**
- **Purpose**: Final cleanup (called after UnloadAsync)
- **Example**:
  ```csharp
  public void Dispose()
  {
      _cancellationTokenSource?.Dispose();
      _httpClient?.Dispose();
  }
  ```

### Monitoring Unload Token

Long-running operations should monitor `context.UnloadToken`:

```csharp
public async Task ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
{
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken,
        context.PluginContext.UnloadToken);

    try
    {
        await LongRunningOperationAsync(linkedCts.Token);
    }
    catch (OperationCanceledException)
    {
        context.PluginContext.Logger?.Info("Operation cancelled");
    }
}
```

---

## Best Practices

### Performance

1. **Fast Load**: Keep `Load()` under 100ms
2. **Lazy Initialization**: Defer expensive operations to `InitializeAsync()`
3. **Cache Actions**: Don't create new action instances in `GetActions()`
4. **Async Operations**: Use async/await for I/O operations
5. **Cancellation**: Always respect cancellation tokens

### Security

1. **Minimal Permissions**: Only request necessary permissions
2. **Validate Input**: Sanitize user input before use
3. **Secure Secrets**: Store API keys in configuration, not code
4. **HTTPS Only**: Use secure connections for network requests
5. **No Code Execution**: Never execute arbitrary code from user input

### Reliability

1. **Error Handling**: Wrap operations in try-catch blocks
2. **Logging**: Log errors with context for debugging
3. **Graceful Degradation**: Provide fallback behavior on errors
4. **Resource Cleanup**: Always dispose resources in `UnloadAsync()`
5. **Thread Safety**: Ensure shared state is thread-safe

### User Experience

1. **Descriptive Names**: Use clear, concise action names
2. **Helpful Descriptions**: Explain what actions do
3. **Progress Feedback**: Show progress for slow operations
4. **Error Messages**: Display user-friendly error messages
5. **Consistency**: Follow WinClip UI conventions

### Code Quality

1. **XML Documentation**: Document all public APIs
2. **Unit Tests**: Test action logic thoroughly
3. **Code Style**: Follow C# coding conventions
4. **Dependency Management**: Keep dependencies minimal
5. **Versioning**: Use semantic versioning

### Example: Well-Designed Action

```csharp
public class ReverseTextAction : IPluginAction
{
    public string Id => "reverse-text";
    public string Name => "Reverse Text";
    public string Description => "Reverses the order of characters in the selected text";
    public string? IconPath => "icons/reverse.png";
    public string? Category => "Text";
    public IEnumerable<string> Keywords => new[] { "flip", "backwards", "mirror" };
    public string? KeyboardShortcut => null;
    public bool SupportsBatchExecution => true;
    public ExecutionTime EstimatedExecutionTime => ExecutionTime.Instant;

    public bool IsApplicable(ActionContext context)
    {
        // Only show for text input
        return !string.IsNullOrEmpty(context.Text);
    }

    public async Task ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(context.Text))
            {
                throw new ArgumentException("No text to reverse");
            }

            // Perform operation
            var reversed = new string(context.Text.Reverse().ToArray());

            // Update clipboard
            context.PluginContext.RequirePermission("clipboard");
            // clipboard.SetText(reversed); // Assuming clipboard API available

            // Show notification
            if (context.PluginContext.HasPermission("notifications"))
            {
                // await notifications.ShowAsync("Text Reversed", $"Reversed {context.Text.Length} characters", NotificationType.Success);
            }

            context.PluginContext.Logger?.Info($"Reversed text: {context.Text.Length} characters");
        }
        catch (Exception ex)
        {
            context.PluginContext.Logger?.Error("Failed to reverse text", ex);
            throw;
        }
    }
}
```

---

## API Version Compatibility

The WinClip Plugin API follows semantic versioning:

- **Major Version**: Breaking changes (e.g., 1.0.0 → 2.0.0)
- **Minor Version**: New features, backwards-compatible (e.g., 1.0.0 → 1.1.0)
- **Patch Version**: Bug fixes, backwards-compatible (e.g., 1.0.0 → 1.0.1)

### Checking API Version

```csharp
public bool Load(PluginContext context)
{
    var apiVersion = Version.Parse(context.ApiVersion);
    var minVersion = Version.Parse("1.0.0");

    if (apiVersion < minVersion)
    {
        context.Logger?.Error($"Requires API version {minVersion} or higher, found {apiVersion}");
        return false;
    }

    return true;
}
```

### Handling API Changes

```csharp
public bool Load(PluginContext context)
{
    var apiVersion = Version.Parse(context.ApiVersion);

    if (apiVersion >= new Version(1, 1, 0))
    {
        // Use new API features
        _useNewFeatures = true;
    }
    else
    {
        // Fallback for older API
        _useNewFeatures = false;
    }

    return true;
}
```

---

## Troubleshooting

### Common Issues

#### Plugin Not Loading

**Symptoms:** Plugin doesn't appear in Plugin Manager

**Solutions:**
1. Check manifest.json is valid JSON
2. Verify all required manifest fields are present
3. Ensure entryAssembly and pluginType are correct
4. Check plugin directory structure
5. Review WinClip logs for errors

#### Permission Denied

**Symptoms:** `UnauthorizedAccessException` when calling APIs

**Solutions:**
1. Add required permission to manifest.json
2. Check permission granted in Plugin Manager
3. Call `HasPermission()` before API usage
4. Reinstall plugin to request new permissions

#### Actions Not Appearing

**Symptoms:** Plugin loaded but actions don't show in popup

**Solutions:**
1. Ensure `GetActions()` returns non-empty collection
2. Verify `IsApplicable()` returns true for test input
3. Check plugin is enabled in Plugin Manager
4. Review action properties (Id, Name, Description are set)

#### Execution Timeout

**Symptoms:** Action execution times out

**Solutions:**
1. Reduce `EstimatedExecutionTime` if operation is faster
2. Optimize slow operations (caching, async patterns)
3. Break up long operations into smaller chunks
4. Respect cancellation tokens

---

## Additional Resources

- **Plugin Development Guide**: See `PluginDevelopmentGuide.md` for step-by-step tutorial
- **Sample Plugins**: See `samples/` directory for reference implementations
- **Homepage**: [https://duncansmith.tech/apps/winclip](https://duncansmith.tech/apps/winclip)
- **Community Forum**: [https://duncansmith.tech/apps/winclip/community](https://duncansmith.tech/apps/winclip/community)
- **Issue Tracker**: [https://duncansmith.tech/apps/winclip/support](https://duncansmith.tech/apps/winclip/support)

---

**Last Updated:** 2026-02-02
**API Version:** 1.0.0
