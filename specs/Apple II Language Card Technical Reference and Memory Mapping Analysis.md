# Apple II Language Card: Technical Reference and Memory Mapping Analysis

------

## Introduction

The Apple II Language Card, introduced in 1979, was a pivotal expansion for the original Apple II and II Plus computers. By providing an additional 16 KB of RAM, it enabled users to run advanced programming languages (notably UCSD Pascal), load alternate BASIC interpreters, and utilize software that required a full 64 KB memory map. The card's unique ability to map RAM into the address space normally occupied by system ROM ($D000–$FFFF) was achieved through a sophisticated bank-switching mechanism controlled by a set of softswitches at $C080–$C08F. This technical reference provides an exhaustive, accurate, and detailed explanation of how the Language Card organizes and maps its memory, the function and behavior of its softswitches, the nuances of its write-protect logic, and the differences between the original card and later built-in implementations such as the Apple IIe. It is intended as a definitive guide for hardware designers, emulator authors, reverse engineers, and advanced Apple II programmers.

------

## Physical Organization of the Language Card's 16 KB RAM

### Overview of Card Architecture

The Apple II Language Card is installed in slot 0 and connects to the mainboard via a ribbon cable that replaces one of the onboard DRAM chips. This cable allows the card to intercept address and control signals, enabling it to selectively override the system ROM in the upper 16 KB of address space ($D000–$FFFF) with its own RAM. The card contains 16 KB of dynamic RAM, typically implemented as eight 16Kx1 DRAM chips, and, in early versions, an optional F8 Autostart ROM chip for booting enhancements.

#### Memory Slices and Banks

The 16 KB RAM is physically divided as follows:

- **$D000–$DFFF (4 KB):** This region is bank-switched between two 4 KB slices, referred to as "Bank 1" and "Bank 2."
- **$E000–$FFFF (8 KB):** This region is mapped as a single contiguous block, not bank-switched; it is always visible when RAM is enabled.
- **$F800–$FFFF (2 KB):** This region can be mapped to either the card's RAM or its onboard ROM (if present), depending on softswitch settings.

This organization is necessary because the Apple II's address space from $C000–$CFFF is reserved for I/O and peripheral cards, leaving only 12 KB ($D000–$FFFF) available for mapping the card's 16 KB RAM. The bank-switching mechanism allows both 4 KB slices to be accessed in the $D000–$DFFF window, effectively multiplexing the physical RAM into the available address space.

#### Schematic and Hardware Details

The card's logic includes:

- **Address Decoders:** Typically implemented with 3-to-8 line decoders (e.g., SN74HCS137) to select softswitches and memory banks.
- **Bus Transceivers:** Octal bus transceivers (e.g., 74LS245) buffer data between the card and the system bus, allowing bidirectional communication and isolation when the card is not active.
- **Latches:** Used to hold the state of softswitches and write-protect logic, ensuring stable memory mapping even as the CPU executes instructions.
- **Pull-up Resistors:** Ensure defined logic levels on control lines, preventing floating inputs that could cause erratic behavior.
- **Optional ROM Socket:** For the F8 Autostart ROM, which can replace the system monitor ROM and provide enhanced boot features.

Modern reproductions and FPGA implementations often use static RAM and programmable logic devices, simplifying the design and eliminating the need for the ribbon cable connection to the motherboard.

------

## Softswitches at $C080–$C08F: Functions and Control Mechanism

### Softswitch Address Map

The Language Card's operation is controlled by reading from specific addresses in the $C080–$C08F range. Each address acts as a "softswitch," changing the card's internal state. The softswitches are divided into two groups, corresponding to the two 4 KB banks for $D000–$DFFF:

| Address | Bank | Function               | Read/Write Behavior |
| ------- | ---- | ---------------------- | ------------------- |
| $C080   | 2    | Enable RAM read        | Read only           |
| $C081   | 2    | Enable RAM write       | Read only           |
| $C082   | 2    | Disable RAM read (ROM) | Read only           |
| $C083   | 2    | Enable RAM read/write  | Read only           |
| $C088   | 1    | Enable RAM read        | Read only           |
| $C089   | 1    | Enable RAM write       | Read only           |
| $C08A   | 1    | Disable RAM read (ROM) | Read only           |
| $C08B   | 1    | Enable RAM read/write  | Read only           |

**Note:** Only reads from these addresses affect the card's state. Writes (e.g., STA $C080) do not reliably change the mapping on real hardware, though some emulators may incorrectly support this.

### Softswitch Functionality

Each softswitch controls three aspects:

1. **Bank Selection:** Bit 3 of the address ($C08x, where x = 0–3 for Bank 2, x = 8–B for Bank 1) determines which 4 KB bank is mapped into $D000–$DFFF.
2. **RAM/ROM Selection:** Bits 0–1 select whether RAM or ROM is mapped into the address space and whether RAM is readable.
3. **Write-Protect Logic:** Certain softswitches enable or disable write access to the RAM, protecting it from accidental modification.

#### Detailed Behavior

- **Enable RAM Read ($C080/$C088):** Maps the selected bank's RAM into $D000–$DFFF for reading. Write-protect is enabled; RAM cannot be written.
- **Enable RAM Write ($C081/$C089):** Maps ROM into $D000–$DFFF for reading, but enables writing to RAM at those addresses. This is a "pre-write latch" state; two consecutive reads to this address are required to fully enable RAM for writing.
- **Disable RAM Read (ROM) ($C082/$C08A):** Maps ROM into $D000–$DFFF for both reading and writing. RAM is write-protected.
- **Enable RAM Read/Write ($C083/$C08B):** Maps the selected bank's RAM into $D000–$DFFF for both reading and writing. Two consecutive reads to this address are required to fully enable read/write access.

**Quirk:** The card's write-enable logic is edge-sensitive; enabling write access requires two consecutive reads to the appropriate softswitch ($C083 or $C08B). This prevents accidental writes due to spurious reads and is a safeguard against programming errors.

#### Address Decoding

Bit 2 of the softswitch address is ignored, so $C080–$C083 and $C084–$C087 are equivalent; similarly, $C088–$C08B and $C08C–$C08F are equivalent. This allows for some redundancy and compatibility with software that may use different addresses for the same function.

------

## Memory Mapping: $D000–$FFFF Region

### Mapping Overview

The Language Card maps its 16 KB RAM into the $D000–$FFFF address space as follows:

- **$D000–$DFFF (4 KB):** Bank-switched between Bank 1 and Bank 2 via softswitches.
- **$E000–$FFFF (8 KB):** Mapped as a single block; always RAM when enabled, not bank-switched.
- **$F800–$FFFF (2 KB):** Can be mapped to RAM or the card's ROM, depending on softswitch settings.

#### $E000–$FFFF: Switched as a Unit

The $E000–$FFFF region is switched as a single 8 KB unit. When RAM is enabled via the softswitches, this entire region is mapped to the card's RAM. There is no further subdivision into 4 KB slices; the bank-switching mechanism only affects $D000–$DFFF.

#### $D000–$DFFF: Bank-Switched Behavior

This region is unique in that it can be mapped to either Bank 1 or Bank 2 of the card's RAM, depending on which softswitch is accessed. This allows software to access both 4 KB slices by toggling the softswitches, effectively multiplexing 8 KB of RAM into a 4 KB window.

**Example:** To access Bank 1, read from $C088–$C08B; to access Bank 2, read from $C080–$C083. The rest of the address space ($E000–$FFFF) remains mapped to the card's RAM regardless of the bank selection for $D000–$DFFF.

#### $F800–$FFFF: ROM/RAM Selection

When RAM is deselected (i.e., when the appropriate softswitch is accessed), the card's onboard ROM (if present) is mapped into $F800–$FFFF. Otherwise, this region is mapped to RAM. This feature was primarily used to provide the Autostart ROM for early Apple II systems that lacked it on the mainboard.

------

## Truth Table: Softswitch Combinations and Memory Visibility

The following table summarizes all valid combinations of softswitch settings and their effects on memory mapping. Each row represents a softswitch read; the mapping is updated immediately after the read.

| Softswitch | Bank | RAM Read | RAM Write | $D000–$DFFF Mapping            | $E000–$FFFF Mapping | $F800–$FFFF Mapping | Notes                                                     |
| ---------- | ---- | -------- | --------- | ------------------------------ | ------------------- | ------------------- | --------------------------------------------------------- |
| $C080      | 2    | Yes      | No        | Bank 2 RAM (read)              | RAM                 | RAM                 | Write-protect enabled                                     |
| $C081      | 2    | No       | Yes*      | ROM (read), Bank 2 RAM (write) | RAM                 | RAM                 | Pre-write latch; two reads required for full write-enable |
| $C082      | 2    | No       | No        | ROM                            | ROM                 | ROM                 | RAM write-protect enabled                                 |
| $C083      | 2    | Yes      | Yes*      | Bank 2 RAM (read/write)        | RAM                 | RAM                 | Two reads required for full read/write enable             |
| $C088      | 1    | Yes      | No        | Bank 1 RAM (read)              | RAM                 | RAM                 | Write-protect enabled                                     |
| $C089      | 1    | No       | Yes*      | ROM (read), Bank 1 RAM (write) | RAM                 | RAM                 | Pre-write latch; two reads required for full write-enable |
| $C08A      | 1    | No       | No        | ROM                            | ROM                 | ROM                 | RAM write-protect enabled                                 |
| $C08B      | 1    | Yes      | Yes*      | Bank 1 RAM (read/write)        | RAM                 | RAM                 | Two reads required for full read/write enable             |

**Legend:**

- "Yes*" indicates that write-enable requires two consecutive reads to the softswitch.
- "Bank 1" and "Bank 2" refer to the two 4 KB slices of RAM mapped into $D000–$DFFF.
- "ROM" refers to the system ROM or the card's onboard ROM, depending on configuration.

**Redundancy:** $C084–$C087 and $C08C–$C08F are equivalent to $C080–$C083 and $C088–$C08B, respectively.

### Detailed Explanation

When a softswitch is read, the card's internal latches update the mapping as follows:

- **Bank Selection:** Bit 3 of the address selects Bank 1 ($C088–$C08B) or Bank 2 ($C080–$C083).
- **RAM/ROM Selection:** Bits 0–1 determine whether RAM is mapped for reading, writing, both, or neither.
- **Write-Protect Logic:** Write access is only enabled after two consecutive reads to the appropriate softswitch ($C083 or $C08B). This prevents accidental writes and ensures that software must explicitly request write access.

**Edge Cases:** If RAM is deselected (i.e., ROM is mapped), the card's RAM may still be written if write-enable is active, but it cannot be read. This is a rare state, typically used for initialization or testing purposes.

------

## Pre-Write Latch Behavior and Write-Protect Logic

### Pre-Write Latch Mechanism

The Language Card employs a "pre-write latch" to control write access to its RAM. To enable writing, software must perform two consecutive reads to the write-enable softswitch ($C083 for Bank 2, $C08B for Bank 1). The first read arms the latch; the second read activates write access. This mechanism prevents accidental writes due to stray reads and ensures that only deliberate software actions enable writing.

**Example Sequence:**

1. LDA $C083      ; Arms the write-enable latch for Bank 2
2. LDA $C083      ; Enables write access to Bank 2 RAM
3. STA $D000      ; Writes to Bank 2 RAM at $D000

If only one read is performed, write access is not enabled, and the RAM remains protected.

### Write-Protect Logic

Write-protect is enforced by the card's latches and address decoders. When write-protect is active (i.e., after reading $C080/$C088 or $C082/$C08A), any attempt to write to the card's RAM is ignored. This protects critical data (such as loaded interpreters or language environments) from accidental modification.

**Quirk:** If RAM is deselected (i.e., ROM is mapped), but write-enable is active, writes to the $D000–$DFFF region go to RAM, but reads return ROM data. This can be used for certain initialization routines but is generally avoided in normal operation.

------

## Practical Examples: DOS 3.3 and BASIC Usage

### DOS 3.3 and BASIC Mapping

When booting DOS 3.3 with the Language Card installed, the system automatically loads the alternate BASIC interpreter (Applesoft or Integer BASIC, depending on the mainboard ROM) into the card's RAM. The mapping is configured as follows:

- **$D000–$F7FF:** Interpreter code loaded into RAM; mapped via softswitches for read/write access during initialization, then write-protected for normal operation.
- **$F800–$FFFF:** System monitor code; mapped to ROM or RAM as needed.

**Switching BASIC Interpreters:** The INT and FP commands in DOS 3.3 toggle between Integer BASIC and Applesoft BASIC by remapping the card's RAM and updating the softswitches. This allows seamless switching without rebooting or reloading code.

### UCSD Pascal and Other Languages

UCSD Pascal and other advanced language environments require a full 64 KB of RAM. The Language Card enables this by mapping its RAM into the upper 16 KB, allowing these environments to load and execute code in the entire address space. The bank-switching mechanism is used to access both 4 KB slices in $D000–$DFFF, while the contiguous 8 KB in $E000–$FFFF is always available when RAM is enabled.

------

## Hardware Implementation Details

### Latch Chips and Decoders

- **Latches:** Typically implemented with D-type flip-flops or SR latches, these hold the state of the softswitches and write-protect logic. They are level-sensitive and update immediately upon reading a softswitch address.
- **Decoders:** 3-to-8 line decoders (e.g., SN74HCS137) are used to select which softswitch is being accessed and to route control signals to the appropriate memory bank.
- **Bus Transceivers:** Octal bus transceivers (e.g., 74LS245) buffer data between the card and the system bus, allowing for bidirectional communication and isolation when the card is not active.
- **Pull-ups:** Ensure defined logic levels on control lines, preventing floating inputs that could cause erratic behavior.

### Bus Isolation and Timing

The card's logic ensures that only one memory source (RAM or ROM) drives the data bus at any time, preventing bus contention. Timing is critical; the latches must update quickly enough to ensure correct mapping before the CPU accesses memory. Modern reproductions often use faster static RAM and programmable logic devices, simplifying timing constraints.

------

## Differences: Original Language Card vs. Apple IIe Built-In Implementation

### Original Language Card

- **Physical Card:** Installed in slot 0; requires a ribbon cable connection to the mainboard.
- **Softswitches:** Controlled via $C080–$C08F; only reads affect mapping.
- **Bank-Switching:** Two 4 KB banks for $D000–$DFFF; 8 KB contiguous for $E000–$FFFF.
- **Optional ROM:** F8 Autostart ROM socket for enhanced boot features.

### Apple IIe Built-In Implementation

- **Integrated Logic:** Bank-switching and softswitches are implemented in the mainboard logic; no physical card required.
- **Auxiliary RAM:** The IIe supports an auxiliary 64 KB RAM bank, allowing for more advanced memory mapping and 80-column text support.
- **Softswitches:** Similar addresses ($C080–$C08F) are used, but the logic is integrated and supports additional features (e.g., 80-column card, double hi-res graphics).
- **Slot 0:** The IIe does not have a physical slot 0; auxiliary RAM expansion uses a dedicated slot.

**Compatibility:** The original Language Card is not required or beneficial in the IIe, as its functionality is built-in and extended. Attempting to install a Language Card in the IIe is not recommended and may cause conflicts or reduced functionality.

------

## Community Reverse-Engineering Notes and Modern Reproductions

### Reverse Engineering

Enthusiasts have reverse-engineered the Language Card's logic, producing schematics, FPGA implementations, and modern reproductions. Key findings include:

- **Softswitch Redundancy:** Bit 2 of the softswitch address is ignored, allowing for multiple equivalent addresses.
- **Write-Enable Quirk:** Two consecutive reads are required to enable writing; this is faithfully reproduced in modern emulators and hardware clones.
- **Bus Isolation:** Proper timing and isolation are critical for reliable operation; modern reproductions often use faster logic and static RAM to simplify design.

### FPGA and Static RAM Implementations

Modern reproductions use static RAM and programmable logic devices, eliminating the need for the ribbon cable and simplifying installation. These cards are compatible with the original softswitch logic and provide full 16 KB RAM mapping in $D000–$FFFF.

------

## Edge Cases and Invalid/Unsupported Softswitch Combinations

### Invalid Combinations

- **Writes to Softswitches:** Only reads from $C080–$C08F affect the card's state. Writes (e.g., STA $C080) do not reliably change mapping on real hardware, though some emulators may incorrectly support this.
- **Uninitialized State:** On power-up, the card defaults to Bank 2 mapped into $D000–$DFFF, RAM enabled for writing, and ROM mapped into $F800–$FFFF.
- **Redundant Addresses:** $C084–$C087 and $C08C–$C08F are equivalent to $C080–$C083 and $C088–$C08B, respectively; accessing these addresses does not cause errors but may be unnecessary.

### Unsupported States

- **RAM Deselect with Write-Enable:** If RAM is deselected (i.e., ROM is mapped), but write-enable is active, writes go to RAM but reads return ROM data. This is a rare state and generally avoided in normal operation.

------

## Autostart ROM Interaction

### F8 ROM Replacement

The original Language Card includes an optional F8 Autostart ROM socket. When installed, this ROM replaces the system monitor ROM at $F800–$FFFF, providing enhanced boot features such as automatic disk booting and improved screen editing. The ROM is mapped via softswitches and can be enabled or disabled as needed.

### Boot Sequence

On power-up, the Autostart ROM initializes the system, displays the "APPLE II" message, and attempts to boot from the disk in drive 1, slot 6. If no disk is present, the system falls back to BASIC or monitor mode, depending on configuration.

------

## Summary Table: Softswitch Effects

| Softswitch  | Bank | RAM Read | RAM Write | $D000–$DFFF                    | $E000–$FFFF | $F800–$FFFF | Notes                 |
| ----------- | ---- | -------- | --------- | ------------------------------ | ----------- | ----------- | --------------------- |
| $C080/$C084 | 2    | Yes      | No        | Bank 2 RAM (read)              | RAM         | RAM         | Write-protect enabled |
| $C081/$C085 | 2    | No       | Yes*      | ROM (read), Bank 2 RAM (write) | RAM         | RAM         | Pre-write latch       |
| $C082/$C086 | 2    | No       | No        | ROM                            | ROM         | ROM         | Write-protect enabled |
| $C083/$C087 | 2    | Yes      | Yes*      | Bank 2 RAM (read/write)        | RAM         | RAM         | Two reads required    |
| $C088/$C08C | 1    | Yes      | No        | Bank 1 RAM (read)              | RAM         | RAM         | Write-protect enabled |
| $C089/$C08D | 1    | No       | Yes*      | ROM (read), Bank 1 RAM (write) | RAM         | RAM         | Pre-write latch       |
| $C08A/$C08E | 1    | No       | No        | ROM                            | ROM         | ROM         | Write-protect enabled |
| $C08B/$C08F | 1    | Yes      | Yes*      | Bank 1 RAM (read/write)        | RAM         | RAM         | Two reads required    |

------

## Conclusion

The Apple II Language Card is a masterful example of early memory expansion technology, enabling advanced language environments and full 64 KB RAM operation on the original Apple II and II Plus. Its sophisticated bank-switching mechanism, controlled by a set of softswitches at $C080–$C08F, allows for flexible mapping of its 16 KB RAM into the $D000–$FFFF address space. The card's design, including pre-write latch behavior and write-protect logic, ensures reliable operation and protects critical data from accidental modification. While later systems such as the Apple IIe integrated this functionality into the mainboard, the original Language Card remains a vital component for understanding Apple II memory architecture and for running legacy software that requires its unique capabilities.

This technical reference has provided a comprehensive, detailed, and accurate explanation of the Language Card's physical organization, softswitch control, memory mapping, hardware implementation, and practical usage. It serves as a definitive guide for hardware designers, emulator authors, reverse engineers, and advanced Apple II programmers seeking to understand or reproduce the card's behavior.

------

**References:**

- Apple Language Card Installation and Operation Manual
- Community reverse-engineering notes and modern reproductions
- Apple II peripheral card documentation and schematics
- Emulator and FPGA implementation details
- Hardware component datasheets and logic guides