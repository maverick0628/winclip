$ErrorActionPreference = 'Stop'

$packageName = 'winclip'
$toolsDir    = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url64       = 'https://duncansmith.tech/apps/winclip/releases/v2.2.0/WinClip-2.2.0-x64-Setup.exe'

$packageArgs = @{
  packageName    = $packageName
  fileType       = 'exe'
  url64bit       = $url64
  softwareName   = 'WinClip*'

  # Inno Setup silent installation arguments
  silentArgs     = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-'
  validExitCodes = @(0)

  # SHA256 checksum — update when building the release
  checksum64     = '730F24B89F251663CD09C9DF8A13774A253AE27961E5F22FC8B2C3C8767DBDF8'
  checksumType64 = 'sha256'
}

Install-ChocolateyPackage @packageArgs






