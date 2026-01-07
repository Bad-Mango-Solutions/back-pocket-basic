# Apple IIe/IIc Startup Sequence Reference

## Overview

This document describes the complete startup sequence of Apple IIe and IIc systems, from the moment the CPU reads the RESET vector at $FFFC-$FFFD through to either the Monitor prompt, BASIC prompt, or a booted operating system. The sequence covers hardware initialization, ROM self-test, slot scanning, and the boot process for various device configurations.

---

## Phase 1: CPU Reset and Vector Fetch

### Hardware Reset Sequence

When the Apple II is powered on or the RESET line is asserted:

1. **CPU Reset State**
   - CPU holds in reset state for several clock cycles
   - All registers cleared (A=X=Y=0, S=$00 initially)
   - Interrupt disable flag set (I=1)
   - Decimal mode cleared (D=0)
   - Stack pointer set to $FD after pushing fake return address

2. **Vector Fetch**
   ```
   CPU reads $FFFC ? Low byte of reset vector
   CPU reads $FFFD ? High byte of reset vector
   ```

3. **Jump to Reset Handler**
   - Apple IIe: Vector points to $FA62
   - Apple IIc: Vector points to $FF00 (then redirects)

### Reset Vector Values

| System | Vector Address | Points To | Handler Name |
|--------|---------------|-----------|--------------|
| Apple IIe | $FFFC-$FFFD | $FA62 | RESET |
| Apple IIe Enhanced | $FFFC-$FFFD | $FA62 | RESET |
| Apple IIc | $FFFC-$FFFD | $FF00 | RESET (redirector) |
| Apple IIc+ | $FFFC-$FFFD | $FF00 | RESET (redirector) |

---

## Phase 2: ROM Reset Handler ($FA62)

The reset handler performs essential hardware initialization.

### 2.1 Initial Hardware Setup

```assembly
; Pseudocode for reset handler entry
RESET:  CLD             ; Clear decimal mode
        LDX #$FF
        TXS             ; Initialize stack pointer to $FF
        
        ; Check for warm start vs cold start
        LDA $03F4       ; PWREDUP - Power-up byte
        CMP #$A5        ; Magic value for warm start
        BNE COLD_START  ; If not $A5, do cold start
        
        ; Warm start - check SOFTEV vector
        LDA $03F3       ; SOFTEV checksum
        EOR $03F2       ; 
        EOR #$A5        ; Validate checksum
        BNE COLD_START  ; Invalid - do cold start
        
        JMP ($03F2)     ; Jump through SOFTEV (warm start)
```

### 2.2 Cold Start Initialization

```assembly
COLD_START:
        ; Initialize soft switches to known state
        STA $C000       ; 80STORE off (IIe/IIc)
        STA $C002       ; Read main RAM
        STA $C004       ; Write main RAM
        STA $C00C       ; 80COL off
        STA $C00E       ; ALTCHAR off
        STA $C006       ; Slot ROM (vs internal)
        STA $C008       ; Main zero page
        
        ; Set text mode
        STA $C051       ; TEXT mode
        STA $C054       ; PAGE1
        STA $C056       ; LORES (not HIRES)
        STA $C052       ; FULLSCREEN (not MIXED)
        
        ; Initialize screen
        JSR HOME        ; Clear screen ($FC58)
```

### 2.3 Memory Test (Optional)

On cold boot, the ROM may perform a memory test:

```assembly
        ; Quick RAM test (page zero and stack)
        LDX #$00
MEM_TST:
        LDA #$00
        STA $00,X
        LDA #$FF
        STA $00,X
        CMP $00,X
        BNE MEM_ERR
        INX
        BNE MEM_TST
```

### 2.4 Zero Page Initialization

Critical zero page locations are initialized:

| Address | Value | Purpose |
|---------|-------|---------|
| $20     | $00   | WNDLFT - Window left |
| $21     | $28   | WNDWDTH - Window width (40) |
| $22     | $00   | WNDTOP - Window top |
| $23     | $18   | WNDBTM - Window bottom (24) |
| $24     | $00   | CH - Cursor horizontal |
| $25     | $00   | CV - Cursor vertical |
| $32     | $00   | INVFLG - Inverse flag |
| $33     | $DD   | PROMPT - Default prompt character |

### 2.5 I/O Hook Setup

```assembly
        ; Set default I/O vectors
        LDA #<KEYIN     ; $FD1B
        STA $38         ; KSW low
        LDA #>KEYIN
        STA $39         ; KSW high
        
        LDA #<COUT1     ; $FDF0
        STA $36         ; CSW low
        LDA #>COUT1
        STA $37         ; CSW high
```

---

## Phase 3: Autostart ROM Behavior

The Apple IIe and IIc include "Autostart ROM" which automatically scans for bootable devices.

### 3.1 Determining Boot Action

The autostart ROM checks several conditions:

```assembly
        ; Check reset key combination
        LDA $C061       ; Open-Apple key
        AND $C062       ; Closed-Apple key  
        BMI GO_MONITOR  ; Both keys = enter Monitor
        
        LDA $C061       ; Open-Apple key
        BMI GO_BASIC    ; Open-Apple alone = BASIC
        
        ; Check for Control-Reset
        ; (internal flag set during reset)
        BIT CTRL_RESET_FLAG
        BMI SCAN_SLOTS  ; Normal boot - scan slots
        
        JMP BASIC_COLD  ; Warm reset goes to BASIC
```

### 3.2 Key Combinations at Reset

| Keys Held | Result |
|-----------|--------|
| None | Scan slots for bootable device |
| Open-Apple | Enter Applesoft BASIC |
| Open-Apple + Closed-Apple | Enter Monitor |
| Control (during reset) | Cold boot, scan slots |
| Control + Open-Apple + Reset | Run self-test (IIe/IIc) |

---

## Phase 4: Slot Scanning

If no special keys are held, the autostart ROM scans expansion slots for bootable devices.

### 4.1 Scan Order

**Apple IIe:** Scans slots 7 down to 1, then slot 0 (internal)
**Apple IIc:** Scans built-in devices in fixed order

```assembly
SCAN_SLOTS:
        LDX #$07        ; Start with slot 7
SCAN_LOOP:
        ; Calculate slot ROM address: $Cn00
        TXA
        ORA #$C0
        STA SCAN_ADDR+1
        LDA #$00
        STA SCAN_ADDR
        
        ; Check for card presence
        ; (signature bytes at offset $05, $07, $0B)
        LDY #$05
        LDA (SCAN_ADDR),Y
        CMP #$20        ; Must be $20
        BNE NEXT_SLOT
        
        LDY #$07
        LDA (SCAN_ADDR),Y
        CMP #$00        ; Must be $00
        BNE NEXT_SLOT
        
        LDY #$0B
        LDA (SCAN_ADDR),Y
        CMP #$00        ; Must be $00
        BNE NEXT_SLOT
        
        ; Valid card found - check if bootable
        LDY #$FF
        LDA (SCAN_ADDR),Y   ; Byte at $CnFF
        BEQ NEXT_SLOT       ; $00 = not bootable
        
        ; Boot from this slot
        JMP (SCAN_ADDR)     ; Jump to $Cn00
        
NEXT_SLOT:
        DEX
        BPL SCAN_LOOP
        
        ; No bootable device found
        JMP BASIC_COLD
```

### 4.2 Slot ROM Signature Bytes

A valid peripheral card has these signature bytes:

| Offset | Required Value | Purpose |
|--------|---------------|---------|
| $Cn05  | $20           | Signature byte 1 |
| $Cn07  | $00           | Signature byte 2 |
| $Cn0B  | $00           | Signature byte 3 |
| $CnFF  | Non-zero      | Boot indicator (entry offset or $00) |

### 4.3 Bootable vs Non-Bootable Cards

| $CnFF Value | Meaning |
|-------------|---------|
| $00         | Not bootable |
| $01-$FF     | Bootable; value is entry point offset |

Common values:
- Disk II: $00 at $C6FF (but special handling)
- SmartPort: $00 at offset (uses $Cn00 entry)
- SCSI cards: Varies by manufacturer

---

## Phase 5: Device-Specific Boot Sequences

### 5.1 Scenario: No Disk Devices

**Configuration:** No cards in slots, or no bootable cards

**Boot Sequence:**
1. Slot scan completes with no bootable device
2. Jump to BASIC cold start ($E000)
3. Initialize BASIC interpreter
4. Clear program memory
5. Display "]" prompt

```
$E000: BASIC cold start
       - Set TXTAB to $0801
       - Set VARTAB, ARYTAB, STREND
       - Set HIMEM ($73-$74) to $9600 or top of RAM
       - Display "]" prompt
       - Enter command loop
```

**Final State:**
- Text mode, 40-column display
- "]" prompt visible
- Cursor blinking
- Ready for BASIC commands

---

### 5.2 Scenario: Disk II Only

**Configuration:** Disk II controller in slot 6 (typical)

**Boot Sequence:**

#### Stage 0: ROM Boot ($C600)

```assembly
; Slot 6 ROM entry point
$C600:  LDX #$20        ; Timing constant
        LDY #$00        ; Sector 0
        LDA #$01        ; Track 0
        
        ; Turn on drive motor
        LDA $C0E9       ; MOTORON
        LDA $C0EA       ; Drive 1 select
        
        ; Seek to track 0
        JSR SEEK_TRACK
        
        ; Wait for motor spinup (~1 second)
        JSR WAIT_MOTOR
```

#### Stage 1: Boot Sector 0

```assembly
        ; Read boot sector 0 into $0800
        LDA #$08
        STA DEST_PAGE
        JSR READ_SECTOR ; Read T0S0 into $0800
        
        ; Jump to boot sector code
        JMP $0801
```

#### Stage 2: Boot Sector Code ($0801)

The boot sector contains the **Boot 1** loader:

```assembly
; Boot sector 0 code (DOS 3.3 or ProDOS)
$0801:  ; For ProDOS:
        ; Load PRODOS file from disk
        ; (sectors 0-9 of track 0)
        
        LDX #$00        ; Starting sector
LOAD_LOOP:
        JSR READ_SECTOR
        INC DEST_PAGE   ; Next page
        INX             ; Next sector
        CPX #$0A        ; 10 sectors
        BNE LOAD_LOOP
        
        JMP $2000       ; Jump to ProDOS loader
```

#### Stage 3: ProDOS Load

```assembly
$2000:  ; ProDOS secondary loader
        ; Load rest of ProDOS into language card
        ; Initialize MLI
        ; Set up global page ($BF00)
        ; Find and load STARTUP system file
```

#### Stage 4: STARTUP or BASIC.SYSTEM

```assembly
        ; If STARTUP exists on disk:
        ;   Load and execute it
        ; Else if BASIC.SYSTEM exists:
        ;   Load at $2000
        ;   Initialize BASIC.SYSTEM
        ;   Display "BASIC.SYSTEM" message
        ;   Enter BASIC prompt
```

**Final State (ProDOS + BASIC.SYSTEM):**
- ProDOS loaded in language card
- BASIC.SYSTEM at $2000-$9A00
- "]" prompt displayed
- Prefix set to boot volume

**Timing:** ~3-5 seconds for full ProDOS boot

---

### 5.3 Scenario: SmartPort Only

**Configuration:** SmartPort device (e.g., UniDisk 3.5, Apple 3.5 Drive) in slot 5

**Boot Sequence:**

#### SmartPort Identification

SmartPort devices have additional signature bytes:

| Offset | Value | Meaning |
|--------|-------|---------|
| $Cn01  | $20   | SmartPort signature |
| $Cn03  | $00   | SmartPort signature |
| $Cn05  | $03   | SmartPort signature |
| $Cn07  | $00   | SmartPort (vs Disk II = $3C) |

#### Stage 0: SmartPort Boot Entry

```assembly
; SmartPort boot entry
$C500:  ; (or $Cn00 for slot n)
        
        ; Initialize SmartPort interface
        JSR SMARTPORT_INIT
        
        ; Read block 0 from unit 1
        LDA #$00
        STA BLOCK_LO
        STA BLOCK_HI
        LDA #$01        ; Unit 1
        JSR READ_BLOCK
        
        ; Block 0 loaded at $0800
        JMP $0801
```

#### SmartPort Call Convention

```assembly
; SmartPort driver entry: $Cn00 + dispatch offset
; Dispatch offset at $CnFF

SMARTPORT_CALL:
        JSR $Cn00       ; Entry point
        DB  CMD         ; Command code
        DW  PARM_LIST   ; Parameter pointer
        ; Returns: Carry clear = success
        ;          Carry set = error, A = error code
```

#### SmartPort Commands Used During Boot

| Code | Command | Description |
|------|---------|-------------|
| $00  | STATUS  | Get device status |
| $01  | READBLOCK | Read 512-byte block |
| $02  | WRITEBLOCK | Write 512-byte block |
| $03  | FORMAT  | Format device |
| $04  | CONTROL | Device-specific control |
| $05  | INIT    | Initialize device |

#### Stage 1: ProDOS Bootstrap

SmartPort block 0 contains a boot block:

```assembly
$0800:  ; Boot block header
        DB $01          ; Boot block indicator
        ; ... loader code ...
        
$0801:  ; Load ProDOS from disk
        ; SmartPort uses 512-byte blocks
        ; ProDOS is at blocks 0-15 (approximately)
        
        LDA #$00        ; Start at block 0
        STA BLOCK_NUM
LOAD_PRODOS:
        JSR SMARTPORT_READ
        INC BLOCK_NUM
        INC DEST_PAGE
        INC DEST_PAGE   ; 2 pages per block
        LDA BLOCK_NUM
        CMP #$10        ; 16 blocks
        BNE LOAD_PRODOS
        
        JMP $2000
```

**Final State:** Same as Disk II - ProDOS loaded, BASIC.SYSTEM ready

**Timing:** ~2-3 seconds (faster than 5.25" disk)

---

### 5.4 Scenario: Disk II and SmartPort

**Configuration:**
- Disk II in slot 6
- SmartPort (3.5" drive) in slot 5

**Boot Priority:**

The autostart ROM scans slots 7?1, so:
1. Slot 7 checked first (usually empty)
2. Slot 6 (Disk II) found and checked
3. If slot 6 is bootable ? boot from Disk II
4. If slot 6 not bootable ? continue to slot 5
5. Slot 5 (SmartPort) found and checked
6. If slot 5 is bootable ? boot from SmartPort

**Controlling Boot Device:**

| Method | Effect |
|--------|--------|
| PR#6 | Boot from slot 6 explicitly |
| PR#5 | Boot from slot 5 explicitly |
| No disk in 5.25" drive | Falls through to slot 5 |
| Both drives have disks | Boots from slot 6 (higher priority) |

**Mixed Device ProDOS Environment:**

After ProDOS boots (from either device), both devices are available:

```
/SLOT6DISK     ; 5.25" Disk II volume
/SLOT5DISK     ; 3.5" SmartPort volume
```

**Boot Sequence with Disk II Primary:**

```assembly
; Slot 6 boot begins
$C600:  ; Normal Disk II boot
        ; ... read boot sectors ...
        
        ; ProDOS loads
$2000:  ; ProDOS initialization
        ; Scan all slots for devices
        ; Register Disk II in slot 6
        ; Register SmartPort in slot 5
        
        ; Device table populated:
        ; Unit $60 = Slot 6, Drive 1
        ; Unit $D0 = Slot 6, Drive 2
        ; Unit $50 = Slot 5, Drive 1
        ; Unit $D0 = Slot 5, Drive 2 (if present)
```

---

### 5.5 Scenario: SCSI/HDD and SmartPort

**Configuration:**
- SCSI card (e.g., Apple High-Speed SCSI) in slot 7
- SmartPort (3.5" drive) in slot 5

**SCSI Card Identification:**

SCSI cards typically identify as SmartPort devices with extended capabilities:

| Offset | Value | Meaning |
|--------|-------|---------|
| $Cn01  | $20   | SmartPort signature |
| $Cn03  | $00   | SmartPort signature |
| $Cn05  | $03   | SmartPort signature |
| $Cn07  | $00   | SmartPort type |
| $CnFB  | $xx   | Extended SmartPort ID |
| $CnFF  | $xx   | Boot entry offset |

**Boot Priority:**

1. Slot 7 (SCSI) - highest priority
2. Boot from SCSI hard drive
3. SmartPort in slot 5 available after boot

**SCSI Boot Sequence:**

```assembly
; SCSI slot 7 entry
$C700:  ; SCSI controller initialization
        
        ; Issue SCSI INQUIRY to find devices
        JSR SCSI_SCAN
        
        ; Find bootable device (usually SCSI ID 0)
        LDA #$00        ; SCSI ID 0
        JSR SCSI_SELECT
        
        ; Read block 0
        JSR SCSI_READ_BLOCK
        
        ; Boot block at $0800
        JMP $0801
```

**SCSI Advantages:**

- Much faster boot (~1-2 seconds)
- Large volumes supported (up to 32MB per volume in ProDOS)
- Multiple partitions appear as separate volumes

**ProDOS Device Registration:**

```
; After SCSI boot, device table:
/HARD1         ; SCSI partition 1 (boot volume)
/HARD2         ; SCSI partition 2
/HARD3         ; SCSI partition 3
/SLOT5         ; SmartPort 3.5" drive
```

**Extended SmartPort Status:**

SCSI cards often support extended SmartPort calls:

```assembly
; Extended status call
        JSR SMARTPORT
        DB  $00         ; STATUS command
        DW  STATUS_PARMS
        
STATUS_PARMS:
        DB  $03         ; Parameter count
        DB  UNIT        ; Unit number
        DW  BUFFER      ; Status buffer
        DB  $03         ; Status code: Device Info Block
```

---

## Phase 6: ProDOS Initialization

Once ProDOS begins loading, regardless of boot device:

### 6.1 ProDOS Memory Setup

```assembly
$2000:  ; ProDOS loader entry
        
        ; Relocate ProDOS to language card
        ; Bank in LC RAM
        LDA $C08B       ; Read RAM, write RAM, bank 1
        LDA $C08B       ; (double read for write enable)
        
        ; Copy ProDOS to $D000-$FFFF
        ; (approximately 16KB)
        JSR COPY_TO_LC
        
        ; Set up MLI entry point
        ; MLI jumps to LC code
```

### 6.2 Global Page Initialization ($BF00)

```assembly
        ; Initialize global page
        LDA #$4C        ; JMP opcode
        STA $BF00       ; JSPARE
        STA $BF03       ; DATETIME
        STA $BF06       ; SYSERR
        STA $BF09       ; SYSDEATH
        
        ; Set machine ID
        JSR DETECT_MACHINE
        STA $BF96       ; MACHID
        
        ; Clear date/time (until clock driver found)
        LDA #$00
        STA $BF90
        STA $BF91
        STA $BF92
        STA $BF93
```

### 6.3 Device Driver Scanning

```assembly
        ; Scan all slots for devices
        LDX #$70        ; Start with slot 7
DEVICE_SCAN:
        ; Check for device in slot
        JSR CHECK_SLOT
        BCS NO_DEVICE
        
        ; Register device
        JSR REGISTER_DEVICE
        
NO_DEVICE:
        TXA
        SEC
        SBC #$10        ; Next lower slot
        TAX
        BPL DEVICE_SCAN
        
        ; Check for /RAM
        JSR CHECK_RAM_DISK
```

### 6.4 Clock Driver Search

```assembly
        ; Search for clock/calendar card
        ; Check common locations
        
        ; ThunderClock (slot-independent)
        JSR CHECK_THUNDERCLOCK
        BCC CLOCK_FOUND
        
        ; Apple IIc built-in clock
        LDA $BF96       ; MACHID
        AND #$C0
        CMP #$C0        ; IIc?
        BNE NO_IIC_CLOCK
        JSR INIT_IIC_CLOCK
        BCC CLOCK_FOUND
        
NO_IIC_CLOCK:
        ; No clock found - date/time will be 0
        JMP CONTINUE_BOOT
        
CLOCK_FOUND:
        ; Install clock driver
        LDA #<CLOCK_DRIVER
        STA $BF03+1
        LDA #>CLOCK_DRIVER
        STA $BF03+2
```

### 6.5 System File Loading

```assembly
        ; Set prefix to boot volume
        JSR SET_BOOT_PREFIX
        
        ; Look for startup file
        ; Check in order:
        ; 1. STARTUP (special system file)
        ; 2. XXX.SYSTEM (first .SYSTEM file found)
        
        JSR FIND_STARTUP
        BCC LOAD_STARTUP
        
        JSR FIND_SYSTEM_FILE
        BCC LOAD_SYSTEM
        
        ; No system file found
        JMP BASIC_COLD
        
LOAD_STARTUP:
LOAD_SYSTEM:
        ; Load file at $2000
        ; (or address in AUX_TYPE for SYS files)
        JSR LOAD_FILE
        JMP $2000       ; Execute
```

---

## Phase 7: BASIC.SYSTEM Initialization

If BASIC.SYSTEM is loaded as the system file:

### 7.1 BASIC.SYSTEM Setup

```assembly
$2000:  ; BASIC.SYSTEM entry
        
        ; Display banner
        JSR PRINT_BANNER
        ; "BASIC.SYSTEM"
        
        ; Hook Applesoft for DOS commands
        ; Modify CHRGET routine
        LDA #$4C        ; JMP
        STA $00B1       ; In CHRGET
        LDA #<CMD_HANDLER
        STA $00B2
        LDA #>CMD_HANDLER
        STA $00B3
        
        ; Set up ampersand vector
        LDA #<AMP_HANDLER
        STA $03F5
        LDA #>AMP_HANDLER
        STA $03F6
        
        ; Initialize BASIC
        JSR BASIC_INIT
        
        ; Set HIMEM
        LDA #$00
        STA $73         ; HIMEM low = $00
        LDA #$9A        ; HIMEM high = $9A
        STA $74         ; HIMEM = $9A00
        
        ; Jump to BASIC prompt
        JMP $E003       ; Warm start
```

### 7.2 Final State

**Memory Layout:**
```
$0000-$00FF   Zero page
$0100-$01FF   Stack
$0200-$02FF   Input buffer
$0300-$03FF   Vectors (BASIC.SYSTEM patches some)
$0800-$09FF   BASIC.SYSTEM low code
$0800-$9A00   Available for BASIC program
$9A00-$9FFF   BASIC.SYSTEM high code
$BF00-$BFFF   ProDOS global page
$D000-$FFFF   ProDOS in language card
```

**I/O State:**
- KSW ($38-$39): Points to BASIC.SYSTEM input handler
- CSW ($36-$37): Points to BASIC.SYSTEM output handler
- Prefix: Set to boot volume (e.g., "/MYDISK")

---

## Apple IIc-Specific Differences

### Built-in Device Addresses

The Apple IIc has no physical slots but maps built-in hardware to slot addresses:

| Pseudo-Slot | Device |
|-------------|--------|
| Slot 1 | Serial Port 1 (Modem) |
| Slot 2 | Serial Port 2 (Printer) |
| Slot 3 | 80-Column Firmware |
| Slot 4 | Mouse |
| Slot 5 | SmartPort (External 3.5") |
| Slot 6 | Internal Disk II |
| Slot 7 | (Not used) |

### IIc Boot Priority

1. External 3.5" drive (slot 5) - if present and has disk
2. Internal 5.25" drive (slot 6)
3. BASIC prompt

**Note:** This is the opposite of IIe default priority!

### IIc Boot Control

| Keys at Startup | Effect |
|-----------------|--------|
| None | Boot from external 3.5" first |
| Escape | Boot from internal 5.25" |
| Open-Apple | Enter BASIC |
| Open-Apple + Closed-Apple | Enter Monitor |

### IIc Clock/Calendar

The IIc has a built-in clock chip. ProDOS initializes it automatically:

```assembly
IIC_CLOCK_INIT:
        ; Access IIc clock registers
        LDA $C033       ; Read clock register
        AND #$0F
        STA $BF90       ; Date low
        ; ... continue reading clock ...
```

---

## Timing Summary

| Boot Stage | Disk II | SmartPort | SCSI |
|------------|---------|-----------|------|
| Reset to ROM | 10ms | 10ms | 10ms |
| ROM self-test | 100ms | 100ms | 100ms |
| Slot scan | 200ms | 200ms | 200ms |
| Motor spinup | 1000ms | 500ms | 100ms |
| Boot sector | 500ms | 200ms | 50ms |
| ProDOS load | 2000ms | 1000ms | 500ms |
| BASIC.SYSTEM | 500ms | 300ms | 200ms |
| **Total** | **~4-5s** | **~2-3s** | **~1-2s** |

---

## Error Conditions

### Boot Failures

| Error | Cause | Result |
|-------|-------|--------|
| No bootable device | No cards, or all have $CnFF=$00 | Enter BASIC |
| Disk not ready | No disk, door open | Display "?" and retry |
| Read error | Bad disk, hardware fault | Display "ERR" or beep |
| Invalid boot block | Corrupted disk | Undefined (may hang) |
| No system file | ProDOS disk without .SYSTEM | "UNABLE TO LOAD PRODOS" |

### Recovery Options

| Action | Method |
|--------|--------|
| Enter Monitor | Open-Apple + Closed-Apple + Reset |
| Enter BASIC | Open-Apple + Reset |
| Retry boot | Control + Reset |
| Boot different slot | PR#n from Monitor or BASIC |

---

## Emulation Considerations

### Critical Timing Points

1. **Motor spinup delay**: Must wait ~1 second for Disk II
2. **Head settle time**: Must wait after track seek
3. **Slot scan order**: Must be accurate (7?1 for IIe)

### State That Must Be Preserved

For accurate reset behavior:

| Location | Purpose | Initial Value |
|----------|---------|---------------|
| $03F4 | Power-up byte | $00 (cold) or $A5 (warm) |
| $03F2-$03F3 | SOFTEV | Soft reset vector + checksum |
| $BF96 | MACHID | Machine type |
| $0800-$08FF | Boot sector | Loaded by ROM |

### Trap Points for ROM-Free Boot

| Address | Routine | Emulator Action |
|---------|---------|-----------------|
| $FA62 | RESET | Native init, scan for boot device |
| $C600 | DISKII_BOOT | Native: Read boot sector from disk image |
| $Cn00 | SMARTPORT_ENTRY | Native: SmartPort dispatch |
| $2000 | PRODOS_ENTRY | Native: Load ProDOS from volume |

---

## Document History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-12-30 | Initial specification |
