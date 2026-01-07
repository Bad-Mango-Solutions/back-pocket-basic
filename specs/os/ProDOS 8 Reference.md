# ProDOS 8 Reference

## Overview

ProDOS 8 (Professional Disk Operating System) is Apple's advanced disk operating system for 8-bit Apple II computers. It provides a hierarchical file system, clock/calendar support, and a clean machine language interface (MLI).

**Memory Requirements:** ~16KB in language card
**RAM Disk Support:** /RAM on IIe with 128KB

---

## Memory Map

ProDOS uses memory differently than DOS 3.3:

```
$0000-$00FF   Zero page (ProDOS uses $40-$4F)
$0100-$01FF   Stack
$0200-$02FF   Input buffer (shared with Monitor)
$0300-$03FF   System vectors and global page
$0800-$BEFF   User program space (varies)
$BF00-$BFFF   ProDOS global page
$D000-$DFFF   ProDOS in Language Card (bank 1)
$D000-$DFFF   ProDOS in Language Card (bank 2)
$E000-$FFFF   ProDOS in Language Card
```

### BASIC.SYSTEM Loaded

When BASIC.SYSTEM is loaded:

```
$0800-$09FF   BASIC.SYSTEM command handler
$0A00-$0BFF   BASIC.SYSTEM continued
$9A00-$9FFF   BASIC.SYSTEM continued
$2000-$9A00   User program space
```

**HIMEM with ProDOS and BASIC.SYSTEM:** $9A00

---

## ProDOS Global Page ($BF00-$BFFF)

The global page contains system status and vectors.

### System Vectors

| Address | Name        | Description |
|---------|-------------|-------------|
| $BF00   | JSPARE      | Spare JMP (used by quit) |
| $BF03   | DATETIME    | JMP to date/time update |
| $BF06   | SYSERR      | JMP to error handler |
| $BF09   | SYSDEATH    | JMP to fatal error handler |
| $BF0C   | SELCXROM    | JMP to select CX ROM |
| $BF0F   | Unused      | |

### Machine Identification

| Address | Name        | Description |
|---------|-------------|-------------|
| $BF96   | MACHID      | Machine identification byte |
| $BF97   | SESSION     | Session byte |
| $BF98   | PFIXPTR     | Prefix buffer pointer |
| $BF9A   | MEMTAESSION | Memory size (pages) |

#### MACHID Byte ($BF96)

| Bit | Meaning (if set) |
|-----|------------------|
| 7-6 | Machine type: 00=II, 01=II+, 10=IIe, 11=IIc |
| 5   | Reserved |
| 4   | 80-column card present |
| 3   | Memory > 64K |
| 2   | Clock/calendar present |
| 1   | Reserved |
| 0   | Reserved |

### Date/Time

| Address | Name      | Description |
|---------|-----------|-------------|
| $BF90   | DATE      | Date (2 bytes, ProDOS format) |
| $BF92   | TIME      | Time (2 bytes, ProDOS format) |

#### Date Format ($BF90-$BF91)

```
Bits 15-9: Year (0-127, relative to 1900)
Bits 8-5:  Month (1-12)
Bits 4-0:  Day (1-31)
```

#### Time Format ($BF92-$BF93)

```
Bits 15-13: Unused
Bits 12-8:  Hour (0-23)
Bits 7-6:   Unused
Bits 5-0:   Minute (0-59)
```

---

## Machine Language Interface (MLI)

The MLI is ProDOS's programmatic interface. All operations use a consistent calling convention.

### Calling Convention

```assembly
        JSR $BF00       ; MLI entry point
        DB  CMD         ; Command number
        DW  PARMLIST    ; Pointer to parameter list
        BCS ERROR       ; Carry set on error
        ; Success - A = 0
ERROR:  ; A = error code
```

### MLI Commands

#### Housekeeping Calls

| Code | Name           | Description |
|------|----------------|-------------|
| $40  | ALLOC_INTERRUPT| Install interrupt handler |
| $41  | DEALLOC_INTERRUPT| Remove interrupt handler |
| $65  | QUIT           | Exit to ProDOS |
| $80  | READ_BLOCK     | Read disk block |
| $81  | WRITE_BLOCK    | Write disk block |
| $82  | GET_TIME       | Update time globals |

#### Filing Calls

| Code | Name           | Description |
|------|----------------|-------------|
| $C0  | CREATE         | Create file or directory |
| $C1  | DESTROY        | Delete file or directory |
| $C2  | RENAME         | Rename file or directory |
| $C3  | SET_FILE_INFO  | Set file attributes |
| $C4  | GET_FILE_INFO  | Get file attributes |
| $C5  | ON_LINE        | Get volume names |
| $C6  | SET_PREFIX     | Set current directory |
| $C7  | GET_PREFIX     | Get current directory |
| $C8  | OPEN           | Open file |
| $C9  | NEWLINE        | Set newline mode |
| $CA  | READ           | Read from file |
| $CB  | WRITE          | Write to file |
| $CC  | CLOSE          | Close file |
| $CD  | FLUSH          | Flush file buffer |
| $CE  | SET_MARK       | Set file position |
| $CF  | GET_MARK       | Get file position |
| $D0  | SET_EOF        | Set file size |
| $D1  | GET_EOF        | Get file size |
| $D2  | SET_BUF        | Set file buffer address |
| $D3  | GET_BUF        | Get file buffer address |

---

## MLI Call Details

### CREATE ($C0)

**Purpose:** Create a new file or directory.

**Parameter List (7 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $07 |
| $01    | 2    | PATHNAME     | Pointer to pathname |
| $03    | 1    | ACCESS       | Access permissions |
| $04    | 1    | FILE_TYPE    | File type |
| $05    | 2    | AUX_TYPE     | Auxiliary type |
| $07    | 1    | STORAGE_TYPE | Storage type ($0D=dir, $01=file) |

**Example:**
```assembly
CREATE_PARMS:
        DB $07          ; Parameter count
        DW PATHNAME     ; Pointer to pathname
        DB $C3          ; Access: rename/delete/read/write
        DB $06          ; File type: BIN
        DW $2000        ; Aux type: load address
        DB $01          ; Storage: seedling file
PATHNAME:
        DB $0A          ; Length
        ASC "MYFILE.BIN"

        JSR $BF00
        DB  $C0
        DW  CREATE_PARMS
```

### DESTROY ($C1)

**Purpose:** Delete a file or directory (directory must be empty).

**Parameter List (3 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $01 |
| $01    | 2    | PATHNAME     | Pointer to pathname |

### RENAME ($C2)

**Purpose:** Rename a file or directory.

**Parameter List (5 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $02 |
| $01    | 2    | PATHNAME     | Pointer to old pathname |
| $03    | 2    | NEW_PATHNAME | Pointer to new pathname |

### GET_FILE_INFO ($C4)

**Purpose:** Get file information.

**Parameter List (18 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $0A |
| $01    | 2    | PATHNAME     | Pointer to pathname |
| $03    | 1    | ACCESS       | (returned) |
| $04    | 1    | FILE_TYPE    | (returned) |
| $05    | 2    | AUX_TYPE     | (returned) |
| $07    | 1    | STORAGE_TYPE | (returned) |
| $08    | 2    | BLOCKS_USED  | (returned) |
| $0A    | 2    | MOD_DATE     | (returned) |
| $0C    | 2    | MOD_TIME     | (returned) |
| $0E    | 2    | CREATE_DATE  | (returned) |
| $10    | 2    | CREATE_TIME  | (returned) |

### OPEN ($C8)

**Purpose:** Open a file for I/O.

**Parameter List (4 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $03 |
| $01    | 2    | PATHNAME     | Pointer to pathname |
| $03    | 2    | IO_BUFFER    | 1024-byte buffer address |
| $05    | 1    | REF_NUM      | (returned) Reference number |

**Example:**
```assembly
OPEN_PARMS:
        DB $03          ; Parameter count
        DW PATHNAME     ; Pathname pointer
        DW $1C00        ; I/O buffer (1024 bytes)
        DB $00          ; Ref num (returned)

        JSR $BF00
        DB  $C8
        DW  OPEN_PARMS
        BCS ERROR
        LDA OPEN_PARMS+5 ; Get ref num
```

### READ ($CA)

**Purpose:** Read data from an open file.

**Parameter List (5 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $04 |
| $01    | 1    | REF_NUM      | File reference number |
| $02    | 2    | DATA_BUFFER  | Destination address |
| $04    | 2    | REQUEST_COUNT| Bytes to read |
| $06    | 2    | TRANS_COUNT  | (returned) Bytes actually read |

### WRITE ($CB)

**Purpose:** Write data to an open file.

**Parameter List (5 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $04 |
| $01    | 1    | REF_NUM      | File reference number |
| $02    | 2    | DATA_BUFFER  | Source address |
| $04    | 2    | REQUEST_COUNT| Bytes to write |
| $06    | 2    | TRANS_COUNT  | (returned) Bytes actually written |

### CLOSE ($CC)

**Purpose:** Close an open file.

**Parameter List (2 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $01 |
| $01    | 1    | REF_NUM      | File reference number (0 = all) |

### SET_MARK ($CE)

**Purpose:** Set file position (seek).

**Parameter List (4 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $02 |
| $01    | 1    | REF_NUM      | File reference number |
| $02    | 3    | POSITION     | New position (24-bit) |

### GET_MARK ($CF)

**Purpose:** Get current file position.

**Parameter List (4 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $02 |
| $01    | 1    | REF_NUM      | File reference number |
| $02    | 3    | POSITION     | (returned) Current position |

### GET_EOF ($D1)

**Purpose:** Get file size.

**Parameter List (4 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $02 |
| $01    | 1    | REF_NUM      | File reference number |
| $02    | 3    | EOF          | (returned) File size |

### SET_PREFIX ($C6)

**Purpose:** Set current directory prefix.

**Parameter List (2 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $01 |
| $01    | 2    | PATHNAME     | Pointer to pathname |

**Note:** Pathname must begin with "/" for absolute, or no "/" for relative.

### GET_PREFIX ($C7)

**Purpose:** Get current directory prefix.

**Parameter List (2 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $01 |
| $01    | 2    | DATA_BUFFER  | Pointer to 64-byte buffer |

### ON_LINE ($C5)

**Purpose:** Get list of online volumes.

**Parameter List (4 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $02 |
| $01    | 1    | UNIT_NUM     | Unit (0 = all) |
| $02    | 2    | DATA_BUFFER  | 256-byte buffer |

### READ_BLOCK ($80)

**Purpose:** Read a 512-byte block directly from device.

**Parameter List (6 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $03 |
| $01    | 1    | UNIT_NUM     | Device unit number |
| $02    | 2    | DATA_BUFFER  | 512-byte buffer address |
| $04    | 2    | BLOCK_NUM    | Block number |

### WRITE_BLOCK ($81)

**Purpose:** Write a 512-byte block directly to device.

**Parameter List (6 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $03 |
| $01    | 1    | UNIT_NUM     | Device unit number |
| $02    | 2    | DATA_BUFFER  | 512-byte buffer address |
| $04    | 2    | BLOCK_NUM    | Block number |

### QUIT ($65)

**Purpose:** Exit program and return to ProDOS selector.

**Parameter List (5 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $04 |
| $01    | 1    | QUIT_TYPE    | $00 = normal |
| $02    | 2    | Unused       | |
| $04    | 1    | Unused       | |
| $05    | 2    | Unused       | |

---

## Interrupt Handlers

### ALLOC_INTERRUPT ($40)

**Purpose:** Install an interrupt handler.

**Parameter List (3 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $02 |
| $01    | 1    | INT_NUM      | (returned) Handler number |
| $02    | 2    | INT_CODE     | Pointer to handler code |

**Handler Requirements:**
- Must preserve all registers
- Must CLC and return if interrupt handled
- Must SEC and return if not your interrupt
- Handler must be in non-bank-switched memory

### DEALLOC_INTERRUPT ($41)

**Purpose:** Remove an interrupt handler.

**Parameter List (2 bytes):**

| Offset | Size | Name         | Description |
|--------|------|--------------|-------------|
| $00    | 1    | PARAM_COUNT  | $01 |
| $01    | 1    | INT_NUM      | Handler number to remove |

---

## File Types

| Code | Name | Description |
|------|------|-------------|
| $00  | UNK  | Unknown (typeless) |
| $01  | BAD  | Bad blocks file |
| $04  | TXT  | Text file |
| $06  | BIN  | Binary file |
| $0F  | DIR  | Directory |
| $19  | ADB  | AppleWorks database |
| $1A  | AWP  | AppleWorks word processor |
| $1B  | ASP  | AppleWorks spreadsheet |
| $EF  | PAS  | Pascal area |
| $F0  | CMD  | ProDOS added command |
| $FA  | INT  | Integer BASIC program |
| $FB  | IVR  | Integer BASIC variables |
| $FC  | BAS  | Applesoft BASIC program |
| $FD  | VAR  | Applesoft variables |
| $FE  | REL  | Relocatable code |
| $FF  | SYS  | System file (executable) |

### Auxiliary Type

The auxiliary type provides additional information:

- For BIN files: Load address
- For TXT files: Record length (0 = sequential)
- For SYS files: Load address (usually $2000)

---

## Error Codes

| Code | Name                    | Description |
|------|-------------------------|-------------|
| $00  | (none)                  | No error |
| $01  | BAD_SYSTEM_CALL         | Invalid MLI call number |
| $04  | BAD_PARAMETER           | Invalid parameter count |
| $25  | INTERRUPT_TBL_FULL      | Interrupt table full |
| $27  | I/O_ERROR               | Device I/O error |
| $28  | NO_DEVICE_CONNECTED     | Device not found |
| $2B  | WRITE_PROTECTED         | Disk is write-protected |
| $2E  | SWITCHED_DISK           | Disk switched during operation |
| $40  | INVALID_PATHNAME        | Bad pathname syntax |
| $42  | MAXFILES_EXCEEDED       | Too many files open |
| $43  | INVALID_REFERENCE_NUM   | Bad file reference number |
| $44  | DIRECTORY_NOT_FOUND     | Directory not found |
| $45  | VOLUME_NOT_FOUND        | Volume not found |
| $46  | FILE_NOT_FOUND          | File not found |
| $47  | DUPLICATE_FILENAME      | File already exists |
| $48  | VOLUME_FULL             | Disk full |
| $49  | DIRECTORY_FULL          | Directory full |
| $4A  | INCOMPATIBLE_FORMAT     | Not a ProDOS disk |
| $4B  | UNSUPPORTED_STORAGE     | Unknown storage type |
| $4C  | END_OF_FILE             | Read past end of file |
| $4D  | OUT_OF_RANGE            | Position out of range |
| $4E  | INVALID_ACCESS          | Access denied |
| $50  | FILE_IS_OPEN            | File already open |
| $51  | DIRECTORY_DAMAGED       | Directory structure corrupted |
| $52  | NOT_PRODOS_VOLUME       | Not a ProDOS volume |
| $53  | INVALID_PARAMETER       | Bad parameter value |
| $55  | VOLUME_DIRECTORY_FULL   | Volume directory full |
| $56  | BAD_BUFFER_ADDRESS      | Buffer address invalid |
| $57  | DUPLICATE_VOLUME        | Volume already online |
| $5A  | FILE_STRUCTURE_DAMAGED  | File structure corrupted |

---

## Pathname Format

ProDOS pathnames follow these rules:

- Maximum 64 characters total
- Volume name: 1-15 characters
- Each segment: 1-15 characters
- Valid characters: A-Z, 0-9, period
- Must start with letter
- Case-insensitive
- Stored as length-prefixed Pascal string

### Examples

| Pathname | Description |
|----------|-------------|
| /MYDISK  | Volume root |
| /MYDISK/GAMES | Directory |
| /MYDISK/GAMES/LODERUN | File |
| MYFILE   | Relative to current prefix |

---

## Disk Format

### Physical Format

- 512 bytes per block
- 280 blocks per 5.25" disk (140KB)
- 1600 blocks per 3.5" disk (800KB)

### Key Blocks

| Block | Contents |
|-------|----------|
| 0-1   | Boot code |
| 2     | Volume directory key block |
| 3-5   | Volume directory (continuation) |
| 6     | Volume bitmap start |

### Volume Directory Format (Block 2)

| Offset | Size | Description |
|--------|------|-------------|
| $00    | 2    | Prev block pointer (0) |
| $02    | 2    | Next block pointer |
| $04    | 1    | Entry type/name length |
| $05    | 15   | Volume name |
| $14    | 8    | Reserved |
| $1C    | 2    | Creation date |
| $1E    | 2    | Creation time |
| $20    | 1    | Version (0) |
| $21    | 1    | Min version (0) |
| $22    | 1    | Access |
| $23    | 1    | Entry length ($27) |
| $24    | 1    | Entries per block ($0D) |
| $25    | 2    | File count |
| $27    | 2    | Bitmap pointer |
| $29    | 2    | Total blocks |

### File Entry Format (39 bytes)

| Offset | Size | Description |
|--------|------|-------------|
| $00    | 1    | Storage type/name length |
| $01    | 15   | Filename |
| $10    | 1    | File type |
| $11    | 2    | Key pointer (first block) |
| $13    | 2    | Blocks used |
| $15    | 3    | EOF (file size) |
| $18    | 2    | Creation date |
| $1A    | 2    | Creation time |
| $1C    | 1    | Version |
| $1D    | 1    | Min version |
| $1E    | 1    | Access |
| $1F    | 2    | Auxiliary type |
| $21    | 2    | Last modified date |
| $23    | 2    | Last modified time |
| $25    | 2    | Header pointer |

### Storage Types (high nibble of byte $00)

| Type | Description |
|------|-------------|
| $0   | Deleted entry |
| $1   | Seedling (1-block file) |
| $2   | Sapling (2-256 blocks) |
| $3   | Tree (large file) |
| $D   | Subdirectory header |
| $E   | Subdirectory entry |
| $F   | Volume directory header |

---

## BASIC.SYSTEM Commands

BASIC.SYSTEM is loaded by ProDOS to provide BASIC commands similar to DOS 3.3.

### File Commands

| Command | Description |
|---------|-------------|
| CATALOG [pathname] | List directory |
| CAT [pathname]     | Abbreviated catalog |
| PREFIX [pathname]  | Set/show current directory |
| LOAD pathname      | Load BASIC program |
| SAVE pathname      | Save BASIC program |
| RUN pathname       | Load and run |
| CHAIN pathname     | Run preserving variables |
| DELETE pathname    | Delete file |
| LOCK pathname      | Lock file |
| UNLOCK pathname    | Unlock file |
| RENAME old,new     | Rename file |
| VERIFY pathname    | Verify file |
| CREATE pathname    | Create directory |

### Binary Commands

| Command | Description |
|---------|-------------|
| BLOAD pathname[,Aaddr][,Ttype]  | Load binary |
| BSAVE pathname,Aaddr,Llen[,Ttype] | Save binary |
| BRUN pathname[,Aaddr][,Ttype]  | Load and run binary |

### Text File Commands

| Command | Description |
|---------|-------------|
| OPEN pathname       | Open text file |
| CLOSE [pathname]    | Close file(s) |
| READ pathname       | Set file for reading |
| WRITE pathname      | Set file for writing |
| APPEND pathname     | Open for appending |
| EXEC pathname       | Execute text file as commands |

### System Commands

| Command | Description |
|---------|-------------|
| -pathname          | Run system file |
| FRE(0)             | Show/collect free memory |
| STORE pathname     | Save hi-res picture |
| RESTORE pathname   | Load hi-res picture |
| MONO               | Monochrome mode |
| COLOR              | Color mode |
| FLASH              | Flash text |
| INVERSE            | Inverse text |
| NORMAL             | Normal text |

---

## IIe vs IIc Differences

| Feature | Apple IIe | Apple IIc |
|---------|-----------|-----------|
| /RAM disk | Requires 128KB | Built-in 128KB |
| Boot slot | Usually slot 6 | Built-in |
| Clock | Requires card | Built-in |
| MACHID | $83 or $D3 | $C3 |
| Default prefix | /slot6disk | /slot5disk |

### Clock Support

The IIc has a built-in clock. ProDOS automatically uses it if present.

For IIe, a clock card (like the ThunderClock) must be installed. ProDOS searches for clock drivers at boot.

### RAM Disk (/RAM)

Both 128KB IIe and IIc support a /RAM volume using auxiliary memory:

- Size: 60KB (approximately)
- Slot 3, Drive 2 (/RAM)
- Contents lost on reboot
- ProDOS reserves space for /RAM bitmap

---

## Example: Reading a File

```assembly
; Complete file reading example
        
        ; Set prefix
        JSR MLI
        DB  $C6          ; SET_PREFIX
        DW  PREFIX_PARMS
        BCS ERROR
        
        ; Open file
        JSR MLI
        DB  $C8          ; OPEN
        DW  OPEN_PARMS
        BCS ERROR
        
        ; Read file
        LDA REF_NUM
        STA READ_PARMS+1
        JSR MLI
        DB  $CA          ; READ
        DW  READ_PARMS
        BCS ERROR
        
        ; Close file
        LDA REF_NUM
        STA CLOSE_PARMS+1
        JSR MLI
        DB  $CC          ; CLOSE
        DW  CLOSE_PARMS
        
        RTS

MLI     EQU $BF00

PREFIX_PARMS:
        DB $01
        DW PREFIX
PREFIX: DB 8
        ASC "/MYDISK"

OPEN_PARMS:
        DB $03
        DW FILENAME
        DW $1C00         ; I/O buffer
REF_NUM:DB $00

READ_PARMS:
        DB $04
        DB $00           ; Ref num (filled in)
        DW $2000         ; Data buffer
        DW $2000         ; Request count
        DW $0000         ; Trans count (returned)

CLOSE_PARMS:
        DB $01
        DB $00           ; Ref num

FILENAME:
        DB 8
        ASC "TESTFILE"

ERROR:  ; Handle error in A
        RTS
```

---

## Document History

| Version | Date       | Changes |
|---------|------------|---------|
| 1.0     | 2025-12-30 | Initial specification |
