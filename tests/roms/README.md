# Test ROMs

Third-party test programs vendored only so the test suites can run offline. They are not
covered by this repository's MIT license; each keeps the terms of its author, listed below.

| File | Author and source | License |
|---|---|---|
| `cpu_instrs.gb` | Shay Green (blargg), Game Boy hardware test ROMs | No license file, see below |
| `instr_timing.gb` | Shay Green (blargg), Game Boy hardware test ROMs | No license file, see below |
| `dmg-acid2.gb` | Matt Currie, dmg-acid2 v1.0 | MIT, see `LICENSE-dmg-acid2.txt` |
| `dmg-acid2-ref.png` | Matt Currie, dmg-acid2 reference image | MIT, see `LICENSE-dmg-acid2.txt` |

## Blargg's test ROMs

- Original location: http://blargg.8bitalley.com/parodius/gb-tests/
- Copied from the mirror https://github.com/retrio/gb-test-roms
  (`cpu_instrs/cpu_instrs.gb`, `instr_timing/instr_timing.gb`).
- The author published these ROMs freely for emulator developers. Neither the original
  archive nor the mirror ships a license file, so there is no formal grant beyond that
  public distribution. The same files are redistributed by test collections such as
  https://github.com/c-sp/game-boy-test-roms.

## dmg-acid2

- Source: https://github.com/mattcurrie/dmg-acid2
- `dmg-acid2.gb` is the `v1.0` release asset; `dmg-acid2-ref.png` is `img/reference-dmg.png`.
- Released under the MIT License, Copyright (c) 2020 Matt Currie. The full notice is in
  `LICENSE-dmg-acid2.txt` next to the files.

## Checksums (SHA-256)

All four files are byte-identical to the upstream copies above.

```
8c5e12f41e0ba5bbca796944f92ffe6de28809198682c4332e38d1b3cf56fcf2  cpu_instrs.gb
646067b3d6c79fda810e9c3f1cb7c0efd5abb0a7ac06437c54e65720c15d9925  instr_timing.gb
464e14b7d42e7feea0b7ede42be7071dc88913f75b9ffa444299424b63d1dff1  dmg-acid2.gb
ca966d50895c7efef05838590d148c2cbfd7fba57dab986f25b35b4da71abb57  dmg-acid2-ref.png
```
