<#
.SYNOPSIS
    Build MSIX package for WinClip

.DESCRIPTION
    This script builds a WinClip MSIX package for Microsoft Store distribution.
    It restores dependencies, builds the application in Release mode, and generates
    the MSIX package for the specified architecture.

.PARAMETER Architecture
    Target architecture: x64 or ARM64 (default: x64)

.PARAMETER Configuration
    Build configuration (default: Release)

.PARAMETER Clean
    Clean build output before building

.PARAMETER Sign
    Sign the package after building

.PARAMETER CertificatePath
    Path to the PFX certificate file (required if -Sign is specified)

.PARAMETER CertificatePassword
    Password for the PFX certificate file (required if -Sign is specified)

.EXAMPLE
    .\build-msix.ps1 -Architecture x64
    Build MSIX package for x64 architecture

.EXAMPLE
    .\build-msix.ps1 -Architecture ARM64 -Clean
    Clean and build MSIX package for ARM64 architecture

.EXAMPLE
    .\build-msix.ps1 -Architecture x64 -Sign -CertificatePath ..\Certificates\WinClip-Test.pfx -CertificatePassword "test123"
    Build and sign MSIX package with a certificate
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
$OutputDir = Join-Path $ScriptDir ".." "Output"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WinClip MSIX Package Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Architecture:  $Architecture" -ForegroundColor Yellow
Write-Host "Project Root:  $ProjectRoot" -ForegroundColor Yellow
Write-Host "Output Dir:    $OutputDir" -ForegroundColor Yellow
Write-Host ""

# Ensure output directory exists
if (-not (Test-Path $OutputDir)) {
    Write-Host "Creating output directory..." -ForegroundColor Gray
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

try {
    # Change to project root
    Push-Location $ProjectRoot

    # Determine total steps based on options
    $totalSteps = 4
    if ($Sign) { $totalSteps = 5 }

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

    # Step 3: Build the application
    Write-Host "[3/$totalSteps] Building application for $Architecture..." -ForegroundColor Green

    # MSBuild parameters for MSIX generation
    $msbuildParams = @(
        'build',
        '--configuration', $Configuration,
        '--no-restore',
        '/p:Platform=' + $Architecture,
        '/p:AppxBundle=Always',
        '/p:AppxBundlePlatforms=' + $Architecture,
        '/p:GenerateAppxPackageOnBuild=true',
        '/p:AppxPackageDir=' + $OutputDir + '\',
        '/p:UapAppxPackageBuildMode=StoreUpload'
    )

    Write-Host "      Running: dotnet $($msbuildParams -join ' ')" -ForegroundColor DarkGray
    & dotnet $msbuildParams

    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "      Build completed successfully" -ForegroundColor Gray
    Write-Host ""

    # Step 4: Verify output
    Write-Host "[4/$totalSteps] Verifying MSIX package..." -ForegroundColor Green

    # Look for generated MSIX files in output directory
    $msixFiles = Get-ChildItem -Path $OutputDir -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue
    $msixBundleFiles = Get-ChildItem -Path $OutputDir -Filter "*.msixbundle" -Recurse -ErrorAction SilentlyContinue
    $allMsixFiles = $msixFiles + $msixBundleFiles

    if ($allMsixFiles.Count -eq 0) {
        Write-Warning "No MSIX files found in output directory"
        Write-Host "      Checking alternate build locations..." -ForegroundColor Gray

        # Check bin directory as fallback
        $binMsix = Get-ChildItem -Path (Join-Path $ProjectRoot "bin") -Filter "*.msix*" -Recurse -ErrorAction SilentlyContinue
        if ($binMsix.Count -gt 0) {
            Write-Host "      Found MSIX files in bin directory:" -ForegroundColor Yellow
            foreach ($file in $binMsix) {
                Write-Host "        - $($file.FullName)" -ForegroundColor Gray
                Write-Host "          Size: $([math]::Round($file.Length / 1MB, 2)) MB" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "      MSIX package created successfully!" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Generated files:" -ForegroundColor Cyan

        foreach ($file in $allMsixFiles) {
            Write-Host "  - $($file.Name)" -ForegroundColor White
            Write-Host "    Path: $($file.FullName)" -ForegroundColor Gray
            Write-Host "    Size: $([math]::Round($file.Length / 1MB, 2)) MB" -ForegroundColor Gray
        }
    }
    Write-Host ""

    # Step 5: Sign package if requested
    if ($Sign) {
        Write-Host "[5/$totalSteps] Signing MSIX package..." -ForegroundColor Green

        if ($allMsixFiles.Count -eq 0) {
            throw "Cannot sign: No MSIX files found to sign"
        }

        # Sign each MSIX file found
        $signScriptPath = Join-Path $ScriptDir "sign-package.ps1"
        if (-not (Test-Path $signScriptPath)) {
            throw "Sign script not found: $signScriptPath"
        }

        foreach ($file in $allMsixFiles) {
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
        Write-Host "      All packages signed successfully!" -ForegroundColor Gray
        Write-Host ""
    }

    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Build completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    if ($Sign) {
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Test the MSIX package by installing it" -ForegroundColor Gray
        Write-Host "  2. Submit to Microsoft Store Partner Center" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "  1. Test the MSIX package by installing it" -ForegroundColor Gray
        Write-Host "  2. Sign the package for production (see sign-package.ps1)" -ForegroundColor Gray
        Write-Host "  3. Submit to Microsoft Store Partner Center" -ForegroundColor Gray
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
    Write-Host "Stack Trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    Write-Host ""

    Pop-Location
    exit 1
} finally {
    Pop-Location
}
