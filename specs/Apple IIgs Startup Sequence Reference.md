# Apple IIgs Startup Sequence Reference

## Overview

This document describes the complete startup sequence of the Apple IIgs, from the moment the 65C816 CPU reads the RESET vector through to either the GS/OS Finder, ProDOS 8, or the built-in control panel. The IIgs startup is significantly more complex than the Apple IIe/IIc due to its 16-bit processor, Mega II chip for Apple II compatibility, Toolbox ROM, and sophisticated operating system.

**Key Differences from Apple IIe/IIc:**
- 65C816 processor (16-bit native mode)
- 256KB ROM containing Toolbox and compatibility firmware
- Mega II chip handles Apple II compatibility
- Built-in control panel (CDev) accessed via Control-Open Apple-Escape
- GS/OS as primary operating system (ProDOS 8 also supported)
- ADB (Apple Desktop Bus) for keyboard and mouse
- Ensoniq DOC sound chip initialization

---

## Phase 1: CPU Reset and Initial State

### 65C816 Reset Behavior

When power is applied or the RESET line is asserted:

1. **CPU Reset State**
   ```
   - CPU enters emulation mode (E=1)
   - Program Bank Register (PBR) = $00
   - Data Bank Register (DBR) = $00  
   - Direct Page Register (D) = $0000
   - Stack Pointer (S) = $01FF (8-bit stack in page 1)
   - Processor Status: M=1, X=1, I=1, D=0
   - All other registers undefined
   ```

2. **Vector Fetch**
   ```
   CPU reads $00/FFFC ? Low byte of reset vector
   CPU reads $00/FFFD ? High byte of reset vector
   ```
   
   **Note:** In emulation mode, the CPU reads the reset vector from bank $00, which is mapped to ROM on the IIgs.

3. **Reset Vector Values**
   
   | ROM Version | Vector Points To | Description |
   |-------------|-----------------|-------------|
   | ROM 00 | $FA62 | Original IIgs ROM |
   | ROM 01 | $FA62 | First revision |
   | ROM 03 | $FA62 | Most common ROM |

### IIgs Memory State at Reset

At reset, the IIgs memory controller configures:

```
Bank $00: 
  $0000-$BFFF ? Slow RAM (Mega II accessible)
  $C000-$CFFF ? I/O space
  $D000-$FFFF ? ROM (mapped from $FF/D000)

Bank $01:
  $0000-$FFFF ? Auxiliary RAM (Mega II accessible)

Banks $E0-$E1:
  Mega II slow RAM shadowing (initialized later)

Bank $FE-$FF:
  $0000-$FFFF ? ROM
```

---

## Phase 2: ROM Reset Handler

### 2.1 Initial Entry Point ($FA62)

The IIgs ROM reset handler is significantly more complex than the IIe:

```assembly
; IIgs Reset Entry
RESET:      SEI                 ; Disable interrupts
            CLD                 ; Clear decimal mode
            
            ; Force emulation mode for compatibility
            SEC
            XCE                 ; Exchange carry and emulation
            
            ; Initialize stack
            LDX #$FF
            TXS
            
            ; Check for special key combinations
            JSR CHECK_SPECIAL_KEYS
            BCS SPECIAL_BOOT
            
            ; Normal boot path
            JMP COLD_START
```

### 2.2 Special Key Detection

The IIgs checks multiple key combinations at startup:

```assembly
CHECK_SPECIAL_KEYS:
            ; Read ADB keyboard status
            JSR ADB_INIT_MINIMAL
            
            ; Control-Open Apple-Reset = Control Panel
            LDA $C025           ; Modifier keys
            AND #$C1            ; Control + Open Apple
            CMP #$C1
            BEQ GO_CONTROL_PANEL
            
            ; Option key = boot from slot scan
            LDA $C025
            AND #$40            ; Option key
            BNE FORCE_SLOT_SCAN
            
            ; Open Apple-Control-Escape = Control Panel (alternate)
            ; This is checked continuously after boot
            
            CLC
            RTS
```

### 2.3 Key Combinations at Reset

| Keys Held | Result |
|-----------|--------|
| None | Normal boot (check startup slot, then scan) |
| Option | Force slot scan, ignore startup slot |
| Control + Open-Apple | Enter Control Panel immediately |
| Open-Apple + Option | Boot into ProDOS 8 mode (from ROM disk if available) |
| Control + Open-Apple + Option | Enter self-test |
| Shift | (Later ROM) Disable extensions during GS/OS boot |
| Control + Shift + Open-Apple + Option | Clear Parameter RAM (reset all settings) |

---

## Phase 3: Hardware Initialization

### 3.1 Mega II Chip Initialization

The Mega II chip handles Apple II compatibility. It must be initialized before any Apple II-style I/O:

```assembly
INIT_MEGA_II:
            ; Set Mega II to known state
            STA $C029           ; NEWVIDEO - clear all bits
            
            ; Initialize shadowing
            LDA #$00
            STA $C035           ; Shadow register - disable all shadowing initially
            
            ; Set speed control
            LDA #$80            ; Fast mode (2.8 MHz)
            STA $C036           ; CYAREG - speed control
            
            ; Initialize display
            STA $C051           ; TEXT mode
            STA $C054           ; Page 1
            STA $C056           ; LORES
            STA $C00C           ; 40 columns
            
            RTS
```

### 3.2 IIgs-Specific Soft Switches

| Address | Name | Description |
|---------|------|-------------|
| $C022 | SCREENCOLOR | Text color/border color |
| $C023 | VGCINT | VGC interrupt status |
| $C024 | MOUSEDATA | ADB mouse data |
| $C025 | KEYMODREG | Modifier key status |
| $C026 | DATAREG | ADB data register |
| $C027 | KMSTATUS | Keyboard/Mouse status |
| $C029 | NEWVIDEO | Super Hi-Res control |
| $C02B | LANGSEL | ROM bank select |
| $C02D | SLTROMSEL | Slot ROM select |
| $C034 | CLOCKDATA | Clock data |
| $C035 | SHADOW | Memory shadowing control |
| $C036 | CYAREG | Speed/slot control |
| $C039 | SOUNDCTL | Sound control |
| $C03A | SOUNDDATA | Sound data |
| $C03B | SOUNDADRL | Sound address low |
| $C03C | SOUNDADRH | Sound address high |
| $C041 | INTEN | Mega II interrupt enable |
| $C046 | DIAGTYPE | Diagnostic type |
| $C068 | STATEREG | State register |

### 3.3 ADB (Apple Desktop Bus) Initialization

The IIgs must initialize ADB before reading keyboard/mouse:

```assembly
INIT_ADB:
            ; Reset ADB microcontroller
            LDA #$00
            STA $C026           ; Clear data register
            
            ; Wait for ADB controller ready
            LDX #$00
ADB_WAIT:   LDA $C027           ; ADB status
            AND #$80            ; Data available?
            BEQ ADB_READY
            LDA $C026           ; Flush data
            DEX
            BNE ADB_WAIT
            
ADB_READY:  ; Send reset command to all devices
            LDA #$00            ; Reset command
            JSR ADB_SEND_CMD
            
            ; Wait for devices to reset (~3ms)
            JSR WAIT_3MS
            
            ; Poll for keyboard and mouse
            JSR ADB_POLL_DEVICES
            
            RTS
```

### 3.4 Ensoniq DOC Initialization

The Ensoniq 5503 DOC (Digital Oscillator Chip) sound system:

```assembly
INIT_SOUND:
            ; Disable all oscillators
            LDA #$E0            ; Access oscillator control
            STA $C03C           ; Address high
            LDA #$00
            STA $C03B           ; Address low
            
            ; Clear all 32 oscillators
            LDX #$1F
CLR_OSC:    LDA #$00
            STA $C03D           ; Oscillator halt
            DEX
            BPL CLR_OSC
            
            ; Set master volume
            LDA #$00
            STA $C03C
            LDA #$FF            ; Max volume
            STA $C03A           ; Sound data
            
            RTS
```

---

## Phase 4: Memory Test and Sizing

### 4.1 RAM Size Detection

The IIgs can have 256KB to 8MB of RAM. The firmware detects installed RAM:

```assembly
SIZE_RAM:
            ; Start from bank $02 (first expansion bank)
            LDA #$02
            STA BANK_COUNT
            
            ; Test each bank
TEST_BANK:  
            ; Switch to test bank
            LDA BANK_COUNT
            PHA
            PLB                 ; Set data bank
            
            ; Write test pattern
            LDA #$AA
            STA $0000
            LDA #$55
            STA $0001
            
            ; Verify pattern
            LDA $0000
            CMP #$AA
            BNE BANK_END
            LDA $0001
            CMP #$55
            BNE BANK_END
            
            ; Bank is valid
            INC BANK_COUNT
            LDA BANK_COUNT
            CMP #$80            ; Max 8MB
            BNE TEST_BANK
            
BANK_END:   ; BANK_COUNT contains number of valid banks
            LDA #$00
            PHA
            PLB                 ; Restore bank 0
            
            RTS
```

### 4.2 Battery RAM Test

The IIgs has 256 bytes of battery-backed RAM for settings:

```assembly
TEST_BRAM:
            ; Access battery RAM via $C033/$C034
            LDA #$00
            STA $C033           ; BRAM address
            
            ; Read checksum byte
            LDA $C034           ; BRAM data
            STA BRAM_CHECK
            
            ; Calculate expected checksum
            JSR CALC_BRAM_CHECKSUM
            
            ; Compare
            CMP BRAM_CHECK
            BEQ BRAM_OK
            
            ; Invalid - load defaults
            JSR INIT_DEFAULT_BRAM
            
BRAM_OK:    RTS
```

---

## Phase 5: Display Initialization

### 5.1 Text Display Setup

```assembly
INIT_DISPLAY:
            ; Set text mode
            STA $C051           ; TEXT
            STA $C054           ; PAGE1
            STA $C00C           ; 40-column
            
            ; Set text colors (IIgs specific)
            LDA #$F0            ; White text, black background
            STA $C022           ; SCREENCOLOR
            
            ; Clear screen
            JSR HOME
            
            ; Display startup message
            JSR PRINT_STARTUP_MSG
            
            RTS
```

### 5.2 Super Hi-Res Initialization (for GS/OS)

If booting into GS/OS, the display is reconfigured:

```assembly
INIT_SHR:
            ; Enable Super Hi-Res
            LDA $C029
            ORA #$C1            ; SHR on, linear addressing, 200 lines
            STA $C029
            
            ; Enable shadowing for SHR area
            LDA $C035
            ORA #$08            ; Enable SHR shadowing
            STA $C035
            
            ; Clear SHR screen memory ($E1/2000-$E1/9FFF)
            ; ... (32KB of screen memory)
            
            ; Initialize scan line control bytes
            ; $E1/9D00-$E1/9DFF = color palettes
            ; $E1/9E00-$E1/9FFF = SCBs
            
            RTS
```

---

## Phase 6: Boot Device Selection

### 6.1 Reading Startup Slot from Battery RAM

The IIgs stores the preferred startup device in battery RAM:

```assembly
GET_STARTUP_SLOT:
            ; Battery RAM byte $0B = startup slot/device
            LDA #$0B
            STA $C033
            LDA $C034           ; Read startup slot
            
            ; Decode startup device
            ; Bits 7-4: Startup method
            ;   0 = Scan slots
            ;   1 = Slot (bits 3-0 = slot number)
            ;   2 = SmartPort device
            ;   3 = SCSI ID
            ;   4 = AppleTalk
            ;   5 = ROM disk (ROM 01+)
            ; Bits 3-0: Device/slot number
            
            PHA
            AND #$F0            ; Get method
            LSR A
            LSR A
            LSR A
            LSR A
            TAX                 ; X = method
            PLA
            AND #$0F            ; A = device number
            
            RTS
```

### 6.2 Boot Methods

| Method | Value | Description |
|--------|-------|-------------|
| Scan | $0n | Scan slots 7 to n, then lower slots |
| Slot | $1n | Boot directly from slot n |
| SmartPort | $2n | Boot from SmartPort unit n |
| SCSI | $3n | Boot from SCSI ID n |
| AppleTalk | $4n | Boot from network |
| ROM | $5n | Boot from ROM disk (if present) |

### 6.3 Slot Scanning (if Method = 0)

```assembly
SCAN_SLOTS:
            ; Get starting slot from lower nibble (default 7)
            LDX #$07
            
SCAN_LOOP:
            ; Check for card in slot
            JSR CHECK_SLOT_X
            BCS NEXT_SLOT       ; No card
            
            ; Check if bootable
            JSR CHECK_BOOTABLE_X
            BCS NEXT_SLOT       ; Not bootable
            
            ; Found bootable device
            JMP BOOT_FROM_SLOT_X
            
NEXT_SLOT:
            DEX
            BNE SCAN_LOOP       ; Slots 7-1
            
            ; Check built-in SmartPort (slot 5)
            JSR CHECK_INTERNAL_DRIVE
            BCC BOOT_INTERNAL
            
            ; Check ROM disk
            JSR CHECK_ROM_DISK
            BCC BOOT_ROM_DISK
            
            ; No bootable device - enter BASIC or control panel
            JMP GO_BASIC
```

---

## Phase 7: Device-Specific Boot Sequences

### 7.1 Scenario: Boot from Internal 3.5" Drive (SmartPort)

**Configuration:** Built-in 3.5" drive with ProDOS or GS/OS disk

**Boot Sequence:**

#### Stage 0: SmartPort Entry (Internal Drive)

```assembly
; Internal SmartPort is at slot 5 equivalent
BOOT_INTERNAL:
            ; The IIgs internal drive firmware is in ROM
            
            ; Initialize SmartPort
            JSR $C500           ; Internal SmartPort entry
            
            ; Read block 0 from drive 1
SMARTPORT_CMD:
            JSR $C50D           ; SmartPort dispatch
            DB  $01             ; READ_BLOCK command
            DW  SP_PARAMS
            BCS BOOT_ERROR
            
            ; Block 0 now at $0800
            JMP $0801
            
SP_PARAMS:
            DB  $03             ; Parameter count
            DB  $01             ; Unit 1
            DW  $0800           ; Buffer address
            DW  $0000           ; Block 0
```

#### Stage 1: ProDOS/GS/OS Boot Block

The boot block at block 0 contains loader code:

```assembly
; Boot block header
$0800:      DB  $01             ; ProDOS boot block marker
            DB  $70             ; Block count for ProDOS loader
            ; ... more header ...
            
$0801:      ; Loader code begins
            ; Detect if this is GS/OS or ProDOS 8
            
            ; Check for GS/OS signature
            LDA BLOCK_0_SIG
            CMP #$4C            ; JMP opcode
            BNE PRODOS8_BOOT
            
            ; GS/OS boot
            JMP GSOS_LOADER
            
PRODOS8_BOOT:
            ; Load ProDOS 8
            JMP PRODOS8_LOADER
```

### 7.2 Scenario: GS/OS Boot

**Configuration:** GS/OS System Disk in drive

**Full GS/OS Boot Sequence:**

#### Stage 1: GS/OS Loader (First Stage)

```assembly
GSOS_LOADER:
            ; Switch to native mode
            CLC
            XCE                 ; Clear emulation mode
            
            ; Set 16-bit registers
            REP #$30            ; M=0, X=0 (16-bit A, X, Y)
            
            ; Load GS/OS kernel
            ; The kernel is in SYSTEM/START.GS.OS
            
            ; Read the directory to find file
            JSR FIND_KERNEL_FILE
            BCS BOOT_FAIL
            
            ; Load kernel into memory
            ; Kernel loads at $01/0000 and up
            JSR LOAD_KERNEL
            BCS BOOT_FAIL
            
            ; Transfer control to kernel
            JML KERNEL_ENTRY    ; Long jump to kernel
```

#### Stage 2: GS/OS Kernel Initialization

```assembly
KERNEL_ENTRY:
            ; GS/OS kernel entry (typically $01/0000)
            
            ; Initialize Memory Manager
            JSR INIT_MEMORY_MGR
            
            ; Initialize Tool Locator
            JSR INIT_TOOL_LOCATOR
            
            ; Load and initialize tools
            JSR LOAD_TOOLS
            
            ; Initialize Event Manager
            JSR INIT_EVENT_MGR
            
            ; Initialize Device Manager
            JSR INIT_DEVICE_MGR
            
            ; Mount boot volume
            JSR MOUNT_BOOT_VOL
            
            ; Load Finder or startup application
            JML LOAD_FINDER
```

#### Stage 3: Finder Launch

```assembly
LOAD_FINDER:
            ; Open SYSTEM/FINDER
            ; If not found, try SYSTEM/START
            
            ; Load Finder code
            JSR LOAD_APP_FINDER
            
            ; Initialize Finder
            JSR INIT_FINDER
            
            ; Enter main event loop
            JML FINDER_MAIN_LOOP
```

**Final State (GS/OS):**
- 65816 in native mode (16-bit)
- GS/OS kernel in memory ($01/0000+)
- Memory Manager, Event Manager initialized
- Finder displaying desktop
- Super Hi-Res mode active (640x200 or 320x200)
- All mounted volumes visible on desktop

**Timing:** ~15-45 seconds depending on extensions

---

### 7.3 Scenario: ProDOS 8 Boot on IIgs

**Configuration:** ProDOS 8 disk (5.25" or 3.5")

**Boot Sequence:**

#### Stage 0: Slot Boot (Disk II or SmartPort)

Same as IIe - reads boot sector from device.

#### Stage 1: ProDOS 8 Load

```assembly
$0801:      ; ProDOS 8 loader
            ; Stays in emulation mode
            
            ; Load ProDOS into Language Card
            ; (same as IIe)
            
            ; Initialize MLI
            JSR PRODOS_INIT
            
            ; Set machine ID for IIgs
            LDA #$B3            ; IIgs with 128K+
            STA $BF96           ; MACHID
            
            ; Find and load BASIC.SYSTEM
            JMP $2000
```

#### Stage 2: BASIC.SYSTEM

Same as IIe - hooks Applesoft, displays prompt.

**Final State (ProDOS 8 on IIgs):**
- 65816 in emulation mode (8-bit compatible)
- ProDOS 8 in language card
- 40 or 80 column text mode
- Running at 1 MHz for compatibility (or 2.8 MHz if software supports)

---

### 7.4 Scenario: ROM Disk Boot (ROM 01+)

**Configuration:** IIgs ROM 01 or later with 512KB+ RAM

ROM 01 and ROM 03 include a built-in RAM disk image containing:
- ProDOS 8
- BASIC.SYSTEM
- Utilities

```assembly
CHECK_ROM_DISK:
            ; Check ROM version
            LDA $FE/1EFF        ; ROM version byte
            CMP #$01            ; ROM 01 or later?
            BCC NO_ROM_DISK
            
            ; Check if ROM disk enabled in Battery RAM
            LDA #$1C            ; ROM disk setting byte
            STA $C033
            LDA $C034
            AND #$80
            BEQ NO_ROM_DISK     ; Disabled
            
            ; ROM disk is available
            ; It appears as SmartPort unit at slot 5, drive 2
            
            ; Initialize ROM disk
            JSR INIT_ROM_DISK
            
            CLC
            RTS
            
NO_ROM_DISK:
            SEC
            RTS

BOOT_ROM_DISK:
            ; Boot from ROM disk
            ; Uses SmartPort protocol
            
            JSR SMARTPORT_ENTRY
            DB  $01             ; READ_BLOCK
            DW  ROM_DISK_PARAMS
            BCS BOOT_ERROR
            
            JMP $0801
            
ROM_DISK_PARAMS:
            DB  $03
            DB  $02             ; Unit 2 (ROM disk)
            DW  $0800
            DW  $0000           ; Block 0
```

---

### 7.5 Scenario: SCSI Hard Drive Boot

**Configuration:** Apple High-Speed SCSI card in slot 7

```assembly
BOOT_SCSI:
            ; SCSI cards present as SmartPort
            ; with extended device support
            
            ; Get SCSI boot ID from Battery RAM
            LDA #$0C            ; SCSI ID setting
            STA $C033
            LDA $C034
            AND #$07            ; SCSI ID 0-7
            STA SCSI_ID
            
            ; Initialize SCSI card
            JSR $C700           ; Slot 7 ROM entry
            
            ; Select boot device
            LDA SCSI_ID
            JSR SCSI_SELECT
            
            ; Read block 0
            JSR SMARTPORT_READ_BLOCK
            
            JMP $0801
```

**ProDOS vs GS/OS on SCSI:**

The boot block determines which OS loads:

| Boot Block Type | OS Loaded |
|-----------------|-----------|
| ProDOS 8 header | ProDOS 8 |
| GS/OS header | GS/OS |
| HFS (Mac) | Not supported (error) |

---

### 7.6 Scenario: Network (AppleTalk) Boot

**Configuration:** AppleTalk card, network server available

```assembly
BOOT_APPLETALK:
            ; AppleTalk boot requires:
            ; 1. AppleTalk card installed
            ; 2. NetBoot server on network
            
            ; Initialize AppleTalk
            JSR INIT_APPLETALK
            BCS NETWORK_ERROR
            
            ; Look for NetBoot server
            JSR FIND_NETBOOT_SERVER
            BCS NO_SERVER
            
            ; Request boot image
            JSR REQUEST_BOOT_IMAGE
            BCS BOOT_FAIL
            
            ; Image loaded at $0800+
            JMP $0801
```

---

## Phase 8: Control Panel (CDev) Entry

### 8.1 Entering the Control Panel

The Control Panel can be entered at any time via Control-Open Apple-Escape:

```assembly
; Interrupt handler checks for Control Panel request
CHECK_CONTROL_PANEL:
            ; Check key combination
            LDA $C025           ; Modifier keys
            AND #$C0            ; Control + Open Apple
            CMP #$C0
            BNE NOT_CP
            
            LDA $C000           ; Key pressed
            CMP #$9B            ; Escape
            BNE NOT_CP
            
            ; Enter Control Panel
            JMP CONTROL_PANEL_ENTRY
            
NOT_CP:     RTI

CONTROL_PANEL_ENTRY:
            ; Save current state
            JSR SAVE_MACHINE_STATE
            
            ; Initialize Control Panel display
            JSR INIT_CP_DISPLAY
            
            ; Main Control Panel loop
            JMP CP_MAIN_LOOP
```

### 8.2 Control Panel Options

The Control Panel provides access to:

| Option | Function |
|--------|----------|
| Slots | Configure slot assignments |
| Display | Monitor type, colors |
| Sound | Volume, startup beep |
| Startup | Startup device, RAM disk |
| System Speed | Fast (2.8 MHz) or Normal (1 MHz) |
| Clock | Set date and time |
| Keyboard | Layout, repeat rate |
| Mouse | Tracking speed |
| Network | AppleTalk settings |
| Printer | Port configuration |
| Modem | Port configuration |
| Memory | RAM size, expansion |

---

## Phase 9: GS/OS Toolbox Initialization

### 9.1 Tool Locator

GS/OS uses a tool-based architecture. Tools must be loaded and started:

```assembly
INIT_TOOLS:
            ; Initialize Tool Locator (Tool #1)
            ; Tool Locator is in ROM, always available
            
            ; Start Tool Locator
            PushWord #$0100     ; Tool number, version
            _TLStartUp
            
            ; Initialize Memory Manager (Tool #2)
            PushWord #$0000     ; User ID = 0 (system)
            _MMStartUp
            
            ; Initialize Miscellaneous Tools (Tool #3)
            _MTStartUp
            
            ; Continue loading tools from SYSTEM/TOOLS folder...
```

### 9.2 Standard Tool Numbers

| Tool # | Name | Purpose |
|--------|------|---------|
| $01 | Tool Locator | Manages all tools |
| $02 | Memory Manager | Memory allocation |
| $03 | Miscellaneous Tools | Various utilities |
| $04 | QuickDraw II | Graphics primitives |
| $05 | Desk Manager | Desktop Accessories |
| $06 | Event Manager | Event handling |
| $07 | Window Manager | Window management |
| $08 | Control Manager | GUI controls |
| $09 | Menu Manager | Menu handling |
| $0A | LineEdit | Text editing |
| $0B | Dialog Manager | Dialog boxes |
| $0C | Scrap Manager | Clipboard |
| $0E | Integer Math | Integer math |
| $0F | Text Tools | Text manipulation |
| $10 | SANE | Floating-point math |
| $11 | Font Manager | Font handling |
| $12 | List Manager | List controls |
| $14 | Print Manager | Printing |
| $15 | Note Synthesizer | Sound |
| $16 | Note Sequencer | Music sequencing |
| $1A | Sound Tool Set | Sound playback |
| $1B | ADB Tool Set | ADB device access |
| $1C | MIDI Tool Set | MIDI interface |
| $20 | Video Overlay | Video digitizer |
| $22 | TextEdit | Text editing (advanced) |
| $23 | Resource Manager | Resource files |

---

## ROM Versions and Differences

### ROM Version Detection

```assembly
; ROM version byte at $FE/1EFF
GET_ROM_VERSION:
            LDA $FE1EFF
            ; $00 = ROM 00 (original)
            ; $01 = ROM 01 
            ; $03 = ROM 03 (most common)
            RTS
```

### ROM Version Features

| Version | Features |
|---------|----------|
| ROM 00 | Original release, limited slot support |
| ROM 01 | ROM disk, improved ADB, bug fixes |
| ROM 03 | Memory expansion support, more tools, improved compatibility |

### ROM 03 Specific Startup

ROM 03 adds several boot features:

```assembly
ROM03_STARTUP:
            ; Check for ROM 03
            LDA $FE1EFF
            CMP #$03
            BNE OLDER_ROM
            
            ; ROM 03 specific initialization
            ; - Support for >1MB RAM
            ; - Improved AppleTalk
            ; - More built-in tools
            
            ; Initialize extended memory
            JSR INIT_EXTENDED_MEM
            
            ; Check for System 6 boot
            JSR CHECK_SYSTEM6
```

---

## Memory Map at Various Boot Stages

### After ROM Reset Handler

```
Bank $00:
  $0000-$00FF : Direct Page (zero page)
  $0100-$01FF : Stack
  $0200-$03FF : System work area
  $0400-$07FF : Text Page 1
  $0800-$BFFF : Available RAM
  $C000-$C0FF : I/O space
  $C100-$CFFF : Slot ROM area
  $D000-$FFFF : ROM (via $FF bank)

Bank $E0-$E1: Mega II shadowing not yet active
Bank $FF: System ROM visible
```

### After ProDOS 8 Boot

```
Bank $00:
  $0000-$00FF : Direct Page
  $0100-$01FF : Stack
  $0200-$02FF : Input buffer
  $0300-$03FF : Vectors
  $0800-$BEFF : User program area
  $BF00-$BFFF : ProDOS global page
  $C000-$C0FF : I/O
  $D000-$FFFF : ProDOS/Language Card

CPU: Emulation mode (E=1)
Speed: 1 MHz or 2.8 MHz
```

### After GS/OS Boot

```
Bank $00:
  $0000-$00FF : Direct Page 0
  $0100-$01FF : Stack (may move)
  $0400-$07FF : Text Page (shadowed to $E0)
  $2000-$9FFF : Hi-Res/SHR (shadowed to $E0-$E1)
  $C000-$CFFF : I/O

Bank $01:
  $0000-$FFFF : GS/OS kernel, tools, heap

Bank $E0-$E1:
  $2000-$9FFF : Super Hi-Res display
  $9D00-$9DFF : SHR palettes
  $9E00-$9FFF : SHR SCBs

Banks $02+:
  Application memory, documents

CPU: Native mode (E=0)
Speed: 2.8 MHz (fast mode)
```

---

## Timing Summary

| Boot Stage | ProDOS 8 | GS/OS (minimal) | GS/OS (full) |
|------------|----------|-----------------|--------------|
| Reset to ROM | 10ms | 10ms | 10ms |
| Hardware init | 200ms | 200ms | 200ms |
| ADB init | 100ms | 100ms | 100ms |
| Memory test | 500ms | 500ms | 500ms |
| Drive spinup | 500ms | 500ms | 500ms |
| Boot block | 200ms | 200ms | 200ms |
| OS load | 2000ms | 5000ms | 10000ms |
| Tools/extensions | - | 2000ms | 15000ms |
| Finder | - | 2000ms | 5000ms |
| **Total** | **~4s** | **~11s** | **~30-45s** |

---

## Error Conditions

### Startup Errors

| Error | Cause | Display |
|-------|-------|---------|
| RAM error | Bad RAM chip | Checkerboard pattern + beep |
| ROM error | ROM checksum fail | Alternating beeps |
| No boot device | No disk/drive | "Check startup device" |
| GS/OS error | Missing system file | "Unable to load System file" |
| Tool error | Missing/bad tool | Error dialog |

### Recovery Options

| Action | Method |
|--------|--------|
| Control Panel | Control-Open Apple-Escape |
| Force slot scan | Hold Option at startup |
| Boot ProDOS 8 | Hold Open Apple-Option |
| Self-test | Control-Open Apple-Option |
| Clear settings | Control-Shift-Open Apple-Option |

---

## Emulation Considerations

### Critical Components

1. **65C816 mode switching**: Emulation ? Native mode
2. **Bank switching**: Must accurately emulate all 256 banks
3. **Mega II timing**: 1 MHz vs 2.8 MHz speed control
4. **Shadowing**: Memory mirroring for display
5. **ADB protocol**: Keyboard/mouse communication

### Trap Points for IIgs

| Address | Routine | Emulator Action |
|---------|---------|-----------------|
| $00/FA62 | RESET | Native init, hardware setup |
| $C500 | SMARTPORT | Native: SmartPort dispatch |
| $E1/0000 | TOOLS | Tool dispatch table |
| $E1/0200 | GS/OS | GS/OS entry points |

### State Preservation

| Location | Purpose | Notes |
|----------|---------|-------|
| $C033-$C034 | Battery RAM | 256 bytes, must persist |
| $BF96 | MACHID | Must return IIgs ID |
| $FE1EFF | ROM version | $00, $01, or $03 |
| Bank $00 | Mega II area | Shadowing state |

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-12-30 | Initial specification |
