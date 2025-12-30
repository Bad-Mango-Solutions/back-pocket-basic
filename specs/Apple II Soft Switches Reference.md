# Apple II Soft Switches Reference ($C000-$C0FF)

## Overview

The Apple II soft switches are memory-mapped hardware registers located in the I/O page from $C000 to $C0FF. Accessing these addresses (read or write) triggers hardware state changes or returns status information.

**Key Concepts:**
- Most switches are **strobe-based**: any access (read or write) triggers the action
- Status reads return state in **bit 7** (allowing efficient BMI/BPL testing)
- Write-protect enable requires **two consecutive reads** (prevents accidental writes)
- The Apple IIe and IIc add auxiliary memory and 80-column soft switches

---

## Keyboard ($C000-$C01F)

### Keyboard Data and Strobe

| Address | Name       | R/W | Description |
|---------|------------|-----|-------------|
| $C000   | KBD        | R   | Keyboard data with strobe bit |
| $C010   | KBDSTRB    | R/W | Clear keyboard strobe |

#### KBD ($C000) - Read Keyboard Data

**Function:** Returns the ASCII code of the last key pressed.

| Bit | Meaning |
|-----|---------|
| 7   | Key available (strobe): 1 = key waiting, 0 = no key |
| 6-0 | ASCII code of key (with high bit set when strobe is active) |

**Usage:**
```assembly
        LDA $C000       ; Read keyboard
        BPL NO_KEY      ; Branch if no key (bit 7 clear)
        STA $C010       ; Clear strobe
        AND #$7F        ; Mask off strobe bit for ASCII
```

**IIe/IIc Differences:** None for basic keyboard reading.

#### KBDSTRB ($C010) - Clear Keyboard Strobe

**Function:** Any read or write clears the keyboard strobe (bit 7 of $C000).

**Usage:**
```assembly
        STA $C010       ; Clear strobe (value doesn't matter)
        ; or
        BIT $C010       ; Clear strobe without modifying A
```

---

## Apple IIe/IIc Extended Keyboard Switches ($C000-$C00F Write)

These switches exist only on the Apple IIe and IIc. They control auxiliary memory and display modes.

| Address | Name       | W   | Description |
|---------|------------|-----|-------------|
| $C000   | 80STOREOFF | W   | Disable 80STORE mode |
| $C001   | 80STOREON  | W   | Enable 80STORE mode |
| $C002   | RDMAINRAM  | W   | Read from main 48K RAM |
| $C003   | RDCARDRAM  | W   | Read from auxiliary 48K RAM |
| $C004   | WRMAINRAM  | W   | Write to main 48K RAM |
| $C005   | WRCARDRAM  | W   | Write to auxiliary 48K RAM |
| $C006   | SETSLOTCX  | W   | Peripheral ROM at $C100-$CFFF |
| $C007   | SETINTCX   | W   | Internal ROM at $C100-$CFFF |
| $C008   | SETSTDZP   | W   | Main zero page, stack, LC |
| $C009   | SETALTZP   | W   | Auxiliary zero page, stack, LC |
| $C00A   | SETINTC3   | W   | Internal ROM at $C300 |
| $C00B   | SETSLOTC3  | W   | Peripheral ROM at $C300 |
| $C00C   | 80COLOFF   | W   | 40-column display |
| $C00D   | 80COLON    | W   | 80-column display |
| $C00E   | ALTCHAROFF | W   | Primary character set |
| $C00F   | ALTCHARON  | W   | Alternate character set (MouseText) |

### 80STORE Mode ($C000/$C001)

When 80STORE is on, the PAGE2 switch ($C054/$C055) controls auxiliary memory access for display pages instead of page selection:
- PAGE2 off: Access main memory display pages
- PAGE2 on: Access auxiliary memory display pages

**IIc Difference:** 80STORE is always available; no slot 3 card needed.

### Auxiliary Memory ($C002-$C005)

| Switch | Effect |
|--------|--------|
| $C002  | Reads from $0200-$BFFF come from main RAM |
| $C003  | Reads from $0200-$BFFF come from auxiliary RAM |
| $C004  | Writes to $0200-$BFFF go to main RAM |
| $C005  | Writes to $0200-$BFFF go to auxiliary RAM |

**Note:** Zero page ($00-$FF), stack ($100-$1FF), and language card are controlled separately by ALTZP.

### Internal/Slot ROM ($C006/$C007, $C00A/$C00B)

| Switch | Effect |
|--------|--------|
| $C006  | Slot ROM visible at $C100-$CFFF (normal) |
| $C007  | Internal (motherboard) ROM at $C100-$CFFF |
| $C00A  | Internal ROM at $C300 only (80-col firmware) |
| $C00B  | Slot 3 ROM at $C300 (if card present) |

**IIc Difference:** $C006/$C007 have no effect (IIc has no slots). $C00A/$C00B toggle built-in 80-column firmware.

### Alternate Zero Page ($C008/$C009)

| Switch | Effect |
|--------|--------|
| $C008  | Use main zero page, stack, and language card |
| $C009  | Use auxiliary zero page, stack, and language card |

**Critical:** Switching ALTZP affects the entire $00-$01FF range plus language card bank selection.

---

## Status Reads ($C010-$C01F)

These addresses return status information about various soft switch states. The status is always in **bit 7**.

| Address | Name       | R   | Bit 7 = 1 Means |
|---------|------------|-----|-----------------|
| $C010   | KBDSTRB    | R   | Any key down (momentary) |
| $C011   | RDLCBNK2   | R   | Language card bank 2 selected |
| $C012   | RDLCRAM    | R   | Language card RAM read enabled |
| $C013   | RDRAMRD    | R   | Reading auxiliary RAM (main 48K) |
| $C014   | RDRAMWRT   | R   | Writing auxiliary RAM (main 48K) |
| $C015   | RDCXROM    | R   | Internal ROM at $C100-$CFFF |
| $C016   | RDALTZP    | R   | Auxiliary zero page enabled |
| $C017   | RDC3ROM    | R   | Internal ROM at $C300 |
| $C018   | RD80STORE  | R   | 80STORE mode enabled |
| $C019   | RDVBL      | R   | **Bit 7 = 0** during vertical blank |
| $C01A   | RDTEXT     | R   | Text mode enabled |
| $C01B   | RDMIXED    | R   | Mixed mode enabled |
| $C01C   | RDPAGE2    | R   | Page 2 displayed |
| $C01D   | RDHIRES    | R   | Hi-res mode enabled |
| $C01E   | RDALTCHAR  | R   | Alternate character set enabled |
| $C01F   | RD80COL    | R   | 80-column mode enabled |

### Vertical Blank Detection ($C019)

**Special:** RDVBL returns **bit 7 = 0** during vertical blank (inverted from other status reads).

**Usage for timing:**
```assembly
WAIT_VBL:
        LDA $C019       ; Check VBL status
        BMI WAIT_VBL    ; Wait while bit 7 = 1 (not in VBL)
        ; Now in vertical blank period
```

**IIe vs IIc:** Identical behavior.

---

## Cassette ($C020-$C02F)

Legacy cassette I/O (rarely used on IIe/IIc).

| Address | Name     | R/W | Description |
|---------|----------|-----|-------------|
| $C020   | TAPEOUT  | W   | Toggle cassette output |

**IIc Difference:** $C020 is active but IIc has no cassette port. Some IIc programs repurpose this for timing.

---

## Speaker ($C030-$C03F)

| Address | Name   | R/W | Description |
|---------|--------|-----|-------------|
| $C030   | SPKR   | R/W | Toggle speaker cone position |

**Function:** Any access toggles the speaker between two positions, creating a click. Rapid toggling produces tones.

**Usage for a beep:**
```assembly
BEEP:   LDY #$20        ; Duration
        LDX #$00
LOOP:   LDA $C030       ; Toggle speaker
        DEX
        BNE LOOP        ; Inner loop (frequency)
        DEY
        BNE LOOP        ; Outer loop (duration)
        RTS
```

**IIe vs IIc:** Identical behavior, but IIc speaker is internal and quieter.

---

## Game I/O Strobe ($C040-$C04F)

| Address | Name   | R/W | Description |
|---------|--------|-----|-------------|
| $C040   | STROBE | R/W | Game I/O strobe |

**Function:** Triggers the analog-to-digital conversion for game paddles. Used with PTRIG.

---

## Graphics Mode Switches ($C050-$C05F)

### Display Mode Selection

| Address | Name    | R/W | Description |
|---------|---------|-----|-------------|
| $C050   | TXTCLR  | R/W | Graphics mode (clear TEXT) |
| $C051   | TXTSET  | R/W | Text mode (set TEXT) |
| $C052   | MIXCLR  | R/W | Full-screen mode (clear MIXED) |
| $C053   | MIXSET  | R/W | Mixed mode: 4 lines text at bottom |
| $C054   | LOWSCR  | R/W | Display page 1 |
| $C055   | HISCR   | R/W | Display page 2 |
| $C056   | LORES   | R/W | Lo-res graphics (clear HIRES) |
| $C057   | HIRES   | R/W | Hi-res graphics (set HIRES) |

### Display Mode Truth Table

| TEXT | MIXED | HIRES | Result |
|------|-------|-------|--------|
| 1    | X     | X     | 40-column text (or 80-col if enabled) |
| 0    | 0     | 0     | Full-screen lo-res (40×48) |
| 0    | 1     | 0     | Lo-res with 4 lines text |
| 0    | 0     | 1     | Full-screen hi-res (280×192) |
| 0    | 1     | 1     | Hi-res with 4 lines text |

### Page Memory Addresses

| Mode | Page 1 | Page 2 |
|------|--------|--------|
| Text/Lo-Res | $0400-$07FF | $0800-$0BFF |
| Hi-Res | $2000-$3FFF | $4000-$5FFF |

### Annunciator Outputs ($C058-$C05F)

| Address | Name   | R/W | Description |
|---------|--------|-----|-------------|
| $C058   | AN0OFF | R/W | Annunciator 0 = 0 |
| $C059   | AN0ON  | R/W | Annunciator 0 = 1 |
| $C05A   | AN1OFF | R/W | Annunciator 1 = 0 |
| $C05B   | AN1ON  | R/W | Annunciator 1 = 1 |
| $C05C   | AN2OFF | R/W | Annunciator 2 = 0 |
| $C05D   | AN2ON  | R/W | Annunciator 2 = 1 |
| $C05E   | AN3OFF | R/W | Annunciator 3 = 0 / Double Hi-Res OFF |
| $C05F   | AN3ON  | R/W | Annunciator 3 = 1 / Double Hi-Res ON |

**Double Hi-Res (IIe/IIc):** AN3 ($C05E/$C05F) also controls double hi-res mode when 80STORE is enabled.

To enable double hi-res:
```assembly
        STA $C001       ; 80STORE on
        STA $C00D       ; 80COL on  
        STA $C057       ; HIRES on
        STA $C050       ; Graphics on
        STA $C05F       ; AN3 on (DHIRES)
```

---

## Pushbuttons and Paddles ($C060-$C07F)

### Digital Inputs ($C060-$C063)

| Address | Name   | R   | Description |
|---------|--------|-----|-------------|
| $C060   | TAPEIN | R   | Cassette input (legacy) |
| $C061   | PB0    | R   | Pushbutton 0 / Open-Apple / Button 0 |
| $C062   | PB1    | R   | Pushbutton 1 / Solid-Apple / Button 1 |
| $C063   | PB2    | R   | Pushbutton 2 / Shift key modifier |

**Bit 7:** 1 = button pressed, 0 = not pressed

**Usage:**
```assembly
        LDA $C061       ; Read Open-Apple
        BMI PRESSED     ; Branch if pressed (bit 7 = 1)
```

**IIc Difference:** $C063 reads the state of the keyboard's shift key (active low on some revisions).

### Analog Inputs ($C064-$C067)

| Address | Name   | R   | Description |
|---------|--------|-----|-------------|
| $C064   | PADDL0 | R   | Paddle 0 / Joystick X |
| $C065   | PADDL1 | R   | Paddle 1 / Joystick Y |
| $C066   | PADDL2 | R   | Paddle 2 / Second joystick X |
| $C067   | PADDL3 | R   | Paddle 3 / Second joystick Y |

**Bit 7:** 1 = timer still running, 0 = timer expired

**Reading Paddle Values:**
1. Trigger the timers by accessing PTRIG ($C070)
2. Count cycles until each paddle's bit 7 goes low
3. The count corresponds to the paddle position (0-255)

**Usage:**
```assembly
PREAD:  LDA $C070       ; Trigger timers
        LDY #$00        ; Counter
LOOP:   LDA $C064       ; Check paddle 0
        BPL DONE        ; Done if bit 7 = 0
        INY
        BNE LOOP        ; Continue counting
DONE:   ; Y = paddle value (0-255)
```

**IIc Difference:** IIc has only one joystick port (paddles 0-1). Paddles 2-3 always read as maximum value.

### Paddle Trigger ($C070-$C07F)

| Address | Name  | R/W | Description |
|---------|-------|-----|-------------|
| $C070   | PTRIG | R/W | Trigger paddle timers |

**Function:** Any access starts the one-shot timers for all four paddle inputs.

---

## Language Card ($C080-$C08F)

The Language Card (LC) provides 16KB of RAM that can overlay the ROM at $D000-$FFFF. The IIe and IIc have this built-in.

### Bank Organization

```
$D000-$DFFF : 4KB - Either Bank 1 or Bank 2 (switchable)
$E000-$FFFF : 8KB - Always the same bank
```

**Total:** 16KB (4KB + 4KB + 8KB, with 4KB bank-switched)

### Language Card Switches

| Address | Read Source | Write Enable | D000 Bank | Notes |
|---------|-------------|--------------|-----------|-------|
| $C080   | RAM         | Disabled     | 2         | |
| $C081   | ROM         | Enabled*     | 2         | *Requires 2 reads |
| $C082   | ROM         | Disabled     | 2         | |
| $C083   | RAM         | Enabled*     | 2         | *Requires 2 reads |
| $C084   | RAM         | Disabled     | 2         | (Alias of $C080) |
| $C085   | ROM         | Enabled*     | 2         | (Alias of $C081) |
| $C086   | ROM         | Disabled     | 2         | (Alias of $C082) |
| $C087   | RAM         | Enabled*     | 2         | (Alias of $C083) |
| $C088   | RAM         | Disabled     | 1         | |
| $C089   | ROM         | Enabled*     | 1         | *Requires 2 reads |
| $C08A   | ROM         | Disabled     | 1         | |
| $C08B   | RAM         | Enabled*     | 1         | *Requires 2 reads |
| $C08C   | RAM         | Disabled     | 1         | (Alias of $C088) |
| $C08D   | ROM         | Enabled*     | 1         | (Alias of $C089) |
| $C08E   | ROM         | Disabled     | 1         | (Alias of $C08A) |
| $C08F   | RAM         | Enabled*     | 1         | (Alias of $C08B) |

### Write-Enable Protocol

To enable writes to language card RAM, you must read the same enable address **twice in succession**:

```assembly
        LDA $C08B       ; First read - write still disabled
        LDA $C08B       ; Second read - write now enabled
        ; Now can write to $D000-$FFFF
```

**Rationale:** Prevents accidental corruption of RAM contents from stray memory accesses.

### Common Patterns

**Read/Write RAM, Bank 1:**
```assembly
        LDA $C08B       ; First read
        LDA $C08B       ; Second read - enables write
```

**Read ROM, Write RAM, Bank 2:**
```assembly
        LDA $C081       ; First read
        LDA $C081       ; Second read - enables write
        ; Reads come from ROM, writes go to RAM
```

**Read RAM, No Write, Bank 2:**
```assembly
        LDA $C080       ; Single read sufficient
```

### Status Reading

| Address | R   | Bit 7 = 1 Means |
|---------|-----|-----------------|
| $C011   | R   | Bank 2 selected (vs Bank 1) |
| $C012   | R   | Reading from LC RAM (vs ROM) |

---

## Slot Device I/O ($C090-$C0FF)

Each expansion slot has 16 bytes of device-specific I/O registers.

| Address Range | Slot | Typical Use |
|---------------|------|-------------|
| $C090-$C09F   | 1    | Printer cards |
| $C0A0-$C0AF   | 2    | Serial cards, modems |
| $C0B0-$C0BF   | 3    | 80-column card (IIe internal) |
| $C0C0-$C0CF   | 4    | Mouse, clock, Mockingboard |
| $C0D0-$C0DF   | 5    | RAM cards, accelerators |
| $C0E0-$C0EF   | 6    | **Disk II controller** |
| $C0F0-$C0FF   | 7    | RAM cards, hard disk |

### Disk II Registers (Slot 6: $C0E0-$C0EF)

| Offset | Name     | Function |
|--------|----------|----------|
| +$00   | PH0OFF   | Stepper motor phase 0 off |
| +$01   | PH0ON    | Stepper motor phase 0 on |
| +$02   | PH1OFF   | Stepper motor phase 1 off |
| +$03   | PH1ON    | Stepper motor phase 1 on |
| +$04   | PH2OFF   | Stepper motor phase 2 off |
| +$05   | PH2ON    | Stepper motor phase 2 on |
| +$06   | PH3OFF   | Stepper motor phase 3 off |
| +$07   | PH3ON    | Stepper motor phase 3 on |
| +$08   | MOTOROFF | Drive motor off |
| +$09   | MOTORON  | Drive motor on |
| +$0A   | DRV0EN   | Select drive 1 |
| +$0B   | DRV1EN   | Select drive 2 |
| +$0C   | Q6L      | Shift/read data |
| +$0D   | Q6H      | Load/read write-protect |
| +$0E   | Q7L      | Read mode |
| +$0F   | Q7H      | Write mode |

**IIc Difference:** IIc uses built-in disk controller at slot 6 addresses. Slot 5 addresses ($C0D0-$C0DF) access the external drive port.

---

## IIe vs IIc Summary of Differences

| Feature | Apple IIe | Apple IIc |
|---------|-----------|-----------|
| Physical slots | 7 (1-7) | None (all built-in) |
| $C006/$C007 | Switches slot/internal ROM | No effect |
| $C063 (PB2) | Accent-grave key / Open-Apple | Shift key state |
| Paddle ports | 2 (4 paddles) | 1 (2 paddles) |
| Disk controller | Slot 6 card | Built-in at slot 6 addresses |
| Second drive | Slot 6 drive 2 | External port at slot 5 addresses |
| 80-column | Requires aux card | Built-in |
| Serial ports | Requires slot cards | Built-in at slots 1, 2 |
| Mouse | Requires slot card | Built-in at slot 4 |

---

## Document History

| Version | Date       | Changes |
|---------|------------|---------|
| 1.0     | 2025-12-30 | Initial specification |
