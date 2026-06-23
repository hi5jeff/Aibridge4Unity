#!/usr/bin/env bash
# Publish: sync the package from the dev Unity project into this repo and push.
# Usage: scripts/publish.sh "commit message"
set -euo pipefail

REPO_DIR="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$REPO_DIR/../testgame/Packages/com.aibridge.unity"
DST="$REPO_DIR/com.aibridge.unity"

if [ ! -d "$SRC" ]; then
  echo "Source package not found: $SRC" >&2
  exit 1
fi

echo "Syncing $SRC -> $DST"
rm -rf "$DST"
cp -r "$SRC" "$DST"

cd "$REPO_DIR"
git add -A
if git diff --cached --quiet; then
  echo "No changes to publish."
  exit 0
fi
git commit -m "${1:-sync: update package from dev project}"
git push
echo "Published."
