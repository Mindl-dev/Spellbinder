# Client Position Update Rate

## How it works

The client sends position updates from the main game loop in `MainMatchLoop` (0x4474DD).

### The timer

```c
// MainMatchLoop @ 0x4481D5
dword_5992E0 += g_TickDelta / -10;   // countdown in milliseconds
if ( dword_5992E0 < 0 )
{
    dword_5992E0 = 500;              // reset to 500ms
    sub_428AB0(a1);                  // → position send decision
}
```

- `g_TickDelta` is in 0.1ms units (10kHz timer from `GetCurrentTimeStamp`)
- Divided by -10 converts to milliseconds countdown
- **Default rate: 500ms (2 updates/second)**
- Patch address: **`0x4481EA`** — `mov dword_5992E0, 1F4h`

### The send decision (sub_428AB0 @ 0x428AB0)

Called every 500ms. Compares current position against last-sent:

```
if (position changed)     → S_PlayerMoveState (12 bytes, opcode 0x01)
else if (heading changed) → S_PlayerMoveShortState (8 bytes, opcode 0x12)
else if (counter == 4)    → force full send anyway (keepalive)
else                      → skip
```

Last-sent state stored at:
- `dword_631E00` — heading
- `dword_631E04` — X
- `dword_631E08` — Y
- `dword_631E0C` — Z
- `dword_631E10` — flags

8-frame counter at `dword_631FE8`, wraps at 8, forces full send at 4.

### Event-based sends (immediate, bypass the timer)

These call `S_PlayerMoveState` directly, independent of the 500ms timer:
- Jump landing (0x443368, 0x443436) — has its own 7.5s debounce
- Teleporter trigger (0x456FD7)
- Ghost teleport (0x45403B)
- Jump/yank received (0x42C558, 0x42C666)
- Shrine raise (0x43FA10, 0x43FB70)

### Other periodic timers in the main loop

```c
// Integrity check — every 5 seconds
dword_5992E8 += g_TickDelta / -10;
if ( dword_5992E8 < 0 )
{
    dword_5992E8 = 5000;             // 5 seconds
    sub_430EE0();                     // integrity check
    TransmitIntegrityCheck(a1);
}

// Status effects + UI — every 250ms
if ( dword_6BF7E0 >= 250 )
{
    UpdatePlayerStatusEffects();
    DisplayPlayerStatusMessage();
    UpdatePingDisplay();
}
```

## Timestamp units

`GetCurrentTimeStamp()` returns 0.1ms ticks (100-microsecond resolution). Confirmed by:

```c
// NIF loader timing
dword_6BE990 = GetCurrentTimeStamp() - start;
fprintf(log, "Time to NDLLoadGridNIFS: %d msecs\n", dword_6BE990 / 10);
//                                                              ^^^^
//                                              divide by 10 to get ms
```

## How to patch

### Increase update rate to ~60/sec (16ms)

Patch `0x4481EA`: change `mov dword_5992E0, 1F4h` to `mov dword_5992E0, 10h`

- `0x1F4` = 500 (current, 2/sec)
- `0x10` = 16 (~60/sec)
- `0x21` = 33 (~30/sec)
- `0x64` = 100 (10/sec)

### Server-side consideration

The C# server's speedhack detector in `Arena.PlayerMove` counts states:

```csharp
if (arenaPlayer.StateReceivedCount++ >= 500)
{
    Int64 deltaState = TimeHelper.DeltaMilliseconds(...);
    Int32 minDelta = arenaPlayer.HasFliedSinceHackDetect ? 20000 : 30000;
    // 500 states in < 30 seconds = kick
}
```

At 2/sec: 500 states = 250 seconds — never triggers.
At 60/sec: 500 states = 8.3 seconds — **will trigger speedhack kick**.

Fix: raise the threshold or adjust the time window in `Arena.cs` before patching the client.

## Key addresses

| Address | What |
|---------|------|
| 0x4481EA | `mov dword_5992E0, 1F4h` — position send interval (500ms) |
| 0x4481D5 | Countdown subtract: `add dword_5992E0, tick/-10` |
| 0x4481F4 | Call to `sub_428AB0` (position send decision) |
| 0x428AB0 | Position send decision (full vs short vs skip) |
| 0x42A470 | `S_PlayerMoveState` — builds and sends 12-byte packet |
| 0x437BA0 | `GenerateAndEnqueueTCPPacket` — all outgoing packets funnel here |
| 0x437BA0 | Breakpoint: `BL == func_id` to catch specific opcodes |

## Opcode 0x18 (client→server)

The client periodically sends opcode 0x18 to the server. The C# server **ignores it** (`IGNORED` in dispatch). Purpose unknown — possibly a state acknowledgment or scoreboard echo from the original protocol. Set a conditional breakpoint at `0x437BA0` with `BL == 0x18` to catch it and examine the call stack.
