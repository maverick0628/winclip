# WinClip Distribution

This directory contains package manager manifests and distribution configurations for WinClip.

## Package Managers

WinClip is distributed through the following Windows package managers:

### Winget (Windows Package Manager)
- **Directory**: `winget/`
- **Command**: `winget install WinClip.WinClip`
- **Repository**: [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs)

### Chocolatey
- **Directory**: `chocolatey/`
- **Command**: `choco install winclip`
- **Repository**: [chocolatey.org](https://chocolatey.org/)

### Scoop (not published)
- **Directory**: `scoop/`
- **Status**: the manifest is kept current, but WinClip is not in any public bucket, so `scoop install winclip` does not work yet
- **Repository**: [ScoopInstaller](https://github.com/ScoopInstaller)

## Documentation

- **DISTRIBUTION.md**: Release order and submission procedures. Start with "Current status and release order"
- **TESTING.md**: Testing checklist for package validation

## Automation

Package manifests are automatically updated via GitHub Actions workflow on each release.

See `.github/workflows/package-release.yml` for automation details.
