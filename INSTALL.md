# WinClip Installation Guide

WinClip can be installed through multiple methods to suit your preference. Choose the installation method that works best for you.

## Table of Contents

- [Package Managers (Recommended)](#package-managers-recommended)
  - [Winget](#winget)
  - [Chocolatey](#chocolatey)
- [Microsoft Store](#microsoft-store)
- [Build from Source](#build-from-source)
- [Direct Download](#direct-download)
- [System Requirements](#system-requirements)
- [Post-Installation](#post-installation)
- [Updating WinClip](#updating-winclip)
- [Uninstallation](#uninstallation)
- [Troubleshooting](#troubleshooting)

---

## Package Managers (Recommended)

Package managers provide the easiest way to install and keep WinClip up to date. Choose your preferred package manager below.

### Winget

**Windows Package Manager** (Winget) is Microsoft's official package manager, pre-installed on Windows 10 (1809+) and Windows 11.

#### Install

```powershell
winget install WinClip.WinClip
```

#### Features
- ✅ Official Microsoft package manager
- ✅ Pre-installed on modern Windows
- ✅ Automatic updates available
- ✅ Integrates with Windows settings

#### Requirements
- Windows 10 version 1809 or later
- Windows 11 (any version)

---

### Chocolatey

**Chocolatey** is a popular package manager for Windows, especially favored in enterprise environments.

#### Install

```powershell
choco install winclip
```

#### Features
- ✅ Popular in enterprise environments
- ✅ Robust dependency management
- ✅ Silent installation support
- ✅ Extensive package repository

#### Prerequisites

If you don't have Chocolatey installed, install it first:

```powershell
# Run in PowerShell as Administrator
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
```

For more information, visit: https://chocolatey.org/install

---

WinClip isn't in a public Scoop bucket. A manifest lives in `distribution/scoop/`, but it isn't published yet.

---

## Microsoft Store

WinClip is a free download in the [Microsoft Store](https://apps.microsoft.com/detail/9P6G2V1SB97M).

Install from the Store app, or from PowerShell:

```powershell
winget install --id 9P6G2V1SB97M --source msstore
```

The Store keeps WinClip up to date.

---

## Build from Source

WinClip is open source. To build it yourself you need Windows 10 or 11 and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0):

```powershell
git clone https://github.com/maverick0628/winclip.git
cd winclip
dotnet build WinClip.sln -c Release
dotnet test WinClip.Tests/WinClip.Tests.csproj -c Release
dotnet test src/WinClip.Tests/WinClip.Tests.csproj -c Release
```

The release installers are built from the Inno Setup scripts in `installer/`; see [installer/README.md](installer/README.md).

---

## Direct Download

If you prefer not to use a package manager, you can download the installer directly.

### Download Links

Download the latest version from [duncansmith.tech/apps/winclip](https://duncansmith.tech/apps/winclip/):

- **x64**: `WinClip-x.x.x-x64-Setup.exe` for most 64-bit Windows PCs
- **ARM64**: `WinClip-x.x.x-arm64-Setup.exe` for ARM-based Windows PCs

### Installation Steps

1. Download the appropriate installer for your system architecture
2. Double-click the downloaded `.exe` file
3. Follow the installation wizard
4. WinClip will be installed and ready to use

### Verify Download Integrity

To verify the integrity of your download, check the SHA256 checksum:

```powershell
# Run in PowerShell
Get-FileHash "path\to\WinClip-x.x.x-x64-Setup.exe" -Algorithm SHA256
```

Compare the output with the checksum listed on the download page.

---

## System Requirements

### Minimum Requirements
- **Operating System**: Windows 10 (version 1809 or later) or Windows 11
- **Architecture**: x64 or ARM64
- **RAM**: 512 MB
- **Disk Space**: 50 MB free space
- **Display**: 1024x768 or higher resolution

### Recommended Requirements
- **Operating System**: Windows 10 (version 21H2 or later) or Windows 11
- **Architecture**: x64 or ARM64
- **RAM**: 2 GB or more
- **Disk Space**: 100 MB free space
- **Display**: 1920x1080 or higher resolution

---

## Post-Installation

After installing WinClip, follow these steps to get started:

1. **Launch WinClip**
   - Find WinClip in the Start Menu
   - Or press `Win + R`, type `winclip`, and press Enter

2. **Configure Preferences** (Optional)
   - Open WinClip settings
   - Configure keyboard shortcuts
   - Set clipboard history size
   - Choose startup behavior

3. **Test Functionality**
   - Copy some text to test clipboard capture
   - Access clipboard history with the default hotkey
   - Verify clipboard items are being tracked

---

## Updating WinClip

### Using Package Managers

#### Winget
```powershell
winget upgrade WinClip.WinClip
```

Or update all packages:
```powershell
winget upgrade --all
```

#### Chocolatey
```powershell
choco upgrade winclip
```

Or update all packages:
```powershell
choco upgrade all
```

#### Microsoft Store
The Store updates WinClip automatically.

### Manual Update

1. Download the latest installer from the [WinClip website](https://duncansmith.tech/apps/winclip)
2. Run the installer - it will automatically detect and upgrade your existing installation
3. Your settings and clipboard history will be preserved

---

## Uninstallation

### Using Package Managers

#### Winget
```powershell
winget uninstall WinClip.WinClip
```

#### Chocolatey
```powershell
choco uninstall winclip
```

### Manual Uninstallation

1. Open **Settings** > **Apps** > **Installed apps** (Windows 11) or **Apps & features** (Windows 10)
2. Find **WinClip** in the list
3. Click the three dots (...) or click on the app
4. Select **Uninstall**
5. Follow the uninstallation wizard

### Complete Removal

To remove all user data and settings after uninstallation:

```powershell
# Remove user data directory
Remove-Item -Path "$env:APPDATA\WinClip" -Recurse -Force

# Remove local cache
Remove-Item -Path "$env:LOCALAPPDATA\WinClip" -Recurse -Force
```

**Note**: This will permanently delete your clipboard history and settings.

---

## Troubleshooting

### Installation Issues

#### "Installation failed" or "Installer error"

**Solution**:
1. Run the installer as Administrator
   - Right-click the installer
   - Select "Run as administrator"
2. Temporarily disable antivirus software
3. Ensure you have sufficient disk space
4. Check Windows Event Viewer for detailed error messages

#### Package manager reports "Package not found"

**Solution**:
- **Winget**: Update the package source
  ```powershell
  winget source update
  ```
- **Chocolatey**: Ensure you have the latest Chocolatey version
  ```powershell
  choco upgrade chocolatey
  ```

#### "Access denied" or permission errors

**Solution**:
1. Run PowerShell or Command Prompt as Administrator
2. Check that your user account has installation permissions

### Runtime Issues

#### WinClip doesn't start after installation

**Solution**:
1. Check if the process is running in Task Manager
2. Try launching from Start Menu
3. Check Windows Event Viewer for error logs
4. Reinstall using the package manager or installer

#### Clipboard history not working

**Solution**:
1. Ensure WinClip is running (check system tray)
2. Verify clipboard permissions in Windows Settings
3. Check WinClip settings for clipboard monitoring
4. Restart WinClip

### Getting Help

If you continue to experience issues:

1. **Check Documentation**: Visit the [WinClip website](https://duncansmith.tech/apps/winclip)
2. **Report a Bug**: Email support with:
   - Installation method used
   - Windows version
   - Error messages or logs
   - Steps to reproduce the problem

---

## Additional Resources

- **Homepage**: https://duncansmith.tech/apps/winclip
- **Source code**: https://github.com/maverick0628/winclip

---

## License

WinClip is released under the MIT licence. See the [LICENSE](LICENSE) file for details.

---

WinClip is free and open source. If you find a bug or want a feature, open an issue or a pull request; see [CONTRIBUTING.md](CONTRIBUTING.md).
