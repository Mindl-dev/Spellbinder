# Binary Patches

Documented patches for the SpellBinder game client. These enable debug logging
and other development features without distributing copyrighted binaries.

## Usage

```bash
# Auto-detect version from MD5 and apply all patches
python3 apply_patches.py /path/to/game.dll.clean

# Specify patch file and output
python3 apply_patches.py game.dll.clean demo_v070.json --output game.dll

# List patches without applying
python3 apply_patches.py game.dll.clean --list
```

The patcher verifies MD5 checksums before and after to ensure the correct
binary is being patched and the result matches expectations.

## Patch Files

| File | Target | Clean MD5 |
|------|--------|-----------|
| `demo_v070.json` | Demo v0.70 (from spell70.exe) | `21396bfb2687...` |

## Current Patches (Demo v0.70)

### debug_flag_startup
Sets `verbose_debug_flag = 2` at WinMain startup via a code cave in a NOP sled.
Enables the game's built-in debug logging from launch — shows packet info,
arena state, damage calculations, etc. in the debug output.

### debug_flag_cave
The code cave itself: `mov dword ptr [0x68AFCC], 2; jmp sub_453980`.
Located in an existing 15-byte NOP sled at 0x406DF1 (.text section).

### debug_off_keeps_debug
Prevents the `/debug off` chat command from disabling debug mode — changes
the immediate value from 0 to 2 so the flag stays at "full debug" regardless.

## Adding Patches for New Versions

1. Get the clean binary's MD5: `md5sum game.dll`
2. Create a new JSON file (e.g., `full_v101.json`)
3. Map virtual addresses to file offsets using the PE section table
4. Document original and patched bytes in hex
5. Test with `apply_patches.py`
