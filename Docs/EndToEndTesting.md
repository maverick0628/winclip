# End-to-End SDK Workflow Testing

This document describes how to test the complete WinClip SDK workflow from packaging to plugin development and testing.

## Overview

The end-to-end workflow test verifies that:

1. The SDK can be packaged as a NuGet package
2. The plugin template can be installed and used
3. A new plugin can be created from the template
4. The plugin can be built successfully
5. The plugin can be loaded and tested with the SDK Test Harness

This ensures that the complete developer experience works as expected.

## Quick Start

### Automated Testing

The easiest way to run the end-to-end workflow test is using the automated test script:

```bash
./test-sdk-workflow.sh
```

This will run all verification steps automatically and provide clear feedback on each step.

### Options

```bash
# Run with verbose output to see detailed build logs
./test-sdk-workflow.sh --verbose

# Keep test artifacts after successful run (for inspection)
./test-sdk-workflow.sh --no-cleanup

# Show help
./test-sdk-workflow.sh --help
```

## Manual Testing Steps

If you prefer to run the steps manually or need to troubleshoot specific issues, follow these steps:

### Step 1: Pack the SDK

Pack the SDK into a NuGet package:

```bash
dotnet pack ./WinClip.SDK/WinClip.SDK.csproj -c Release
```

**Expected Result:**
- Package created at `./WinClip.SDK/bin/Release/WinClip.SDK.1.0.0.nupkg`
- No build errors or warnings
- Package includes SDK DLL, XML documentation, icon, and README

**Verification:**
```bash
# List package contents
unzip -l ./WinClip.SDK/bin/Release/WinClip.SDK.1.0.0.nupkg

# Should include:
# - lib/netstandard2.0/WinClip.SDK.dll
# - lib/netstandard2.0/WinClip.SDK.xml
# - icon.png
# - README.md
# - LICENSE.txt
```

### Step 2: Install the Plugin Template

Install the dotnet new template:

```bash
# Uninstall previous version if exists
dotnet new uninstall WinClip.Plugin.Template

# Install the template
dotnet new install ./Templates/WinClip.Plugin.Template
```

**Expected Result:**
- Template installed successfully
- Template appears in `dotnet new list`

**Verification:**
```bash
# List installed templates
dotnet new list | grep winclip-plugin

# Should show:
# winclip-plugin    WinClip Plugin    C#    WinClip/Plugin
```

### Step 3: Create a Test Plugin

Create a new plugin from the template:

```bash
dotnet new winclip-plugin -n TestPlugin \
    --PluginName "Test Plugin" \
    --PluginDescription "End-to-end test plugin" \
    --PluginAuthor "SDK Test Suite"
```

**Expected Result:**
- New directory `TestPlugin/` created
- Contains: `TestPlugin.csproj`, `MyPlugin.cs`, `plugin.json`

**Verification:**
```bash
# Check created files
ls -la TestPlugin/

# Verify plugin.json metadata
cat TestPlugin/plugin.json
```

### Step 4: Verify SDK Reference

Confirm the SDK reference is properly configured:

```bash
grep "WinClip.SDK" TestPlugin/TestPlugin.csproj
```

**Expected Result:**
- Should show: `<PackageReference Include="WinClip.SDK" Version="1.0.0" />`

### Step 5: Build the Test Plugin

Build the plugin project:

```bash
dotnet build ./TestPlugin/TestPlugin.csproj
```

**Expected Result:**
- Build succeeds with no errors
- Plugin DLL created at `./TestPlugin/bin/Debug/net8.0/TestPlugin.dll`
- SDK dependencies resolved correctly

**Verification:**
```bash
# Check DLL exists
ls -lh ./TestPlugin/bin/Debug/net8.0/TestPlugin.dll

# List dependencies
dotnet list ./TestPlugin/TestPlugin.csproj package
```

### Step 6: Test with SDK Test Harness

Load and test the plugin using the test harness:

```bash
# Build test harness
dotnet build ./WinClip.SDK.TestHarness/WinClip.SDK.TestHarness.csproj

# Run test harness with plugin
dotnet run --project ./WinClip.SDK.TestHarness -- \
    --plugin ./TestPlugin/bin/Debug/net8.0/TestPlugin.dll
```

**Expected Result:**
- Plugin loads successfully
- Plugin metadata displayed correctly
- Available actions listed
- Actions can be executed

**Interactive Testing:**

In the test harness menu, try these commands:

1. **Load plugin** - Should show plugin loaded successfully
2. **Show plugin info** - Display plugin metadata (name, version, author)
3. **Check if enabled** - Test ShouldEnable logic
4. **List actions** - Show all available actions
5. **Execute action** - Run a specific action by number
6. **Quick test** - Automated test of all actions

## Expected Output

### Successful Run

When all steps complete successfully, you should see output like:

```
╔════════════════════════════════════════════════════════════════╗
║    WinClip SDK End-to-End Workflow Test                       ║
╚════════════════════════════════════════════════════════════════╝

==> Checking prerequisites...
✓ Found dotnet CLI version 8.0.x

==> Step 1/6: Packing WinClip.SDK NuGet package...
✓ SDK package created: WinClip.SDK.1.0.0.nupkg (XXkB)

==> Step 2/6: Installing plugin template...
✓ Template installed successfully
✓ Template 'winclip-plugin' is available

==> Step 3/6: Creating test plugin from template...
✓ Test plugin created in ./TestPlugin
✓ Found TestPlugin.csproj
✓ Found MyPlugin.cs
✓ Found plugin.json

==> Step 4/6: Verifying SDK reference...
✓ SDK reference found in project file

==> Step 5/6: Building test plugin...
✓ Test plugin built successfully
✓ Plugin DLL created: ./TestPlugin/bin/Debug/net8.0/TestPlugin.dll (XkB)

==> Step 6/6: Testing plugin with SDK Test Harness...
✓ Test harness loaded plugin successfully
✓ Discovered 3 action(s)

╔════════════════════════════════════════════════════════════════╗
║    ✓ All SDK workflow tests passed successfully!              ║
╚════════════════════════════════════════════════════════════════╝

Summary:
  • SDK package created and verified
  • Plugin template installed successfully
  • Test plugin created from template
  • Test plugin built without errors
  • Plugin loaded and tested in SDK Test Harness

✓ SDK is ready for distribution!
```

## Troubleshooting

### SDK Package Not Created

**Problem:** `dotnet pack` fails or doesn't create package

**Solutions:**
- Check that .NET SDK 8.0 or later is installed: `dotnet --version`
- Verify WinClip.SDK.csproj has valid XML
- Check for missing files referenced in csproj (icon.png, LICENSE.txt, etc.)
- Review build output for specific errors

### Template Installation Fails

**Problem:** `dotnet new install` fails

**Solutions:**
- Ensure template.json is valid JSON
- Check that .template.config directory exists
- Verify template structure matches dotnet template requirements
- Try uninstalling first: `dotnet new uninstall WinClip.Plugin.Template`

### Plugin Build Fails

**Problem:** Test plugin doesn't build

**Solutions:**
- Verify SDK package was created successfully
- Check that SDK reference version matches packaged version
- Ensure .NET 8.0 SDK is installed
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Restore packages explicitly: `dotnet restore ./TestPlugin/TestPlugin.csproj`

### Test Harness Can't Load Plugin

**Problem:** Plugin loads but shows errors

**Solutions:**
- Verify plugin implements IPlugin interface correctly
- Check that plugin class is public
- Ensure plugin has PluginAttribute
- Review test harness output for specific error messages
- Check that all SDK dependencies are included in build output

### Template Parameters Not Working

**Problem:** Template creates plugin but parameters aren't substituted

**Solutions:**
- Check template.json symbols section
- Verify parameter names match in template.json and template files
- Ensure source files use correct parameter syntax: `MyPlugin` not `$MyPlugin$`

## Continuous Integration

### GitHub Actions Example

```yaml
name: SDK E2E Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test-sdk-workflow:
    runs-on: windows-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'

    - name: Run SDK E2E Workflow Test
      run: ./test-sdk-workflow.sh --verbose
      shell: bash
```

### Azure DevOps Pipeline

```yaml
trigger:
  branches:
    include:
    - main
    - develop

pool:
  vmImage: 'windows-latest'

steps:
- task: UseDotNet@2
  inputs:
    packageType: 'sdk'
    version: '8.0.x'

- script: |
    chmod +x ./test-sdk-workflow.sh
    ./test-sdk-workflow.sh --verbose
  displayName: 'Run SDK E2E Tests'
```

## What Gets Tested

### SDK Packaging
- [x] NuGet package creation
- [x] Package metadata (version, author, description)
- [x] Package contents (DLL, XML docs, icon, README, license)
- [x] Package size and structure

### Template System
- [x] Template installation
- [x] Template parameters work correctly
- [x] Generated project structure is valid
- [x] Generated files have correct content

### Plugin Development
- [x] Plugin project builds without errors
- [x] SDK reference resolves correctly
- [x] Plugin implements required interfaces
- [x] Plugin metadata is valid

### Testing Tools
- [x] Test harness can load plugins
- [x] Plugin actions are discovered
- [x] Actions can be executed
- [x] Error handling works correctly

## Success Criteria

The end-to-end workflow test passes when:

1. ✅ SDK packs without errors or warnings
2. ✅ Template installs and appears in `dotnet new list`
3. ✅ New plugin can be created from template
4. ✅ Plugin project builds successfully
5. ✅ Plugin DLL is created in expected location
6. ✅ Test harness loads plugin without errors
7. ✅ Plugin actions are discovered and listed
8. ✅ No runtime exceptions during plugin execution

## Next Steps

After successful end-to-end testing:

1. **Publish SDK to NuGet.org**
   ```bash
   dotnet nuget push ./WinClip.SDK/bin/Release/WinClip.SDK.1.0.0.nupkg \
       --api-key YOUR_API_KEY \
       --source https://api.nuget.org/v3/index.json
   ```

2. **Publish Template to NuGet.org**
   ```bash
   dotnet pack ./Templates/WinClip.Plugin.Template -c Release
   dotnet nuget push ./Templates/WinClip.Plugin.Template/bin/Release/WinClip.Plugin.Template.1.0.0.nupkg \
       --api-key YOUR_API_KEY \
       --source https://api.nuget.org/v3/index.json
   ```

3. **Update Documentation**
   - Add NuGet package links to README.md
   - Update Getting Started guide with published package versions
   - Create release notes

4. **Create GitHub Release**
   - Tag the release: `git tag v1.0.0`
   - Create release on GitHub with changelog
   - Attach SDK package and example plugins

## Related Documentation

- [Getting Started Guide](./GettingStarted.md) - For new plugin developers
- [Testing and Debugging](./Testing.md) - Detailed testing strategies
- [Publishing Guide](./Publishing.md) - How to distribute plugins
- [Test Harness README](../WinClip.SDK.TestHarness/README.md) - Test harness details

## Support

If you encounter issues during end-to-end testing:

1. Check this troubleshooting guide first
2. Review the [Testing documentation](./Testing.md)
3. Contact support via the [WinClip website](https://duncansmith.tech/winclip)
4. When reporting issues, include:
   - Complete error message
   - Steps to reproduce
   - Environment details (`dotnet --info`)
   - Test script output with `--verbose` flag
