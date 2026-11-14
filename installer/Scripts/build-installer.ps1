<#
.SYNOPSIS
    Build Inno Setup installer for WinClip

.DESCRIPTION
    This script builds a WinClip installer using Inno Setup for direct distribution.
    It publishes the .NET application as self-contained, then compiles the installer
    using Inno Setup Compiler (iscc.exe) for the specified architecture.

.PARAMETER Architecture
    Target architecture: x64 or ARM64 (default: x64)

.PARAMETER Configuration
    Build configuration (default: Release)

.PARAMETER Clean
    Clean build output before building

.PARAMETER InnoSetupPath
    Path to Inno Setup Compiler (iscc.exe). If not specified, script will search common locations.

.PARAMETER Sign
    Sign the installer after building

.PARAMETER CertificatePath
    Path to the PFX certificate file (required if -Sign is specified)

.PARAMETER CertificatePassword
    Password for the PFX certificate file (required if -Sign is specified)

.EXAMPLE
    .\build-installer.ps1 -Architecture x64
    Build installer for x64 architecture

.EXAMPLE
    .\build-installer.ps1 -Architecture ARM64 -Clean
    Clean and build installer for ARM64 architecture

.EXAMPLE
    .\build-installer.ps1 -Architecture x64 -InnoSetupPath "C:\Program Files (x86)\Inno Setup 6\iscc.exe"
    Build installer with custom Inno Setup path

.EXAMPLE
    .\build-installer.ps1 -Architecture x64 -Sign -CertificatePath ..\Certificates\WinClip-Test.pfx -CertificatePassword "test123"
    Build and sign installer with a certificate
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('x64', 'ARM64')]
    [string]$Architecture = 'x64',

    [Parameter(Mandatory=$false)]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory=$false)]
    [switch]$Clean,

    [Parameter(Mandatory=$false)]
    [string]$InnoSetupPath = "",

    [Parameter(Mandatory=$false)]
    [switch]$Sign,

    [Parameter(Mandatory=$false)]
    [string]$CertificatePath = "",

    [Parameter(Mandatory=$false)]
    [string]$CertificatePassword = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Validate signing parameters
if ($Sign) {
    if (-not $CertificatePath) {
        Write-Error "-CertificatePath is required when -Sign is specified"
        exit 1
    }
    if (-not $CertificatePassword) {
        Write-Error "-CertificatePassword is required when -Sign is specified"
        exit 1
    }
}

# Script directory and project root
$ScriptDir = Split-Path -Parent $PSCommandPath
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$InstallerDir = Join-Path $ScriptDir ".."
$OutputDir = Join-Path $InstallerDir "Output"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WinClip Inno Setup Installer Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Architecture:  $Architecture" -ForegroundColor Yellow
Write-Host "Project Root:  $ProjectRoot" -ForegroundColor Yellow
Write-Host "Installer Dir: $InstallerDir" -ForegroundColor Yellow
Write-Host "Output Dir:    $OutputDir" -ForegroundColor Yellow
Write-Host ""

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    Write-Host "Creating output directory..." -ForegroundColor Gray
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Find Inno Setup Compiler
function Find-InnoSetup {
    if ($InnoSetupPath -and (Test-Path $InnoSetupPath)) {
        return $InnoSetupPath
    }

    # Common installation paths
    $commonPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe",
        "${env:ProgramFiles}\Inno Setup 6\iscc.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\iscc.exe",
        "${env:ProgramFiles}\Inno Setup 5\iscc.exe"
    )

    foreach ($path in $commonPaths) {
        if (Test-Path $path) {
            return $path
        }
    }

    # Try to find it in PATH
    $isccCmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($isccCmd) {
        return $isccCmd.Source
    }

    return $null
}

try {
    # Change to project root
    Push-Location $ProjectRoot

    # Determine total steps based on options
    $totalSteps = 5
    if ($Sign) { $totalSteps = 6 }

    # Step 1: Clean if requested
    if ($Clean) {
        Write-Host "[1/$totalSteps] Cleaning previous build outputs..." -ForegroundColor Green
        dotnet clean --configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Clean failed with exit code $LASTEXITCODE"
        }
        Write-Host "      Clean completed successfully" -ForegroundColor Gray
    } else {
        Write-Host "[1/$totalSteps] Skipping clean (use -Clean to clean before build)" -ForegroundColor Gray
    }
    Write-Host ""

    # Step 2: Restore dependencies
    Write-Host "[2/$totalSteps] Restoring NuGet dependencies..." -ForegroundColor Green
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed with exit code $LASTEXITCODE"
    }
    Write-Host "      Dependencies restored successfully" -ForegroundColor Gray
    Write-Host ""

    # Step 3: Publish the application
    Write-Host "[3/$totalSteps] Publishing application for $Architecture..." -ForegroundColor Green

    # Determine runtime identifier
    $rid = switch ($Architecture) {
        'x64'   { 'win-x64' }
        'ARM64' { 'win-arm64' }
    }

    $publishDir = Join-Path $ProjectRoot "bin\$Configuration\net8.0-windows\$rid\publish"

    # Publish parameters
    $publishParams = @(
        'publish',
        '--configuration', $Configuration,
        '--runtime', $rid,
        '--self-contained', 'true',
        '--no-restore',
        '-p:PublishSingleFile=false',
        '-p:PublishReadyToRun=true'
    )

    Write-Host "      Runtime:      $rid" -ForegroundColor DarkGray
    Write-Host "      Publish Dir:  $publishDir" -ForegroundColor DarkGray
    Write-Host "      Running: dotnet $($publishParams -join ' ')" -ForegroundColor DarkGray
    & dotnet $publishParams

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed with exit code $LASTEXITCODE"
    }

    # Verify publish directory exists and has files
    if (-not (Test-Path $publishDir)) {
        throw "Publish directory not found: $publishDir"
    }

    $publishedFiles = Get-ChildItem -Path $publishDir -File
    if ($publishedFiles.Count -eq 0) {
        throw "No files found in publish directory: $publishDir"
    }

    Write-Host "      Published $($publishedFiles.Count) files to $publishDir" -ForegroundColor Gray
    Write-Host ""

    # Step 4: Find Inno Setup Compiler
    Write-Host "[4/$totalSteps] Locating Inno Setup Compiler..." -ForegroundColor Green

    $isccPath = Find-InnoSetup
    if (-not $isccPath) {
        throw "Inno Setup Compiler (iscc.exe) not found. Please install Inno Setup from https://jrsoftware.org/isdl.php or specify path with -InnoSetupPath"
    }

    Write-Host "      Found: $isccPath" -ForegroundColor Gray
    Write-Host ""

    # Step 5: Compile installer with Inno Setup
    Write-Host "[5/$totalSteps] Compiling installer with Inno Setup..." -ForegroundColor Green

    # Determine which .iss file to use
    $issFile = switch ($Architecture) {
        'x64'   { 'WinClip-x64.iss' }
        'ARM64' { 'WinClip-arm64.iss' }
    }
    $issPath = Join-Path $InstallerDir $issFile

    if (-not (Test-Path $issPath)) {
        throw "Inno Setup script not found: $issPath"
    }

    Write-Host "      Script: $issFile" -ForegroundColor DarkGray
    Write-Host "      Running: $isccPath ""$issPath""" -ForegroundColor DarkGray

    # Run Inno Setup Compiler
    & $isccPath $issPath

    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup compilation failed with exit code $LASTEXITCODE"
    }

    Write-Host "      Compilation completed successfully" -ForegroundColor Gray
    Write-Host ""

    # Step 6: Verify output
    Write-Host "Verifying installer output..." -ForegroundColor Green

    # Look for generated installer in output directory
    $expectedPattern = "WinClip-Setup-*$($Architecture.ToLower())-*.exe"
    $installerFiles = Get-ChildItem -Path $OutputDir -Filter "*.exe" | Where-Object { $_.Name -like $expectedPattern }

    if ($installerFiles.Count -eq 0) {
        Write-Warning "Expected installer not found with pattern: $expectedPattern"
        Write-Host "      Checking all EXE files in output directory..." -ForegroundColor Gray

        $allExeFiles = Get-ChildItem -Path $OutputDir -Filter "*.exe" -ErrorAction SilentlyContinue
        if ($allExeFiles.Count -gt 0) {
            Write-Host "      Found installer files:" -ForegroundColor Yellow
            foreach ($file in $allExeFiles) {
                Write-Host "        - $($file.FullName)" -ForegroundColor Gray
                Write-Host "          Size: $([math]::Round($file.Length / 1MB, 2)) MB" -ForegroundColor Gray
            }
            # Use all found EXE files if expected pattern not found
            $installerFiles = $allExeFiles
        } else {
            throw "No installer files found in output directory: $OutputDir"
        }
    } else {
        Write-Host "      Installer created successfully!" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Generated installer:" -ForegroundColor Cyan

        foreach ($file in $installerFiles) {
            Write-Host "  - $($file.Name)" -ForegroundColor White
            Write-Host "    Path: $($file.FullName)" -ForegroundColor Gray
            Write-Host "    Size: $([math]::Round($file.Length / 1MB, 2)) MB" -ForegroundColor Gray
        }
    }
    Write-Host ""

    # Step 7: Sign installer if requested
    if ($Sign) {
        Write-Host "[6/$totalSteps] Signing installer..." -ForegroundColor Green

        if ($installerFiles.Count -eq 0) {
            throw "Cannot sign: No installer files found to sign"
        }

        # Sign each installer file found
        $signScriptPath = Join-Path $ScriptDir "sign-package.ps1"
        if (-not (Test-Path $signScriptPath)) {
            throw "Sign script not found: $signScriptPath"
        }

        foreach ($file in $installerFiles) {
            Write-Host "      Signing: $($file.Name)" -ForegroundColor Gray

            # Call sign-package.ps1
            & $signScriptPath `
                -PackagePath $file.FullName `
                -CertificatePath $CertificatePath `
                -Password $CertificatePassword `
                -Verify $true

            if ($LASTEXITCODE -ne 0) {
                throw "Signing failed for $($file.Name)"
            }

            Write-Host "      Signed successfully: $($file.Name)" -ForegroundColor Gray
        }

        Write-Host ""
        Write-Host "      All installers signed successfully!" -ForegroundColor Gray
        Write-Host ""
    }

    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Build completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    if ($Sign) {
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Test the installer by running it" -ForegroundColor Gray
        Write-Host "  2. Verify auto-start option works correctly" -ForegroundColor Gray
        Write-Host "  3. Test uninstallation and cleanup" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Test the installer by running it" -ForegroundColor Gray
        Write-Host "  2. Verify auto-start option works correctly" -ForegroundColor Gray
        Write-Host "  3. Test uninstallation and cleanup" -ForegroundColor Gray
        Write-Host "  4. Sign the installer for production (see sign-package.ps1)" -ForegroundColor Gray
        Write-Host ""
    }

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Build Failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""

    if ($_.Exception.Message -like "*iscc.exe*") {
        Write-Host "Inno Setup is required to build the installer." -ForegroundColor Yellow
        Write-Host "Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
        Write-Host ""
    }

    Write-Host "Stack Trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    Write-Host ""

    Pop-Location
    exit 1
} finally {
    Pop-Location
}
