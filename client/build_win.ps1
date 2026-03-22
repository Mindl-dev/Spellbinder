# build_win.ps1 — Full pipeline: extract, patch, compile launcher, assemble client
# Usage: .\client\build_win.ps1 [-Version "v0.4.1"] [-Server "ip"] [-Release]
# Requires: spelinst.exe in repo root, Python 3
param(
    [string]$Version,
    [string]$Server,
    [switch]$Release
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$ContentDir = "$RepoRoot\GameFiles"
$OutDir = "$RepoRoot\SpellBinder-win"
$ZipName = "$RepoRoot\SpellBinder-win.zip"
$Installer = "$RepoRoot\spelinst.exe"
$PatchDir = "$RepoRoot\patches"

# --- Step 1: Extract game files from installer ---
Write-Host "=== Extracting game files ==="
if (-not (Test-Path $Installer)) {
    Write-Error "spelinst.exe not found in $RepoRoot. Download from the Internet Archive."
    exit 1
}
python "$RepoRoot\build_content.py" $Installer
if ($LASTEXITCODE -ne 0) { Write-Error "Extraction failed"; exit 1 }

# --- Step 2: Apply binary patches ---
Write-Host ""
Write-Host "=== Applying patches ==="

# Tick rate: 500ms -> 16ms (60hz position updates)
Write-Host "Applying tick rate patch..."
python "$PatchDir\apply_patches.py" "$ContentDir\game.dll" "$PatchDir\tickrate_60hz.json" --output "$ContentDir\game.dll"
if ($LASTEXITCODE -ne 0) { Write-Error "Tick rate patch failed"; exit 1 }

# Interpolation: 30ms -> 100ms window
Write-Host "Applying interpolation patch..."
python "$PatchDir\apply_patches.py" "$ContentDir\game.dll" "$PatchDir\interp_100ms.json" --output "$ContentDir\game.dll"
if ($LASTEXITCODE -ne 0) { Write-Error "Interpolation patch failed"; exit 1 }

# --- Step 3: Compile Play.exe ---
Write-Host ""
Write-Host "=== Compiling Play.exe ==="

# Stamp version into Play.cs before compiling
$versionClean = $Version.TrimStart("v")
$playSrc = "$ScriptDir\Play.cs"
(Get-Content $playSrc -Raw) -replace 'private const string VERSION = "[^"]*"', "private const string VERSION = `"$versionClean`"" | Set-Content $playSrc -NoNewline
Write-Host "Stamped VERSION = $versionClean into Play.cs"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    Write-Error "csc.exe not found. Install .NET Framework 4.8."
}

Push-Location $ScriptDir
& $csc /target:winexe /out:Play.exe /win32icon:spellbinder.ico `
    /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Net.Http.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll `
    Play.cs
if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Error "Compile failed"; exit 1 }
Pop-Location

# --- Step 4: Assemble ---
Write-Host ""
Write-Host "=== Assembling $OutDir ==="
if (Test-Path $OutDir) { Remove-Item -Recurse -Force $OutDir }
New-Item -ItemType Directory -Path "$OutDir\game" | Out-Null

# Play.exe at root
Copy-Item "$ScriptDir\Play.exe" "$OutDir\Play.exe"

# Game files into game/ (skip installer/RE artifacts)
$skip = @("manifest.json", "spells.json", "spells_summary.txt", "UNWISE.EXE",
          "spell.exe", "game.dll.orig", "game.dll.clean", "game.dll.pre-debug")
Get-ChildItem $ContentDir | Where-Object { $skip -notcontains $_.Name } | ForEach-Object {
    Copy-Item -Recurse $_.FullName "$OutDir\game\$($_.Name)"
}

# dgVoodoo DLLs + config
if (Test-Path "$ScriptDir\dgvoodoo") {
    Get-ChildItem "$ScriptDir\dgvoodoo" | ForEach-Object {
        Copy-Item $_.FullName "$OutDir\game\$($_.Name)" -Force
    }
}
if (Test-Path "$ScriptDir\dgVoodoo.conf") {
    Copy-Item "$ScriptDir\dgVoodoo.conf" "$OutDir\game\dgVoodoo.conf" -Force
}

# Default keybinds
if (Test-Path "$ScriptDir\defaults") {
    Get-ChildItem "$ScriptDir\defaults" | ForEach-Object {
        Copy-Item $_.FullName "$OutDir\game\$($_.Name)" -Force
    }
}

# Set server address
if ($Server) {
    Write-Host "Setting server to $Server"
    $mainDat = "$OutDir\game\main.dat"
    if (Test-Path $mainDat) {
        (Get-Content $mainDat -Raw) -replace "address=.*", "address=$Server" | Set-Content $mainDat -NoNewline
    }
}

# Write version.txt — -Version is always required
if (-not $Version) {
    Write-Error "Version required: .\client\build_win.ps1 -Version v0.4.1 [-Release]"
    exit 1
}
$gitTag = $Version
Set-Content "$OutDir\version.txt" $gitTag.Trim()
Write-Host "Version: $gitTag"

Write-Host ""
Write-Host "=== Built $OutDir ==="
Write-Host "  Play.exe   (double-click to play)"
Write-Host "  game\      (game files + dgVoodoo)"

if ($Release) {
    Write-Host ""
    Write-Host "=== Creating zip ==="
    if (Test-Path $ZipName) { Remove-Item $ZipName }
    Compress-Archive -Path $OutDir -DestinationPath $ZipName
    $size = [math]::Round((Get-Item $ZipName).Length / 1MB, 1)
    Write-Host "Created $ZipName ($size MB)"
}
