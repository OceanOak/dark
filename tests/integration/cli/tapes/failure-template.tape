# VHS Tape for recording test failures
# This template gets copied and customized for each failed test

Output "__SCRIPT_DIR__/failure-gifs/__GIFNAME__"

Set FontSize 14
Set Width 1200
Set Height 600
Set TypingSpeed 50ms
Set Theme "Dracula"

# Show what directory we're in
Type "# Test failure recording for: __TESTNAME__"
Enter
Sleep 1s

Type "# Working directory: $(pwd)"
Enter
Sleep 1s

Type "echo '=== Reproducing failure ==='"
Enter
Sleep 1s

# Run the failing command
Type "__COMMAND__"
Enter
Sleep 3s

Type "echo '=== End of failure recording ==='"
Enter
Sleep 2s
