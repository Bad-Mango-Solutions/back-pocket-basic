Here's a thorough implementation checklist you can paste into a GitHub issue or hand to Copilot. I've organized it by area, with specific opcodes, addresses, and behaviors called out.

------

# 65C02 / Apple II Emulator Correctness Checklist

## 1. Address arithmetic (the bug we just fixed, plus relatives)

If you haven't already, apply the masking fix to **every** addressing mode that involves index addition:

- [ ] `abs,X` and `abs,Y` — masked at 16 bits (wraps $FFFF → $0000)
- [ ] `(zp),Y` — pointer fetched from zero page, then +Y, result masked to 16 bits
- [ ] `(zp,X)` — zp+X wraps within zero page (8-bit wrap), then pointer fetched
- [ ] `(zp)` (65C02-only, no index) — pointer fetched from zp and zp+1, both within zero page (8-bit wrap of the pointer fetch address)
- [ ] `zp,X` and `zp,Y` — wraps within zero page (8-bit, never escapes $00-$FF)
- [ ] `(abs,X)` — used by `JMP ($abs,X)` on 65C02; masked to 16 bits
- [ ] Branch target computation — PC + signed 8-bit offset, masked to 16 bits

## 2. JMP indirect bug fix (65C02 specific)

- [ ] `JMP ($xxFF)` on 65C02 reads low byte from $xxFF and high byte from $(xx+1)00 — NOT $xx00 as NMOS did
- [ ] This case takes **one extra cycle** on 65C02 (6 cycles total instead of 5)
- [ ] Add regression test: place jump vector at $20FF/$2100, execute `JMP ($20FF)`, verify it reads both bytes correctly

## 3. Page-crossing dummy read behavior

For indexed reads (`abs,X` / `abs,Y` / `(zp),Y`) when the index causes a page crossing:

- [ ] Take the extra cycle (5 cycles instead of 4 for `LDA abs,X` etc.)
- [ ] On 65C02, the dummy cycle does NOT perform a spurious read from the un-fixed-up address (the high byte of the base with the wrapped low byte). This matters if your read function has side effects on memory-mapped I/O ($C000-$CFFF on Apple II).
- [ ] Acceptable implementation: skip the dummy read entirely, or re-read the last instruction byte (PC-1). Do NOT read the un-fixed address.

Note: write instructions (`STA abs,X` etc.) always take the extra cycle on both NMOS and CMOS, regardless of page crossing.

## 4. 65C02 new/changed opcodes (must implement)

### New addressing modes

- [ ] $12 — `ORA (zp)`
- [ ] $32 — `AND (zp)`
- [ ] $52 — `EOR (zp)`
- [ ] $72 — `ADC (zp)` — sets N/V/Z flags correctly (decimal mode also corrected on 65C02; see below)
- [ ] $92 — `STA (zp)`
- [ ] $B2 — `LDA (zp)`
- [ ] $D2 — `CMP (zp)`
- [ ] $F2 — `SBC (zp)` — sets N/V/Z flags correctly
- [ ] $7C — `JMP ($abs,X)` (6 cycles)
- [ ] $89 — `BIT #imm` — only affects Z flag (N and V are NOT modified, unlike other BIT addressing modes)
- [ ] $34 — `BIT zp,X`
- [ ] $3C — `BIT abs,X`

### BIT flag behavior (subtle!)

- [ ] `BIT #imm` ($89): only Z is set/cleared based on `A AND operand`; N and V are unchanged
- [ ] All other `BIT` forms: Z from `A AND operand`, N from bit 7 of operand, V from bit 6 of operand

### Branch always

- [ ] $80 — `BRA rel` (3 cycles, +1 on page cross)

### Stack push/pull

- [ ] $DA — `PHX`
- [ ] $FA — `PLX` (sets N/Z based on pulled value)
- [ ] $5A — `PHY`
- [ ] $7A — `PLY` (sets N/Z based on pulled value)

### STZ

- [ ] $64 — `STZ zp`
- [ ] $74 — `STZ zp,X`
- [ ] $9C — `STZ abs`
- [ ] $9E — `STZ abs,X` (always 5 cycles, no page-cross variation since it's a write)

### TSB / TRB

- [ ] $04 — `TSB zp`
- [ ] $0C — `TSB abs`
- [ ] $14 — `TRB zp`
- [ ] $1C — `TRB abs`
- [ ] Flag behavior: only Z is set, based on `A AND memory` BEFORE the modification. TSB sets bits where A has 1s; TRB clears bits where A has 1s.

### INC / DEC accumulator

- [ ] $1A — `INC A` (alias `INA`)
- [ ] $3A — `DEC A` (alias `DEA`)
- [ ] Both update N and Z

### Decimal mode flags

- [ ] On 65C02, `ADC` and `SBC` in decimal mode correctly set N, V, and Z based on the BCD result (NMOS 6502 left these undefined in decimal mode). This takes one extra cycle compared to binary mode.
- [ ] After `SED` (set decimal), `ADC`/`SBC` produce valid BCD results AND correct flags.

### CLD on reset/interrupt

- [ ] On 65C02, D flag is cleared on reset, IRQ, and NMI (NMOS only cleared it on reset, leading to bugs if interrupts fired in decimal mode).

## 5. Rockwell/WDC bit instructions (present on Apple IIe Enhanced, IIc, IIc+)

- [ ] $07/$17/$27/$37/$47/$57/$67/$77 — `RMB0 zp` through `RMB7 zp` (5 cycles)
- [ ] $87/$97/$A7/$B7/$C7/$D7/$E7/$F7 — `SMB0 zp` through `SMB7 zp` (5 cycles)
- [ ] $0F/$1F/$2F/$3F/$4F/$5F/$6F/$7F — `BBR0` through `BBR7` (5 cycles, +1 if branch taken, +1 more if page cross)
- [ ] $8F/$9F/$AF/$BF/$CF/$DF/$EF/$FF — `BBS0` through `BBS7`
- [ ] BBR/BBS format: `op zp, rel` — 3 bytes: opcode, zero-page address, signed branch offset. Branches if specified bit of zero-page byte is reset (BBR) or set (BBS). Does NOT modify flags.

## 6. NOP slots (unused opcodes on 65C02)

The WDC 65C02 defines all unused opcodes as multi-byte/multi-cycle NOPs with specific behavior. Do NOT treat them as single-byte 2-cycle NOPs:

- [ ] $02, $22, $42, $62, $82, $C2, $E2 — 2 bytes, 2 cycles (these read and discard the immediate operand)
- [ ] $44 — 2 bytes, 3 cycles
- [ ] $54, $D4, $F4 — 2 bytes, 4 cycles
- [ ] $5C — 3 bytes, 8 cycles (reads from absolute address; this is the only "loud" NOP that touches the bus)
- [ ] $DC, $FC — 3 bytes, 4 cycles
- [ ] $03, $13, $23, $33, $43, $53, $63, $73, $83, $93, $A3, $B3, $C3, $D3, $E3, $F3 — 1 byte, 1 cycle
- [ ] $0B, $1B, $2B, $3B, $4B, $5B, $6B, $7B, $8B, $9B, $AB, $BB, $EB, $FB — 1 byte, 1 cycle
- [ ] None of these affect any flags or registers

The $C2 case (the one that started this) MUST be: 2 bytes, 2 cycles, no flag changes, no register changes. Verify your implementation explicitly.

## 7. STP and WAI (WDC extensions; rare but implement stubs)

- [ ] $DB — `STP` (stop) — halts CPU until reset. In an emulator, set a halt flag.
- [ ] $CB — `WAI` (wait for interrupt) — halts CPU until IRQ or NMI. Implement as wait state cleared on interrupt.

~~If you're targeting Rockwell R65C02 (no STP/WAI), treat these as 1-byte NOPs instead. For Apple II emulation, the WDC behavior is the safer default.~~ We've already implemented these. Stick with the WDC implementation.

## 8. Apple II ROM and machine ID requirements (ProDOS specific)

These are ROM-related and are satisfied by proper ROM images we already have:

- [ ] System ROM image must contain the ASCII string "Apple" somewhere in the $D000–$FFFF range, or ProDOS will hang at the splash screen. Standard Apple ROM dumps have this; custom/stub ROMs may not.
- [ ] Machine ID signature bytes must be correct for the model you're emulating:
  - $FBB3 — primary machine ID byte ($06 = II/II+, $EA = IIe, $E0 = enhanced IIe / IIc / IIgs)
  - $FBC0 — secondary ID ($00 = IIe unenhanced or IIc, $E0 = IIe enhanced, $00 also for some others; check Apple II Technical Notes)
  - $FBBF — IIc/IIgs identification
  - $FBDD — beep routine entry point (some software identifies models via this)
- [ ] After boot, the ProDOS machine ID is stored at $BF98. This is informational; you don't write it, but tests can read it to verify the boot path made the right decision.

## 9. Memory map basics (sanity check)

- [ ] $0000-$00FF — zero page
- [ ] $0100-$01FF — stack (SP indexes here; pushes decrement, pulls increment; wraps within page)
- [ ] $C000-$C0FF — I/O soft switches (reads/writes have side effects; the page-crossing dummy-read fix matters here)
- [ ] $C100-$CFFF — slot ROM / expansion ROM
- [ ] $D000-$FFFF — system ROM (or language card RAM when switched in)
- [ ] Stack wraparound **(check this behavior for sure!)**: `PHA` at SP=$00 stores to $0100 and SP becomes $FF; `PLA` at SP=$FF reads from $0100 and SP becomes $00. Stack never escapes page $01.

## 10. Interrupt and reset behavior

- [ ] Reset: read vector from $FFFC/$FFFD, set PC. Set I flag, clear D flag (65C02 only — NMOS leaves D undefined on reset). SP is decremented by 3 but no actual writes occur (the "fake push" of PC and P).
- [ ] IRQ (if I flag clear): push PCH, PCL, P (with B=0), set I, clear D (65C02), read vector from $FFFE/$FFFF.
- [ ] NMI: push PCH, PCL, P (with B=0), set I, clear D (65C02), read vector from $FFFA/$FFFB. NMI is edge-triggered and ignores I flag.
- [ ] BRK: push PCH, PCL (of instruction AFTER BRK — i.e., PC+2 since BRK is treated as 2 bytes), push P (with B=1), set I, clear D (65C02), read vector from $FFFE/$FFFF. Same vector as IRQ; handler distinguishes via B flag in pushed P. (Make sure disassembly handles this and formats BRK accordingly.)

## 11. Suggested test/regression cases

Build these into automated tests:

- [ ] `LDA $FF48,Y` with Y=$FE → reads from $0046 (the original bug)
- [ ] `LDA $FFFF,X` with X=$01 → reads from $0000
- [ ] `STA $FFFF,X` with X=$01 → writes to $0000
- [ ] `LDA ($FE),Y` where $FE/$FF contains $FF/$FF and Y=$02 → reads from $0001
- [ ] `JMP ($20FF)` → reads vector low from $20FF, high from $2100 (not $2000)
- [ ] `BRA $80` at $1000 with offset $7F → branches to $1081
- [ ] `BRA $80` at $1000 with offset $80 → branches to $0F82 (signed offset)
- [ ] `C2 02` sequence: flags unchanged, PC advances by 2, 2 cycles consumed
- [ ] Bitsy Bye CPU detection sequence: after Z=1 entering $102C, BEQ at $102E branches taken on 65C02
- [ ] `INC A` on $FF → A=$00, Z=1, N=0
- [ ] `DEC A` on $00 → A=$FF, Z=0, N=1
- [ ] `STZ` variants all write $00 to target without affecting flags or A
- [ ] `TSB zp` with A=$0F and ($zp)=$F0 → ($zp) becomes $FF, Z flag set from ($0F AND $F0)=$00 → Z=1
- [ ] `TRB zp` with A=$0F and ($zp)=$FF → ($zp) becomes $F0, Z flag set from ($0F AND $FF)=$0F → Z=0
- [ ] `BIT #$00` with A=$FF → Z=1, N and V unchanged from previous state
- [ ] `BIT $abs` with A=$FF and ($abs)=$C0 → Z=0, N=1, V=1
- [ ] `RMB3 $80` with ($80)=$FF → ($80) becomes $F7
- [ ] `SMB5 $80` with ($80)=$00 → ($80) becomes $20
- [ ] `BBR0 $80, +4` with ($80)=$FE → branch taken (bit 0 is reset)
- [ ] `BBS7 $80, +4` with ($80)=$80 → branch taken (bit 7 is set)
- [ ] Decimal mode: `SED; LDA #$09; CLC; ADC #$01` → A=$10, Z=0, N=0, C=0, V=0; takes 3 cycles (one extra over binary mode)
- [ ] Decimal mode flags after `ADC` correctly reflect BCD result (not binary intermediate)
- [ ] After reset: D flag is 0, I flag is 1
- [ ] $C2 followed by any byte: 2 cycles, no flag changes, PC+=2
- [ ] $5C followed by two bytes: 8 cycles, reads from the formed absolute address (this NOP does hit the bus)
- [ ] $03/$13/etc.: 1 cycle, 1 byte, nothing happens

## 12. Implementation pattern suggestions for Copilot

These are for reference only.

```csharp
// Effective address helpers — use these everywhere
private static ushort AddrAbsIndexed(ushort baseAddr, byte index) 
    => (ushort)(baseAddr + index);

private static byte AddrZpIndexed(byte zp, byte index) 
    => (byte)(zp + index);  // wraps in zero page

private static bool PageCrossed(ushort baseAddr, byte index) 
    => ((baseAddr & 0xFF) + index) > 0xFF;

// JMP indirect — 65C02 correct version
private ushort ReadIndirect(ushort ptr) {
    byte lo = ReadByte(ptr);
    byte hi = ReadByte((ushort)(ptr + 1));  // proper 16-bit increment, NOT (ptr & 0xFF00) | ((ptr+1) & 0xFF)
    return (ushort)(lo | (hi << 8));
}

// Zero page indirect — wraps within zero page for both bytes
private ushort ReadZpIndirect(byte zp) {
    byte lo = ReadByte(zp);
    byte hi = ReadByte((byte)(zp + 1));  // wraps in zero page!
    return (ushort)(lo | (hi << 8));
}
```

------

That should give Copilot enough context to systematically work through 65C02 conformance. The "Suggested test/regression cases" section is especially valuable — if you turn those into actual unit tests, you'll catch regressions on every change.

One caveat: I generated this from memory plus the search results, and a few of the cycle counts for the rare NOP slots and the BBR/BBS exact +1/+1 page-cross details should be cross-checked against the WDC W65C02S datasheet before you commit them as ground truth. The 65C02 opcode matrix at [6502.org](http://www.6502.org/tutorials/65c02opcodes.html) is also a good reference. But the addressing-mode wrap behavior, the BIT immediate flag quirk, the JMP indirect fix, and the decimal-mode flag fix are all solid.