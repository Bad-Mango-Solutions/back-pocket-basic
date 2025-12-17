# Memory Map

Detailed documentation of the Apple II memory layout ($0000-$FFFF).

## Overview

The Apple II has a 64KB address space (addresses $0000 to $FFFF). This page documents the memory layout as emulated by the interpreter.

## Complete Memory Map

| Address Range | Size | Region Name | Description |
|---------------|------|-------------|-------------|
| $0000-$00FF | 256 bytes | Zero Page | Fast-access memory |
| $0100-$01FF | 256 bytes | Stack | 6502 hardware stack |
| $0200-$02FF | 256 bytes | Input Buffer | Keyboard input buffer |
| $0300-$03CF | 208 bytes | System Vectors | System and user vectors |
| $03D0-$03FF | 48 bytes | DOS/ProDOS | DOS/ProDOS workspace |
| $0400-$07FF | 1024 bytes | Text Page 1 | Text/Lo-Res graphics page 1 |
| $0800-$0BFF | 1024 bytes | Text Page 2 | Text/Lo-Res graphics page 2 |
| $0800-$95FF | ~37KB | BASIC Area | Program text and variables |
| $9600-$BFFF | ~10KB | Hi-Res Shape | Hi-res shape tables (optional) |
| $2000-$3FFF | 8192 bytes | Hi-Res Page 1 | Hi-res graphics page 1 |
| $4000-$5FFF | 8192 bytes | Hi-Res Page 2 | Hi-res graphics page 2 |
| $C000-$CFFF | 4096 bytes | I/O | Memory-mapped I/O |
| $D000-$F7FF | 10240 bytes | ROM 1 | Applesoft BASIC ROM |
| $F800-$FFFF | 2048 bytes | ROM 2 | Monitor ROM |

## Detailed Regions

### Zero Page ($0000-$00FF)

The first 256 bytes of memory. Called "Zero Page" because the high byte of the address is always $00.

**Special Properties:**
- Fastest memory access on 6502
- Used by Applesoft BASIC for internal variables
- Some locations available for user programs

**Common Locations:**

| Address | Name | Usage |
|---------|------|-------|
| $00-$02 | - | Monitor temps |
| $06-$09 | - | User subroutine vectors |
| $0A-$0C | USRADR | USR function jump vector (JMP instruction) |
| $32-$33 | - | Monitor cursor position |
| $67-$69 | TXTTAB | BASIC program start |
| $6A-$6C | VARTAB | BASIC variables start |
| $6D-$6F | ARYTAB | BASIC arrays start |
| $70-$72 | STREND | End of array storage |
| $73-$75 | FRETOP | Top of string storage |
| $9D-$A1 | FAC1 | Primary floating-point accumulator (5 bytes, MBF format) |
| $A2 | FAC1SIGN | FAC1 sign byte |
| $A5-$A9 | FAC2 | Secondary floating-point accumulator (5 bytes, MBF format) |
| $AF-$B0 | HIMEM | High memory limit |
| $CA | - | Cursor column |
| $E0-$E1 | LOMEM | Low memory limit |

**Floating-Point Accumulator (FAC) Format:**

The FAC registers use Microsoft Binary Format (MBF), a 5-byte representation:
- **Byte 0**: Exponent (biased by 128)
- **Bytes 1-4**: Mantissa (normalized with implicit leading 1)
- **Sign**: Stored in MSB of byte 1

See `MBF` struct and `FacConverter` class for programmatic access.

**Usage Example:**
```basic
10 REM READ CURSOR COLUMN
20 COLUMN = PEEK(202)
30 PRINT "CURSOR AT COLUMN"; COLUMN
```

---

### Stack ($0100-$01FF)

The 6502 hardware stack. Used for:
- Subroutine return addresses (JSR/RTS)
- Interrupt handling (BRK/RTI)
- Temporary storage (PHA/PLA)

**Stack Pointer:**
- Starts at $FF (stack empty)
- Decrements as data is pushed
- Stack grows downward from $01FF to $0100

**BASIC Usage:**
- GOSUB/RETURN uses the stack
- DEF FN calls may use stack
- Not directly accessible from BASIC

**Machine Code Example:**
```
LDA #$42      ; Load 42
PHA           ; Push A to stack ($01FF)
LDA #$00      ; Load 0
PLA           ; Pull from stack (A = $42)
```

---

### Input Buffer ($0200-$02FF)

Keyboard input buffer and line editing workspace.

**Purpose:**
- Stores characters as you type
- Used by INPUT and GET commands
- Line editor workspace

**Size:** 256 bytes (one full page)

---

### System Vectors ($0300-$03CF)

System and user-defined vectors for interrupts and I/O.

**Notable Addresses:**

| Address | Vector | Purpose |
|---------|--------|---------|
| $03F0 | CONNECT | Output redirection |
| $03F2 | RDKEY | Keyboard input |
| $03F8 | ERROUT | Error output |

**Safe for User Code:**
- $0300-$03CF generally safe for machine code
- Often called "Page 3"
- Traditional location for short ML routines

---

### Text/Lo-Res Graphics Pages

#### Text Page 1 ($0400-$07FF)

**Layout:** 24 rows × 40 columns = 960 characters + 64 bytes unused

**Row Address Calculation:**
```
Row 0:  $0400-$0427  (40 bytes)
Row 1:  $0480-$04A7
Row 2:  $0500-$0527
...
Row 23: $07D0-$07F7
```

**Character Codes:**
- $00-$3F: Inverse characters
- $40-$7F: Flashing characters
- $80-$FF: Normal characters

**Example:**
```basic
10 REM WRITE DIRECTLY TO SCREEN
20 POKE 1024, 193: REM PUT 'A' AT TOP-LEFT
```

#### Text Page 2 ($0800-$0BFF)

Same layout as Page 1, used for double-buffering or additional storage.

---

### BASIC Program Area ($0800-$95FF)

**Layout:**
```
[$0800] → Program Text
          ↓
[VARTAB] → Simple Variables
          ↓
[ARYTAB] → Array Storage
          ↓
[STREND] → String Descriptors
          ↓
[FRETOP] → Free Space
          ↓
[HIMEM]  → String Storage (grows down)
```

**Program Text Format:**

Each line stored as:
```
[Next Line Pointer (2 bytes)]
[Line Number (2 bytes)]
[Tokenized Code (variable length)]
[$00 terminator]
```

**Variable Storage:**

**Simple Variables:**
- Numeric: 7 bytes (name + 5-byte float)
- String: 7 bytes (name + pointer + length)
- Integer: 4 bytes (name + 2-byte int)

**Arrays:**
- Header with dimensions
- Elements stored sequentially

---

### Hi-Res Graphics Pages

#### Hi-Res Page 1 ($2000-$3FFF)

**Resolution:** 280 × 192 pixels (6 colors)

**Layout:**
- Interleaved row ordering (not sequential)
- 8 bytes per row (40 columns)
- Bit patterns determine color

**Memory Organization:**
```
$2000-$27FF: Every 8th row starting at 0 (0, 8, 16, ...)
$2800-$2FFF: Every 8th row starting at 1 (1, 9, 17, ...)
$3000-$37FF: Every 8th row starting at 2 (2, 10, 18, ...)
$3800-$3FFF: Every 8th row starting at 3 (3, 11, 19, ...)
```

**Example:**
```basic
10 REM PLOT PIXEL AT (0,0)
20 POKE 8192, 128: REM $2000, bit 7
```

#### Hi-Res Page 2 ($4000-$5FFF)

Same layout as Page 1, starting at $4000.

---

### I/O Space ($C000-$CFFF)

Memory-mapped I/O. Reading or writing these addresses controls hardware.

#### Soft Switches

**Keyboard:**

| Address | Name | Mode | Function |
|---------|------|------|----------|
| $C000 | KBD | R | Read keyboard data |
| $C010 | KBDSTRB | R/W | Clear keyboard strobe |

**Speaker:**

| Address | Name | Mode | Function |
|---------|------|------|----------|
| $C030 | SPKR | R/W | Toggle speaker |

**Graphics Modes:**

| Address | Name | Mode | Function |
|---------|------|------|----------|
| $C050 | TXTCLR | W | Clear text (graphics) |
| $C051 | TXTSET | W | Set text mode |
| $C052 | MIXCLR | W | Full screen graphics |
| $C053 | MIXSET | W | Mixed text/graphics |
| $C054 | LOWSCR | W | Display page 1 |
| $C055 | HISCR | W | Display page 2 |
| $C056 | LORES | W | Lo-res graphics |
| $C057 | HIRES | W | Hi-res graphics |

**Example:**
```basic
10 REM TOGGLE SPEAKER
20 X = PEEK(49200): REM $C030
30 REM SPEAKER CLICKS
```

**Paddles/Joystick:**

| Address | Name | Mode | Function |
|---------|------|------|----------|
| $C061-$C063 | PDL | R | Read paddle position |
| $C070 | PTRIG | R/W | Trigger paddle timers |

**Other I/O:**

| Address Range | Device |
|---------------|--------|
| $C080-$C08F | Language card control |
| $C090-$C0FF | Slot I/O space |

---

### ROM: Applesoft BASIC ($D000-$F7FF)

**Size:** 10KB

**Contents:**
- Applesoft BASIC interpreter
- BASIC keywords and routines
- Floating-point math library
- String handling
- Line editor

**Key Entry Points:**

| Address | Routine | Purpose |
|---------|---------|---------|
| $D000 | - | Applesoft cold start |
| $D003 | - | Applesoft warm start |
| $E000 | - | Floating-point routines |

**Note:** In this emulator, ROM is not directly mapped. BASIC is implemented in C#.

---

### ROM: Monitor ($F800-$FFFF)

**Size:** 2KB

**Contents:**
- Apple II Monitor program
- Machine code routines
- System vectors at $FFFA-$FFFF

**System Vectors:**

| Address | Vector | Purpose |
|---------|--------|---------|
| $FFFA-$FFFB | NMI | Non-maskable interrupt |
| $FFFC-$FFFD | RESET | Reset vector |
| $FFFE-$FFFF | IRQ/BRK | Interrupt request |

---

## Memory Configuration

### HIMEM and LOMEM

**HIMEM:** High memory limit (top of available memory)
```basic
10 PRINT HIMEM:        : REM DISPLAY HIMEM
20 HIMEM: 32767        : REM SET HIMEM TO 32767
```

**LOMEM:** Low memory limit (bottom of available memory)
```basic
10 PRINT LOMEM:        : REM DISPLAY LOMEM
20 LOMEM: 2048         : REM SET LOMEM TO 2048
```

**Purpose:**
- Reserve memory for machine code
- Protect areas from BASIC
- Control available memory

---

## Memory Access from BASIC

### PEEK - Read Byte

```basic
10 X = PEEK(1024)      : REM READ FROM $0400
20 PRINT X
```

### POKE - Write Byte

```basic
10 POKE 1024, 65       : REM WRITE 'A' TO $0400
```

### Valid Range

- Addresses: 0-65535 ($0000-$FFFF)
- Values: 0-255 (single byte)
- Bounds checking prevents invalid access

---

## Common Memory Operations

### Clear Screen

```basic
10 REM CLEAR TEXT SCREEN
20 FOR I = 1024 TO 2047
30 POKE I, 160: REM SPACE CHARACTER
40 NEXT I
```

### Read/Write Variables

```basic
10 REM STORE VALUE IN PAGE 3
20 POKE 768, 42
30 REM READ IT BACK
40 X = PEEK(768)
50 PRINT X
```

### Check Available Memory

```basic
10 PRINT FRE(0)
20 PRINT "LOMEM:"; LOMEM:
30 PRINT "HIMEM:"; HIMEM:
```

---

## Memory Safety

### Emulated Environment

This interpreter emulates memory:
- All addresses are simulated
- Cannot access your computer's actual memory
- Safe to experiment with POKE

### Bounds Checking

- Invalid addresses generate errors
- Cannot overflow memory
- Protects against crashes

### Read-Only Regions

Some regions are read-only:
- ROM areas ($D000-$FFFF)
- Attempting to POKE generates warning

---

## Hexadecimal Notation

Apple II documentation uses hexadecimal (base 16):

**Conversion:**
```
Decimal   Hex
0         $00
255       $FF
1024      $0400
8192      $2000
49152     $C000
```

**In BASIC:**
```basic
10 REM DECIMAL ONLY IN APPLESOFT
20 X = PEEK(49152): REM $C000 IN DECIMAL
```

---

## Related Topics

- **[6502 Emulation](6502-Emulation)** - CPU and memory details
- **[Language Reference](Language-Reference)** - PEEK, POKE, CALL commands
- **[Sample Programs](Sample-Programs)** - memory.bas example

## External Resources

- [Apple II Memory Map](https://apple2history.org/appendices/a2maps/)
- [Understanding the Apple II](https://archive.org/details/Understanding_the_Apple_II_1981_Quality_Software)
- [6502 Memory Addressing](http://www.6502.org/tutorials/6502opcodes.html)
