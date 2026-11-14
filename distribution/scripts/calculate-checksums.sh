#!/bin/bash
#
# Calculate SHA256 checksums for WinClip installer files
#
# Usage:
#   ./calculate-checksums.sh <x64-installer> <x86-installer>
#
# Example:
#   ./calculate-checksums.sh WinClipSetup-1.0.0-x64.exe WinClipSetup-1.0.0-x86.exe
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check arguments
if [ $# -ne 2 ]; then
    echo -e "${RED}Error: Expected 2 arguments${NC}"
    echo "Usage: $0 <x64-installer> <x86-installer>"
    echo ""
    echo "Example:"
    echo "  $0 WinClipSetup-1.0.0-x64.exe WinClipSetup-1.0.0-x86.exe"
    exit 1
fi

X64_FILE="$1"
X86_FILE="$2"

# Check if files exist
if [ ! -f "$X64_FILE" ]; then
    echo -e "${RED}Error: x64 installer not found: $X64_FILE${NC}"
    exit 1
fi

if [ ! -f "$X86_FILE" ]; then
    echo -e "${RED}Error: x86 installer not found: $X86_FILE${NC}"
    exit 1
fi

echo -e "${YELLOW}Calculating SHA256 checksums...${NC}"
echo ""

# Calculate checksums using shasum (macOS/Linux) or sha256sum (Linux)
if command -v sha256sum &> /dev/null; then
    HASH_CMD="sha256sum"
elif command -v shasum &> /dev/null; then
    HASH_CMD="shasum -a 256"
else
    echo -e "${RED}Error: Neither sha256sum nor shasum found${NC}"
    exit 1
fi

# Calculate x64 checksum
echo -e "${GREEN}x64 installer:${NC} $X64_FILE"
HASH_X64=$($HASH_CMD "$X64_FILE" | awk '{print $1}' | tr '[:lower:]' '[:upper:]')
echo -e "  SHA256: ${GREEN}$HASH_X64${NC}"
echo ""

# Calculate x86 checksum
echo -e "${GREEN}x86 installer:${NC} $X86_FILE"
HASH_X86=$($HASH_CMD "$X86_FILE" | awk '{print $1}' | tr '[:lower:]' '[:upper:]')
echo -e "  SHA256: ${GREEN}$HASH_X86${NC}"
echo ""

# Output in a format easy to copy/paste or use in scripts
echo -e "${YELLOW}Export variables:${NC}"
echo "export HASH_X64=$HASH_X64"
echo "export HASH_X86=$HASH_X86"
echo ""

# Save to file for easy sourcing
CHECKSUM_FILE="checksums.env"
cat > "$CHECKSUM_FILE" <<EOF
# SHA256 checksums for WinClip installers
# Generated: $(date -u +"%Y-%m-%d %H:%M:%S UTC")

HASH_X64=$HASH_X64
HASH_X86=$HASH_X86
EOF

echo -e "${GREEN}Checksums saved to: $CHECKSUM_FILE${NC}"
echo -e "You can source this file with: ${YELLOW}source $CHECKSUM_FILE${NC}"
