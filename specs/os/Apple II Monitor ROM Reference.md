# Apple II Monitor ROM Reference

## Overview

The Monitor is the lowest-level system software in the Apple II, residing in ROM at $F800-$FFFF. It provides:
- Basic I/O routines (keyboard input, screen output)
- Memory examination and modification
- Mini-assembler and disassembler
- Graphics primitives
- Floating-point support vectors

The Monitor is always available regardless of what higher-level software (BASIC, DOS) is running.

---

## Memory Map

```
$F800-$F881   Lo-res graphics routines
$F882-$FAFF   Various support routines
$FB00-$FBFF   Screen utilities, keyboard, paddle
$FC00-$FCFF   Screen I/O, cursor movement, scrolling
$FD00-$FDFF   Character I/O, line input
$FE00-$FEFF   Video mode, tape I/O
$FF00-$FF58   Hi-res graphics routines (IIe)
$FF59-$FFF9   Various routines
$FFFA-$FFFF   6502 vectors (NMI, RESET, IRQ)
```

---

## Zero Page Locations Used by Monitor

| Address | Name    | Description |
|---------|---------|-------------|
| $20     | WNDLFT  | Window left edge (0-39) |
| $21     | WNDWDTH | Window width (1-40) |
| $22     | WNDTOP  | Window top edge (0-23) |
| $23     | WNDBTM  | Window bottom edge (1-24) |
| $24     | CH      | Cursor horizontal position |
| $25     | CV      | Cursor vertical position |
| $26-$27 | GPTS    | General purpose temp storage |
| $28-$29 | BARONE  | Screen base address (low) |
| $2A-$2B | H2      | Hi-res cursor position |
| $2C     | LMNEM   | Disassembler mnemonic |
| $2D     | RMNEM   | Disassembler mnemonic |
| $2E     | PCL     | Program counter low (for stepping) |
| $2F     | PCH     | Program counter high |
| $30     | A1L     | General pointer low |
| $31     | A1H     | General pointer high |
| $32     | A2L     | General pointer low |
| $33     | A2H     | General pointer high |
| $34     | A3L     | General pointer low |
| $35     | A3H     | General pointer high |
| $36     | A4L     | General pointer low |
| $37     | A4H     | General pointer high |
| $38-$3B | Temp    | Temporary storage |
| $3C     | A5L     | General pointer low |
| $3D     | A5H     | General pointer high |
| $3E-$3F | Temp    | Temporary storage |
| $45     | ACC     | Saved accumulator |
| $46     | XREG    | Saved X register |
| $47     | YREG    | Saved Y register |
| $48     | STATUS  | Saved processor status |
| $49     | SPNT    | Saved stack pointer |
| $4E     | RPTS    | Key repeat counter |
| $4F     | KYBTS   | Keyboard/video state |

---

## Character Output Routines

### COUT ($FDED) - Character Output

**Purpose:** Output a character to the current output device.

**Input:**
- A = Character to output (high bit set for normal display)

**Output:**
- Character displayed or sent to current output hook
- A, X, Y modified

**Usage:**
```assembly
        LDA #$C1        ; 'A' with high bit set
        JSR $FDED       ; Output character
```

**Notes:**
- Handles control characters (CR, LF, etc.)
- Goes through CSW vector ($36-$37) for I/O redirection
- For inverse video, OR character with $00-$3F range

### COUT1 ($FDF0) - Direct Character Output

**Purpose:** Output character directly to screen (bypasses CSW hook).

**Input:**
- A = Character to output

**Output:**
- Character placed at cursor position
- Cursor advances

**Usage:**
```assembly
        LDA #$C1        ; 'A'
        JSR $FDF0       ; Direct screen output
```

### CROUT ($FD8E) - Output Carriage Return

**Purpose:** Output a carriage return (move to start of next line).

**Input:** None

**Output:**
- Cursor moves to beginning of next line
- Screen scrolls if at bottom

**Usage:**
```assembly
        JSR $FD8E       ; New line
```

### CROUT1 ($FD8B) - Output CR After Clearing to EOL

**Purpose:** Clear to end of line, then output carriage return.

**Input:** None

**Output:**
- Line cleared from cursor to right edge
- Cursor moves to beginning of next line

---

## Character Input Routines

### RDKEY ($FD0C) - Read Keyboard

**Purpose:** Wait for and return a keypress.

**Input:** None

**Output:**
- A = Character pressed (high bit set)

**Usage:**
```assembly
        JSR $FD0C       ; Wait for key
        AND #$7F        ; Clear high bit
        CMP #$1B        ; ESC key?
```

**Notes:**
- Flashes cursor while waiting
- Goes through KSW vector ($38-$39) for input redirection

### RDCHAR ($FD35) - Read Character with Escape Handling

**Purpose:** Read character, handling ESC sequences.

**Input:** None

**Output:**
- A = Character (processed for ESC sequences)

**Notes:**
- ESC followed by A-Z provides control characters
- Used by line input routines

### KEYIN ($FD1B) - Low-Level Key Input

**Purpose:** Direct keyboard read without cursor flashing.

**Input:** None

**Output:**
- A = Key code if available
- Waits for keypress

---

## Line Input Routines

### GETLN ($FD67) - Get Line of Input

**Purpose:** Input a line of text with prompt character.

**Input:**
- PROMPT ($33) = Prompt character

**Output:**
- Input buffer at $0200-$02FF
- X = Length of input (not including CR)

**Usage:**
```assembly
        LDA #$BA        ; ':' prompt
        STA $33
        JSR $FD67       ; Get line
        ; Input now at $0200, length in X
```

### GETLN1 ($FD6A) - Get Line Without Prompt

**Purpose:** Input a line without displaying prompt.

**Input:** None

**Output:**
- Input buffer at $0200-$02FF
- X = Length of input

### GETLNZ ($FD6F) - Get Line with Clear

**Purpose:** Clear line first, then get input.

**Input:** None

**Output:**
- Input buffer at $0200-$02FF
- X = Length of input

---

## Numeric Output Routines

### PRBYTE ($FDDA) - Print Byte as Hex

**Purpose:** Print accumulator as two hex digits.

**Input:**
- A = Byte to print

**Output:**
- Two hex digits displayed
- A modified

**Usage:**
```assembly
        LDA #$3F
        JSR $FDDA       ; Prints "3F"
```

### PRHEX ($FDE3) - Print Low Nibble as Hex

**Purpose:** Print low nibble of A as one hex digit.

**Input:**
- A = Value (low 4 bits used)

**Output:**
- One hex digit displayed

**Usage:**
```assembly
        LDA #$0C
        JSR $FDE3       ; Prints "C"
```

### PRNTAX ($F941) - Print A and X as Hex

**Purpose:** Print A and X registers as 4 hex digits.

**Input:**
- A = High byte
- X = Low byte

**Output:**
- Four hex digits displayed

---

## Screen Control Routines

### HOME ($FC58) - Clear Screen and Home Cursor

**Purpose:** Clear the text screen and position cursor at top-left.

**Input:** None

**Output:**
- Screen cleared
- Cursor at position 0,0

**Usage:**
```assembly
        JSR $FC58       ; Clear screen
```

### CLREOL ($FC9C) - Clear to End of Line

**Purpose:** Clear from cursor position to right edge of window.

**Input:**
- CH ($24) = Current cursor column
- CV ($25) = Current cursor row

**Output:**
- Line cleared from cursor to right edge

### CLREOP ($FC42) - Clear to End of Page

**Purpose:** Clear from cursor to bottom of screen.

**Input:**
- Cursor position

**Output:**
- Screen cleared from cursor to bottom

### VTAB ($FC22) - Vertical Tab

**Purpose:** Move cursor to specified line.

**Input:**
- A = Line number (0-23)

**Output:**
- CV updated
- Base address calculated

**Usage:**
```assembly
        LDA #$0C        ; Line 12
        JSR $FC22       ; Move to line 12
```

### HTAB - Horizontal Tab

**Note:** There is no HTAB routine in Monitor. Set CH ($24) directly:
```assembly
        LDA #$14        ; Column 20
        STA $24         ; Set horizontal position
```

### SCROLL ($FC70) - Scroll Screen Up

**Purpose:** Scroll entire window up one line.

**Input:**
- Window parameters at $20-$23

**Output:**
- Screen scrolled
- Bottom line cleared

---

## Video Mode Routines

### SETGR ($FB40) - Set Lo-Res Graphics Mode

**Purpose:** Enable lo-res graphics with mixed text.

**Input:** None

**Output:**
- Lo-res graphics mode enabled
- Mixed mode (4 lines text at bottom)
- Screen cleared to black

**Soft switches activated:**
- $C050 (graphics)
- $C053 (mixed)
- $C056 (lo-res)

### SETGR2 ($FB39) - Set Full-Screen Lo-Res

**Purpose:** Enable full-screen lo-res graphics.

**Input:** None

**Output:**
- Full-screen lo-res mode
- No text window

### SETTXT ($FB39) - Set Text Mode

**Purpose:** Return to text mode.

**Input:** None

**Output:**
- Text mode enabled
- Screen cleared

### CLRSCR ($F832) - Clear Lo-Res Screen

**Purpose:** Clear lo-res screen to black.

**Input:** None

**Output:**
- Lo-res portion cleared (top 40 lines)

### CLRTOP ($F836) - Clear Top of Lo-Res Screen

**Purpose:** Clear top portion of lo-res screen.

**Input:** None

**Output:**
- Top 40 lines cleared

---

## Lo-Res Graphics Routines

### PLOT ($F800) - Plot Lo-Res Point

**Purpose:** Plot a single lo-res block.

**Input:**
- A = Color (0-15)
- Y = X coordinate (0-39)
- TEMP ($2C) or CV = Y coordinate (0-47)

**Output:**
- Point plotted

**Usage:**
```assembly
        LDA #$01        ; Red
        JSR $F864       ; Set color
        LDA #$14        ; Y = 20
        LDY #$14        ; X = 20
        JSR $F800       ; Plot point
```

### HLINE ($F819) - Draw Horizontal Line

**Purpose:** Draw horizontal lo-res line.

**Input:**
- Y = Starting X position
- A = Ending X position (stored in H2 at $2C)
- V = Y position (stored via PLOT setup)
- Color previously set

**Output:**
- Horizontal line drawn

### VLINE ($F828) - Draw Vertical Line

**Purpose:** Draw vertical lo-res line.

**Input:**
- A = Ending Y position (stored in V2 at $2D)
- Y = X position
- Starting Y previously set
- Color previously set

**Output:**
- Vertical line drawn

### SETCOL ($F864) - Set Lo-Res Color

**Purpose:** Set the current drawing color.

**Input:**
- A = Color (0-15)

**Output:**
- Color stored for subsequent drawing operations

**Color Values:**
| Value | Color |
|-------|-------|
| 0     | Black |
| 1     | Magenta/Red |
| 2     | Dark Blue |
| 3     | Purple |
| 4     | Dark Green |
| 5     | Grey 1 |
| 6     | Medium Blue |
| 7     | Light Blue |
| 8     | Brown |
| 9     | Orange |
| 10    | Grey 2 |
| 11    | Pink |
| 12    | Green |
| 13    | Yellow |
| 14    | Aqua |
| 15    | White |

### SCRN ($F871) - Read Lo-Res Screen

**Purpose:** Read color value at screen position.

**Input:**
- Y = X coordinate
- A = Y coordinate

**Output:**
- A = Color at that position (0-15)

---

## Hi-Res Graphics Routines

### HGR ($F3E2) - Set Hi-Res Page 1

**Purpose:** Enable hi-res graphics, page 1.

**Input:** None

**Output:**
- Hi-res mode enabled
- Page 1 displayed
- Screen cleared to black

### HGR2 ($F3D8) - Set Hi-Res Page 2

**Purpose:** Enable hi-res graphics, page 2.

**Input:** None

**Output:**
- Hi-res mode enabled
- Page 2 displayed
- Screen cleared to black

### HCLR ($F3F2) - Clear Hi-Res Screen

**Purpose:** Clear current hi-res page.

**Input:** None

**Output:**
- Hi-res screen cleared to current color

### HCOLOR ($F6F0) - Set Hi-Res Color

**Purpose:** Set drawing color for hi-res graphics.

**Input:**
- X = Color (0-7)

**Output:**
- Color set for subsequent operations

**Color Values:**
| Value | Color |
|-------|-------|
| 0     | Black 1 |
| 1     | Green |
| 2     | Violet |
| 3     | White 1 |
| 4     | Black 2 |
| 5     | Orange |
| 6     | Blue |
| 7     | White 2 |

### HPLOT ($F457) - Plot Hi-Res Point

**Purpose:** Plot a point on hi-res screen.

**Input:**
- Y,X = X coordinate (0-279): Y=high, X=low
- A = Y coordinate (0-191)

**Output:**
- Point plotted

**Usage:**
```assembly
        LDA #$00        ; X high byte
        LDY #$8C        ; X low = 140
        LDX #$60        ; Y = 96 (center)
        JSR $F457       ; Plot point at 140,96
```

### HPLOT TO ($F53A) - Draw Line To Point

**Purpose:** Draw line from current position to new position.

**Input:**
- Same as HPLOT for destination

**Output:**
- Line drawn from previous position

---

## Paddle/Joystick Routines

### PREAD ($FB1E) - Read Paddle

**Purpose:** Read analog paddle value.

**Input:**
- X = Paddle number (0-3)

**Output:**
- Y = Paddle value (0-255)

**Usage:**
```assembly
        LDX #$00        ; Paddle 0
        JSR $FB1E       ; Read paddle
        STY POSITION    ; Store value
```

**Timing:** This routine takes variable time based on paddle position (~11 + 12*Y cycles).

### PREAD4 - Read All Paddles

**Note:** No single routine reads all 4. Loop through PREAD.

---

## Delay Routines

### WAIT ($FCA8) - Delay Loop

**Purpose:** Delay for a specified duration.

**Input:**
- A = Delay value (larger = longer delay)

**Output:**
- Returns after delay
- A = 0

**Timing:** Approximately (A × (A×2 + 3) / 2) cycles

**Usage:**
```assembly
        LDA #$80        ; Medium delay
        JSR $FCA8
```

### WAIT2 ($FCCF) - Short Fixed Delay

**Purpose:** Very short delay.

**Input:** None

**Output:**
- Brief delay (~14 cycles)

---

## Memory Routines

### MOVE ($FE2C) - Move Memory Block

**Purpose:** Copy a block of memory.

**Input:**
- A1L/A1H ($3C-$3D) = Source start
- A2L/A2H ($3E-$3F) = Source end
- A4L/A4H ($42-$43) = Destination

**Output:**
- Memory copied

**Usage:**
```assembly
        LDA #<SOURCE
        STA $3C
        LDA #>SOURCE
        STA $3D
        LDA #<SOURCE_END
        STA $3E
        LDA #>SOURCE_END
        STA $3F
        LDA #<DEST
        STA $42
        LDA #>DEST
        STA $43
        LDY #$00
        JSR $FE2C
```

### VERIFY ($FE36) - Verify Memory

**Purpose:** Compare two memory regions.

**Input:**
- Same as MOVE

**Output:**
- Z flag set if equal

---

## Monitor Entry Points

### MON ($FF65) - Enter Monitor

**Purpose:** Enter the Apple II Monitor.

**Input:** None

**Output:**
- Monitor prompt displayed
- Awaits commands

### MONZ ($FF69) - Enter Monitor (Cold)

**Purpose:** Cold entry to Monitor with register display.

**Input:** None

**Output:**
- Registers displayed
- Monitor prompt

### RESET Entry ($FF59)

**Purpose:** System reset entry point.

**Notes:**
- Vector at $FFFC points here
- Initializes system and enters Monitor or BASIC depending on configuration

---

## System Vectors

| Address | Vector | Description |
|---------|--------|-------------|
| $FFFA   | NMI    | Non-maskable interrupt vector |
| $FFFC   | RESET  | Reset vector |
| $FFFE   | IRQ    | Interrupt request/BRK vector |

### Soft Vectors (in Page 3)

| Address | Name   | Description |
|---------|--------|-------------|
| $03D0   | DOS WS | DOS warm start |
| $03D3   | DOS CS | DOS cold start |
| $03EA   | &HOOK  | Ampersand vector |
| $03F0   | BRKV   | BRK handler vector |
| $03F2   | SOFTEV | Soft reset vector |
| $03F4   | PWREDUP| Power-up byte ($A5 = valid) |
| $03F5   | &VEC   | Ampersand alternate |
| $03F8   | USRVEC | USR function vector |
| $03FE   | IRQV   | IRQ handler vector |

---

## I/O Hooks

### CSW ($36-$37) - Character Output Hook

**Purpose:** Vector for character output.

**Default:** Points to COUT1

**Usage:** Set to custom routine for output redirection (printer, serial, etc.)

### KSW ($38-$39) - Character Input Hook

**Purpose:** Vector for character input.

**Default:** Points to KEYIN

**Usage:** Set to custom routine for input redirection

---

## IIe Enhanced vs IIc Differences

| Feature | IIe Enhanced | IIc |
|---------|--------------|-----|
| ROM version | 342-0304-A | 342-0445-A (or later) |
| Hi-res routines | Standard | Standard |
| 80-column support | In auxiliary ROM | Built into main ROM |
| Self-test | PR#7 | Ctrl-OpenApple-Reset |
| MouseText | In 80-col ROM | Built-in |

### Additional IIc Monitor Entry Points

| Address | Name | Description |
|---------|------|-------------|
| $C300   | 80COL| 80-column firmware entry |
| $C400   | MOUSE| Mouse firmware entry |

---

## Quick Reference Table

| Address | Name    | Function |
|---------|---------|----------|
| $F800   | PLOT    | Plot lo-res point |
| $F819   | HLINE   | Draw horizontal line |
| $F828   | VLINE   | Draw vertical line |
| $F832   | CLRSCR  | Clear lo-res screen |
| $F836   | CLRTOP  | Clear top lo-res |
| $F864   | SETCOL  | Set lo-res color |
| $F871   | SCRN    | Read lo-res point |
| $FB1E   | PREAD   | Read paddle |
| $FB2F   | INIT    | Initialize screen |
| $FB39   | SETTXT  | Set text mode |
| $FB40   | SETGR   | Set lo-res graphics |
| $FBF4   | VERSION | ROM version byte |
| $FC10   | PRBLNK  | Print 3 blanks |
| $FC22   | VTAB    | Vertical tab |
| $FC42   | CLREOP  | Clear to end of page |
| $FC58   | HOME    | Clear screen/home cursor |
| $FC62   | CR      | Output carriage return |
| $FC66   | LF      | Output line feed |
| $FC70   | SCROLL  | Scroll screen up |
| $FC9C   | CLREOL  | Clear to end of line |
| $FCA8   | WAIT    | Delay loop |
| $FD0C   | RDKEY   | Read keyboard |
| $FD1B   | KEYIN   | Direct key input |
| $FD35   | RDCHAR  | Read with ESC handling |
| $FD67   | GETLN   | Get line input |
| $FD6A   | GETLN1  | Get line (no prompt) |
| $FD6F   | GETLNZ  | Get line (with clear) |
| $FD8E   | CROUT   | Output CR |
| $FDDA   | PRBYTE  | Print byte as hex |
| $FDE3   | PRHEX   | Print nibble as hex |
| $FDED   | COUT    | Character output |
| $FDF0   | COUT1   | Direct screen output |
| $FE2C   | MOVE    | Move memory |
| $FE80   | SETINV  | Set inverse mode |
| $FE84   | SETNORM | Set normal mode |
| $FE89   | SETKBD  | Set keyboard input |
| $FE93   | SETVID  | Set screen output |
| $FF59   | RESET   | Reset handler |
| $FF65   | MON     | Enter monitor |
| $FF69   | MONZ    | Monitor cold entry |

---

## Document History

| Version | Date       | Changes |
|---------|------------|---------|
| 1.0     | 2025-12-30 | Initial specification |
