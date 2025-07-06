#!/usr/bin/env bash
set -euo pipefail

# VHS-based integration test runner
# Usage: ./run-integration-tests.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}🎬 Darklang CLI Integration Tests (VHS-powered)${NC}"
echo "=============================================="

# Check if VHS is installed
if ! command -v vhs &> /dev/null; then
    echo -e "${RED}❌ VHS is not installed${NC}"
    echo "Install with: go install github.com/charmbracelet/vhs@latest"
    echo "Or visit: https://github.com/charmbracelet/vhs"
    exit 1
fi

# Track test results
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Function to run a test
run_test() {
    local test_name="$1"
    local test_script="$2"
    local command_to_record="${3:-}"  # Optional: command to record on failure
    
    echo ""
    echo -e "${BLUE}🧪 Running: $test_name${NC}"
    echo "---"
    
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    
    if "$test_script"; then
        PASSED_TESTS=$((PASSED_TESTS + 1))
        echo -e "${GREEN}✓ $test_name PASSED${NC}"
    else
        FAILED_TESTS=$((FAILED_TESTS + 1))
        echo -e "${RED}✗ $test_name FAILED${NC}"
        
        # Record failure with VHS if command provided
        if [[ -n "${command_to_record:-}" ]]; then
            echo -e "${YELLOW}🎬 Recording failure GIF...${NC}"
            "$SCRIPT_DIR/record-failure.sh" "$test_name" "$command_to_record" || echo -e "${YELLOW}⚠️  Failed to record GIF${NC}"
        fi
    fi
}

# Run all tests
run_test "Help Command" "$SCRIPT_DIR/run-help-test.sh" "./scripts/run-cli help"

# Add more tests here later:
# run_test "Version Command" "$SCRIPT_DIR/run-version-test.sh" "./scripts/run-cli version"

# Final results
echo ""
echo -e "${BLUE}📊 Test Results Summary${NC}"
echo "======================"
echo -e "Total tests:  $TOTAL_TESTS"
echo -e "${GREEN}Passed:       $PASSED_TESTS${NC}"
echo -e "${RED}Failed:       $FAILED_TESTS${NC}"

if [[ $FAILED_TESTS -eq 0 ]]; then
    echo -e "${GREEN}🎉 All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}💥 Some tests failed!${NC}"
    echo -e "${YELLOW}📁 Check failure recordings in tests/integration/cli/failure-gifs/${NC}"
    exit 1
fi
