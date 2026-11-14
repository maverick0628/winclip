#!/bin/bash
#
# Update package manager manifests with new version and checksums
#
# Usage:
#   ./update-manifests.sh <version> <url_x64> <url_x86> <hash_x64> <hash_x86>
#
# Example:
#   ./update-manifests.sh 1.0.0 \
#     "https://github.com/org/repo/releases/download/v1.0.0/WinClipSetup-1.0.0-x64.exe" \
#     "https://github.com/org/repo/releases/download/v1.0.0/WinClipSetup-1.0.0-x86.exe" \
#     "ABC123..." \
#     "DEF456..."
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Check arguments
if [ $# -ne 5 ]; then
    echo -e "${RED}Error: Expected 5 arguments${NC}"
    echo "Usage: $0 <version> <url_x64> <url_x86> <hash_x64> <hash_x86>"
    echo ""
    echo "Example:"
    echo "  $0 1.0.0 \\"
    echo "    'https://github.com/org/repo/releases/download/v1.0.0/WinClipSetup-1.0.0-x64.exe' \\"
    echo "    'https://github.com/org/repo/releases/download/v1.0.0/WinClipSetup-1.0.0-x86.exe' \\"
    echo "    'ABC123...' \\"
    echo "    'DEF456...'"
    exit 1
fi

VERSION="$1"
URL_X64="$2"
URL_X86="$3"
HASH_X64="$4"
HASH_X86="$5"

# Get script directory to find distribution folder
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

echo -e "${BLUE}=== Updating Package Manager Manifests ===${NC}"
echo ""
echo -e "Version:    ${GREEN}$VERSION${NC}"
echo -e "x64 URL:    $URL_X64"
echo -e "x86 URL:    $URL_X86"
echo -e "x64 SHA256: $HASH_X64"
echo -e "x86 SHA256: $HASH_X86"
echo ""

# Check if running on macOS or Linux for sed compatibility
if [[ "$OSTYPE" == "darwin"* ]]; then
    SED_INPLACE=(-i '')
else
    SED_INPLACE=(-i)
fi

#
# Update Winget Manifests
#
echo -e "${YELLOW}Updating Winget manifests...${NC}"

# Update version manifest
WINGET_VERSION_FILE="$DIST_DIR/winget/WinClip.WinClip.yaml"
if [ -f "$WINGET_VERSION_FILE" ]; then
    sed "${SED_INPLACE[@]}" "s/^PackageVersion: .*/PackageVersion: $VERSION/" "$WINGET_VERSION_FILE"
    echo -e "  ${GREEN}✓${NC} Updated $WINGET_VERSION_FILE"
else
    echo -e "  ${RED}✗${NC} File not found: $WINGET_VERSION_FILE"
fi

# Update installer manifest
WINGET_INSTALLER_FILE="$DIST_DIR/winget/WinClip.WinClip.installer.yaml"
if [ -f "$WINGET_INSTALLER_FILE" ]; then
    # Update version
    sed "${SED_INPLACE[@]}" "s/^PackageVersion: .*/PackageVersion: $VERSION/" "$WINGET_INSTALLER_FILE"

    # Update x64 installer URL and hash
    # This is tricky with sed, so we'll use a temporary file approach
    awk -v url="$URL_X64" -v hash="$HASH_X64" '
        /^  - Architecture: x64/ { x64_section=1 }
        /^  - Architecture: x86/ { x64_section=0 }
        x64_section && /^    InstallerUrl:/ { print "    InstallerUrl: " url; next }
        x64_section && /^    InstallerSha256:/ { print "    InstallerSha256: " hash; next }
        { print }
    ' "$WINGET_INSTALLER_FILE" > "$WINGET_INSTALLER_FILE.tmp"
    mv "$WINGET_INSTALLER_FILE.tmp" "$WINGET_INSTALLER_FILE"

    # Update x86 installer URL and hash
    awk -v url="$URL_X86" -v hash="$HASH_X86" '
        /^  - Architecture: x86/ { x86_section=1 }
        /^  - Architecture:/ && !/^  - Architecture: x86/ { x86_section=0 }
        x86_section && /^    InstallerUrl:/ { print "    InstallerUrl: " url; next }
        x86_section && /^    InstallerSha256:/ { print "    InstallerSha256: " hash; next }
        { print }
    ' "$WINGET_INSTALLER_FILE" > "$WINGET_INSTALLER_FILE.tmp"
    mv "$WINGET_INSTALLER_FILE.tmp" "$WINGET_INSTALLER_FILE"

    echo -e "  ${GREEN}✓${NC} Updated $WINGET_INSTALLER_FILE"
else
    echo -e "  ${RED}✗${NC} File not found: $WINGET_INSTALLER_FILE"
fi

# Update locale manifest
WINGET_LOCALE_FILE="$DIST_DIR/winget/WinClip.WinClip.locale.en-US.yaml"
if [ -f "$WINGET_LOCALE_FILE" ]; then
    sed "${SED_INPLACE[@]}" "s/^PackageVersion: .*/PackageVersion: $VERSION/" "$WINGET_LOCALE_FILE"
    echo -e "  ${GREEN}✓${NC} Updated $WINGET_LOCALE_FILE"
else
    echo -e "  ${RED}✗${NC} File not found: $WINGET_LOCALE_FILE"
fi

echo ""

#
# Update Chocolatey Manifests
#
echo -e "${YELLOW}Updating Chocolatey manifests...${NC}"

# Update nuspec version (XML file)
CHOCO_NUSPEC_FILE="$DIST_DIR/chocolatey/winclip.nuspec"
if [ -f "$CHOCO_NUSPEC_FILE" ]; then
    sed "${SED_INPLACE[@]}" "s|<version>[^<]*</version>|<version>$VERSION</version>|" "$CHOCO_NUSPEC_FILE"
    echo -e "  ${GREEN}✓${NC} Updated $CHOCO_NUSPEC_FILE"
else
    echo -e "  ${RED}✗${NC} File not found: $CHOCO_NUSPEC_FILE"
fi

# Update install script
CHOCO_INSTALL_FILE="$DIST_DIR/chocolatey/tools/chocolateyinstall.ps1"
if [ -f "$CHOCO_INSTALL_FILE" ]; then
    sed "${SED_INPLACE[@]}" "s|url\s*=\s*'[^']*'|url        = '$URL_X86'|" "$CHOCO_INSTALL_FILE"
    sed "${SED_INPLACE[@]}" "s|url64bit\s*=\s*'[^']*'|url64bit   = '$URL_X64'|" "$CHOCO_INSTALL_FILE"
    sed "${SED_INPLACE[@]}" "s|checksum\s*=\s*'[^']*'|checksum   = '$HASH_X86'|" "$CHOCO_INSTALL_FILE"
    sed "${SED_INPLACE[@]}" "s|checksum64\s*=\s*'[^']*'|checksum64 = '$HASH_X64'|" "$CHOCO_INSTALL_FILE"
    echo -e "  ${GREEN}✓${NC} Updated $CHOCO_INSTALL_FILE"
else
    echo -e "  ${RED}✗${NC} File not found: $CHOCO_INSTALL_FILE"
fi

# Update verification file
CHOCO_VERIFICATION_FILE="$DIST_DIR/chocolatey/tools/VERIFICATION.txt"
if [ -f "$CHOCO_VERIFICATION_FILE" ]; then
    sed "${SED_INPLACE[@]}" "s|x64: [A-Fa-f0-9]\{64\}|x64: $HASH_X64|" "$CHOCO_VERIFICATION_FILE"
    sed "${SED_INPLACE[@]}" "s|x86: [A-Fa-f0-9]\{64\}|x86: $HASH_X86|" "$CHOCO_VERIFICATION_FILE"
    sed "${SED_INPLACE[@]}" "s|WinClipSetup-[0-9.]*-x64\.exe|WinClipSetup-$VERSION-x64.exe|g" "$CHOCO_VERIFICATION_FILE"
    sed "${SED_INPLACE[@]}" "s|WinClipSetup-[0-9.]*-x86\.exe|WinClipSetup-$VERSION-x86.exe|g" "$CHOCO_VERIFICATION_FILE"
    sed "${SED_INPLACE[@]}" "s|download/v[0-9.]*|download/v$VERSION|g" "$CHOCO_VERIFICATION_FILE"
    echo -e "  ${GREEN}✓${NC} Updated $CHOCO_VERIFICATION_FILE"
else
    echo -e "  ${RED}✗${NC} File not found: $CHOCO_VERIFICATION_FILE"
fi

echo ""

#
# Update Scoop Manifest
#
echo -e "${YELLOW}Updating Scoop manifest...${NC}"

SCOOP_MANIFEST_FILE="$DIST_DIR/scoop/winclip.json"
if [ -f "$SCOOP_MANIFEST_FILE" ]; then
    # Check if jq is available for proper JSON manipulation
    if command -v jq &> /dev/null; then
        jq \
            --arg version "$VERSION" \
            --arg url_x64 "$URL_X64" \
            --arg url_x86 "$URL_X86" \
            --arg hash_x64 "$HASH_X64" \
            --arg hash_x86 "$HASH_X86" \
            '.version = $version |
             .architecture."64bit".url = $url_x64 |
             .architecture."64bit".hash = $hash_x64 |
             .architecture."32bit".url = $url_x86 |
             .architecture."32bit".hash = $hash_x86' \
            "$SCOOP_MANIFEST_FILE" > "$SCOOP_MANIFEST_FILE.tmp"
        mv "$SCOOP_MANIFEST_FILE.tmp" "$SCOOP_MANIFEST_FILE"
        echo -e "  ${GREEN}✓${NC} Updated $SCOOP_MANIFEST_FILE (using jq)"
    else
        # Fallback to sed (less reliable for JSON)
        echo -e "  ${YELLOW}⚠${NC}  jq not found, using sed (install jq for better JSON handling)"
        sed "${SED_INPLACE[@]}" "s|\"version\": \"[^\"]*\"|\"version\": \"$VERSION\"|" "$SCOOP_MANIFEST_FILE"
        sed "${SED_INPLACE[@]}" "s|\"url\": \"https://github.com/.*/WinClipSetup-[^\"]*-x64.exe\"|\"url\": \"$URL_X64\"|" "$SCOOP_MANIFEST_FILE"
        sed "${SED_INPLACE[@]}" "s|\"url\": \"https://github.com/.*/WinClipSetup-[^\"]*-x86.exe\"|\"url\": \"$URL_X86\"|" "$SCOOP_MANIFEST_FILE"

        # Update hashes (this is fragile with sed, recommend jq)
        awk -v hash_x64="$HASH_X64" -v hash_x86="$HASH_X86" '
            /"64bit":/ { in_64bit=1; in_32bit=0 }
            /"32bit":/ { in_32bit=1; in_64bit=0 }
            in_64bit && /"hash":/ { print "            \"hash\": \"" hash_x64 "\","; next }
            in_32bit && /"hash":/ { print "            \"hash\": \"" hash_x86 "\","; next }
            { print }
        ' "$SCOOP_MANIFEST_FILE" > "$SCOOP_MANIFEST_FILE.tmp"
        mv "$SCOOP_MANIFEST_FILE.tmp" "$SCOOP_MANIFEST_FILE"
        echo -e "  ${GREEN}✓${NC} Updated $SCOOP_MANIFEST_FILE (using sed)"
    fi
else
    echo -e "  ${RED}✗${NC} File not found: $SCOOP_MANIFEST_FILE"
fi

echo ""
echo -e "${GREEN}=== Update Complete ===${NC}"
echo ""
echo -e "Updated manifests for version ${GREEN}$VERSION${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "  1. Review the changes: git diff distribution/"
echo "  2. Test manifest validation"
echo "  3. Commit changes: git add distribution/ && git commit -m 'Update manifests to v$VERSION'"
echo ""
