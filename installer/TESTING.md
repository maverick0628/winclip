# WinClip Installer Testing Guide

This document provides comprehensive testing procedures for verifying WinClip installer packages work correctly across different scenarios.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Test Environment Setup](#test-environment-setup)
- [Build Verification](#build-verification)
- [MSIX Package Testing](#msix-package-testing)
- [Inno Setup Installer Testing](#inno-setup-installer-testing)
- [Code Signing Verification](#code-signing-verification)
- [Multi-Architecture Testing](#multi-architecture-testing)
- [Regression Testing](#regression-testing)
- [Test Checklists](#test-checklists)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software

- **Windows 10 version 1809 (build 17763) or later** (or Windows 11)
- **.NET 8.0 SDK or later**
- **PowerShell 7.0 or later**
- **Windows SDK** (for signtool verification)
- **Inno Setup 6.0 or later** (for building EXE installers)
- **Administrator privileges** (for some tests)

### Test Machine Requirements

**Recommended Test Configurations:**

1. **Clean Windows 10 VM** - For fresh install testing
2. **Clean Windows 11 VM** - For latest OS testing
3. **ARM64 Device or VM** - For ARM architecture testing (optional but recommended)

### Setup Test Environment

```powershell
# Verify PowerShell version
$PSVersionTable.PSVersion

# Verify .NET SDK
dotnet --version

# Verify Windows SDK (signtool)
signtool /?

# Verify Inno Setup
iscc /?
```

## Test Environment Setup

### 1. Create Test Certificate

For testing signed installers, create a self-signed test certificate:

```powershell
cd installer/Scripts
.\create-test-cert.ps1 -InstallToStore
```

This creates and installs a test certificate to your local machine's Trusted Root Certification Authorities.

**⚠️ Warning:** Test certificates should only be used on development/test machines, never in production.

### 2. Prepare Clean Test Directory

```powershell
# Create a test directory
New-Item -ItemType Directory -Path "C:\WinClip-Tests" -Force
cd "C:\WinClip-Tests"
```

### 3. Verify Prerequisites

```powershell
# Check if WinClip is already installed
Get-AppxPackage -Name "*WinClip*"
Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*" | Where-Object { $_.DisplayName -like "*WinClip*" }
```

If WinClip is already installed, uninstall it before testing.

## Build Verification

### Test 1: Build MSIX Package (x64)

```powershell
cd installer/Scripts
.\build-msix.ps1 -Architecture x64
```

**Expected Results:**
- ✅ Build completes without errors
- ✅ Output directory created: `installer/Output/`
- ✅ MSIX files present:
  - `WinClip_x64.msix`
  - `WinClip_x64.msixbundle` (if bundle generation is enabled)
- ✅ Files are at least 1 MB in size

**Verification:**
```powershell
Test-Path "installer\Output\WinClip_x64.msix"
(Get-Item "installer\Output\WinClip_x64.msix").Length -gt 1MB
```

### Test 2: Build MSIX Package (ARM64)

```powershell
.\build-msix.ps1 -Architecture ARM64
```

**Expected Results:**
- ✅ Build completes without errors
- ✅ ARM64 MSIX files present: `WinClip_ARM64.msix`

### Test 3: Build Inno Setup Installer (x64)

```powershell
.\build-installer.ps1 -Architecture x64
```

**Expected Results:**
- ✅ Build completes without errors
- ✅ Self-contained publish succeeds
- ✅ Inno Setup compilation succeeds
- ✅ Output file present: `installer\Output\WinClip-Setup-x64-1.0.0.exe`
- ✅ File is at least 5 MB in size (self-contained .NET runtime)

**Verification:**
```powershell
Test-Path "installer\Output\WinClip-Setup-x64-1.0.0.exe"
(Get-Item "installer\Output\WinClip-Setup-x64-1.0.0.exe").Length -gt 5MB
```

### Test 4: Build Inno Setup Installer (ARM64)

```powershell
.\build-installer.ps1 -Architecture ARM64
```

**Expected Results:**
- ✅ Build completes without errors
- ✅ Output file present: `installer\Output\WinClip-Setup-ARM64-1.0.0.exe`

### Test 5: Build with Signing

```powershell
# Build and sign MSIX
.\build-msix.ps1 -Architecture x64 -Sign -CertificatePath ..\Certificates\WinClip-Test.pfx -CertificatePassword "test123"

# Build and sign Inno Setup installer
.\build-installer.ps1 -Architecture x64 -Sign -CertificatePath ..\Certificates\WinClip-Test.pfx -CertificatePassword "test123"
```

**Expected Results:**
- ✅ Build completes without errors
- ✅ Signing step executes successfully
- ✅ Signature verification passes

## MSIX Package Testing

### Test 6: MSIX Installation (Basic)

**Prerequisites:**
- Clean Windows 10/11 VM or fresh user account
- No previous WinClip installation

**Steps:**
1. Double-click `installer\Output\WinClip_x64.msix`
2. Click "Install" in the App Installer dialog
3. Wait for installation to complete

**Expected Results:**
- ✅ Installation completes without errors
- ✅ No security warnings (if signed with trusted certificate)
- ✅ WinClip appears in Start Menu
- ✅ WinClip shortcut works from Start Menu

**Verification:**
```powershell
# Check if installed
Get-AppxPackage -Name "*WinClip*"

# Should show:
# Name: WinClip
# Version: 1.0.0.0
# InstallLocation: C:\Program Files\WindowsApps\...
```

### Test 7: MSIX - Application Launch

**Steps:**
1. Launch WinClip from Start Menu
2. Verify application window appears
3. Test basic functionality (clipboard operations)

**Expected Results:**
- ✅ Application launches without errors
- ✅ No crashes or exceptions
- ✅ UI renders correctly
- ✅ Basic features work (clipboard monitoring, history, etc.)

### Test 8: MSIX - File System Access

**Steps:**
1. Launch WinClip
2. Verify application can access required resources
3. Check logs/settings location

**Expected Results:**
- ✅ Application data stored in correct location: `%LocalAppData%\Packages\WinClip_...\LocalCache\`
- ✅ No permission errors
- ✅ Settings persist across sessions

### Test 9: MSIX - Update Scenario

**Prerequisites:**
- WinClip 1.0.0 installed via MSIX

**Steps:**
1. Build a new MSIX with incremented version (1.0.1)
2. Double-click the new MSIX file
3. Click "Update" in the App Installer dialog

**Expected Results:**
- ✅ Update completes successfully
- ✅ Settings and data preserved
- ✅ New version number reflected in Settings > Apps

### Test 10: MSIX - Uninstallation

**Steps:**
1. Open Settings > Apps > Installed apps
2. Find WinClip in the list
3. Click the three-dot menu > Uninstall
4. Confirm uninstallation

**Expected Results:**
- ✅ Uninstallation completes without errors
- ✅ WinClip removed from Start Menu
- ✅ WinClip removed from Installed apps list
- ✅ Application files removed from `C:\Program Files\WindowsApps\`
- ✅ User data removed from `%LocalAppData%\Packages\`

**Verification:**
```powershell
# Should return nothing
Get-AppxPackage -Name "*WinClip*"

# Check for leftover files
Test-Path "$env:LocalAppData\Packages\WinClip*"  # Should be False
```

## Inno Setup Installer Testing

### Test 11: Inno Setup Installation (Standard)

**Prerequisites:**
- Clean Windows 10/11 VM or fresh user account
- No previous WinClip installation

**Steps:**
1. Run `installer\Output\WinClip-Setup-x64-1.0.0.exe`
2. Follow installation wizard
3. Keep default installation path: `%LocalAppData%\Programs\WinClip`
4. **Do NOT check** "Start with Windows" option
5. **Do NOT check** "Launch WinClip" option
6. Complete installation

**Expected Results:**
- ✅ Installation wizard runs without errors
- ✅ No security warnings (if signed with trusted certificate)
- ✅ Installation completes successfully
- ✅ WinClip appears in Start Menu under "WinClip"
- ✅ Optional desktop shortcut created (if selected)
- ✅ Files installed to `%LocalAppData%\Programs\WinClip\`

**Verification:**
```powershell
# Check installation directory
Test-Path "$env:LocalAppData\Programs\WinClip\WinClip.exe"

# Check Start Menu shortcut
Test-Path "$env:AppData\Microsoft\Windows\Start Menu\Programs\WinClip\WinClip.lnk"

# Check uninstaller exists
Test-Path "$env:LocalAppData\Programs\WinClip\unins000.exe"
```

### Test 12: Inno Setup Installation with Auto-Start

**Steps:**
1. Run `installer\Output\WinClip-Setup-x64-1.0.0.exe`
2. Follow installation wizard
3. **Check** "Start with Windows" option
4. Complete installation

**Expected Results:**
- ✅ Installation completes successfully
- ✅ Auto-start registry entry created

**Verification:**
```powershell
# Check registry for auto-start
Get-ItemProperty "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "WinClip" -ErrorAction SilentlyContinue

# Should return:
# WinClip : C:\Users\...\AppData\Local\Programs\WinClip\WinClip.exe
```

### Test 13: Auto-Start Functionality

**Prerequisites:**
- WinClip installed with "Start with Windows" option

**Steps:**
1. Restart Windows (or log out and log back in)
2. Wait for login to complete
3. Check Task Manager > Startup apps

**Expected Results:**
- ✅ WinClip automatically starts on login
- ✅ WinClip process visible in Task Manager
- ✅ Application tray icon appears (if applicable)

**Verification:**
```powershell
# Check if WinClip is running
Get-Process -Name "WinClip" -ErrorAction SilentlyContinue
```

### Test 14: Application Launch (Inno Setup)

**Steps:**
1. Launch WinClip from Start Menu
2. Verify application window appears
3. Test basic functionality

**Expected Results:**
- ✅ Application launches without errors
- ✅ No crashes or exceptions
- ✅ UI renders correctly
- ✅ Basic features work

### Test 15: Inno Setup - Upgrade Scenario

**Prerequisites:**
- WinClip 1.0.0 installed via Inno Setup

**Steps:**
1. Build new installer with incremented version (1.0.1)
2. Run `WinClip-Setup-x64-1.0.1.exe`
3. Installer should detect existing installation
4. Choose to upgrade

**Expected Results:**
- ✅ Installer detects existing version
- ✅ Upgrade option presented
- ✅ Upgrade completes successfully
- ✅ Settings and data preserved
- ✅ Old version fully replaced
- ✅ Auto-start setting preserved (if was enabled)

### Test 16: Inno Setup - Uninstallation

**Steps:**
1. Open Settings > Apps > Installed apps (or Control Panel > Programs)
2. Find WinClip in the list
3. Click Uninstall
4. Follow uninstaller prompts
5. Confirm uninstallation

**Expected Results:**
- ✅ Uninstaller launches successfully
- ✅ Uninstallation completes without errors
- ✅ WinClip removed from Start Menu
- ✅ WinClip removed from Installed apps list
- ✅ Application directory removed: `%LocalAppData%\Programs\WinClip\`
- ✅ Auto-start registry entry removed (if was set)
- ✅ Desktop shortcut removed (if was created)

**Verification:**
```powershell
# Check installation directory (should not exist)
Test-Path "$env:LocalAppData\Programs\WinClip\"  # Should be False

# Check Start Menu shortcut (should not exist)
Test-Path "$env:AppData\Microsoft\Windows\Start Menu\Programs\WinClip\"  # Should be False

# Check registry for auto-start (should not exist)
Get-ItemProperty "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "WinClip" -ErrorAction SilentlyContinue  # Should be null

# Check desktop shortcut (should not exist if was created)
Test-Path "$env:UserProfile\Desktop\WinClip.lnk"  # Should be False
```

### Test 17: Inno Setup - Clean Uninstall Verification

**Steps:**
1. After uninstalling (Test 16), search for leftover files/registry entries

**Expected Results:**
- ✅ No leftover files in installation directory
- ✅ No leftover registry entries
- ✅ No leftover Start Menu items
- ✅ No leftover auto-start entries

**Deep Verification:**
```powershell
# Search for any WinClip files
Get-ChildItem -Path "$env:LocalAppData" -Recurse -Filter "*WinClip*" -ErrorAction SilentlyContinue

# Search for any WinClip registry entries
Get-ChildItem -Path "HKCU:\SOFTWARE" -Recurse | Where-Object { $_.Name -like "*WinClip*" } -ErrorAction SilentlyContinue
```

## Code Signing Verification

### Test 18: Verify MSIX Signature

**Steps:**
```powershell
# Verify signature using signtool
signtool verify /pa /v installer\Output\WinClip_x64.msix
```

**Expected Results:**
- ✅ Signature verification succeeds
- ✅ Certificate chain is valid
- ✅ Timestamp is present and valid
- ✅ No security warnings

**Output should show:**
```
Successfully verified: installer\Output\WinClip_x64.msix

Signing Certificate Chain:
    Issued to: WinClip Test Certificate (or your production certificate)
    Issued by: WinClip Test Certificate
    ...

Number of signatures successfully verified: 1
```

### Test 19: Verify Inno Setup Installer Signature

**Steps:**
```powershell
# Verify signature using signtool
signtool verify /pa /v installer\Output\WinClip-Setup-x64-1.0.0.exe
```

**Expected Results:**
- ✅ Signature verification succeeds
- ✅ Certificate chain is valid
- ✅ Timestamp is present and valid

### Test 20: Windows SmartScreen Test

**Prerequisites:**
- Unsigned installer OR installer signed with test certificate

**Steps:**
1. Run unsigned installer on a fresh Windows VM
2. Observe Windows SmartScreen behavior

**Expected Results (Unsigned/Test Certificate):**
- ⚠️ Windows SmartScreen warning appears
- ⚠️ "Windows protected your PC" message
- ⚠️ Need to click "More info" → "Run anyway"

**Expected Results (Production Certificate):**
- ✅ No SmartScreen warning (after initial reputation build-up)
- ✅ Installer runs directly

**Note:** Even with valid code signing, new publishers may see SmartScreen warnings until reputation is established. EV certificates bypass this.

## Multi-Architecture Testing

### Test 21: ARM64 Installation on ARM Device

**Prerequisites:**
- ARM64 Windows device (Surface Pro X, Windows ARM VM, etc.)

**Steps:**
1. Copy `WinClip-Setup-ARM64-1.0.0.exe` to ARM device
2. Run installer
3. Follow installation wizard
4. Test application launch and functionality

**Expected Results:**
- ✅ Installer runs natively on ARM64
- ✅ Installation completes successfully
- ✅ Application launches and runs natively (not emulated)
- ✅ Performance is smooth (native ARM execution)

**Verification:**
```powershell
# Check process architecture
Get-Process -Name "WinClip" | Select-Object Name, Path, @{Name="Architecture";Expression={(Get-Process -Id $_.Id).StartInfo.EnvironmentVariables}}
```

### Test 22: Architecture Mismatch Detection

**Steps:**
1. Try to install x64 installer on ARM64-only device (if available)
2. Try to install ARM64 installer on x64 device

**Expected Results:**
- ✅ Installer detects architecture mismatch
- ✅ Appropriate error message shown
- ⚠️ Installation blocked or warning displayed

**Note:** Modern ARM64 Windows can run x64 applications via emulation, so x64 installer may work but with reduced performance.

## Regression Testing

### Test 23: Reinstallation Test

**Steps:**
1. Install WinClip (any installer type)
2. Uninstall WinClip
3. Reinstall WinClip (same installer type)

**Expected Results:**
- ✅ First installation succeeds
- ✅ Uninstallation succeeds
- ✅ Reinstallation succeeds
- ✅ No leftover files cause issues
- ✅ Application works correctly after reinstall

### Test 24: Mixed Installer Types

**Steps:**
1. Install WinClip via MSIX
2. Uninstall MSIX
3. Install WinClip via Inno Setup installer
4. Uninstall Inno Setup installer
5. Install WinClip via MSIX again

**Expected Results:**
- ✅ All installations succeed
- ✅ All uninstallations succeed
- ✅ No conflicts between installer types
- ✅ Each installer cleanly removes its files

### Test 25: Installation Path Edge Cases

**Steps (Inno Setup only):**
1. Run installer
2. Choose custom installation path with spaces: `C:\Program Files\My Apps\WinClip`
3. Complete installation
4. Launch application
5. Uninstall

**Expected Results:**
- ✅ Installation to custom path succeeds
- ✅ Application launches from custom path
- ✅ Uninstaller removes custom path cleanly

### Test 26: Low Disk Space Scenario

**Steps:**
1. Create a test environment with limited disk space (< 100 MB free)
2. Attempt to install WinClip

**Expected Results:**
- ✅ Installer detects insufficient disk space
- ✅ Appropriate error message shown
- ✅ Installation cancelled gracefully
- ⚠️ No partial installation left behind

### Test 27: Installation During Active Use

**Steps:**
1. Install WinClip and launch it
2. Keep WinClip running
3. Attempt to run installer again (upgrade scenario)

**Expected Results:**
- ✅ Installer detects running instance
- ✅ Prompt to close application shown
- ✅ Installation proceeds after closing application
- ⚠️ OR installation deferred until app is closed

## Test Checklists

### Pre-Release Checklist

**Build Verification:**
- [ ] MSIX builds successfully for x64
- [ ] MSIX builds successfully for ARM64
- [ ] Inno Setup installer builds successfully for x64
- [ ] Inno Setup installer builds successfully for ARM64
- [ ] All packages are signed with production certificate
- [ ] Signature verification passes for all packages

**MSIX Testing:**
- [ ] MSIX installs cleanly on Windows 10 (1809+)
- [ ] MSIX installs cleanly on Windows 11
- [ ] Application launches after MSIX install
- [ ] MSIX uninstalls cleanly (no leftovers)
- [ ] MSIX update scenario works

**Inno Setup Testing:**
- [ ] Installer runs on Windows 10
- [ ] Installer runs on Windows 11
- [ ] Standard installation works
- [ ] Auto-start option works
- [ ] Desktop shortcut option works
- [ ] Application launches after install
- [ ] Upgrade scenario works
- [ ] Uninstaller removes all files
- [ ] Uninstaller removes registry entries

**Code Signing:**
- [ ] No SmartScreen warnings (production cert)
- [ ] Certificate info displays correctly in properties
- [ ] Timestamp verification works

**Multi-Architecture:**
- [ ] ARM64 installer tested on ARM device (if available)
- [ ] Both architectures produce working installers

### Quick Smoke Test Checklist

For rapid verification during development:

- [ ] Build succeeds without errors
- [ ] Output files present and correct size
- [ ] Installer runs without immediate errors
- [ ] Application launches after install
- [ ] Uninstaller completes successfully

### Full Test Suite Checklist

For comprehensive pre-release testing:

- [ ] All 27 tests completed
- [ ] No critical issues found
- [ ] All issues documented
- [ ] Performance acceptable
- [ ] No security warnings (production)

## Troubleshooting

### Build Issues

**Issue: "dotnet command not found"**
- **Solution:** Install .NET 8.0 SDK or later from https://dotnet.microsoft.com/

**Issue: "signtool not found"**
- **Solution:** Install Windows SDK (included with Visual Studio)
- **Location:** `C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\signtool.exe`

**Issue: "iscc command not found"**
- **Solution:** Install Inno Setup 6.0 or later from https://jrsoftware.org/isinfo.php
- Add to PATH or use `-InnoSetupPath` parameter

**Issue: MSIX build fails with "Package.appxmanifest not found"**
- **Solution:** Ensure you're running the script from the correct directory
- Verify `Package.appxmanifest` exists in project root

### Installation Issues

**Issue: "App Installer failed to install the package" (MSIX)**
- **Solution:** Check Windows version (requires 1809+)
- Verify MSIX file is not corrupted
- Check if app is already installed (uninstall first)

**Issue: "This app can't run on your PC" (MSIX)**
- **Solution:** Architecture mismatch - ensure you're installing the correct architecture (x64 vs ARM64)

**Issue: Inno Setup installer shows "Setup failed to install" error**
- **Solution:** Check Event Viewer > Windows Logs > Application for detailed error
- Ensure sufficient disk space
- Run as administrator if needed

**Issue: "Windows protected your PC" SmartScreen warning**
- **Solution (Development):** This is expected for test certificates
  - Click "More info" → "Run anyway"
- **Solution (Production):** Use EV code signing certificate for immediate trust

### Uninstallation Issues

**Issue: MSIX won't uninstall - "Uninstall failed"**
- **Solution:** Close application first
- Try PowerShell: `Get-AppxPackage *WinClip* | Remove-AppxPackage`

**Issue: Inno Setup uninstaller leaves files behind**
- **Solution:** Check if application is running (close it)
- Manually verify: `Test-Path "$env:LocalAppData\Programs\WinClip"`
- Report as bug if files remain after clean uninstall

**Issue: Auto-start registry entry not removed**
- **Solution:** Manually remove:
  ```powershell
  Remove-ItemProperty "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "WinClip"
  ```

### Runtime Issues

**Issue: Application fails to launch after install**
- **Solution:** Check Event Viewer for crash details
- Verify .NET dependencies are included (self-contained publish)
- Check Windows version compatibility

**Issue: "Cannot find runtime" error**
- **Solution:** Rebuild with self-contained publish option
- Verify PublishSingleFile and SelfContained properties in build script

## Additional Resources

- [MSIX Packaging Documentation](https://docs.microsoft.com/windows/msix/)
- [Inno Setup Documentation](https://jrsoftware.org/ishelp/)
- [Windows App Certification Kit](https://docs.microsoft.com/windows/uwp/debug-test-perf/windows-app-certification-kit)
- [Code Signing Best Practices](https://docs.microsoft.com/windows/win32/seccrypto/cryptography-tools)

## Reporting Issues

When reporting installer issues, include:

1. **Environment:**
   - Windows version (run `winver`)
   - Architecture (x64 or ARM64)
   - Installer type (MSIX or Inno Setup)
   - Installer version

2. **Steps to reproduce:**
   - Detailed steps that lead to the issue
   - Expected vs actual behavior

3. **Logs:**
   - Build output (if build issue)
   - Event Viewer logs (Windows Logs > Application)
   - PowerShell error messages
   - Screenshots of error dialogs

4. **Verification:**
   - Results of verification commands
   - File existence checks
   - Registry entries (if relevant)

---

**Last Updated:** February 2026
**Maintainer:** WinClip Development Team
**Related Documentation:** README.md, Certificates/README.md
