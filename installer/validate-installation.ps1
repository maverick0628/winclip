<#
.SYNOPSIS
    Validation script for WinClip installer testing

.DESCRIPTION
    This script validates various aspects of the WinClip installation including:
    - Installation directory and files
    - Start Menu and Desktop shortcuts
    - Registry entries (settings and startup)
    - Proper cleanup after uninstallation

.PARAMETER TestType
    The type of validation to perform: Installation, Files, StartMenu, Desktop, Startup, Registry, Cleanup, or All

.PARAMETER Features
    For Installation tests, specify which features were installed: All, Minimal, or Custom

.EXAMPLE
    .\validate-installation.ps1 -TestType All
    Run all validation tests

.EXAMPLE
    .\validate-installation.ps1 -TestType Installation -Features All
    Validate installation with all features

.EXAMPLE
    .\validate-installation.ps1 -TestType Cleanup
    Validate post-uninstallation cleanup
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Installation", "Files", "StartMenu", "Desktop", "Startup", "Registry", "Cleanup", "All")]
    [string]$TestType,

    [Parameter(Mandatory=$false)]
    [ValidateSet("All", "Minimal", "Custom")]
    [string]$Features = "All"
)

$ErrorActionPreference = "Continue"
$script:TestsPassed = 0
$script:TestsFailed = 0
$script:TestsWarning = 0

# Color output functions
function Write-TestResult {
    param(
        [string]$TestName,
        [string]$Status,
        [string]$Message = ""
    )

    $statusColor = switch ($Status) {
        "PASS" { "Green" }
        "FAIL" { "Red" }
        "WARN" { "Yellow" }
        default { "White" }
    }

    $statusSymbol = switch ($Status) {
        "PASS" { "✅" }
        "FAIL" { "❌" }
        "WARN" { "⚠️" }
        default { "ℹ️" }
    }

    Write-Host "$statusSymbol " -NoNewline -ForegroundColor $statusColor
    Write-Host "$TestName " -NoNewline
    Write-Host "[$Status]" -ForegroundColor $statusColor

    if ($Message) {
        Write-Host "   $Message" -ForegroundColor Gray
    }

    switch ($Status) {
        "PASS" { $script:TestsPassed++ }
        "FAIL" { $script:TestsFailed++ }
        "WARN" { $script:TestsWarning++ }
    }
}

function Test-InstallationStatus {
    Write-Host "`n=== Testing Installation Status ===" -ForegroundColor Cyan

    # Check if WinClip is installed via registry
    $uninstallKey = Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" -ErrorAction SilentlyContinue |
        Get-ItemProperty | Where-Object { $_.DisplayName -like "*WinClip*" }

    if (-not $uninstallKey) {
        $uninstallKey = Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" -ErrorAction SilentlyContinue |
            Get-ItemProperty | Where-Object { $_.DisplayName -like "*WinClip*" }
    }

    if ($uninstallKey) {
        Write-TestResult "Installation Registry Entry" "PASS" "Found in Add/Remove Programs"
        Write-Host "   Display Name: $($uninstallKey.DisplayName)" -ForegroundColor Gray
        Write-Host "   Version: $($uninstallKey.DisplayVersion)" -ForegroundColor Gray
        Write-Host "   Publisher: $($uninstallKey.Publisher)" -ForegroundColor Gray
    } else {
        Write-TestResult "Installation Registry Entry" "FAIL" "Not found in Add/Remove Programs"
    }
}

function Test-InstallationFiles {
    Write-Host "`n=== Testing Installation Files ===" -ForegroundColor Cyan

    $installDir = "C:\Program Files\WinClip"
    $exePath = Join-Path $installDir "WinClip.exe"

    # Test installation directory
    if (Test-Path $installDir) {
        Write-TestResult "Installation Directory" "PASS" "$installDir exists"
    } else {
        Write-TestResult "Installation Directory" "FAIL" "$installDir not found"
        return
    }

    # Test main executable
    if (Test-Path $exePath) {
        $fileInfo = Get-Item $exePath
        Write-TestResult "Main Executable" "PASS" "WinClip.exe found ($([math]::Round($fileInfo.Length / 1KB, 2)) KB)"
    } else {
        Write-TestResult "Main Executable" "FAIL" "WinClip.exe not found"
    }

    # List all installed files
    $files = Get-ChildItem $installDir -Recurse -File -ErrorAction SilentlyContinue
    Write-Host "`n   Installed Files ($($files.Count) total):" -ForegroundColor Gray
    foreach ($file in $files | Select-Object -First 10) {
        Write-Host "   - $($file.Name) ($([math]::Round($file.Length / 1KB, 2)) KB)" -ForegroundColor Gray
    }
    if ($files.Count -gt 10) {
        Write-Host "   ... and $($files.Count - 10) more files" -ForegroundColor Gray
    }

    # Check for common .NET dependencies
    $expectedDlls = @("WinClip.dll", "System*.dll")
    $dllFiles = $files | Where-Object { $_.Extension -eq ".dll" }

    if ($dllFiles.Count -gt 0) {
        Write-TestResult "Dependency DLLs" "PASS" "Found $($dllFiles.Count) DLL files"
    } else {
        Write-TestResult "Dependency DLLs" "WARN" "No DLL files found (may be single-file deployment)"
    }
}

function Test-StartMenuShortcut {
    Write-Host "`n=== Testing Start Menu Shortcut ===" -ForegroundColor Cyan

    $startMenuPaths = @(
        "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\WinClip",
        "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\WinClip"
    )

    $shortcutFound = $false
    foreach ($path in $startMenuPaths) {
        $shortcutPath = Join-Path $path "WinClip.lnk"
        if (Test-Path $shortcutPath) {
            $shortcutFound = $true
            Write-TestResult "Start Menu Shortcut" "PASS" "$shortcutPath"

            # Validate shortcut target
            $shell = New-Object -ComObject WScript.Shell
            $shortcut = $shell.CreateShortcut($shortcutPath)
            Write-Host "   Target: $($shortcut.TargetPath)" -ForegroundColor Gray
            Write-Host "   Working Directory: $($shortcut.WorkingDirectory)" -ForegroundColor Gray

            if ($shortcut.TargetPath -like "*WinClip.exe") {
                Write-TestResult "Shortcut Target" "PASS" "Points to WinClip.exe"
            } else {
                Write-TestResult "Shortcut Target" "FAIL" "Invalid target path"
            }
            break
        }
    }

    if (-not $shortcutFound) {
        Write-TestResult "Start Menu Shortcut" "FAIL" "Shortcut not found in Start Menu"
    }
}

function Test-DesktopShortcut {
    Write-Host "`n=== Testing Desktop Shortcut ===" -ForegroundColor Cyan

    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopPath "WinClip.lnk"

    if ($Features -eq "All") {
        if (Test-Path $shortcutPath) {
            Write-TestResult "Desktop Shortcut" "PASS" "$shortcutPath"

            # Validate shortcut target
            $shell = New-Object -ComObject WScript.Shell
            $shortcut = $shell.CreateShortcut($shortcutPath)
            Write-Host "   Target: $($shortcut.TargetPath)" -ForegroundColor Gray

            if ($shortcut.TargetPath -like "*WinClip.exe") {
                Write-TestResult "Shortcut Target" "PASS" "Points to WinClip.exe"
            } else {
                Write-TestResult "Shortcut Target" "FAIL" "Invalid target path"
            }
        } else {
            Write-TestResult "Desktop Shortcut" "FAIL" "Expected but not found"
        }
    } else {
        if (Test-Path $shortcutPath) {
            Write-TestResult "Desktop Shortcut" "WARN" "Found but was not expected (feature not selected)"
        } else {
            Write-TestResult "Desktop Shortcut" "PASS" "Not present as expected"
        }
    }
}

function Test-StartupRegistry {
    Write-Host "`n=== Testing Startup Registry Entry ===" -ForegroundColor Cyan

    $runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $runValue = Get-ItemProperty -Path $runKey -Name "WinClip" -ErrorAction SilentlyContinue

    if ($Features -eq "All") {
        if ($runValue) {
            Write-TestResult "Startup Registry Entry" "PASS" "Found in Run key"
            Write-Host "   Key: $runKey" -ForegroundColor Gray
            Write-Host "   Value: $($runValue.WinClip)" -ForegroundColor Gray

            if ($runValue.WinClip -like "*WinClip.exe") {
                Write-TestResult "Startup Path" "PASS" "Points to WinClip.exe"
            } else {
                Write-TestResult "Startup Path" "FAIL" "Invalid path"
            }
        } else {
            Write-TestResult "Startup Registry Entry" "FAIL" "Expected but not found"
        }
    } else {
        if ($runValue) {
            Write-TestResult "Startup Registry Entry" "WARN" "Found but was not expected (feature not selected)"
        } else {
            Write-TestResult "Startup Registry Entry" "PASS" "Not present as expected"
        }
    }
}

function Test-RegistrySettings {
    Write-Host "`n=== Testing Registry Settings ===" -ForegroundColor Cyan

    $settingsKey = "HKCU:\Software\WinClip"

    if (Test-Path $settingsKey) {
        Write-TestResult "Settings Registry Key" "PASS" "$settingsKey exists"

        # Check for expected values
        $installed = Get-ItemProperty -Path $settingsKey -Name "Installed" -ErrorAction SilentlyContinue
        if ($installed -and $installed.Installed -eq 1) {
            Write-TestResult "Installed Registry Value" "PASS" "Set to 1"
        } else {
            Write-TestResult "Installed Registry Value" "WARN" "Not set or invalid"
        }

        # List all values
        $values = Get-ItemProperty -Path $settingsKey
        Write-Host "`n   Registry Values:" -ForegroundColor Gray
        $values.PSObject.Properties | Where-Object { $_.Name -notlike "PS*" } | ForEach-Object {
            Write-Host "   - $($_.Name) = $($_.Value)" -ForegroundColor Gray
        }
    } else {
        Write-TestResult "Settings Registry Key" "FAIL" "$settingsKey not found"
    }
}

function Test-Cleanup {
    Write-Host "`n=== Testing Post-Uninstall Cleanup ===" -ForegroundColor Cyan

    # Test installation directory removed
    $installDir = "C:\Program Files\WinClip"
    if (-not (Test-Path $installDir)) {
        Write-TestResult "Installation Directory Removed" "PASS" "$installDir deleted"
    } else {
        Write-TestResult "Installation Directory Removed" "FAIL" "$installDir still exists"
    }

    # Test Start Menu shortcuts removed
    $startMenuPaths = @(
        "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\WinClip",
        "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\WinClip"
    )

    $shortcutRemaining = $false
    foreach ($path in $startMenuPaths) {
        if (Test-Path $path) {
            $shortcutRemaining = $true
            Write-TestResult "Start Menu Folder Removed" "FAIL" "$path still exists"
        }
    }
    if (-not $shortcutRemaining) {
        Write-TestResult "Start Menu Shortcuts Removed" "PASS" "All shortcuts deleted"
    }

    # Test Desktop shortcut removed
    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopPath "WinClip.lnk"
    if (-not (Test-Path $shortcutPath)) {
        Write-TestResult "Desktop Shortcut Removed" "PASS" "Shortcut deleted"
    } else {
        Write-TestResult "Desktop Shortcut Removed" "FAIL" "Shortcut still exists"
    }

    # Test registry cleanup
    $settingsKey = "HKCU:\Software\WinClip"
    if (-not (Test-Path $settingsKey)) {
        Write-TestResult "Settings Registry Removed" "PASS" "Registry key deleted"
    } else {
        Write-TestResult "Settings Registry Removed" "FAIL" "Registry key still exists"
    }

    $runKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $runValue = Get-ItemProperty -Path $runKey -Name "WinClip" -ErrorAction SilentlyContinue
    if (-not $runValue) {
        Write-TestResult "Startup Registry Removed" "PASS" "Run entry deleted"
    } else {
        Write-TestResult "Startup Registry Removed" "FAIL" "Run entry still exists"
    }

    # Test AppData cleanup
    $appDataPath = Join-Path $env:APPDATA "WinClip"
    $localAppDataPath = Join-Path $env:LOCALAPPDATA "WinClip"

    if (-not (Test-Path $appDataPath)) {
        Write-TestResult "AppData Folder Removed" "PASS" "%APPDATA%\WinClip deleted"
    } else {
        Write-TestResult "AppData Folder Removed" "FAIL" "%APPDATA%\WinClip still exists"
    }

    if (-not (Test-Path $localAppDataPath)) {
        Write-TestResult "LocalAppData Folder Removed" "PASS" "%LOCALAPPDATA%\WinClip deleted"
    } else {
        Write-TestResult "LocalAppData Folder Removed" "WARN" "%LOCALAPPDATA%\WinClip still exists"
    }

    # Check Add/Remove Programs
    $uninstallKey = Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" -ErrorAction SilentlyContinue |
        Get-ItemProperty | Where-Object { $_.DisplayName -like "*WinClip*" }

    if (-not $uninstallKey) {
        $uninstallKey = Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" -ErrorAction SilentlyContinue |
            Get-ItemProperty | Where-Object { $_.DisplayName -like "*WinClip*" }
    }

    if (-not $uninstallKey) {
        Write-TestResult "Uninstall Entry Removed" "PASS" "Not in Add/Remove Programs"
    } else {
        Write-TestResult "Uninstall Entry Removed" "FAIL" "Still in Add/Remove Programs"
    }
}

# Main execution
Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   WinClip Installer Validation Script                 ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "Test Type: $TestType" -ForegroundColor Yellow
if ($TestType -eq "Installation") {
    Write-Host "Features: $Features" -ForegroundColor Yellow
}
Write-Host "Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Yellow
Write-Host ""

# Run appropriate tests
switch ($TestType) {
    "Installation" {
        Test-InstallationStatus
        Test-InstallationFiles
    }
    "Files" {
        Test-InstallationFiles
    }
    "StartMenu" {
        Test-StartMenuShortcut
    }
    "Desktop" {
        Test-DesktopShortcut
    }
    "Startup" {
        Test-StartupRegistry
    }
    "Registry" {
        Test-RegistrySettings
    }
    "Cleanup" {
        Test-Cleanup
    }
    "All" {
        Test-InstallationStatus
        Test-InstallationFiles
        Test-StartMenuShortcut
        Test-DesktopShortcut
        Test-StartupRegistry
        Test-RegistrySettings
    }
}

# Print summary
Write-Host "`n╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Test Summary                                         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host "✅ Tests Passed:  $script:TestsPassed" -ForegroundColor Green
Write-Host "❌ Tests Failed:  $script:TestsFailed" -ForegroundColor Red
Write-Host "⚠️  Warnings:      $script:TestsWarning" -ForegroundColor Yellow
Write-Host ""

$totalTests = $script:TestsPassed + $script:TestsFailed + $script:TestsWarning
if ($totalTests -gt 0) {
    $passRate = [math]::Round(($script:TestsPassed / $totalTests) * 100, 1)
    Write-Host "Pass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 90) { "Green" } elseif ($passRate -ge 70) { "Yellow" } else { "Red" })
}

# Exit with appropriate code
if ($script:TestsFailed -gt 0) {
    Write-Host "`n❌ VALIDATION FAILED - Some tests did not pass" -ForegroundColor Red
    exit 1
} elseif ($script:TestsWarning -gt 0) {
    Write-Host "`n⚠️  VALIDATION WARNING - All tests passed but with warnings" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "`n✅ VALIDATION PASSED - All tests completed successfully" -ForegroundColor Green
    exit 0
}
