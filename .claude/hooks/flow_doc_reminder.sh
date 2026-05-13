#!/usr/bin/env bash
# PostToolUse hook: nudges the assistant to keep the *_flow.md docs in sync
# whenever any source file under a tracked sub-project is written/edited.
#
# Reads the tool-call payload on stdin, picks the file path, decides which
# flow file (if any) needs updating, and emits a JSON `additionalContext`
# back to the assistant with an imperative instruction.

set -euo pipefail

ROOT="/Users/ios/Documents/Deepak-projects/practice/true-capture"

file=$(jq -r '.tool_input.file_path // .tool_response.filePath // empty' 2>/dev/null || true)
[ -z "$file" ] && exit 0

# Skip non-source / generated / config paths we never want to track.
# (Note: in bash `case` patterns, `*` matches across `/`.)
case "$file" in
  *_flow.md|*/.claude/*|*/.git/*|*/build/*|*/.dart_tool/*|*/node_modules/*) exit 0 ;;
  *.lock|*.iml|*.DS_Store) exit 0 ;;
  */docs/*|*/Properties/*|*/obj/*|*/bin/*) exit 0 ;;
esac

emit() {
  # $1 = flow file path, $2 = sub-project label, $3 = relative file path
  jq -nc \
    --arg flow "$1" \
    --arg proj "$2" \
    --arg src  "$3" \
    '{
      hookSpecificOutput: {
        hookEventName: "PostToolUse",
        additionalContext: ("Source change detected in " + $proj + " (" + $src + "). Update " + $flow + " NOW to reflect this change before responding to the user. The flow doc must capture: any new module/screen/route/API, the call flow (UI -> ViewModel -> Repository -> API for mobile; Controller -> Service -> DB for backend), DTOs, and dependencies. Re-read the file first, then edit incrementally — do not regenerate from scratch.")
      }
    }'
}

rel="${file#$ROOT/}"

case "$file" in
  "$ROOT"/true_capture_app/lib/*.dart)
    emit "$ROOT/mobile_app_flow.md" "true_capture_app (Flutter)" "$rel" ;;
  "$ROOT"/true_capture_backend/src/*.cs|"$ROOT"/true_capture_backend/src/*appsettings*.json|"$ROOT"/true_capture_backend/src/*Program.cs)
    emit "$ROOT/backend_api_flow.md" "true_capture_backend (.NET)" "$rel" ;;
  "$ROOT"/true_capture_admin_panel/*)
    emit "$ROOT/admin_panel_flow.md" "true_capture_admin_panel (Next.js admin)" "$rel" ;;
  "$ROOT"/true_capture_web/*)
    emit "$ROOT/web_app_flow.md" "true_capture_web" "$rel" ;;
esac

exit 0
