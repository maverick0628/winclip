# WinClip Package Distribution Guide

This document provides comprehensive instructions for submitting and maintaining WinClip across three Windows package managers: Winget, Chocolatey, and Scoop.

## Current status and release order

Winget (`WinClip.WinClip`) and Chocolatey (`winclip`) are published and carry 2.1.2. The Scoop manifest in `scoop/` is kept up to date by CI but is not in any public bucket, so `scoop install winclip` does not work yet. The steps below are what a release actually takes today. The older sections further down describe first-time setup.

1. **Tag and release.** Push a `vX.Y.Z` tag. `release.yml` signs both installers, publishes them and `latest.json` to R2 and creates the GitHub release. The package manifests download from duncansmith.tech, not from the GitHub release.
2. **Put the installers on duncansmith.tech.** Every package manifest downloads from `https://duncansmith.tech/apps/winclip/releases/vX.Y.Z/WinClip-X.Y.Z-{x64,arm64}-Setup.exe`. Publish both installers and `SHA256SUMS.txt` at that path on the site and update the download links on the WinClip page. Don't request the new URLs before the Pages deploy finishes, or Cloudflare caches a 404 at the edge.
3. **Manifests.** `package-release.yml` runs after `release.yml` succeeds. It updates the winget, Chocolatey and Scoop manifests, regenerates `chocolatey/tools/VERIFICATION.txt` and copies the version's CHANGELOG section into the nuspec `releaseNotes`, then commits to master. Add the version's CHANGELOG section before tagging.
4. **Winget.** Copy the three files in `winget/` to `manifests/w/WinClip/WinClip/X.Y.Z/` in a fork of `microsoft/winget-pkgs` and open a PR titled `New version: WinClip.WinClip version X.Y.Z`. The Contributor License Agreement was signed for DST Digital, LLC on 2026-09-18 and covers later submissions.
5. **Chocolatey.** On Windows, run `choco pack` then `choco push winclip.X.Y.Z.nupkg --source https://push.chocolatey.org/` in `chocolatey/`. The API key is saved on that machine with `choco apikey`. Moderation takes one to three business days.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Winget Distribution](#winget-distribution)
- [Chocolatey Distribution](#chocolatey-distribution)
- [Scoop Distribution](#scoop-distribution)
- [Update Procedures](#update-procedures)
- [Review Timelines](#review-timelines)
- [Troubleshooting](#troubleshooting)

---

## Overview

WinClip is distributed through three major Windows package managers to provide users with flexible installation options:

- **Winget**: Official Microsoft package manager, integrated with Windows 10/11
- **Chocolatey**: Popular enterprise package manager with extensive repository
- **Scoop**: User-focused portable application manager

Each package manager has different submission processes, validation requirements, and maintenance procedures.

---

## Prerequisites

### Required Accounts

1. **GitHub Account**
   - Required for Winget and Scoop submissions
   - Must have 2FA enabled for Winget submissions
   - Used for creating PRs to respective repositories

2. **Chocolatey Account**
   - Create account at [chocolatey.org](https://community.chocolatey.org/account/Register)
   - API key needed for package publishing
   - Email verification required

### Required Tools

- **Git**: For cloning repositories and creating PRs
- **Text editor**: For editing manifest files
- **PowerShell 5.0+**: For Chocolatey package testing (Windows)
- **Winget CLI**: Optional but recommended for manifest validation
- **Chocolatey CLI**: Optional for local package testing

### Release Artifacts

Before submitting to any package manager, ensure you have:

- ✅ Published release with version tag (e.g., `v2.1.2`)
- ✅ Installer binaries (`.exe`, x64 and ARM64) published on duncansmith.tech, not only on the GitHub release
- ✅ SHA256 checksum of installer binary
- ✅ Release notes documenting changes

---

## Winget Distribution

### Initial Submission

Winget packages are submitted as pull requests to the [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) repository.

#### Step 1: Fork the Repository

```bash
# Navigate to https://github.com/microsoft/winget-pkgs
# Click "Fork" to create your own fork
git clone https://github.com/YOUR-USERNAME/winget-pkgs.git
cd winget-pkgs
```

#### Step 2: Create Package Directory

```bash
# Package directory structure: manifests/<first-letter>/<Publisher>/<PackageName>/<Version>/
mkdir -p manifests/w/WinClip/WinClip/1.0.0
```

#### Step 3: Copy Manifest Files

Copy the three manifest files from `distribution/winget/` to the new directory:

```bash
cp /path/to/winclip/distribution/winget/WinClip.WinClip.yaml manifests/w/WinClip/WinClip/1.0.0/
cp /path/to/winclip/distribution/winget/WinClip.WinClip.installer.yaml manifests/w/WinClip/WinClip/1.0.0/
cp /path/to/winclip/distribution/winget/WinClip.WinClip.locale.en-US.yaml manifests/w/WinClip/WinClip/1.0.0/
```

#### Step 4: Update Version and Checksums

Edit the files to include:
- Current version number
- Installer download URL (on duncansmith.tech)
- SHA256 checksum of installer

```yaml
# In WinClip.WinClip.installer.yaml
InstallerUrl: https://duncansmith.tech/apps/winclip/releases/v2.1.2/WinClip-2.1.2-x64-Setup.exe
InstallerSha256: <SHA256-HASH>
```

#### Step 5: Validate Manifests

```bash
# If winget CLI is installed
winget validate manifests/w/WinClip/WinClip/1.0.0

# Alternative: Use online validator
# https://github.com/microsoft/winget-pkgs/blob/master/Tools/YamlCreate.ps1
```

#### Step 6: Create Pull Request

```bash
git checkout -b winclip-1.0.0
git add manifests/w/WinClip/
git commit -m "New package: WinClip version 1.0.0"
git push origin winclip-1.0.0

# Create PR on GitHub from your fork to microsoft/winget-pkgs
```

#### Step 7: PR Description Template

Use this template for your PR description:

```markdown
## New Package Submission: WinClip

**Package Name**: WinClip
**Version**: 1.0.0
**Publisher**: [Your Organization]

### Description
WinClip is a Windows clipboard manager that enhances productivity by providing advanced clipboard history and management features.

### Verification
- [x] Manifests validated with winget validate
- [x] Installer tested on Windows 10/11
- [x] SHA256 checksum verified
- [x] All URLs accessible
- [x] Silent install tested

### Additional Notes
First submission for WinClip. The application is signed and includes silent install options.
```

### Review Process

- **Automated validation**: GitHub Actions will validate manifest syntax
- **Manual review**: Microsoft reviewers check package quality and security
- **Expected timeline**: 3-7 days for initial submission
- **Approval**: Merge triggers package availability in Winget catalog

---

## Chocolatey Distribution

### Initial Submission

Chocolatey packages are submitted directly to chocolatey.org after creating an account.

#### Step 1: Create Chocolatey Account

1. Visit [chocolatey.org/account/Register](https://community.chocolatey.org/account/Register)
2. Complete email verification
3. Navigate to account settings to get API key

#### Step 2: Prepare Package Locally

```bash
cd distribution/chocolatey

# Update version in winclip.nuspec
# Update checksums in tools/chocolateyinstall.ps1
# Verify VERIFICATION.txt is current
```

#### Step 3: Test Package Locally

```powershell
# Build the package
choco pack winclip.nuspec

# Test installation (requires admin PowerShell)
choco install winclip -source . -force

# Test uninstallation
choco uninstall winclip
```

#### Step 4: Push to Chocolatey

```powershell
# Set API key (one-time setup)
choco apikey --key YOUR-API-KEY --source https://push.chocolatey.org/

# Push package
choco push winclip.1.0.0.nupkg --source https://push.chocolatey.org/
```

#### Step 5: Package Verification

After pushing, the package enters moderation:

1. **Automated scanning**: Virus scan and validation checks
2. **Moderator review**: Human review of package contents
3. **Approval/Rejection**: Notification via email
4. **Published**: Package becomes publicly available

### Package Guidelines

Ensure your package meets Chocolatey requirements:

- ✅ Package ID is lowercase: `winclip`
- ✅ Version follows semantic versioning
- ✅ Description is clear and concise (< 4000 chars)
- ✅ Install script uses official Chocolatey helpers
- ✅ Silent install works without user interaction
- ✅ Uninstall script properly removes application
- ✅ VERIFICATION.txt documents source and checksums
- ✅ No binaries in package (downloads from official source)
- ✅ Copyright and license information included

### Review Process

- **Automated validation**: Immediate on push
- **Moderation queue**: Packages reviewed in order submitted
- **Expected timeline**: 24-48 hours for initial submission, faster for updates
- **Trusted packages**: After several successful submissions, packages may auto-approve

---

## Scoop Distribution

### Option A: Main Scoop Repository (Recommended)

Submit to the main Scoop repository for maximum visibility.

#### Step 1: Fork Scoop Main Bucket

```bash
# Fork https://github.com/ScoopInstaller/Main
git clone https://github.com/YOUR-USERNAME/Main.git scoop-main
cd scoop-main
```

#### Step 2: Create Manifest

```bash
# Copy manifest
cp /path/to/winclip/distribution/scoop/winclip.json bucket/winclip.json
```

#### Step 3: Validate Manifest

```bash
# Validate JSON syntax
python -m json.tool bucket/winclip.json

# If scoop is installed, test locally
scoop install bucket/winclip.json
scoop uninstall winclip
```

#### Step 4: Create Pull Request

```bash
git checkout -b add-winclip
git add bucket/winclip.json
git commit -m "winclip: Add version 1.0.0"
git push origin add-winclip

# Create PR on GitHub
```

#### Step 5: PR Description Template

```markdown
## New App: WinClip

**App name**: winclip
**Version**: 1.0.0
**Homepage**: https://duncansmith.tech/apps/winclip

### Checklist
- [x] Valid JSON manifest
- [x] App name is lowercase
- [x] Homepage URL is correct
- [x] License is specified
- [x] Description is clear
- [x] SHA256 hash is correct
- [x] Installation tested locally

### Description
WinClip is a Windows clipboard manager that provides advanced clipboard history and management features.
```

### Option B: Custom Bucket

Create your own Scoop bucket for faster updates and more control.

#### Step 1: Create Bucket Repository

```bash
# Create new GitHub repository named "scoop-winclip-bucket"
git clone https://github.com/YOUR-ORG/scoop-winclip-bucket.git
cd scoop-winclip-bucket
```

#### Step 2: Add Manifest

```bash
cp /path/to/winclip/distribution/scoop/winclip.json winclip.json
git add winclip.json
git commit -m "Add WinClip manifest"
git push
```

#### Step 3: Document Installation

Users will install via:

```powershell
scoop bucket add winclip https://github.com/YOUR-ORG/scoop-winclip-bucket
scoop install winclip
```

### Review Process

**Main Bucket (Option A)**:
- **Automated validation**: GitHub Actions check manifest
- **Maintainer review**: Scoop maintainers review new apps
- **Expected timeline**: 1-5 days depending on complexity
- **Requirements**: Must meet Scoop app guidelines

**Custom Bucket (Option B)**:
- **No review needed**: You control the bucket
- **Immediate availability**: Changes are live on git push
- **Less visibility**: Users must add custom bucket manually

---

## Update Procedures

### Automated Updates (Recommended)

The GitHub Actions workflow (`.github/workflows/package-release.yml`) automates updates:

1. **Trigger**: Creating a new GitHub release
2. **Actions**:
   - Updates manifest files with new version and checksums
   - Creates PR to winget-pkgs repository
   - Pushes updated package to Chocolatey
   - Updates Scoop bucket (if custom bucket)
3. **Manual steps**: Approve Winget PR, verify Chocolatey moderation

### Manual Updates

If automation is not set up or fails:

#### Winget Update

```bash
# Fork and clone winget-pkgs if not already done
cd winget-pkgs
git pull upstream master
git checkout -b winclip-1.1.0

# Create new version directory
mkdir -p manifests/w/WinClip/WinClip/1.1.0
cp manifests/w/WinClip/WinClip/1.0.0/* manifests/w/WinClip/WinClip/1.1.0/

# Edit files to update version and checksums
# Then create PR as described in initial submission
```

#### Chocolatey Update

```powershell
cd distribution/chocolatey

# package-release.yml has already updated winclip.nuspec,
# tools/chocolateyinstall.ps1 and tools/VERIFICATION.txt on master.
git pull

choco pack winclip.nuspec
choco push winclip.2.1.2.nupkg --source https://push.chocolatey.org/
```

#### Scoop Update

**Main Bucket**:
```bash
cd scoop-main
git pull upstream master
git checkout -b update-winclip-1.1.0

# Edit bucket/winclip.json with new version, URL, and hash
git add bucket/winclip.json
git commit -m "winclip: Update to version 1.1.0"
git push origin update-winclip-1.1.0

# Create PR
```

**Custom Bucket**:
```bash
cd scoop-winclip-bucket

# Edit winclip.json with new version, URL, and hash
git add winclip.json
git commit -m "Update WinClip to version 1.1.0"
git push
```

---

## Review Timelines

Expected review and approval timelines for each package manager:

| Package Manager | Initial Submission | Updates | Auto-Approval |
|-----------------|-------------------|---------|---------------|
| **Winget** | 3-7 days | 2-5 days | No |
| **Chocolatey** | 24-48 hours | 12-24 hours | After trusted status |
| **Scoop (Main)** | 1-5 days | 1-3 days | No |
| **Scoop (Custom)** | Immediate | Immediate | N/A |

### Factors Affecting Review Time

- **Package complexity**: More complex packages take longer
- **Security concerns**: Packages with security issues require additional review
- **Completeness**: Well-documented submissions are approved faster
- **Queue length**: Review times vary based on submission volume
- **Holidays/Weekends**: Reviews may be delayed during non-business days

---

## Troubleshooting

### Common Issues and Solutions

#### Winget

**Issue**: Manifest validation fails
```
Solution: Run winget validate and fix schema errors
Check manifest schema version matches template
Ensure all required fields are present
```

**Issue**: PR checks fail with "Invalid SHA256"
```
Solution: Recalculate installer checksum
Use certutil (Windows) or shasum (macOS/Linux)
Example: certutil -hashfile installer.msi SHA256
```

**Issue**: PR rejected for "Not following guidelines"
```
Solution: Review https://github.com/microsoft/winget-pkgs/blob/master/CONTRIBUTING.md
Ensure installer is from trusted source
Verify silent install parameters work
```

#### Chocolatey

**Issue**: Package rejected in moderation
```
Solution: Check email for rejection reason
Common issues:
- Missing VERIFICATION.txt
- Invalid checksums
- Binaries included in package (not allowed)
- Silent install doesn't work
```

**Issue**: "Unable to push package"
```
Solution: Verify API key is set correctly
Check package ID is available (not taken)
Ensure version number is higher than existing
```

**Issue**: Package installs but doesn't work
```
Solution: Test chocolateyinstall.ps1 manually
Check install path and shortcuts
Verify uninstall script removes everything
```

#### Scoop

**Issue**: JSON validation fails
```
Solution: Use json linter (jsonlint.com or python -m json.tool)
Check for missing commas, quotes, brackets
Validate URL and hash format
```

**Issue**: Hash mismatch error
```
Solution: Download installer and recalculate hash
Use: certutil -hashfile installer.msi SHA256
Update hash in manifest
```

**Issue**: PR rejected for "Doesn't meet guidelines"
```
Solution: Review https://github.com/ScoopInstaller/Scoop/wiki/Criteria-for-including-apps-in-the-main-bucket
Ensure app is widely used/popular
Check license is open source or freeware
Verify portable installation (no registry changes)
```

### Getting Help

- **Winget**: Open issue in [winget-pkgs](https://github.com/microsoft/winget-pkgs/issues)
- **Chocolatey**: Community forum at [community.chocolatey.org](https://community.chocolatey.org/)
- **Scoop**: Discord community or [GitHub discussions](https://github.com/ScoopInstaller/Scoop/discussions)

### Validation Tools

Calculate checksums:
```bash
# Windows
certutil -hashfile installer.msi SHA256

# macOS/Linux
shasum -a 256 installer.msi

# PowerShell
Get-FileHash installer.msi -Algorithm SHA256
```

Validate manifests:
```bash
# Winget
winget validate manifest/path/

# Chocolatey
choco pack --validate winclip.nuspec

# Scoop
python -m json.tool winclip.json
```

---

## Maintenance Best Practices

1. **Version Consistency**: Keep version numbers synchronized across all package managers
2. **Automated Testing**: Test packages locally before submission
3. **Documentation**: Keep manifests well-commented for future maintainers
4. **Monitoring**: Track package download statistics and user feedback
5. **Security**: Regularly update dependencies and respond to vulnerability reports
6. **Communication**: Respond promptly to package manager maintainer feedback

---

## Additional Resources

### Official Documentation

- [Winget Manifest Schema](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- [Chocolatey Package Creation](https://docs.chocolatey.org/en-us/create/create-packages)
- [Scoop App Manifests](https://github.com/ScoopInstaller/Scoop/wiki/App-Manifests)

### Community Resources

- [Winget Package Guidelines](https://github.com/microsoft/winget-pkgs/blob/master/CONTRIBUTING.md)
- [Chocolatey Moderation FAQ](https://docs.chocolatey.org/en-us/community-repository/moderation)
- [Scoop Buckets Wiki](https://github.com/ScoopInstaller/Scoop/wiki/Buckets)

---

## Revision History

- **v1.0** - Initial documentation (2024)
  - Added submission procedures for all three package managers
  - Documented update procedures and troubleshooting
  - Included review timelines and best practices
