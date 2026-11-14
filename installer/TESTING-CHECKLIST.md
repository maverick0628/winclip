# WinClip Installer Testing Checklist

## Quick Reference for Testers

This is a condensed checklist for quick validation. For detailed instructions, see [TEST-PLAN.md](./TEST-PLAN.md).

### Prerequisites
- [ ] Windows 10/11 system (VM recommended)
- [ ] .NET SDK 6.0+ installed
- [ ] WiX Toolset v4 installed (`dotnet tool install --global wix`)
- [ ] PowerShell 5.1 or PowerShell Core 7+
- [ ] Administrator access

---

## Build Phase

- [ ] **Build Installer**
  ```powershell
  .\scripts\build-installer.ps1
  ```
  - Expected: MSI created in `installer\bin\Release\en-US\WinClip.msi`
  - Expected: Build completes without errors
  - Expected: MSI file size < 20 MB

- [ ] **Verify Build Output**
  ```powershell
  Test-Path ".\installer\bin\Release\en-US\WinClip.msi"
  ```

---

## Installation Phase

- [ ] **Install with All Features**
  ```powershell
  # As Administrator
  msiexec /i "installer\bin\Release\en-US\WinClip.msi" /qn /l*v install-all.log ADDLOCAL=ALL
  ```

- [ ] **Run Validation Script**
  ```powershell
  .\installer\validate-installation.ps1 -TestType All -Features All
  ```

### Manual Verification

- [ ] **Check Installation Directory**
  - Path: `C:\Program Files\WinClip`
  - Contains: WinClip.exe and dependencies

- [ ] **Check Start Menu**
  - Location: Start Menu > Programs > WinClip
  - Launch WinClip from Start Menu
  - Verify app starts successfully

- [ ] **Check Desktop Shortcut**
  - Desktop icon present
  - Launch from desktop
  - Verify app starts successfully

- [ ] **Check Startup Entry**
  - Registry: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
  - Entry name: WinClip
  - Entry value: `C:\Program Files\WinClip\WinClip.exe`

- [ ] **Check Add/Remove Programs**
  - Open Settings > Apps > Installed apps
  - Find "WinClip" in list
  - Verify version and publisher info

- [ ] **Check Event Viewer**
  - Open Event Viewer
  - Check Windows Logs > Application
  - Filter for "MsiInstaller"
  - Verify no errors during installation

---

## Uninstallation Phase

- [ ] **Uninstall via MSI**
  ```powershell
  # As Administrator
  msiexec /x "installer\bin\Release\en-US\WinClip.msi" /qn /l*v uninstall.log
  ```

- [ ] **Run Cleanup Validation**
  ```powershell
  .\installer\validate-installation.ps1 -TestType Cleanup
  ```

### Manual Verification

- [ ] **Verify Files Removed**
  - `C:\Program Files\WinClip` - should NOT exist

- [ ] **Verify Shortcuts Removed**
  - Start Menu > Programs > WinClip - should NOT exist
  - Desktop shortcut - should NOT exist

- [ ] **Verify Registry Cleaned**
  - `HKCU\Software\WinClip` - should NOT exist
  - `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` - WinClip entry should NOT exist

- [ ] **Verify AppData Cleaned**
  - `%APPDATA%\WinClip` - should NOT exist
  - `%LOCALAPPDATA%\WinClip` - should NOT exist (if applicable)

- [ ] **Verify Uninstall Entry Removed**
  - Settings > Apps > Installed apps
  - WinClip should NOT be in list

- [ ] **Check Event Viewer**
  - Verify no errors during uninstallation

---

## Advanced Tests (Optional)

- [ ] **Test Custom Installation**
  ```powershell
  # Install with UI to select features
  msiexec /i "installer\bin\Release\en-US\WinClip.msi"
  ```
  - Deselect "Desktop Shortcut"
  - Keep "Run at Windows Startup" deselected
  - Verify only selected features installed

- [ ] **Test Reinstallation**
  - Install WinClip
  - Run installer again
  - Choose "Repair"
  - Verify repair succeeds

- [ ] **Test Custom Directory**
  - Install to custom path (e.g., `D:\WinClip`)
  - Verify installation succeeds
  - Verify shortcuts work

- [ ] **Test Multiple User Accounts**
  - Install as User A
  - Switch to User B
  - Verify Start Menu shortcut available
  - Verify Desktop shortcut NOT present (per-user)
  - Launch app as User B

---

## Test Results

**Date:** _____________
**Tester:** _____________
**Windows Version:** _____________
**WinClip Version:** _____________

### Status
- [ ] ✅ All tests passed - Ready for release
- [ ] ⚠️ Tests passed with warnings - Review needed
- [ ] ❌ Tests failed - Requires fixes

### Issues Found

1. _______________________________________________
2. _______________________________________________
3. _______________________________________________

### Notes

_____________________________________________________
_____________________________________________________
_____________________________________________________

### Sign-Off

**Tester Signature:** ___________________
**Date:** ___________________

---

## Quick Commands Reference

### Build
```powershell
.\scripts\build-installer.ps1
```

### Install (Silent, All Features)
```powershell
msiexec /i "installer\bin\Release\en-US\WinClip.msi" /qn /l*v install.log ADDLOCAL=ALL
```

### Install (With UI)
```powershell
msiexec /i "installer\bin\Release\en-US\WinClip.msi"
```

### Uninstall (Silent)
```powershell
msiexec /x "installer\bin\Release\en-US\WinClip.msi" /qn /l*v uninstall.log
```

### Validate Installation
```powershell
.\installer\validate-installation.ps1 -TestType All -Features All
```

### Validate Cleanup
```powershell
.\installer\validate-installation.ps1 -TestType Cleanup
```

### Check Installation Logs
```powershell
Get-Content install.log | Select-String -Pattern "error|fail" -Context 2
```

### Check Uninstallation Logs
```powershell
Get-Content uninstall.log | Select-String -Pattern "error|fail" -Context 2
```

---

## Troubleshooting

### Build Fails
- Verify .NET SDK installed: `dotnet --version`
- Verify WiX installed: `wix --version`
- Check WinClip.exe exists in publish folder

### Installation Fails
- Run as Administrator
- Check install.log for errors
- Verify no conflicting WinClip installation exists

### Uninstallation Fails
- Run as Administrator
- Check uninstall.log for errors
- Manually remove `C:\Program Files\WinClip` if needed

### Validation Script Fails
- Ensure PowerShell execution policy allows scripts
- Run as Administrator if needed
- Check that installation/uninstallation completed first

---

For detailed testing procedures, see [TEST-PLAN.md](./TEST-PLAN.md)
