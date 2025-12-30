# Emulator Architecture Specification v1.0 (Part 1)

## Executive Summary

This document consolidates the complete architectural vision for the Back Pocket BASIC emulator framework, from the immediate **Pocket2e** (Apple IIe-class) target through the speculative **PocketME** (65832-based "Maximum Effort" machine). It serves as the single source of truth for all design decisions, providing a stable foundation for Issue #51 (Page-Based Bus Architecture) and all subsequent work.

---

## Table of Contents

### Part 1: Foundation (this document)
- [Part I: Project Hierarchy & Target Machines](#part-i-project-hierarchy--target-machines)
  - [1.1 Machine Taxonomy](#11-machine-taxonomy)
  - [1.2 Compatibility Layers](#12-compatibility-layers)
  - [1.3 Historical Context & Hardware Understanding](#13-historical-context--hardware-understanding)
- [Part II: CPU Architecture](#part-ii-cpu-architecture)
  - [2.1 CPU Family Inheritance](#21-cpu-family-inheritance)
  - [2.2 Register Models](#22-register-models)
- [Part III: Memory & MMU Architecture](#part-iii-memory--mmu-architecture)
  - [3.1 Address Space Models](#31-address-space-models)
  - [3.2 Page Table Entry Format (65832/PocketME)](#32-page-table-entry-format-65832pocketme)
  - [3.3 Device Page Classes (65832)](#33-device-page-classes-65832)
  - [3.4 Physical Memory Map (PocketME)](#34-physical-memory-map-pocketme)
  - [3.5 Fault Status Codes](#35-fault-status-codes)
- [Part IV: Main Bus Architecture](#part-iv-main-bus-architecture)
  - [4.1 Design Philosophy](#41-design-philosophy)
  - [4.2 Access Context](#42-access-context)
  - [4.3 Fault Model](#43-fault-model)
  - [4.4 Bus Result Types](#44-bus-result-types)
  - [4.5 Page Table Structure](#45-page-table-structure)
  - [4.6 Bus Target Interface](#46-bus-target-interface)
  - [4.7 Memory Bus Interface](#47-memory-bus-interface)
  - [4.8 Main Bus Implementation](#48-main-bus-implementation)
  - [4.9 Access Policy Rules](#49-access-policy-rules)
  - [4.10 CPU Fault Handling Pattern](#410-cpu-fault-handling-pattern)

### [Part 2: Systems & Devices](Architecture%20Spec%20v1.0%20Part%202.md)
- Part V: Signal Bus (Interrupts & Control Lines)
  - 5.1 Signal Model
- Part VI: Scheduler & Timing
  - 6.1 Core Concepts
  - 6.2 Scheduler Interface
  - 6.3 CPU-Scheduler Integration
- Part VII: Device & Peripheral Architecture
  - 7.1 Peripheral Interface
  - 7.2 Slot Manager (Apple II Peripheral Bus)
  - 7.3 Apple II Memory Map
  - 7.4 Soft Switch Handler (Composite Device Page)
  - 7.5 Machine Interface
  - 7.6 CPU Construction Pattern
  - 7.7 Device Initialization Order
- Part VIII: Compatibility Personalities (65832)
  - 8.1 Compatibility Context
  - 8.2 Device Page Object Model
  - 8.3 ROM Routine Interception (Trap Handlers)

### [Part 3: Implementation & Standards](Architecture%20Spec%20v1.0%20Part%203.md)
- Part IX: Observability & Tracing
  - 9.1 Trace Events
  - 9.2 Device Registry
- Part X: Boot & Reset Sequence
  - 10.1 Pocket2e Boot Sequence
  - 10.2 PocketGS Boot Sequence
  - 10.3 PocketME Boot Sequence
  - 10.4 Boot Handoff Structure
- Part XI: Implementation Roadmap
  - Phase 1: Foundation
  - Phase 2: Apple II Integration
  - Phase 3: Devices & Scheduler
  - Phase 4: Observability & Debug
  - Phase 5: 65C816 Extension
  - Phase 6: 65832 Fantasy CPU
- Part XII: Coding Standards & Conventions
  - 12.1 Naming Conventions
  - 12.2 Performance Guidelines
  - 12.3 Testing Strategy

### [Appendix: Quick Reference](Architecture%20Spec%20v1.0%20Appendix.md)
- A.1 Key Types Summary
- A.2 Memory Map Quick Reference
  - Pocket2e (64KB / 128KB)
  - PocketGS (16MB - Apple IIgs / 65C816)
  - PocketME (4GB - 65832)
- A.3 Soft Switch Summary (Pocket2e)
  - Keyboard, Cassette, Speaker, Utility Strobes
  - Graphics Mode Switches, Annunciators
  - Pushbutton / Joystick Inputs, Paddle Trigger
  - Language Card / Bank Switching
  - Slot I/O Space
  - Apple IIe Auxiliary Memory Switches
  - Apple IIe Status Reads
- B.1 Overview (Machine Building)
- B.2 Complete Build Example
- B.3 Machine Interface and Reset Sequence
- B.4 Usage Example
- B.5 Identified Specification Gaps

---

## Part I: Project Hierarchy & Target Machines

### 1.1 Machine Taxonomy

| Machine      | CPU    | Address Space | Target Fidelity    | Status         |
| ------------ | ------ | ------------- | ------------------ | -------------- |
| **Pocket2e** | 65C02  | 128KB         | Apple IIe Enhanced | Primary target |
| **PocketGS** | 65C816 | 16MB          | Apple IIgs         | Future         |
| **PocketME** | 65832  | 4GB           | Speculative modern | Vision target  |

### 1.2 Compatibility Layers

```
┌─────────────────────────────────────────────────────────┐
│                    PocketME (65832)                     │
│  ┌─────────────────────────────────────────────────┐    │
│  │              PocketGS (65C816)                  │    │
│  │  ┌─────────────────────────────────────────────┐    │    │
│  │  │     Pocket2e/2c (65C02)                 │    │    │
│  │  │  ┌───────────────────────────────────┐  │    │    │
│  │  │  │     6502 Emulation Mode           │  │    │    │
│  │  │  └───────────────────────────────────┘  │    │    │
│  │  └─────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

### 1.3 Historical Context & Hardware Understanding

Understanding the Apple II's hardware architecture is essential for implementing an accurate emulator.
The design decisions in the original hardware directly influence how we structure the emulator.

#### 1.3.1 Why Memory-Mapped I/O?

The 6502 processor family has no separate I/O instructions (unlike x86's `IN`/`OUT`). All device
communication happens through memory addresses. When the CPU reads from address $C030, it's not
reading RAM—it's triggering the speaker hardware. This is called **memory-mapped I/O (MMIO)**.

The Apple II dedicates the address range $C000-$CFFF to I/O operations. Any access to this region
doesn't touch RAM; instead, it communicates with hardware devices. This design has implications
for emulation:

- **Side effects on read**: Reading $C030 toggles the speaker. Reading $C000 returns the keyboard
  data. These reads must trigger device behavior, not just return stored values.
- **Write-only switches**: Some addresses respond to writes but return garbage on reads.
- **Strobe addresses**: Some addresses respond to any access (read or write) the same way.

#### 1.3.2 The "Soft Switch" Concept

Many Apple II hardware states are controlled by **soft switches**—memory-mapped flip-flops that
toggle between states. Unlike hardware DIP switches that require physical manipulation, soft
switches change state through software memory accesses.

For example, the text/graphics mode toggle:
- Access $C050: Set graphics mode (TEXT off)
- Access $C051: Set text mode (TEXT on)

The actual access type (read vs. write) often doesn't matter—just touching the address changes
the state. This is why the spec marks many soft switches as "R/W" (any access works).

**Emulation impact**: The bus must invoke device handlers even for reads that don't "return"
meaningful data. A read from $C050 returns a floating bus value, but the read itself has the
side effect of switching to graphics mode.

#### 1.3.3 The Floating Bus

When the Apple II CPU reads from an address with no connected device (or a device that doesn't
drive the data bus), the result is unpredictable—typically the last value that happened to be
on the data bus. This is called the **floating bus**.

In practice, the floating bus often contains video-related data because the video circuitry is
constantly reading memory to generate the display. Some software exploits this for copy protection
or timing tricks.

**Emulation approaches**:
1. **Simple**: Return $FF or $00 for unmapped reads
2. **Moderate**: Return a consistent "last value" per access
3. **Accurate**: Emulate the actual video fetch pattern (cycle-accurate emulation)

For Pocket2e, we choose option 1 or 2 by default, with option 3 available for strict compatibility.

#### 1.3.4 Why 7 Slots?

The Apple II has 7 expansion slots (numbered 1-7, with slot 0 being a special internal slot on
some models). This wasn't arbitrary—it was the maximum that could be addressed with the available
address space allocation:

- Each slot gets 16 bytes of I/O space: $C080 + (slot × 16) = $C090, $C0A0, ..., $C0F0
- Each slot gets 256 bytes of ROM space: $C100 + (slot × 256) = $C100, $C200, ..., $C700
- Slots 1-7 share one 2KB expansion ROM area: $C800-$CFFF

The addressing scheme means:
- Slot 1 I/O: $C090-$C09F, ROM: $C100-$C1FF
- Slot 6 I/O: $C0E0-$C0EF, ROM: $C600-$C6FF
- Any slot's expansion ROM: $C800-$CFFF (one at a time)

This hierarchical allocation enables a consistent device discovery protocol that software can use
to probe for installed cards.

---

## Part II:  CPU Architecture

### 2.1 CPU Family Inheritance

```csharp
public interface ICpu
{
    CpuFamily Family { get; }
    ArchitecturalMode CurrentMode { get; }
    bool Halted { get; }
    ulong CycleCount { get; }
    
    CpuStepResult Step();
    void Reset();
    void SignalIRQ();
    void SignalNMI();
}

public enum CpuFamily
{
    Cpu65C02,     // WDC 65C02 (Pocket2e target)
    Cpu65C816,    // WDC 65C816 (PocketGS target)
    Cpu65832      // Speculative 32-bit (PocketME target)
}
```

### 2.2 Register Models

#### Design Philosophy: Unified Registers with Multi-Size Views

Instead of separate register structs for each CPU variant (65C02, 65C816, 65832), the emulator uses a **single unified `Registers` struct** with maximum-sized registers (`uint`/32-bit). The instruction and addressing handlers determine the effective register width at runtime based on:

- **E flag** (Emulation): `true` = 65C02-compatible 8-bit mode
- **CP flag** (Compatibility): `true` = compatibility mode, `false` = 65832 native
- **M flag** (in P register): Memory/Accumulator width in 65816 native mode
- **X flag** (in P register): Index register width in 65816 native mode

This approach provides several benefits:
- Single codebase handles all CPU variants
- No combinatorial explosion of instruction implementations
- Runtime mode switching without register conversion
- x86-style register views (RAX/EAX/AX/AL pattern)

#### Unified Registers Structure

```csharp
/// <summary>
/// Represents the CPU's register set for all CPU variants (65C02, 65C816, 65832).
/// Registers use maximum size (32-bit) with multi-size view accessors.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Registers
{
    // ─── Classic Registers (Multi-Size Views) ───────────────────────────
    public readonly DWord ZR;           // Constant zero register
    public RegisterAccumulator A;       // Accumulator (8/16/32-bit views)
    public RegisterIndex X;             // X Index (8/16/32-bit views)
    public RegisterIndex Y;             // Y Index (8/16/32-bit views)
    public RegisterDirectPage D;        // Direct Page (16/32-bit views)
    public RegisterStackPointer SP;     // Stack Pointer (8/16/32-bit views)
    public RegisterProgramCounter PC;   // Program Counter (16/32-bit views)
    
    // ─── Status and Mode Flags ──────────────────────────────────────────
    public ProcessorStatusFlags P;      // Processor Status (N V M X D I Z C)
    public bool E;                      // Emulation flag (65C02 mode when true)
    public bool CP;                     // Compatibility mode flag
    
    // ─── Bank Registers (65C816+) ───────────────────────────────────────
    public byte DBR;                    // Data Bank Register
    public byte PBR;                    // Program Bank Register
    
    // ─── General Purpose Registers (65832 only) ─────────────────────────
    public DWord R0, R1, R2, R3, R4, R5, R6, R7;
    // R7 serves as Frame Pointer (FP) by calling convention
    
    // ─── System/Privileged Registers (65832 K-mode only) ────────────────
    public SystemRegisters System;
}

/// <summary>
/// System registers for 65832 privileged operations.
/// Only accessible in Kernel or Hypervisor modes.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SystemRegisters
{
    public DWord CR0;   // Control: PG, NXE, privilege level, ASID
    public DWord PTBR;  // Page Table Base Register (physical)
    public DWord VBAR;  // Vector Base Address Register
    public DWord FAR;   // Fault Address Register
    public DWord FSC;   // Fault Status Code
    public DWord TLS;   // Thread-Local Storage pointer
}
```

#### Register View Types

Each register type provides multiple size views through embedded struct methods:

```csharp
/// <summary>Accumulator with 8/16/32-bit views.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterAccumulator
{
    public DWord acc;   // Full 32-bit storage
    
    // Size-specific accessors
    public readonly byte GetByte() => (byte)(acc & 0xFF);
    public readonly Word GetWord() => (Word)(acc & 0xFFFF);
    public readonly DWord GetDWord() => acc;
    
    public void SetByte(byte value) => acc = (acc & 0xFFFFFF00) | value;
    public void SetWord(Word value) => acc = (acc & 0xFFFF0000) | value;
    public void SetDWord(DWord value) => acc = value;
}

/// <summary>Index register (X, Y) with 8/16/32-bit views.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterIndex
{
    public DWord index;
    
    public readonly byte GetByte() => (byte)(index & 0xFF);
    public readonly Word GetWord() => (Word)(index & 0xFFFF);
    public readonly DWord GetDWord() => index;
    
    public void SetByte(byte value) => index = (index & 0xFFFFFF00) | value;
    public void SetWord(Word value) => index = (index & 0xFFFF0000) | value;
    public void SetDWord(DWord value) => index = value;
}

/// <summary>Stack Pointer with 8/16/32-bit views.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterStackPointer
{
    public DWord stack;
    
    public readonly byte GetByte() => (byte)(stack & 0xFF);
    public readonly Word GetWord() => (Word)(stack & 0xFFFF);
    public readonly DWord GetDWord() => stack;
    
    public void SetByte(byte value) => stack = (stack & 0xFFFFFF00) | value;
    public void SetWord(Word value) => stack = (stack & 0xFFFF0000) | value;
    public void SetDWord(DWord value) => stack = value;
}

/// <summary>Direct Page register with 16/32-bit views.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterDirectPage
{
    public Addr direct;
    
    public readonly Word GetWord() => (Word)(direct & 0xFFFF);
    public readonly Addr GetAddr() => direct;
    
    public void SetWord(Word value) => direct = (direct & 0xFFFF0000) | value;
    public void SetAddr(Addr value) => direct = value;
}

/// <summary>Program Counter with 16/32-bit views and bank support.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterProgramCounter
{
    public Addr addr;
    
    public readonly Addr GetAddr() => addr;
    public readonly Word GetWord() => (Word)(addr & 0xFFFF);
    public readonly byte GetBank() => (byte)((addr >> 16) & 0xFF);
    
    public void SetWord(Word value) => addr = (addr & 0xFFFF0000) | value;
    public void SetBank(byte value) => addr = (addr & 0x00FFFFFF) | ((Addr)value << 16);
    public void SetBankAndWord(byte bank, Word value) => addr = ((Addr)bank << 16) | value;
    public void SetAddr(Addr value) => addr = value;
    
    public void Advance() => addr++;
    public void Advance(int count) => addr = (Addr)(addr + count);
}
```

#### Mode Detection Helpers

Extension methods determine effective register sizes at runtime:

```csharp
extension(ref Registers registers)
{
    /// <summary>Gets the architectural mode based on CP and E flags.</summary>
    public ArchitecturalMode GetArchitecturalMode() => registers switch
    {
        { CP: true, E: true }  => ArchitecturalMode.Mode65C02,
        { CP: true, E: false } => ArchitecturalMode.Mode65816,
        _                      => ArchitecturalMode.Mode65832,
    };
    
    /// <summary>Gets effective accumulator size (8, 16, or 32 bits).</summary>
    public byte GetAccumulatorSize()
    {
        if (registers.Is65C02Mode()) return 8;
        if (registers.Is65816Mode()) 
            return registers.P.IsMemorySize8Bit() ? (byte)8 : (byte)16;
        return 32; // 65832 mode
    }
    
    /// <summary>Gets effective index register size (8, 16, or 32 bits).</summary>
    public byte GetIndexSize()
    {
        if (registers.Is65C02Mode()) return 8;
        if (registers.Is65816Mode())
            return registers.P.IsIndexSize8Bit() ? (byte)8 : (byte)16;
        return 32; // 65832 mode
    }
    
    public bool Is65C02Mode() => registers is { CP: true, E: true };
    public bool Is65816Mode() => registers is { CP: true, E: false };
    public bool Is65832Mode() => registers is { CP: false };
}
```

#### Mode Behavior Summary

| Mode     | E   | CP    | A Size | X/Y Size | SP Size | PC Size | D Range | Banks |
|----------|-----|-------|--------|----------|---------|---------|---------|-------|
| 65C02    | 1   | 1     | 8-bit  | 8-bit    | 8-bit   | 16-bit  | $00     | N/A   |
| 65816 E  | 1   | 1     | 8-bit  | 8-bit    | 8-bit*  | 16-bit  | 16-bit  | DBR/PBR |
| 65816 N  | 0   | 1     | M flag | X flag   | 16-bit  | 16-bit  | 16-bit  | DBR/PBR |
| 65832    | 0   | 0     | 32-bit | 32-bit   | 32-bit  | 32-bit  | 32-bit  | N/A   |

*65816 emulation mode forces SP high byte to $01

#### Initialization Examples

```csharp
// 65C02 compatibility mode (Pocket2e boot)
var registers = new Registers(compat: true, resetVector: 0xFFFC);
// Sets: E=true, CP=true, SP=$01FF, P=Reset flags

// 65832 native mode (PocketME boot)
var registers = new Registers();
// Sets: E=false, CP=false, all registers at 32-bit width
```

#### Usage in Instructions

Instructions use mode-aware accessors for portable implementation:

```csharp
// LDA implementation works for all CPU modes
public static OpcodeHandler LDA(AddressingMode<CpuState> mode)
{
    return (memory, ref state) =>
    {
        Addr address = mode(memory, ref state);
        byte size = state.Registers.GetAccumulatorSize();  // 8, 16, or 32
        var value = memory.ReadValue(address, size);
        
        state.Registers.P.SetZeroAndNegative(value, size);
        state.Registers.A.SetValue(value, size);
    };
}
```

#### Privilege Levels (65832)

```csharp
public enum PrivilegeLevel : byte
{
    User = 0,       // U: Unprivileged code
    Kernel = 1,     // K: OS kernel
    Hypervisor = 2  // H: Reserved for future
}
```

The privilege level is encoded in `CR0` bits 0-1. System registers are only accessible in Kernel or Hypervisor mode; User-mode access triggers a privilege violation trap.

---

## Part III:  Memory & MMU Architecture

### 3.1 Address Space Models

| Machine  | Virtual Bits | Physical Bits | Page Size | Page Table     |
| -------- | ------------ | ------------- | --------- | -------------- |
| Pocket2e | 16           | 16 (128KB)    | N/A       | Soft switches  |
| PocketGS | 24           | 24 (16MB)     | N/A       | Mega II + FPI  |
| PocketME | 32           | 32 (4GB)      | 4KB       | 2-level tables |

### 3.2 Page Table Entry Format (65832/PocketME)

From privileged spec v0.6:

```csharp
[Flags]
public enum PageTableEntryFlags : uint
{
    Present     = 1 << 0,   // P: Page is valid
    Readable    = 1 << 1,   // R: Read allowed
    Writable    = 1 << 2,   // W: Write allowed
    Executable  = 1 << 3,   // X: Execute allowed
    User        = 1 << 4,   // U: User-mode accessible
    Accessed    = 1 << 5,   // A: Has been read (optional HW)
    Dirty       = 1 << 6,   // D: Has been written (optional HW)
    Global      = 1 << 7,   // G:  Ignore ASID for TLB
    Device      = 1 << 8,   // DEV: Device page (not RAM)
    
    // Bits 9-11 reserved for future attributes
    // Bits 12-31:  Physical Frame Number (or Device Page ID if DEV=1)
}

public readonly record struct PageTableEntry(uint Raw)
{
    public bool Present => (Raw & 0x001) != 0;
    public bool Readable => (Raw & 0x002) != 0;
    public bool Writable => (Raw & 0x004) != 0;
    public bool Executable => (Raw & 0x008) != 0;
    public bool User => (Raw & 0x010) != 0;
    public bool Device => (Raw & 0x100) != 0;
    
    public uint PhysicalFrameNumber => Raw >> 12;
    
    // When DEV=1, PFN is interpreted as Device Page ID: 
    // Bits [19:16] = Class (4 bits, 16 classes)
    // Bits [15:8]  = Instance (8 bits, 256 per class)
    // Bits [7:0]   = Page (8 bits, 256 per instance)
    public byte DeviceClass => (byte)((Raw >> 28) & 0xF);
    public byte DeviceInstance => (byte)((Raw >> 20) & 0xFF);
    public byte DevicePage => (byte)((Raw >> 12) & 0xFF);
}
```

### 3.3 Device Page Classes (65832)

```csharp
public enum DevicePageClass : byte
{
    Invalid = 0x0,
    CompatIO = 0x1,       // Apple II $C000-$CFFF I/O page
    SlotROM = 0x2,        // Expansion ROM windows
    Framebuffer = 0x3,    // Video aperture
    Storage = 0x4,        // Disk controller MMIO
    Network = 0x5,        // Network controller
    Timer = 0x6,          // Timer/interrupt controller
    Debug = 0x7,          // Semihosting/debug console
    // 0x8-0xF reserved
}
```

### 3.4 Physical Memory Map (PocketME)

From privileged spec v0.4:

```
0x0000_0000 - 0x0003_FFFF :  Boot ROM (256KB)
0x0004_0000 - ...          : RAM begins
... 
0xFFFC_0000 - 0xFFFF_FFFF :  High ROM alias (256KB mirror)
```

### 3.5 Fault Status Codes

```csharp
public enum FaultStatusCode : uint
{
    NotPresent = 0,
    ReadViolation = 1,
    WriteViolation = 2,
    ExecuteViolation = 3,
    PrivilegeViolation = 4,
    ReservedBitViolation = 5,
    DeviceFault = 6  // DEV page unmapped or device rejected access
}
```

---

## Part IV: Main Bus Architecture

This is the core of Issue #51 and the foundation for all device interaction.

### 4.1 Design Philosophy

From "Main bus architecture.md":

> The bus that's fast on the hot path, but still rich in introspection and cross-talk.

**Three Planes:**
1. **Data Plane**: O(1) page lookup, direct dispatch, minimal overhead
2. **Control Plane**: Device registration, bank switching, configuration
3. **Observability Plane**: Tracing, watchpoints, debugging (zero cost when disabled)

**Fault Philosophy:**
- The bus never silently fixes faults
- The CPU never guesses why an access failed
- Faults are first-class return values, not exceptions
- Permission checks happen before touching devices

### 4.2 Access Context

Every bus operation carries complete context:

```csharp
/// <summary>
/// Defines the bus access semantics for memory operations.
/// </summary>
/// <remarks>
/// This determines whether the bus prefers atomic wide operations or
/// decomposes multi-byte accesses into individual byte cycles.
/// </remarks>
public enum BusAccessMode : byte
{
    /// <summary>
    /// Native mode: prefers atomic wide operations when the target supports them.
    /// </summary>
    /// <remarks>
    /// Used by 65832 native mode for better performance with modern memory.
    /// </remarks>
    Atomic = 0,

    /// <summary>
    /// Compatibility mode: decomposes wide accesses into byte-wise cycles.
    /// </summary>
    /// <remarks>
    /// Matches 65C02/65816 expectations where peripherals observe individual
    /// memory access cycles. Required for accurate emulation of devices
    /// that depend on seeing each byte access separately.
    /// </remarks>
    Decomposed = 1,
}

/// <summary>
/// Complete context for a single bus access.
/// The CPU computes intent; the bus enforces consequences.
/// </summary>
public readonly record struct BusAccess(
    Addr Address,           // Virtual address being accessed
    DWord Value,            // Write payload (low bits used by Width)
    byte WidthBits,         // 8, 16, or 32 (effective width after E/M/X)
    BusAccessMode Mode,     // Atomic or Decomposed
    bool EmulationE,        // E flag state (Compat mode only)
    AccessIntent Intent,    // Data/Fetch/Debug/DMA
    int SourceId,           // Who initiated (CPU=0, DMA channels, debugger)
    ulong Cycle,            // Current machine cycle
    AccessFlags Flags       // Atomic/Decompose/NoSideFx/etc.
);

public enum AccessIntent : byte
{
    DataRead = 0,
    DataWrite = 1,
    InstructionFetch = 2,
    DebugRead = 3,      // Peek: no side effects
    DebugWrite = 4,     // Poke: no side effects (rare)
    DmaRead = 5,
    DmaWrite = 6
}

[Flags]
public enum AccessFlags : uint
{
    None         = 0,
    NoSideFx     = 1 << 0,  // Peek/Poke semantics
    LittleEndian = 1 << 1,  // (always true for 65xx)
    Atomic       = 1 << 2,  // Request atomic wide op
    Decompose    = 1 << 3   // Force byte-wise cycles
}
```

### 4.3 Fault Model

Faults are first-class return values from bus operations. The bus never silently recovers from errors—the CPU receives full information about what happened and can translate it into its architecture's exception/abort mechanism.

```csharp
/// <summary>
/// Types of faults that can occur during bus operations.
/// </summary>
public enum FaultKind : byte
{
    None = 0,           // Success
    Unmapped,           // Address has no page entry or maps to a hole
    Permission,         // Read/write permission denied
    Nx,                 // Execute permission denied on instruction fetch
    Misaligned,         // Alignment violation for atomic wide ops
    DeviceFault         // Device signaled an error
}

/// <summary>
/// Complete fault information for bus operations.
/// Carries exactly what tooling and exception logic need without allocations.
/// </summary>
public readonly record struct BusFault(
    FaultKind Kind,
    Addr Address,
    byte WidthBits,
    AccessIntent Intent,
    CpuMode Mode,
    int SourceId,
    int DeviceId,       // -1 if unmapped
    RegionTag RegionTag,
    ulong Cycle)
{
    public bool IsSuccess => Kind == FaultKind.None;
    public bool IsFault => Kind != FaultKind.None;
    
    // Factory methods for common fault types
    public static BusFault Success() => new(...);
    public static BusFault Success(in BusAccess access, int deviceId, RegionTag tag) => new(...);
    public static BusFault Unmapped(in BusAccess access) => new(...);
    public static BusFault PermissionDenied(in BusAccess access, int deviceId, RegionTag tag) => new(...);
    public static BusFault NoExecute(in BusAccess access, int deviceId, RegionTag tag) => new(...);
    public static BusFault Misaligned(in BusAccess access, int deviceId, RegionTag tag) => new(...);
    public static BusFault Device(in BusAccess access, int deviceId, RegionTag tag) => new(...);
}
```

### 4.4 Bus Result Types

Bus operations return result types that encapsulate either success or fault information:

```csharp
/// <summary>
/// Result of a bus read operation, containing either the value or a fault.
/// </summary>
/// <remarks>
/// Using try-style APIs with BusResult keeps faults cheap and predictable
/// in the hot path. A page permission check becomes a couple of branches,
/// not a thrown exception that murders performance and obscures control flow.
/// </remarks>
public readonly record struct BusResult<T>(T Value, BusFault Fault, ulong Cycles = 0)
    where T : struct
{
    public bool Ok => Fault.Kind == FaultKind.None;
    public bool Failed => Fault.Kind != FaultKind.None;
    
    public static implicit operator BusResult<T>(BusFault fault) => FromFault(fault);
    
    public static BusResult<T> Success(T value, ulong cycles = 0) => new(...);
    public static BusResult<T> Success(T value, in BusAccess access, int deviceId, RegionTag tag, ulong cycles = 0) => new(...);
    public static BusResult<T> FromFault(BusFault fault, ulong cycles = 0) => new(...);
}

/// <summary>
/// Result of a bus write operation (no value, just fault status).
/// </summary>
public readonly record struct BusResult(BusFault Fault, ulong Cycles = 0)
{
    public bool Ok => Fault.Kind == FaultKind.None;
    public bool Failed => Fault.Kind != FaultKind.None;
    
    public static implicit operator BusResult(BusFault fault) => FromFault(fault);
    
    public static BusResult Success(ulong cycles = 0) => new(...);
    public static BusResult Success(in BusAccess access, int deviceId, RegionTag tag, ulong cycles = 0) => new(...);
    public static BusResult FromFault(BusFault fault, ulong cycles = 0) => new(...);
}
```

### 4.5 Page Table Structure

```csharp
/// <summary>
/// Capabilities advertised by a bus target.
/// </summary>
[Flags]
public enum TargetCaps : uint
{
    None          = 0,
    SupportsPeek  = 1 << 0,  // Can handle NoSideFx reads safely
    SupportsWide  = 1 << 1,  // Can do atomic 16/32-bit ops
    SideEffects   = 1 << 2,  // Reads/writes mutate state
    TimingSense   = 1 << 3   // Behavior depends on cycle timing
}

/// <summary>
/// Page permissions for access control.
/// </summary>
[Flags]
public enum PagePerms : byte
{
    None    = 0,
    Read    = 1 << 0,  // R: Data reads allowed
    Write   = 1 << 1,  // W: Writes allowed
    Execute = 1 << 2,  // X: Instruction fetch allowed
    
    // Common combinations
    RW  = Read | Write,
    RX  = Read | Execute,
    RWX = Read | Write | Execute
}

/// <summary>
/// A single 4KB page table entry.
/// </summary>
public readonly record struct PageEntry(
    int DeviceId,           // Structural instance ID
    RegionTag RegionTag,    // Classification for tooling
    PagePerms Perms,        // Access permissions
    TargetCaps Caps,        // What this target supports
    IBusTarget Target,      // The actual handler
    Addr PhysBase           // Base address within the device
);

/// <summary>
/// Region tags for observability and debugging.
/// </summary>
public enum RegionTag : ushort
{
    Unknown = 0,
    MainRAM = 1,
    AuxRAM = 2,
    ROM = 3,
    IOPage = 4,
    SlotROM = 5,
    ExpansionRAM = 6,
    Unmapped = 0xFFFF
}
```

### 4.6 Bus Target Interface

```csharp
/// <summary>
/// Any device that can respond to bus reads/writes.
/// </summary>
public interface IBusTarget
{
    /// <summary>Gets the capabilities of this bus target.</summary>
    TargetCaps Capabilities { get; }
    
    /// <summary>8-bit read (always required).</summary>
    byte Read8(Addr physicalAddress, in BusAccess access);
    
    /// <summary>8-bit write (always required).</summary>
    void Write8(Addr physicalAddress, byte value, in BusAccess access);
    
    /// <summary>16-bit read (optional; bus decomposes if not implemented).</summary>
    Word Read16(Addr physicalAddress, in BusAccess access)
    {
        byte low = Read8(physicalAddress, access);
        byte high = Read8(physicalAddress + 1, access.WithAddressOffset(1));
        return (Word)(low | (high << 8));
    }
    
    /// <summary>16-bit write (optional).</summary>
    void Write16(Addr physicalAddress, Word value, in BusAccess access)
    {
        Write8(physicalAddress, (byte)value, access);
        Write8(physicalAddress + 1, (byte)(value >> 8), access.WithAddressOffset(1));
    }
    
    /// <summary>32-bit read (optional).</summary>
    DWord Read32(Addr physicalAddress, in BusAccess access)
    {
        byte b0 = Read8(physicalAddress, access);
        byte b1 = Read8(physicalAddress + 1, access.WithAddressOffset(1));
        byte b2 = Read8(physicalAddress + 2, access.WithAddressOffset(2));
        byte b3 = Read8(physicalAddress + 3, access.WithAddressOffset(3));
        return (DWord)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));
    }
    
    /// <summary>32-bit write (optional).</summary>
    void Write32(Addr physicalAddress, DWord value, in BusAccess access)
    {
        Write8(physicalAddress, (byte)value, access);
        Write8(physicalAddress + 1, (byte)(value >> 8), access.WithAddressOffset(1));
        Write8(physicalAddress + 2, (byte)(value >> 16), access.WithAddressOffset(2));
        Write8(physicalAddress + 3, (byte)(value >> 24), access.WithAddressOffset(3));
    }
}

/// <summary>
/// A composite bus target that dispatches to sub-targets based on address offset.
/// Used for pages that contain multiple logical regions (e.g., Apple II I/O page).
/// </summary>
/// <remarks>
/// <para>
/// The Apple II I/O page ($C000-$CFFF) contains multiple sub-regions:
/// </para>
/// <list type="bullet">
/// <item><description>$C000-$C0FF: Soft switches</description></item>
/// <item><description>$C100-$C7FF: Slot ROM ($Cn00 for slot n)</description></item>
/// <item><description>$C800-$CFFF: Expansion ROM (selected slot)</description></item>
/// </list>
/// <para>
/// Rather than splitting into sub-pages (which would complicate 4KB page granularity),
/// a composite target handles the internal dispatch.
/// </para>
/// </remarks>
public interface ICompositeTarget : IBusTarget
{
    /// <summary>
    /// Resolves the actual target for a given offset within the page.
    /// </summary>
    /// <param name="offset">Offset within the 4KB page (0x000-0xFFF).</param>
    /// <param name="intent">Access intent (affects slot ROM visibility rules).</param>
    /// <returns>The target to handle this access, or null for floating bus.</returns>
    IBusTarget? ResolveTarget(Addr offset, AccessIntent intent);
    
    /// <summary>
    /// Gets the sub-region tag for a given offset (for tracing/debugging).
    /// </summary>
    /// <param name="offset">Offset within the 4KB page.</param>
    /// <returns>Region tag for the sub-region.</returns>
    RegionTag GetSubRegionTag(Addr offset);
}
```

### 4.7 Memory Bus Interface

The memory bus provides both direct and try-style APIs:

```csharp
/// <summary>
/// Main memory bus interface for routing CPU and DMA memory operations.
/// </summary>
/// <remarks>
/// The CPU does not own memory; all memory interactions flow through the bus.
/// The CPU computes intent; the bus enforces consequences.
/// </remarks>
public interface IMemoryBus
{
    // ─── Configuration ──────────────────────────────────────────────────
    int PageShift { get; }      // Bits to shift for page index (12 for 4KB)
    Addr PageMask { get; }      // Mask for offset within page (0xFFF)
    int PageCount { get; }      // Total pages in address space
    
    // ─── Direct Access (assumes success, for hot path) ──────────────────
    byte Read8(in BusAccess access);
    void Write8(in BusAccess access, byte value);
    Word Read16(in BusAccess access);
    void Write16(in BusAccess access, Word value);
    DWord Read32(in BusAccess access);
    void Write32(in BusAccess access, DWord value);
    
    // ─── Try-Style Access (returns fault information) ───────────────────
    BusResult<byte> TryRead8(in BusAccess access);
    BusFault TryWrite8(in BusAccess access, byte value);
    BusResult<Word> TryRead16(in BusAccess access);
    BusFault TryWrite16(in BusAccess access, Word value);
    BusResult<DWord> TryRead32(in BusAccess access);
    BusFault TryWrite32(in BusAccess access, DWord value);
    
    // ─── Page Table Management (control plane) ──────────────────────────
    
    /// <summary>Gets the page entry for a given address.</summary>
    PageEntry GetPageEntry(Addr address);
    
    /// <summary>Gets the page entry by index for direct inspection.</summary>
    ref readonly PageEntry GetPageEntryByIndex(int pageIndex);
    
    /// <summary>Maps a single page to a target.</summary>
    void MapPage(int pageIndex, PageEntry entry);
    
    /// <summary>Maps a contiguous range of pages to a target.</summary>
    void MapPageRange(int startPage, int pageCount, int deviceId,
                      RegionTag tag, PagePerms perms, TargetCaps caps,
                      IBusTarget target, Addr physBase);
    
    // ─── Dynamic Remapping (bank switching support) ─────────────────────
    
    /// <summary>
    /// Atomically remaps a page to a different target.
    /// Used for language card and auxiliary memory bank switching.
    /// </summary>
    /// <param name="pageIndex">The page index to remap.</param>
    /// <param name="newTarget">The new target device.</param>
    /// <param name="newPhysBase">The new physical base within the target.</param>
    void RemapPage(int pageIndex, IBusTarget newTarget, Addr newPhysBase);
    
    /// <summary>
    /// Atomically remaps a page with full entry replacement.
    /// </summary>
    /// <param name="pageIndex">The page index to remap.</param>
    /// <param name="newEntry">The complete new page entry.</param>
    void RemapPage(int pageIndex, PageEntry newEntry);
    
    /// <summary>
    /// Remaps a contiguous range of pages.
    /// </summary>
    void RemapPageRange(int startPage, int pageCount, 
                        IBusTarget newTarget, Addr newPhysBase);
}
```

### 4.8 Main Bus Implementation

```csharp
/// <summary>
/// The main memory bus. All CPU memory access flows through here.
/// </summary>
public sealed class MainBus : IMemoryBus
{
    private const int DefaultPageShift = 12;    // 4KB pages
    private const Addr DefaultPageMask = 0xFFF;
    
    private readonly PageEntry[] _pageTable;
    private readonly ITraceSink? _trace;
    private bool _traceEnabled;
    
    public int PageShift => DefaultPageShift;
    public Addr PageMask => DefaultPageMask;
    public int PageCount => _pageTable.Length;
    
    public MainBus(int addressSpaceBits = 16)
    {
        int pageCount = 1 << (addressSpaceBits - DefaultPageShift);
        _pageTable = new PageEntry[pageCount];
    }
    
    // ─── Try-Style Read with Permission Checks ──────────────────────────
    public BusResult<byte> TryRead8(in BusAccess ctx)
    {
        ref readonly var page = ref _pageTable[ctx.Address >> PageShift];
        
        // Check for unmapped page
        if (page.Target is null)
            return BusFault.Unmapped(in ctx);
        
        // Check read permission
        if ((page.Perms & PagePerms.Read) == 0)
            return BusFault.PermissionDenied(in ctx, page.DeviceId, page.RegionTag);
        
        // Check NX on instruction fetch (Native mode only)
        if (ctx.Intent == AccessIntent.InstructionFetch &&
            ctx.Mode == CpuMode.Native &&
            (page.Perms & PagePerms.Execute) == 0)
            return BusFault.NoExecute(in ctx, page.DeviceId, page.RegionTag);
        
        // Perform the read
        Addr phys = page.PhysBase + (ctx.Address & PageMask);
        byte value = page.Target.Read8(phys, ctx);
        
        if (_traceEnabled)
            EmitTrace(ctx, value, page);
        
        return BusResult<byte>.Success(value, in ctx, page.DeviceId, page.RegionTag, cycles: 1);
    }
    
    // ─── Direct Read (hot path, no fault checking) ──────────────────────
    public byte Read8(in BusAccess ctx)
    {
        ref readonly var page = ref _pageTable[ctx.Address >> PageShift];
        Addr phys = page.PhysBase + (ctx.Address & PageMask);
        
        byte value = page.Target.Read8(phys, ctx);
        
        if (_traceEnabled)
            EmitTrace(ctx, value, page);
        
        return value;
    }
    
    // ─── Wide Read with Decomposition ───────────────────────────────────
    public BusResult<Word> TryRead16(in BusAccess ctx)
    {
        // Cross-page check: always decompose
        if (CrossesPage(ctx.Address, 2))
            return DecomposeTryRead16(ctx);
        
        // Decompose flag forces byte-wise
        if ((ctx.Flags & AccessFlags.Decompose) != 0)
            return DecomposeTryRead16(ctx);
        
        ref readonly var page = ref _pageTable[ctx.Address >> PageShift];
        
        // Permission checks
        if (page.Target is null)
            return BusFault.Unmapped(in ctx);
        if ((page.Perms & PagePerms.Read) == 0)
            return BusFault.PermissionDenied(in ctx, page.DeviceId, page.RegionTag);
        
        // Atomic request + target supports it
        if ((ctx.Flags & AccessFlags.Atomic) != 0 &&
            (page.Caps & TargetCaps.SupportsWide) != 0)
        {
            Addr phys = page.PhysBase + (ctx.Address & PageMask);
            Word value = page.Target.Read16(phys, ctx);
            return BusResult<Word>.Success(value, in ctx, page.DeviceId, page.RegionTag, cycles: 1);
        }
        
        // Default: Compat decomposes, Native tries atomic
        if (ctx.Mode == CpuMode.Compat)
            return DecomposeTryRead16(ctx);
        
        // Native mode: use wide if available
        if ((page.Caps & TargetCaps.SupportsWide) != 0)
        {
            Addr phys = page.PhysBase + (ctx.Address & PageMask);
            Word value = page.Target.Read16(phys, ctx);
            return BusResult<Word>.Success(value, in ctx, page.DeviceId, page.RegionTag, cycles: 1);
        }
        
        return DecomposeTryRead16(ctx);
    }
    
    private BusResult<Word> DecomposeTryRead16(in BusAccess ctx)
    {
        var ctx0 = ctx with { WidthBits = 8 };
        var result0 = TryRead8(ctx0);
        if (result0.Failed)
            return BusFault.FromFault(result0.Fault);
        
        var ctx1 = ctx with { Address = ctx.Address + 1, WidthBits = 8 };
        var result1 = TryRead8(ctx1);
        if (result1.Failed)
            return BusFault.FromFault(result1.Fault, cycles: 1); // Partial progress
        
        Word value = (Word)(result0.Value | (result1.Value << 8));
        return BusResult<Word>.Success(value, cycles: result0.Cycles + result1.Cycles);
    }
    
    private bool CrossesPage(Addr addr, int bytes)
        => ((addr & PageMask) + (uint)(bytes - 1)) > PageMask;
    
    // Similar patterns for Write8, Write16, Read32, Write32...
}
```

### 4.9 Access Policy Rules

**The bus decides, based on these ordered rules:**

1. **Unmapped page** → Return `FaultKind.Unmapped`
2. **Permission violation** → Return `FaultKind.Permission`
3. **NX violation on fetch** (Native mode only) → Return `FaultKind.Nx`
4. **Cross-page wide access** → Always decompose
5. **`Decompose` flag set** → Force byte cycles
6. **`Atomic` flag set + `SupportsWide`** → Use wide handler
7. **Compat mode default** → Decompose (Apple II expects byte-visible cycles)
8. **Native mode default** → Atomic when target supports it

**NX Enforcement:**
- Only checked on `AccessIntent.InstructionFetch`
- Only enforced in Native mode (Compat mode ignores NX)
- Data reads on NX pages are allowed unless other permissions forbid it

**Peek/Poke Rules:**
- `DebugRead`/`DebugWrite` intent → Must not trigger side effects
- If target lacks `SupportsPeek` → Return floating bus or debug fault
- RAM/ROM always support Peek; I/O depends on device declaration

### 4.10 CPU Fault Handling Pattern

The CPU uses try-style APIs and translates bus faults to its exception model:

```csharp
// Example: CPU instruction fetch with fault handling
public byte FetchOpcode()
{
    var access = new BusAccess(
        Address: _registers.PC.GetAddr(),
        Value: 0,
        WidthBits: 8,
        Mode: GetCurrentMode(),
        EmulationE: _registers.E,
        Intent: AccessIntent.InstructionFetch,
        SourceId: 0,  // CPU
        Cycle: _cycleCount,
        Flags: AccessFlags.None
    );
    
    var result = _bus.TryRead8(access);
    
    if (result.Failed)
    {
        // Translate bus fault to CPU exception
        switch (result.Fault.Kind)
        {
            case FaultKind.Unmapped:
            case FaultKind.Permission:
                RaiseDataAbort(result.Fault);
                break;
            case FaultKind.Nx:
                RaiseInstructionAbort(result.Fault);
                break;
        }
        return 0; // Unreachable if exception taken
    }
    
    _registers.PC.Advance();
    _cycleCount += result.Cycles;
    return result.Value;
}
```

---

## Document History

| Version | Date       | Changes                                                        |
| ------- | ---------- | -------------------------------------------------------------- |
| 1.0     | 2025-12-26 | Initial consolidated specification                             |
| 1.1     | 2025-12-28 | Added section 1.3: Historical Context & Hardware Understanding |
|         |            | - Why memory-mapped I/O matters for Apple II                   |
|         |            | - Soft switch concept explanation                              |
|         |            | - Floating bus behavior and emulation approaches               |
|         |            | - Why 7 slots and their addressing scheme                      |