#!/usr/bin/env bash
set -euo pipefail

# Simple VHS failure recorder - No template version
# Usage: ./record-failure.sh "test-name" "command-that-failed"

if [ $# -ne 2 ]; then
    echo "Usage: $0 <test-name> <command>"
    echo "Example: $0 'help-test' './scripts/run-cli help'"
    exit 1
fi

TEST_NAME="$1"
COMMAND="$2"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

# Check if VHS is available
if ! command -v vhs &> /dev/null; then
    echo "⚠️  VHS not found - skipping failure recording"
    echo "Install with: go install github.com/charmbracelet/vhs@latest"
    return 0 2>/dev/null || exit 0  # Return if sourced, exit if executed
fi

# Create directories
mkdir -p "$SCRIPT_DIR/failure-gifs"
mkdir -p "$SCRIPT_DIR/tapes"

# Generate timestamp and sanitize test name
TIMESTAMP=$(date +"%Y%m%d-%H%M%S")
SAFE_TEST_NAME=$(echo "$TEST_NAME" | tr ' ' '-' | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9-]//g')

# Create the tape file directly
TAPE_FILE="$SCRIPT_DIR/tapes/failure-${SAFE_TEST_NAME}-${TIMESTAMP}.tape"
GIF_NAME="${SAFE_TEST_NAME}-failure-${TIMESTAMP}.gif"
GIF_PATH="$SCRIPT_DIR/failure-gifs/${GIF_NAME}"

# Create a simple VHS tape file
cat > "$TAPE_FILE" << EOF
Output "$GIF_PATH"
Set FontSize 14
Set Width 1200
Set Height 600
Set TypingSpeed 50ms
Set Theme "Dracula"

Type "# Test failure recording for: $TEST_NAME"
Enter
Sleep 1s

Type "# Working directory: \$(pwd)"
Enter
Sleep 1s

Type "echo '=== Reproducing failure ==='"
Enter
Sleep 1s

Type "$COMMAND"
Enter
Sleep 3s

Type "echo '=== End of failure recording ==='"
Enter
Sleep 2s
EOF

echo "🎬 Recording failure GIF for ${TEST_NAME}..."

# Change to project root and run VHS
cd "$PROJECT_ROOT"

if timeout 60s vhs "$TAPE_FILE"; then
    echo "✅ Failure GIF generated: failure-gifs/${GIF_NAME}"
    
    # Clean up the temporary tape file
    rm -f "$TAPE_FILE"
    
    # Show the path to the generated GIF
    echo "📁 View at: tests/integration/cli/failure-gifs/${GIF_NAME}"
else
    echo "❌ Failed to generate VHS recording"
    rm -f "$TAPE_FILE"
fi
