<#
.SYNOPSIS
    Sign WinClip installer packages with a code signing certificate

.DESCRIPTION
    This script signs MSIX packages and EXE installers using signtool.exe from the Windows SDK.
    It supports both test certificates (for development) and production certificates.

.PARAMETER PackagePath
    Path to the package file to sign (.msix, .msixbundle, or .exe)

.PARAMETER CertificatePath
    Path to the PFX certificate file

.PARAMETER Password
    Password for the PFX certificate file

.PARAMETER TimestampUrl
    URL of the timestamp server (default: http://timestamp.digicert.com)

.PARAMETER SignToolPath
    Path to signtool.exe. If not specified, script will search common locations.

.PARAMETER Verify
    Verify the signature after signing (default: true)

.EXAMPLE
    .\sign-package.ps1 -PackagePath ..\Output\WinClip_x64.msix -CertificatePath ..\Certificates\WinClip-Test.pfx -Password "test123"
    Sign an MSIX package with a test certificate

.EXAMPLE
    .\sign-package.ps1 -PackagePath ..\Output\WinClip-Setup-x64-1.0.0.exe -CertificatePath ..\Certificates\WinClip-Production.pfx -Password $env:CERT_PASSWORD
    Sign an EXE installer with a production certificate

.EXAMPLE
    .\sign-package.ps1 -PackagePath ..\Output\WinClip_x64.msix -CertificatePath cert.pfx -Password "pass" -TimestampUrl "http://timestamp.sectigo.com"
    Sign with custom timestamp server

.NOTES
    Requires Windows SDK (signtool.exe) to be installed.
    For production releases, use a commercial code signing certificate from a trusted CA.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$PackagePath,

    [Parameter(Mandatory=$true)]
    [string]$CertificatePath,

    [Parameter(Mandatory=$true)]
    [string]$Password,

    [Parameter(Mandatory=$false)]
    [string]$TimestampUrl = "http://timestamp.digicert.com",

    [Parameter(Mandatory=$false)]
    [string]$SignToolPath = "",

    [Parameter(Mandatory=$false)]
    [bool]$Verify = $true
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WinClip Package Signing Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Find signtool.exe
function Find-SignTool {
    if ($SignToolPath -and (Test-Path $SignToolPath)) {
        return $SignToolPath
    }

    # Common Windows SDK installation paths
    $windowsKitsPath = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"

    if (Test-Path $windowsKitsPath) {
        # Find the latest SDK version
        $sdkVersions = Get-ChildItem -Path $windowsKitsPath -Directory |
            Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
            Sort-Object -Descending

        foreach ($version in $sdkVersions) {
            # Check for x64 version first, then x86
            $x64Path = Join-Path $version.FullName "x64\signtool.exe"
            $x86Path = Join-Path $version.FullName "x86\signtool.exe"

            if (Test-Path $x64Path) {
                return $x64Path
            } elseif (Test-Path $x86Path) {
                return $x86Path
            }
        }
    }

    # Try to find it in PATH
    $signToolCmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($signToolCmd) {
        return $signToolCmd.Source
    }

    return $null
}

try {
    # Validate package path
    Write-Host "[1/4] Validating input..." -ForegroundColor Green

    if (-not (Test-Path $PackagePath)) {
        throw "Package file not found: $PackagePath"
    }

    $packageFile = Get-Item $PackagePath
    $packageExt = $packageFile.Extension.ToLower()

    if ($packageExt -notin @('.msix', '.msixbundle', '.exe')) {
        throw "Unsupported package type: $packageExt (supported: .msix, .msixbundle, .exe)"
    }

    Write-Host "      Package:     $($packageFile.Name)" -ForegroundColor Gray
    Write-Host "      Type:        $packageExt" -ForegroundColor Gray
    Write-Host "      Size:        $([math]::Round($packageFile.Length / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host ""

    # Validate certificate
    if (-not (Test-Path $CertificatePath)) {
        throw "Certificate file not found: $CertificatePath"
    }

    $certFile = Get-Item $CertificatePath
    Write-Host "      Certificate: $($certFile.Name)" -ForegroundColor Gray
    Write-Host "      Timestamp:   $TimestampUrl" -ForegroundColor Gray
    Write-Host ""

    # Step 2: Find signtool.exe
    Write-Host "[2/4] Locating SignTool..." -ForegroundColor Green

    $signToolExe = Find-SignTool
    if (-not $signToolExe) {
        throw "SignTool.exe not found. Please install Windows SDK from https://developer.microsoft.com/windows/downloads/windows-sdk/ or specify path with -SignToolPath"
    }

    Write-Host "      Found: $signToolExe" -ForegroundColor Gray
    Write-Host ""

    # Step 3: Sign the package
    Write-Host "[3/4] Signing package..." -ForegroundColor Green

    # Build signtool command arguments
    $signToolArgs = @(
        'sign',
        '/f', "`"$CertificatePath`"",
        '/p', $Password,
        '/fd', 'SHA256',
        '/tr', $TimestampUrl,
        '/td', 'SHA256',
        '/v',
        "`"$PackagePath`""
    )

    Write-Host "      Running: signtool.exe $($signToolArgs -join ' ')" -ForegroundColor DarkGray
    Write-Host "      (password hidden for security)" -ForegroundColor DarkGray
    Write-Host ""

    # Execute signtool
    $signToolArgsString = $signToolArgs -join ' '
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = $signToolExe
    $processInfo.Arguments = $signToolArgsString
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $processInfo
    $process.Start() | Out-Null

    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    if ($process.ExitCode -ne 0) {
        Write-Host "      SignTool output:" -ForegroundColor Yellow
        Write-Host $stdout -ForegroundColor Gray
        if ($stderr) {
            Write-Host "      SignTool errors:" -ForegroundColor Red
            Write-Host $stderr -ForegroundColor Gray
        }
        throw "SignTool failed with exit code $($process.ExitCode)"
    }

    Write-Host "      Package signed successfully" -ForegroundColor Gray

    # Show relevant output (filter out verbose lines)
    $relevantLines = $stdout -split "`n" | Where-Object {
        $_ -match "(Successfully signed|Number of files successfully Signed)"
    }
    if ($relevantLines) {
        foreach ($line in $relevantLines) {
            Write-Host "      $($line.Trim())" -ForegroundColor DarkGray
        }
    }
    Write-Host ""

    # Step 4: Verify signature
    if ($Verify) {
        Write-Host "[4/4] Verifying signature..." -ForegroundColor Green

        $verifyArgs = @(
            'verify',
            '/pa',
            '/v',
            "`"$PackagePath`""
        )

        Write-Host "      Running: signtool.exe verify /pa /v `"$($packageFile.Name)`"" -ForegroundColor DarkGray
        Write-Host ""

        $verifyArgsString = $verifyArgs -join ' '
        $verifyProcessInfo = New-Object System.Diagnostics.ProcessStartInfo
        $verifyProcessInfo.FileName = $signToolExe
        $verifyProcessInfo.Arguments = $verifyArgsString
        $verifyProcessInfo.RedirectStandardOutput = $true
        $verifyProcessInfo.RedirectStandardError = $true
        $verifyProcessInfo.UseShellExecute = $false
        $verifyProcessInfo.CreateNoWindow = $true

        $verifyProcess = New-Object System.Diagnostics.Process
        $verifyProcess.StartInfo = $verifyProcessInfo
        $verifyProcess.Start() | Out-Null

        $verifyStdout = $verifyProcess.StandardOutput.ReadToEnd()
        $verifyStderr = $verifyProcess.StandardError.ReadToEnd()
        $verifyProcess.WaitForExit()

        if ($verifyProcess.ExitCode -ne 0) {
            Write-Warning "Signature verification failed"
            Write-Host "      Verify output:" -ForegroundColor Yellow
            Write-Host $verifyStdout -ForegroundColor Gray
            if ($verifyStderr) {
                Write-Host $verifyStderr -ForegroundColor Gray
            }
        } else {
            Write-Host "      Signature verified successfully" -ForegroundColor Gray

            # Show certificate info from verification
            $certInfoLines = $verifyStdout -split "`n" | Where-Object {
                $_ -match "(Issued to:|Issued by:|Expires:|SHA256 Thumbprint)"
            }
            if ($certInfoLines) {
                Write-Host ""
                Write-Host "      Certificate information:" -ForegroundColor Gray
                foreach ($line in $certInfoLines) {
                    Write-Host "        $($line.Trim())" -ForegroundColor DarkGray
                }
            }
        }
    } else {
        Write-Host "[4/4] Skipping verification (use -Verify:`$true to verify)" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Signing completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Signed package:" -ForegroundColor Yellow
    Write-Host "  $($packageFile.FullName)" -ForegroundColor White
    Write-Host ""

    if (-not $Verify) {
        Write-Host "To verify the signature manually:" -ForegroundColor Gray
        Write-Host "  signtool verify /pa /v `"$PackagePath`"" -ForegroundColor White
        Write-Host ""
    }

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Signing Failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""

    if ($_.Exception.Message -match "signtool") {
        Write-Host "💡 SignTool is part of the Windows SDK" -ForegroundColor Yellow
        Write-Host "   Download from: https://developer.microsoft.com/windows/downloads/windows-sdk/" -ForegroundColor Yellow
        Write-Host ""
    } elseif ($_.Exception.Message -match "certificate|password") {
        Write-Host "💡 Certificate troubleshooting:" -ForegroundColor Yellow
        Write-Host "   - Verify certificate path is correct" -ForegroundColor Gray
        Write-Host "   - Verify password is correct" -ForegroundColor Gray
        Write-Host "   - Ensure certificate is valid and not expired" -ForegroundColor Gray
        Write-Host "   - See installer/Certificates/README.md for details" -ForegroundColor Gray
        Write-Host ""
    }

    Write-Host "Stack Trace:" -ForegroundColor Gray
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    Write-Host ""

    exit 1
}
