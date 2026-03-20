#!/bin/bash
# build_mac_remote.sh — Build Mac client on a remote Mac via SSH
# Run from the community_server/ directory on Windows
# Usage: ./client/build_mac_remote.sh [user@host] [--release]
#
# Syncs the repo to the Mac, runs build_mac.sh, copies the result back.
# Requires: ssh access to the Mac, clang on the Mac

set -e

# Load .env from repo root if it exists
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$REPO_DIR/.env"
[ -f "$ENV_FILE" ] && export $(grep -v '^#' "$ENV_FILE" | xargs)

RELEASE=""
REMOTE=""
for arg in "$@"; do
    case "$arg" in
        --release) RELEASE="--release" ;;
        *@*) REMOTE="$arg" ;;
    esac
done

# Fall back to .env, then error
if [ -z "$REMOTE" ]; then
    REMOTE="${MAC_BUILD_HOST:-}"
fi
if [ -z "$REMOTE" ]; then
    echo "ERROR: No Mac build host specified."
    echo "Usage: ./client/build_mac_remote.sh user@host [--release]"
    echo "Or set MAC_BUILD_HOST in .env"
    exit 1
fi

REMOTE_DIR="~/spellbinder-build"

echo "=== Syncing to $REMOTE:$REMOTE_DIR ==="
ssh "$REMOTE" "mkdir -p $REMOTE_DIR/client/defaults $REMOTE_DIR/client/dgvoodoo $REMOTE_DIR/patches $REMOTE_DIR/tools"

# Sync client tooling
scp "$SCRIPT_DIR/build_mac.sh" "$REMOTE:$REMOTE_DIR/client/"
scp "$SCRIPT_DIR/launch_mac.sh" "$REMOTE:$REMOTE_DIR/client/"
scp "$SCRIPT_DIR/dgVoodoo.conf" "$REMOTE:$REMOTE_DIR/client/" 2>/dev/null || true
scp "$SCRIPT_DIR/defaults/"* "$REMOTE:$REMOTE_DIR/client/defaults/" 2>/dev/null || true

# Sync GameFiles (the big one)
echo "=== Syncing game files ==="
rsync -az --delete "$REPO_DIR/GameFiles/" "$REMOTE:$REMOTE_DIR/GameFiles/" 2>/dev/null || \
    scp -r "$REPO_DIR/GameFiles/"* "$REMOTE:$REMOTE_DIR/GameFiles/"

echo "=== Building on $REMOTE ==="
ssh "$REMOTE" "cd $REMOTE_DIR && chmod +x client/build_mac.sh && REPO_ROOT=$REMOTE_DIR ./client/build_mac.sh $RELEASE"

if [ -n "$RELEASE" ]; then
    echo "=== Copying SpellBinder-mac.zip back ==="
    scp "$REMOTE:$REMOTE_DIR/SpellBinder-mac.zip" "$REPO_DIR/SpellBinder-mac.zip"
    echo "Done: $REPO_DIR/SpellBinder-mac.zip"
else
    echo "=== Copying SpellBinder.app back ==="
    scp -r "$REMOTE:$REMOTE_DIR/SpellBinder.app" "$REPO_DIR/SpellBinder-mac/"
    echo "Done: $REPO_DIR/SpellBinder-mac/SpellBinder.app"
fi
