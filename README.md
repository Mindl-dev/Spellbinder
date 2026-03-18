# SpellBinder Community Server & Client

Fork of [Magestorm/Magestorm](https://github.com/Magestorm/Magestorm), adapted for SpellBinder: The Nexus Conflict (1999).

Includes a private server (C#/.NET 4.8), binary patcher, and portable game client builds for Windows and macOS.

## Quick Start — Play the Game

### 1. Get the game installer
Download `spelinst.exe` from the [Internet Archive](https://archive.org/details/spelinst) and place it in the repo root.

### 2. Extract and patch
```bash
python build_content.py spelinst.exe
```
This extracts the game, patches `game.dll` for community server compatibility, and patches `spell.bin` with balance fixes. Everything goes into `Content/`.

### 3. Build the client

**Windows:**
```powershell
.\client\build_win.ps1 -Release
# Output: SpellBinder-win.zip — extract, double-click Play.exe
```

**macOS:**
```bash
./client/build_mac.sh --release
# Output: SpellBinder-mac.zip — extract, double-click SpellBinder.app
# See docs/mac-setup.md for CrossOver setup (recommended for best rendering)
```

### 4. Play
Extract the zip, double-click the launcher, pick a server, done.

## Quick Start — Run a Server

### Docker
```bash
docker build -t spellbinder .
docker run -d --name spellbinder --network host \
  -v ./Content:/app/Content -v spellbinder-data:/var/lib/mysql \
  -v ./Logs:/app/Logs spellbinder
```
### Windows
```powershell
.\setup.ps1
.\server.ps1          # kill/build/test/start
```

Host networking is required so the server sees real client IPs (NAT'd container networking makes all clients appear as the same IP, breaking anti-cheat and IP bans). Ports 10601/udp and 10602/tcp bind directly on the host.

The `Content/` volume mount is required — game data files (Spells.dat, Arenas.dat, Grids/) are copyrighted and not baked into the image. The entrypoint handles MariaDB setup, schema import, file case normalization, and account creation automatically.

On first run, credentials are generated and saved to `/app/credentials.txt`:
```bash
docker exec spellbinder cat /app/credentials.txt
```

> **`--dev` flag**: `docker run ... spellbinder --dev` creates accounts with simple passwords (password = username). Do not use in production.

## Project Structure

```
client/                     # Client build tooling
  Play.cs                   # Windows launcher (C# WinForms)
  build_win.ps1             # Build Windows client zip
  build_mac.sh              # Build macOS .app bundle
  launch_mac.sh             # macOS runtime launcher (CrossOver/Wine)
  defaults/keyboard.dat     # Default keybinds
  dgVoodoo.conf             # Windows display compatibility config

patches/                    # Binary patches (no copyrighted code)
  apply_patches.py          # Generic binary patcher with MD5 verification
  full_v202_discord.json    # game.dll UDP fix (1 byte: JNZ→JZ)
  spellbin_discord.json     # spell.bin balance patch (debug spell)

tools/                      # Extraction and RE tools
  extract_game.py           # Extract game files from spelinst.exe

docs/                       # Setup guides
  mac-setup.md              # macOS client setup (CrossOver + Wine)

SpellServer/                # C# game server (.NET Framework 4.8)
SpellServer.Tests/          # NUnit tests
Helper/                     # Shared library (grid, timing, networking)
Content/                    # Extracted game data (not committed)
Build/Debug/                # Compiled server output
build_content.py            # Extract + patch game content
server.ps1                  # Kill/build/test/start server
```

### Updates
```bash
./rebuild.sh
```

## Dev Workflow

### Server iteration
```powershell
# Edit code in SpellServer/
.\server.ps1                # kills, builds, tests, starts (headless)
.\server.ps1 -SkipTests     # skip NUnit tests
```

### Client iteration
```powershell
# Edit client/Play.cs
.\client\build_win.ps1      # compile + assemble
.\client\build_win.ps1 -Server "1.2.3.4"    # custom server address
.\client\build_win.ps1 -Release              # + create zip
```

### Debug flags
```powershell
# Append to SpellServer.exe launch:
SpellServer.exe --headless --debug=ProjectileTracking
SpellServer.exe --headless --debug=ProjectileTracking,PlayerTracking,RuneTracking
# Valid: None, ProjectileTracking, PlayerTracking, RuneTracking, ThinTracking, OneDamageToPlayers
```

## Server Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| DatabaseName | magestorm | Must match SQL database name |
| ServerVersion | 2.0.2 | Must match client version |
| ListenPort | 10602 | TCP game port |
| UDPPort | 10601 | UDP game port |

## Prerequisites

**Server:**
- Windows: [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) + [VS Build Tools 2022](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022) + MySQL 8.0
- Linux: `mono-devel` + MariaDB
- Docker: just works

**Client build:**
- Windows: .NET Framework 4.8 (ships with Windows 10/11) + Python 3
- macOS: Python 3 + CrossOver (recommended) or Wine via Homebrew
