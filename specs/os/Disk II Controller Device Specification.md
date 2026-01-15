# Disk II Controller Device Specification

## Document Information

| Field        | Value                                      |
|--------------|--------------------------------------------|
| Version      | 0.1                                        |
| Date         | 2026-01-15                                 |
| Status       | Draft                                      |
| Applies To   | Apple II/II+, IIe (Disk II / 13-16 sector) |

---

## 1. Overview

The Disk II controller is the original Apple II 5.25" floppy interface. It exposes a
low-level, timing-sensitive interface through slot I/O soft switches plus a 256-byte
slot ROM. Unlike SmartPort, Disk II firmware expects cycle-accurate behavior from the
controller and drive. This document defines the emulator behavior required for a Disk
II controller device, including I/O mapping, ROM handling, disk image formats, timing,
and integration points with the existing emulator core.

### 1.1 Goals

- Provide accurate Disk II hardware behavior (read/write, motor, head, select, latch).
- Support sector, nibble, and bitstream disk images used by DOS 3.3 and copy protection.
- Integrate cleanly with the bus, device registry, and slot ROM model in the emulator core.
- Document timing constraints, format details, and hard-to-emulate edge cases.

### 1.2 Slot 6 Convention

Apple documentation and software conventions treat **slot 6** as the default Disk II slot.
The emulator should default Disk II controllers to slot 6, while still allowing the user
or configuration to place the device in any slot (1-7). Boot commands such as `PR#6` or
monitor `C600G` rely on this convention.

---

## 2. Hardware Behavior

### 2.1 Slot Addressing

The Disk II controller occupies the standard slot I/O space and 256-byte slot ROM for the
assigned slot `S` (1-7):

- **I/O soft switches**: `$C0S0-$C0SF`
- **ROM**: `$CS00-$CSFF` (256 bytes)

The base I/O address is `0xC080 + (S << 4)` (slot 1 -> `$C090-$C09F`, slot 6 ->
`$C0E0-$C0EF`). The ROM base is `0xC000 + (S << 8)` (slot 6 -> `$C600-$C6FF`).

### 2.2 Soft Switches and Registers

All accesses (read or write) to these addresses change controller state. Reads may also
return data or floating bus values depending on Q6/Q7 state.

| Address | Name      | Function (Side Effects)                                               |
|---------|-----------|------------------------------------------------------------------------|
| $C0S0   | PHASE0OFF | De-energize phase 0                                                    |
| $C0S1   | PHASE0ON  | Energize phase 0                                                      |
| $C0S2   | PHASE1OFF | De-energize phase 1                                                    |
| $C0S3   | PHASE1ON  | Energize phase 1                                                      |
| $C0S4   | PHASE2OFF | De-energize phase 2                                                    |
| $C0S5   | PHASE2ON  | Energize phase 2                                                      |
| $C0S6   | PHASE3OFF | De-energize phase 3                                                    |
| $C0S7   | PHASE3ON  | Energize phase 3                                                      |
| $C0S8   | MOTOROFF  | Motor off (also deselects drive)                                       |
| $C0S9   | MOTORON   | Motor on                                                              |
| $C0SA   | DRIVE1    | Select drive 1                                                        |
| $C0SB   | DRIVE2    | Select drive 2                                                        |
| $C0SC   | Q6L       | Q6 low                                                                |
| $C0SD   | Q6H       | Q6 high                                                               |
| $C0SE   | Q7L       | Q7 low                                                                |
| $C0SF   | Q7H       | Q7 high                                                               |

### 2.3 Q6/Q7 Data Path Behavior

Disk II uses the Q6/Q7 soft switches to select the shift register/data latch behavior.
The emulator should model the four combinations (read/write semantics differ by timing
and firmware expectations):

| Q7 | Q6 | Mode                             | Expected Behavior                                  |
|----|----|----------------------------------|----------------------------------------------------|
| 0  | 0  | Read data                         | Read shift register / read latch output            |
| 0  | 1  | Sense write-protect / prewrite    | Read write-protect, prewrite timing state          |
| 1  | 0  | Write data                        | Shift bits to disk (write mode)                    |
| 1  | 1  | Write load                        | Load data latch / shift register with next byte    |

Notes:
- Writes while Q7=0 are ignored.
- Reads while Q7=1 often return floating bus values on hardware; emulate by returning
  the last latched byte or `0xFF` for deterministic behavior unless floating bus is
  emulated globally.

### 2.4 Drive Select and Motor Control

- Two drives are supported per controller. Only the selected drive receives motor, step,
  and data signals.
- Motor on/off is software-managed. There is no automatic motor timeout in hardware.
- Boot ROM expects drive 1 for boot. Switching drives mid-operation should reset the
  read/write state and apply a brief settle delay.

### 2.5 Head Stepping and Track Position

The drive uses a four-phase stepper motor. The controller interprets the phase lines as
quarter-track positions when two adjacent phases are energized. The emulator should:

- Track current phase (0-3) and head position in half-track or quarter-track units.
- Move one quarter-track per valid phase transition sequence (0->1->2->3 or reverse).
- Support dual-phase (quarter-track) positions used by copy protection and diagnostics.
- Clamp the logical track range (0-34) unless supporting 40-track images.
- Ignore invalid phase sequences (e.g., skipping phases) or treat as no movement.

---

## 3. ROM Interface and Initialization

### 3.1 ROM Expectations

The Disk II controller card uses a 256-byte ROM (P5/P5A) mapped into slot ROM space. A
second PROM (P6/P6A) is used internally for controller state sequencing and is not CPU-
visible. Typical ROM behavior includes:

- Boot entry at `$CS00` (e.g., `$C600` for slot 6), used by `PR#6` or `C600G`.
- A boot routine that spins the drive, seeks track 0, reads sector 0, and executes it.
- Standard entry points used by DOS 3.3 or ProDOS 8 drivers.

### 3.2 ROM Loading Rules

- The emulator **must not** ship Apple ROM images. Users provide a 256-byte Disk II ROM.
- Validate ROM length (exactly 256 bytes) and optionally checksum known revisions.
- Map the ROM as a read-only `IBusTarget` (or ROM layer) at `$CS00-$CSFF`.
- Provide a fallback stub ROM in tests only (if needed) to avoid distributing Apple IP.

### 3.3 Reset and Power-On State

On power-on or reset:

- Motor off, drive 1 selected by default (historical behavior).
- Q6/Q7 default low (read mode).
- Phases all off; head position remains at track 0 unless configured otherwise.
- Allow a motor spin-up delay (~1 second) before returning valid disk data.

---

## 4. Disk Data Layout and Encoding

### 4.1 Standard Geometry

- 35 tracks (0-34)
- 16 sectors per track (DOS 3.3), 256 bytes per sector
- Total: 35 * 16 * 256 = **143,360 bytes (140 KB)**

13-sector DOS 3.2 media:

- 35 tracks, 13 sectors, 256 bytes per sector
- Total: 116,480 bytes (≈113.75 KB)

### 4.2 GCR Encoding and Fields

Disk II uses Group Code Recording (GCR) encoding:

- **13-sector (DOS 3.2)**: 5-and-3 encoding (32 legal bytes)
- **16-sector (DOS 3.3)**: 6-and-2 encoding (64 legal bytes)
- Address fields use 4-and-4 encoding plus sync bytes.

A track contains address fields, data fields, and gaps. A formatted 16-sector track
contains ~6,400-6,700 encoded bytes (nibbles) including overhead, depending on formatter
and drive speed.

### 4.3 Sector Ordering (DOS vs. ProDOS)

DOS 3.3 uses a logical-to-physical sector interleave:

| Logical | Physical |
|---------|----------|
| 0       | 0        |
| 1       | 7        |
| 2       | 14       |
| 3       | 5        |
| 4       | 12       |
| 5       | 3        |
| 6       | 10       |
| 7       | 1        |
| 8       | 8        |
| 9       | 15       |
| 10      | 6        |
| 11      | 13       |
| 12      | 4        |
| 13      | 11       |
| 14      | 2        |
| 15      | 9        |

ProDOS order is linear (0,1,2,3...). Disk images must specify or infer which order they
use.

---

## 5. Disk Image Handling

### 5.1 Supported Formats

| Format | Description | Notes |
|--------|-------------|-------|
| `.dsk` | Raw sector image | Common; usually DOS 3.3 order |
| `.do`  | DOS order image | Explicit DOS 3.3 ordering |
| `.po`  | ProDOS order image | Explicit ProDOS ordering |
| `.d13` | 13-sector image | DOS 3.2, 116,480 bytes |
| `.nib` | Nibble image | ~6,656 bytes/track; preserves sync/headers |
| `.woz` | Bitstream image (WOZ 1.0/2.0) | Best for copy protection |
| `.2mg` | Universal image with header | Includes metadata (order, size) |

### 5.2 Format Handling Rules

- `.dsk` is ambiguous; treat as `.do` by default and allow user override.
- `.nib` and `.woz` require nibble/bitstream pipelines; sector order is not relevant.
- `.2mg` headers specify sector order and read-only flags; honor them explicitly.
- Disk image providers should expose a read-only flag (WOZ/2MG metadata or user option).

### 5.3 Copy Protection and Quarter Tracks

- Copy protection often uses half/quarter tracks, nonstandard sector counts, or weak bits.
- `.nib` captures many protections; `.woz` captures bitstream-level anomalies and timing.
- The controller should support quarter-track positioning and track length variance.

---

## 6. I/O Timing and Emulation Challenges

### 6.1 Rotation and Bit Timing

- Disk II drives spin at ~300 RPM (one revolution ≈ 200 ms).
- Bit cell timing is ~4 µs (≈250 kbit/s).
- At 1 MHz CPU speed, bytes arrive roughly every 32 cycles.

### 6.2 CPU Cycle Coupling

Disk II firmware uses cycle-counted loops to find sync bytes and read/write data. The
emulator must ensure:

- The data shift register advances at the correct bit rate relative to CPU cycles.
- Reads from `$C0SC-$C0SF` return the correct bit/byte at that instant.
- Write loops deliver bytes at the expected cadence, or data will be corrupted.
- Interrupts during disk I/O should be avoided or masked to keep timing stable.

### 6.3 Motor Spin-Up and Settling

- After MOTORON, firmware often waits ~1 second before attempting reads.
- Switching drives or stepping tracks should add a small settling delay.

### 6.4 Performance Strategies

- Tie disk bitstream advancement to emulator timekeeping (event scheduler) rather than
  per-cycle polling to avoid excessive overhead.
- For high-accuracy modes, allow per-cycle stepping or a fast path for boot ROM timing.

---

## 7. Integration with the Emulator Core

### 7.1 Device Registration

- Implement as a slot device in `BadMango.Emulator.Devices` with a slot-aware constructor.
- Default slot configuration should prefer slot 6 while remaining configurable.
- Register with `IDeviceRegistry` so the UI/debug tools can display drive state.
- Expose a `SlotIOHandlers` map for `$C0S0-$C0SF` and register it with the `IMemoryBus`.

### 7.2 ROM Mapping

- Add a ROM target at `$CS00-$CSFF` via the bus layer (consistent with other slot ROMs).
- The ROM target should be layered with appropriate priority so banked memory rules
  (language card) remain correct.

### 7.3 Disk Image Providers

- Use (or extend) the unified block device backing API for sector-based formats.
- Provide a nibble/bitstream adapter for `.nib` and `.woz`, with shared disk metadata
  (track count, write-protect, drive type, track length).

### 7.4 Event Scheduling

- Integrate with the event/timing infrastructure in `BadMango.Emulator.Infrastructure`
  to schedule rotational position updates and motor timeouts.
- Avoid polling per CPU cycle by using event-driven transitions for performance.

---

## 8. Edge Cases and Error Handling

- **No disk inserted**: Reads return `0xFF` or floating bus; write attempts ignored.
- **Out-of-range track**: Clamp or ignore head movement beyond track 0-34.
- **Half/quarter track access**: Preserve quarter-track positions for protected disks.
- **Drive select changes mid-track**: Reset read state and apply a settle delay.
- **Motor off during read**: Stop advancing the shift register immediately.
- **Invalid image size**: Reject at load time with a clear error message.
- **Disk change**: Reset internal nibble/sector cache when a disk is swapped.

---

## 9. References

- Apple Disk II Floppy Disk Subsystem Manual: https://mirrors.apple2.org.za/Apple%20II%20Documentation%20Project/Peripherals/Disk%20Drives/Apple%20Disk%20II/Manuals/Apple%20Disk%20II%20Floppy%20Disk%20Subsystem%20-%20Installation%20and%20Operating%20Manual.pdf
- Disk II ROM disassembly (C600ROM): https://6502disassembly.com/a2-rom/C600ROM.html
- Apple II ROM disassembly overview: https://6502disassembly.com/a2-rom/
- Disk II controller hardware overview: https://www.bigmessowires.com/2021/11/12/the-amazing-disk-ii-controller-card/
- Software Control of the Disk II or IWM Controller (1984): https://mirrors.apple2.org.za/ftp.apple.asimov.net/documentation/hardware/storage/disks/Software_Control_of_the_Disk_II_or_IWM_Controller_1984-04-26.pdf
- Beneath Apple DOS (Archive.org): https://archive.org/details/Beneath_Apple_DOS_alt/
- DOS 3.3 filesystem notes (sector order, VTOC): https://ciderpress2.com/formatdoc/DOS-notes.html
- Nibble image format notes: https://ciderpress2.com/formatdoc/Nibble-notes.html
- WOZ specification (WOZ1/WOZ2): https://applesaucefdc.com/woz/reference2/
- WOZ format notes: https://ciderpress2.com/formatdoc/Woz-notes.html
- 2MG (2IMG) format notes: https://ciderpress2.com/formatdoc/TwoIMG-notes.html

---

## 10. Intellectual Property and Licensing Notes

- Apple II ROMs and Disk II firmware are copyrighted by Apple. The emulator **must not**
  ship those ROMs. Users must supply legally obtained ROM images.
- ROM disassemblies and documentation linked above are provided for reference; ensure
  that any embedded code or bytes are not copied into the repository.
- The WOZ specification is explicitly placed in the public domain by its author; confirm
  licensing for any third-party libraries or tools used to parse WOZ images.
