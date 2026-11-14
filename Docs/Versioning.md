# Versioning Guide

This guide explains versioning practices for both the WinClip SDK and plugins built with the SDK.

## Table of Contents

- [Overview](#overview)
- [Semantic Versioning](#semantic-versioning)
- [Versioning the SDK](#versioning-the-sdk)
- [Versioning Your Plugin](#versioning-your-plugin)
- [Pre-release Versions](#pre-release-versions)
- [Version Compatibility](#version-compatibility)
- [Breaking Changes](#breaking-changes)
- [Migration Guides](#migration-guides)
- [Best Practices](#best-practices)
- [Common Scenarios](#common-scenarios)

---

## Overview

Proper versioning is essential for:

- **Clear communication** of changes to users and developers
- **Dependency management** in projects using the SDK
- **Backward compatibility** planning and testing
- **Update strategies** for users and plugin developers
- **Release management** and quality assurance

Both the WinClip SDK and plugins built with it follow [Semantic Versioning](https://semver.org/) (SemVer) 2.0.0 principles.

---

## Semantic Versioning

Semantic Versioning uses a three-part version number: `MAJOR.MINOR.PATCH`

### Version Format

```
MAJOR.MINOR.PATCH
  │      │     │
  │      │     └─── Bug fixes (backwards compatible)
  │      └─────── New features (backwards compatible)
  └────────────── Breaking changes (NOT backwards compatible)
```

### Version Components

#### MAJOR Version

Increment when making **incompatible API changes**:

- Removing public APIs or interfaces
- Changing method signatures (parameters, return types)
- Changing behavior in breaking ways
- Renaming public classes, methods, or properties
- Changing default behaviors that could break existing code

**Example**: `1.5.3` → `2.0.0`

#### MINOR Version

Increment when **adding functionality** in a backwards compatible manner:

- Adding new public APIs, interfaces, or classes
- Adding new optional parameters with defaults
- Adding new plugin actions
- Adding new IActionContext methods
- Improving performance without breaking changes
- Deprecating APIs (marking for future removal)

**Example**: `1.5.3` → `1.6.0`

#### PATCH Version

Increment when making **backwards compatible bug fixes**:

- Fixing bugs without changing APIs
- Fixing documentation errors
- Internal refactoring with no API changes
- Performance improvements with no API changes
- Security patches (when not breaking)

**Example**: `1.5.3` → `1.5.4`

### Additional Rules

1. **Major version zero** (`0.y.z`) is for initial development. Anything may change at any time.
2. **Version 1.0.0** defines the first stable public API.
3. When **MAJOR** increments, reset MINOR and PATCH to 0 (e.g., `1.9.5` → `2.0.0`)
4. When **MINOR** increments, reset PATCH to 0 (e.g., `1.5.9` → `1.6.0`)
5. Once released, a version's content **must not be modified**. Any changes require a new version.

---

## Versioning the SDK

### SDK Version Guidelines

The WinClip SDK follows strict semantic versioning to ensure plugin developers can rely on stable APIs.

#### SDK MAJOR Version Changes

Increment SDK MAJOR version when:

- Changing `IPlugin`, `IClipboardContext`, or `IActionContext` interfaces
- Removing or renaming public classes, interfaces, or methods
- Changing method signatures in breaking ways
- Changing default behaviors that plugins rely on
- Upgrading to incompatible .NET target framework

**Example Scenarios**:
```
1.x.x → 2.0.0: Added required method to IPlugin interface
2.x.x → 3.0.0: Changed IActionContext.SetClipboard() parameters
3.x.x → 4.0.0: Removed deprecated PluginAttribute.Category property
```

#### SDK MINOR Version Changes

Increment SDK MINOR version when:

- Adding new interfaces, classes, or attributes
- Adding new methods to `IClipboardContext` or `IActionContext`
- Adding new plugin metadata properties
- Adding new optional parameters with defaults
- Adding new example plugins or templates
- Deprecating APIs with backward compatibility

**Example Scenarios**:
```
1.0.x → 1.1.0: Added IActionContext.GetPluginDataDirectory() method
1.1.x → 1.2.0: Added ImageTransform example plugin
1.2.x → 1.3.0: Added PluginAttribute.Category property (optional)
```

#### SDK PATCH Version Changes

Increment SDK PATCH version when:

- Fixing bugs in SDK classes (e.g., PluginBase)
- Fixing documentation errors
- Improving internal implementations without API changes
- Updating example plugins without adding new ones
- Fixing NuGet package metadata

**Example Scenarios**:
```
1.0.0 → 1.0.1: Fixed PluginBase action discovery with inherited methods
1.1.0 → 1.1.1: Fixed XML documentation comments
1.2.0 → 1.2.1: Updated example plugin documentation
```

### SDK Version Locations

When releasing a new SDK version, update these locations:

#### 1. Project File (`WinClip.SDK/WinClip.SDK.csproj`)

```xml
<PropertyGroup>
  <Version>1.2.3</Version>
  <AssemblyVersion>1.2.3.0</AssemblyVersion>
  <FileVersion>1.2.3.0</FileVersion>
  <PackageReleaseNotes>Brief description of changes</PackageReleaseNotes>
</PropertyGroup>
```

#### 2. Changelog (`CHANGELOG.md`)

```markdown
## [1.2.3] - 2026-02-15

### Fixed
- Fixed bug in PluginBase action discovery
- Updated documentation for IActionContext

[1.2.3]: https://github.com/yourusername/winclip-sdk/releases/tag/v1.2.3
```

#### 3. README.md (if needed)

Update version badge and examples if they reference specific versions.

#### 4. Git Tag

```bash
git tag -a v1.2.3 -m "Release version 1.2.3"
git push origin v1.2.3
```

---

## Versioning Your Plugin

### Plugin Version Guidelines

Plugin versioning follows the same semantic versioning principles:

- **MAJOR**: Breaking changes for plugin users (removed actions, changed behavior)
- **MINOR**: New features or actions (backwards compatible)
- **PATCH**: Bug fixes and improvements

#### Plugin Version Locations

Update version in three places:

#### 1. Plugin Attribute

```csharp
[Plugin("my-plugin", "My Plugin", "1.2.3",
    Author = "Your Name",
    Description = "Plugin description")]
public class MyPlugin : PluginBase
{
    // ...
}
```

#### 2. Project File (`MyPlugin.csproj`)

```xml
<PropertyGroup>
  <Version>1.2.3</Version>
  <AssemblyVersion>1.2.3.0</AssemblyVersion>
  <FileVersion>1.2.3.0</FileVersion>
</PropertyGroup>
```

#### 3. Plugin Changelog (`CHANGELOG.md`)

```markdown
## [1.2.3] - 2026-02-15

### Fixed
- Fixed null reference when clipboard is empty
```

### Plugin Versioning Examples

#### Scenario 1: Adding a New Action (MINOR)

```
Before (1.0.0):
[Action("uppercase")] public void Uppercase() { }
[Action("lowercase")] public void Lowercase() { }

After (1.1.0):
[Action("uppercase")] public void Uppercase() { }
[Action("lowercase")] public void Lowercase() { }
[Action("titlecase")] public void TitleCase() { }  // NEW ACTION
```

**Version Change**: `1.0.0` → `1.1.0`

#### Scenario 2: Removing an Action (MAJOR)

```
Before (1.2.0):
[Action("uppercase")] public void Uppercase() { }
[Action("lowercase")] public void Lowercase() { }
[Action("old-action")] public void OldAction() { }

After (2.0.0):
[Action("uppercase")] public void Uppercase() { }
[Action("lowercase")] public void Lowercase() { }
// OldAction removed - BREAKING CHANGE
```

**Version Change**: `1.2.0` → `2.0.0`

#### Scenario 3: Fixing a Bug (PATCH)

```
Before (1.1.0):
public void Uppercase()
{
    string text = clipboard.GetText();
    action.SetClipboard(text.ToUpper());  // BUG: Doesn't check for null
}

After (1.1.1):
public void Uppercase()
{
    string text = clipboard.GetText();
    if (string.IsNullOrEmpty(text)) return;  // FIX: Null check added
    action.SetClipboard(text.ToUpper());
}
```

**Version Change**: `1.1.0` → `1.1.1`

---

## Pre-release Versions

Use pre-release versions for testing before stable release.

### Pre-release Format

```
MAJOR.MINOR.PATCH-LABEL.NUMBER
```

### Pre-release Labels

#### alpha

Early development, unstable API:

```
1.0.0-alpha
1.0.0-alpha.1
1.0.0-alpha.2
```

#### beta

Feature complete, testing phase:

```
1.0.0-beta
1.0.0-beta.1
1.0.0-beta.2
```

#### rc (Release Candidate)

Final testing before stable release:

```
1.0.0-rc.1
1.0.0-rc.2
```

### Pre-release Progression

Typical progression for a new major version:

```
1.9.5           (last stable version)
2.0.0-alpha.1   (early development)
2.0.0-alpha.2   (more features added)
2.0.0-beta.1    (feature complete, testing)
2.0.0-beta.2    (bug fixes)
2.0.0-rc.1      (release candidate)
2.0.0-rc.2      (final fixes)
2.0.0           (stable release)
```

### Using Pre-release Versions

#### In Code

```csharp
[Plugin("my-plugin", "My Plugin", "2.0.0-beta.1")]
public class MyPlugin : PluginBase { }
```

#### In Project File

```xml
<Version>2.0.0-beta.1</Version>
```

#### In NuGet

```bash
dotnet pack -c Release /p:VersionSuffix=beta.1
```

---

## Version Compatibility

### SDK Compatibility

Plugin developers should specify minimum SDK version they support.

#### Option 1: Package Reference (Recommended)

In your plugin's `.csproj`:

```xml
<ItemGroup>
  <!-- Minimum version -->
  <PackageReference Include="WinClip.SDK" Version="1.0.0" />

  <!-- Exact version -->
  <PackageReference Include="WinClip.SDK" Version="[1.2.0]" />

  <!-- Version range -->
  <PackageReference Include="WinClip.SDK" Version="[1.0.0,2.0.0)" />
</ItemGroup>
```

#### Option 2: Runtime Check

Check SDK version at runtime during action execution (preferred):

```csharp
// Check version during action execution (PREFERRED - can log properly)
private bool _versionChecked = false;

[Action("my-action", "My Action")]
public void MyAction(IActionContext context)
{
    // Check version once on first action execution
    if (!_versionChecked)
    {
        _versionChecked = true;
        var sdkVersion = typeof(IPlugin).Assembly.GetName().Version;
        if (sdkVersion < new Version(1, 2, 0))
        {
            context.Log($"Warning: This plugin requires SDK 1.2.0+, found {sdkVersion}",
                       LogLevel.Warning);
        }
    }

    // ... rest of action logic ...
}
```

Alternative - check during initialization (but cannot log):

```csharp
public override void Initialize(IClipboardContext context)
{
    var sdkVersion = typeof(IPlugin).Assembly.GetName().Version;
    if (sdkVersion < new Version(1, 2, 0))
    {
        // Note: Cannot use Log() here - IActionContext not available during Initialize
        // Store this information to log during first action execution
        System.Diagnostics.Debug.WriteLine($"Warning: Plugin requires SDK 1.2.0+, found {sdkVersion}");
    }
}
```

#### Option 3: Plugin Metadata (`plugin.json`)

```json
{
  "id": "my-plugin",
  "version": "1.0.0",
  "minSdkVersion": "1.0.0",
  "maxSdkVersion": "2.0.0",
  "targetSdkVersion": "1.2.0"
}
```

### WinClip Version Compatibility

Specify minimum WinClip application version if your plugin requires specific features.

#### In plugin.json

```json
{
  "minWinClipVersion": "1.0.0",
  "targetWinClipVersion": "1.2.0"
}
```

#### Runtime Check

```csharp
// Check during action execution when IActionContext is available
private bool _winclipVersionChecked = false;

[Action("my-action", "My Action")]
public void MyAction(IActionContext context)
{
    if (!_winclipVersionChecked)
    {
        _winclipVersionChecked = true;
        // Hypothetical future API
        if (context is IWinClipVersion versionInfo)
        {
            var winclipVersion = versionInfo.GetApplicationVersion();
            if (winclipVersion < new Version("1.2.0"))
            {
                context.ShowNotification(
                    "This plugin works best with WinClip 1.2.0 or later",
                    NotificationType.Warning);
            }
        }
    }

    // ... rest of action logic ...
}
```

---

## Breaking Changes

### What Constitutes a Breaking Change?

Breaking changes require a MAJOR version bump.

#### For SDK (affects plugin developers)

- ✅ Removing public API
- ✅ Changing method signatures
- ✅ Changing interface definitions
- ✅ Renaming public members
- ✅ Changing default behavior
- ✅ Removing or renaming attributes
- ✅ Changing exception types thrown
- ❌ Adding new optional API
- ❌ Deprecating API (still works)
- ❌ Internal refactoring
- ❌ Documentation updates
- ❌ Bug fixes

#### For Plugins (affects users)

- ✅ Removing actions
- ✅ Changing action behavior significantly
- ✅ Changing keyboard shortcuts
- ✅ Requiring new permissions
- ✅ Removing configuration options
- ❌ Adding new actions
- ❌ Improving performance
- ❌ Fixing bugs
- ❌ Adding configuration options

### Handling Breaking Changes

#### Step 1: Deprecation (MINOR version)

In version `1.5.0`, mark API as deprecated:

```csharp
[Obsolete("Use NewMethod() instead. This method will be removed in version 2.0.0")]
public void OldMethod()
{
    // Redirect to new method for backward compatibility
    NewMethod();
}

public void NewMethod()
{
    // New implementation
}
```

#### Step 2: Removal (MAJOR version)

In version `2.0.0`, remove deprecated API:

```csharp
// OldMethod() removed completely
public void NewMethod()
{
    // New implementation
}
```

### Documenting Breaking Changes

#### In CHANGELOG.md

```markdown
## [2.0.0] - 2026-03-01

### Breaking Changes

- **Removed**: `OldMethod()` - Use `NewMethod()` instead
- **Changed**: `IActionContext.SetClipboard()` now requires content type parameter
- **Renamed**: `PluginAttribute.Category` → `PluginAttribute.Tags`

### Migration Guide

See [MIGRATION-2.0.md](MIGRATION-2.0.md) for detailed upgrade instructions.
```

---

## Migration Guides

Create migration guides for MAJOR version releases to help users upgrade.

### Migration Guide Template

Create `MIGRATION-{VERSION}.md`:

```markdown
# Migration Guide: Upgrading to 2.0.0

This guide helps you upgrade from 1.x to 2.0.0.

## Breaking Changes

### 1. IActionContext.SetClipboard() Signature Changed

**Before (1.x)**:
```csharp
action.SetClipboard(text);
```

**After (2.0)**:
```csharp
action.SetClipboard(text, ClipboardContentType.Text);
```

**Migration**: Add `ClipboardContentType.Text` parameter.

### 2. PluginAttribute.Category Removed

**Before (1.x)**:
```csharp
[Plugin("id", "name", "1.0.0", Category = "Text")]
```

**After (2.0)**:
```csharp
[Plugin("id", "name", "1.0.0", Tags = new[] { "Text" })]
```

**Migration**: Replace `Category` with `Tags` array.

## New Features in 2.0

- Added support for image clipboard content
- New `IActionContext.GetPluginDataDirectory()` method
- Performance improvements in action discovery

## Deprecations

- `OldMethod()` is deprecated, use `NewMethod()` instead (will be removed in 3.0)

## Testing Your Plugin

After upgrading:

1. Update SDK package reference:
   ```xml
   <PackageReference Include="WinClip.SDK" Version="2.0.0" />
   ```

2. Fix compilation errors following the migration steps above

3. Test thoroughly with Test Harness:
   ```bash
   dotnet run --project WinClip.SDK.TestHarness -- --plugin YourPlugin.dll
   ```

4. Update your plugin version following semantic versioning:
   - If you made breaking changes: increment MAJOR
   - If you only added features: increment MINOR
   - If you only fixed bugs: increment PATCH
```

---

## Best Practices

### For SDK Maintainers

1. **Never break compatibility in MINOR/PATCH versions**
   - Only add, never remove or change existing APIs
   - Use deprecation before removal

2. **Provide migration guides for MAJOR versions**
   - Document all breaking changes
   - Provide before/after code examples
   - Include testing instructions

3. **Use pre-release versions for testing**
   - Alpha: internal testing
   - Beta: community testing
   - RC: final validation before release

4. **Document all changes in CHANGELOG**
   - Be specific and detailed
   - Include code examples
   - Link to related issues/PRs

5. **Test thoroughly before releasing**
   - Build all example plugins
   - Run Test Harness against examples
   - Verify NuGet package creation

### For Plugin Developers

1. **Follow semantic versioning strictly**
   - Users depend on version numbers for updates
   - Breaking changes = MAJOR bump

2. **Maintain a CHANGELOG**
   - Users need to know what changed
   - Include migration notes for breaking changes

3. **Test with multiple SDK versions**
   - Test with minimum supported SDK version
   - Test with latest SDK version
   - Document version compatibility

4. **Deprecate before removing**
   - Give users time to migrate
   - Provide clear deprecation messages
   - Document replacements

5. **Use version tags in git**
   - Tag each release
   - Makes it easy to track history
   - Enables easy rollback if needed

6. **Document version compatibility**
   - Specify minimum SDK version
   - Specify minimum WinClip version
   - Update documentation when requirements change

---

## Common Scenarios

### Scenario 1: First Release

```
Initial development: 0.1.0, 0.2.0, 0.3.0
First stable release: 1.0.0
```

**Tip**: Use 0.x.x during initial development, 1.0.0 for first stable public release.

### Scenario 2: Bug Fix

```
Current version: 1.2.3
Bug fix: 1.2.3 → 1.2.4
```

**Tip**: Only increment PATCH for bug fixes with no API changes.

### Scenario 3: New Feature

```
Current version: 1.2.4
New feature: 1.2.4 → 1.3.0
```

**Tip**: Reset PATCH to 0 when incrementing MINOR.

### Scenario 4: Breaking Change

```
Current version: 1.5.9
Breaking change: 1.5.9 → 2.0.0
```

**Tip**: Reset MINOR and PATCH to 0 when incrementing MAJOR.

### Scenario 5: Multiple Changes

If a release includes bug fixes, new features, AND breaking changes:

```
Current version: 1.5.9

Changes:
- Fixed null reference bug (PATCH)
- Added new action (MINOR)
- Removed old action (MAJOR)

Result: 1.5.9 → 2.0.0
```

**Tip**: The highest-priority change determines the version bump (MAJOR > MINOR > PATCH).

### Scenario 6: Beta Testing

```
Current stable: 1.5.9
Next version will be: 2.0.0 (breaking changes)

Beta progression:
1.5.9 → 2.0.0-beta.1 → 2.0.0-beta.2 → 2.0.0-rc.1 → 2.0.0
```

**Tip**: Use pre-release versions for community testing before stable release.

### Scenario 7: Security Patch

```
Current version: 1.2.3
Security fix (no API changes): 1.2.3 → 1.2.4
Security fix (breaking changes): 1.2.3 → 2.0.0
```

**Tip**: Security fixes are PATCH unless they require breaking changes.

### Scenario 8: Deprecating an API

```
Version 1.5.0 (MINOR):
- Mark OldMethod() as [Obsolete]
- Add NewMethod()
- OldMethod() still works, calls NewMethod()

Version 2.0.0 (MAJOR):
- Remove OldMethod()
- NewMethod() is the only option
```

**Tip**: Always deprecate first (MINOR), remove later (MAJOR).

---

## Tools and Automation

### Automatic Version Bumping

Use tools to automate version management:

#### PowerShell Script

```powershell
# bump-version.ps1
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('major','minor','patch')]
    [string]$Type
)

$csproj = "WinClip.SDK/WinClip.SDK.csproj"
$xml = [xml](Get-Content $csproj)
$currentVersion = [Version]$xml.Project.PropertyGroup.Version

$newVersion = switch ($Type) {
    'major' { [Version]::new($currentVersion.Major + 1, 0, 0) }
    'minor' { [Version]::new($currentVersion.Major, $currentVersion.Minor + 1, 0) }
    'patch' { [Version]::new($currentVersion.Major, $currentVersion.Minor, $currentVersion.Build + 1) }
}

$xml.Project.PropertyGroup.Version = $newVersion.ToString()
$xml.Save($csproj)

Write-Host "Version bumped from $currentVersion to $newVersion"
```

Usage:
```powershell
.\bump-version.ps1 -Type patch
```

### Git Hooks

Create `.git/hooks/pre-commit` to enforce version updates:

```bash
#!/bin/bash
# Check if version was updated in CHANGELOG.md
if git diff --cached --name-only | grep -q "CHANGELOG.md"; then
    if ! git diff --cached CHANGELOG.md | grep -q "^+## \["; then
        echo "Error: CHANGELOG.md modified but no new version entry added"
        exit 1
    fi
fi
```

### GitHub Actions

Automate releases with GitHub Actions:

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Build
        run: dotnet build -c Release

      - name: Pack
        run: dotnet pack -c Release --no-build

      - name: Publish to NuGet
        run: dotnet nuget push **/*.nupkg -k ${{ secrets.NUGET_API_KEY }} -s https://api.nuget.org/v3/index.json
```

---

## References

- [Semantic Versioning 2.0.0](https://semver.org/)
- [Keep a Changelog](https://keepachangelog.com/)
- [NuGet Versioning](https://docs.microsoft.com/en-us/nuget/concepts/package-versioning)
- [.NET Assembly Versioning](https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/versioning)
- [Publishing Guide](Publishing.md)
- [CHANGELOG.md](../CHANGELOG.md)

---

## Quick Reference

### Version Bump Decision Tree

```
Did you make changes? ──────────────────────────────────────┐
    │                                                         │
    ↓                                                         ↓
Are there breaking changes?                                 No changes
    │                                                    (no version bump)
    ├─── Yes → MAJOR version bump (2.0.0)
    │
    ↓
Are there new features?
    │
    ├─── Yes → MINOR version bump (1.3.0)
    │
    ↓
Are there bug fixes?
    │
    └─── Yes → PATCH version bump (1.2.4)
```

### Quick Commands

```bash
# Create and push version tag
git tag -a v1.2.3 -m "Release 1.2.3"
git push origin v1.2.3

# Build release package
dotnet pack WinClip.SDK/WinClip.SDK.csproj -c Release

# Publish to NuGet
dotnet nuget push WinClip.SDK/bin/Release/WinClip.SDK.1.2.3.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json

# Install specific SDK version in your plugin
dotnet add package WinClip.SDK --version 1.2.3
```

---

*For more information, see the [Publishing Guide](Publishing.md) and [CHANGELOG.md](../CHANGELOG.md).*
