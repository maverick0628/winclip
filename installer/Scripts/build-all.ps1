<#
.SYNOPSIS
    Master build script for WinClip complete build pipeline

.DESCRIPTION
    This script orchestrates the complete build pipeline for WinClip, creating both
    MSIX packages and Inno Setup installers for x64 and ARM64 architectures.
    It optionally signs all packages and generates a release summary with checksums.

.PARAMETER Configuration
    Build configuration (default: Release)

.PARAMETER Clean
    Clean build output before building

.PARAMETER SkipMSIX
    Skip building MSIX packages

.PARAMETER SkipInstaller
    Skip building Inno Setup installers

.PARAMETER Architectures
    Target architectures to build (default: x64, ARM64)

.PARAMETER Sign
    Sign all packages after building

.PARAMETER CertificatePath
    Path to the PFX certificate file (required if -Sign is specified)

.PARAMETER CertificatePassword
    Password for the PFX certificate file (required if -Sign is specified)

.PARAMETER SkipChecksums
    Skip generating checksums in release summary

.EXAMPLE
    .\build-all.ps1
    Build all packages for x64 and ARM64 architectures

.EXAMPLE
    .\build-all.ps1 -Clean
    Clean and build all packages

.EXAMPLE
    .\build-all.ps1 -Architectures x64
    Build only x64 packages

.EXAMPLE
    .\build-all.ps1 -SkipMSIX
    Build only Inno Setup installers

.EXAMPLE
    .\build-all.ps1 -Sign -CertificatePath ..\Certificates\WinClip-Test.pfx -CertificatePassword "test123"
    Build and sign all packages

.NOTES
    This script calls build-msix.ps1 and build-installer.ps1 for each architecture.
    Generated packages are placed in installer/Output/ directory.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory=$false)]
    [switch]$Clean,

    [Parameter(Mandatory=$false)]
    [switch]$SkipMSIX,

    [Parameter(Mandatory=$false)]
    [switch]$SkipInstaller,

    [Parameter(Mandatory=$false)]
    [ValidateSet('x64', 'ARM64')]
    [string[]]$Architectures = @('x64', 'ARM64'),

    [Parameter(Mandatory=$false)]
    [switch]$Sign,

    [Parameter(Mandatory=$false)]
    [string]$CertificatePath = "",

    [Parameter(Mandatory=$false)]
    [string]$CertificatePassword = "",

    [Parameter(Mandatory=$false)]
    [switch]$SkipChecksums
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

# Validate that at least one build type is enabled
if ($SkipMSIX -and $SkipInstaller) {
    Write-Error "Cannot skip both MSIX and Installer builds. At least one must be enabled."
    exit 1
}

# Script directory and paths
$ScriptDir = Split-Path -Parent $PSCommandPath
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$OutputDir = Join-Path $ScriptDir ".." "Output"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WinClip Master Build Pipeline" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration:  $Configuration" -ForegroundColor Yellow
Write-Host "Architectures:  $($Architectures -join ', ')" -ForegroundColor Yellow
Write-Host "MSIX Packages:  $(if ($SkipMSIX) { 'Disabled' } else { 'Enabled' })" -ForegroundColor Yellow
Write-Host "Inno Setup:     $(if ($SkipInstaller) { 'Disabled' } else { 'Enabled' })" -ForegroundColor Yellow
Write-Host "Code Signing:   $(if ($Sign) { 'Enabled' } else { 'Disabled' })" -ForegroundColor Yellow
Write-Host "Project Root:   $ProjectRoot" -ForegroundColor Yellow
Write-Host "Output Dir:     $OutputDir" -ForegroundColor Yellow
Write-Host ""

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    Write-Host "Creating output directory..." -ForegroundColor Gray
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Track build results
$buildResults = @{
    MSIX = @{}
    Installer = @{}
    Errors = @()
}

# Start time tracking
$startTime = Get-Date

try {
    # Build MSIX packages
    if (-not $SkipMSIX) {
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "  Building MSIX Packages" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""

        $buildMsixScript = Join-Path $ScriptDir "build-msix.ps1"
        if (-not (Test-Path $buildMsixScript)) {
            throw "build-msix.ps1 not found: $buildMsixScript"
        }

        foreach ($arch in $Architectures) {
            Write-Host "Building MSIX for $arch..." -ForegroundColor Green
            Write-Host ""

            try {
                $params = @{
                    Architecture = $arch
                    Configuration = $Configuration
                }

                if ($Clean) {
                    $params['Clean'] = $true
                }

                if ($Sign) {
                    $params['Sign'] = $true
                    $params['CertificatePath'] = $CertificatePath
                    $params['CertificatePassword'] = $CertificatePassword
                }

                & $buildMsixScript @params

                if ($LASTEXITCODE -ne 0) {
                    throw "MSIX build failed for $arch with exit code $LASTEXITCODE"
                }

                $buildResults.MSIX[$arch] = "Success"
                Write-Host ""
            } catch {
                $errorMsg = "MSIX build failed for ${arch}: $_"
                Write-Warning $errorMsg
                $buildResults.MSIX[$arch] = "Failed: $_"
                $buildResults.Errors += $errorMsg
                Write-Host ""
            }
        }
    }

    # Build Inno Setup installers
    if (-not $SkipInstaller) {
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "  Building Inno Setup Installers" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""

        $buildInstallerScript = Join-Path $ScriptDir "build-installer.ps1"
        if (-not (Test-Path $buildInstallerScript)) {
            throw "build-installer.ps1 not found: $buildInstallerScript"
        }

        foreach ($arch in $Architectures) {
            Write-Host "Building Installer for $arch..." -ForegroundColor Green
            Write-Host ""

            try {
                $params = @{
                    Architecture = $arch
                    Configuration = $Configuration
                }

                if ($Clean) {
                    $params['Clean'] = $true
                }

                if ($Sign) {
                    $params['Sign'] = $true
                    $params['CertificatePath'] = $CertificatePath
                    $params['CertificatePassword'] = $CertificatePassword
                }

                & $buildInstallerScript @params

                if ($LASTEXITCODE -ne 0) {
                    throw "Installer build failed for $arch with exit code $LASTEXITCODE"
                }

                $buildResults.Installer[$arch] = "Success"
                Write-Host ""
            } catch {
                $errorMsg = "Installer build failed for ${arch}: $_"
                Write-Warning $errorMsg
                $buildResults.Installer[$arch] = "Failed: $_"
                $buildResults.Errors += $errorMsg
                Write-Host ""
            }
        }
    }

    # Calculate build time
    $endTime = Get-Date
    $buildDuration = $endTime - $startTime

    # Generate release summary
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Release Summary" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Build Duration: $($buildDuration.ToString('mm\:ss'))" -ForegroundColor Yellow
    Write-Host ""

    # Collect all output files
    $allFiles = @()
    if (Test-Path $OutputDir) {
        $allFiles = Get-ChildItem -Path $OutputDir -File -Recurse | Where-Object {
            $_.Extension -in @('.msix', '.msixbundle', '.exe')
        } | Sort-Object Name
    }

    if ($allFiles.Count -eq 0) {
        Write-Warning "No output files found in $OutputDir"
    } else {
        Write-Host "Generated Packages:" -ForegroundColor Cyan
        Write-Host ""

        $totalSize = 0

        foreach ($file in $allFiles) {
            $fileSizeMB = [math]::Round($file.Length / 1MB, 2)
            $totalSize += $file.Length

            Write-Host "  $($file.Name)" -ForegroundColor White
            Write-Host "    Path: $($file.FullName)" -ForegroundColor Gray
            Write-Host "    Size: $fileSizeMB MB" -ForegroundColor Gray

            # Calculate checksum if not skipped
            if (-not $SkipChecksums) {
                try {
                    $hash = Get-FileHash -Path $file.FullName -Algorithm SHA256
                    Write-Host "    SHA256: $($hash.Hash)" -ForegroundColor Gray
                } catch {
                    Write-Host "    SHA256: Failed to calculate" -ForegroundColor DarkGray
                }
            }

            Write-Host ""
        }

        $totalSizeMB = [math]::Round($totalSize / 1MB, 2)
        Write-Host "Total Size: $totalSizeMB MB" -ForegroundColor Yellow
        Write-Host "File Count: $($allFiles.Count)" -ForegroundColor Yellow
        Write-Host ""
    }

    # Show build results summary
    Write-Host "Build Results:" -ForegroundColor Cyan
    Write-Host ""

    if (-not $SkipMSIX) {
        Write-Host "  MSIX Packages:" -ForegroundColor White
        foreach ($arch in $Architectures) {
            $status = $buildResults.MSIX[$arch]
            $color = if ($status -eq "Success") { "Green" } else { "Red" }
            Write-Host "    $arch`: " -NoNewline -ForegroundColor Gray
            Write-Host $status -ForegroundColor $color
        }
        Write-Host ""
    }

    if (-not $SkipInstaller) {
        Write-Host "  Inno Setup Installers:" -ForegroundColor White
        foreach ($arch in $Architectures) {
            $status = $buildResults.Installer[$arch]
            $color = if ($status -eq "Success") { "Green" } else { "Red" }
            Write-Host "    $arch`: " -NoNewline -ForegroundColor Gray
            Write-Host $status -ForegroundColor $color
        }
        Write-Host ""
    }

    # Check for any errors
    if ($buildResults.Errors.Count -gt 0) {
        Write-Host "========================================" -ForegroundColor Yellow
        Write-Host "  Build Completed with Errors" -ForegroundColor Yellow
        Write-Host "========================================" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Errors encountered:" -ForegroundColor Red
        foreach ($error in $buildResults.Errors) {
            Write-Host "  - $error" -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "Some packages may not have been built. Check the errors above." -ForegroundColor Yellow
        Write-Host ""
        exit 1
    } else {
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "  Build Pipeline Completed Successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Test all generated packages on target architectures" -ForegroundColor Gray
        Write-Host "  2. Verify installation, launch, and uninstallation" -ForegroundColor Gray

        if (-not $Sign) {
            Write-Host "  3. Sign packages for production release (use -Sign option)" -ForegroundColor Gray
            Write-Host "  4. Submit MSIX to Microsoft Store Partner Center" -ForegroundColor Gray
            Write-Host "  5. Distribute EXE installers via website or download portal" -ForegroundColor Gray
        } else {
            Write-Host "  3. Submit MSIX to Microsoft Store Partner Center" -ForegroundColor Gray
            Write-Host "  4. Distribute EXE installers via website or download portal" -ForegroundColor Gray
        }
        Write-Host ""
    }

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Build Pipeline Failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    Write-Host ""

    exit 1
}
