#!/usr/bin/env bash
# Claude Code SubagentStop hook.
#
# Runs the full CloudTek harness (`dotnet tool run cloudtek-build --target All`:
# checks, compile, unit + integration tests, pack) after an agent finishes.
# On failure the compiler/test errors go back to the agent as its next
# instruction (exit 2). Each distinct tree state is built at most once.
set -u

input=$(cat)
root="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel 2>/dev/null)}"
[ -n "$root" ] && cd "$root" || exit 0

lock="$root/.claude/.quick-build.lock"
stamp="$root/.claude/.quick-build.stamp"
log="${TMPDIR:-/tmp}/hive-quick-build.log"

# Fingerprint of the tree: HEAD, tracked changes and untracked files
# (editor/agent scratch folders excluded).
fingerprint() {
  {
    git rev-parse HEAD
    git status --porcelain --untracked-files=all -- . ':!.ai' ':!.junie'
    git diff HEAD
  } 2>/dev/null | shasum -a 256 | cut -d' ' -f1
}
fp=$(fingerprint)

summary_of_log() {
  sed 's/\x1b\[[0-9;]*m//g' "$log" | awk '/Errors & Warnings/ { p = 1 } p' | grep -v '^[[:space:]]*$' | head -60
}

# Same tree already built: do not build again.
if [ -f "$stamp" ]; then
  read -r status last_fp < "$stamp"
  if [ "$last_fp" = "$fp" ]; then
    if [ "$status" = "fail" ]; then
      jq -n --arg m "cloudtek-build --target All is still failing for this tree (log: $log):"$'\n'"$(summary_of_log)" '{systemMessage: $m}'
    fi
    exit 0
  fi
fi

# One harness run at a time; a concurrent agent's run already covers this tree.
mkdir "$lock" 2>/dev/null || exit 0
trap 'rmdir "$lock" 2>/dev/null' EXIT

# The harness's DotnetOutdatedInstall target rewrites the tool manifest.
# Leave the agent's tree exactly as it was when the manifest was clean before.
manifest=.config/dotnet-tools.json
manifest_was_clean=0
git diff --quiet -- "$manifest" 2>/dev/null && manifest_was_clean=1

dotnet tool run cloudtek-build --target All >"$log" 2>&1
rc=$?

[ "$manifest_was_clean" = 1 ] && git checkout --quiet -- "$manifest" 2>/dev/null

if [ "$rc" = 0 ]; then
  echo "ok $(fingerprint)" >"$stamp"
  exit 0
fi
echo "fail $(fingerprint)" >"$stamp"

summary=$(summary_of_log)

# This agent was already sent back once; report instead of blocking again.
if echo "$input" | jq -e '.stop_hook_active == true' >/dev/null 2>&1; then
  jq -n --arg m "cloudtek-build --target All still failing after the agent's retry (log: $log):"$'\n'"$summary" '{systemMessage: $m}'
  exit 0
fi

{
  echo "cloudtek-build --target All failed after your changes. Fix the cause before finishing (full log: $log):"
  echo "$summary"
} >&2
exit 2
