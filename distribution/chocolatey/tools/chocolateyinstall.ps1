$ErrorActionPreference = 'Stop'

$packageName = 'winclip'
$toolsDir    = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url64       = 'https://duncansmith.tech/apps/winclip/releases/v2.3.0/WinClip-2.3.0-x64-Setup.exe'

$packageArgs = @{
  packageName    = $packageName
  fileType       = 'exe'
  url64bit       = $url64
  softwareName   = 'WinClip*'

  # Inno Setup silent installation arguments
  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
  validExitCodes = @(0)

  # SHA256 checksum — update when building the release
  checksum64     = '5C3859523FFC1325084761A76CAFDDC118A13DE70B1674B479C31EF3EF828AB5'
  checksumType64 = 'sha256'
}

Install-ChocolateyPackage @packageArgs







