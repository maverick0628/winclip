# WinClip Installer Test Plan

## Overview
This document outlines the end-to-end testing procedure for the WinClip MSI installer. These tests must be performed on a Windows system with .NET 6+ and WiX Toolset v4 installed.

## Prerequisites
- Windows 10 or Windows 11
- .NET SDK 6.0 or later
- WiX Toolset v4
- PowerShell 5.1 or PowerShell Core 7+
- Administrator access for installation testing
- Clean test environment (VM recommended)

## Test Environment Setup
1. Create a clean Windows VM or use a test machine
2. Install .NET SDK: https://dotnet.microsoft.com/download
3. Install WiX Toolset v4: `dotnet tool install --global wix`
4. Clone the WinClip repository
5. Ensure WinClip.exe has been built (from previous tasks)

## Test Procedure

### Phase 1: Build Verification

#### Test 1.1: Build Installer
**Objective:** Verify the installer builds without errors

**Steps:**
1. Open PowerShell in the repository root
2. Run: `.\scripts\build-installer.ps1`
3. Verify build completes successfully
4. Verify MSI file is created in `installer\bin\Release\en-US\WinClip.msi`
5. Check MSI file size is reasonable (< 20 MB for single-file app)

**Expected Results:**
- Build script completes with exit code 0
- No build errors or warnings
- MSI file exists and has reasonable size
- Build output shows: "Build script completed successfully!"

**Validation Command:**
```powershell
Test-Path ".\installer\bin\Release\en-US\WinClip.msi"
```

---

### Phase 2: Installation Testing

#### Test 2.1: Silent Installation (All Features)
**Objective:** Verify installer installs with all features enabled

**Steps:**
1. Open PowerShell as Administrator
2. Run: `msiexec /i "installer\bin\Release\en-US\WinClip.msi" /qn /l*v install-all.log ADDLOCAL=ALL`
3. Wait for installation to complete
4. Check installation log for errors

**Expected Results:**
- Installation completes without errors
- Log shows successful completion
- Exit code is 0

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "Installation" -Features "All"
```

#### Test 2.2: Verify Installation Directory
**Objective:** Confirm files are installed to correct location

**Steps:**
1. Navigate to `C:\Program Files\WinClip`
2. Verify WinClip.exe exists
3. Verify all dependency DLLs are present
4. Check file permissions

**Expected Results:**
- Directory exists: `C:\Program Files\WinClip`
- WinClip.exe is present and executable
- All .NET dependencies are present
- Files have correct permissions

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "Files"
```

#### Test 2.3: Verify Start Menu Shortcut
**Objective:** Confirm Start Menu shortcut exists and works

**Steps:**
1. Open Start Menu
2. Search for "WinClip"
3. Verify shortcut appears in "WinClip" folder
4. Right-click shortcut and check properties
5. Launch WinClip from Start Menu
6. Verify application starts successfully

**Expected Results:**
- Shortcut exists in Start Menu > Programs > WinClip
- Shortcut points to correct executable path
- Shortcut has correct icon
- Application launches successfully from shortcut

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "StartMenu"
```

#### Test 2.4: Verify Desktop Shortcut
**Objective:** Confirm Desktop shortcut exists and works (if selected)

**Steps:**
1. Check desktop for WinClip shortcut
2. Right-click shortcut and check properties
3. Launch WinClip from desktop
4. Verify application starts successfully

**Expected Results:**
- Shortcut exists on desktop (if ALL features installed)
- Shortcut points to correct executable path
- Shortcut has correct icon
- Application launches successfully from shortcut

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "Desktop"
```

#### Test 2.5: Verify Auto-Start Registry Entry
**Objective:** Confirm startup registry entry is created (if selected)

**Steps:**
1. Open Registry Editor (regedit)
2. Navigate to `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
3. Verify "WinClip" entry exists
4. Verify entry points to correct path
5. Restart computer (optional)
6. Verify WinClip starts automatically (optional)

**Expected Results:**
- Registry entry exists at correct location (if ALL features installed)
- Entry value is correct path to WinClip.exe
- Application starts automatically after reboot (if tested)

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "Startup"
```

#### Test 2.6: Verify Registry Settings
**Objective:** Confirm installation registry entries are created

**Steps:**
1. Open Registry Editor (regedit)
2. Navigate to `HKEY_CURRENT_USER\Software\WinClip`
3. Verify "Installed" value exists and is set to 1
4. Verify other registry values are present

**Expected Results:**
- Registry key exists at `HKCU\Software\WinClip`
- "Installed" value exists and equals 1
- Other expected registry values are present

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "Registry"
```

---

### Phase 3: Uninstallation Testing

#### Test 3.1: Uninstall via Add/Remove Programs
**Objective:** Verify clean uninstallation through Windows UI

**Steps:**
1. Open Settings > Apps > Installed apps
2. Find "WinClip" in the list
3. Click "Uninstall"
4. Follow uninstall wizard
5. Wait for uninstallation to complete

**Expected Results:**
- WinClip appears in installed apps list
- Uninstall wizard completes successfully
- No error messages displayed

#### Test 3.2: Uninstall via MSI (Silent)
**Objective:** Verify silent uninstallation works

**Steps:**
1. Install WinClip again if needed
2. Run: `msiexec /x "installer\bin\Release\en-US\WinClip.msi" /qn /l*v uninstall.log`
3. Wait for uninstallation to complete
4. Check uninstallation log for errors

**Expected Results:**
- Uninstallation completes without errors
- Log shows successful completion
- Exit code is 0

#### Test 3.3: Verify Files Removed
**Objective:** Confirm all installed files are deleted

**Steps:**
1. Check `C:\Program Files\WinClip` directory
2. Verify directory is completely removed
3. Search for any remaining WinClip files

**Expected Results:**
- Installation directory is completely removed
- No WinClip files remain in Program Files
- No orphaned files in system directories

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "Cleanup"
```

#### Test 3.4: Verify Shortcuts Removed
**Objective:** Confirm all shortcuts are deleted

**Steps:**
1. Check Start Menu for WinClip folder/shortcuts
2. Check desktop for WinClip shortcut
3. Check taskbar and other locations

**Expected Results:**
- Start Menu shortcut is removed
- Start Menu folder "WinClip" is removed
- Desktop shortcut is removed (if it was created)
- No shortcuts remain anywhere

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "Cleanup"
```

#### Test 3.5: Verify Registry Cleanup
**Objective:** Confirm all registry entries are removed

**Steps:**
1. Open Registry Editor (regedit)
2. Check `HKEY_CURRENT_USER\Software\WinClip`
3. Check `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
4. Search for any remaining WinClip registry entries

**Expected Results:**
- `HKCU\Software\WinClip` key is removed
- "WinClip" entry removed from Run key (if it was created)
- No orphaned registry entries remain

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "Cleanup"
```

#### Test 3.6: Verify AppData Cleanup
**Objective:** Confirm user data directory is removed

**Steps:**
1. Navigate to `%APPDATA%\WinClip`
2. Verify directory is removed
3. Check `%LOCALAPPDATA%\WinClip` as well

**Expected Results:**
- `%APPDATA%\WinClip` directory is removed
- No user data files remain
- Clean slate for reinstallation

**Validation Command:**
```powershell
.\installer\validate-installation.ps1 -TestType "Cleanup"
```

---

### Phase 4: Advanced Testing

#### Test 4.1: Custom Installation (Selected Features)
**Objective:** Verify feature selection works correctly

**Steps:**
1. Run installer with UI: `msiexec /i "installer\bin\Release\en-US\WinClip.msi"`
2. In feature selection dialog, deselect "Desktop Shortcut"
3. Keep "Run at Windows Startup" deselected
4. Complete installation
5. Verify only selected features are installed

**Expected Results:**
- Installation completes successfully
- Start Menu shortcut is created
- Desktop shortcut is NOT created
- Startup registry entry is NOT created

#### Test 4.2: Upgrade Installation
**Objective:** Verify upgrade from older version works

**Steps:**
1. Install version 1.0.0.0
2. Build a new installer with version 1.0.1.0
3. Run new installer
4. Verify upgrade completes successfully
5. Verify settings are preserved
6. Verify only one entry in Add/Remove Programs

**Expected Results:**
- Upgrade completes without errors
- Old version is automatically removed
- Settings and data are preserved
- Only new version appears in installed apps

#### Test 4.3: Reinstallation
**Objective:** Verify reinstalling same version works

**Steps:**
1. Install WinClip
2. Run the same installer again
3. Choose "Repair" option
4. Verify repair completes successfully

**Expected Results:**
- Repair completes without errors
- Application functions correctly after repair
- All features are restored

#### Test 4.4: Installation to Custom Directory
**Objective:** Verify custom installation path works

**Steps:**
1. Run installer with UI
2. Change installation directory to custom path (e.g., `D:\WinClip`)
3. Complete installation
4. Verify files are installed to custom location
5. Verify shortcuts still work

**Expected Results:**
- Installation to custom directory succeeds
- Files are in correct custom location
- Shortcuts point to custom directory
- Application launches successfully

---

### Phase 5: System Integration Testing

#### Test 5.1: Windows Event Viewer
**Objective:** Verify no errors logged during install/uninstall

**Steps:**
1. Open Event Viewer
2. Navigate to Windows Logs > Application
3. Filter for "MsiInstaller" source
4. Check for errors or warnings during installation
5. Perform uninstall
6. Check for errors or warnings during uninstallation

**Expected Results:**
- No error events from MsiInstaller
- Only informational events logged
- Clean installation and uninstallation

#### Test 5.2: Multiple User Accounts
**Objective:** Verify per-user settings work correctly

**Steps:**
1. Install WinClip as User A (with admin rights)
2. Verify shortcuts work for User A
3. Switch to User B
4. Verify Start Menu shortcut is available
5. Verify Desktop shortcut is NOT present (per-user setting)
6. Launch application as User B

**Expected Results:**
- Application installs system-wide
- Start Menu shortcut available to all users
- Desktop shortcut and startup are per-user settings
- Application works for all users

#### Test 5.3: Windows Defender / Antivirus
**Objective:** Verify installer doesn't trigger security warnings

**Steps:**
1. Ensure Windows Defender is enabled
2. Run installer
3. Monitor for security warnings
4. Check quarantine after installation

**Expected Results:**
- No security warnings during installation
- Installer not flagged as malware
- Application not quarantined
- Clean reputation

---

## Test Results Template

### Test Session Information
- **Date:** [Date]
- **Tester:** [Name]
- **Windows Version:** [e.g., Windows 11 22H2]
- **Test Environment:** [VM/Physical/Cloud]
- **WinClip Version:** [e.g., 1.0.0.0]
- **Installer Build:** [e.g., WinClip-1.0.0-Setup.msi]

### Test Results Summary
| Test ID | Test Name | Status | Notes |
|---------|-----------|--------|-------|
| 1.1 | Build Installer | ⬜ | |
| 2.1 | Silent Installation | ⬜ | |
| 2.2 | Installation Directory | ⬜ | |
| 2.3 | Start Menu Shortcut | ⬜ | |
| 2.4 | Desktop Shortcut | ⬜ | |
| 2.5 | Auto-Start Registry | ⬜ | |
| 2.6 | Registry Settings | ⬜ | |
| 3.1 | Uninstall via UI | ⬜ | |
| 3.2 | Silent Uninstall | ⬜ | |
| 3.3 | Files Removed | ⬜ | |
| 3.4 | Shortcuts Removed | ⬜ | |
| 3.5 | Registry Cleanup | ⬜ | |
| 3.6 | AppData Cleanup | ⬜ | |
| 4.1 | Custom Features | ⬜ | |
| 4.2 | Upgrade Installation | ⬜ | |
| 4.3 | Reinstallation | ⬜ | |
| 4.4 | Custom Directory | ⬜ | |
| 5.1 | Event Viewer | ⬜ | |
| 5.2 | Multiple Users | ⬜ | |
| 5.3 | Antivirus | ⬜ | |

**Legend:** ✅ Pass | ❌ Fail | ⚠️ Warning | ⬜ Not Tested

### Issues Found
1. [Issue description]
   - Severity: [Critical/High/Medium/Low]
   - Steps to reproduce: [Steps]
   - Expected: [Expected behavior]
   - Actual: [Actual behavior]

## Sign-Off

### QA Approval
- [ ] All critical tests passed
- [ ] No blocking issues found
- [ ] Installer ready for release

**QA Engineer:** ___________________
**Date:** ___________________
**Signature:** ___________________
