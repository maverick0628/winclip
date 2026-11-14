# MSIX Auto-Update via `.appinstaller`

WinClip's non-Store distribution can use Windows' built-in MSIX auto-update mechanism
instead of (or alongside) the in-app `UpdateService` polling the R2 update feed.

## How it works

1. The maintainer hosts a `.appinstaller` XML at a stable HTTPS URL (`duncansmith.tech/apps/winclip/winclip.appinstaller`).
2. Users install once by opening that URL — Windows installs the linked `.msixbundle` and remembers the source URL.
3. On every subsequent app launch (throttled to once per 24h) Windows re-fetches the
   `.appinstaller`, compares versions, and installs the bundle if newer.
4. `<AutomaticBackgroundTask />` registers a Windows Maintenance Task that does the
   same poll even when WinClip isn't running, so always-on machines get patched silently.

## Release process

The template lives at [`WinClip.Package/winclip.appinstaller.template.xml`](../WinClip.Package/winclip.appinstaller.template.xml).
Each release:

```powershell
# 1. Render the template (replace __VERSION__ with the new version, e.g. 2.1.0)
$version = "2.1.0"
(Get-Content WinClip.Package\winclip.appinstaller.template.xml) `
    -replace "__VERSION__", $version `
    | Set-Content WinClip.Package\AppPackages\winclip.appinstaller -Encoding UTF8

# 2. Build + sign the bundle (already in CI's release.yml)
msbuild WinClip.Package\WinClip.Package.wapproj /p:Configuration=Release /p:Platform=x64

# 3. Upload both files to the static host for duncansmith.tech
#    under /apps/winclip/
```

## URL layout on duncansmith.tech

| URL | Content |
|---|---|
| `/apps/winclip/winclip.appinstaller` | The XML manifest (small, updated each release) |
| `/apps/winclip/WinClip.msixbundle` | The signed MSIX bundle (~tens of MB) |

The MSIX bundle URL inside the manifest must match the bundle's actual hosted URL or
Windows will fail validation with `0x80190005`.

## User-facing install

The shortest install string:

```
ms-appinstaller:?source=https://duncansmith.tech/apps/winclip/winclip.appinstaller
```

That URL scheme is intercepted by Windows 10/11 and triggers the App Installer dialog
without a download/extract step. Marketing copy on the WinClip product page should
include a "Smart Auto-Update Install" button pointing at this URL alongside the
existing winget/choco/scoop options.

## Coexistence with the in-app `UpdateService`

The two mechanisms aren't mutually exclusive but they probably should be — if both
fire, the user sees double-update prompts. Recommended cutover:

- For users who installed via `.appinstaller` or the Store: `UpdateService` already
  skips the feed when the process has package identity (`GetCurrentPackageFullName`).
- For users on the legacy Inno Setup installer: keep `UpdateService` polling the R2
  update feed. They self-migrate by reinstalling with the .appinstaller URL when ready.
