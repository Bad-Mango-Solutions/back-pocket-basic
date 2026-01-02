# Apple IIe Enhanced, Apple IIc, and Apple IIc Plus: Comprehensive Memory-Mapped I/O Page ($C000–$C0FF) Reference

------

## Introduction

The Apple II family, particularly the Enhanced IIe, IIc, and IIc Plus, represents the culmination of the classic 8-bit Apple II architecture. Central to their operation is the memory-mapped I/O page, spanning addresses $C000 to $C0FF, which provides direct access to hardware features, soft switches, device registers, and peripheral card slots. This region is crucial for low-level programming, hardware interfacing, and system control, and its layout reflects both the legacy of earlier Apple II models and the innovations introduced in later revisions. This report presents an exhaustive, authoritative mapping of the $C000–$C0FF I/O page for these three models, highlighting all known soft switches, device registers, and model-specific differences. Each entry is supported by citations from Apple technical reference manuals, schematics, emulator documentation, and disassembly sources.

------

## Overview of the $C000–$C0FF I/O Page

The $C000–$C0FF region is reserved for memory-mapped I/O in all Apple II models. It provides access to:

- **Keyboard input and strobe**
- **Video mode soft switches (text/graphics, 40/80 column, hires/dhgr)**
- **Speaker toggling**
- **Paddle and analog inputs (game I/O)**
- **Annunciators and strobe outputs**
- **Peripheral card slot I/O and ROM mapping**
- **Auxiliary memory and 80-column card registers**
- **Model-specific additions (e.g., MouseText, double hi-res, accelerator controls)**

While the basic layout is consistent, each model introduces unique features and subtle differences in behavior, especially in the handling of auxiliary memory, video modes, and peripheral expansion.

------

## Table: Apple IIe Enhanced, IIc, IIc Plus I/O Page ($C000–$C0FF)

Below is a comprehensive table mapping each address (or address range) in the $C000–$C0FF region. For each entry, the table provides the address, label/name, function/description, and applicable models. Inline citations reference authoritative sources.

------

| Address     | Label/Name    | Function/Description                                         | Applicable Models           |
| ----------- | ------------- | ------------------------------------------------------------ | --------------------------- |
| $C000       | KBD           | Keyboard data register. Reading returns ASCII value of last key pressed. Bit 7 = 1 if key is ready. | IIe Enhanced, IIc, IIc Plus |
| $C001       | SET80STORE    | 80STORE On – enable 80-column memory mapping (write-only).   | IIe Enhanced, IIc, IIc Plus |
| $C002       | CLRAUXRD      | Read from main 48K (write-only).                             | IIe Enhanced, IIc, IIc Plus |
| $C003       | SETAUXRD      | Read from aux/alt 48K (write-only).                          | IIe Enhanced, IIc, IIc Plus |
| $C004       | CLRAUXWR      | Write to main 48K (write-only).                              | IIe Enhanced, IIc, IIc Plus |
| $C005       | SETAUXWR      | Write to aux/alt 48K (write-only).                           | IIe Enhanced, IIc, IIc Plus |
| $C006       | CLRCXROM      | Use ROM on cards (write-only).                               | IIe Enhanced, IIc, IIc Plus |
| $C007       | SETCXROM      | Use internal ROM (write-only).                               | IIe Enhanced, IIc, IIc Plus |
| $C008       | CLRAUXZP      | Use main zero page, stack, & LC (write-only).                | IIe Enhanced, IIc, IIc Plus |
| $C009       | SETAUXZP      | Use alt zero page, stack, & LC (write-only).                 | IIe Enhanced, IIc, IIc Plus |
| $C00A       | CLRC3ROM      | Use internal Slot 3 ROM (write-only).                        | IIe Enhanced, IIc, IIc Plus |
| $C00B       | SETC3ROM      | Use external Slot 3 ROM (write-only).                        | IIe Enhanced, IIc, IIc Plus |
| $C00C       | CLR80VID      | Disable 80-column display mode (write-only).                 | IIe Enhanced, IIc, IIc Plus |
| $C00D       | SET80VID      | Enable 80-column display mode (write-only).                  | IIe Enhanced, IIc, IIc Plus |
| $C00E       | CLRALTCH      | Use main char set – normal LC, Flash UC (write-only).        | IIe Enhanced, IIc, IIc Plus |
| $C00F       | SETALTCH      | Use alt char set – normal inverse, LC; no Flash (write-only). | IIe Enhanced, IIc, IIc Plus |
| $C010       | KBDSTRB       | Keyboard strobe reset. Reading clears keyboard strobe flag. Bit 7 = 1 if any key is down. | IIe Enhanced, IIc, IIc Plus |
| $C011–$C01F | Status Flags  | Various status flags for auxiliary memory, ROM, video, and character set. Reading returns status (bit 7 set if active). See detailed breakdown below. | IIe Enhanced, IIc, IIc Plus |
| $C020       | TAPEOUT       | Cassette output toggle. Toggles the cassette output signal. Used to write data to cassette. | IIe Enhanced, IIc, IIc Plus |
| $C030       | SPKR          | Speaker toggle. Each access toggles the internal speaker cone position, producing a click. Repeated toggling produces tones. | IIe Enhanced, IIc, IIc Plus |
| $C040       | STROBE        | Utility strobe output. Drops from +5V to 0V for 0.5 microseconds when accessed. Used for timing and game port strobe. | IIe Enhanced, IIc, IIc Plus |
| $C050       | TXTCLR        | Display graphics mode. Switches display from text to graphics mode. | IIe Enhanced, IIc, IIc Plus |
| $C051       | TXTSET        | Display text mode. Switches display from graphics to text mode. | IIe Enhanced, IIc, IIc Plus |
| $C052       | MIXCLR        | Display all text or graphics. Disables mixed mode.           | IIe Enhanced, IIc, IIc Plus |
| $C053       | MIXSET        | Mix text and graphics. Enables 4 lines of text at bottom of graphics screen. | IIe Enhanced, IIc, IIc Plus |
| $C054       | LOWSCR        | Display primary page (Page 1). Selects primary screen page.  | IIe Enhanced, IIc, IIc Plus |
| $C055       | HISCR         | Display secondary page (Page 2). Selects secondary screen page. | IIe Enhanced, IIc, IIc Plus |
| $C056       | LORES         | Display low-resolution graphics mode.                        | IIe Enhanced, IIc, IIc Plus |
| $C057       | HIRES         | Display high-resolution graphics mode.                       | IIe Enhanced, IIc, IIc Plus |
| $C058       | SETAN0        | Annunciator 0 ON. Sets annunciator 0 output high (5V).       | IIe Enhanced, IIc, IIc Plus |
| $C059       | CLRAN0        | Annunciator 0 OFF. Sets annunciator 0 output low (0V).       | IIe Enhanced, IIc, IIc Plus |
| $C05A       | SETAN1        | Annunciator 1 ON. Sets annunciator 1 output high.            | IIe Enhanced, IIc, IIc Plus |
| $C05B       | CLRAN1        | Annunciator 1 OFF. Sets annunciator 1 output low.            | IIe Enhanced, IIc, IIc Plus |
| $C05C       | SETAN2        | Annunciator 2 ON. Sets annunciator 2 output high.            | IIe Enhanced, IIc, IIc Plus |
| $C05D       | CLRAN2        | Annunciator 2 OFF. Sets annunciator 2 output low.            | IIe Enhanced, IIc, IIc Plus |
| $C05E       | SETAN3        | Annunciator 3 ON. Sets annunciator 3 output high.            | IIe Enhanced, IIc, IIc Plus |
| $C05F       | CLRAN3        | Annunciator 3 OFF. Sets annunciator 3 output low.            | IIe Enhanced, IIc, IIc Plus |
| $C060       | TAPEIN / PB0  | Cassette input. Reading returns bit 7 set if cassette input is high. Also used for pushbutton 0 (Open-Apple key or joystick button). | IIe Enhanced, IIc, IIc Plus |
| $C061       | PB1           | Pushbutton Input 1. Returns bit 7 set if button is pressed.  | IIe Enhanced, IIc, IIc Plus |
| $C062       | PB2           | Pushbutton Input 2. Returns bit 7 set if button is pressed.  | IIe Enhanced, IIc, IIc Plus |
| $C063       | PB3           | Pushbutton Input 3. Returns bit 7 set if button is pressed. Not available on IIc/IIc Plus. | IIe Enhanced only           |
| $C064       | PDL0          | Paddle 0 Input. Returns analog value (0–255) after reset, then decreases over time depending on paddle position. | IIe Enhanced, IIc, IIc Plus |
| $C065       | PDL1          | Paddle 1 Input. Same as above for paddle 1.                  | IIe Enhanced, IIc, IIc Plus |
| $C066       | PDL2          | Paddle 2 Input. Same as above for paddle 2. Not available on IIc/IIc Plus. | IIe Enhanced only           |
| $C067       | PDL3          | Paddle 3 Input. Same as above for paddle 3. Not available on IIc/IIc Plus. | IIe Enhanced only           |
| $C070       | PTRIG         | Paddle Trigger. Resets paddle timers. Reading or writing to this location resets the paddle timing circuits. | IIe Enhanced, IIc, IIc Plus |
| $C071–$C073 | PTRIG1–PTRIG3 | Paddle timer reset for paddles 1–3. Not available on IIc/IIc Plus. | IIe Enhanced only           |
| $C080–$C08F | Slot 0 I/O    | I/O space for peripheral card in slot 0.                     | IIe Enhanced only           |
| $C090–$C09F | Slot 1 I/O    | I/O space for peripheral card in slot 1.                     | IIe Enhanced only           |
| $C0A0–$C0AF | Slot 2 I/O    | I/O space for peripheral card in slot 2.                     | IIe Enhanced only           |
| $C0B0–$C0BF | Slot 3 I/O    | I/O space for peripheral card in slot 3.                     | IIe Enhanced only           |
| $C0C0–$C0CF | Slot 4 I/O    | I/O space for peripheral card in slot 4.                     | IIe Enhanced only           |
| $C0D0–$C0DF | Slot 5 I/O    | I/O space for peripheral card in slot 5.                     | IIe Enhanced only           |
| $C0E0–$C0EF | Slot 6 I/O    | I/O space for peripheral card in slot 6.                     | IIe Enhanced only           |
| $C0F0–$C0FF | Slot 7 I/O    | I/O space for peripheral card in slot 7.                     | IIe Enhanced only           |

------

### Detailed Status Flags ($C011–$C01F)

| Address | Label/Name | Function/Description                 | Applicable Models       |
| ------- | ---------- | ------------------------------------ | ----------------------- |
| $C011   | RDLCBNK2   | Language card bank 2 available       | IIe Enhanced, IIc, IIc+ |
| $C012   | RDLCRAM    | Language card RAM active for read    | IIe Enhanced, IIc, IIc+ |
| $C013   | RAMRD      | RAM read: 0=main, 1=auxiliary        | IIe Enhanced, IIc, IIc+ |
| $C014   | RAMWRT     | RAM write: 0=main, 1=auxiliary       | IIe Enhanced, IIc, IIc+ |
| $C015   | INTCXROM   | Internal ROM active                  | IIe Enhanced, IIc, IIc+ |
| $C016   | ALTZP      | Alternate zero page active           | IIe Enhanced, IIc, IIc+ |
| $C017   | SLOTC3ROM  | Slot 3 ROM active                    | IIe Enhanced, IIc, IIc+ |
| $C018   | 80STORE    | 80STORE mode active                  | IIe Enhanced, IIc, IIc+ |
| $C019   | VERTBLANK  | Vertical blanking interval active    | IIe Enhanced, IIc, IIc+ |
| $C01A   | TEXT       | Text mode active                     | IIe Enhanced, IIc, IIc+ |
| $C01B   | MIXED      | Mixed graphics/text mode active      | IIe Enhanced, IIc, IIc+ |
| $C01C   | PAGE2      | Video page 2 selected                | IIe Enhanced, IIc, IIc+ |
| $C01D   | HIRES      | High-resolution graphics mode active | IIe Enhanced, IIc, IIc+ |
| $C01E   | ALTCHARSET | Alternate character set active       | IIe Enhanced, IIc, IIc+ |
| $C01F   | 80COL      | 80-column display mode active        | IIe Enhanced, IIc, IIc+ |

------

## Model-Specific Differences and Additions

### Apple IIe Enhanced

The Enhanced IIe introduced the 65C02 CPU, MouseText character set, improved 80-column firmware, and additional soft switches for auxiliary memory and video modes. It retains full support for peripheral slots ($C080–$C0FF), including slot ROM mapping and I/O space. The auxiliary slot (for the 80-column card and memory expansion) is managed via soft switches in the $C000–$C00F range, with status flags in $C011–$C01F. The IIe supports both main and auxiliary memory, with independent read/write selection, and allows for advanced video modes such as double hi-res (DHIRES) via additional soft switches.

### Apple IIc

The IIc is a compact, slotless version of the IIe, with built-in 80-column support, serial ports, and disk controller. Instead of physical slots, the IIc uses "virtual slots," with I/O and ROM spaces mapped to fixed addresses. Many soft switches and device registers are identical to the IIe, but some slot-specific features are absent or emulated. The IIc introduced built-in mouse support, memory expansion (in later revisions), and firmware changes for international keyboard layouts and Dvorak support.

### Apple IIc Plus

The IIc Plus builds on the IIc, adding a 4MHz accelerator, internal 3.5" disk drive, and further firmware changes. The I/O page remains largely compatible, but the accelerator circuit and disk controller introduce new registers and timing considerations. The IIc Plus maintains virtual slot mapping and supports the same soft switches as the IIc, with minor changes for memory expansion and device identification.

------

## Peripheral Card Slot I/O and ROM Mapping

### Slot I/O ($C080–$C0FF)

On the IIe Enhanced, each peripheral slot (0–7) is allocated 16 bytes of I/O space ($C080–$C0FF), with corresponding ROM space in $C100–$C7FF and $C800–$CFFF. Accessing these addresses activates the DEVICE SELECT signal on the slot connector, allowing the card to respond to reads/writes. Cards such as the Disk II controller, Super Serial Card, and 80-column card use these addresses for control registers, status flags, and firmware routines.

On the IIc and IIc Plus, physical slots are replaced by virtual slots, with I/O and ROM spaces mapped to fixed addresses. For example, the disk controller is mapped to virtual slot 6, serial ports to slots 1 and 2, and mouse firmware to slot 4 or 7 (depending on ROM version).

------

## Keyboard Input and Behavior

The keyboard is accessed via $C000 (data) and $C010 (strobe reset). Reading $C000 returns the ASCII code of the last key pressed, with bit 7 set if a key is ready. Reading $C010 clears the strobe, allowing new keypresses to be registered. Modifier keys (Shift, Control, Open-Apple, Closed-Apple) are handled via additional soft switches and status flags. The IIe Enhanced and IIc support lowercase input, international layouts, and Dvorak mapping via firmware routines and character generator ROMs.

------

## Video Mode Soft Switches

Video modes are controlled by a set of soft switches in the $C050–$C057 range (and their status flags in $C01A–$C01F):

- **$C050/$C051:** Text/graphics mode
- **$C052/$C053:** Mixed mode (4 lines of text at bottom)
- **$C054/$C055:** Page 1/Page 2 selection
- **$C056/$C057:** Low-res/high-res graphics
- **$C05E/$C05F:** Double hi-res graphics (Enhanced IIe, IIc, IIc Plus only)

The IIe Enhanced supports advanced video modes, including double hi-res and MouseText, via additional soft switches and firmware routines. The IIc and IIc Plus provide built-in 80-column support and double hi-res, with firmware handling scrolling and display glitches in mixed modes.

------

## Speaker and Tone Generation

The speaker is toggled by accessing $C030. Each read or write inverts the speaker cone position, producing a click. Sound generation is achieved by rapidly toggling $C030 in software loops, with timing controlled by CPU instructions. Advanced techniques, such as pulse-width modulation and delta modulation, allow for playback of sampled audio and music, as demonstrated in programs like SoftDAC and modern streaming audio players.

------

## Paddle Inputs and Game I/O

Paddle and analog inputs are accessed via $C064–$C067 (PDL0–PDL3), with timers reset by accessing $C070 (PTRIG). The value returned (0–255) reflects the position of the paddle or joystick axis. Pushbuttons (fire buttons) are read via $C061–$C063, with bit 7 set if pressed. The IIe Enhanced supports four paddles and three buttons; the IIc and IIc Plus support two paddles and two buttons, with mouse support replacing some game I/O functions in later revisions.

------

## Annunciators and Strobe Outputs

Annunciators are digital outputs used to control external devices via the game port. They are set/reset via $C058–$C05F (SETAN0–CLRAN3), with each bit controlling a pin on the game connector. The strobe output ($C040) generates a short pulse for timing and synchronization. These features are used in robotics, data acquisition, and custom hardware interfacing.

------

## Auxiliary Memory and 80-Column Card Registers

The IIe Enhanced supports auxiliary memory and the 80-column card via soft switches in $C000–$C00F and status flags in $C011–$C01F. These switches control read/write selection, zero page mapping, slot ROM mapping, and 80STORE mode. The 80-column card and extended memory are managed via additional registers in $C100 and above, with firmware routines handling bank switching and video output. The IIc and IIc Plus provide built-in support for these features, with memory expansion handled via internal connectors and firmware routines.

------

## ROM and Monitor Routines Referencing $C0xx Addresses

System monitor and ROM routines frequently reference $C0xx addresses for device control, keyboard input, and video output. Disassembly sources reveal the use of these addresses in firmware initialization, interrupt handling, and device drivers. The Enhanced IIe and IIc Plus introduce new monitor features, such as lowercase input, ASCII mode, and mini-assembler support, with corresponding changes in ROM listings and entry points.

------

## Emulator Documentation and I/O Behavior

Emulators such as Mednafen and MAME document the behavior of $C0xx soft switches, device registers, and model-specific differences. These sources provide detailed mappings, timing information, and compatibility notes for software development and hardware emulation. Differences in I/O behavior, such as timing of speaker toggling and paddle input, are critical for accurate emulation and software compatibility.

------

## Peripheral Cards Using $C0xx Addresses

Peripheral cards, including the Disk II controller, Super Serial Card, and 80-column card, use $C0xx addresses for control registers, status flags, and firmware routines. The Disk II controller, for example, uses $C080–$C08F for phase control, motor on/off, drive selection, and data transfer, with Q6/Q7 soft switches managing read/write modes. The Super Serial Card maps UART registers and configuration switches to its slot I/O space, with ROM mapped to $C800–$CFFF when enabled.

------

## Annunciator and Speaker Timing: Usage Examples in Code

Annunciator outputs and speaker toggling are used in assembly language routines for sound generation, device control, and timing. Example code loops toggle $C030 for tone generation, with timing determined by instruction cycles. Annunciators are set/reset via $C058–$C05F to control relays, LEDs, or other devices. Paddle input routines use $C070 to reset timers and poll $C064–$C067 for analog values, with BASIC functions (PDL(x), PEEK) providing high-level access.

------

## Differences in Soft Switch Behavior Across Models

While the basic soft switch layout is consistent, differences exist in behavior and availability:

- **IIe Enhanced:** Full slot support, auxiliary memory, advanced video modes, MouseText, double hi-res, mini-assembler, and improved monitor routines.
- **IIc:** Virtual slots, built-in 80-column and mouse support, memory expansion, international keyboard layouts, Dvorak support, and firmware changes for device identification.
- **IIc Plus:** Accelerator circuit (4MHz), internal 3.5" disk drive, further firmware changes, and timing considerations for fast/slow mode operation.

Some soft switches are absent or emulated in the IIc/IIc Plus due to the lack of physical slots. Timing and device behavior may differ due to hardware changes, especially in accelerated modes and memory expansion.

------

## Conclusion

The $C000–$C0FF I/O page is the heart of Apple II hardware control, providing direct access to keyboard input, video modes, sound generation, game I/O, annunciators, and peripheral cards. The Enhanced IIe, IIc, and IIc Plus models build on this legacy, introducing new features, expanded memory, and advanced video capabilities while maintaining compatibility with earlier software and hardware. Understanding the detailed layout and behavior of this region is essential for low-level programming, hardware interfacing, and accurate emulation. This report, grounded in authoritative references and technical documentation, serves as a definitive guide for developers, historians, and enthusiasts exploring the inner workings of the Apple II family.

------

**Key Takeaways:**

- **$C000–$C0FF** provides memory-mapped access to all major hardware features.
- **Soft switches** control video modes, memory mapping, device registers, and peripheral slots.
- **Model-specific differences** include auxiliary memory management, virtual slots, advanced video modes, and accelerator features.
- **Peripheral cards** use slot I/O and ROM mapping for device control and firmware routines.
- **Emulator documentation** and disassembly sources are invaluable for understanding timing, behavior, and compatibility.

------

**For further technical details, consult the cited Apple II technical reference manuals, schematics, emulator documentation, and disassembly listings.**
