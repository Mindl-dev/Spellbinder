#!/bin/bash
export PATH="/usr/local/bin:/opt/homebrew/bin:/opt/X11/bin:$PATH"

DIR="$(cd "$(dirname "$0")/.." && pwd)"
GAME_DIR="$DIR/Resources/game"
SERVERS_FILE="$DIR/Resources/servers.txt"
VERSION_FILE="$DIR/Resources/version.txt"
CX="/Applications/CrossOver.app/Contents/SharedSupport/CrossOver"
GITHUB_API="https://api.github.com/repos/Mindl-dev/Spellbinder/releases/latest"

# ================================================================
# Auto-update check
# ================================================================
check_update() {
    LOCAL_VERSION="0.3.0"
    [ -f "$VERSION_FILE" ] && LOCAL_VERSION=$(cat "$VERSION_FILE" | tr -d '[:space:]')
    LOCAL_CLEAN="${LOCAL_VERSION#v}"

    # Fetch latest release (fail silently)
    RELEASE_JSON=$(curl -sf -H "User-Agent: SpellBinder-Launcher" "$GITHUB_API" 2>/dev/null) || return
    REMOTE_TAG=$(python3 -c "
import json,sys
try:
    d=json.loads(sys.stdin.read())
    print(d.get('tag_name',''))
except: pass
" <<< "$RELEASE_JSON" 2>/dev/null)
    [ -z "$REMOTE_TAG" ] && return
    REMOTE_CLEAN="${REMOTE_TAG#v}"

    [ "$LOCAL_CLEAN" = "$REMOTE_CLEAN" ] && return

    # Find mac zip URL
    DOWNLOAD_URL=$(python3 -c "
import json,sys
try:
    d=json.loads(sys.stdin.read())
    for a in d.get('assets',[]):
        if 'mac' in a['name'].lower():
            print(a['browser_download_url'])
            break
except: pass
" <<< "$RELEASE_JSON" 2>/dev/null)
    [ -z "$DOWNLOAD_URL" ] && return

    # Prompt user
    CHOICE=$(osascript -e "display alert \"Update Available\" message \"${LOCAL_CLEAN} -> ${REMOTE_CLEAN}\n\nDownload and install?\" buttons {\"Skip\", \"Update\"} default button \"Update\"" 2>/dev/null)
    echo "$CHOICE" | grep -q "Skip" && return

    # Backup user config
    [ -f "$GAME_DIR/main.dat" ] && cp "$GAME_DIR/main.dat" "$GAME_DIR/main.dat.bak"
    [ -f "$GAME_DIR/user.dat" ] && cp "$GAME_DIR/user.dat" "$GAME_DIR/user.dat.bak"
    [ -f "$GAME_DIR/keyboard.dat" ] && cp "$GAME_DIR/keyboard.dat" "$GAME_DIR/keyboard.dat.bak"
    [ -f "$SERVERS_FILE" ] && cp "$SERVERS_FILE" "$SERVERS_FILE.bak"

    # Download and extract
    TEMP_ZIP="/tmp/SpellBinder-mac-update.zip"
    TEMP_DIR="/tmp/SpellBinder-mac-update"
    curl -sL -o "$TEMP_ZIP" "$DOWNLOAD_URL" || { osascript -e 'display alert "Update Failed" message "Download failed."'; return; }

    rm -rf "$TEMP_DIR"
    unzip -qo "$TEMP_ZIP" -d "$TEMP_DIR" || { osascript -e 'display alert "Update Failed" message "Extract failed."'; return; }

    # Find extracted .app
    EXTRACTED_APP=$(find "$TEMP_DIR" -maxdepth 2 -name "*.app" -type d | head -1)
    if [ -z "$EXTRACTED_APP" ]; then
        osascript -e 'display alert "Update Failed" message "Could not find app in update."'
        return
    fi

    # Copy new files over current app (preserve the bundle location)
    cp -rf "$EXTRACTED_APP/Contents/MacOS/"* "$DIR/MacOS/" 2>/dev/null
    cp -rf "$EXTRACTED_APP/Contents/Resources/game/"* "$GAME_DIR/" 2>/dev/null
    [ -f "$EXTRACTED_APP/Contents/Resources/servers.txt" ] && cp "$EXTRACTED_APP/Contents/Resources/servers.txt" "$SERVERS_FILE.new"
    cp "$EXTRACTED_APP/Contents/Info.plist" "$DIR/Info.plist" 2>/dev/null

    # Restore user config
    [ -f "$GAME_DIR/main.dat.bak" ] && mv "$GAME_DIR/main.dat.bak" "$GAME_DIR/main.dat"
    [ -f "$GAME_DIR/user.dat.bak" ] && mv "$GAME_DIR/user.dat.bak" "$GAME_DIR/user.dat"
    [ -f "$GAME_DIR/keyboard.dat.bak" ] && mv "$GAME_DIR/keyboard.dat.bak" "$GAME_DIR/keyboard.dat"
    [ -f "$SERVERS_FILE.bak" ] && mv "$SERVERS_FILE.bak" "$SERVERS_FILE"

    # Write version
    echo "$REMOTE_TAG" > "$VERSION_FILE"

    # Cleanup
    rm -rf "$TEMP_ZIP" "$TEMP_DIR"

    osascript -e 'display alert "Update Complete" message "SpellBinder has been updated. Restarting." buttons {"OK"} default button "OK"'
    exec "$DIR/MacOS/launch.sh"
    exit 0
}

check_update

# Server picker
declare -a SERVER_NAMES
declare -a SERVER_ADDRS
while IFS="|" read -r name addr; do
    [[ "$name" =~ ^#.*$ || -z "$name" ]] && continue
    SERVER_NAMES+=("$name")
    SERVER_ADDRS+=("$addr")
done < "$SERVERS_FILE"

[ ${#SERVER_NAMES[@]} -eq 0 ] && exit 1

AS_LIST=""
for name in "${SERVER_NAMES[@]}"; do
    [ -n "$AS_LIST" ] && AS_LIST="$AS_LIST, "
    AS_LIST="$AS_LIST\"$name\""
done

CHOICE=$(osascript -e "choose from list {${AS_LIST}, \"--- Create Account ---\"} with title \"SpellBinder\" with prompt \"Select a server or create an account:\" default items {\"${SERVER_NAMES[0]}\"}" 2>/dev/null)
[ "$CHOICE" = "false" ] || [ -z "$CHOICE" ] && exit 0

# Handle account creation
if [ "$CHOICE" = "--- Create Account ---" ]; then
    # Pick server first
    SERVER_CHOICE=$(osascript -e "choose from list {${AS_LIST}} with title \"SpellBinder\" with prompt \"Which server to register on?\" default items {\"${SERVER_NAMES[0]}\"}" 2>/dev/null)
    [ "$SERVER_CHOICE" = "false" ] || [ -z "$SERVER_CHOICE" ] && exit 0

    REG_ADDR=""
    for i in "${!SERVER_NAMES[@]}"; do
        [ "${SERVER_NAMES[$i]}" = "$SERVER_CHOICE" ] && REG_ADDR="${SERVER_ADDRS[$i]}" && break
    done

    USERNAME=$(osascript -e 'text returned of (display dialog "Username (3-20 characters):" default answer "" with title "Create Account")' 2>/dev/null)
    [ -z "$USERNAME" ] && exit 0

    PASSWORD=$(osascript -e 'text returned of (display dialog "Password:" default answer "" with title "Create Account" with hidden answer)' 2>/dev/null)
    [ -z "$PASSWORD" ] && exit 0

    CONFIRM=$(osascript -e 'text returned of (display dialog "Confirm password:" default answer "" with title "Create Account" with hidden answer)' 2>/dev/null)
    [ -z "$CONFIRM" ] && exit 0

    if [ "$PASSWORD" != "$CONFIRM" ]; then
        osascript -e 'display alert "Error" message "Passwords don'\''t match."'
        exec "$0"
        exit 0
    fi

    RESULT=$(curl -s -w "\n%{http_code}" -X POST "http://${REG_ADDR}:10603/api/register" \
        -d "username=$(python3 -c "import urllib.parse; print(urllib.parse.quote('$USERNAME'))")&password=$(python3 -c "import urllib.parse; print(urllib.parse.quote('$PASSWORD'))")" 2>&1)
    HTTP_CODE=$(echo "$RESULT" | tail -1)
    BODY=$(echo "$RESULT" | head -1)

    if [ "$HTTP_CODE" = "201" ]; then
        osascript -e 'display alert "Account Created" message "You can now log in as '"$USERNAME"'." buttons {"Play Now"} default button "Play Now"'
        # Continue to server selection
        CHOICE="$SERVER_CHOICE"
    else
        ERROR=$(echo "$BODY" | python3 -c "import sys,json; print(json.load(sys.stdin).get('error','Unknown error'))" 2>/dev/null || echo "$BODY")
        osascript -e "display alert \"Registration Failed\" message \"$ERROR\""
        exec "$0"
        exit 0
    fi
fi

ADDRESS=""
for i in "${!SERVER_NAMES[@]}"; do
    [ "${SERVER_NAMES[$i]}" = "$CHOICE" ] && ADDRESS="${SERVER_ADDRS[$i]}" && break
done
[ -z "$ADDRESS" ] && exit 1

# Resolve DNS to IP (main.dat doesn't support hostnames)
RESOLVED_IP=$(python3 -c "import socket; print(socket.gethostbyname('$ADDRESS'))" 2>/dev/null || echo "$ADDRESS")
[ -f "$GAME_DIR/main.dat" ] && sed -i "" "s/^address=.*/address=${RESOLVED_IP}/" "$GAME_DIR/main.dat"

# Try CrossOver first (best rendering)
if [ -d "$CX" ]; then
    # Find a bottle created through CrossOver GUI
    BOTTLE_NAME=""
    for name in spellbinder 98; do
        BOTTLE_DIR="$HOME/Library/Application Support/CrossOver/Bottles/$name"
        if [ -d "$BOTTLE_DIR" ]; then
            BOTTLE_NAME="$name"
            break
        fi
    done

    if [ -z "$BOTTLE_NAME" ]; then
        osascript -e 'display alert "Bottle Required" message "Open CrossOver and create a bottle first:\n\n1. Bottle menu → New Bottle\n2. Name: spellbinder\n3. Template: Windows XP\n4. Click Create\n\nThen run SpellBinder.app again." buttons {"Open CrossOver", "Cancel"} default button "Open CrossOver"'
        CLICKED=$?
        [ "$CLICKED" -eq 0 ] && open -a CrossOver
        exit 0
    fi

    # Symlink game into bottle
    DRIVE_C="$BOTTLE_DIR/drive_c/game"
    [ ! -e "$DRIVE_C" ] && ln -sf "$GAME_DIR" "$DRIVE_C"

    # Open CrossOver and tell user to use Run Command
    open -a CrossOver
    osascript -e 'display alert "SpellBinder" message "Server set to '"$ADDRESS"'.\n\nIn CrossOver:\n1. Select bottle \"'"$BOTTLE_NAME"'\"\n2. Run Command → C:\\game\\game.exe\n3. Click Run" buttons {"OK"} default button "OK"'
    exit 0
fi

# Fallback: stock Wine (walls may be invisible)
if ! command -v wine &>/dev/null; then
    osascript -e 'display alert "Wine Required" message "Install CrossOver (recommended) or Wine via Homebrew.\n\nbrew tap gcenx/wine && brew install --cask wine-crossover" buttons {"OK"} default button "OK"'
    exit 1
fi

osascript -e 'display notification "Using stock Wine — some walls may be invisible. Install CrossOver for best rendering." with title "SpellBinder"'

if ! pgrep -x Xquartz >/dev/null 2>&1; then
    open -a XQuartz
    sleep 2
fi
export DISPLAY=:0
export WINEPREFIX="$DIR/Resources/wineprefix"

if [ ! -d "$WINEPREFIX" ]; then
    wine wineboot --init 2>/dev/null || true
fi

cd "$GAME_DIR"
wine game.exe 2>"$DIR/Resources/wine.log"
