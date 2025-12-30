# SmartPort Specification

## Document Information

| Field        | Value                                              |
|--------------|----------------------------------------------------|
| Version      | 1.0                                                |
| Date         | 2025-12-28                                         |
| Status       | Initial Draft                                      |
| Applies To   | Pocket2c (Apple IIc), PocketGS (Apple IIgs)        |

---

## 1. Overview

SmartPort is Apple's enhanced disk interface protocol introduced with the Apple IIc and
used extensively on the Apple IIgs. It supersedes the simpler Disk II interface while
maintaining backward compatibility with ProDOS.

### 1.1 Why SmartPort Exists

The original Disk II controller was designed for 5.25" floppy drives with a simple
read/write protocol. As Apple introduced new storage devices (3.5" drives, hard drives,
RAM disks), a more flexible protocol was needed:

1. **Device abstraction**: SmartPort provides a uniform interface for different device types
2. **Extended capacity**: Supports devices larger than the Disk II's 140KB limit
3. **Multiple devices**: Up to 127 devices on a single controller
4. **Device information**: Standardized way to query device capabilities

### 1.2 SmartPort vs. Disk II

| Feature              | Disk II                | SmartPort              |
|----------------------|------------------------|------------------------|
| Max devices          | 2                      | 127                    |
| Max block size       | 256 bytes (sectors)    | 512 bytes (blocks)     |
| Max device size      | 140KB                  | 32MB (per device)      |
| Device types         | 5.25" floppy only      | Any block device       |
| Protocol             | Hardware-level         | Firmware command/status|
| Error handling       | Basic                  | Extended status codes  |

---

## 2. SmartPort Architecture

### 2.1 Hardware Interface

SmartPort uses a daisy-chain topology where multiple devices share a single cable:

```
??????????     ????????????     ????????????     ????????????
? Host   ??????? Device 1 ??????? Device 2 ??????? Device N ?
?(IIc/gs)?     ?(Internal)?     ?          ?     ?          ?
??????????     ????????????     ????????????     ????????????
```

### 2.2 Device Addressing

Each device on the SmartPort chain has a unique unit number:

| Unit Number | Device                                    |
|-------------|-------------------------------------------|
| 0           | Host controller (used for bus commands)   |
| 1           | First device (often internal drive)       |
| 2           | Second device                             |
| ...         | ...                                       |
| 127         | Maximum device number                     |

### 2.3 Memory-Mapped Interface

On the Apple IIc, SmartPort is accessed through slot 5 ($C0D0-$C0DF):

| Address | Name    | Function                              |
|---------|---------|---------------------------------------|
| $C0D0   | PHASE0  | Phase 0 control                       |
| $C0D1   | PHASE0  | Phase 0 control (alternate)           |
| $C0D2   | PHASE1  | Phase 1 control                       |
| $C0D3   | PHASE1  | Phase 1 control (alternate)           |
| $C0D4   | PHASE2  | Phase 2 control                       |
| $C0D5   | PHASE2  | Phase 2 control (alternate)           |
| $C0D6   | PHASE3  | Phase 3 control                       |
| $C0D7   | PHASE3  | Phase 3 control (alternate)           |
| $C0D8   | MOTOROFF| Motor off (also deselect)             |
| $C0D9   | MOTORON | Motor on (also select)                |
| $C0DA   | DEVSEL1 | Select drive 1                        |
| $C0DB   | DEVSEL2 | Select drive 2                        |
| $C0DC   | Q6L     | Strobe data latch / shift             |
| $C0DD   | Q6H     | Load data latch                       |
| $C0DE   | Q7L     | Prepare for read                      |
| $C0DF   | Q7H     | Prepare for write                     |

---

## 3. SmartPort Commands

SmartPort commands are invoked via a firmware call at the SmartPort entry point:

```assembly
; SmartPort call convention
    JSR $C5xx          ; Call SmartPort entry point
    .BYTE command      ; Command number
    .WORD param_list   ; Pointer to parameter list
    ; Returns here with error code in A, carry set on error
```

### 3.1 Standard Commands

| Command | Name           | Description                           |
|---------|----------------|---------------------------------------|
| $00     | STATUS         | Get device/bus status                 |
| $01     | READ BLOCK     | Read one or more blocks               |
| $02     | WRITE BLOCK    | Write one or more blocks              |
| $03     | FORMAT         | Format the device                     |
| $04     | CONTROL        | Device-specific control               |
| $05     | INIT           | Initialize device                     |
| $06     | OPEN           | Open access to device                 |
| $07     | CLOSE          | Close access to device                |
| $08     | READ           | Character-device read                 |
| $09     | WRITE          | Character-device write                |

### 3.2 Extended Commands (IIgs)

| Command | Name           | Description                           |
|---------|----------------|---------------------------------------|
| $40     | STATUS (ext)   | Extended status with long addressing  |
| $41     | READ (ext)     | Extended read with long addressing    |
| $42     | WRITE (ext)    | Extended write with long addressing   |
| $43     | FORMAT (ext)   | Extended format                       |
| $44     | CONTROL (ext)  | Extended control                      |

---

## 4. Command Details

### 4.1 STATUS Command ($00)

Returns information about a device or the entire SmartPort bus.

**Parameter List**:
```
+0: Parameter count (3)
+1: Unit number (0 = bus, 1-127 = device)
+2: Status list pointer (low byte)
+3: Status list pointer (high byte)
+4: Status code
```

**Status Codes**:
| Code | Description                                       |
|------|---------------------------------------------------|
| $00  | Device status (returns DIB - Device Info Block)   |
| $01  | Get DCB (Device Control Block)                    |
| $02  | Get newline status                                |
| $03  | Get DIB (Device Information Block)                |

**Device Status ($00) Return Data**:
```
+0: General status byte
    Bit 7: Block device
    Bit 6: Write-allowed
    Bit 5: Read-allowed
    Bit 4: Device online/disk inserted
    Bit 3: Format-allowed
    Bit 2: Write-protected media
    Bit 1: Interruptible
    Bit 0: Device open
+1-3: Device size in blocks (24-bit, little-endian)
```

### 4.2 READ BLOCK Command ($01)

Reads one or more 512-byte blocks from the device.

**Parameter List**:
```
+0: Parameter count (3)
+1: Unit number (1-127)
+2: Buffer pointer (low)
+3: Buffer pointer (high)
+4: Block number (low)
+5: Block number (middle)
+6: Block number (high)
```

**Implementation**:
```csharp
public TrapResult SmartPortRead(ICpu cpu, IMemoryBus bus, IEventContext context)
{
    // Get parameter list address from inline bytes after JSR
    ushort paramList = GetInlineWord(cpu, bus);
    
    byte paramCount = bus.Read8(paramList);
    byte unitNumber = bus.Read8((ushort)(paramList + 1));
    ushort bufferPtr = bus.Read16((ushort)(paramList + 2));
    uint blockNumber = bus.Read8((ushort)(paramList + 4)) |
                       ((uint)bus.Read8((ushort)(paramList + 5)) << 8) |
                       ((uint)bus.Read8((ushort)(paramList + 6)) << 16);
    
    var device = GetDevice(unitNumber);
    if (device == null)
        return SmartPortError(cpu, 0x28);  // No device connected
    
    var block = device.ReadBlock(blockNumber);
    if (block == null)
        return SmartPortError(cpu, 0x27);  // I/O error
    
    // Copy block to buffer
    for (int i = 0; i < 512; i++)
        bus.Write8((ushort)(bufferPtr + i), block[i]);
    
    return SmartPortSuccess(cpu);
}
```

### 4.3 WRITE BLOCK Command ($02)

Writes one or more 512-byte blocks to the device.

**Parameter List**:
```
+0: Parameter count (3)
+1: Unit number (1-127)
+2: Buffer pointer (low)
+3: Buffer pointer (high)
+4: Block number (low)
+5: Block number (middle)
+6: Block number (high)
```

### 4.4 FORMAT Command ($03)

Formats the device (initializes all blocks).

**Parameter List**:
```
+0: Parameter count (1)
+1: Unit number (1-127)
```

### 4.5 CONTROL Command ($04)

Sends a device-specific control command.

**Parameter List**:
```
+0: Parameter count (3)
+1: Unit number (1-127)
+2: Control list pointer (low)
+3: Control list pointer (high)
+4: Control code
```

**Common Control Codes**:
| Code | Description                                       |
|------|---------------------------------------------------|
| $00  | Reset device                                      |
| $01  | Set DCB                                           |
| $02  | Set newline mode                                  |
| $03  | Eject disk                                        |
| $04  | Set disk-switched flag                            |

---

## 5. Error Codes

SmartPort returns error codes in the accumulator with carry set:

| Code | Name              | Description                          |
|------|-------------------|--------------------------------------|
| $00  | No error          | Operation successful                 |
| $01  | Bad command       | Unknown command number               |
| $04  | Bad param count   | Wrong number of parameters           |
| $21  | Invalid unit      | Unit number out of range             |
| $27  | I/O error         | Device read/write failed             |
| $28  | No device         | No device at specified unit          |
| $2B  | Write protected   | Attempt to write read-only media     |
| $2D  | Disk switched     | Disk was changed since last access   |
| $2E  | Device offline    | No disk in drive                     |
| $2F  | Volume too large  | Block number exceeds device size     |

---

## 6. Device Types

SmartPort supports various device types, identified in the Device Information Block:

### 6.1 Device Type Codes

| Code | Device Type                                       |
|------|---------------------------------------------------|
| $00  | Memory expansion card (RAM disk)                  |
| $01  | 3.5" floppy disk drive                            |
| $02  | ProFile hard drive                                |
| $03  | Generic SCSI device                               |
| $04  | SCSI hard disk                                    |
| $05  | SCSI tape drive                                   |
| $06  | SCSI CD-ROM                                       |
| $07  | SCSI printer                                      |
| $08  | Host adapter                                      |
| $09  | Character device (serial, etc.)                   |
| $0A  | Tape backup unit                                  |

### 6.2 Device Information Block (DIB)

The DIB is returned by STATUS command code $03:

```
+0:    Status byte (same as STATUS $00 byte 0)
+1-3:  Block count (24-bit)
+4:    String length (1-16)
+5-20: Device name (Pascal string, 16 bytes max)
+21-22: Device type and subtype
+23-24: Version number
```

---

## 7. SmartPort Interface

```csharp
/// <summary>
/// Interface for SmartPort controller emulation.
/// </summary>
public interface ISmartPortController : IPeripheral
{
    /// <summary>Gets the number of connected devices.</summary>
    int DeviceCount { get; }
    
    /// <summary>Gets a connected device by unit number.</summary>
    ISmartPortDevice? GetDevice(int unitNumber);
    
    /// <summary>Adds a device to the SmartPort chain.</summary>
    int AddDevice(ISmartPortDevice device);
    
    /// <summary>Removes a device from the SmartPort chain.</summary>
    bool RemoveDevice(int unitNumber);
    
    /// <summary>Executes a SmartPort command.</summary>
    SmartPortResult ExecuteCommand(SmartPortCommand command, in SmartPortParams parameters);
}

/// <summary>
/// Interface for a SmartPort-compatible device.
/// </summary>
public interface ISmartPortDevice
{
    /// <summary>Gets the device type code.</summary>
    byte DeviceType { get; }
    
    /// <summary>Gets the device name (max 16 characters).</summary>
    string DeviceName { get; }
    
    /// <summary>Gets the total number of blocks.</summary>
    uint BlockCount { get; }
    
    /// <summary>Gets whether the device is online (media present).</summary>
    bool IsOnline { get; }
    
    /// <summary>Gets whether the device is write-protected.</summary>
    bool IsWriteProtected { get; }
    
    /// <summary>Reads a block from the device.</summary>
    SmartPortResult ReadBlock(uint blockNumber, Span<byte> buffer);
    
    /// <summary>Writes a block to the device.</summary>
    SmartPortResult WriteBlock(uint blockNumber, ReadOnlySpan<byte> buffer);
    
    /// <summary>Formats the device.</summary>
    SmartPortResult Format();
    
    /// <summary>Gets device status.</summary>
    SmartPortResult GetStatus(byte statusCode, Span<byte> buffer);
    
    /// <summary>Executes a control command.</summary>
    SmartPortResult Control(byte controlCode, ReadOnlySpan<byte> parameters);
}

/// <summary>
/// Result of a SmartPort operation.
/// </summary>
public readonly record struct SmartPortResult(
    byte ErrorCode,
    int BytesTransferred = 0
)
{
    public bool IsSuccess => ErrorCode == 0;
    
    public static SmartPortResult Success(int bytes = 0) 
        => new(0, bytes);
    public static SmartPortResult Error(byte code) 
        => new(code);
}
```

---

## 8. ProDOS Integration

SmartPort integrates seamlessly with ProDOS through the ProDOS block device driver interface.

### 8.1 ProDOS Device Driver Calls

ProDOS uses a simplified interface that maps to SmartPort:

| ProDOS Call    | Maps To SmartPort   |
|----------------|---------------------|
| DRIVER_STATUS  | STATUS ($00)        |
| DRIVER_READ    | READ BLOCK ($01)    |
| DRIVER_WRITE   | WRITE BLOCK ($02)   |
| DRIVER_FORMAT  | FORMAT ($03)        |

### 8.2 Volume Mapping

ProDOS maps SmartPort units to drive slots:

```
/S5D1 ? Slot 5, Drive 1 ? SmartPort Unit 1
/S5D2 ? Slot 5, Drive 2 ? SmartPort Unit 2
```

---

## 9. Implementation Notes

### 9.1 Disk Image Support

Common disk image formats for SmartPort devices:

| Format | Extension | Description                           |
|--------|-----------|---------------------------------------|
| 2IMG   | .2mg      | Universal disk image with metadata    |
| ProDOS | .po      | Raw ProDOS-order blocks               |
| DOS    | .do       | Raw DOS 3.3-order sectors             |
| HDV    | .hdv      | Hard disk volume image                |

### 9.2 Block Translation

For 2IMG and ProDOS images, blocks map directly:

```csharp
long fileOffset = blockNumber * 512;
```

For DOS-order images, sector interleaving applies.

### 9.3 Trap Handler Example

```csharp
/// <summary>
/// SmartPort entry point trap handler.
/// </summary>
public TrapResult SmartPortEntry(ICpu cpu, IMemoryBus bus, IEventContext context)
{
    // Get return address (points to inline parameters)
    ushort returnAddr = PopWord(cpu, bus);
    
    // Read inline command and parameter list pointer
    byte command = bus.Read8(returnAddr);
    ushort paramList = bus.Read16((ushort)(returnAddr + 1));
    
    // Advance return address past inline data
    returnAddr += 3;
    PushWord(cpu, bus, returnAddr);
    
    // Execute command
    var result = _controller.ExecuteCommand(
        (SmartPortCommand)command,
        ReadParameters(bus, paramList));
    
    // Set result in accumulator and carry
    cpu.SetRegisterA(result.ErrorCode);
    cpu.SetCarry(result.ErrorCode != 0);
    
    return new TrapResult(
        Handled: true,
        CyclesConsumed: new Cycle(result.IsSuccess ? 100 : 50),
        ReturnAddress: null);
}
```

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |

---

## Appendix A: Bus Architecture Integration

This appendix provides implementation guidance for integrating SmartPort with the
emulator's bus architecture.

### A.1 SmartPort as IPeripheral

SmartPort controllers implement `IPeripheral` for slot integration:

```csharp
/// <summary>
/// SmartPort controller implementing the peripheral interface.
/// </summary>
public sealed class SmartPortController : IPeripheral, ISchedulable
{
    private readonly List<ISmartPortDevice> _devices = new();
    private readonly byte[] _slotRom;
    private readonly byte[] _expansionRom;
    
    /// <inheritdoc/>
    public string Name => "SmartPort Controller";
    
    /// <inheritdoc/>
    public string DeviceType => "SmartPort";
    
    /// <inheritdoc/>
    public int SlotNumber { get; set; }
    
    /// <inheritdoc/>
    public IBusTarget? MMIORegion { get; }
    
    /// <inheritdoc/>
    public IBusTarget? ROMRegion { get; }
    
    /// <inheritdoc/>
    public IBusTarget? ExpansionROMRegion { get; }
    
    public SmartPortController()
    {
        MMIORegion = new SmartPortMMIO(this);
        ROMRegion = new RomTarget(_slotRom);
        ExpansionROMRegion = new RomTarget(_expansionRom);
    }
}
```

### A.2 MMIO Target Implementation

The SmartPort MMIO region ($C0n0-$C0nF) implements `IBusTarget`:

```csharp
/// <summary>
/// SmartPort MMIO registers (16 bytes per slot).
/// </summary>
public sealed class SmartPortMMIO : IBusTarget
{
    private readonly SmartPortController _controller;
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.SideEffects | TargetCaps.TimingSense;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return 0xFF;  // Debug reads return floating bus
        
        int offset = (int)(physicalAddress & 0x0F);
        
        return offset switch
        {
            0x00 => _controller.GetPhase0(),
            0x02 => _controller.GetPhase1(),
            0x04 => _controller.GetPhase2(),
            0x06 => _controller.GetPhase3(),
            0x08 => _controller.MotorOff(),
            0x09 => _controller.MotorOn(),
            0x0C => _controller.ReadQ6L(),
            0x0D => _controller.ReadQ6H(),
            0x0E => _controller.ReadQ7L(),
            0x0F => _controller.ReadQ7H(),
            _ => 0xFF
        };
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return;
        
        int offset = (int)(physicalAddress & 0x0F);
        
        // Most SmartPort accesses trigger on read; writes echo the behavior
        _ = Read8(physicalAddress, access);
    }
}
```

### A.3 Trap Handler for SmartPort Entry Point

SmartPort firmware entry is trapped for native implementation:

```csharp
/// <summary>
/// SmartPort entry point trap handler.
/// </summary>
public sealed class SmartPortTrap
{
    private readonly SmartPortController _controller;
    private readonly ISlotManager _slots;
    
    /// <summary>
    /// Trap handler for SmartPort firmware entry ($Cn00).
    /// </summary>
    public TrapResult Execute(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        // Verify slot contains SmartPort controller
        var card = _slots.GetCard(_controller.SlotNumber);
        if (card?.DeviceType != "SmartPort")
            return new TrapResult(Handled: false, default, null);
        
        // Select expansion ROM (simulates ROM access)
        _slots.SelectExpansionSlot(_controller.SlotNumber);
        
        // Get return address from stack (points to inline parameters)
        ushort returnAddr = PopWord(cpu, bus, context);
        
        // Read inline command and parameter list
        var access = CreateAccess(cpu, context, AccessIntent.DataRead);
        byte command = bus.Read8(access with { Address = returnAddr }).Value;
        ushort paramList = bus.Read16(access with { Address = (Addr)(returnAddr + 1) }).Value;
        
        // Advance return address past inline data
        returnAddr += 3;
        PushWord(cpu, bus, context, returnAddr);
        
        // Execute the SmartPort command
        var result = ExecuteCommand(command, paramList, bus, context);
        
        // Set result in accumulator and carry
        cpu.A = result.ErrorCode;
        cpu.SetCarry(result.ErrorCode != 0);
        
        return new TrapResult(
            Handled: true,
            CyclesConsumed: new Cycle(result.IsSuccess ? 200 : 50),
            ReturnAddress: null);
    }
    
    private SmartPortResult ExecuteCommand(
        byte command, 
        ushort paramList,
        IMemoryBus bus,
        IEventContext context)
    {
        var access = CreateAccess(null, context, AccessIntent.DataRead);
        
        byte paramCount = bus.Read8(access with { Address = paramList }).Value;
        byte unitNumber = bus.Read8(access with { Address = (Addr)(paramList + 1) }).Value;
        
        return command switch
        {
            0x00 => HandleStatus(unitNumber, paramList, bus, access),
            0x01 => HandleReadBlock(unitNumber, paramList, bus, access),
            0x02 => HandleWriteBlock(unitNumber, paramList, bus, access),
            0x03 => HandleFormat(unitNumber),
            0x04 => HandleControl(unitNumber, paramList, bus, access),
            _ => SmartPortResult.Error(0x01)  // Bad command
        };
    }
}
```

### A.4 Block Device Implementation

SmartPort devices implement block I/O with proper bus access:

```csharp
/// <summary>
/// SmartPort block device (e.g., 3.5" floppy, hard drive).
/// </summary>
public sealed class SmartPortBlockDevice : ISmartPortDevice
{
    private readonly Stream _diskImage;
    private readonly byte[] _blockBuffer = new byte[512];
    
    /// <inheritdoc/>
    public SmartPortResult ReadBlock(uint blockNumber, Span<byte> buffer)
    {
        if (blockNumber >= BlockCount)
            return SmartPortResult.Error(0x2F);  // Volume too large
        
        if (!IsOnline)
            return SmartPortResult.Error(0x2E);  // Device offline
        
        _diskImage.Seek(blockNumber * 512, SeekOrigin.Begin);
        int read = _diskImage.Read(_blockBuffer);
        
        if (read != 512)
            return SmartPortResult.Error(0x27);  // I/O error
        
        _blockBuffer.CopyTo(buffer);
        return SmartPortResult.Success(512);
    }
    
    /// <inheritdoc/>
    public SmartPortResult WriteBlock(uint blockNumber, ReadOnlySpan<byte> buffer)
    {
        if (IsWriteProtected)
            return SmartPortResult.Error(0x2B);  // Write protected
        
        if (blockNumber >= BlockCount)
            return SmartPortResult.Error(0x2F);  // Volume too large
        
        buffer.CopyTo(_blockBuffer);
        _diskImage.Seek(blockNumber * 512, SeekOrigin.Begin);
        _diskImage.Write(_blockBuffer);
        
        return SmartPortResult.Success(512);
    }
}
```

### A.5 Memory Transfer with Bus Access

Block transfers copy data through the bus with proper access semantics:

```csharp
private SmartPortResult HandleReadBlock(
    byte unitNumber,
    ushort paramList,
    IMemoryBus bus,
    BusAccess baseAccess)
{
    ushort bufferPtr = bus.Read16(baseAccess with { Address = (Addr)(paramList + 2) }).Value;
    uint blockNumber = bus.Read8(baseAccess with { Address = (Addr)(paramList + 4) }).Value |
                       ((uint)bus.Read8(baseAccess with { Address = (Addr)(paramList + 5) }).Value << 8) |
                       ((uint)bus.Read8(baseAccess with { Address = (Addr)(paramList + 6) }).Value << 16);
    
    var device = _controller.GetDevice(unitNumber);
    if (device == null)
        return SmartPortResult.Error(0x28);  // No device connected
    
    Span<byte> buffer = stackalloc byte[512];
    var result = device.ReadBlock(blockNumber, buffer);
    
    if (!result.IsSuccess)
        return result;
    
    // Copy block to guest memory
    var writeAccess = baseAccess with { Intent = AccessIntent.DataWrite };
    for (int i = 0; i < 512; i++)
    {
        bus.Write8(writeAccess with { 
            Address = (Addr)(bufferPtr + i),
            Value = buffer[i]
        });
    }
    
    return SmartPortResult.Success(512);
}
```

### A.6 Scheduler Integration for Motor Timing

SmartPort uses the scheduler for realistic motor timing:

```csharp
public sealed class SmartPortController : ISchedulable
{
    private const ulong MotorSpinUpCycles = 500_000;  // ~0.5 second
    private const ulong MotorSpinDownCycles = 2_000_000;  // ~2 seconds
    
    private bool _motorRunning;
    private bool _motorSpinningUp;
    
    public byte MotorOn()
    {
        if (!_motorRunning && !_motorSpinningUp)
        {
            _motorSpinningUp = true;
            _scheduler.ScheduleAfter(this, MotorSpinUpCycles);
        }
        return 0xFF;
    }
    
    public byte MotorOff()
    {
        if (_motorRunning)
        {
            _motorRunning = false;
            // Motor coasts to stop
            _scheduler.ScheduleAfter(this, MotorSpinDownCycles);
        }
        return 0xFF;
    }
    
    /// <inheritdoc/>
    public ulong Execute(ulong currentCycle)
    {
        if (_motorSpinningUp)
        {
            _motorSpinningUp = false;
            _motorRunning = true;
        }
        return 0;  // One-shot event
    }
}
```

### A.7 Device Registry

```csharp
public void RegisterSmartPortDevices(IDeviceRegistry registry, int slot)
{
    // Controller
    int controllerId = registry.GenerateId();
    registry.Register(
        controllerId,
        DevicePageId.Create(DevicePageClass.Storage, instance: (byte)slot, page: 0),
        kind: "SmartPortController",
        name: $"SmartPort (Slot {slot})",
        wiringPath: $"main/slots/{slot}/smartport");
    
    // Individual devices
    foreach (var device in _devices)
    {
        registry.Register(
            registry.GenerateId(),
            DevicePageId.Create(DevicePageClass.Storage, instance: (byte)slot, page: (byte)device.UnitNumber),
            kind: device.DeviceType.ToString(),
            name: device.DeviceName,
            wiringPath: $"main/slots/{slot}/smartport/unit{device.UnitNumber}");
    }
}
```

### A.8 Expansion ROM Trap Considerations

SmartPort expansion ROM traps must validate slot selection:

```csharp
/// <summary>
/// Trap for SmartPort routine in expansion ROM.
/// </summary>
public TrapResult SmartPortExpansionTrap(ICpu cpu, IMemoryBus bus, IEventContext context)
{
    // Only handle if correct slot's expansion ROM is active
    if (_slots.ActiveExpansionSlot != _controller.SlotNumber)
    {
        // Different slot's expansion ROM is selected - don't trap
        return new TrapResult(Handled: false, default, null);
    }
    
    // Verify slot has SmartPort controller
    var card = _slots.GetCard(_controller.SlotNumber);
    if (card?.DeviceType != "SmartPort")
        return new TrapResult(Handled: false, default, null);
    
    // Handle the trap...
    return HandleSmartPortRoutine(cpu, bus, context);
}
