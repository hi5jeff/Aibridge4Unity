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

# Tag the release v<version> (from package.json) so users can pin/update predictably.
VERSION="$(sed -nE 's/.*"version": "([^"]+)".*/\1/p' "$DST/package.json" | head -1)"
if [ -n "$VERSION" ]; then
  TAG="v$VERSION"
  if git rev-parse "$TAG" >/dev/null 2>&1; then
    echo "Tag $TAG already exists — skipping (bump version in package.json to cut a new release)."
  else
    git tag -a "$TAG" -m "release $TAG"
    git push origin "$TAG"
    echo "Tagged $TAG"
  fi
fi
echo "Published."
