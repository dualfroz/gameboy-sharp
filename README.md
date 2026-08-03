# gameboy-sharp

A Game Boy (DMG) emulator written in C# (.NET 6). The core is a pure library with
no frontend dependency, so the whole machine runs and is verified headless.

## Status - wave 1

Working, test-verified core that runs real DMG software:

- CPU LR35902: full opcode set + CB prefix, cycle-accurate M-cycle timing, Z/N/H/C
  flags, interrupts (VBlank/LCD/Timer/Serial/Joypad), HALT + HALT bug, STOP (minimal),
  delayed EI, DI, RETI.
- MMU: full memory map, I/O registers, echo RAM, OAM DMA.
- Cartridge: header parse, MBC1 (ROM/RAM banking), MBC3 and MBC5 banking (no RTC yet),
  plain ROM.
- PPU: per-scanline mode 2/3/0 + VBlank timing, LCDC/STAT/SCX/SCY/LY/LYC/WX/WY/BGP/OBP0/OBP1,
  background + window (with the window internal line counter) + sprites (8x8 and 8x16,
  X/OAM priority, 10-per-line limit, BG priority), 160x144 framebuffer, STAT/LYC interrupts.
- Timer: DIV/TIMA/TMA/TAC via the internal 16-bit counter, falling-edge detection and the
  TIMA overflow reload delay.
- Joypad and serial (serial output is captured, which is how the Blargg tests are scored).
- APU: register-level stub (open-bus read masks, NR52 power). No synthesis yet - wave 2.

## Layout

- `src/GameBoy.Core` - the machine (no frontend dependency).
- `src/GameBoy.Cli` - headless runner: loads a ROM, runs frames, dumps the framebuffer.
- `tests/GameBoy.Core.Tests` - the verification gate.

## Verification gate

`dotnet test` runs, headless:

- `cpu_instrs` (Blargg) - asserts the serial log reports `Passed` and never `Failed`.
- `instr_timing` (Blargg) - same serial assertion.
- `dmg-acid2` - decodes the reference PNG and asserts the rendered framebuffer matches it
  pixel for pixel (0 mismatches out of 23040).
- CPU micro tests (flags, DAA, push/pop, CB swap).

```
dotnet test -c Release
```

## Running a ROM (headless)

```
dotnet run --project src/GameBoy.Cli -c Release -- <rom.gb> [options]
  --frames N        run N frames (default 60)
  --serial          run until the serial log says Passed/Failed, then print it
  --png <path>      write the framebuffer as an 8-bit grayscale PNG
  --ascii           print the framebuffer as ASCII art
  --ascii-out <p>   write the ASCII art to a file
```

Examples:

```
dotnet run --project src/GameBoy.Cli -c Release -- tests/roms/cpu_instrs.gb --serial
dotnet run --project src/GameBoy.Cli -c Release -- tests/roms/dmg-acid2.gb --frames 200 --png acid2.png
```

## Test ROMs and licenses

Vendored under `tests/roms/` purely to run the verification gate:

- `cpu_instrs.gb`, `instr_timing.gb` - Blargg's Game Boy hardware test ROMs by Shay Green
  (blargg), released for free distribution. Fetched from
  https://github.com/retrio/gb-test-roms
- `dmg-acid2.gb` and `dmg-acid2-ref.png` - the DMG variant of the PPU acid test by
  Matt Currie, MIT licensed. Fetched from https://github.com/mattcurrie/dmg-acid2

These are third-party artifacts kept only for testing; their own licenses apply.

## Not covered yet (wave 2+)

APU synthesis (4 channels + mixdown), MBC3 RTC + MBC5 corner cases, battery RAM save
(`.sav`), Game Boy Color, a windowed frontend (Raylib-cs/SDL - needs a display, so it was
left out of this headless wave), a debugger (breakpoints, disassembler, tile/OAM viewers)
and save states.
