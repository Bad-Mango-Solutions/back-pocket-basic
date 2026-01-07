# PocketOS Gen 1 Technical Specification for 65C02-Based Apple II Emulator (Pocket2e)

---

## Introduction

PocketOS is a minimal, extensible operating system designed for the 65C02-based Apple II architecture, specifically targeting the Pocket2e emulator with language card and auxiliary RAM support. This specification outlines the minimum viable requirements for a bootable Gen 1 OS, focusing on compatibility with ProDOS-formatted disks, binary compatibility with real Apple II hardware and third-party emulators, and a modular architecture that supports future extensibility toward 65816/65832 CPUs and advanced privilege models. The document provides a comprehensive overview of the boot process, memory map, syscall layer, VFS architecture, hardware detection, COP instruction usage, and compatibility considerations, with implementation-ready details, diagrams, tables, and code snippets in C-style pseudocode and 6502 assembly.

---

## 1. Boot Process Overview

### 1.1 Bootloader Requirements

PocketOS must be bootable from a 5.25" floppy disk image, ideally using a ProDOS-formatted disk but also supporting its own virtual file system (VFS). The bootloader must:

- Load from standard Apple II boot vectors (track 0, sector 0 for DOS 3.3, block 0 for ProDOS).
- Detect disk format (ProDOS, DOS 3.3, or PocketFS) and select appropriate loader routines.
- Initialize system memory, including main RAM, auxiliary RAM, and language card memory.
- Detect ROM version and hardware mode (real vs enhanced).
- Load the PocketOS kernel and modules into designated memory regions.
- Pass control to the OS entry point.

#### Boot Sequence Diagram

```
+-------------------+
| Power-On/Reset    |
+-------------------+
         |
         v
+-------------------+
| Disk Controller   |
| ROM Boot Vector   |
+-------------------+
         |
         v
+-------------------+
| Read Boot Sector  |
| (T0/S0 or Block 0)|
+-------------------+
         |
         v
+-------------------+
| Detect Disk Format|
| (ProDOS/DOS/PFS)  |
+-------------------+
         |
         v
+-------------------+
| Load Bootloader   |
| to $0800          |
+-------------------+
         |
         v
+-------------------+
| Bootloader Loads  |
| Kernel & Modules  |
+-------------------+
         |
         v
+-------------------+
| OS Entry Point    |
+-------------------+
```

The bootloader must be compact (≤256 bytes for initial sector) and capable of loading additional sectors as needed for larger kernels or multi-stage boot processes.

### 1.2 Disk Image Formats and ProDOS Compatibility

PocketOS must support:

- ProDOS-formatted disk images (.po, .dsk) with 512-byte blocks.
- DOS 3.3-formatted images (.do, .dsk) with 256-byte sectors.
- PocketFS or other custom VFS images for advanced features.

Detection of disk format is performed by examining key filesystem structures (ProDOS Volume Directory Header, DOS VTOC) at known offsets.

---

## 2. Memory Map and Layout Strategy

### 2.1 Apple II Memory Architecture

The Apple II (and IIe) address space is 64KB, divided as follows:

| Address Range | Description                  |
|---------------|-----------------------------|
| $0000-$00FF   | Zero Page                   |
| $0100-$01FF   | Stack                       |
| $0200-$BFFF   | Main RAM                    |
| $C000-$CFFF   | I/O, Peripheral ROMs        |
| $D000-$FFFF   | Bank-switched RAM/ROM       |

Auxiliary RAM and language card memory provide additional banks, accessible via soft switches.

#### Memory Map Diagram

```
Main Memory ($0000-$BFFF)
+------------------------+
| Zero Page ($0000-$00FF)|
| Stack     ($0100-$01FF)|
| ...                    |
| User RAM ($0200-$BFFF) |
+------------------------+

I/O Space ($C000-$CFFF)
+------------------------+
| Peripheral ROMs        |
| Soft Switches          |
+------------------------+

Bank-Switched ($D000-$FFFF)
+------------------------+
| Main RAM / ROM         |
| Language Card / Aux RAM|
+------------------------+
```

### 2.2 Use of Auxiliary RAM and Language Card

Auxiliary RAM (typically 64KB) and the language card (16KB) are accessed via soft switches in the $C0xx range. PocketOS must:

- Detect presence of auxiliary RAM and language card.
- Use auxiliary RAM for extended kernel modules, file buffers, or RAM disk.
- Use language card for code overlays, system workspace, or extended drivers.

#### Soft Switch Table (Apple IIe)

| Address   | Function                          |
|-----------|-----------------------------------|
| $C000     | 80STOREOFF (video page1/page2)    |
| $C001     | 80STOREON (main/aux video memory) |
| $C002     | RAMRDOFF (main RAM read enable)   |
| $C003     | RAMDRON (aux RAM read enable)     |
| $C004     | RAMWRTOFF (main RAM write enable) |
| $C005     | RAMWRTON (aux RAM write enable)   |
| $C008     | ALZTPOFF (main zero page)         |
| $C009     | ALTZPON (aux zero page)           |
| ...       | ...                               |

Aux RAM and language card memory are mapped into the main address space as needed for code execution and data storage.

### 2.3 Memory Allocation Strategy

PocketOS must reserve key memory regions for system use:

- Zero Page ($00-$FF): OS vectors, indirect addressing, fast access.
- Stack ($100-$1FF): Subroutine return addresses, temporary storage.
- System Workspace: OS data structures, syscall tables, VFS buffers.
- User RAM: Application code, file buffers, dynamic allocations.
- Auxiliary RAM: Extended workspace, RAM disk, overlays.
- Language Card: Code overlays, workspace, drivers.

#### Example Memory Map Table

| Region         | Start   | End     | Usage                       |
|----------------|---------|---------|-----------------------------|
| Zero Page      | $0000   | $00FF   | OS vectors, fast access     |
| Stack          | $0100   | $01FF   | Subroutine stack            |
| System WS      | $0200   | $03FF   | OS workspace, syscall table |
| User RAM       | $0400   | $BFFF   | Apps, buffers, heap         |
| I/O Space      | $C000   | $CFFF   | Devices, ROMs, switches     |
| Bank-switched  | $D000   | $FFFF   | Language card/Aux RAM/ROM   |

**Analysis:** The memory map must be carefully managed to avoid conflicts with BASIC interpreters, disk drivers, and display buffers. ProDOS and DOS 3.3 reserve specific zero page and workspace locations; PocketOS should save and restore these as needed for compatibility.

---

## 3. Real vs Enhanced Mode Detection and Behavior

### 3.1 ROM Version and Hardware Detection

PocketOS must detect the hardware mode (real vs enhanced) by reading specific ROM signature bytes:

| Model                  | ROM Byte(s) | Value(s) |
|------------------------|-------------|----------|
| Apple ][               | $FBB3       | $38      |
| Apple ][+              | $EA         | $AD      |
| Apple IIe              | $06         | $EA      |
| Apple IIe (enhanced)   | $06         | $E0      |
| Apple IIc              | $06         | $00      |
| Apple IIgs             | $FE1F       | $60      |

**6502 Assembly Example:**

```assembly
LDA $FBB3
CMP #$06
BEQ IsIIe
; ... check other bytes for enhanced mode
```


### 3.2 Custom ROM Signature for Pocket2e

For Pocket2e emulator, a custom ROM signature byte may be defined (e.g., $FBC0 or $FBBF) to indicate enhanced features or emulator-specific capabilities. The OS should check for this signature and adapt its behavior accordingly.

### 3.3 Mode-Dependent Behavior

- **Real Mode:** Use only 6502/65C02 instructions, avoid enhanced features, restrict memory access to main RAM.
- **Enhanced Mode:** Enable 65C02 instructions, auxiliary RAM, language card overlays, and advanced VFS features.

**Analysis:** This detection ensures binary compatibility with real hardware and emulators, allowing PocketOS to adapt its feature set dynamically.

---

## 4. Syscall Layer and Calling Convention

### 4.1 Minimal Syscall Layer

PocketOS provides a minimal syscall layer for file I/O, memory management, device access, and system services. The syscall interface is designed for extensibility and modularity.

#### Syscall Vector Table (Example)

| Vector | Address | Function         |
|--------|---------|------------------|
| 0      | $0300   | File Open        |
| 1      | $0302   | File Read        |
| 2      | $0304   | File Write       |
| 3      | $0306   | File Close       |
| 4      | $0308   | Memory Alloc     |
| 5      | $030A   | Memory Free      |
| 6      | $030C   | Device IOCTL     |
| 7      | $030E   | VFS Mount        |
| ...    | ...     | ...              |

**6502 Assembly Example:**

```assembly
JSR ($0300) ; Call File Open syscall
```


#### Syscall Table Diagram

```
+-------------------+
| Syscall Vector    |
| Table ($0300)     |
+-------------------+
| 0: File Open      |
| 1: File Read      |
| 2: File Write     |
| ...               |
+-------------------+
```

### 4.2 Calling Convention

- **Parameters:** Passed via registers (A, X, Y) or memory locations (zero page, stack).
- **Return Values:** Typically in A (Accumulator) or designated memory location.
- **Error Codes:** Standardized error codes returned in A or status register.

**C-Style Pseudocode Example:**

```c
uint8_t syscall(uint8_t vector, void* params) {
    // Set up registers
    A = vector;
    X = params_low;
    Y = params_high;
    JSR syscall_vector[vector];
    return A; // error code or result
}
```


### 4.3 Extensibility

The syscall vector table is located in RAM and can be patched at runtime to add new system calls or redirect existing ones. This allows for modular driver installation and future expansion.

---

## 5. Virtual File System (VFS) and Filesystem Translator (FST) Architecture

### 5.1 Modular VFS Layer

PocketOS features a modular VFS layer with pluggable filesystem translators (FSTs). The VFS abstracts file and device access, allowing support for multiple filesystems (ProDOS, PocketFS, ZealFS, etc.).

#### VFS Architecture Diagram

```
+-------------------+
| PocketOS VFS      |
+-------------------+
| FST: ProDOS       |
| FST: PocketFS     |
| FST: ZealFS       |
| ...               |
+-------------------+
| Block Device      |
| RAM Disk          |
| Floppy Disk       |
| ...               |
+-------------------+
```

### 5.2 Filesystem Translator Interface

Each FST implements a standard interface:

- `mount(volume)`
- `open(file)`
- `read(file, buffer, len)`
- `write(file, buffer, len)`
- `close(file)`
- `ioctl(device, cmd, args)`

**C-Style Pseudocode Example:**

```c
struct FST {
    int (*mount)(Volume*);
    int (*open)(File*);
    int (*read)(File*, void*, size_t);
    int (*write)(File*, const void*, size_t);
    int (*close)(File*);
    int (*ioctl)(Device*, int, void*);
};
```


### 5.3 ProDOS Translator

- Supports hierarchical directories, 15-character filenames, 512-byte blocks.
- Implements seedling/sapling/tree file structures for efficient storage.
- Compatible with ProDOS disk images and utilities.

### 5.4 PocketFS Translator

- Designed for small-memory systems, minimal overhead.
- Supports files and directories with unlimited nesting, BCD timestamps, and compact page-based storage.
- Extensible to larger storage sizes and FAT tables for fast lookups.

### 5.5 VFS Mount Table (Example)

| Slot | Volume Name | FST      | Mounted | Block Size | Max Files |
|------|------------|----------|---------|------------|-----------|
| 0    | /RAM       | PocketFS | Yes     | 256        | 12        |
| 1    | /DISK      | ProDOS   | Yes     | 512        | 51        |
| 2    | /NVRAM     | ZealFS   | Yes     | 256        | 6         |

**Analysis:** The modular VFS design allows PocketOS to support legacy and modern filesystems, RAM disks, and future storage devices with minimal changes to the kernel.

---

## 6. COP Instruction Usage Plan and Trap Mechanism

### 6.1 COP Instruction Overview

The COP instruction (opcode $02) is available on 65816/65832 CPUs and can be reserved for future system calls or trap mechanisms. On 65C02, COP is not available, but PocketOS should be designed for future extensibility.

**COP Usage:**

- Acts as a software interrupt, similar to BRK.
- Pushes program counter and status register to stack, jumps to COP vector ($FFF4/$FFF5).
- Signature byte following COP can be used as syscall index.

**6502 Assembly Example:**

```assembly
COP
.byte $05 ; syscall index
```


### 6.2 Trap Handler Implementation

The OS COP handler retrieves the signature byte and dispatches to the appropriate syscall:

```assembly
COP_Handler:
    PLA         ; Pull return address low
    STA temp
    PLA
    STA temp+1
    DEC temp    ; Adjust to signature byte
    LDA (temp)  ; Load signature
    TAX         ; Syscall index
    JMP (SyscallTable,X)
```


### 6.3 Extensibility Toward 65816/65832

- Reserve COP for privileged system calls, traps, or future ring-zero enforcement.
- Design syscall table to support both BRK and COP dispatch.
- Plan for MMU permissions and privilege model in future versions.

**Analysis:** Using COP for system calls enables modular, relocatable OS design and future privilege separation.

---

## 7. Compatibility Considerations for Real Hardware and Emulators

### 7.1 Binary-Compatible Mode

PocketOS must run on:

- Real Apple II hardware (][, ][+, IIe, IIc, IIgs).
- Third-party emulators (Pocket2e, AppleWin, MicroM8, etc.).

#### Compatibility Table

| Feature           | Real Hardware | Emulators | Pocket2e |
|-------------------|--------------|-----------|----------|
| 65C02 Instructions| Yes (IIe+)   | Yes       | Yes      |
| Aux RAM           | IIe/IIc      | Yes       | Yes      |
| Language Card     | ][+/IIe      | Yes       | Yes      |
| COP Instruction   | No           | Partial   | Future   |
| VFS Layer         | Yes          | Yes       | Yes      |

### 7.2 Hardware Constraints

- Respect reserved memory regions (zero page, stack, display buffers).
- Use soft switches for bank switching and device access.
- Avoid overwriting BASIC interpreter or disk driver workspace.

### 7.3 Emulator-Specific Features

- Detect emulator-specific ROM signatures for enhanced features.
- Support debugging, tracing, and virtual peripherals as available.

**Analysis:** Careful hardware and emulator detection ensures robust binary compatibility and smooth operation across platforms.

---

## 8. Bootloader Design for Dual VFS (ProDOS and PocketFS)

### 8.1 Dual VFS Bootloader

The bootloader must:

- Detect disk format (ProDOS, DOS, PocketFS) by examining key sectors/blocks.
- Load appropriate kernel modules for detected filesystem.
- Initialize VFS layer and mount root volume.

**6502 Assembly Snippet:**

```assembly
LDA #$00
STA $0800 ; Boot sector loaded
JSR DetectFS
BEQ ProDOSBoot
JSR PocketFSBoot
```


### 8.2 Modular Driver Model and .SYSTEM Chaining

- Support modular driver installation via .SYSTEM files (ProDOS convention).
- Chain drivers for clock, RAM disk, quit handler, etc..
- Allow custom driver modules for PocketOS features.

**Analysis:** Modular bootloader and driver chaining enable flexible system configuration and easy extension.

---

## 9. Memory Management and Future 65816/65832 Extensibility

### 9.1 Memory Allocator Design

PocketOS should implement a simple memory allocator for dynamic allocation:

- First-fit, best-fit, or segregated-list algorithms.
- Support for block splitting and coalescing.
- Explicit free-list for efficient reuse.

**C-Style Pseudocode Example:**

```c
struct Block {
    size_t size;
    bool used;
    Block* next;
    uint8_t data[];
};

void* alloc(size_t size);
void free(void* ptr);
```


### 9.2 Extensibility Toward MMU and Privilege Model

- Plan for MMU support and virtual address translation in future versions.
- Design memory allocator and syscall layer for privilege separation (ring-zero enforcement).
- Reserve COP instruction and syscall vectors for privileged operations.

**Analysis:** Forward-compatible memory management and privilege model support future migration to 65816/65832 architectures.

---

## 10. Syscall Examples

### 10.1 File I/O

**Open File:**

```assembly
LDA #filename_ptr
LDX #flags
JSR ($0300) ; File Open
```

**Read File:**

```assembly
LDA #file_handle
LDX #buffer_ptr
LDY #length
JSR ($0302) ; File Read
```

### 10.2 Memory Allocation

**Allocate Memory:**

```assembly
LDA #size
JSR ($0304) ; Memory Alloc
```

**Free Memory:**

```assembly
LDA #ptr
JSR ($0306) ; Memory Free
```

### 10.3 Device IOCTL

**Device Control:**

```assembly
LDA #device_id
LDX #command
LDY #args_ptr
JSR ($0308) ; Device IOCTL
```


---

## 11. VFS Translator Examples

### 11.1 ProDOS Translator

- Implements hierarchical directories, seedling/sapling/tree file structures.
- Supports file creation, reading, writing, and deletion.
- Compatible with ProDOS utilities and disk images.

### 11.2 PocketFS Translator

- Minimal overhead, page-based storage.
- Supports files and directories with unlimited nesting.
- Efficient for small-memory systems and RAM disks.

**Analysis:** Pluggable translators enable support for legacy and modern filesystems, RAM disks, and future storage devices.

---

## 12. Testing and Debugging on Emulator and Real Hardware

### 12.1 Emulator Testing

- Use Pocket2e, AppleWin, MicroM8, and other emulators for development and debugging.
- Test bootloader, kernel, VFS, and drivers in various configurations.
- Validate memory map, syscall layer, and hardware detection.

### 12.2 Real Hardware Testing

- Test on Apple ][, ][+, IIe, IIc, and IIgs hardware.
- Verify binary compatibility, memory access, and device support.
- Use diagnostic utilities and hardware monitors for validation.

**Analysis:** Comprehensive testing ensures robust operation and compatibility across platforms.

---

## 13. Security and Privilege Model for Future 65816/65832

### 13.1 Privilege Rings and MMU Permissions

- Plan for hierarchical protection domains (rings) in future versions.
- Implement ring-zero (kernel mode) for OS code, ring-three (user mode) for applications.
- Use MMU to enforce memory access permissions and process isolation.

### 13.2 COP Instruction for Privileged System Calls

- Reserve COP for privileged operations and trap mechanisms.
- Use syscall table and signature byte for secure dispatch.

**Analysis:** Advanced privilege models and MMU support enhance security and stability in future PocketOS versions.

---

## 14. Documentation and Developer API

### 14.1 Developer API Overview

- Provide clear documentation for syscall layer, VFS interface, memory management, and device access.
- Offer C-style and assembly examples for common operations.
- Document hardware detection, soft switches, and compatibility constraints.

### 14.2 Example API Functions

**C-Style Pseudocode:**

```c
int file_open(const char* path, int flags);
int file_read(int handle, void* buffer, size_t len);
int file_write(int handle, const void* buffer, size_t len);
int file_close(int handle);
void* mem_alloc(size_t size);
void mem_free(void* ptr);
int device_ioctl(int device, int cmd, void* args);
```


**Assembly Example:**

```assembly
LDA #filename_ptr
LDX #flags
JSR ($0300) ; File Open
```

**Analysis:** Comprehensive documentation and API examples facilitate developer adoption and system extensibility.

---

## 15. Implementation-Ready Summary

### 15.1 Key Specification Points

- **Bootable from ProDOS-formatted disk, supports PocketFS VFS.**
- **Binary-compatible mode for real hardware and emulators.**
- **Detects real vs enhanced mode via ROM signature bytes.**
- **Utilizes auxiliary RAM and language card for extended functionality.**
- **Minimal syscall layer with extensible vector table.**
- **Modular VFS layer with pluggable filesystem translators.**
- **COP instruction reserved for future system calls/traps.**
- **Designed for extensibility toward 65816/65832 architectures and privilege models.**

### 15.2 Implementation Checklist

- [ ] Bootloader supports ProDOS and PocketFS detection.
- [ ] Memory map reserves zero page, stack, system workspace, and overlays.
- [ ] Syscall vector table implemented in RAM, patchable at runtime.
- [ ] VFS layer supports multiple translators and device types.
- [ ] COP instruction and trap handler reserved for future use.
- [ ] Compatibility tested on real hardware and emulators.
- [ ] Documentation and developer API provided.

---

## 16. References

- Apple II ProDOS-8 system files and driver chaining
- Apple II peripheral cards and memory expansion
- Apple II ROM disassembly and hardware detection
- 6502/65C02/65816 assembly programming and instruction set
- ZealFS and modular VFS architecture
- ProDOS technical reference and memory use
- COP instruction and privilege model for future extensibility
- Emulator compatibility and testing

---

## 17. Appendix: Code Snippets

### 17.1 Bootloader (6502 Assembly)

```assembly
; Bootloader loads PocketOS kernel from disk
ORG $0800
LDX #0
LOOP: LDA BOOTMSG,X
      BEQ DONE
      JSR $FDED ; COUT
      INX
      BNE LOOP
DONE: JMP $2000 ; Jump to OS entry
BOOTMSG: .BYTE "PocketOS Boot", $00
```


### 17.2 Syscall Vector Table (6502 Assembly)

```assembly
; Syscall vector table at $0300
ORG $0300
JMP FileOpen
JMP FileRead
JMP FileWrite
JMP FileClose
JMP MemAlloc
JMP MemFree
JMP DeviceIOCTL
JMP VFSMount
```


### 17.3 COP Trap Handler (65816 Assembly)

```assembly
COP_Handler:
    PLA         ; Pull return address low
    STA temp
    PLA
    STA temp+1
    DEC temp    ; Adjust to signature byte
    LDA (temp)  ; Load signature
    TAX         ; Syscall index
    JMP (SyscallTable,X)
```


---

## 18. Conclusion

PocketOS Gen 1 provides a robust, extensible foundation for 65C02-based Apple II emulation, supporting boot from ProDOS and custom VFS, binary compatibility with real hardware, dynamic hardware detection, modular syscall and VFS layers, and future extensibility toward advanced CPUs and privilege models. The specification is implementation-ready, with detailed memory maps, syscall tables, boot diagrams, and code examples, ensuring a smooth development and deployment process for both emulator and real hardware environments.

---
Absolutely — this is the kind of foundational spec that sets the tone for everything to come. I’ve kicked off a deep research task to design the **minimum viable, bootable Gen 1 “PocketOS”** spec for your 65C02-based Pocket2e emulator, with the following goals:

- Bootable from a 5.25" floppy (ProDOS or custom VFS)
- Binary-compatible mode for real Apple II hardware and third-party emulators
- Enhanced mode detection via ROM version or custom ROM signature
- Full use of auxiliary RAM and language card memory
- Minimal syscall layer with extensibility toward 65816/65832
- Modular VFS with pluggable filesystem translators
- Optional COP instruction usage for future system calls or traps
- Designed for future privilege model and MMU enforcement

🛠 This will take me several minutes, so feel free to step away — I’ll keep working in the background. Your spec will be saved in this conversation.
