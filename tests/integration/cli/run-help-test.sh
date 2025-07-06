#!/usr/bin/env bash
set -euo pipefail

# CLI integration test runner for help command (VHS-free)
# Usage: ./run-help-test.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
TEST_DIR="$SCRIPT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🧪 CLI Help Integration Test${NC}"
echo "============================"

# Expected and actual output files
EXPECTED_FILE="$TEST_DIR/expected/help.txt"
ACTUAL_FILE="$TEST_DIR/actual/help.txt"

# Create directories
mkdir -p "$TEST_DIR/actual"
mkdir -p "$TEST_DIR/expected"

# Change to project root for execution
cd "$PROJECT_ROOT"

echo -e "🔧 Running CLI help command..."

# Run CLI and capture output
if timeout 30s ./scripts/run-cli help > "$ACTUAL_FILE" 2>&1; then
    echo -e "${GREEN}✓${NC} CLI help command executed successfully"
else
    EXIT_CODE=$?
    echo -e "${RED}✗${NC} CLI help command failed (exit code: $EXIT_CODE)"
    echo "CLI output:"
    cat "$ACTUAL_FILE" 2>/dev/null || echo "No output captured"
    exit 1
fi

# Verify actual output has content
if [[ ! -s "$ACTUAL_FILE" ]]; then
    echo -e "${RED}✗${NC} Output file is empty."
    exit 1
fi

echo -e "${GREEN}✓${NC} Captured $(wc -l < "$ACTUAL_FILE") lines of output"

# Check if expected file exists
if [[ ! -f "$EXPECTED_FILE" ]]; then
    echo -e "${YELLOW}⚠${NC}  Expected output file not found: $EXPECTED_FILE"
    echo -e "${YELLOW}ℹ${NC}  Creating it with current output for future comparisons"
    cp "$ACTUAL_FILE" "$EXPECTED_FILE"
    echo -e "${GREEN}✓${NC} Test setup complete. Expected output saved."
    echo ""
    echo "Preview of captured help output:"
    echo "================================"
    head -10 "$EXPECTED_FILE"
    echo "... ($(wc -l < "$EXPECTED_FILE") total lines)"
    exit 0
fi

# Compare actual vs expected
echo "🔍 Comparing CLI output vs expected..."

if diff -q "$EXPECTED_FILE" "$ACTUAL_FILE" > /dev/null; then
    echo -e "${GREEN}✅ PASS${NC} - Help command output matches expected"
    rm -f "$ACTUAL_FILE"  # Clean up on success
    exit 0
else
    echo -e "${RED}❌ FAIL${NC} - Help command output differs from expected"
    echo ""
    echo -e "${BLUE}📊 Detailed diff:${NC}"
    echo "=================="
    echo -e "${YELLOW}Expected vs Actual:${NC}"
    diff --unified=3 --color=always "$EXPECTED_FILE" "$ACTUAL_FILE" || true
    echo ""
    echo -e "${YELLOW}Files:${NC}"
    echo "  Expected: $EXPECTED_FILE"
    echo "  Actual:   $ACTUAL_FILE"
    echo ""
    echo -e "${YELLOW}💡 Tip:${NC} If the new output is correct, update expected with:"
    echo "  cp \"$ACTUAL_FILE\" \"$EXPECTED_FILE\""
    exit 1
fi
