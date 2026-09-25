# gameboy-sharp

A Game Boy (DMG) emulator written in C# for .NET 6. The machine lives in a UI-free core
library that runs and is tested headless; a Raylib desktop frontend makes it playable with
picture, sound, keyboard input and save states, and a small command-line runner drives the
core for scripting and test ROMs.

## Features

Core (`GameBoy.Core`):

- CPU (Sharp LR35902): the full base and CB-prefixed opcode set, M-cycle timing where every
  memory access advances the rest of the machine, all five interrupts, HALT with the HALT
  bug, delayed EI, RETI, minimal STOP. Starts in the post-boot-ROM register state.
- Memory: full DMG map, echo RAM, I/O registers, OAM DMA, and VRAM/OAM locked to the CPU
  during the PPU modes that own them.
- PPU: mode 2/3/0 and VBlank timing per scanline, LCDC/STAT/LY/LYC/SCX/SCY/WX/WY/BGP/OBP0/OBP1,
  background, window with its internal line counter, sprites (8x8 and 8x16, X and OAM
  priority, 10 per line, BG-over-OBJ priority, flips), STAT and LYC interrupts, a
  160x144 framebuffer of 2-bit shades.
- APU: both square channels (CH1 with frequency sweep), the wave RAM channel, the noise
  channel (15-bit and 7-bit LFSR), length counters, volume envelopes, DAC enables, the
  512 Hz frame sequencer, NR50/NR51 stereo mixing, NR52 power with register clearing and
  the documented read masks. Output is 44.1 kHz stereo float samples in a ring buffer.
- Timer: DIV/TIMA/TMA/TAC on the internal 16-bit counter with falling-edge detection and
  the delayed TIMA reload.
- Joypad with interrupt on press, serial port with captured output.
- Cartridges: ROM only, MBC1 (ROM/RAM banking, both banking modes), MBC3 with the real-time
  clock (latching, halt, day counter carry), MBC5 (up to 512 ROM banks, 16 RAM banks).
- Battery saves: cartridge RAM export/import; MBC3 RTC carts append a 48-byte clock footer
  (five current registers, five latched registers, 64-bit UNIX timestamp) and the clock is
  advanced by the real time that passed while the game was closed.
- Save states: the full machine (CPU, PPU, APU, timer, DMA, mapper, RTC, RAM) serialized
  to a binary snapshot with a format header; restoring continues cycle-identically.

Desktop frontend (`GameBoy.Desktop`, Raylib-cs):

- Window with the LCD at an integer scale (1-12, default 3) and nearest-neighbour filtering.
- Palettes: `green` (default), `gray`, or any four colors given as hex.
- Audio streamed from the APU with bounded latency.
- Frame pacing at the DMG refresh rate (4194304 / 70224 = 59.73 Hz), independent of the
  monitor; after a stall it drops frames instead of fast-playing the backlog.
- Pause, hold-to-fast-forward (2x-16x, default 4x, muted), quick save/load state with an
  on-screen confirmation, battery RAM written on exit and loaded on start.
- Every key can be rebound from the command line or a config file.

## Controls

| Action | Default key |
|---|---|
| D-pad | Arrow keys |
| A / B | X / Z |
| Start / Select | Enter / Right Shift |
| Save state / Load state | F5 / F8 |
| Pause | P |
| Fast-forward (hold) | Tab |
| Quit | Esc |

## Build and run

Requires the .NET 6 SDK. Raylib-cs ships the native raylib library for Windows (x64, x86),
Linux x64 and macOS (x64, arm64), so there is nothing else to install.

```
dotnet build -c Release
dotnet run --project src/GameBoy.Desktop -c Release -- path/to/game.gb
```

Frontend options:

```
--scale N                integer LCD scale, 1-12 (default 3)
--palette NAME           green, gray or #RRGGBB,#RRGGBB,#RRGGBB,#RRGGBB (default green)
--volume N               audio volume 0-100 (default 70)
--fast-forward-speed N   speed while fast-forward is held, 2-16 (default 4)
--save-dir PATH          directory for .sav and .state files (default: next to the ROM)
--key ACTION=KEY         rebind an action, e.g. --key a=X --key start=SPACE
--config PATH            read settings from PATH instead of the default config file
```

Actions are `up down left right a b start select save-state load-state pause fast-forward`.
Key names are raylib key names without the `KEY_` prefix (`Z`, `SPACE`, `ENTER`,
`RIGHT_SHIFT`, `F5`, `KP_8`, ...), case-insensitive.

Settings can also live in a config file. Without `--config` the frontend reads
`config.ini` from the user configuration directory (`~/.config/gameboy-sharp/` on Linux,
`%APPDATA%\gameboy-sharp\` on Windows) when it exists. Command-line options override it.

```
# ~/.config/gameboy-sharp/config.ini
scale = 4
palette = gray
volume = 50
save-dir = saves
key.a = S
key.b = A
key.start = SPACE
```

A relative `save-dir` in a config file is resolved against the config file's directory.
For `game.gb` the frontend writes `game.sav` (battery RAM, only for cartridges with a
battery) and `game.state` (the quick-save slot).

### Headless runner

`GameBoy.Cli` runs a ROM without a window, which is how the test ROMs are scored:

```
dotnet run --project src/GameBoy.Cli -c Release -- <rom.gb> [options]
  --frames N        run N frames (default 60)
  --serial          run until the serial log says Passed/Failed, then print it
  --png <path>      write the framebuffer as an 8-bit grayscale PNG
  --ascii           print the framebuffer as ASCII art
  --ascii-out <p>   write the ASCII art to a file
```

```
dotnet run --project src/GameBoy.Cli -c Release -- tests/roms/cpu_instrs.gb --serial
dotnet run --project src/GameBoy.Cli -c Release -- tests/roms/dmg-acid2.gb --frames 200 --png acid2.png
```

## Tests

```
dotnet test
```

Accuracy ROMs, all run headless by `GameBoy.Core.Tests`:

| ROM | Result |
|---|---|
| Blargg `cpu_instrs` | Passed, all 11 sub-tests report `ok` over serial |
| Blargg `instr_timing` | Passed over serial |
| `dmg-acid2` | 0 of 23040 pixels differ from the reference image |

The core suite also covers CPU flag and DAA behaviour, every APU channel (period,
amplitude, DAC, length counter, power-off), MBC1/3/5 battery RAM round trips, the MBC3
clock (rollover, latch, halt, day carry, battery footer with elapsed wall time) and
save-state round trips that must continue identically to an uninterrupted run.

`GameBoy.Desktop.Tests` checks the frontend without opening a window: the frame pacer
(59.73 Hz, fast-forward, pause, stall capping), option and config parsing, key mapping into
the joypad register, the hotkeys, battery files on exit and start, and a paced frame loop
that must render `dmg-acid2` pixel-exact through the same code path the window uses.

The test ROMs and their licenses are listed in [`tests/roms/README.md`](tests/roms/README.md).

## Project layout

```
src/GameBoy.Core            the emulator, no UI dependencies
  Cpu/                      LR35902 core, ALU, CB opcodes, interrupt controller
  Memory/                   MMU and OAM DMA
  Video/                    PPU timing and scanline renderer
  Audio/                    APU, square, wave and noise channels
  Cartridge/                header parsing, MBC1/MBC3/MBC5, MBC3 RTC
  Timing/ Input/ Serial/    timer, joypad, serial port
  Util/PngImage.cs          minimal PNG writer and decoder
src/GameBoy.Desktop         Raylib frontend: window, audio, input, pacing, saves
src/GameBoy.Cli             headless runner
tests/GameBoy.Core.Tests    core and accuracy tests
tests/GameBoy.Desktop.Tests headless frontend tests
tests/roms                  third-party test ROMs
```

## Limitations

- DMG only: no Game Boy Color mode, no Super Game Boy features.
- The PPU renders a whole scanline at the start of mode 3 with a fixed mode 3 length, so
  mid-scanline register changes and variable mode 3 timing are not reproduced.
- No boot ROM; the machine starts in the state the boot ROM leaves behind.
- Unsupported mappers (MBC2, MBC6, MBC7, MMM01, HuC1/HuC3, Pocket Camera) fall back to
  MBC1 behaviour, so those games will not run correctly.
- Serial transfers on the internal clock complete at once and read back 0xFF; there is no
  link cable emulation.
- One quick-save slot per ROM; battery RAM is written when the window closes, not
  continuously.
