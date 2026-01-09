# Applesoft BASIC ROM Reference

## Overview

Applesoft BASIC is Microsoft BASIC adapted for the Apple II, residing in ROM at $D000-$F7FF (12KB). It provides:
- Full-featured BASIC interpreter
- Floating-point math (5-byte format)
- Graphics commands (GR, HGR, PLOT, etc.)
- String handling
- File I/O hooks for DOS/ProDOS integration

---

## Memory Map

```
$D000-$D3FF   BASIC cold/warm start, error handling
$D400-$D7FF   Tokenizer, line editing
$D800-$DBFF   Expression evaluation
$DC00-$DFFF   Variable management
$E000-$E3FF   FOR/NEXT, GOSUB/RETURN
$E400-$E7FF   INPUT, DATA, READ
$E800-$EBFF   PRINT, string functions
$EC00-$EFFF   Math routines (add, subtract)
$F000-$F3FF   Math routines (multiply, divide)
$F400-$F7FF   Math functions (SQR, LOG, SIN, etc.)
```

---

## Zero Page Locations ($00-$FF)

### Interpreter State

| Address | Name     | Description |
|---------|----------|-------------|
| $00-$01 | USRVEC   | USR function vector |
| $02-$03 | CHARONE  | Search character |
| $06-$07 | Temp     | Temporary storage |
| $08     | GCESSION | String pointer temp |
| $09     | GCESSION+1 | String pointer temp |
| $0A-$0B | Temp     | Expression temp |
| $0D     | Temp     | Temporary |
| $0E     | Temp     | Temporary |
| $0F     | Temp     | Temporary |
| $10     | REMESSION| Remaining session |
| $50-$51 | TXTPTR   | BASIC text pointer (current position in program) |
| $67-$68 | TXTTAB   | Start of BASIC program |
| $69-$6A | VARTAB   | Start of simple variables |
| $6B-$6C | ARYTAB   | Start of array variables |
| $6D-$6E | STREND   | End of array variables |
| $6F-$70 | FRETOP   | Top of string free space |
| $71-$72 | FRESPC   | String pointer temp |
| $73-$74 | MEMSIZ   | Top of BASIC memory (HIMEM) |
| $75-$76 | CURLIN   | Current line number ($FFFF = immediate mode) |
| $77-$78 | OLDLIN   | Previous line number (for CONT) |
| $79-$7A | OLDTXT   | Previous text pointer (for CONT) |
| $7B-$7C | DATLIN   | Line number of current DATA |
| $7D-$7E | DATPTR   | Pointer to current DATA |
| $7F-$80 | INPPTR   | Pointer to INPUT buffer |
| $81-$82 | VARNAM   | Variable name (first 2 chars) |
| $83-$84 | VARPNT   | Pointer to variable value |
| $85-$86 | FORPNT   | FOR variable pointer |
| $87     | TXPSAVE  | Temp storage |
| $9A     | SUBFLG   | Subscript flag |
| $9D     | TPTS     | FOR loop type |
| $A0-$A1 | LINNUM   | Line number for LIST, etc. |
| $A2-$A3 | TEMPPT   | String descriptor temp |
| $A4-$A5 | LASTPT   | Last string pointer |
| $A6-$A8 | Temp     | String temps |
| $A9-$AD | TEMP1-3  | Expression temps |

### Floating-Point Accumulator (FAC)

| Address | Name   | Description |
|---------|--------|-------------|
| $9D     | FACSGN | FAC sign byte |
| $9E     | FAC    | FAC exponent |
| $9F-$A2 | FAC+1  | FAC mantissa (4 bytes) |
| $A3     | Temp   | Extension byte |
| $A4     | ARGSGN | ARG sign byte |
| $A5     | ARG    | ARG exponent |
| $A6-$A9 | ARG+1  | ARG mantissa (4 bytes) |

The FAC (Floating-point ACcumulator) and ARG are the two working registers for floating-point math.

---

## Page 3 Vectors

| Address | Name    | Description |
|---------|---------|-------------|
| $03D0   | DOSWS   | DOS warm start JMP |
| $03D3   | DOSCS   | DOS cold start JMP |
| $03F0   | BRKV    | BRK vector (used by STOP) |
| $03F5   | AMPVEC  | Ampersand (&) vector |

---

## Error Codes

| Code | Message | Description |
|------|---------|-------------|
| 0    | NEXT WITHOUT FOR | NEXT without matching FOR |
| 16   | SYNTAX ERROR | Syntax error |
| 22   | RETURN WITHOUT GOSUB | RETURN without matching GOSUB |
| 42   | OUT OF DATA | No more DATA to READ |
| 53   | ILLEGAL QUANTITY | Value out of range |
| 69   | OVERFLOW | Number too large |
| 77   | OUT OF MEMORY | Memory exhausted |
| 90   | UNDEF'D STATEMENT | Line number not found |
| 107  | BAD SUBSCRIPT | Array index out of bounds |
| 120  | REDIM'D ARRAY | Array already dimensioned |
| 133  | DIVISION BY ZERO | Division by zero |
| 163  | TYPE MISMATCH | String/number mismatch |
| 176  | STRING TOO LONG | String > 255 characters |
| 191  | FORMULA TOO COMPLEX | Expression too deep |
| 224  | UNDEF'D FUNCTION | DEF FN not found |
| 254  | BREAK | Ctrl-C pressed |

---

## Entry Points - Program Control

### COLD ($E000) - Cold Start

**Purpose:** Initialize BASIC and clear program memory.

**Input:** None

**Output:**
- All variables cleared
- BASIC ready prompt

**Usage:** Called on system reset or NEW command.

### WARM ($D365/RESTART) - Warm Start

**Purpose:** Re-enter BASIC preserving program.

**Input:** None

**Output:**
- BASIC ready prompt
- Program preserved

**Entry Point:** $D365

### RUN - Run Program

**Entry Point:** $D566

**Purpose:** Execute BASIC program from beginning.

**Input:** Optional line number in LINNUM ($A0-$A1)

### NEW - Clear Program

**Entry Point:** $D64B

**Purpose:** Clear program memory and all variables.

### CLR - Clear Variables

**Entry Point:** $D66A

**Purpose:** Clear all variables without affecting program.

### CONT ($D896) - Continue

**Purpose:** Continue execution after STOP or Ctrl-C.

**Input:**
- OLDLIN ($77-$78) = Line to continue from
- OLDTXT ($79-$7A) = Text pointer position

---

## Entry Points - Input/Output

### PRINT - Output Value

**Entry Point:** $DFE3

**Purpose:** PRINT statement handler.

### STROUT ($E10C) - Print String

**Purpose:** Output a null-terminated string.

**Input:**
- Y,A = Pointer to string (Y=high, A=low)

**Output:**
- String printed

**Usage:**
```assembly
        LDY #>MESSAGE
        LDA #<MESSAGE
        JSR $E10C
```

### LINPRT ($E2F2) - Print Line Number

**Purpose:** Print the integer in X,A as a decimal number.

**Input:**
- A = High byte
- X = Low byte

**Output:**
- Number printed

**Usage:**
```assembly
        LDA CURLIN+1
        LDX CURLIN
        JSR $E2F2       ; Print current line number
```

### INPUT - Get Input

**Entry Point:** $E752

**Purpose:** INPUT statement handler.

### INLIN ($D52E) - Input Line

**Purpose:** Input a line to buffer.

**Input:** None

**Output:**
- Line in input buffer at $0200

---

## Entry Points - String Functions

### LEN - String Length

**Entry Point:** $E20E

**Purpose:** Return length of string.

### LEFT$ - Left Substring

**Entry Point:** $E2E7

### RIGHT$ - Right Substring

**Entry Point:** $E2ED

### MID$ - Middle Substring

**Entry Point:** $E2F3

### CHR$ - Character from Code

**Entry Point:** $E07B

### ASC - Code from Character

**Entry Point:** $E089

### STR$ - Number to String

**Entry Point:** $E3C5

### VAL - String to Number

**Entry Point:** $E707

---

## Entry Points - Mathematical Functions

### ABS - Absolute Value

**Entry Point:** $EBAF

**Purpose:** ABS(X) - Return absolute value.

**Input:** FAC contains value

**Output:** FAC contains |value|

### SGN - Sign Function

**Entry Point:** $EB82

**Purpose:** SGN(X) - Return sign (-1, 0, +1).

**Input:** FAC contains value

**Output:** FAC contains -1, 0, or +1

### INT - Integer Part

**Entry Point:** $EC11

**Purpose:** INT(X) - Return integer part (floor).

**Input:** FAC contains value

**Output:** FAC contains floor(value)

### SQR ($EE8A) - Square Root

**Purpose:** SQR(X) - Calculate square root.

**Input:** FAC contains value

**Output:** FAC contains square root

### LOG ($E2B8) - Natural Logarithm

**Purpose:** LOG(X) - Calculate natural log.

**Input:** FAC contains positive value

**Output:** FAC contains ln(value)

**Error:** ILLEGAL QUANTITY if value ≤ 0

### EXP ($EF09) - Exponential

**Purpose:** EXP(X) - Calculate e^x.

**Input:** FAC contains value

**Output:** FAC contains e^value

### SIN ($EFA8) - Sine

**Purpose:** SIN(X) - Calculate sine (radians).

**Input:** FAC contains angle in radians

**Output:** FAC contains sin(value)

### COS ($EF9F) - Cosine

**Purpose:** COS(X) - Calculate cosine (radians).

**Input:** FAC contains angle in radians

**Output:** FAC contains cos(value)

### TAN ($EFCD) - Tangent

**Purpose:** TAN(X) - Calculate tangent (radians).

**Input:** FAC contains angle in radians

**Output:** FAC contains tan(value)

### ATN ($F03A) - Arctangent

**Purpose:** ATN(X) - Calculate arctangent.

**Input:** FAC contains value

**Output:** FAC contains atan(value) in radians

### RND ($EFAE) - Random Number

**Purpose:** RND(X) - Generate random number.

**Input:** 
- X > 0: Return next random number (0-1)
- X = 0: Return last random number
- X < 0: Seed generator with X

**Output:** FAC contains random number 0 ≤ n < 1

---

## Entry Points - Floating-Point Operations

### Floating-Point Format

Applesoft uses a 5-byte floating-point format:
- Byte 0: Exponent (biased by 128)
- Bytes 1-4: Mantissa (MSB first, normalized with hidden bit)
- Sign in MSB of byte 1

### FADD ($EB63) - Add

**Purpose:** FAC = FAC + ARG

**Input:**
- FAC = First operand
- ARG = Second operand

**Output:**
- FAC = Sum

### FSUB ($EB90) - Subtract

**Purpose:** FAC = ARG - FAC

**Input:**
- FAC = Subtrahend
- ARG = Minuend

**Output:**
- FAC = Difference

### FMULT ($EC23) - Multiply

**Purpose:** FAC = FAC × ARG

**Input:**
- FAC = First operand
- ARG = Second operand

**Output:**
- FAC = Product

### FDIV ($ED36) - Divide

**Purpose:** FAC = ARG ÷ FAC

**Input:**
- FAC = Divisor
- ARG = Dividend

**Output:**
- FAC = Quotient

### FPWRT ($EE97) - Power

**Purpose:** FAC = ARG ^ FAC

**Input:**
- FAC = Exponent
- ARG = Base

**Output:**
- FAC = Result

### NEGOP ($EED0) - Negate FAC

**Purpose:** FAC = -FAC

**Input:** FAC

**Output:** FAC = negated value

### FCOMP ($EBE5) - Compare

**Purpose:** Compare FAC to memory.

**Input:**
- FAC = First value
- Y,A = Pointer to 5-byte float in memory

**Output:**
- A = $FF if FAC < MEM
- A = $00 if FAC = MEM
- A = $01 if FAC > MEM

---

## Entry Points - Conversion

### GIVAYF ($E2F2) - Integer to Float

**Purpose:** Convert 16-bit signed integer to float.

**Input:**
- A = High byte
- Y = Low byte

**Output:**
- FAC = Float value

### AYINT ($E10C) - Float to Integer

**Purpose:** Convert FAC to 16-bit signed integer.

**Input:** FAC

**Output:**
- Result at FAESSION ($A0-$A1)

### FIN ($EAF9) - ASCII to Float

**Purpose:** Convert ASCII string to float.

**Input:**
- TXTPTR ($50-$51) points to string

**Output:**
- FAC = Converted value
- TXTPTR advanced past number

### FOUT ($E752) - Float to ASCII

**Purpose:** Convert FAC to ASCII string.

**Input:** FAC

**Output:**
- String in buffer starting at $0100
- Y,A = Pointer to string

---

## Entry Points - Expression Evaluation

### FRMNUM ($D6A5) - Evaluate Numeric Expression

**Purpose:** Evaluate expression and return number.

**Input:**
- TXTPTR points to expression

**Output:**
- FAC = Result
- TXTPTR advanced

**Error:** TYPE MISMATCH if result is string

### FRMEVL ($D6DA) - Evaluate Expression

**Purpose:** Evaluate any expression (string or numeric).

**Input:**
- TXTPTR points to expression

**Output:**
- FAC = Result (or string descriptor)
- VALTYP flag set: $00=numeric, $FF=string

### CHKNUM ($DD6A) - Check Numeric

**Purpose:** Verify last expression was numeric.

**Error:** TYPE MISMATCH if VALTYP = $FF

### CHKSTR ($DD6C) - Check String

**Purpose:** Verify last expression was string.

**Error:** TYPE MISMATCH if VALTYP = $00

### CHKCOM ($DEBE) - Check for Comma

**Purpose:** Verify next character is comma, skip it.

**Input:** TXTPTR

**Output:** TXTPTR advanced past comma

**Error:** SYNTAX ERROR if no comma

### GETBYT ($E6D8) - Get Byte Value

**Purpose:** Evaluate expression and return byte.

**Input:** TXTPTR

**Output:** X = Value (0-255)

**Error:** ILLEGAL QUANTITY if > 255

### GETADR ($E6E5) - Get Address

**Purpose:** Evaluate expression and return 16-bit value.

**Input:** TXTPTR

**Output:**
- LINNUM ($A0-$A1) = Value

---

## Entry Points - Variables

### VAR ($DD67) - Get Variable

**Purpose:** Get pointer to variable value.

**Input:**
- TXTPTR points to variable name

**Output:**
- VARPNT ($83-$84) points to value
- VARNAM ($81-$82) contains name

### PTRGET ($DFE7) - Find or Create Variable

**Purpose:** Find variable or create if not found.

**Input:**
- TXTPTR points to variable name

**Output:**
- VARPNT points to value
- Variable created if new

### AYINT ($E10C) - Store Integer

**Purpose:** Store integer in variable.

### MOVFM ($EAF9) - Load FAC from Memory

**Purpose:** Copy 5-byte float from memory to FAC.

**Input:**
- Y,A = Pointer to float

**Output:**
- FAC = Value

### MOVMF ($EB2B) - Store FAC to Memory

**Purpose:** Copy FAC to memory.

**Input:**
- Y,X = Pointer to destination

**Output:**
- Memory = FAC value

---

## Entry Points - Graphics

### GR - Lo-Res Graphics

**Entry Point:** $F390

**Purpose:** Set lo-res graphics mode with text window.

**BASIC:** GR

### TEXT - Text Mode

**Entry Point:** $F399

**Purpose:** Return to text mode.

**BASIC:** TEXT

### COLOR= ($F6F0) - Set Lo-Res Color

**Entry Point:** $F6EC

**Purpose:** Set drawing color 0-15.

**Input:** Color value in FAC

**BASIC:** COLOR=n

### PLOT ($F225) - Plot Lo-Res Point

**Entry Point:** $F225

**Purpose:** Plot point at X,Y.

**Input:** X and Y coordinates from evaluation

**BASIC:** PLOT X,Y

### HLIN/VLIN - Draw Lines

**Entry Points:** $F232 (HLIN), $F241 (VLIN)

**Purpose:** Draw horizontal/vertical lines.

**BASIC:** HLIN X1,X2 AT Y / VLIN Y1,Y2 AT X

### SCRN - Read Lo-Res

**Entry Point:** $F267

**Purpose:** Read color at position.

**BASIC:** A=SCRN(X,Y)

### HGR - Hi-Res Page 1

**Entry Point:** $F3E2

**Purpose:** Enable hi-res mode, page 1.

**BASIC:** HGR

### HGR2 - Hi-Res Page 2

**Entry Point:** $F3D8

**Purpose:** Enable hi-res mode, page 2.

**BASIC:** HGR2

### HCOLOR= ($F6E9) - Set Hi-Res Color

**Entry Point:** $F6E9

**Purpose:** Set hi-res drawing color 0-7.

**BASIC:** HCOLOR=n

### HPLOT ($F411) - Hi-Res Plot

**Entry Point:** $F411

**Purpose:** Plot point or draw line on hi-res screen.

**BASIC:** HPLOT X,Y or HPLOT TO X,Y

---

## Entry Points - Sound

### PEEK Location for Speaker

The speaker is toggled by accessing $C030. Applesoft doesn't have a native sound command, but:

```basic
10 S = PEEK(-16336) : REM Toggle speaker
```

Or use machine language for tones.

---

## Entry Points - Miscellaneous

### CALL ($E775) - Machine Language Call

**Purpose:** Execute machine language subroutine.

**Input:** Address in LINNUM ($A0-$A1)

**BASIC:** CALL addr

**Implementation:**
```assembly
        LDA $A0
        PHA
        LDA $A1
        PHA
        LDA #$00        ; Clear A, X, Y
        TAX
        TAY
        PHA             ; Push 0 on stack
        RTS             ; "Return" to user routine
```

### POKE ($E77B) - Store Byte

**Purpose:** Store byte at address.

**BASIC:** POKE addr,value

### PEEK ($E78B) - Read Byte

**Purpose:** Read byte from address.

**BASIC:** A=PEEK(addr)

**Output:** Result in FAC

### WAIT ($E784) - Wait for Memory

**Purpose:** Wait for memory location to match pattern.

**BASIC:** WAIT addr,mask[,invert]

**Operation:**
1. Read memory at addr
2. XOR with invert (default 0)
3. AND with mask
4. If result = 0, repeat

### USR - User Function

**Entry Point:** Via vector at $00-$01

**Purpose:** Call machine language function.

**Input:** FAC = Argument

**Output:** FAC = Result

**BASIC:** X=USR(arg)

---

## Token Table

Applesoft tokenizes keywords to single bytes ($80-$FF):

| Token | Keyword | Token | Keyword |
|-------|---------|-------|---------|
| $80   | END     | $81   | FOR     |
| $82   | NEXT    | $83   | DATA    |
| $84   | INPUT   | $85   | DEL     |
| $86   | DIM     | $87   | READ    |
| $88   | GR      | $89   | TEXT    |
| $8A   | PR#     | $8B   | IN#     |
| $8C   | CALL    | $8D   | PLOT    |
| $8E   | HLIN    | $8F   | VLIN    |
| $90   | HGR2    | $91   | HGR     |
| $92   | HCOLOR= | $93   | HPLOT   |
| $94   | DRAW    | $95   | XDRAW   |
| $96   | HTAB    | $97   | HOME    |
| $98   | ROT=    | $99   | SCALE=  |
| $9A   | SHLOAD  | $9B   | TRACE   |
| $9C   | NOTRACE | $9D   | NORMAL  |
| $9E   | INVERSE | $9F   | FLASH   |
| $A0   | COLOR=  | $A1   | POP     |
| $A2   | VTAB    | $A3   | HIMEM:  |
| $A4   | LOMEM:  | $A5   | ONERR   |
| $A6   | RESUME  | $A7   | RECALL  |
| $A8   | STORE   | $A9   | SPEED=  |
| $AA   | LET     | $AB   | GOTO    |
| $AC   | RUN     | $AD   | IF      |
| $AE   | RESTORE | $AF   | &       |
| $B0   | GOSUB   | $B1   | RETURN  |
| $B2   | REM     | $B3   | STOP    |
| $B4   | ON      | $B5   | WAIT    |
| $B6   | LOAD    | $B7   | SAVE    |
| $B8   | DEF     | $B9   | POKE    |
| $BA   | PRINT   | $BB   | CONT    |
| $BC   | LIST    | $BD   | CLEAR   |
| $BE   | GET     | $BF   | NEW     |
| $C0   | TAB(    | $C1   | TO      |
| $C2   | FN      | $C3   | SPC(    |
| $C4   | THEN    | $C5   | AT      |
| $C6   | NOT     | $C7   | STEP    |
| $C8   | +       | $C9   | -       |
| $CA   | *       | $CB   | /       |
| $CC   | ^       | $CD   | AND     |
| $CE   | OR      | $CF   | >       |
| $D0   | =       | $D1   | <       |
| $D2   | SGN     | $D3   | INT     |
| $D4   | ABS     | $D5   | USR     |
| $D6   | FRE     | $D7   | SCRN(   |
| $D8   | PDL     | $D9   | POS     |
| $DA   | SQR     | $DB   | RND     |
| $DC   | LOG     | $DD   | EXP     |
| $DE   | COS     | $DF   | SIN     |
| $E0   | TAN     | $E1   | ATN     |
| $E2   | PEEK    | $E3   | LEN     |
| $E4   | STR$    | $E5   | VAL     |
| $E6   | ASC     | $E7   | CHR$    |
| $E8   | LEFT$   | $E9   | RIGHT$  |
| $EA   | MID$    | $EB   | (unused)|

---

## BASIC Program Format

A BASIC program is stored as a linked list:

```
+0,+1   Link to next line (or $0000 for end)
+2,+3   Line number (16-bit)
+4...   Tokenized line content
+n      $00 (end of line marker)
```

Example:
```
10 PRINT "HELLO"
```
Stored as:
```
$0801: $09 $08       ; Link to next line ($0809)
$0803: $0A $00       ; Line number 10
$0805: $BA           ; PRINT token
$0806: $22           ; "
$0807: $48 $45 $4C $4C $4F  ; HELLO
$080C: $22           ; "
$080D: $00           ; End of line
$080E: $00 $00       ; End of program (link = 0)
```

---

## IIe Enhanced vs IIc Differences

| Feature | IIe Enhanced | IIc |
|---------|--------------|-----|
| ROM location | $D000-$F7FF | $D000-$F7FF |
| ROM version | 342-0304-A | 342-0445-A |
| &-vector | $03F5-$03F7 | $03F5-$03F7 |
| Mouse support | Requires card | Built-in via &M |

### IIc-Specific Features

The Apple IIc Applesoft ROM includes hooks for:
- Built-in serial ports via IN#1, PR#1, IN#2, PR#2
- Mouse support via ampersand commands

---

## Important Constants

| Symbol | Address | Description |
|--------|---------|-------------|
| TXTTAB | $67-$68 | Start of program (typically $0801) |
| VARTAB | $69-$6A | End of program, start of variables |
| ARYTAB | $6B-$6C | Start of arrays |
| STREND | $6D-$6E | End of arrays |
| FRETOP | $6F-$70 | Bottom of string space |
| MEMSIZ | $73-$74 | Top of BASIC memory |
| CURLIN | $75-$76 | Current line ($FFFF = direct) |

---

## Document History

| Version | Date       | Changes |
|---------|------------|---------|
| 1.0     | 2025-12-30 | Initial specification |
