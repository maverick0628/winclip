# WinClip Package Testing Guide

This document provides comprehensive testing procedures for WinClip package distribution across Winget, Chocolatey, and Scoop. Follow these steps to ensure packages work correctly before public release.

## Table of Contents

- [Overview](#overview)
- [Testing Environment Setup](#testing-environment-setup)
- [Pre-Submission Testing](#pre-submission-testing)
- [Winget Testing](#winget-testing)
- [Chocolatey Testing](#chocolatey-testing)
- [Scoop Testing](#scoop-testing)
- [Post-Publication Testing](#post-publication-testing)
- [Update Testing](#update-testing)
- [Troubleshooting Tests](#troubleshooting-tests)

---

## Overview

### Testing Objectives

- ✅ Validate manifest syntax and schema compliance
- ✅ Verify installation works on clean systems
- ✅ Confirm silent installation (non-interactive)
- ✅ Test uninstallation removes all components
- ✅ Validate update scenarios work correctly
- ✅ Ensure checksums match installer binaries

### Test Environments

**Minimum Test Matrix**:
- Windows 10 (version 21H2 or later)
- Windows 11 (latest version)
- Both x64 and x86 architectures (if supporting both)
- Clean VM or fresh Windows installation (recommended)

**Tools Required**:
- PowerShell 5.1 or later
- Administrator access
- Test virtual machines or containers
- Checksum calculation utilities

---

## Testing Environment Setup

### Prepare Test Virtual Machine

Use a clean Windows installation to avoid conflicts:

```powershell
# Recommended VM setup
# - Windows 10/11 fresh install
# - No existing clipboard managers
# - Windows Defender enabled
# - Network access for downloads
```

### Install Testing Tools

```powershell
# Install Winget (if not already present on Windows 11)
# Download from: https://github.com/microsoft/winget-cli/releases

# Install Chocolatey
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# Install Scoop
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
irm get.scoop.sh | iex
```

### Prepare Test Checklist

Create a testing spreadsheet to track results:

| Test Case | Winget | Chocolatey | Scoop | Notes |
|-----------|--------|------------|-------|-------|
| Manifest validation | ⬜ | ⬜ | ⬜ | |
| Silent install | ⬜ | ⬜ | ⬜ | |
| Interactive install | ⬜ | ⬜ | ⬜ | |
| Application launches | ⬜ | ⬜ | ⬜ | |
| Shortcuts created | ⬜ | ⬜ | ⬜ | |
| Uninstall works | ⬜ | ⬜ | ⬜ | |
| Clean uninstall | ⬜ | ⬜ | ⬜ | |
| Update from previous | ⬜ | ⬜ | ⬜ | |

---

## Pre-Submission Testing

### 1. Validate Checksums

Before testing installation, verify installer checksums are correct:

```powershell
# Calculate SHA256 checksum
$installer = "WinClip-1.0.0-x64.exe"
$hash = Get-FileHash -Path $installer -Algorithm SHA256

Write-Host "Installer: $installer"
Write-Host "SHA256: $($hash.Hash)"

# Compare with manifest files
# Winget: Check WinClip.WinClip.installer.yaml
# Chocolatey: Check tools/chocolateyinstall.ps1
# Scoop: Check winclip.json
```

**Verification Checklist**:
- [ ] x64 installer checksum matches all manifests
- [ ] x86 installer checksum matches all manifests (if applicable)
- [ ] Installer URLs are accessible
- [ ] Installer downloads complete without errors

### 2. Verify Installer Binary

Test the installer directly before package manager testing:

```powershell
# Test silent installation
.\WinClip-1.0.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-

# Wait for installation to complete
Start-Sleep -Seconds 30

# Verify application installed
Test-Path "C:\Program Files\WinClip\WinClip.exe"

# Test application launches
Start-Process "C:\Program Files\WinClip\WinClip.exe"

# Test uninstallation
$uninstaller = Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* |
    Where-Object { $_.DisplayName -eq "WinClip" } |
    Select-Object -ExpandProperty UninstallString

if ($uninstaller) {
    Start-Process $uninstaller -ArgumentList "/VERYSILENT" -Wait
}
```

**Verification Checklist**:
- [ ] Silent install completes without prompts
- [ ] Application installs to correct location
- [ ] Application launches successfully
- [ ] Silent uninstall removes application
- [ ] No leftover files after uninstall

---

## Winget Testing

### 1. Manifest Validation

Validate manifest files comply with Winget schema:

```bash
# Navigate to winget manifests directory
cd distribution/winget

# Validate using winget CLI (if available)
winget validate --manifest .

# Alternative: Manual validation
# Check schema version matches: ManifestVersion: 1.4.0
# Verify all required fields are present
# Ensure YAML syntax is correct
```

**Expected Output**:
```
Manifest validation succeeded.
```

**Validation Checklist**:
- [ ] No YAML syntax errors
- [ ] All required fields present (PackageIdentifier, PackageVersion, etc.)
- [ ] InstallerUrl is accessible
- [ ] InstallerSha256 matches actual installer
- [ ] Schema version is current (1.4.0)

### 2. Local Installation Testing

Test installation from local manifest files:

```powershell
# Install from local manifest
winget install --manifest distribution/winget --accept-package-agreements --accept-source-agreements

# Verify installation
Get-AppxPackage | Where-Object { $_.Name -like "*WinClip*" }
# OR for Win32 apps:
Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* |
    Where-Object { $_.DisplayName -eq "WinClip" }

# Test application
Start-Process "WinClip"

# Verify application works
# - Check system tray icon appears
# - Test clipboard capture
# - Verify hotkeys work
```

**Verification Checklist**:
- [ ] Installation downloads correct installer
- [ ] Installation completes successfully
- [ ] No error messages during install
- [ ] Application appears in installed apps list
- [ ] Start menu shortcut created
- [ ] Application launches and runs correctly

### 3. Silent Installation Testing

Verify silent installation works (important for enterprise deployment):

```powershell
# Silent install test
winget install --id WinClip.WinClip --silent --accept-package-agreements --accept-source-agreements

# Verify no UI prompts appeared
# Check installation completed
Get-Process | Where-Object { $_.Name -eq "WinClip" }
```

**Verification Checklist**:
- [ ] No UI prompts during installation
- [ ] Installation completes in background
- [ ] Application is functional after install
- [ ] Exit code is 0 (success)

### 4. Uninstallation Testing

Test complete removal of the application:

```powershell
# Uninstall via winget
winget uninstall WinClip.WinClip

# Verify removal
Test-Path "C:\Program Files\WinClip" # Should return False

# Check registry
Get-ItemProperty HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\* |
    Where-Object { $_.DisplayName -eq "WinClip" } # Should return nothing

# Check Start Menu
Test-Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\WinClip.lnk" # Should return False
```

**Verification Checklist**:
- [ ] Application removed from installed apps
- [ ] Program files directory removed
- [ ] Start menu shortcuts removed
- [ ] Registry entries cleaned up
- [ ] User data preserved (if applicable)

---

## Chocolatey Testing

### 1. Package Validation

Validate Chocolatey package structure and content:

```powershell
# Navigate to Chocolatey package directory
cd distribution/chocolatey

# Pack the package
choco pack winclip.nuspec

# Inspect package contents
7z l winclip.1.0.0.nupkg

# Verify package structure
# Should contain:
# - winclip.nuspec
# - tools/chocolateyinstall.ps1
# - tools/chocolateyuninstall.ps1
# - tools/VERIFICATION.txt
```

**Validation Checklist**:
- [ ] Package builds without errors
- [ ] .nuspec file is valid XML
- [ ] Package ID is lowercase: `winclip`
- [ ] Version follows semantic versioning
- [ ] All required PowerShell scripts present
- [ ] VERIFICATION.txt documents checksums
- [ ] No binaries included in package

### 2. Local Installation Testing

Test installation from local package:

```powershell
# Install from local package
choco install winclip --source . --force --yes

# Verify installation
choco list --local-only | Select-String "winclip"

# Check application installed
Test-Path "C:\Program Files\WinClip\WinClip.exe"

# Test application
Start-Process "WinClip"
```

**Verification Checklist**:
- [ ] Package installs successfully
- [ ] Installer downloads from correct URL
- [ ] Checksum validation passes
- [ ] Installation completes without errors
- [ ] Application appears in Chocolatey list
- [ ] Application is functional

### 3. Silent Installation Testing

Chocolatey installations should always be silent:

```powershell
# Install with verbose output to verify silent install
choco install winclip --source . --force --yes --verbose

# Monitor installation
# Should see: "Installing winclip..."
# Should NOT see: Any UI windows or prompts

# Verify process completed silently
Get-WinEvent -LogName Application -MaxEvents 50 |
    Where-Object { $_.Message -like "*WinClip*" }
```

**Verification Checklist**:
- [ ] No installer UI appears
- [ ] No user prompts during install
- [ ] Installation completes automatically
- [ ] Success message shows in PowerShell
- [ ] Application ready to use immediately

### 4. Uninstallation Testing

Test uninstall script removes application completely:

```powershell
# Uninstall the package
choco uninstall winclip --yes

# Verify removal
choco list --local-only | Select-String "winclip" # Should return nothing

# Check application removed
Test-Path "C:\Program Files\WinClip" # Should return False

# Verify uninstaller ran
# Check Windows event logs for uninstall events
```

**Verification Checklist**:
- [ ] Uninstall completes successfully
- [ ] Application removed from system
- [ ] Package removed from Chocolatey list
- [ ] Registry entries cleaned
- [ ] No error messages during uninstall

### 5. Script Testing

Test PowerShell scripts directly:

```powershell
# Test install script
cd distribution/chocolatey/tools

# Dry-run install script (review parameters)
Get-Content chocolateyinstall.ps1

# Verify parameters:
# - Correct download URLs
# - Valid checksums
# - Proper silent arguments: /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-

# Test uninstall script
Get-Content chocolateyuninstall.ps1

# Verify:
# - Uses Get-UninstallRegistryKey
# - Has error handling
# - Proper silent uninstall arguments
```

**Verification Checklist**:
- [ ] Install script has correct URL
- [ ] Checksums match actual installers
- [ ] Silent install arguments correct
- [ ] Uninstall script finds app correctly
- [ ] Error handling in place

---

## Scoop Testing

### 1. Manifest Validation

Validate JSON manifest syntax and structure:

```bash
# Navigate to Scoop manifest
cd distribution/scoop

# Validate JSON syntax
python -m json.tool winclip.json

# Alternative: Use jq
jq empty winclip.json

# Alternative: Use PowerShell
Get-Content winclip.json | ConvertFrom-Json | ConvertTo-Json
```

**Expected Output**:
```
(Formatted JSON output - no errors)
```

**Validation Checklist**:
- [ ] Valid JSON syntax
- [ ] All required fields present (version, description, license, etc.)
- [ ] URLs are accessible
- [ ] Hashes match installers
- [ ] Architecture blocks correct (64bit, 32bit)

### 2. Local Installation Testing

Test installation from local manifest:

```powershell
# Install from local manifest file
scoop install distribution/scoop/winclip.json

# Verify installation
scoop list | Select-String "winclip"

# Check application installed
scoop which winclip

# Test application
scoop which winclip | ForEach-Object { & $_ }
```

**Verification Checklist**:
- [ ] Scoop downloads installer
- [ ] Hash verification passes
- [ ] Installer runs silently
- [ ] Application appears in scoop list
- [ ] Shortcuts created (if configured)
- [ ] Application launches successfully

### 3. Architecture Testing

Test both x64 and x86 architectures if supported:

```powershell
# Test x64 installation (on 64-bit Windows)
scoop install distribution/scoop/winclip.json

# Verify correct architecture installed
Get-Process | Where-Object { $_.Name -eq "WinClip" } |
    Select-Object Name, Path

# On 32-bit Windows, verify x86 version installs
# (Test in 32-bit VM if supporting both architectures)
```

**Verification Checklist**:
- [ ] Correct architecture installer downloaded
- [ ] x64 installer on 64-bit Windows
- [ ] x86 installer on 32-bit Windows (if supported)
- [ ] Application runs natively (not emulated)

### 4. Uninstallation Testing

Test Scoop uninstall process:

```powershell
# Uninstall application
scoop uninstall winclip

# Verify removal
scoop list | Select-String "winclip" # Should return nothing

# Check application directory
Test-Path "$env:USERPROFILE\scoop\apps\winclip" # Should return False

# Check shortcuts
Test-Path "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Scoop Apps\WinClip.lnk"
# Should return False
```

**Verification Checklist**:
- [ ] Uninstall completes successfully
- [ ] Application removed from scoop list
- [ ] Application directory removed
- [ ] Shortcuts removed
- [ ] No errors during uninstall

### 5. Autoupdate Testing

If manifest includes autoupdate configuration, test it:

```powershell
# Check current version
scoop list winclip

# Simulate new version check
scoop update

# Check if new version detected (when available)
scoop status
```

**Verification Checklist**:
- [ ] Autoupdate configuration present
- [ ] Version detection works (checkver)
- [ ] URL pattern generates correct download links
- [ ] Hash extraction/calculation configured

---

## Post-Publication Testing

After packages are published to official repositories, test public installation:

### Winget Public Installation

```powershell
# Search for package
winget search WinClip

# Install from public repository
winget install WinClip.WinClip

# Verify installation
winget list --id WinClip.WinClip

# Test upgrade (when new version available)
winget upgrade WinClip.WinClip
```

### Chocolatey Public Installation

```powershell
# Search for package
choco search winclip

# Install from Chocolatey Community Repository
choco install winclip

# Verify installation
choco list --local-only winclip

# Test upgrade
choco upgrade winclip
```

### Scoop Public Installation

> Not applicable yet. WinClip is not in a public Scoop bucket, so these commands fail until it is. Test the local manifest with the Scoop Testing section above instead.

```powershell
# Search for package
scoop search winclip

# Install from bucket
scoop install winclip

# Verify installation
scoop list winclip

# Test update
scoop update winclip
```

**Post-Publication Checklist**:
- [ ] Package appears in search results
- [ ] Public installation works
- [ ] Correct version displayed
- [ ] Package metadata correct (description, license, etc.)
- [ ] Download statistics tracking (if available)

---

## Update Testing

Test update scenarios when releasing new versions:

### Test Update Scenarios

```powershell
# Scenario 1: Clean install of old version, upgrade to new
# Install v1.0.0
winget install WinClip.WinClip --version 1.0.0

# Upgrade to v1.1.0
winget upgrade WinClip.WinClip

# Verify upgrade successful
winget list WinClip.WinClip # Should show v1.1.0

# Test application still works after upgrade
Start-Process "WinClip"
```

### Version Migration Testing

```powershell
# Install old version
choco install winclip --version 1.0.0 --force

# Check application settings/data location
Test-Path "$env:APPDATA\WinClip"

# Create test data
# (e.g., clipboard history, preferences)

# Upgrade to new version
choco upgrade winclip

# Verify settings preserved
Test-Path "$env:APPDATA\WinClip" # Should still exist
# Check that user data is intact
```

**Update Testing Checklist**:
- [ ] Upgrade from previous version works
- [ ] User settings preserved
- [ ] Application data not lost
- [ ] No duplicate installations
- [ ] Old version fully replaced
- [ ] Application functions correctly after upgrade

---

## Troubleshooting Tests

### Common Test Failures

#### Installation Fails

```powershell
# Debug installation failure
# Check installer URL is accessible
Invoke-WebRequest -Uri "https://github.com/.../WinClip-1.0.0-x64.exe" -Method Head

# Verify checksum
$installer = "WinClip-1.0.0-x64.exe"
Get-FileHash -Path $installer -Algorithm SHA256

# Check installation logs
# Winget: %LOCALAPPDATA%\Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState\DiagOutputDir\
# Chocolatey: C:\ProgramData\chocolatey\logs\chocolatey.log
# Scoop: $env:USERPROFILE\scoop\apps\scoop\current\logs\
```

#### Checksum Mismatch

```powershell
# Recalculate and update checksums
$x64_installer = "WinClip-1.0.0-x64.exe"
$x86_installer = "WinClip-1.0.0-x86.exe"

$x64_hash = (Get-FileHash -Path $x64_installer -Algorithm SHA256).Hash
$x86_hash = (Get-FileHash -Path $x86_installer -Algorithm SHA256).Hash

Write-Host "x64 SHA256: $x64_hash"
Write-Host "x86 SHA256: $x86_hash"

# Update manifests with correct hashes
# Winget: Update WinClip.WinClip.installer.yaml
# Chocolatey: Update tools/chocolateyinstall.ps1
# Scoop: Update winclip.json
```

#### Silent Install Not Working

```powershell
# Test installer silent arguments manually
.\WinClip-1.0.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /LOG="install.log"

# Review install.log for errors
Get-Content install.log

# Verify Inno Setup silent arguments are correct
# /VERYSILENT - No UI
# /SUPPRESSMSGBOXES - No message boxes
# /NORESTART - Don't restart computer
# /SP- - Skip startup prompt
```

#### Uninstall Leaves Files

```powershell
# After uninstall, check for leftover files
Get-ChildItem "C:\Program Files\WinClip" -Recurse -ErrorAction SilentlyContinue

# Check registry for leftover entries
Get-ItemProperty HKLM:\Software\WinClip -ErrorAction SilentlyContinue
Get-ItemProperty HKCU:\Software\WinClip -ErrorAction SilentlyContinue

# Check Start Menu
Get-ChildItem "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\" -Recurse |
    Where-Object { $_.Name -like "*WinClip*" }
```

### Test Automation

Create automated test script for continuous validation:

```powershell
# test-packages.ps1 - Automated testing script
param(
    [string]$Version = "1.0.0",
    [string]$TestMode = "all" # winget, chocolatey, scoop, or all
)

function Test-WingetPackage {
    Write-Host "Testing Winget package..." -ForegroundColor Cyan

    # Validate manifests
    $validation = winget validate --manifest distribution/winget 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Manifest validation passed" -ForegroundColor Green
    } else {
        Write-Host "✗ Manifest validation failed: $validation" -ForegroundColor Red
        return $false
    }

    # Test installation
    winget install --manifest distribution/winget --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Installation successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Installation failed" -ForegroundColor Red
        return $false
    }

    # Test uninstallation
    winget uninstall WinClip.WinClip
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Uninstallation successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Uninstallation failed" -ForegroundColor Red
        return $false
    }

    return $true
}

function Test-ChocolateyPackage {
    Write-Host "Testing Chocolatey package..." -ForegroundColor Cyan

    # Build package
    choco pack distribution/chocolatey/winclip.nuspec --out distribution/chocolatey/
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Package build failed" -ForegroundColor Red
        return $false
    }
    Write-Host "✓ Package built successfully" -ForegroundColor Green

    # Test installation
    choco install winclip --source distribution/chocolatey --force --yes
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Installation successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Installation failed" -ForegroundColor Red
        return $false
    }

    # Test uninstallation
    choco uninstall winclip --yes
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Uninstallation successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Uninstallation failed" -ForegroundColor Red
        return $false
    }

    return $true
}

function Test-ScoopPackage {
    Write-Host "Testing Scoop package..." -ForegroundColor Cyan

    # Validate JSON
    try {
        Get-Content distribution/scoop/winclip.json | ConvertFrom-Json | Out-Null
        Write-Host "✓ JSON validation passed" -ForegroundColor Green
    } catch {
        Write-Host "✗ JSON validation failed: $_" -ForegroundColor Red
        return $false
    }

    # Test installation
    scoop install distribution/scoop/winclip.json
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Installation successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Installation failed" -ForegroundColor Red
        return $false
    }

    # Test uninstallation
    scoop uninstall winclip
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Uninstallation successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Uninstallation failed" -ForegroundColor Red
        return $false
    }

    return $true
}

# Main test execution
$results = @{}

if ($TestMode -eq "all" -or $TestMode -eq "winget") {
    $results["Winget"] = Test-WingetPackage
}

if ($TestMode -eq "all" -or $TestMode -eq "chocolatey") {
    $results["Chocolatey"] = Test-ChocolateyPackage
}

if ($TestMode -eq "all" -or $TestMode -eq "scoop") {
    $results["Scoop"] = Test-ScoopPackage
}

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
foreach ($manager in $results.Keys) {
    $status = if ($results[$manager]) { "✓ PASS" } else { "✗ FAIL" }
    $color = if ($results[$manager]) { "Green" } else { "Red" }
    Write-Host "$manager : $status" -ForegroundColor $color
}

# Exit code
$allPassed = $results.Values | Where-Object { $_ -eq $false } | Measure-Object | Select-Object -ExpandProperty Count
exit $allPassed
```

---

## Final Testing Checklist

Before releasing to production, complete this comprehensive checklist:

### Pre-Release Validation

- [ ] All manifest files validated
- [ ] Checksums verified for all installers
- [ ] Download URLs accessible and permanent
- [ ] Version numbers consistent across all manifests
- [ ] Silent install arguments tested and working

### Installation Testing

- [ ] Winget install works on Windows 10
- [ ] Winget install works on Windows 11
- [ ] Chocolatey install works (admin)
- [ ] Scoop install works (user-level)
- [ ] Silent installation works for all managers
- [ ] Application launches after install

### Functionality Testing

- [ ] Application runs correctly after install
- [ ] All features work as expected
- [ ] Shortcuts created in correct locations
- [ ] System tray icon appears (if applicable)
- [ ] Settings persist correctly

### Uninstallation Testing

- [ ] Winget uninstall removes app completely
- [ ] Chocolatey uninstall removes app completely
- [ ] Scoop uninstall removes app completely
- [ ] No leftover files in Program Files
- [ ] No leftover registry entries
- [ ] Shortcuts removed

### Update Testing

- [ ] Upgrade from previous version works
- [ ] Settings preserved after upgrade
- [ ] User data not lost
- [ ] Application functional after upgrade

### Documentation

- [ ] Installation instructions accurate
- [ ] Submission procedures documented
- [ ] Troubleshooting guide complete
- [ ] Testing checklist complete

---

## Additional Resources

### Testing Tools

- [Winget CLI](https://github.com/microsoft/winget-cli) - Official Winget command-line tool
- [Chocolatey CLI](https://chocolatey.org/install) - Chocolatey package manager
- [Scoop CLI](https://scoop.sh/) - Scoop installer
- [Windows Sandbox](https://docs.microsoft.com/en-us/windows/security/threat-protection/windows-sandbox/windows-sandbox-overview) - Isolated testing environment

### Validation Resources

- [Winget Manifest Validator](https://github.com/microsoft/winget-pkgs/tree/master/Tools)
- [Chocolatey Package Validator](https://github.com/chocolatey/package-validator)
- [JSON Lint](https://jsonlint.com/) - Online JSON validator

### Testing Best Practices

- Always test in clean VMs to avoid environmental conflicts
- Test both x64 and x86 if supporting both architectures
- Verify silent installation for enterprise deployment scenarios
- Test upgrade paths from previous versions
- Document any test failures and resolutions
- Automate testing where possible for consistency

---

## Revision History

- **v1.0** - Initial testing documentation (2024)
  - Added comprehensive testing procedures for all three package managers
  - Included validation, installation, and uninstall testing
  - Documented troubleshooting and automation approaches
