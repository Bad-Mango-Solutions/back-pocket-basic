# DOS 3.3 Reference

## Overview

DOS 3.3 (Disk Operating System) is Apple's disk operating system, providing file management and disk I/O for the Apple II. DOS 3.3 loads into memory at $9600-$BFFF and hooks into BASIC to add disk commands.

**Memory Requirements:** ~10KB ($9600-$BFFF)
**HIMEM with DOS:** $9600 (from $BFFF)

---

## Memory Map

```
$9600-$9CFF   DOS buffers and work area
$9D00-$A8FF   File Manager (RWTS wrapper, catalog, file ops)
$A900-$AAFF   File Manager extended
$AB00-$ACFF   Additional buffers
$AD00-$B3FF   RWTS (Read/Write Track/Sector)
$B400-$B5FF   Disk arm movement
$B600-$B7FF   Boot code and low-level disk access
$B800-$BFFF   DOS command interpreter and main routines
```

---

## Zero Page Locations

DOS 3.3 uses very few zero page locations to preserve compatibility:

| Address | Name     | Description |
|---------|----------|-------------|
| $00-$01 | Reserved | Used by BASIC USR |
| $40-$47 | DOS work | Temporary workspace (shared) |
| $48     | DOSESSION| DOS save byte |

---

## Page 3 Vectors

| Address | Name    | Description |
|---------|---------|-------------|
| $03D0   | DOS WS  | JMP to DOS warm start ($9DBF) |
| $03D3   | DOS CS  | JMP to DOS cold start ($9D84) |
| $03D6   | FM ENTRY| JMP to File Manager entry ($A764) |
| $03D9   | RWTS    | JMP to RWTS entry ($B7B5) |
| $03DC   | LOCRPL  | JMP to locate command (unused) |
| $03DF   | INTRRPL | JMP to interrupt handler |
| $03E2   | RESET   | DOS reset handler |
| $03E7   | & ENTRY | Ampersand command entry |

---

## File Manager Entry ($A764)

The File Manager is the high-level disk interface. Call via JMP ($03D6).

### File Manager Parameter List (IOB)

The File Manager is called with X,A pointing to a 24-byte parameter block:

| Offset | Name     | Description |
|--------|----------|-------------|
| $00    | CMD      | Command code (see below) |
| $01    | SUBCMD   | Sub-command |
| $02-$03| BUFPTR   | Buffer pointer |
| $04    | TRACK    | Track number |
| $05    | SECTOR   | Sector number |
| $06    | VOLNUM   | Volume number (0 = any) |
| $07    | DESSION  | Drive number (1 or 2) |
| $08    | SLOT     | Slot number × 16 |
| $09    | FTYPE    | File type |
| $0A-$0B| FIESSION | File length |
| $0C-$0D| LOADESSION| Load address |
| $0E    | Unused   | |
| $0F-$1E| FNAME    | Filename (30 bytes, space-padded) |

### File Manager Commands

| Code | Command | Description |
|------|---------|-------------|
| $01  | OPEN    | Open file |
| $02  | CLOSE   | Close file |
| $03  | READ    | Read from file |
| $04  | WRITE   | Write to file |
| $05  | DELETE  | Delete file |
| $06  | CATALOG | Display catalog |
| $07  | LOCK    | Lock file |
| $08  | UNLOCK  | Unlock file |
| $09  | RENAME  | Rename file |
| $0A  | POSITION| Position in file |
| $0B  | INIT    | Initialize disk |
| $0C  | VERIFY  | Verify file |

### File Types

| Code | Type | Description |
|------|------|-------------|
| $00  | T    | Text file |
| $01  | I    | Integer BASIC program |
| $02  | A    | Applesoft BASIC program |
| $04  | B    | Binary file |
| $08  | S    | Type S (special) |
| $10  | R    | Relocatable |
| $20  | A    | Type A (new A) |
| $40  | B    | Type B (new B) |

---

## RWTS Entry ($B7B5)

RWTS (Read/Write Track/Sector) is the low-level disk driver.

### Calling RWTS

```assembly
        LDA #<IOB       ; Low byte of IOB address
        LDY #>IOB       ; High byte of IOB address
        JSR $B7B5       ; Call RWTS
        BCS ERROR       ; Carry set = error
```

### RWTS Input/Output Block (IOB)

| Offset | Name     | Description |
|--------|----------|-------------|
| $00    | TBESSION | Table type ($01 = IOB) |
| $01    | SLOT     | Slot number × 16 |
| $02    | DRIVE    | Drive number (1 or 2) |
| $03    | VOL      | Expected volume (0 = any) |
| $04    | TRACK    | Track number (0-34) |
| $05    | SECTOR   | Sector number (0-15) |
| $06-$07| DCT      | Device characteristics table pointer |
| $08-$09| BUFF     | Data buffer pointer (256 bytes) |
| $0A    | Unused   | |
| $0B    | RWEESSION| Command byte |
| $0C    | RTVOL    | Returned volume number |
| $0D    | RTSLOT   | Returned slot |
| $0E    | RTDRIVE  | Returned drive |
| $0F    | RTERR    | Error code (if carry set) |

### RWTS Commands ($0B)

| Value | Command | Description |
|-------|---------|-------------|
| $00   | SEEK    | Move to track |
| $01   | READ    | Read sector |
| $02   | WRITE   | Write sector |
| $04   | FORMAT  | Format track |

### RWTS Error Codes

| Code | Description |
|------|-------------|
| $00  | No error |
| $10  | Write protected |
| $20  | Volume mismatch |
| $40  | Drive error |
| $80  | Read error |

### RWTS Usage Example

```assembly
; Read sector example
IOB:    DB $01          ; IOB type
        DB $60          ; Slot 6
        DB $01          ; Drive 1  
        DB $00          ; Any volume
        DB $11          ; Track 17
        DB $00          ; Sector 0
        DW DCTBL        ; DCT pointer
        DW BUFFER       ; 256-byte buffer
        DB $00          ; Unused
        DB $01          ; READ command
        DS 4            ; Return values

        LDA #<IOB
        LDY #>IOB
        JSR $B7B5
        BCS ERROR
```

---

## DOS Commands

DOS 3.3 adds commands to BASIC by intercepting the input line:

### File Commands

| Command | Description |
|---------|-------------|
| CATALOG [,Dn][,Sn][,Vn] | Display disk catalog |
| LOAD filename[,Dn][,Sn][,Vn] | Load BASIC program |
| SAVE filename[,Dn][,Sn][,Vn] | Save BASIC program |
| RUN filename[,Dn][,Sn][,Vn] | Load and run program |
| CHAIN filename[,Dn][,Sn][,Vn] | Run preserving variables |
| DELETE filename | Delete file |
| LOCK filename | Lock file (prevent deletion) |
| UNLOCK filename | Unlock file |
| RENAME oldname,newname | Rename file |
| VERIFY filename | Verify file is readable |

### File I/O Commands

| Command | Description |
|---------|-------------|
| OPEN filename[,Ln][,Dn][,Sn] | Open file for I/O |
| CLOSE [filename] | Close file(s) |
| READ filename | Set file for reading |
| WRITE filename | Set file for writing |
| APPEND filename | Open for appending |
| POSITION filename,Rn | Position to record |

### Binary File Commands

| Command | Description |
|---------|-------------|
| BLOAD filename[,Aadr][,Dn][,Sn] | Load binary file |
| BSAVE filename,Aadr,Llen[,Dn][,Sn] | Save binary file |
| BRUN filename[,Aadr][,Dn][,Sn] | Load and execute binary |

### Disk Commands

| Command | Description |
|---------|-------------|
| INIT filename[,Vn] | Initialize disk |
| FP | Enter BASIC (clear DOS hooks) |
| INT | Enter Integer BASIC |
| MON[,C][,I][,O] | Enable I/O monitoring |
| NOMON[,C][,I][,O] | Disable I/O monitoring |
| MAXFILES n | Set max open files (1-16) |

### Parameter Suffixes

| Suffix | Description |
|--------|-------------|
| ,Dn    | Drive number (1 or 2) |
| ,Sn    | Slot number (1-7) |
| ,Vn    | Volume number (1-254) |
| ,Ln    | Record length (text files) |
| ,Rn    | Record number |
| ,Aadr  | Load/execute address |
| ,Llen  | Length in bytes |
| ,Bn    | Byte offset in record |

---

## Disk Format

### Physical Format

- 35 tracks (0-34)
- 16 sectors per track (0-15)
- 256 bytes per sector
- Total: 140KB per disk

### Logical Structure

| Track | Sector | Contents |
|-------|--------|----------|
| 0     | 0      | Boot sector 0 (loads more boot code) |
| 0     | 1-9    | DOS boot code |
| 17    | 0      | VTOC (Volume Table of Contents) |
| 17    | 1-15   | Catalog sectors (linked list) |
| Other | Other  | File data |

### Volume Table of Contents (Track 17, Sector 0)

| Offset | Description |
|--------|-------------|
| $00    | Unused |
| $01    | Catalog track |
| $02    | Catalog sector |
| $03    | DOS version ($03 = DOS 3.3) |
| $04-$05| Unused |
| $06    | Volume number |
| $27    | Max T/S pairs per T/S list |
| $30    | Last track allocated |
| $31    | Allocation direction (+1 or -1) |
| $34    | Tracks per disk (35) |
| $35    | Sectors per track (16) |
| $36-$37| Bytes per sector (256) |
| $38-$C3| Free sector bitmap (4 bytes per track) |

### Catalog Entry Format

| Offset | Description |
|--------|-------------|
| $00    | Track of first T/S list (0 = deleted) |
| $01    | Sector of first T/S list |
| $02    | File type + locked flag |
| $03-$20| Filename (30 bytes, high bit set) |
| $21-$22| File length in sectors |

### File Type Byte ($02)

| Bit | Meaning |
|-----|---------|
| 7   | Locked flag (1 = locked) |
| 6-0 | File type |

### Track/Sector List

Each T/S list sector contains pointers to data sectors:

| Offset | Description |
|--------|-------------|
| $00    | Unused |
| $01    | Track of next T/S list (0 = none) |
| $02    | Sector of next T/S list |
| $05-$06| Sector offset of first entry |
| $0C-$0D| Track/sector pair 0 |
| $0E-$0F| Track/sector pair 1 |
| ...    | (up to 122 pairs) |

---

## Important RAM Locations

| Address | Name | Description |
|---------|------|-------------|
| $9600   | DOS Start | Start of DOS |
| $AA72   | FILNAM | Current filename buffer |
| $AA75   | FESSION | File manager session info |
| $AAB6   | IESSION | IOB for file operations |
| $B3D2   | RWESSION | Current track |
| $B3D3   | RWESSION | Current sector |
| $B3F4   | PRESSION | Previous track position |
| $B7E8   | SLOT     | Current slot × 16 |
| $B7EA   | BUFPTR   | Buffer pointer |
| $B7EB   | RESSION  | Active drive |
| $B7EC   | TOESSION | Track number for RWTS |
| $B7ED   | SESSION  | Sector number for RWTS |
| $B7F0   | DESSION  | Drive (1 or 2) |
| $B7F1   | VOL      | Volume number |
| $B7F3   | CMDDESSION| RWTS command code |

---

## Common Routines

### DOS Entry ($9D84) - Cold Start

**Purpose:** Initialize DOS from cold start.

**Called:** After boot, or by explicit JMP.

### DOS Warm Start ($9DBF)

**Purpose:** Re-initialize DOS vectors.

**Called:** After BASIC reset.

### CATALOG ($A56E)

**Purpose:** Display disk catalog.

**Input:**
- Slot/Drive set from defaults or command

### OPEN ($A2A3)

**Purpose:** Open a file.

**Input:**
- Filename in buffer
- File type and parameters

### CLOSE ($A2EA)

**Purpose:** Close file(s).

### READ ($A318)

**Purpose:** Read from open file.

### WRITE ($A350)

**Purpose:** Write to open file.

---

## Error Messages

| Code | Message |
|------|---------|
| 1    | LANGUAGE NOT AVAILABLE |
| 2    | RANGE ERROR |
| 4    | WRITE PROTECTED |
| 5    | END OF DATA |
| 6    | FILE NOT FOUND |
| 7    | VOLUME MISMATCH |
| 8    | I/O ERROR |
| 9    | DISK FULL |
| 10   | FILE LOCKED |
| 11   | SYNTAX ERROR |
| 12   | NO BUFFERS AVAILABLE |
| 13   | FILE TYPE MISMATCH |
| 14   | PROGRAM TOO LARGE |
| 15   | NOT DIRECT COMMAND |

---

## BASIC Integration

DOS patches BASIC's input routine to intercept commands. When you type a DOS command, DOS:

1. Checks if line starts with a DOS keyword
2. If yes, parses and executes the DOS command
3. If no, passes line to BASIC

### I/O Hooks

DOS intercepts character I/O for file operations:

| Vector | Default | With File Open |
|--------|---------|----------------|
| CSW ($36) | $FDF0 | DOS output hook |
| KSW ($38) | $FD1B | DOS input hook |

When a file is open for output, PRINTed data goes to the file instead of screen.

---

## IIe vs IIc Differences

| Feature | Apple IIe | Apple IIc |
|---------|-----------|-----------|
| Boot slot | Slot 6 card | Built-in slot 6 |
| Second drive | Slot 6 drive 2 | External port |
| Memory | $9600-$BFFF | $9600-$BFFF |

No functional differences in DOS 3.3 between IIe and IIc.

---

## Compatibility Notes

### ProDOS Conversion

DOS 3.3 disks are not directly readable by ProDOS. Files must be converted using utilities like COPY II PLUS or Apple's conversion programs.

### File Limitations

- Maximum files per disk: ~105 (catalog space)
- Maximum file size: ~126KB (limited by T/S list)
- Filename length: 30 characters

### Memory Conflicts

Programs using memory above $9600 will conflict with DOS. Use HIMEM: $9600 or MAXFILES 1 to minimize DOS footprint.

---

## Document History

| Version | Date       | Changes |
|---------|------------|---------|
| 1.0     | 2025-12-30 | Initial specification |
