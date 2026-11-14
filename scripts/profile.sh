#!/usr/bin/env bash
# CPU Profiling Script using cargo-flamegraph
# Generates flamegraph.svg for visual CPU profiling

set -e

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}WinClip CPU Profiling Tool${NC}"
echo "================================"

# Check if cargo-flamegraph is installed
if ! command -v cargo-flamegraph &> /dev/null; then
    echo -e "${RED}cargo-flamegraph not found!${NC}"
    echo "Installing cargo-flamegraph..."
    cargo install flamegraph
fi

# Default values
TARGET="perf_monitor"
DURATION=10
OUTPUT="flamegraph.svg"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --target)
            TARGET="$2"
            shift 2
            ;;
        --duration)
            DURATION="$2"
            shift 2
            ;;
        --output)
            OUTPUT="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --target BINARY    Binary to profile (default: perf_monitor)"
            echo "  --duration SECS    Duration to run in seconds (default: 10)"
            echo "  --output FILE      Output file name (default: flamegraph.svg)"
            echo "  --help            Show this help message"
            echo ""
            echo "Examples:"
            echo "  $0                                    # Profile perf_monitor for 10s"
            echo "  $0 --duration 30                      # Profile for 30s"
            echo "  $0 --target perf_monitor --duration 5 # Custom target and duration"
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo -e "${BLUE}Configuration:${NC}"
echo "  Target: $TARGET"
echo "  Duration: ${DURATION}s"
echo "  Output: $OUTPUT"
echo ""

# Run flamegraph
echo -e "${BLUE}Starting profiling...${NC}"
echo "This will take approximately ${DURATION} seconds"
echo ""

# Use cargo flamegraph with the binary target
cargo flamegraph --bin "$TARGET" -o "$OUTPUT" -- --duration "$DURATION"

if [ -f "$OUTPUT" ]; then
    echo ""
    echo -e "${GREEN}✓ Profiling complete!${NC}"
    echo -e "Flamegraph saved to: ${GREEN}$OUTPUT${NC}"
    echo ""
    echo "Open the flamegraph in your browser to analyze CPU usage:"
    echo "  file://$(pwd)/$OUTPUT"
    echo ""
    echo "Tip: Look for wide bars in the graph - these represent functions"
    echo "     consuming the most CPU time."
else
    echo -e "${RED}✗ Error: Flamegraph generation failed${NC}"
    exit 1
fi
