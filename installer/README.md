# WinClip Installer

This directory contains all resources and scripts needed to build WinClip installers for distribution.

## Directory Structure

```
installer/
├── Assets/              # Application icons, logos, and Store assets
├── Scripts/             # PowerShell build and signing scripts
├── Certificates/        # Code signing certificates (not in source control)
└── Output/             # Generated installer packages (not in source control)
```

## Distribution Methods

### 1. MSIX Package (Microsoft Store)
- Modern Windows packaging format
- Automatic updates via Microsoft Store
- Sandboxed application environment
- Built using Package.appxmanifest and MSBuild

### 2. Inno Setup Installer (Direct Download)
- Traditional EXE installer for users who prefer direct downloads
- Supports custom installation options
- Auto-start with Windows functionality
- Clean uninstallation

## Building Installers

### Prerequisites
- .NET 8.0 SDK or later
- Windows 10 SDK (for MSIX packaging and signtool)
- Inno Setup 6.0 or later (for EXE installer)
- PowerShell 7.0 or later

### Build Scripts

All build scripts are located in the `Scripts/` directory:

- `build-msix.ps1` - Build MSIX package for Microsoft Store
- `build-installer.ps1` - Build Inno Setup EXE installer
- `build-all.ps1` - Build all installer types (master script)
- `sign-package.ps1` - Sign packages with code signing certificate
- `create-test-cert.ps1` - Create self-signed certificate for testing

### Quick Start

```powershell
# Build MSIX package for x64
pwsh ./Scripts/build-msix.ps1 -Architecture x64

# Build Inno Setup installer for x64
pwsh ./Scripts/build-installer.ps1 -Architecture x64

# Build everything
pwsh ./Scripts/build-all.ps1
```

## Supported Architectures

- **x64** - Intel/AMD 64-bit processors
- **ARM64** - ARM-based Windows devices (Surface Pro X, etc.)

## Code Signing

Production installers must be signed to avoid Windows security warnings. See `Certificates/README.md` for details on:
- Obtaining a commercial code signing certificate
- Creating test certificates for development
- Signing workflow

## Testing

See `TESTING.md` for comprehensive testing procedures for both installer types.

## Output Files

After building, installer packages will be in the `Output/` directory:

- `WinClip_x64.msix` - MSIX package for x64
- `WinClip_ARM64.msix` - MSIX package for ARM64
- `WinClip-Setup-x64.exe` - Inno Setup installer for x64
- `WinClip-Setup-ARM64.exe` - Inno Setup installer for ARM64

## Version Management

Version information is managed in `WinClip.csproj`. Update the `<Version>` property to set the version for all installers.
