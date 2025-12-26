# Emulator Architecture Specification v1.0 (Part 3)

## Part IX: Observability & Tracing

### 9.1 Trace Events

```csharp
/// <summary>
/// Compact trace event for bus access logging.
/// No allocations; fits in a ring buffer.
/// </summary>
public readonly struct BusTraceEvent
{
    public readonly ulong Cycle;
    public readonly uint Address;
    public readonly uint Value;
    public readonly byte WidthBits;
    public readonly AccessIntent Intent;
    public readonly AccessFlags Flags;
    public readonly int SourceId;
    public readonly int DeviceId;
    public readonly ushort RegionTag;
}

/// <summary>
/// Ring buffer trace sink.
/// </summary>
public sealed class TraceRingBuffer
{
    private readonly BusTraceEvent[] _buffer;
    private int _writeIndex;
    
    public TraceRingBuffer(int capacity = 65536)
    {
        // Capacity must be power of 2 for fast wrap
        _buffer = new BusTraceEvent[capacity];
    }
    
    public void Emit(in BusTraceEvent evt)
    {
        _buffer[_writeIndex & (_buffer.Length - 1)] = evt;
        _writeIndex++;
    }
}
```

### 9.2 Device Registry

The device registry provides a mapping from structural device IDs to rich metadata.
It supports both simple integer IDs and structured `DevicePageId` values for DEV-backed PTEs.

```csharp
/// <summary>
/// Device page class identifiers for DEV-backed page table entries.
/// </summary>
public enum DevicePageClass : byte
{
    Invalid = 0x0,      // Reserved/unmapped
    CompatIO = 0x1,     // Apple II-family compat I/O ($C000-$CFFF)
    SlotROM = 0x2,      // Slot/expansion ROM windows
    Framebuffer = 0x3,  // Video/framebuffer aperture
    Storage = 0x4,      // Storage controller MMIO
    Network = 0x5,      // Network controller MMIO
    Timer = 0x6,        // Timer/interrupt controller MMIO
    Debug = 0x7,        // Debug/semihosting console
    Audio = 0x8,        // Audio/sound device MMIO
    Input = 0x9,        // Input device MMIO
    Dma = 0xA,          // DMA controller MMIO
    SystemControl = 0xB // System control registers
    // 0xC-0xF reserved
}

/// <summary>
/// Structured 20-bit identifier for device pages in DEV-backed PTEs.
/// Encoding: Class (4 bits) | Instance (8 bits) | Page (8 bits)
/// </summary>
public readonly struct DevicePageId
{
    private readonly uint value;
    
    public DevicePageClass Class => (DevicePageClass)((value >> 16) & 0xF);
    public byte Instance => (byte)((value >> 8) & 0xFF);
    public byte Page => (byte)(value & 0xFF);
    public uint RawValue => value;
    public bool IsValid => Class != DevicePageClass.Invalid;
    
    // Factory methods for common device types
    public static DevicePageId CreateCompatIO(byte guestId, byte page = 0);
    public static DevicePageId CreateSlotROM(byte slotNumber, byte page = 0);
    public static DevicePageId CreateStorage(byte controllerId, byte page = 0);
    public static DevicePageId CreateTimer(byte controllerId, byte page = 0);
    public static DevicePageId CreateDebug(byte channelId = 0, byte page = 0);
}

/// <summary>
/// Metadata describing a registered device instance.
/// </summary>
public readonly record struct DeviceInfo(
    int Id,                    // Simple integer ID for hot-path storage
    DevicePageId PageId,       // Structured ID for DEV-backed PTEs
    string Kind,               // Device category (e.g., "SlotCard", "Ram")
    string Name,               // Human-readable display name
    string WiringPath);        // Hierarchical path (e.g., "main/slots/6/disk2")

/// <summary>
/// Registry for device instances with human-readable metadata.
/// </summary>
public interface IDeviceRegistry
{
    int Count { get; }
    
    void Register(int id, string kind, string name, string wiringPath);
    void Register(int id, DevicePageId pageId, string kind, string name, string wiringPath);
    
    bool TryGet(int id, out DeviceInfo info);
    bool TryGetByPageId(DevicePageId pageId, out DeviceInfo info);
    
    DeviceInfo Get(int id);
    DeviceInfo GetByPageId(DevicePageId pageId);
    
    IEnumerable<DeviceInfo> GetAll();
    IEnumerable<DeviceInfo> GetByClass(DevicePageClass deviceClass);
    
    bool Contains(int id);
    bool ContainsPageId(DevicePageId pageId);
    
    int GenerateId();
}
```

### 9.3 Device Page ID Encoding

The Device Page ID uses a 20-bit structured encoding that fits in the PFN field of a DEV-backed PTE:

| Bits    | Field    | Size   | Range   | Purpose                           |
|---------|----------|--------|---------|-----------------------------------|
| 19-16   | Class    | 4 bits | 0-15    | Device category                   |
| 15-8    | Instance | 8 bits | 0-255   | Device instance within class      |
| 7-0     | Page     | 8 bits | 0-255   | Page within device instance       |

This provides capacity for:
- 16 device classes
- 256 instances per class
- 256 pages per instance
- Total: 1,048,576 addressable device pages

The structured encoding makes dumps readable and provides a natural seam for multi-guest
virtualization (each guest's devices use different instance numbers).

---

## Part X: Boot & Reset Sequence

### 10.1 Pocket2e Boot Sequence

```
1. Power-on reset
2. CPU reads reset vector from $FFFC-$FFFD
3. ROM code at reset vector executes
4. Firmware initializes soft switches
5. Autostart ROM searches for bootable disk
6. If found: load boot sector, jump to $0801
7. If not: enter monitor or BASIC prompt
```

### 10.2 PocketGS Boot Sequence

The Apple IIgs boot process involves multiple ROM versions and the Mega II chip:

```
1. Power-on reset
   - 65C816 starts in emulation mode (E=1)
   - CPU reads reset vector from $00/FFFC-$00/FFFD
   
2. ROM03 initialization:
   - Switches to native mode
   - Initializes Mega II chip
   - Sets up shadowing for Bank $E0-$E1
   - Probes and sizes RAM
   - Initializes toolbox
   
3. Control Panel check:
   - If Open-Apple held: enter Control Panel
   - Otherwise: continue boot
   
4. Slot scan:
   - Scans slots 7 down to 1 for bootable device
   - Checks $Cn00 for signature bytes ($20, $00, $03)
   
5. Boot device found:
   - Loads boot block from device
   - Transfers control to loaded code
   
6. No boot device:
   - Enters BASIC.SYSTEM or ROM BASIC
```

### 10.3 PocketME Boot Sequence

From privileged spec v0.4:

```
1. Hard reset:
   - CPU enters K privilege, M2 mode
   - CR0.PG = 0, CR0.NXE = 0
   - VBAR = 0x00000000
   
2. CPU reads RESET vector from VBAR + 0
   - With PG=0, this is physical address 0x00000000
   - Boot ROM must be mapped here
   
3. Boot ROM code:
   - Initializes minimal hardware state
   - Probes memory, builds memory map
   - Loads stage-2 loader (or kernel) into RAM
   - Builds initial page tables at RAM base
   - Sets PTBR to page table physical address
   - Sets VBAR to kernel's vector page
   - Sets CR0.PG = 1 (enable paging)
   - Sets CR0.NXE = 1 (enable NX)
   - Jumps to kernel entry point
   
4. Kernel entry:
   - Still in K privilege, M2 mode
   - Full MMU and protection now active
   - Sets up interrupt handlers
   - Initializes devices
   - Optionally: sets up compatibility contexts for Apple II guests
   - Launches init process or shell
```

### 10.4 Boot Handoff Structure

```csharp
/// <summary>
/// Boot ROM passes this structure to the kernel.
/// Located at physical 0x00040000 (first RAM after ROM).
/// Pointer passed in R0 at kernel entry.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BootHandoff
{
    public const uint Magic = 0x4F484D42;  // "BMHO"
    
    public uint magic;
    public ushort versionMajor;     // 1
    public ushort versionMinor;     // 0
    public uint totalSize;
    public uint flags;
    
    public uint bootRomPhysBase;    // 0x00000000
    public uint bootRomSize;        // 256KB
    public uint ramPhysBase;        // 0x00040000
    public uint ramSize;
    
    public uint compatIdDefault;    // Default personality
    
    public uint cmdlineOffset;      // Offset to command line string
    public uint cmdlineLength;
    
    public uint memMapOffset;       // Offset to memory map entries
    public uint memMapCount;
    
    public uint romInvOffset;       // Offset to ROM inventory
    public uint romInvCount;
}
```

---

## Part XI: Implementation Roadmap

### Phase 1: Foundation (Issue #51 Phases 1-3)
**Goal:** Core interfaces, basic implementations, page-table routing

- [ ] Define all core types (`BusAccess`, `PageEntry`, `TargetCaps`, etc.)
- [ ] Implement `MainBus` with page table routing
- [ ] Implement `SignalBus` for interrupts
- [ ] Implement `DeviceRegistry`
- [ ] Unit tests for bus routing logic

### Phase 2: Apple II Integration (Issue #51 Phases 4-5)
**Goal:** Pocket2e can boot to ROM prompt

- [ ] Implement `AppleIISoftSwitchPage` (composite page)
- [ ] Implement `SlotManager` for peripheral bus
- [ ] Implement basic RAM/ROM as `IBusTarget`
- [ ] Wire 65C02 to bus architecture
- [ ] Integration test: boot Apple II ROM

### Phase 3: Devices & Scheduler (Issue #51 Phases 6-7)
**Goal:** Timing-accurate emulation

- [ ] Implement `Scheduler` with cycle-accurate events
- [ ] Video timing (scanlines, VBL)
- [ ] Keyboard handling
- [ ] Speaker output
- [ ] Disk II controller (basic)

### Phase 4: Observability & Debug (Issue #51 Phase 8 + Issue #64)
**Goal:** Developer tooling

- [ ] Trace ring buffer
- [ ] Debug console commands
- [ ] CPU state inspection
- [ ] Memory dumping with Peek semantics
- [ ] Breakpoints and watchpoints

### Phase 5: 65C816 Extension
**Goal:** PocketGS foundation

- [ ] Implement 65C816 CPU core
- [ ] 24-bit addressing in bus
- [ ] Bank switching
- [ ] Mega II emulation
- [ ] Super Hi-Res graphics support
- [ ] Ensoniq DOC sound chip

### Phase 6: 65832 Fantasy CPU
**Goal:** PocketME vision

- [ ] Prefix opcode decoding ($42, $43, $44)
- [ ] 32-bit registers and addressing
- [ ] Page-table MMU
- [ ] Privilege levels (U/K)
- [ ] Trap/exception handling
- [ ] Compatibility contexts

---

## Part XII: Coding Standards & Conventions

### 12.1 Naming Conventions

```csharp
// Interfaces: I-prefix
public interface IBusTarget { }
public interface IMemoryBus { }

// Enums: PascalCase, no prefix
public enum AccessIntent { DataRead, DataWrite }

// Structs: readonly record struct for immutable data
public readonly record struct BusAccess(...);

// Constants: Within class, PascalCase
private const int PageShift = 12;
```

### 12.2 Performance Guidelines

1. **Hot path first**: The data plane must be allocation-free
2. **Ref readonly for large structs**: Pass `BusAccess` as `in` parameter
3. **Avoid virtual dispatch in tight loops**: Use concrete types or delegates
4. **Page table is array-indexed**: O(1) lookup, no dictionary
5. **Tracing is guarded**: Single `if (enabled)` check

### 12.3 Testing Strategy

```
Unit Tests:
  - Bus routing logic
  - Page table management
  - Signal assertion/deassertion
  - Scheduler ordering

Integration Tests:
  - CPU + Bus: instruction execution
  - Device interaction through soft switches
  - Boot sequence to ROM prompt

Conformance Tests:
  - 6502/65C02 instruction accuracy
  - Apple II memory map behavior
  - Timing accuracy (optional strict mode)
```

---

## Document History

| Version | Date       | Changes                                      |
| ------- | ---------- | -------------------------------------------- |
| 1.0     | 2025-12-26 | Initial consolidated specification           |
| 1.1     | 2025-01-13 | Split into Part 3; added PocketGS boot seq   |