# Unified Block Device Backing API for Apple II Emulator in C# 14 / .NET 10

---

## Introduction

Emulating the Apple II family’s storage ecosystem requires a nuanced approach to block device abstraction, honoring both historical slot conventions and the diversity of device types (floppy, hard disk, CD-ROM, network volumes). The Apple II’s SmartPort (slot 5) and SCSI (slot 7) interfaces are the primary means by which block devices are exposed to the system, each with distinct protocol and enumeration semantics. Modern emulators must support both, providing a clean, extensible API that enables future device types and network-backed volumes, while maintaining compatibility with ProDOS and GS/OS.

This technical design document proposes a unified block device backing API in C# 14 targeting .NET 10, with SmartPort and SCSI façade classes, device enumeration and slot assignment logic, a future-proof disk image format, and extensibility recommendations. The design leverages contemporary C# features (extension members, field-backed properties, spans) and draws on best practices from emulator, driver, and plugin architectures.

---

## High-Level Architecture and Goals

### Design Objectives

- **Unified Abstraction:** Present a single, coherent API for block-oriented devices, regardless of underlying physical or emulated type.
- **Historical Fidelity:** Honor Apple II slot conventions, device enumeration, and firmware behaviors.
- **SmartPort and SCSI Support:** Provide façade classes that wrap the base device and implement slot-specific protocol logic.
- **ProDOS/GSOS Compatibility:** Ensure block device semantics, block sizes, and metadata align with Apple II operating system expectations.
- **Extensibility:** Enable future device types (e.g., network volumes, RAM disks, tape, CD-ROM) and advanced features (caching, concurrency, security).
- **Developer Ergonomics:** Offer a clean, well-documented API surface, leveraging modern C# idioms.

### Architectural Overview

The architecture is layered as follows:

- **BlockDeviceBase:** Abstract base class or interface for block devices, exposing core read/write, info, and command methods.
- **SmartPortFaçade / SCSIFaçade:** Protocol-specific wrappers that adapt the base device to SmartPort or SCSI semantics, including slot assignment and command translation.
- **DeviceManager:** Handles device enumeration, slot assignment, and OS compatibility checks.
- **DiskImageProvider:** Loads and manages disk images, mapping metadata to device instances.
- **Extensibility Points:** Plugin architecture for new device types, network volumes, and advanced features.

---

## Base Interface / Abstract Class for Block Devices

### Core Interface

The base interface defines the essential contract for block-oriented devices:

```csharp
/// <summary>
/// Unified block device interface for Apple II emulation.
/// </summary>
public interface IBlockDevice
{
    /// <summary>
    /// Reads a block of data asynchronously.
    /// </summary>
    /// <param name="blockNumber">Logical block number.</param>
    /// <param name="buffer">Destination buffer (Span).</param>
    /// <returns>Task representing the operation.</returns>
    Task ReadBlockAsync(uint blockNumber, Span<byte> buffer);

    /// <summary>
    /// Writes a block of data asynchronously.
    /// </summary>
    /// <param name="blockNumber">Logical block number.</param>
    /// <param name="buffer">Source buffer (ReadOnlySpan).</param>
    /// <returns>Task representing the operation.</returns>
    Task WriteBlockAsync(uint blockNumber, ReadOnlySpan<byte> buffer);

    /// <summary>
    /// Queries device metadata and capabilities.
    /// </summary>
    BlockDeviceInfo GetDeviceInfo();

    /// <summary>
    /// Issues a device-specific command (passthrough).
    /// </summary>
    /// <param name="command">Command descriptor.</param>
    /// <param name="parameters">Command parameters.</param>
    /// <returns>Command result.</returns>
    Task<DeviceCommandResult> SendDeviceCommandAsync(DeviceCommand command, object? parameters = null);

    /// <summary>
    /// Event raised on device error.
    /// </summary>
    event EventHandler<BlockDeviceErrorEventArgs> DeviceError;
}
```

#### Key Features

- **Async I/O:** All block operations are asynchronous, supporting modern emulator threading and performance.
- **Span Support:** Uses `Span<byte>` and `ReadOnlySpan<byte>` for efficient, safe buffer management.
- **Device Info:** Returns a rich metadata object, including type, block size, LUN, volume count, and capabilities.
- **Command Passthrough:** Allows façade layers to issue protocol-specific or vendor-specific commands.
- **Error Handling:** Event-driven error reporting for robust recovery and diagnostics.

### Device Info Structure

```csharp
public record BlockDeviceInfo(
    string DeviceId,
    BlockDeviceType DeviceType,
    uint BlockSize,
    ulong BlockCount,
    int LogicalUnitNumber,
    int VolumeCount,
    bool IsRemovable,
    bool IsReadOnly,
    IReadOnlyDictionary<string, object>? ExtendedMetadata = null
);
```

- **DeviceId:** Unique identifier (e.g., GUID, SCSI ID, SmartPort unit).
- **DeviceType:** Enum (Floppy, HardDisk, CDROM, Network, RAMDisk, Tape, etc.).
- **BlockSize / BlockCount:** Physical/logical geometry.
- **LUN / VolumeCount:** SCSI/SmartPort logical unit and partitioning.
- **Removable/ReadOnly:** Flags for media characteristics.
- **ExtendedMetadata:** Arbitrary key-value pairs for extensibility.

### Device-Specific Command Handling

The API supports device-specific commands, such as SCSI INQUIRY, MODE SENSE, or SmartPort CONTROL:

```csharp
public enum DeviceCommand
{
    Inquiry,
    ModeSense,
    Format,
    Eject,
    Custom
}

public record DeviceCommandResult(
    bool Success,
    byte[]? Data,
    string? Message,
    Exception? Exception
);
```

This enables façade layers to implement protocol-specific behaviors and passthroughs.

---

## Block Read/Write Semantics and Async I/O

### Block Semantics

- **Block-Oriented:** All I/O is in terms of logical blocks (typically 512 bytes for ProDOS/GSOS, but may vary for CD-ROM, network, or legacy devices).
- **Alignment:** API enforces block alignment; partial reads/writes are not permitted at the base layer.
- **Sparse Support:** Devices may support sparse allocation (e.g., ProDOS tree files, network volumes).

### Asynchronous I/O

- **Task-Based:** All I/O methods return `Task`, enabling non-blocking emulator operation.
- **Cancellation:** Optional support for cancellation tokens for advanced scenarios.
- **Thread-Safety:** Implementations must be thread-safe, supporting concurrent access from emulator subsystems.

### Example Implementation

```csharp
public class FileBackedBlockDevice : IBlockDevice
{
    // ... fields omitted for brevity

    public async Task ReadBlockAsync(uint blockNumber, Span<byte> buffer)
    {
        // Seek and read block from file or memory-mapped region
        // Use async file I/O for performance
    }

    public async Task WriteBlockAsync(uint blockNumber, ReadOnlySpan<byte> buffer)
    {
        // Seek and write block to file or memory-mapped region
        // Flush or defer as per caching policy
    }

    // ... other methods
}
```

---

## Device Info and Capability Querying (Metadata)

### Metadata Model

The API exposes device metadata via `BlockDeviceInfo`, supporting:

- **Type Identification:** Floppy, hard disk, CD-ROM, network, etc.
- **Geometry:** Block size, block count, volume count.
- **LUN:** Logical unit number for SCSI devices.
- **Removability/ReadOnly:** Flags for media characteristics.
- **Extended Metadata:** Arbitrary extensibility for vendor, firmware, or emulator-specific data.

### Capability Flags

Devices may expose capabilities such as:

- **SupportsFormat:** Can be formatted.
- **SupportsEject:** Media can be ejected.
- **SupportsMultipleVolumes:** Device supports partitioning.
- **SupportsPassthrough:** Device supports protocol passthrough commands.

These are exposed via the `ExtendedMetadata` dictionary or as explicit properties.

---

## Device-Specific Command Handling and Passthrough

### SCSI Command Passthrough

For SCSI devices, the API supports command descriptor blocks (CDBs) and sense data:

```csharp
public record ScsiCommand(
    byte[] CommandDescriptorBlock,
    int ExpectedDataLength,
    bool IsWrite
);

public record ScsiCommandResult(
    bool Success,
    byte[]? Data,
    ScsiSenseData? SenseData,
    string? Message,
    Exception? Exception
);
```

- **INQUIRY, READ, WRITE, MODE SENSE, REQUEST SENSE:** Supported via façade translation.
- **Sense Data:** Error reporting and diagnostics.

### SmartPort Command Passthrough

SmartPort commands (STATUS, READ BLOCK, WRITE BLOCK, FORMAT, CONTROL, INIT, OPEN, CLOSE, READ, WRITE) are mapped to base device operations.

- **STATUS:** Device info and state.
- **CONTROL:** Device-specific control codes.
- **INIT:** Bus/device initialization.
- **OPEN/CLOSE:** For character devices (future extensibility).

### Custom Commands

The API supports custom or vendor-specific commands via the `Custom` enum value and extended parameters.

---

## SmartPort Façade Design (Slot 5)

### Historical Context

SmartPort is Apple’s protocol for intelligent block devices, typically exposed via slot 5 (or built-in on IIc/IIgs). It supports up to 127 devices in theory, but ProDOS limits to two per slot; GS/OS can enumerate more.

### Façade Responsibilities

- **Slot Assignment:** Maps device to slot 5, assigns unit numbers.
- **Command Translation:** Converts SmartPort commands to base device operations.
- **STATUS Handling:** Returns device info, block count, device name, type, subtype, firmware version.
- **Mirroring:** Handles ProDOS mirroring for >2 devices per slot (see Technical Note 20).
- **Compatibility:** Ensures block size, geometry, and device type align with ProDOS/GSOS expectations.

### Example Façade Class

```csharp
public class SmartPortFaçade : IBlockDevice
{
    private readonly IBlockDevice _device;
    private readonly int _slotNumber;
    private readonly int _unitNumber;

    public SmartPortFaçade(IBlockDevice device, int slotNumber, int unitNumber)
    {
        _device = device;
        _slotNumber = slotNumber;
        _unitNumber = unitNumber;
    }

    // Translate SmartPort commands to base device operations
    public async Task ReadBlockAsync(uint blockNumber, Span<byte> buffer)
        => await _device.ReadBlockAsync(blockNumber, buffer);

    public async Task WriteBlockAsync(uint blockNumber, ReadOnlySpan<byte> buffer)
        => await _device.WriteBlockAsync(blockNumber, buffer);

    public BlockDeviceInfo GetDeviceInfo()
    {
        var info = _device.GetDeviceInfo();
        // Add SmartPort-specific metadata (slot, unit, etc.)
        return info with { ExtendedMetadata = new Dictionary<string, object>
        {
            ["SlotNumber"] = _slotNumber,
            ["UnitNumber"] = _unitNumber
        }};
    }

    public async Task<DeviceCommandResult> SendDeviceCommandAsync(DeviceCommand command, object? parameters = null)
        => await _device.SendDeviceCommandAsync(command, parameters);

    public event EventHandler<BlockDeviceErrorEventArgs>? DeviceError
    {
        add => _device.DeviceError += value;
        remove => _device.DeviceError -= value;
    }
}
```

### Device Enumeration and Mirroring

- **ProDOS:** Supports two devices per slot; additional devices are mirrored to slot 2 (see Technical Note 20).
- **GS/OS:** Can enumerate more devices; driver must support dynamic unit assignment.

---

## SCSI Façade Design (Slot 7)

### Historical Context

Apple II SCSI cards (slot 7) expose up to seven external devices, each with a SCSI ID and potentially multiple ProDOS partitions (mapped to unit numbers).

### Façade Responsibilities

- **Slot Assignment:** Maps device to slot 7, assigns SCSI ID and LUN.
- **Command Translation:** Converts SCSI commands to base device operations.
- **INQUIRY, MODE SENSE, REQUEST SENSE:** Supported via command passthrough.
- **Partition Mapping:** Maps SCSI partitions to ProDOS/GSOS volumes and unit numbers.
- **Compatibility:** Ensures block size, geometry, and device type align with OS expectations.

### Example Façade Class

```csharp
public class SCSIFaçade : IBlockDevice
{
    private readonly IBlockDevice _device;
    private readonly int _slotNumber;
    private readonly int _scsiId;
    private readonly int _lun;

    public SCSIFaçade(IBlockDevice device, int slotNumber, int scsiId, int lun)
    {
        _device = device;
        _slotNumber = slotNumber;
        _scsiId = scsiId;
        _lun = lun;
    }

    // Translate SCSI commands to base device operations
    public async Task ReadBlockAsync(uint blockNumber, Span<byte> buffer)
        => await _device.ReadBlockAsync(blockNumber, buffer);

    public async Task WriteBlockAsync(uint blockNumber, ReadOnlySpan<byte> buffer)
        => await _device.WriteBlockAsync(blockNumber, buffer);

    public BlockDeviceInfo GetDeviceInfo()
    {
        var info = _device.GetDeviceInfo();
        // Add SCSI-specific metadata (slot, SCSI ID, LUN)
        return info with { ExtendedMetadata = new Dictionary<string, object>
        {
            ["SlotNumber"] = _slotNumber,
            ["SCSIId"] = _scsiId,
            ["LUN"] = _lun
        }};
    }

    public async Task<DeviceCommandResult> SendDeviceCommandAsync(DeviceCommand command, object? parameters = null)
        => await _device.SendDeviceCommandAsync(command, parameters);

    public event EventHandler<BlockDeviceErrorEventArgs>? DeviceError
    {
        add => _device.DeviceError += value;
        remove => _device.DeviceError -= value;
    }
}
```

### Device Enumeration

- **SCSI ID Assignment:** Devices are assigned SCSI IDs (0-7); card itself is typically ID 7.
- **Unit Number Mapping:** Each partition is mapped to a unit number; firmware assigns unit numbers based on SCSI ID and partition order.
- **GS/OS:** Supports dynamic driver loading and device enumeration.

---

## Device Enumeration and Slot Assignment Rules

### Slot Assignment Table

| Slot | Interface   | Max Devices | OS Support         | Notes                                 |
|------|-------------|-------------|--------------------|---------------------------------------|
| 5    | SmartPort   | 2 (ProDOS)  | ProDOS, GS/OS      | Mirroring for >2 devices (slot 2)     |
| 5    | SmartPort   | 127 (GS/OS) | GS/OS              | GS/OS can enumerate more devices      |
| 7    | SCSI        | 7           | ProDOS, GS/OS      | SCSI ID assignment, partition mapping |
| Any  | Custom      | Varies      | GS/OS              | Network, RAMDisk, future devices      |

- **ProDOS:** Two devices per slot; mirroring for additional devices.
- **GS/OS:** Dynamic enumeration; supports loaded/generated drivers.

### Enumeration Algorithm

1. **Scan Slots:** Emulator scans slots 1-7 for block device cards or firmware.
2. **Query Devices:** For each slot, query device count via STATUS/INQUIRY.
3. **Assign Unit Numbers:** Map devices to unit numbers per slot and OS rules.
4. **Mirroring:** For SmartPort, mirror additional devices to slot 2 if >2 devices.
5. **Partition Mapping:** For SCSI, map partitions to unit numbers.
6. **Expose Devices:** Register devices with OS driver tables.

---

## ProDOS and GS/OS Compatibility Considerations

### ProDOS

- **Block Size:** 512 bytes per block; maximum volume size 32MB (65535 blocks).
- **Device Signature:** ProDOS checks for block device signature bytes at $Cn01, $Cn03, $Cn05, $Cn07.
- **Unit Numbering:** Two devices per slot; mirroring for additional devices.
- **Sparse Files:** Supports seedling, sapling, tree files; sparse allocation.

### GS/OS

- **Driver Model:** Supports loaded and generated drivers; can enumerate more devices.
- **Partitioning:** Supports Apple Partition Map (APM); can boot from CD-ROM or hard disk partitions.
- **Block Size:** Typically 512 bytes, but can support larger blocks for CD-ROM/network.
- **Device Types:** Supports hard disk, CD-ROM, network, RAMDisk, tape.

### Compatibility Table

| OS     | Block Size | Max Volume | Device Types           | Partitioning      |
|--------|------------|------------|------------------------|-------------------|
| ProDOS | 512 bytes  | 32MB       | Floppy, Hard Disk      | None/ProDOS order |
| GS/OS  | 512+ bytes | >32MB      | HD, CD-ROM, Network    | APM, ISO9660      |

---

## Disk Image Format Proposal and Mapping to API

### Requirements

- **Metadata Support:** Device type, block size, LUN, volume count, partitioning.
- **Extensible:** Support for future device types, network volumes, sparse allocation.
- **Compatibility:** Map cleanly to API and OS expectations.

### Proposed Format: Extended 2IMG / DIMG Hybrid

Building on the Apple II Universal Disk Image (2IMG) format, with extensions for device metadata and multi-volume support, and drawing on DIMG’s dual-block architecture for paired images.

#### Header Structure

| Offset | Length | Field                | Description                                 |
|--------|--------|----------------------|---------------------------------------------|
| 0      | 4      | Magic                | "2IMG" or "D1MG"                            |
| 4      | 4      | Creator Signature    | Application identifier                      |
| 8      | 2      | Header Length        | Size of header (e.g., 64 bytes)             |
| 10     | 2      | Format Version       | Format version (e.g., 1)                    |
| 12     | 4      | Image Data Format    | DOS/ProDOS/nibble/network                   |
| 16     | 4      | Flags                | Write-protect, volume number, etc.          |
| 20     | 4      | Block Size           | Block size in bytes                         |
| 24     | 4      | Block Count          | Number of blocks                            |
| 28     | 4      | LUN                  | Logical Unit Number                         |
| 32     | 4      | Volume Count         | Number of volumes/partitions                |
| 36     | 4      | Device Type          | Enum (floppy, hard, CD-ROM, network, etc.)  |
| 40     | 4      | Data Offset          | Offset to image data                        |
| 44     | 4      | Data Length          | Length of image data                        |
| 48     | 4      | Metadata Offset      | Offset to extended metadata                 |
| 52     | 4      | Metadata Length      | Length of metadata section                  |
| 56     | 8      | Reserved             | For future use                              |

#### Metadata Section (JSON or XML)

- **DeviceType:** "HardDisk", "Floppy", "CDROM", "Network", etc.
- **BlockSize:** 512, 2048, etc.
- **LUN:** Integer.
- **VolumeCount:** Integer.
- **Partitions:** Array of partition descriptors (start, length, type, name).
- **ExtendedAttributes:** Arbitrary key-value pairs.

#### Data Section

- **Raw Blocks:** Sequential block data, optionally sparse or compressed.
- **Multi-Volume:** For multi-volume images, data is partitioned per volume.

#### Example Mapping to API

When loading a disk image:

1. **Parse Header:** Extract device type, block size, LUN, volume count.
2. **Parse Metadata:** Map extended attributes to `BlockDeviceInfo.ExtendedMetadata`.
3. **Instantiate Device:** Create `IBlockDevice` instance with parsed parameters.
4. **Expose Volumes:** For multi-volume images, create multiple device instances or expose as partitions.

#### Format Comparison Table

| Format   | Metadata Support | Multi-Volume | Device Types | Extensible | Sparse/Compressed |
|----------|------------------|--------------|--------------|------------|-------------------|
| .po/.do  | Minimal          | No           | Floppy/HD    | No         | No                |
| .2mg     | Good             | No           | Floppy/HD    | Limited    | No                |
| .dimg    | Excellent        | Yes          | Any          | Yes        | Yes               |
| .hdv     | Minimal          | No           | HD           | No         | No                |

---

## Mapping Device Types (Floppy, Hard, CD-ROM, Network)

### Device Type Enum

```csharp
public enum BlockDeviceType
{
    Floppy,
    HardDisk,
    CDROM,
    Network,
    RAMDisk,
    Tape,
    Custom
}
```

### Type-Specific Behaviors

- **Floppy:** Typically 140KB (5.25"), 800KB (3.5"); block size 512 bytes; removable.
- **Hard Disk:** 32MB+; block size 512 bytes; may support multiple volumes.
- **CD-ROM:** Block size 2048 bytes; read-only; may use ISO9660 or APM partitioning.
- **Network:** Block size variable; may support sparse allocation, dynamic volumes.
- **RAMDisk:** Volatile; block size 512 bytes; fast access.
- **Tape:** Sequential access; block size variable.

### API Mapping

- **Block Size:** Set per device type.
- **ReadOnly/Removable:** Flags set per device type.
- **Partitioning:** Multi-volume support for hard disk, CD-ROM, network.

---

## Logical Unit Number (LUN) and Volume Count Handling

### SCSI LUNs

- **LUN:** SCSI devices may expose multiple logical units; mapped via LUN field in image and API.
- **Partition Mapping:** Each LUN may map to a separate volume or partition.

### SmartPort Units

- **Unit Number:** SmartPort devices are assigned unit numbers per slot; mapped via unit field in image and API.

### Volume Count

- **Multi-Volume Devices:** Devices may expose multiple volumes; API supports enumeration and access.

---

## Block Size and Alignment Strategies

### Block Size Table

| Device Type | Typical Block Size | Notes                      |
|-------------|-------------------|----------------------------|
| Floppy      | 512 bytes         | ProDOS, DOS 3.3            |
| Hard Disk   | 512 bytes         | ProDOS, GS/OS               |
| CD-ROM      | 2048 bytes        | ISO9660, APM                |
| Network     | Variable          | May be 512, 1024, 4096      |
| RAMDisk     | 512 bytes         | Volatile                    |
| Tape        | Variable          | Sequential                  |

- **Alignment:** API enforces block alignment; partial reads/writes not permitted.
- **Sparse Support:** Devices may support sparse allocation; API exposes capability.

---

## Extensibility for Future Device Types and Network Volumes

### Plugin Architecture

Leverage .NET plugin patterns (reflection, MEF, DI container composition) for extensibility.

- **Contracts:** Define interfaces for new device types.
- **Discovery:** Load plugins from assemblies or packages.
- **Isolation:** Use AssemblyLoadContext or sandboxing for security.
- **Registration:** Devices register themselves with DeviceManager.

### Extension Members (C# 14)

Use C# 14 extension members to add properties, methods, and indexers to device types without modifying base interfaces.

```csharp
public static class BlockDeviceExtensions
{
    extension(IBlockDevice device)
    {
        public bool IsNetworkBacked => device.GetDeviceInfo().DeviceType == BlockDeviceType.Network;
        public int PartitionCount => device.GetDeviceInfo().VolumeCount;
        // Additional extension properties/methods
    }
}
```

### Future Device Types

- **Network Volumes:** Support for remote block devices, cloud storage, distributed filesystems.
- **Tape Drives:** Sequential access, backup/restore semantics.
- **CD/DVD/BD:** Optical media, ISO9660/UDF support.
- **RAMDisk:** Volatile, high-speed storage.
- **Custom Devices:** Emulator-specific or experimental types.

---

## Performance Considerations and Caching Strategies

### Multi-Tier Caching

Implement multi-tier caching for block device operations, inspired by bcache/lvmcache in Linux.

- **Write-Back:** Data written to cache, flushed to backing device asynchronously.
- **Write-Through:** Data written to both cache and backing device synchronously.
- **Read-Ahead:** Prefetch blocks for sequential access.
- **Dirty Block Tracking:** Mark blocks as dirty; flush on demand or schedule.
- **Cache Aside:** Application manages cache population and eviction.

### Concurrency and Thread-Safety

- **Locking:** Use fine-grained locks or concurrent collections for device state.
- **Async I/O:** Task-based methods enable non-blocking operation.
- **Queueing:** Request queue for pending I/O operations.

---

## Error Handling, Reporting, and Recovery

### Error Model

- **DeviceError Event:** Raised on I/O errors, command failures, or device faults.
- **Exception Propagation:** API methods throw or return exceptions as appropriate.
- **Sense Data:** For SCSI devices, expose sense codes and additional sense data.
- **Recovery:** Support for retry, failover, and device reset.

### Error Reporting Table

| Error Type      | API Event/Exception | Recovery Strategy           |
|-----------------|--------------------|-----------------------------|
| I/O Error       | DeviceError         | Retry, failover, log        |
| Media Not Ready | DeviceError         | Wait, retry, notify user    |
| Write Protect   | DeviceError         | Block write, notify user    |
| Command Failure | DeviceError         | Retry, escalate             |
| Device Offline  | DeviceError         | Remove device, notify user  |

---

## Concurrency, Thread-Safety, and Synchronization

### Thread-Safety

- **Locking:** Use `ReaderWriterLockSlim` or similar for device state.
- **Async Methods:** All I/O is async; avoid blocking emulator threads.
- **Request Queue:** Queue I/O requests for serialized execution if needed.

### Synchronization Strategies

- **Per-Device Locks:** Each device manages its own synchronization.
- **Global Device Manager:** Coordinates access across devices.

---

## Testing, Validation, and Compatibility Test Suites

### Test Coverage

- **Unit Tests:** For all API methods, device types, and façade behaviors.
- **Integration Tests:** Emulator integration, OS compatibility, slot assignment.
- **Compatibility Tests:** ProDOS, GS/OS, disk image formats, edge cases.
- **Performance Tests:** I/O throughput, caching, concurrency.

### Reference Implementations

- **SmartPortSD:** Arduino-based SmartPort emulator; supports up to four ProDOS images on SD card.
- **SPIISD:** SD card SmartPort device; supports .po, .2mg, .hdv images; config.txt for boot order.
- **CiderPress2:** Disk image tool for Apple II and vintage Mac; supports .2mg, .po, .hdv, multi-volume, metadata.
- **DiskM8:** Command-line disk image manipulation and analysis tool; supports ProDOS, DOS, .2mg, .nib, multi-volume.

---

## C# 14 Language Features to Leverage

### Extension Members

- **Add properties, indexers, and static members to interfaces and types without modifying base code.**

### Field-Backed Properties

- **Simplify property implementation with `field` keyword.**

### Span Support

- **Efficient buffer management with `Span<byte>` and `ReadOnlySpan<byte>`.**

### Partial Events and Constructors

- **Enable partial implementation for extensibility.**

### Implicit Span Conversions

- **Natural programming with `Span<T>` and `ReadOnlySpan<T>`.**

---

## API Surface: Public Types, Methods, Events, and Examples

### Public Types

- `IBlockDevice`
- `BlockDeviceInfo`
- `DeviceCommand`, `DeviceCommandResult`
- `SmartPortFaçade`, `SCSIFaçade`
- `DeviceManager`
- `DiskImageProvider`
- `BlockDeviceType`
- `BlockDeviceErrorEventArgs`

### Example Usage

```csharp
// Load disk image and create device
var image = DiskImageProvider.Load("disk.2mg");
var device = new FileBackedBlockDevice(image);

// Wrap with SmartPort façade for slot 5
var smartPortDevice = new SmartPortFaçade(device, slotNumber: 5, unitNumber: 1);

// Read block 0
byte[] buffer = new byte[512];
await smartPortDevice.ReadBlockAsync(0, buffer);

// Query device info
var info = smartPortDevice.GetDeviceInfo();
Console.WriteLine($"Device: {info.DeviceType}, Blocks: {info.BlockCount}");

// Handle errors
smartPortDevice.DeviceError += (sender, args) =>
{
    Console.WriteLine($"Error: {args.Message}");
};
```

---

## Disk Image Tooling and Utilities (Create, Inspect, Convert)

### Tools

- **CiderPress2:** Create, inspect, convert .2mg, .po, .hdv images; supports metadata, multi-volume.
- **Disk Jockey:** Create blank images, partitioned device images, multi-volume, SCSI driver installation.
- **DiskM8:** Manipulate, catalog, analyze disk images; supports ProDOS, DOS, .2mg, .nib, multi-volume.

### Utility Functions

- **CreateImage:** Generate blank or partitioned disk images.
- **InspectImage:** Parse and display metadata, partitions, device type.
- **ConvertImage:** Convert between formats (.po, .2mg, .hdv, .dimg).
- **ValidateImage:** Check for corruption, compatibility, block alignment.

---

## Security and Sandboxing Considerations

### Isolation Strategies

- **AssemblyLoadContext:** Isolate plugins and device implementations.
- **Windows Sandbox:** Run untrusted code in disposable VM.
- **Permission Model:** Restrict device access, file I/O, network operations.

### Security Table

| Threat           | Mitigation                   |
|------------------|-----------------------------|
| Untrusted Plugin | AssemblyLoadContext, sandbox |
| Data Corruption  | Validation, checksums        |
| Unauthorized I/O | Permission model, isolation  |
| Network Risks    | TLS, authentication          |

---

## Documentation, Migration, and Developer Ergonomics

### XML Documentation Comments

- **Use triple-slash `///` comments with XML tags for API documentation.**
- **Generate XML documentation files for integration with OpenAPI, IntelliSense, and external tools.**

### Migration Strategies

- **Legacy Devices:** Provide adapters for legacy device types and image formats.
- **API Evolution:** Use extension members and partial classes for non-breaking changes.

### Developer Ergonomics

- **Clear API Surface:** Well-named types, methods, and events.
- **Rich Metadata:** Device info, error reporting, extensibility.
- **Sample Code:** Provide examples for common scenarios.
- **Tooling:** Integrate with disk image utilities and emulator configuration.

---

## Reference Implementations and Open-Source Examples

### SmartPortSD

- **Arduino-based SmartPort emulator; supports up to four ProDOS images on SD card; FAT/FAT32 support; LED and button interface; open-source forks and PCB designs.**

### SPIISD

- **SD card SmartPort device; supports .po, .2mg, .hdv images; config.txt for boot order; LCD and button interface; compatibility with IIe, IIc, IIgs; open-source lineage.**

### CiderPress2

- **Disk image tool for Apple II and vintage Mac; supports .2mg, .po, .hdv, multi-volume, metadata; open-source, cross-platform; API for disk image manipulation.**

### DiskM8

- **Command-line disk image manipulation and analysis tool; supports ProDOS, DOS, .2mg, .nib, multi-volume; open-source, written in Go.**

---

## Summary Table: Unified Block Device API Features

| Feature                | Description                                              | Supported By |
|------------------------|---------------------------------------------------------|--------------|
| Block Read/Write       | Async, block-aligned I/O                                | API, façade  |
| Device Info/Metadata   | Rich device info, type, block size, LUN, volumes        | API          |
| Command Passthrough    | SCSI/SmartPort command translation                      | Façade       |
| Slot Assignment        | Historical slot mapping, unit number, mirroring         | Façade       |
| ProDOS/GSOS Support    | Block size, volume limits, partitioning                 | API, façade  |
| Disk Image Format      | Metadata, multi-volume, extensible                      | API, tools   |
| Extensibility          | Plugin architecture, extension members                  | API          |
| Performance/Caching    | Multi-tier caching, async I/O, concurrency              | API          |
| Error Handling         | Event-driven, sense data, recovery                      | API          |
| Testing/Validation     | Unit, integration, compatibility, performance           | API, tools   |
| Security/Sandboxing    | Isolation, permission model, sandbox                    | API          |
| Documentation          | XML comments, OpenAPI integration                       | API          |
| Tooling/Utilities      | Create, inspect, convert disk images                    | Tools        |

---

## Conclusion

This unified block device backing API for Apple II emulation in C# 14 / .NET 10 provides a robust, extensible foundation for supporting both SmartPort and SCSI interfaces, honoring historical slot conventions while enabling modern flexibility. By abstracting block device operations, metadata, and command handling, and by leveraging contemporary C# features and plugin architectures, the design ensures future-proof compatibility with ProDOS, GS/OS, and emerging device types (network, RAMDisk, tape, CD-ROM).

The proposed disk image format supports rich metadata, multi-volume, and extensibility, mapping cleanly to the API and emulator requirements. Comprehensive error handling, performance optimizations, and developer ergonomics further enhance the architecture.

Reference implementations and open-source tools (SmartPortSD, SPIISD, CiderPress2, DiskM8) demonstrate the viability and adaptability of these concepts. By integrating these design principles, emulator developers can deliver accurate, flexible, and maintainable Apple II storage emulation for years to come.

---

## Appendix: Example Disk Image Format Specification

| Field           | Offset | Length | Description                                 |
|-----------------|--------|--------|---------------------------------------------|
| Magic           | 0      | 4      | "2IMG" or "D1MG"                            |
| Creator         | 4      | 4      | Application identifier                      |
| Header Length   | 8      | 2      | Size of header (e.g., 64 bytes)             |
| Format Version  | 10     | 2      | Format version (e.g., 1)                    |
| Data Format     | 12     | 4      | DOS/ProDOS/nibble/network                   |
| Flags           | 16     | 4      | Write-protect, volume number, etc.          |
| Block Size      | 20     | 4      | Block size in bytes                         |
| Block Count     | 24     | 4      | Number of blocks                            |
| LUN             | 28     | 4      | Logical Unit Number                         |
| Volume Count    | 32     | 4      | Number of volumes/partitions                |
| Device Type     | 36     | 4      | Enum (floppy, hard, CD-ROM, network, etc.)  |
| Data Offset     | 40     | 4      | Offset to image data                        |
| Data Length     | 44     | 4      | Length of image data                        |
| Metadata Offset | 48     | 4      | Offset to extended metadata                 |
| Metadata Length | 52     | 4      | Length of metadata section                  |
| Reserved        | 56     | 8      | For future use                              |

---

## Appendix: Reference Code Snippet (C# 14 Extension Members)

```csharp
public static class BlockDeviceExtensions
{
    extension(IBlockDevice device)
    {
        public bool IsNetworkBacked => device.GetDeviceInfo().DeviceType == BlockDeviceType.Network;
        public int PartitionCount => device.GetDeviceInfo().VolumeCount;
        public string DeviceSummary => $"{device.GetDeviceInfo().DeviceType} ({device.GetDeviceInfo().BlockCount} blocks)";
    }
}
```

---

## Appendix: Example Device Enumeration Algorithm

```csharp
public class DeviceManager
{
    public IReadOnlyList<IBlockDevice> EnumerateDevices()
    {
        var devices = new List<IBlockDevice>();
        for (int slot = 1; slot <= 7; slot++)
        {
            var card = ProbeSlot(slot);
            if (card is ISmartPortCard smartPort)
            {
                var unitCount = smartPort.GetUnitCount();
                for (int unit = 1; unit <= unitCount; unit++)
                {
                    devices.Add(new SmartPortFaçade(smartPort.GetDevice(unit), slot, unit));
                }
            }
            else if (card is ISCSICard scsi)
            {
                foreach (var (scsiId, lun, device) in scsi.EnumerateDevices())
                {
                    devices.Add(new SCSIFaçade(device, slot, scsiId, lun));
                }
            }
        }
        return devices;
    }
}
```

---

**End of Technical Design Document**
Perfect — I’ve kicked off a deep research task to design a unified block device API in C#14 targeting .NET 10, with support for both SmartPort and SCSI façades. This will include:

- A clean abstraction for block-oriented devices
- Slot-specific façade implementations for SmartPort and SCSI
- A proposed disk image format with metadata for device type, block size, volume count, and more
- Extensibility hooks for future device types (e.g., network-backed volumes)

🛠 This will take me several minutes, so feel free to leave — I'll keep working in the background. Your report will be saved in this conversation.
