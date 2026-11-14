# SDK Integration with WinClip Main Application

This guide explains how plugins built with the WinClip SDK integrate with the main WinClip application, including file formats, installation procedures, and testing strategies.

## Overview

Plugins built using the WinClip SDK are designed to work seamlessly with the main WinClip application. However, there are some important differences between the SDK development format and the WinClip application runtime format that developers need to understand.

## File Format Differences

### During Development: `plugin.json`

When developing plugins with the SDK, you use a `plugin.json` file that provides metadata for development tools, documentation, and packaging:

```json
{
  "id": "com.example.helloworld",
  "name": "Hello World",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "A simple example plugin",
  "requirements": {
    "os": "Windows 10+",
    "dotnet": ".NET 8.0+"
  },
  "tags": ["example", "text", "utility"]
}
```

### For WinClip Runtime: `manifest.json`

The WinClip application expects plugins to have a `manifest.json` file with additional technical details:

```json
{
  "id": "com.example.helloworld",
  "name": "Hello World",
  "version": "1.0.0",
  "author": "Your Name",
  "description": "A simple example plugin",
  "entryAssembly": "HelloWorld.dll",
  "pluginType": "WinClip.Plugins.HelloWorld.HelloWorldPlugin",
  "minApiVersion": "1.0.0",
  "permissions": [],
  "tags": ["example", "text", "utility"]
}
```

**Key Differences:**
- **filename**: `plugin.json` → `manifest.json`
- **entryAssembly**: DLL filename (required for runtime)
- **pluginType**: Fully qualified class name (optional, auto-discovered if not specified)
- **minApiVersion**: Minimum WinClip API version required
- **permissions**: Array of required permissions

## Plugin Directory Structure

### SDK Development Structure

```
HelloWorld/
├── HelloWorld.csproj
├── HelloWorldPlugin.cs
├── plugin.json          # Development metadata
└── README.md
```

### WinClip Runtime Structure

```
%APPDATA%\WinClip\Plugins\HelloWorld\
├── HelloWorld.dll              # Compiled plugin
├── manifest.json               # Runtime metadata
├── WinClip.SDK.dll            # SDK dependency
└── [other dependencies]
```

**Default Plugin Directory:**
- Windows: `%APPDATA%\WinClip\Plugins\`
- Typical path: `C:\Users\<username>\AppData\Roaming\WinClip\Plugins\`

Each plugin must be in its own subdirectory with its `manifest.json` and DLL files.

## Integration Testing

### Automated Integration Test

Use the provided integration test script to verify SDK-built plugins work with WinClip:

```bash
# Test with default HelloWorld plugin
./test-plugin-integration.sh

# Test with specific plugin
./test-plugin-integration.sh --plugin TextTransform

# Verbose output and keep test artifacts
./test-plugin-integration.sh --verbose --no-cleanup

# Release build
./test-plugin-integration.sh --release
```

**What the script does:**
1. ✓ Verifies .NET SDK is installed
2. ✓ Builds WinClip.SDK package
3. ✓ Builds the example plugin
4. ✓ Converts `plugin.json` → `manifest.json`
5. ✓ Installs plugin to WinClip plugins directory
6. ✓ Verifies plugin structure is correct

### Manual Integration Test

To manually test a plugin with WinClip:

#### Step 1: Build Your Plugin

```bash
cd Examples/HelloWorld
dotnet build -c Release
```

#### Step 2: Create manifest.json

Convert your `plugin.json` to `manifest.json` format. Here's a template:

```json
{
  "id": "<same as plugin.json>",
  "name": "<same as plugin.json>",
  "version": "<same as plugin.json>",
  "author": "<same as plugin.json>",
  "description": "<same as plugin.json>",
  "entryAssembly": "<YourPlugin>.dll",
  "pluginType": "WinClip.Plugins.<Namespace>.<PluginClassName>",
  "minApiVersion": "1.0.0",
  "permissions": [],
  "tags": ["<from plugin.json>"]
}
```

**Example for HelloWorld:**

```json
{
  "id": "com.example.helloworld",
  "name": "Hello World",
  "version": "1.0.0",
  "author": "WinClip Examples",
  "description": "A simple example plugin that demonstrates the basics",
  "entryAssembly": "HelloWorld.dll",
  "pluginType": "WinClip.Plugins.HelloWorld.HelloWorldPlugin",
  "minApiVersion": "1.0.0",
  "permissions": [],
  "tags": ["example", "text"]
}
```

**Finding the pluginType:**
- Format: `<RootNamespace>.<PluginClassName>`
- For HelloWorld: The class is `HelloWorldPlugin` in namespace `WinClip.Plugins.HelloWorld`
- Result: `"pluginType": "WinClip.Plugins.HelloWorld.HelloWorldPlugin"`

#### Step 3: Install Plugin

1. Create plugin directory:
   ```bash
   mkdir "%APPDATA%\WinClip\Plugins\HelloWorld"
   ```

2. Copy files:
   ```bash
   # Copy plugin DLL
   copy "bin\Release\net8.0\HelloWorld.dll" "%APPDATA%\WinClip\Plugins\HelloWorld\"

   # Copy manifest
   copy "manifest.json" "%APPDATA%\WinClip\Plugins\HelloWorld\"

   # Copy SDK dependency (if not in GAC)
   copy "bin\Release\net8.0\WinClip.SDK.dll" "%APPDATA%\WinClip\Plugins\HelloWorld\"
   ```

#### Step 4: Launch WinClip

1. Start the WinClip application
2. The plugin should be automatically discovered and loaded
3. Check the plugins list in WinClip settings to verify it loaded

#### Step 5: Test Plugin Functionality

1. **Check Plugin Appears:**
   - Open WinClip settings
   - Navigate to Plugins section
   - Verify your plugin is listed and enabled

2. **Test Plugin Actions:**
   - Select some text
   - Trigger WinClip popup menu (usually via hotkey)
   - Verify plugin actions appear in the menu
   - Execute an action
   - Verify the action works as expected

3. **Check for Errors:**
   - Look for error messages or warnings
   - Check WinClip logs for any plugin loading errors
   - Verify plugin enablement logic (ShouldEnable) works correctly

## Testing with WinClip.SDK.TestHarness

Before installing to WinClip, you can test plugins with the SDK Test Harness:

```bash
cd WinClip.SDK.TestHarness

# Load and test plugin interactively
dotnet run -- --plugin "../Examples/HelloWorld/bin/Debug/net8.0/HelloWorld.dll"

# Or test installed plugin
dotnet run -- --plugin "%APPDATA%\WinClip\Plugins\HelloWorld\HelloWorld.dll"
```

The test harness provides:
- Plugin loading verification
- Action discovery
- Mock clipboard context
- Interactive action execution
- Error diagnostics

See [WinClip.SDK.TestHarness/README.md](../WinClip.SDK.TestHarness/README.md) for detailed usage.

## Common Integration Issues

### Issue: Plugin Not Appearing in WinClip

**Possible Causes:**
1. Plugin directory not in correct location
2. Missing or invalid `manifest.json`
3. Missing DLL dependencies
4. Invalid plugin structure

**Solutions:**
- Verify plugin is in `%APPDATA%\WinClip\Plugins\<PluginName>\`
- Check `manifest.json` is valid JSON with all required fields
- Ensure all DLL dependencies are copied to plugin directory
- Check WinClip logs for loading errors

### Issue: Plugin Loads but Actions Don't Appear

**Possible Causes:**
1. `ShouldEnable()` returns false
2. No actions decorated with `[Action]` attribute
3. Actions don't meet visibility criteria

**Solutions:**
- Test `ShouldEnable()` logic with test harness
- Verify all action methods have `[Action]` attribute
- Check action method signatures match expected format
- Test with different clipboard states

### Issue: Plugin Actions Fail at Runtime

**Possible Causes:**
1. Missing permissions
2. Incorrect IActionContext usage
3. Exceptions in action code
4. Missing external dependencies

**Solutions:**
- Add required permissions to `manifest.json`
- Wrap operations in try-catch blocks
- Test thoroughly with test harness before deploying
- Include all dependencies in plugin directory

### Issue: Wrong pluginType in manifest.json

**Symptoms:**
- "Plugin type not found in assembly" error
- Plugin fails to instantiate

**Solution:**
Find the correct fully qualified type name:

```bash
# On Windows with ildasm or similar tools
ildasm HelloWorld.dll

# Or check your code
# If your class is:
namespace WinClip.Plugins.HelloWorld
{
    public class HelloWorldPlugin : PluginBase
    {
        // ...
    }
}

# Then pluginType should be:
"pluginType": "WinClip.Plugins.HelloWorld.HelloWorldPlugin"
```

**Note:** If you omit `pluginType` from manifest.json, WinClip will auto-discover the first public class implementing `IPlugin`.

## Packaging for Distribution

When distributing your plugin, you can:

### Option 1: ZIP Archive

Create a ZIP file with:
- `<PluginName>.dll`
- `manifest.json`
- `WinClip.SDK.dll` (if needed)
- Dependencies
- README.md (optional)

Users extract to `%APPDATA%\WinClip\Plugins\<PluginName>\`

### Option 2: .winclip-plugin Package

Use WinClip's package format (ZIP with `.winclip-plugin` extension):

```bash
# Create package structure
mkdir package
cp bin/Release/net8.0/*.dll package/
cp manifest.json package/

# Create package
cd package
zip -r ../MyPlugin.winclip-plugin .
```

Users install via WinClip's plugin installer.

### Option 3: NuGet Package

Publish to NuGet for easy distribution:

```xml
<!-- In your .csproj -->
<PropertyGroup>
  <PackageId>WinClip.Plugin.MyPlugin</PackageId>
  <Version>1.0.0</Version>
  <!-- ... other metadata ... -->
</PropertyGroup>
```

Users install via:
```bash
dotnet add package WinClip.Plugin.MyPlugin
```

See [Publishing.md](Publishing.md) for detailed packaging instructions.

## Continuous Integration

To automate integration testing in CI/CD:

### GitHub Actions Example

```yaml
name: Integration Test

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Build SDK
      run: dotnet build WinClip.SDK/WinClip.SDK.csproj -c Release

    - name: Build Example Plugins
      run: |
        dotnet build Examples/HelloWorld/HelloWorld.csproj -c Release
        dotnet build Examples/TextTransform/TextTransform.csproj -c Release
        dotnet build Examples/WebSearch/WebSearch.csproj -c Release

    - name: Run Integration Test
      run: ./test-plugin-integration.sh --verbose
      shell: bash

    - name: Test with Harness
      run: |
        cd WinClip.SDK.TestHarness
        dotnet run -- --plugin "../Examples/HelloWorld/bin/Release/net8.0/HelloWorld.dll"
```

## API Compatibility

The SDK maintains API compatibility with the main WinClip application through:

1. **Interface Contracts**: `IPlugin`, `IClipboardContext`, `IActionContext` match exactly
2. **Version Checking**: `minApiVersion` in manifest ensures compatibility
3. **Graceful Degradation**: Missing features return errors rather than crash

**Version Matrix:**

| SDK Version | WinClip Version | Compatible |
|-------------|-----------------|------------|
| 1.0.x       | 1.0.x           | ✓ Yes      |
| 1.0.x       | 1.1.x           | ✓ Yes      |
| 1.1.x       | 1.0.x           | ⚠ Partial  |

## Debugging Integration Issues

### Enable Verbose Logging

In WinClip settings, enable verbose plugin logging to see:
- Plugin discovery process
- Manifest parsing
- Assembly loading
- Plugin instantiation
- Action discovery
- Enablement checks

### Use Diagnostic Tools

1. **Event Viewer**: Check Windows Event Viewer for .NET errors
2. **Process Monitor**: Monitor file access and registry operations
3. **Debugger**: Attach Visual Studio debugger to WinClip process
4. **Test Harness**: Use SDK test harness for isolated testing

### Debugging Workflow

```bash
# 1. Test with harness first
cd WinClip.SDK.TestHarness
dotnet run -- --plugin "../Examples/HelloWorld/bin/Debug/net8.0/HelloWorld.dll"

# 2. If harness works, create manifest.json
# (use automated script or manual process)

# 3. Install to WinClip plugins directory
./test-plugin-integration.sh --plugin HelloWorld --no-cleanup

# 4. Launch WinClip with debugger
# In Visual Studio: Debug > Attach to Process > WinClip.exe

# 5. Set breakpoints in plugin code
# (need PDB files in plugin directory)

# 6. Trigger plugin actions and step through code
```

## Summary

Integration between SDK-built plugins and WinClip requires:

1. ✓ Building plugin with SDK reference
2. ✓ Creating proper `manifest.json` file
3. ✓ Installing to correct directory structure
4. ✓ Including all dependencies
5. ✓ Testing with test harness first
6. ✓ Verifying in WinClip application

Use the automated `test-plugin-integration.sh` script for streamlined testing, or follow the manual steps for detailed control.

For more information:
- [Getting Started Guide](GettingStarted.md)
- [Plugin API Reference](PluginAPI.md)
- [Testing Guide](Testing.md)
- [Publishing Guide](Publishing.md)
- [Test Harness README](../WinClip.SDK.TestHarness/README.md)
