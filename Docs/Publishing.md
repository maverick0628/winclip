# Publishing WinClip Plugins

This guide covers everything you need to know about packaging, distributing, and publishing your WinClip plugins to share with others.

## Table of Contents

- [Overview](#overview)
- [Preparing for Publication](#preparing-for-publication)
- [Packaging Your Plugin](#packaging-your-plugin)
- [Distribution Methods](#distribution-methods)
- [Versioning Your Plugin](#versioning-your-plugin)
- [Plugin Metadata](#plugin-metadata)
- [Creating a Release](#creating-a-release)
- [Listing in Plugin Directory](#listing-in-plugin-directory)
- [Supporting Your Plugin](#supporting-your-plugin)
- [Best Practices](#best-practices)

---

## Overview

Publishing your WinClip plugin allows others to benefit from your work. This guide will help you:

- **Package** your plugin properly for distribution
- **Choose** the right distribution method
- **Version** your plugin following best practices
- **Document** your plugin for users
- **Maintain** your plugin over time

---

## Preparing for Publication

Before publishing, ensure your plugin is ready:

### Quality Checklist

- [ ] **Functionality**: All features work as expected
- [ ] **Testing**: Thoroughly tested with various inputs
- [ ] **Error Handling**: Gracefully handles edge cases and errors
- [ ] **Performance**: Responsive and doesn't slow down WinClip
- [ ] **Documentation**: Clear README with usage instructions
- [ ] **Versioning**: Proper semantic version number
- [ ] **Licensing**: Appropriate license file included
- [ ] **No Hardcoded Paths**: Uses `GetPluginDataDirectory()` for storage
- [ ] **No Console Output**: Uses `context.Log()` instead of `Console.WriteLine()`
- [ ] **Clean Code**: Well-organized, commented, and maintainable

### Security Considerations

**IMPORTANT**: Plugins run with the same permissions as WinClip. Consider security:

- **No Malicious Code**: Never include code that could harm users
- **Validate Input**: Always validate clipboard data before processing
- **Network Requests**: Be transparent about external API calls
- **Data Privacy**: Don't collect or transmit user data without consent
- **Dependencies**: Audit all third-party libraries for security issues

### Legal Requirements

- **License**: Include a clear license (MIT, Apache 2.0, GPL, etc.)
- **Attribution**: Credit any code, libraries, or resources you've used
- **Trademark**: Don't use "WinClip" in your plugin name without permission
- **Terms**: Follow WinClip's plugin developer terms of service

---

## Packaging Your Plugin

### Basic Package Structure

A complete plugin package should include:

```
MyPlugin/
├── MyPlugin.dll              # Compiled plugin
├── MyPlugin.deps.json        # .NET dependencies
├── plugin.json               # Plugin metadata (optional)
├── README.md                 # Usage documentation
├── LICENSE.txt               # License file
├── CHANGELOG.md              # Version history
└── dependencies/             # Third-party DLLs (if any)
    └── Newtonsoft.Json.dll
```

### Creating a Release Build

Build your plugin in Release configuration for optimal performance:

```bash
# Clean previous builds
dotnet clean

# Build in Release mode
dotnet build -c Release

# Your plugin will be in:
# bin/Release/net8.0/MyPlugin.dll
```

### Including Dependencies

If your plugin uses NuGet packages, they need to be included:

#### Option 1: Publish with Dependencies

```bash
# Publish as self-contained
dotnet publish -c Release -o publish/

# This creates a folder with all dependencies
# Copy the entire publish/ folder to your distribution
```

#### Option 2: Manual Dependency Management

If you only want to include specific DLLs:

```bash
# Build normally
dotnet build -c Release

# Copy your plugin DLL and required dependencies
cp bin/Release/net8.0/MyPlugin.dll package/
cp bin/Release/net8.0/Newtonsoft.Json.dll package/dependencies/
```

**Note**: Don't include .NET runtime DLLs (System.*, Microsoft.*) as WinClip provides these.

### Creating plugin.json (Optional)

Add metadata for better plugin discovery:

```json
{
  "id": "my-awesome-plugin",
  "name": "My Awesome Plugin",
  "version": "1.0.0",
  "description": "Does awesome things with your clipboard",
  "author": "Your Name",
  "homepage": "https://github.com/yourusername/my-awesome-plugin",
  "license": "MIT",
  "minWinClipVersion": "1.0.0",
  "icon": "icon.png",
  "tags": ["text", "productivity", "utility"],
  "screenshots": [
    "screenshots/action-menu.png",
    "screenshots/result.png"
  ]
}
```

### Creating README.md

Every plugin should have a README with:

```markdown
# My Awesome Plugin

Brief description of what your plugin does.

## Features

- Feature 1
- Feature 2
- Feature 3

## Installation

### Manual Installation

1. Download the latest release from [releases page](link)
2. Extract the files to `%APPDATA%\WinClip\Plugins\MyAwesomePlugin\`
3. Restart WinClip or reload plugins

### Automatic Installation (if available)

```bash
winclip plugin install my-awesome-plugin
```

## Usage

1. Copy some text
2. The plugin will appear in WinClip menu
3. Select the action you want to perform

## Actions

### Action Name

Description of what this action does.

**Example**: Copy "hello world" → Action → Result

## Configuration

If your plugin has settings, explain them here.

## Requirements

- WinClip 1.0.0 or later
- .NET 8.0 Runtime
- Internet connection (if applicable)

## Known Issues

- Issue 1: Description and workaround
- Issue 2: Description and workaround

## Support

- **Issues**: [GitHub Issues](link)
- **Discussions**: [GitHub Discussions](link)
- **Email**: your@email.com

## License

MIT License - see [LICENSE.txt](LICENSE.txt)

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.
```

### Creating CHANGELOG.md

Document your version history:

```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-01-15

### Added
- Initial release
- Action for transforming text to uppercase
- Action for transforming text to lowercase
- Action for reversing text

### Changed
- N/A (initial release)

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

### Security
- N/A

## [Unreleased]

### Added
- Feature in development
```

---

## Distribution Methods

### 1. GitHub Releases (Recommended)

Host your plugin on GitHub for easy distribution:

#### Setup

1. **Create Repository**
   ```bash
   git init
   git add .
   git commit -m "Initial commit"
   git remote add origin https://github.com/yourusername/my-plugin.git
   git push -u origin main
   ```

2. **Tag Your Version**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. **Create Release**
   - Go to GitHub → Releases → New Release
   - Select your tag (v1.0.0)
   - Add release notes
   - Attach compiled plugin package (.zip)

#### Package for Release

```bash
# Build release
dotnet publish -c Release -o publish/MyPlugin

# Create zip archive
cd publish
zip -r MyPlugin-v1.0.0.zip MyPlugin/

# Upload to GitHub Releases
```

**Users can install**:
1. Download MyPlugin-v1.0.0.zip
2. Extract to `%APPDATA%\WinClip\Plugins\`
3. Restart WinClip

### 2. NuGet Package (Advanced)

Publish as a NuGet package for developers:

#### Create Package

Modify your `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>

    <!-- Package Metadata -->
    <PackageId>WinClip.Plugin.MyAwesomePlugin</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>My awesome WinClip plugin</Description>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageProjectUrl>https://github.com/yourusername/my-plugin</PackageProjectUrl>
    <RepositoryUrl>https://github.com/yourusername/my-plugin</RepositoryUrl>
    <PackageTags>winclip;plugin;clipboard</PackageTags>
    <PackageIcon>icon.png</PackageIcon>
    <PackageReadmeFile>README.md</PackageReadmeFile>
  </PropertyGroup>

  <ItemGroup>
    <None Include="icon.png" Pack="true" PackagePath="\" />
    <None Include="README.md" Pack="true" PackagePath="\" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="WinClip.SDK" Version="1.0.0" />
  </ItemGroup>
</Project>
```

#### Publish to NuGet

```bash
# Create package
dotnet pack -c Release

# Publish to NuGet.org
dotnet nuget push bin/Release/MyPlugin.1.0.0.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
```

### 3. Direct Download

Host the plugin on your website or file hosting service:

1. Create downloadable .zip package
2. Provide installation instructions
3. Keep a direct download link
4. Maintain version history page

### 4. WinClip Plugin Directory (Future)

WinClip may offer an official plugin directory in the future:

- Curated list of verified plugins
- One-click installation
- Automatic updates
- User ratings and reviews

Check [WinClip Documentation](https://winclip.dev/docs/plugins) for updates.

---

## Versioning Your Plugin

### Semantic Versioning

Follow [Semantic Versioning](https://semver.org/) (MAJOR.MINOR.PATCH):

**Format**: `MAJOR.MINOR.PATCH` (e.g., 2.3.1)

- **MAJOR**: Incompatible API changes (e.g., 1.x.x → 2.0.0)
- **MINOR**: New features, backwards compatible (e.g., 1.2.x → 1.3.0)
- **PATCH**: Bug fixes, backwards compatible (e.g., 1.2.3 → 1.2.4)

### Examples

```
1.0.0 - Initial release
1.0.1 - Fixed null reference bug
1.1.0 - Added new "reverse text" action
1.1.1 - Fixed reverse text performance issue
2.0.0 - Changed IPlugin interface (breaking change)
```

### Pre-release Versions

For beta or alpha releases:

```
1.0.0-alpha
1.0.0-alpha.1
1.0.0-beta
1.0.0-beta.2
1.0.0-rc.1 (release candidate)
1.0.0 (stable release)
```

### Version in Code

Update three places when versioning:

1. **Plugin Attribute**
   ```csharp
   [Plugin("my-plugin", "My Plugin", "1.2.0")]
   public class MyPlugin : PluginBase
   ```

2. **Project File**
   ```xml
   <Version>1.2.0</Version>
   <AssemblyVersion>1.2.0.0</AssemblyVersion>
   <FileVersion>1.2.0.0</FileVersion>
   ```

3. **CHANGELOG.md**
   ```markdown
   ## [1.2.0] - 2024-01-20
   ```

### Version Compatibility

Specify minimum WinClip version in `plugin.json`:

```json
{
  "minWinClipVersion": "1.0.0",
  "targetWinClipVersion": "1.2.0"
}
```

Or check at runtime:

```csharp
public override void Initialize(IClipboardContext context)
{
    var winclipVersion = context.GetWinClipVersion();
    if (winclipVersion < new Version("1.2.0"))
    {
        context.Log("Warning: This plugin requires WinClip 1.2.0 or later", LogLevel.Warning);
    }
}
```

---

## Plugin Metadata

### Essential Metadata

Include in your `[Plugin]` attribute:

```csharp
[Plugin(
    id: "url-utilities",                    // Unique identifier (lowercase-with-hyphens)
    name: "URL Utilities",                  // Display name
    version: "1.0.0",                       // Semantic version
    Description = "Utilities for working with URLs",
    Author = "John Doe",
    Url = "https://github.com/johndoe/url-utilities"
)]
public class UrlUtilitiesPlugin : PluginBase
{
    // Plugin implementation
}
```

### Extended Metadata (plugin.json)

Provide richer metadata in `plugin.json`:

```json
{
  "id": "url-utilities",
  "name": "URL Utilities",
  "version": "1.0.0",
  "description": "Parse, validate, and manipulate URLs from clipboard",
  "author": {
    "name": "John Doe",
    "email": "john@example.com",
    "url": "https://johndoe.dev"
  },
  "homepage": "https://github.com/johndoe/url-utilities",
  "repository": {
    "type": "git",
    "url": "https://github.com/johndoe/url-utilities.git"
  },
  "license": "MIT",
  "keywords": ["url", "link", "web", "http"],
  "minWinClipVersion": "1.0.0",
  "icon": "icon.png",
  "screenshots": [
    "screenshots/url-parse.png",
    "screenshots/url-encode.png"
  ],
  "categories": ["text", "web", "utilities"],
  "dependencies": {
    "WinClip.SDK": "1.0.0"
  }
}
```

---

## Creating a Release

### Release Checklist

Before creating a release:

- [ ] Update version number in all places
- [ ] Update CHANGELOG.md with changes
- [ ] Run all tests
- [ ] Test manually in WinClip
- [ ] Update documentation if needed
- [ ] Create git tag
- [ ] Build release package
- [ ] Write release notes

### Automated Release Process

Create a GitHub Actions workflow (`.github/workflows/release.yml`):

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
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
      run: dotnet build -c Release --no-restore

    - name: Test
      run: dotnet test -c Release --no-build

    - name: Publish
      run: dotnet publish -c Release -o publish/MyPlugin

    - name: Create Release Archive
      run: |
        cd publish
        Compress-Archive -Path MyPlugin -DestinationPath MyPlugin-${{ github.ref_name }}.zip

    - name: Create GitHub Release
      uses: softprops/action-gh-release@v1
      with:
        files: publish/MyPlugin-${{ github.ref_name }}.zip
        body_path: RELEASE_NOTES.md
        draft: false
        prerelease: false
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Release Notes Template

Create `RELEASE_NOTES.md` for each release:

```markdown
# MyPlugin v1.2.0

## 🎉 New Features

- Added support for processing multiple URLs at once
- New "Open All Links" action for lists of URLs
- Configuration dialog for setting default browser

## 🐛 Bug Fixes

- Fixed crash when clipboard contains invalid URL
- Fixed encoding issues with international characters in URLs
- Improved error messages for malformed URLs

## 🔧 Improvements

- 50% faster URL validation
- Better handling of edge cases
- Improved documentation

## ⚠️ Breaking Changes

None

## 📦 Installation

Download `MyPlugin-v1.2.0.zip` and extract to `%APPDATA%\WinClip\Plugins\`

## 📝 Full Changelog

See [CHANGELOG.md](CHANGELOG.md) for complete version history.

## 🙏 Thanks

Thanks to @contributor1 and @contributor2 for their contributions!
```

---

## Listing in Plugin Directory

### Submitting to WinClip Plugin Directory

When the official plugin directory is available:

1. **Prepare Submission**
   - Ensure plugin meets quality guidelines
   - Have clear documentation
   - Include screenshots/demo
   - Test thoroughly

2. **Submit Plugin**
   - Visit WinClip Plugin Directory
   - Click "Submit Plugin"
   - Fill out submission form
   - Provide repository URL
   - Upload icon and screenshots

3. **Review Process**
   - WinClip team reviews submission
   - May request changes or improvements
   - Approval typically within 7 days

4. **Publication**
   - Plugin appears in directory
   - Users can install with one click
   - Updates can be pushed automatically

### Plugin Listing Requirements

- **Quality**: No crashes, good error handling
- **Documentation**: Clear README and usage guide
- **Security**: No malicious code, audited dependencies
- **Performance**: Doesn't slow down WinClip noticeably
- **Unique Value**: Provides unique functionality
- **Maintenance**: Active maintenance and support

---

## Supporting Your Plugin

### Issue Tracking

Enable GitHub Issues for bug reports and feature requests:

1. **Issue Templates**

Create `.github/ISSUE_TEMPLATE/bug_report.md`:

```markdown
---
name: Bug report
about: Report a bug in the plugin
---

**Describe the bug**
A clear description of the bug.

**To Reproduce**
Steps to reproduce:
1. Copy '...'
2. Click on '...'
3. See error

**Expected behavior**
What you expected to happen.

**Environment**
- WinClip Version: [e.g., 1.2.0]
- Plugin Version: [e.g., 1.0.0]
- OS: [e.g., Windows 11]

**Additional context**
Any other relevant information.
```

2. **Responding to Issues**
   - Respond within 24-48 hours
   - Ask for clarification if needed
   - Reproduce the bug
   - Fix and release patch version
   - Close issue with fix version

### Documentation

Maintain comprehensive documentation:

- **README.md**: Overview and quick start
- **USAGE.md**: Detailed usage guide
- **API.md**: For plugins with exposed APIs
- **FAQ.md**: Common questions
- **CONTRIBUTING.md**: For open source projects

### Community

Build a community around your plugin:

- **GitHub Discussions**: Q&A and feature discussions
- **Discord/Slack**: Real-time support
- **Twitter/Social**: Updates and tips
- **Blog**: Deep dives and tutorials

### Updates

Keep your plugin maintained:

- **Bug Fixes**: Address critical bugs quickly (patch releases)
- **Features**: Regular feature updates (minor releases)
- **Compatibility**: Update for new WinClip versions
- **Dependencies**: Keep dependencies updated and secure
- **Deprecation**: Announce breaking changes well in advance

---

## Best Practices

### Documentation

- **Write for beginners**: Don't assume technical knowledge
- **Include examples**: Show, don't just tell
- **Keep it updated**: Update docs with each release
- **Add screenshots**: Visuals help understanding
- **Write clearly**: Use simple, direct language

### Code Quality

- **Follow conventions**: Match WinClip SDK patterns
- **Add comments**: Explain complex logic
- **Handle errors**: Never let exceptions crash WinClip
- **Optimize performance**: Keep actions fast (<100ms ideal)
- **Write tests**: Prevent regressions

### User Experience

- **Clear action names**: Use descriptive, action-oriented names
- **Helpful notifications**: Inform users of results
- **Graceful degradation**: Handle edge cases smoothly
- **Consistent behavior**: Be predictable
- **Respect user data**: Don't modify clipboard unexpectedly

### Security

- **Validate all input**: Never trust clipboard data
- **Minimize permissions**: Only request what you need
- **Audit dependencies**: Check all third-party libraries
- **No telemetry without consent**: Be transparent about data collection
- **Update dependencies**: Keep libraries patched

### Maintenance

- **Monitor issues**: Stay on top of bug reports
- **Version regularly**: Release updates consistently
- **Communicate changes**: Keep users informed
- **Deprecate gracefully**: Give users time to migrate
- **Archive responsibly**: If abandoning, communicate clearly

### Marketing

- **Create demo video**: Show plugin in action
- **Write blog post**: Explain your plugin's value
- **Share on social media**: Reach potential users
- **Submit to directories**: Get discovered
- **Collect feedback**: Learn from users

---

## Next Steps

Now that you know how to publish your plugin:

- **[Best Practices](./BestPractices.md)** - Professional development tips
- **[Testing Guide](./Testing.md)** - Ensure quality before publishing
- **[Plugin API Reference](./PluginAPI.md)** - Complete API documentation
- **[Getting Started](./GettingStarted.md)** - Build your first plugin

---

**Ready to Publish?** 🚀

Share your plugin with the WinClip community and help others be more productive!

**Questions?** Open an issue on [GitHub](https://github.com/winclip/sdk/issues) or join the [community discussions](https://github.com/winclip/sdk/discussions).
