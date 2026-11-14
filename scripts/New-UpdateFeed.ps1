<#
.SYNOPSIS
    Writes latest.json, the update feed installed copies of WinClip poll.

.DESCRIPTION
    release.yml runs this after the installers are built and signed, then uploads the
    installers and this file to the winclip-updates R2 bucket. The schema is read by
    Services/UpdateFeed.cs, and WinClip.Tests/Services/UpdateFeedScriptTests.cs runs this
    script and parses its output with that reader so the two cannot drift apart.

.EXAMPLE
    ./scripts/New-UpdateFeed.ps1 -Version 2.2.0 -BaseUrl https://winclip-updates.duncansmith.tech `
        -X64Installer installer/output/WinClip-2.2.0-x64-Setup.exe `
        -Arm64Installer installer/output/WinClip-2.2.0-arm64-Setup.exe `
        -OutFile installer/output/latest.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string] $BaseUrl,
    [Parameter(Mandatory)] [string] $X64Installer,
    [Parameter(Mandatory)] [string] $Arm64Installer,
    [Parameter(Mandatory)] [string] $OutFile,
    [string] $ReleaseNotesUrl = 'https://duncansmith.tech/apps/winclip/'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($Version -notmatch '^\d+\.\d+\.\d+$') {
    throw "Version '$Version' is not MAJOR.MINOR.PATCH. The feed only advertises stable releases."
}
if ($BaseUrl -notmatch '^https://[^/]+') {
    throw "BaseUrl '$BaseUrl' is not an https URL."
}
$BaseUrl = $BaseUrl.TrimEnd('/')

function Get-InstallerEntry {
    param([string] $Path, [string] $Arch)

    $expectedName = "WinClip-$Version-$Arch-Setup.exe"
    $item = Get-Item -LiteralPath $Path
    if ($item.Name -ne $expectedName) {
        throw "Installer '$($item.Name)' should be named '$expectedName'."
    }
    if ($item.Length -le 0) {
        throw "Installer '$($item.FullName)' is empty."
    }

    [ordered]@{
        url    = "$BaseUrl/$expectedName"
        sha256 = (Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash
        size   = $item.Length
    }
}

$x64 = Get-InstallerEntry -Path $X64Installer -Arch 'x64'
$arm64 = Get-InstallerEntry -Path $Arm64Installer -Arch 'arm64'

$feed = [ordered]@{
    schemaVersion   = 1
    product         = 'WinClip'
    version         = $Version
    publishedAt     = [DateTime]::UtcNow.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
    releaseNotesUrl = $ReleaseNotesUrl
    installers      = [ordered]@{
        x64   = $x64
        arm64 = $arm64
    }
}

$outPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutFile)
$json = ($feed | ConvertTo-Json -Depth 5) + "`n"
[IO.File]::WriteAllText($outPath, $json, [Text.UTF8Encoding]::new($false))
Write-Host "Wrote $outPath for WinClip $Version"
