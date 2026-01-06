# Deep Technical Analysis of the Apple IIe and IIc Monitor ROM

---

## Introduction

The Apple IIe and IIc Monitor ROMs represent the culmination of Apple’s early firmware engineering for the 6502 and 65C02-based systems. These ROMs provide the foundational machine-level environment for system initialization, input/output, memory management, and hardware interaction. Their design reflects both the constraints and the ingenuity of late 1970s and early 1980s microcomputer architecture, with a blend of documented and undocumented features, intricate memory mapping, and a robust set of callable routines for user and system programs. This report provides a comprehensive technical breakdown of the Monitor ROM’s internal control flow, memory map, entry points, calling conventions, scratch RAM usage, and device interactions, drawing on authoritative manuals, disassemblies, and community reverse engineering efforts.

---

## Monitor ROM Architecture and Versions

### Evolution and Structure

The Monitor ROM is the core firmware component present in all Apple II-family computers, but its architecture evolved significantly across models:

- **Apple II/II+**: The Monitor ROM is self-contained within $F800–$FFFF, providing basic memory examination, modification, and cassette I/O routines.
- **Apple IIe**: The Monitor ROM ($F800–$FFFF) is extended with additional routines for 80-column display, bank-switched RAM, and enhanced I/O. Some routines jump outside this area to interact with auxiliary firmware, notably for 80-column support and diagnostics.
- **Apple IIc**: The IIc ROM is bank-switched (main and alternate banks, each 16KB), with the Monitor routines distributed across both banks. The IIc introduces the Protocol Converter (SmartPort) firmware at $C500, supporting intelligent peripherals like the UniDisk 3.5.

The Monitor ROM is tightly integrated with the system’s memory map, softswitches, and peripheral expansion architecture. Its entry points and conventions are largely consistent across models, but with notable enhancements and quirks in later revisions.

---

## Internal Control Flow

### Cold Start and Reset Sequence

Upon power-on or RESET, the Monitor ROM executes a sequence of hardware and software initialization steps:

1. **Vector Fetch**: The 6502/65C02 processor fetches the RESET vector from $FFFC/$FFFD, which points to the Monitor’s reset routine (typically at $FA62 in the IIe).
2. **Hardware Initialization**:
   - Keyboard set as input, screen as output.
   - Video mode set to text, primary display page selected.
   - High-resolution graphics cleared via $C056.
   - Annunciator outputs and expansion ROM disabled.
   - Keyboard strobe cleared via $C010.
3. **Page Zero and System Variables**:
   - Scroll Window parameters (WNDLFT, WNDWDTH, WNDTOP, WNDBTM) initialized.
   - Cursor position (CV, CH) set.
   - Output and input vectors (CSWL,H and KSWL,H) set to COUT1 and KEYIN, respectively.
   - INVFLG set for normal video (white on black).
4. **Cold/Warm Start Determination**:
   - The Monitor checks $03F4 for a validation byte: if $03F3 XOR $03F4 = $A5, a warm start is possible (vector at $03F2–$03F3).
   - Otherwise, a cold start is performed, initializing vectors and scanning slots for bootable devices.
5. **Slot Scanning and Boot**:
   - Slots 7 to 1 are scanned for disk controllers. If found, control jumps to the slot’s boot ROM ($C0X0).
   - If no bootable device is found, the system falls back to BASIC or displays an error message (IIc/IIgs).
6. **Monitor Command Processor Entry**:
   - After initialization, control passes to the Monitor command interpreter (MONZ at $FF69), which displays the prompt and awaits user input.

This sequence ensures that the system is ready for either BASIC execution, machine-language programming, or peripheral bootstrapping.

### Interrupt Handling: IRQ and BRK

The Monitor ROM handles both hardware interrupts (IRQ) and software interrupts (BRK) via the vector at $FFFE/$FFFF:

- **IRQ Handling**:
  - On IRQ, the processor pushes PC and P-reg to the stack.
  - The Monitor saves A-reg at $45 (ACC), pulls P-reg, and tests the BRK bit.
  - If BRK, jumps to the BREAK handler; otherwise, jumps indirect via $03FE–$03FF to the user IRQ handler.
- **BRK Handling**:
  - The BREAK routine saves all registers (A, X, Y, P, S) to $45–$49.
  - PC is saved to $3A–$3B (PCL,H).
  - In the Autostart Monitor, control jumps via the BRK vector at $03F0–$03F1 (default to OLDBRK at $FA59).
  - The Old Monitor displays instruction and registers, then enters the Monitor.

This mechanism allows both system and user programs to hook into interrupt processing, with conventions for register and stack usage.

### Monitor Command Execution

The Monitor command interpreter parses and executes commands entered via the keyboard:

- **Command Parsing**: The input line is scanned, with addresses and commands decoded into zero-page variables (MODE, AIL,H, A2L,H, etc.).
- **Command Execution**: Supported commands include memory examination (L), modification (:), movement (M), comparison (V), register display (Ctrl-E), cassette I/O (R/W), and more.
- **Custom Commands**: Users can create custom command sequences, repeat commands, and invoke the mini-assembler (via ! in IIe/IIc).

The command processor is modular, with entry points for both direct user interaction and programmatic invocation.

---

## Memory Map of Monitor ROM

### System Memory Organization

The Apple IIe and IIc memory map is divided into distinct regions, each serving specific roles in system operation:

| Address Range | Description |
|---------------|-------------|
| $0000–$00FF   | Zero-page RAM (temporary variables, pointers) |
| $0100–$01FF   | Stack (6502 hardware stack) |
| $0200–$02FF   | Keyboard input buffer (GETLN) |
| $0300–$03CF   | Free space for ML programs, shape tables |
| $03D0–$03FF   | DOS/ProDOS/interrupt vectors |
| $0400–$07FF   | Text video page 1 / peripheral screen holes |
| $0800–$0BFF   | Text video page 2 / Applesoft program area |
| $0C00–$1FFF   | Free space for ML, shapes, etc. |
| $2000–$3FFF   | High-resolution graphics page 1 |
| $4000–$5FFF   | High-resolution graphics page 2 |
| $6000–$95FF   | Applesoft string data |
| $9600–$BFFF   | DOS/ProDOS routines and buffers |
| $C000–$C0FF   | Softswitches and status locations |
| $C100–$C7FF   | Peripheral card ROMs and firmware |
| $C800–$CFFF   | Extended memory for peripheral cards |
| $D000–$F7FF   | Applesoft interpreter / bank-switched RAM |
| $F800–$FFFF   | Monitor ROM |

**Monitor ROM**: The core Monitor firmware resides at $F800–$FFFF, with key routines and vectors located throughout this region.

### Monitor ROM Routine Map

The following table summarizes key Monitor ROM routines and their addresses:

| Routine | Address | Description |
|---------|---------|-------------|
| PLOT    | $F800   | Plot point (A: line, Y: col, $30: color) |
| HLINE   | $F819   | Horizontal line plot |
| VLINE   | $F828   | Vertical line plot |
| COUT    | $FDED   | Output character via CSWL |
| COUT1   | $FDF0   | Output to screen with INVFLG |
| COUTZ   | $FDF6   | Output to screen, bypass INVFLG |
| GETLN   | $FD6A   | Read line from keyboard |
| RDKEY   | $FDC0   | Read single key via KSWL |
| KEYIN   | $FDCB   | Read from keyboard hardware |
| CROUT   | $FD8E   | Output carriage return |
| MONZ    | $FF69   | Monitor command interpreter |
| SAVE    | $FF4A   | Save registers to $45–$49 |
| RESTORE | $FF3F   | Restore registers from $45–$48 |
| MOVE    | $FE2C   | Move memory (AIL–A2L to A4L) |
| GO      | $FEB6   | Jump to PCL,H after RESTORE |
| WAIT    | $FCA8   | Timing delay routine |

These routines are callable via JSR from user programs, with well-defined calling conventions.

### Interrupt and Vector Locations

| Vector | Address | Description |
|--------|---------|-------------|
| RESET  | $FFFC/$FFFD | Points to Monitor reset routine ($FA62) |
| IRQ/BRK| $FFFE/$FFFF | Points to interrupt handler ($FA40 or $FA92) |
| BRK    | $03F0/$03F1 | Autostart BRK handler vector |
| RESET Soft Entry | $03F2/$03F3 | Warm start vector |
| Validation Byte | $03F4 | $03F3 XOR $A5 |
| IRQ Handler | $03FE/$03FF | User IRQ handler vector |

These vectors enable flexible interrupt handling and system restart behavior.

### Peripheral Controller Work Areas

Peripheral cards use reserved screen holes for scratchpad RAM:

| Slot | Addresses     |
|------|---------------|
| 1    | $0478–$047F   |
| 2    | $04F8–$04FF   |
| 3    | $0578–$057F   |
| 4    | $05F8–$05FF   |
| 5    | $0678–$067F   |
| 6    | $06F8–$06FF   |
| 7    | $0778–$077F   |
| All  | $07F8–$07FF   |

Misuse of these areas can cause unpredictable behavior, especially when loading or saving screen memory.

---

## Entry Points and Calling Conventions

### Documented Entry Points

The Monitor ROM exposes a set of documented entry points for system and user programs:

| Entry Point | Address | Description | Calling Convention |
|-------------|---------|-------------|--------------------|
| RESET       | $FFFC   | Cold start/reset entry | Jumps to Monitor reset routine |
| RDKEY       | $FD0C   | Waits for and returns keypress | Returns ASCII in A |
| GETLN       | $FD6A   | Reads a line from keyboard | Uses buffer at $0200 |
| COUT        | $FDED   | Outputs character in A to screen | A = ASCII char |
| CROUT       | $FD8E   | Outputs carriage return | No input required |
| MONZ        | $FF69   | Entry to Monitor command interpreter | JSR MONZ |
| KEYIN       | $FDCB   | Reads raw keyboard input | Returns ASCII in A |
| VTAB        | $FC22   | Set BASL,H from CV | A = CV |
| VTABZ       | $FC24   | Set BASL,H from A | A = line |
| BASCALC     | $FBC1   | Compute BASL,H from A | A = line |
| GBASCALC    | $F847   | Compute GBASL,H from A | A = line |
| PLOT        | $F800   | Plot point at (A,Y) | A = line, Y = col |
| HLINE       | $F819   | Horizontal line from (Y) to H2 | A = line, Y = col |
| VLINE       | $F828   | Vertical line from (A) to V2 | A = line, Y = col |
| MOVE        | $FE2C   | Move memory AIL–A2L to A4L | Y = 0 |
| GO          | $FEB6   | Jump to PCL,H after RESTORE | AIL,H, ACC, XREG, YREG, STATUS |
| SAVE        | $FF4A   | Save A,X,Y,P,S to $45–$49 | — |
| RESTORE     | $FF3F   | Restore A,X,Y,P from $45–$48 | — |

These entry points are stable across Apple IIe and IIc, with minor address differences in enhanced ROMs.

### Undocumented Entry Points

Community reverse engineering has identified additional, less-documented entry points:

- **COUTZ ($FDF6)**: Output to screen, bypass INVFLG.
- **ESCNEW ($FBA5)**: Autostart multi-Escape handler.
- **BELL ($FBDD, $FF3A)**: Speaker bell routines.
- **PRERR ($FF2D)**: Output "ERR" and bell.
- **Protocol Converter ($C500)**: Entry point for SmartPort/CBus routines in IIc.

These routines are often invoked by system software or advanced user programs for specialized tasks.

### Calling Conventions and Stack/Register Usage

- **Subroutine Calls**: All Monitor routines are called via JSR, with the 6502/65C02 pushing the return address onto the stack.
- **Register Usage**:
  - **A**: Input/output for most routines (e.g., COUT expects character in A).
  - **X/Y**: Used for indexing, buffer pointers, and return values (e.g., paddle read returns value in Y).
  - **P (Processor Status)**: May be modified; saved/restored via SAVE/RESTORE.
- **Stack Behavior**: Standard 6502 conventions apply. Some routines (e.g., Protocol Converter) require significant stack space (up to 35 bytes).
- **Zero Page**: Many routines use zero-page addresses for temporary storage, pointers, and command parsing.

User programs must preserve registers and stack state as needed, especially when chaining Monitor routines.

---

## Scratch RAM Usage

### Zero Page Conventions

The Monitor ROM uses zero-page RAM extensively for temporary storage, pointers, and system variables:

| Address | Label     | Description |
|---------|-----------|-------------|
| $20     | WNDLFT    | Scroll Window left column |
| $21     | WNDWDTH   | Scroll Window width |
| $22     | WNDTOP    | Scroll Window top line |
| $23     | WNDBTM    | Scroll Window bottom line + 1 |
| $24     | CH        | Cursor horizontal position |
| $25     | CV        | Cursor vertical position |
| $26–$27 | GBASL,H   | LORES plot line memory address |
| $28–$29 | BASL,H    | Text line base address |
| $2A–$2B | BAS2L,H   | Scroll destination line pointer |
| $2C     | H2        | Horizontal line end |
| $2D     | V2        | Vertical line end |
| $2E     | MASK      | Plot mask / CHKSUM during tape read |
| $2F     | FORMAT    | Instruction format/sign flag |
| $30     | COLOR     | Color code for graphics |
| $31     | MODE      | Command parsing mode |
| $32     | INVFLG    | Inverse video control |
| $33     | PROMPT    | Prompt character |
| $34     | YSAV      | Y-reg save for command processor |
| $35     | YSAV1     | Y-reg save across COUT1 |
| $36–$37 | CSWL,H    | Output vector (default: COUT1) |
| $38–$39 | KSWL,H    | Input vector (default: KEYIN) |
| $3A–$3B | PCL,H     | Program Counter |
| $3C–$3D | AIL,H     | Source pointer for MOVE, etc. |
| $3E–$3F | A2L,H     | End pointer for MOVE, etc. |
| $40–$41 | A3L,H     | Destination pointer for STORE |
| $42–$43 | A4L,H     | Destination pointer for MOVE |
| $44–$45 | A5L,H     | Used with A4L,H; ACC |
| $45     | ACC       | A-reg save |
| $46     | XREG      | X-reg save |
| $47     | YREG      | Y-reg save |
| $48     | STATUS    | P-reg save |
| $49     | SPNT      | S-reg save |

Addresses $4A–$4D are unused by the Monitor, and $4E–$4F serve as random seeds (RNDL,H). The Old Monitor uses $50–$55 for multiply/divide routines.

### Main Memory Usage

- **Page One ($0100–$01FF)**: Stack area, used by 6502 instructions (PHA, JSR, RTS, etc.).
- **Page Two ($0200–$02FF)**: Keyboard input buffer, used by GETLN and related routines.
- **Page Three ($03F0–$03FF)**: Interrupt vectors and soft entry points (BRK, RESET, Applesoft "&", Control-Y, NMI, IRQ).
- **Text/Graphics Buffers**:
  - $0400–$07FF: Primary text/LORES display.
  - $0800–$0BFF: Secondary text/LORES display.
  - $2000–$3FFF: Primary HIRES display.
  - $4000–$5FFF: Secondary HIRES display.

**Peripheral Controller Work Areas**: Reserved screen holes for slot-based expansion cards ($0478–$077F).

### Reserved and Special Addresses

- **Protocol Converter**: Uses zero-page locations $0006/$0007 for temporary storage; programs must avoid using these when interacting with SmartPort devices.
- **Bank-Switched RAM**: In IIe/IIc, bank switching affects $D000–$FFFF and $0000–$BFFF, with conventions for read/write enable and zero-page/stack selection.

User programs must respect these conventions to avoid conflicts and unpredictable behavior.

---

## Device Interactions

### Keyboard

- **Polling**: The keyboard is polled via softswitch at $C000. Keypress data is read from $C000; strobe is cleared by reading $C010.
- **Input Routines**:
  - **RDKEY**: Waits for keypress, returns ASCII code in A.
  - **KEYIN**: Reads raw keyboard input.
  - **GETLN**: Reads a line from the keyboard into buffer at $0200.
- **Echo and Editing**: Input is echoed to the screen via COUT; editing features (backspace, cancel, retype) are supported in GETLN.

### Display

- **Text Screen Memory**: $0400–$07FF (primary), $0800–$0BFF (secondary).
- **Output Routines**:
  - **COUT/COUT1/COUTZ**: Output characters to screen, with INVFLG controlling inverse/blinking.
  - **CROUT**: Outputs carriage return.
- **Escape Sequences**: ESC/@ clears screen; ESC sequences control cursor movement and screen formatting.
- **Softswitches**:
  - $C050: Text mode.
  - $C051: Graphics mode.
  - $C052/$C053: Mixed/full graphics.
  - $C054/$C055: Page selection.
  - $C056/$C057: HIRES graphics.

80-column mode is enabled via additional softswitches ($C00C/$C00D) and uses both main and auxiliary memory for display.

### Cassette Interface

- **Output**: $C020 (CASSETTE-OUT) toggles output signal.
- **Input**: $C060 (CASSETTE-IN) reads input signal (bit 7).
- **Monitor Routines**:
  - **WRITE ($FECD)**: Writes data to tape.
  - **READ ($FEFD)**: Reads data from tape.
  - **HEADR, RD2BIT, RDBIT, RDBYTE, WRBIT, WRBYTE**: Low-level routines for cassette protocol.
- **Signal Processing**: Firmware interprets audio tones, sync bits, and checksums for reliable data transfer. Minimum input is ~1V peak-to-peak; output is ~32mV.

### Speaker

- **Toggle**: $C030 toggles speaker state (click).
- **Bell Routine**: BELL ($FBDD, $FF3A) produces 1000 Hz tone for 0.1 seconds, triggered by outputting $87 via COUT.

### Game I/O

- **Paddle Read**: PREAD ($FB1E), X=0–3, result in Y.
- **Paddle Trigger**: $C070.
- **Paddle Inputs**: $C064–$C067.
- **Buttons**: $C061–$C063.
- **Annunciators**: $C058–$C05F (set/clear outputs).

### Slot-Based Expansion and Peripheral ROMs

- **Slot ROMs**: $C100–$C7FF, 256 bytes per slot.
- **BOOT0 Routine**: Scans slots for bootable ROMs, jumps to $C0X0 if found.
- **Slot Vectors**: Polled in order to find bootable devices; expansion cards use reserved screen holes for work areas.
- **80-Column Card**: Firmware at $C300; activated via PR#3 or jump to $C300. Emulates slot 3 ROM for display routines.

### Protocol Converter (SmartPort/CBus)

- **Location**: $C500 in IIc with 32K ROM.
- **Entry Point**: JSR to $C500 + ($C5FF) + 3, with command number and parameter list.
- **Commands**: STATUS, READ BLOCK, WRITE BLOCK, FORMAT, CONTROL, INIT, OPEN, CLOSE, READ, WRITE.
- **Stack Usage**: Up to 35 bytes required.
- **Interrupt Handling**: Devices use EXTINT line; CONTROL call enables/disables interrupts. Status byte indicates interrupt state; handler must poll devices as needed.

### Softswitch Usage and Polling Behavior

- **Access**: Softswitches are accessed by reading or writing to $C000–$C0FF.
- **Behavior**: Writing sets/clears modes; reading returns status (bit 7).
- **Polling**: Monitor routines poll $C000/$C010 for keyboard input, $C030 for speaker, $C050–$C057 for display modes.
- **Quirks**: Indexed store operations may cause double toggling due to 6502 bus behavior; some softswitches toggle on access (e.g., speaker, cassette out).

---

## Bank-Switched RAM and Memory Management

### IIe/IIc Bank Switching

The Apple IIe and IIc support bank-switched RAM, enabling access to additional memory beyond the base 64KB:

- **Softswitches**:
  - $C002/$C003: RAMRDON/RAMRDOFF (read enable aux/main memory $0200–$BFFF)
  - $C004/$C005: RAMWRTON/RAMWRTOFF (write enable aux/main memory $0200–$BFFF)
  - $C008/$C009: ALTZPON/ALZTPOFF (enable aux/main zero page and stack)
  - $C00A/$C00B: SLOTC3ROMON/SLOTC3ROMOFF (slot 3 ROM selection)
- **Auxiliary Memory**: 80-column card and IIc use auxiliary RAM for display and program storage.
- **Bank-Switched ROM**: IIc ROM is divided into main and alternate banks; switching via $C028.

Programs must ensure that code is present in both banks before switching execution, using ROM routines (AUXMOVE, XFER) for copying data between banks.

---

## Monitor ROM Timing Routines

### WAIT Routine

The WAIT routine at $FCA8 provides a timing delay for synchronization and hardware interaction:

- **Cycle Count Formula**: 2.5A² + 13.5A + 13 machine cycles, where A is the accumulator value on entry.
- **Time Calculation**: Multiply cycle count by 0.980 μs (average clock period for 1.0205 MHz CPU).
- **Minimum Delay**: Actual delay may be longer due to interrupts and system overhead; use only for minimum timing guarantees.

---

## Community Reverse Engineering Tools and Projects

### Disassembly and Analysis Tools

- **SourceGen**: Interactive disassembler for 6502/65C02/65816, supporting annotation, visualization, and cross-referencing.
- **6502bench**: Code development workbench with static analysis, assembler source generation, and project sharing.
- **6502disasm**: Disassembly listings for Apple II ROMs, including Monitor, BASIC, and peripheral firmware.
- **Apple II Monitors Peeled**: Authoritative manual with detailed routine descriptions, memory maps, and address tables.

### Community Projects

- **ROM 4X/5X**: Enhanced IIc/IIc Plus firmware with diagnostics, boot options, and RAM card support.
- **Apple II Dead Test ROM**: Diagnostic ROM for RAM testing without zero page usage.
- **UniDisk 3.5 Co-Processor**: Project for using UniDisk as a co-processor via Protocol Converter calls.
- **Apple II Audit**: Test suite for hardware, ROM versions, RAM configuration, and softswitch behavior.

These tools and projects facilitate ongoing reverse engineering, documentation, and enhancement of Apple II-family firmware.

---

## Undocumented Behaviors and Quirks

### Notable Quirks

- **BRK Vector**: Autostart Monitor uses $03F0–$03F1 for BRK handler; not present in Old Monitor.
- **RESET Vector Validation**: $03F4 = $03F3 XOR $A5; used to determine cold/warm start.
- **INVFLG Behavior**: $7F causes blinking only if character has high bits set.
- **DOS Overwrites**: DOS/ProDOS may overwrite page 3 vectors; user must restore if needed.
- **Softswitch Side Effects**: Indexed store operations may cause double toggling; shift-key mod enables reading shift key via SW2 ($C063).
- **Bank Switching**: Programs must ensure code/data is present in both banks before switching execution; use ROM routines for copying.

These behaviors are documented in community manuals and reverse engineering notes, but may not be present in official Apple documentation.

---

## Summary Table: Key Monitor ROM Routines and Vectors

| Routine/Vector | Address | Description | Registers/Stack |
|----------------|---------|-------------|-----------------|
| RESET          | $FFFC   | System reset entry | JSR, stack push |
| IRQ/BRK        | $FFFE   | Interrupt handler | PC, P pushed; A saved at $45 |
| BRK Vector     | $03F0   | Autostart BRK handler | JMP ($03F0) |
| RDKEY          | $FD0C   | Wait for keypress | Returns ASCII in A |
| GETLN          | $FD6A   | Read line from keyboard | Buffer at $0200 |
| COUT           | $FDED   | Output character | A = ASCII char |
| CROUT          | $FD8E   | Output carriage return | No input required |
| MONZ           | $FF69   | Monitor command interpreter | JSR MONZ |
| KEYIN          | $FDCB   | Raw keyboard input | Returns ASCII in A |
| WAIT           | $FCA8   | Timing delay | A = delay value |
| SAVE/RESTORE   | $FF4A/$FF3F | Save/restore registers | $45–$49 |
| Protocol Converter | $C500 | SmartPort/CBus entry | JSR, stack usage |

---

## Conclusion

The Apple IIe and IIc Monitor ROMs embody a sophisticated and flexible firmware architecture, providing essential system initialization, input/output, memory management, and hardware interaction capabilities. Their design leverages the 6502/65C02 instruction set, memory-mapped I/O, and softswitches to enable a rich set of routines and entry points for both system and user programs. The Monitor ROM’s internal control flow, memory map, calling conventions, and device interactions are well-documented in technical manuals and community reverse engineering efforts, with ongoing projects enhancing and extending their functionality.

Understanding the Monitor ROM’s architecture is crucial for advanced programming, hardware interfacing, and system diagnostics on Apple II-family computers. The combination of documented routines, reserved memory areas, and subtle quirks provides both power and complexity, reflecting the ingenuity of early microcomputer engineering. As community tools and projects continue to evolve, the Monitor ROM remains a foundational component for retrocomputing enthusiasts, hardware developers, and digital archaeologists.

---

**Key Takeaways**:

- The Monitor ROM is central to Apple IIe/IIc system operation, handling initialization, I/O, and hardware interaction.
- Memory mapping is intricate, with reserved areas for zero page, stack, input buffers, display, and peripheral controllers.
- Entry points are well-defined, with standard calling conventions and stack/register usage.
- Device interactions are managed via memory-mapped softswitches, with routines for keyboard, display, cassette, speaker, and expansion cards.
- Bank-switched RAM and ROM require careful management, with conventions for read/write enable and zero-page selection.
- Community tools and reverse engineering projects provide invaluable resources for understanding and extending Monitor ROM functionality.
- Undocumented behaviors and quirks highlight the need for careful programming and system awareness.

This report synthesizes technical details from authoritative sources, disassemblies, and community efforts, providing a comprehensive reference for deep technical understanding of the Apple IIe and IIc Monitor ROM.
Great — I’m on it. I’ll be researching the Apple IIe and IIc Monitor ROM internals, including:

- 🧭 Control flow: how the ROM initializes, handles I/O, and manages system state
- 🧠 Memory map: key routines, vectors, and data tables
- 🪝 Entry points: documented and undocumented, with calling conventions
- 🗂️ Scratch RAM: zero-page and main memory usage
- 🧩 Device interactions: keyboard, display, cassette, and slot cards
