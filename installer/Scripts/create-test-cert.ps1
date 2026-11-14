<#
.SYNOPSIS
    Create a self-signed code signing certificate for testing

.DESCRIPTION
    This script creates a self-signed certificate that can be used for signing
    WinClip installers during development and testing. The certificate is NOT
    suitable for production use and should only be used on development machines.

.PARAMETER CertificateName
    The subject name for the certificate (default: "WinClip Test Certificate")

.PARAMETER Password
    Password to protect the exported PFX file (default: "test123")

.PARAMETER OutputPath
    Directory where the PFX file will be saved (default: ../Certificates/)

.PARAMETER ExportToFile
    Export the certificate to a PFX file (default: true)

.PARAMETER InstallToStore
    Install the certificate to the local machine's Trusted Root store (default: false)

.PARAMETER ValidityYears
    Number of years the certificate will be valid (default: 2)

.EXAMPLE
    .\create-test-cert.ps1
    Create a test certificate with default settings

.EXAMPLE
    .\create-test-cert.ps1 -CertificateName "WinClip Development" -Password "MyPassword123"
    Create a test certificate with custom name and password

.EXAMPLE
    .\create-test-cert.ps1 -InstallToStore
    Create a test certificate and install it to the Trusted Root store

.NOTES
    Requires administrative privileges to install to Trusted Root store.
    Test certificates should NEVER be distributed to end users.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$CertificateName = "WinClip Test Certificate",

    [Parameter(Mandatory=$false)]
    [string]$Password = "test123",

    [Parameter(Mandatory=$false)]
    [string]$OutputPath = $null,

    [Parameter(Mandatory=$false)]
    [bool]$ExportToFile = $true,

    [Parameter(Mandatory=$false)]
    [switch]$InstallToStore,

    [Parameter(Mandatory=$false)]
    [int]$ValidityYears = 2
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Script directory and default output path
$ScriptDir = Split-Path -Parent $PSCommandPath
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $ScriptDir ".." "Certificates"
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WinClip Test Certificate Creator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Certificate Name: $CertificateName" -ForegroundColor Yellow
Write-Host "Output Directory: $OutputPath" -ForegroundColor Yellow
Write-Host "Validity Period:  $ValidityYears years" -ForegroundColor Yellow
Write-Host "Export to File:   $ExportToFile" -ForegroundColor Yellow
Write-Host "Install to Store: $InstallToStore" -ForegroundColor Yellow
Write-Host ""

# Security warning
Write-Host "⚠️  WARNING: This creates a SELF-SIGNED certificate for TESTING ONLY" -ForegroundColor Red
Write-Host "    - Do NOT use for production releases" -ForegroundColor Red
Write-Host "    - Do NOT distribute installers signed with this certificate" -ForegroundColor Red
Write-Host "    - Only install on development machines" -ForegroundColor Red
Write-Host ""

# Check if running as administrator (needed for installing to store)
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if ($InstallToStore -and -not $isAdmin) {
    Write-Warning "Installing to Trusted Root store requires administrator privileges."
    Write-Host "Please run this script as Administrator or use -InstallToStore:`$false" -ForegroundColor Yellow
    Write-Host ""

    $response = Read-Host "Continue without installing to store? (Y/N)"
    if ($response -ne 'Y' -and $response -ne 'y') {
        Write-Host "Operation cancelled." -ForegroundColor Gray
        exit 0
    }
    $InstallToStore = $false
}

try {
    # Ensure output directory exists
    if ($ExportToFile -and -not (Test-Path $OutputPath)) {
        Write-Host "[1/3] Creating output directory..." -ForegroundColor Green
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
        Write-Host "      Directory created: $OutputPath" -ForegroundColor Gray
    } else {
        Write-Host "[1/3] Output directory exists" -ForegroundColor Gray
    }
    Write-Host ""

    # Step 2: Create the self-signed certificate
    Write-Host "[2/3] Creating self-signed certificate..." -ForegroundColor Green

    $notAfter = (Get-Date).AddYears($ValidityYears)

    # Create certificate with code signing usage
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=$CertificateName" `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256 `
        -NotAfter $notAfter `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyUsage DigitalSignature `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3") `
        -FriendlyName $CertificateName

    Write-Host "      Certificate created successfully" -ForegroundColor Gray
    Write-Host "      Thumbprint: $($cert.Thumbprint)" -ForegroundColor Gray
    Write-Host "      Valid From: $($cert.NotBefore)" -ForegroundColor Gray
    Write-Host "      Valid To:   $($cert.NotAfter)" -ForegroundColor Gray
    Write-Host ""

    # Step 3: Export and/or install
    Write-Host "[3/3] Finalizing certificate..." -ForegroundColor Green

    # Export to PFX file
    if ($ExportToFile) {
        $pfxPath = Join-Path $OutputPath "WinClip-Test.pfx"
        $securePassword = ConvertTo-SecureString -String $Password -Force -AsPlainText

        Write-Host "      Exporting to PFX file..." -ForegroundColor Gray
        Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePassword | Out-Null

        Write-Host "      Certificate exported to:" -ForegroundColor Gray
        Write-Host "      $pfxPath" -ForegroundColor White
        Write-Host "      Password: $Password" -ForegroundColor White
        Write-Host ""
    }

    # Install to Trusted Root store
    if ($InstallToStore) {
        Write-Host "      Installing to Trusted Root Certification Authorities..." -ForegroundColor Gray

        # Export the certificate (without private key) and import to Root store
        $cerPath = [System.IO.Path]::GetTempFileName()
        Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null

        $store = New-Object System.Security.Cryptography.X509Certificates.X509Store(
            [System.Security.Cryptography.X509Certificates.StoreName]::Root,
            [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine
        )

        try {
            $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
            $rootCert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($cerPath)
            $store.Add($rootCert)
            Write-Host "      Certificate installed to Trusted Root store" -ForegroundColor Gray
        } finally {
            $store.Close()
            Remove-Item $cerPath -ErrorAction SilentlyContinue
        }
        Write-Host ""
    }

    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Certificate created successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    # Usage instructions
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host ""

    if ($ExportToFile) {
        Write-Host "  1. Use this certificate to sign packages:" -ForegroundColor Gray
        Write-Host "     .\sign-package.ps1 -PackagePath <package> -CertificatePath `"$pfxPath`" -Password `"$Password`"" -ForegroundColor White
        Write-Host ""
    }

    if (-not $InstallToStore) {
        Write-Host "  2. To test signed installers, install certificate to Trusted Root:" -ForegroundColor Gray
        Write-Host "     • Re-run this script with -InstallToStore flag (requires admin)" -ForegroundColor White
        Write-Host "     • Or manually: Double-click the PFX file and import to 'Trusted Root Certification Authorities'" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host "  2. Test signed installers on this machine" -ForegroundColor Gray
        Write-Host "     The certificate is trusted on this machine." -ForegroundColor White
        Write-Host ""
    }

    Write-Host "  3. Verify certificate installation:" -ForegroundColor Gray
    Write-Host "     certmgr.msc → Trusted Root Certification Authorities → Certificates" -ForegroundColor White
    Write-Host ""

    Write-Host "⚠️  Remember:" -ForegroundColor Yellow
    Write-Host "    - This is a TEST certificate only" -ForegroundColor Gray
    Write-Host "    - For production, obtain a certificate from DigiCert, Sectigo, or GlobalSign" -ForegroundColor Gray
    Write-Host "    - See Certificates/README.md for details" -ForegroundColor Gray
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Certificate Creation Failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""

    if ($_.Exception.Message -match "access|privilege|permission") {
        Write-Host "💡 Tip: Try running PowerShell as Administrator" -ForegroundColor Yellow
    }

    Write-Host "Stack Trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    Write-Host ""

    exit 1
}
