# SmartPort Specification

## Document Information

| Field        | Value                                              |
|--------------|----------------------------------------------------|
| Version      | 2.0                                                |
| Date         | 2025-06-11                                         |
| Status       | Revised Draft                                      |
| Applies To   | Pocket2c (Apple IIc), PocketGS (Apple IIgs)        |

---

## 1. Overview

SmartPort is Apple's block-level device interface protocol introduced with the Apple IIc
and UniDisk 3.5 drive, and later used extensively on the Apple IIgs. It provides a
firmware-level abstraction for block devices that supersedes the hardware-level Disk II
interface.

### 1.1 History and Purpose

SmartPort was developed to address limitations of the original Disk II interface:

1. **Device independence**: A uniform command interface for different storage device types
2. **Extended capacity**: Support for devices larger than 140KB (up to 32MB per device)
3. **Daisy-chaining**: Multiple devices (up to 8 per controller, not 127) on a single port
4. **Standard status reporting**: Consistent error codes and device information

### 1.2 SmartPort vs. Disk II

| Feature              | Disk II                | SmartPort              |
|----------------------|------------------------|------------------------|
| Max devices          | 2 per controller       | 8 per controller       |
| Block size           | 256 bytes (sectors)    | 512 bytes (blocks)     |
| Max device size      | 140KB                  | 32MB (per device)      |
| Addressing           | Track/Sector           | Linear block number    |
| Protocol             | Hardware bit-level     | Firmware command calls |
| Error handling       | Basic (carry flag)     | Extended error codes   |

---

## 2. SmartPort Architecture

### 2.1 Physical Interface

SmartPort uses a DB-19 connector (same as standard Apple II disk port) but with
enhanced signaling. Devices are daisy-chained with each device having IN and OUT
ports.

```
??????????     ??????????????     ??????????????
? Host   ??????? Device 1   ??????? Device 2   ?...
?(IIc/gs)?     ? (Unit 1)   ?     ? (Unit 2)   ?
??????????     ??????????????     ??????????????
```

### 2.2 Device Addressing

Unit numbers are assigned sequentially starting at 1:

| Unit Number | Description                                       |
|-------------|---------------------------------------------------|
| 0           | Host adapter (bus-level STATUS only)              |
| 1           | First device in chain                             |
| 2           | Second device in chain                            |
| ...         | ...                                               |
| 8           | Maximum devices per SmartPort controller          |

**Note**: The maximum of 8 devices per controller is a practical limit imposed by the
protocol and enumeration scheme, not 127 as sometimes erroneously stated.

### 2.3 Slot Assignment

On the Apple IIc, SmartPort is accessible through slot 5 (memory-mapped at $C500).
On the Apple IIgs, SmartPort is accessible through slot 5 (internal) and slot 6 or
other slots with SmartPort-compatible cards.

---

## 3. SmartPort Calling Convention

SmartPort commands are invoked via a firmware call with inline parameters:

```assembly
; Standard SmartPort call convention (8-bit)
        JSR     $Cn00+dispatch  ; Call SmartPort entry point
        .BYTE   command         ; Command number (1 byte)
        .WORD   param_list      ; Pointer to parameter list (2 bytes)
        ; Returns here with:
        ;   A = error code (0 = success)
        ;   Carry clear = success, Carry set = error
```

### 3.1 Finding the SmartPort Entry Point

The SmartPort entry point is located by examining the slot ROM:

```assembly
; Locating SmartPort entry point for slot N
; Slot ROM is at $CN00
        LDA     $CNFF           ; Signature byte 1
        CMP     #$00            ; Must be $00 for SmartPort
        BNE     not_smartport
        LDA     $CNFE           ; Signature byte 2
        CMP     #$FF            ; Usually $FF
        BNE     not_smartport
        LDA     $CNFB           ; Signature byte 3
        CMP     #$00            ; Must be $00
        BNE     not_smartport
        LDA     $CN07           ; Check ProDOS device signature
        CMP     #$00            ; Must be $00
        BNE     not_smartport
        
        ; SmartPort entry point offset is at $CNFF
        ; Actual entry = $CN00 + [$CNFF] + 3
        LDA     $CNFF           ; Get ProDOS entry offset
        CLC
        ADC     #$03            ; SmartPort entry is ProDOS+3
        STA     entry_lo
```

For slot 5 on the Apple IIc, the SmartPort entry point is typically at $C50D.

---

## 4. Standard Commands

### 4.1 Command Summary

| Cmd | Name           | Description                               |
|-----|----------------|-------------------------------------------|
| $00 | STATUS         | Get device or bus status                  |
| $01 | READ BLOCK     | Read one 512-byte block                   |
| $02 | WRITE BLOCK    | Write one 512-byte block                  |
| $03 | FORMAT         | Format/initialize the device              |
| $04 | CONTROL        | Send device-specific control command      |
| $05 | INIT           | Initialize device (reset)                 |
| $06 | OPEN           | Open device (character devices)           |
| $07 | CLOSE          | Close device (character devices)          |
| $08 | READ           | Character read (character devices)        |
| $09 | WRITE          | Character write (character devices)       |

### 4.2 Extended Commands (Apple IIgs)

Extended commands use 3-byte block addresses and 4-byte buffer pointers:

| Cmd  | Name              | Description                            |
|------|-------------------|----------------------------------------|
| $40  | STATUS (ext)      | Extended status with long parameters   |
| $41  | READ BLOCK (ext)  | Extended read with 24-bit addressing   |
| $42  | WRITE BLOCK (ext) | Extended write with 24-bit addressing  |
| $43  | FORMAT (ext)      | Extended format                        |
| $44  | CONTROL (ext)     | Extended control                       |
| $45  | INIT (ext)        | Extended init                          |
| $46  | OPEN (ext)        | Extended open                          |
| $47  | CLOSE (ext)       | Extended close                         |
| $48  | READ (ext)        | Extended character read                |
| $49  | WRITE (ext)       | Extended character write               |

---

## 5. Command Details

### 5.1 STATUS Command ($00)

Returns information about a device or the SmartPort bus.

**Parameter List (3 bytes)**:
```
Offset  Size  Description
+0      1     Parameter count (always 3)
+1      1     Unit number (0 = bus, 1-8 = device)
+2      2     Status list pointer (result buffer)
+4      1     Status code
```

**Status Codes**:

| Code | Name              | Description                             |
|------|-------------------|-----------------------------------------|
| $00  | Device Status     | General device status and block count   |
| $01  | DCB               | Device Control Block (vendor-specific)  |
| $02  | Newline Status    | Get newline character (char devices)    |
| $03  | DIB               | Device Information Block                |

**Device Status ($00) Return Buffer (4 bytes)**:
```
Offset  Size  Description
+0      1     General status byte:
              Bit 7: 1 = Block device, 0 = Character device
              Bit 6: 1 = Write allowed
              Bit 5: 1 = Read allowed
              Bit 4: 1 = Device online (disk in drive)
              Bit 3: 1 = Format allowed
              Bit 2: 1 = Media write-protected
              Bit 1: 1 = Interrupting (device needs attention)
              Bit 0: 1 = Device currently open
+1      3     Number of blocks (little-endian, 24-bit)
```

**Bus Status (Unit 0)**: When unit 0 is specified, returns number of devices
connected to the SmartPort bus in the first byte.

**Device Information Block (Status Code $03)**:
```
Offset  Size  Description
+0      1     General status byte (same as Device Status)
+1      3     Number of blocks (little-endian)
+4      1     Device ID string length (1-16)
+5      16    Device ID string (padded with spaces)
+21     2     Device type:
              Byte 0: Type (see Device Types)
              Byte 1: Subtype
+23     2     Firmware version (major.minor)
```

### 5.2 READ BLOCK Command ($01)

Reads a single 512-byte block from the device.

**Parameter List (3 bytes)**:
```
Offset  Size  Description
+0      1     Parameter count (always 3)
+1      1     Unit number (1-8)
+2      2     Data buffer pointer
+4      3     Block number (little-endian, 24-bit)
```

**Notes**:
- Returns 512 bytes to the specified buffer
- Block numbers start at 0
- Maximum block number is device-dependent (see STATUS)

### 5.3 WRITE BLOCK Command ($02)

Writes a single 512-byte block to the device.

**Parameter List (3 bytes)**:
```
Offset  Size  Description
+0      1     Parameter count (always 3)
+1      1     Unit number (1-8)
+2      2     Data buffer pointer (source)
+4      3     Block number (little-endian, 24-bit)
```

### 5.4 FORMAT Command ($03)

Formats/initializes the device. Effect is device-dependent.

**Parameter List (1 byte)**:
```
Offset  Size  Description
+0      1     Parameter count (always 1)
+1      1     Unit number (1-8)
```

**Notes**:
- For floppy disks: Low-level format
- For hard disks: May write boot blocks or do nothing
- For RAM disks: Typically zeroes all blocks

### 5.5 CONTROL Command ($04)

Sends a device-specific control command.

**Parameter List (3 bytes)**:
```
Offset  Size  Description
+0      1     Parameter count (always 3)
+1      1     Unit number (1-8)
+2      2     Control list pointer
+4      1     Control code
```

**Standard Control Codes**:

| Code | Name              | Description                             |
|------|-------------------|-----------------------------------------|
| $00  | Reset Device      | Reset device to power-on state          |
| $01  | Set DCB           | Set Device Control Block                |
| $02  | Set Newline       | Set newline character (char devices)    |
| $03  | Service Interrupt | Acknowledge/clear device interrupt      |
| $04  | Eject             | Eject removable media                   |
| $05  | Set IW Mode       | Set interleave/write mode               |

**Control List Format** (for control code $04 - Eject):
```
Offset  Size  Description
+0      1     List length (0)
```

### 5.6 INIT Command ($05)

Initializes/resets the device.

**Parameter List (1 byte)**:
```
Offset  Size  Description
+0      1     Parameter count (always 1)
+1      1     Unit number (1-8)
```

---

## 6. Error Codes

All SmartPort commands return an error code in the accumulator:

| Code | Name                 | Description                          |
|------|----------------------|--------------------------------------|
| $00  | NO_ERROR             | Operation successful                 |
| $01  | BAD_COMMAND          | Invalid command number               |
| $04  | BAD_PARAM_COUNT      | Wrong number of parameters           |
| $11  | BUS_ERROR            | Bus communication error              |
| $21  | BAD_UNIT             | Unit number out of range             |
| $27  | IO_ERROR             | General I/O error                    |
| $28  | NO_DEVICE            | No device at specified unit          |
| $2B  | WRITE_PROTECTED      | Media is write-protected             |
| $2E  | DISK_SWITCHED        | Disk changed since last access       |
| $2F  | DEVICE_OFFLINE       | No media in drive                    |
| $30  | VOLUME_TOO_LARGE     | Block number exceeds device capacity |

**Note on "Disk Switched"**: This error indicates the media was changed. The host
should re-read the volume directory before continuing.

---

## 7. Device Types

### 7.1 Device Type Codes

Device type is returned in the DIB (STATUS code $03):

| Type | Subtype | Device                                   |
|------|---------|------------------------------------------|
| $00  | $00     | Memory expansion (RAM disk)              |
| $01  | $00     | 3.5" floppy disk (400K/800K)             |
| $01  | $01     | 3.5" floppy disk (Apple UniDisk 3.5)     |
| $02  | $00     | ProFile hard disk                        |
| $02  | $01     | Generic hard disk                        |
| $03  | $xx     | Generic SCSI device                      |
| $04  | $00     | SCSI hard disk                           |
| $05  | $00     | SCSI tape drive                          |
| $06  | $00     | SCSI CD-ROM                              |
| $08  | $00     | Host adapter                             |
| $09  | $00     | Serial/character device                  |
| $0A  | $00     | AppleTalk/network device                 |

### 7.2 Determining Device Characteristics

```csharp
public bool IsBlockDevice(byte statusByte) => (statusByte & 0x80) != 0;
public bool IsWriteAllowed(byte statusByte) => (statusByte & 0x40) != 0;
public bool IsReadAllowed(byte statusByte) => (statusByte & 0x20) != 0;
public bool IsOnline(byte statusByte) => (statusByte & 0x10) != 0;
public bool IsFormatAllowed(byte statusByte) => (statusByte & 0x08) != 0;
public bool IsWriteProtected(byte statusByte) => (statusByte & 0x04) != 0;
public bool IsInterrupting(byte statusByte) => (statusByte & 0x02) != 0;
public bool IsOpen(byte statusByte) => (statusByte & 0x01) != 0;
```

---

## 8. ProDOS Integration

SmartPort integrates with ProDOS through the MLI (Machine Language Interface).

### 8.1 ProDOS Device Numbers

ProDOS maps SmartPort devices to slot/drive combinations:

```
ProDOS Unit = $S0 + (D-1) * $10
Where:
  S = Slot number (1-7)
  D = Drive number (1 or 2)

Example: Slot 5, Drive 1 = $50
         Slot 5, Drive 2 = $D0 (bit 7 indicates drive 2)
```

### 8.2 Block Device Driver Entry

ProDOS calls device drivers with:
- Command in zero page $42
- Unit number in zero page $43
- Buffer pointer in zero page $44-45
- Block number in zero page $46-47

| ProDOS Command | SmartPort Equivalent |
|----------------|----------------------|
| $00 - Status   | STATUS ($00)         |
| $01 - Read     | READ BLOCK ($01)     |
| $02 - Write    | WRITE BLOCK ($02)    |
| $03 - Format   | FORMAT ($03)         |

---

## 9. Implementation Interfaces

### 9.1 SmartPort Controller Interface

```csharp
/// <summary>
/// Interface for SmartPort controller emulation.
/// </summary>
public interface ISmartPortController : IPeripheral
{
    /// <summary>Gets the number of devices on this SmartPort bus.</summary>
    int DeviceCount { get; }
    
    /// <summary>Gets a device by unit number (1-8).</summary>
    /// <param name="unitNumber">The unit number (1-based).</param>
    /// <returns>The device, or null if no device at that unit.</returns>
    ISmartPortDevice? GetDevice(int unitNumber);
    
    /// <summary>Adds a device to the SmartPort bus.</summary>
    /// <param name="device">The device to add.</param>
    /// <returns>The assigned unit number, or -1 if bus is full.</returns>
    int AddDevice(ISmartPortDevice device);
    
    /// <summary>Removes a device from the SmartPort bus.</summary>
    /// <param name="unitNumber">The unit number to remove.</param>
    /// <returns>True if device was removed.</returns>
    bool RemoveDevice(int unitNumber);
    
    /// <summary>Executes a SmartPort command.</summary>
    /// <param name="command">The command number.</param>
    /// <param name="paramListAddress">Address of the parameter list.</param>
    /// <param name="bus">Memory bus for parameter/data access.</param>
    /// <returns>Error code (0 = success).</returns>
    byte ExecuteCommand(byte command, ushort paramListAddress, IMemoryBus bus);
}
```

### 9.2 SmartPort Device Interface

```csharp
/// <summary>
/// Interface for a SmartPort-compatible device.
/// </summary>
public interface ISmartPortDevice
{
    /// <summary>Gets the device type code.</summary>
    byte DeviceType { get; }
    
    /// <summary>Gets the device subtype code.</summary>
    byte DeviceSubtype { get; }
    
    /// <summary>Gets the device name (1-16 characters).</summary>
    string DeviceName { get; }
    
    /// <summary>Gets the total number of 512-byte blocks.</summary>
    uint BlockCount { get; }
    
    /// <summary>Gets the firmware version.</summary>
    ushort FirmwareVersion { get; }
    
    /// <summary>Gets whether the device is online (media present).</summary>
    bool IsOnline { get; }
    
    /// <summary>Gets whether the device is write-protected.</summary>
    bool IsWriteProtected { get; }
    
    /// <summary>Gets whether the media was changed since last access.</summary>
    bool DiskSwitched { get; }
    
    /// <summary>Reads a 512-byte block.</summary>
    /// <param name="blockNumber">Block number (0-based).</param>
    /// <param name="buffer">512-byte buffer to fill.</param>
    /// <returns>Error code (0 = success).</returns>
    byte ReadBlock(uint blockNumber, Span<byte> buffer);
    
    /// <summary>Writes a 512-byte block.</summary>
    /// <param name="blockNumber">Block number (0-based).</param>
    /// <param name="buffer">512-byte buffer to write.</param>
    /// <returns>Error code (0 = success).</returns>
    byte WriteBlock(uint blockNumber, ReadOnlySpan<byte> buffer);
    
    /// <summary>Formats the device.</summary>
    /// <returns>Error code (0 = success).</returns>
    byte Format();
    
    /// <summary>Gets device status.</summary>
    /// <param name="statusCode">Status code to retrieve.</param>
    /// <param name="buffer">Buffer to fill with status data.</param>
    /// <returns>Error code (0 = success).</returns>
    byte GetStatus(byte statusCode, Span<byte> buffer);
    
    /// <summary>Executes a control command.</summary>
    /// <param name="controlCode">Control code.</param>
    /// <param name="controlList">Control list data.</param>
    /// <returns>Error code (0 = success).</returns>
    byte Control(byte controlCode, ReadOnlySpan<byte> controlList);
    
    /// <summary>Initializes/resets the device.</summary>
    /// <returns>Error code (0 = success).</returns>
    byte Init();
}
```

### 9.3 Result Type

```csharp
/// <summary>
/// SmartPort error codes.
/// </summary>
public static class SmartPortError
{
    public const byte NoError = 0x00;
    public const byte BadCommand = 0x01;
    public const byte BadParamCount = 0x04;
    public const byte BusError = 0x11;
    public const byte BadUnit = 0x21;
    public const byte IoError = 0x27;
    public const byte NoDevice = 0x28;
    public const byte WriteProtected = 0x2B;
    public const byte DiskSwitched = 0x2E;
    public const byte DeviceOffline = 0x2F;
    public const byte VolumeTooLarge = 0x30;
}

/// <summary>
/// Represents the result of a SmartPort command handler.
/// </summary>
/// <param name="ResultCode">The result code (0 = success).</param>
/// <param name="CyclesConsumed">The number of cycles consumed by the operation.</param>
public readonly record struct SmartPortCommandHandlerResult(byte ResultCode, Cycle CyclesConsumed);
```

---

## 10. Implementation Notes

### 10.1 Disk Image Formats

Common disk image formats used with SmartPort:

| Format | Extension | Block Size | Notes                         |
|--------|-----------|------------|-------------------------------|
| 2IMG   | .2mg      | 512        | Includes metadata header      |
| ProDOS | .po       | 512        | Raw ProDOS-order blocks       |
| DOS    | .do       | 256?512    | Requires sector translation   |
| HDV    | .hdv      | 512        | Hard disk volume image        |
| DC42   | .dc       | 512        | DiskCopy 4.2 format           |

### 10.2 Block Addressing

For 2IMG, .po, and .hdv images:
```csharp
long fileOffset = blockNumber * 512L;
```

For .do (DOS-order) images, physical-to-logical sector interleaving is required:
```csharp
// DOS 3.3 to ProDOS sector translation
private static readonly byte[] DosToProDos = 
{
    0x0, 0xE, 0xD, 0xC, 0xB, 0xA, 0x9, 0x8,
    0x7, 0x6, 0x5, 0x4, 0x3, 0x2, 0x1, 0xF
};
```

### 10.3 Trap Handler Implementation

```csharp
/// <summary>
/// SmartPort entry point trap handler.
/// </summary>
public TrapResult HandleSmartPortCall(IMachine machine)
{
    ICpu cpu = machine.Cpu;
    IMemoryBus bus = machine.Bus;

    // Get return address from stack (points to inline parameters)
    ushort returnAddr = (ushort)(cpu.PopWord() + 1);
    
    // Read inline command and parameter list pointer
    byte command = bus.Read8(returnAddr);
    ushort paramList = bus.Read16((ushort)(returnAddr + 1));
    
    // Advance return address past inline data (3 bytes)
    returnAddr += 3;
    cpu.PushWord((ushort)(returnAddr - 1));
    
    // Read parameter count and unit number
    byte paramCount = bus.Read8(paramList);
    byte unitNumber = bus.Read8((ushort)(paramList + 1));
    
    // Validate unit number
    if (unitNumber > MaxDevices && command != 0x00)
    {
        cpu.A = SmartPortError.BadUnit;
        cpu.SetCarry(true);
        return TrapResult.Success(10, TrapReturnMethod.Rts);
    }
    
    // Execute command
    SmartPortCommandHandlerResult result = command switch
    {
        0x00 => HandleStatus(unitNumber, paramList, bus),
        0x01 => HandleReadBlock(unitNumber, paramList, bus),
        0x02 => HandleWriteBlock(unitNumber, paramList, bus),
        0x03 => HandleFormat(unitNumber, paramList, bus),
        0x04 => HandleControl(unitNumber, paramList, bus),
        0x05 => HandleInit(unitNumber, paramList, bus),
        _ => new SmartPortCommandHandlerResult(SmartPortError.BadCommand, 10)
    };
    
    // Set result in accumulator and carry flag
    cpu.A = result.ResultCode;
    cpu.SetCarry(result.ResultCode != 0);
    
    return TrapResult.Success(result.CyclesConsumed, TrapReturnMethod.Rts);
}

private SmartPortCommandHandlerResult HandleReadBlock(byte unitNumber, ushort paramList, IMemoryBus bus)
{
    var device = GetDevice(unitNumber);
    if (device == null)
        return SmartPortError.NoDevice;
    
    ushort bufferPtr = bus.Read16((ushort)(paramList + 2));
    uint blockNumber = bus.Read8((ushort)(paramList + 4)) |
                       ((uint)bus.Read8((ushort)(paramList + 5)) << 8) |
                       ((uint)bus.Read8((ushort)(paramList + 6)) << 16);
    
    Span<byte> buffer = stackalloc byte[512];
    byte resultCode = device.ReadBlock(blockNumber, buffer);
    
    if (resultCode == SmartPortError.NoError)
    {
        // Copy block to guest memory
        for (int i = 0; i < 512; i++)
            bus.Write8((ushort)(bufferPtr + i), buffer[i]);
    }
    
    return new SmartPortCommandHandlerResult(resultCode, 50000); // 50000 cycles for Read
}
```

### 10.4 Timing Considerations

SmartPort operations should model realistic timing:

| Operation      | Typical Cycles | Notes                          |
|----------------|----------------|--------------------------------|
| STATUS         | 500-2000       | Quick device query             |
| READ BLOCK     | 20000-50000    | ~20-50ms for floppy            |
| WRITE BLOCK    | 20000-50000    | ~20-50ms for floppy            |
| FORMAT         | 1000000+       | Full disk format               |
| Motor spin-up  | 500000         | ~0.5 second                    |

---

## 11. Bus Architecture Integration

### 11.1 SmartPort as IPeripheral

```csharp
public sealed class SmartPortController : IPeripheral
{
    private readonly List<ISmartPortDevice> _devices = [];
    
    /// <inheritdoc/>
    public string Name => "SmartPort Controller";
    
    /// <inheritdoc/>
    public string DeviceType => "SmartPort";
    
    /// <inheritdoc/>
    public int SlotNumber { get; set; }
    
    /// <inheritdoc/>
    public IBusTarget? SlotRomRegion { get; }
    
    /// <inheritdoc/>
    public IBusTarget? ExpansionRomRegion { get; }
    
    public SmartPortController()
    {
        SlotRomRegion = new SmartPortSlotRom(this);
        ExpansionRomRegion = new SmartPortExpansionRom(this);
    }
}
```

### 11.2 Slot ROM Identification

SmartPort slot ROM must contain identifying bytes:

```
$CnFF = $00     (SmartPort signature)
$CnFE = $FF     (Usually)
$CnFB = $00     (SmartPort signature)
$Cn07 = $00     (ProDOS block device signature)
$Cn05 = $03     (ProDOS status byte - 3.5" drive)
$CnFC = SmartPort entry point offset
```

### 11.3 Device Registry Integration

```csharp
public void RegisterDevices(IDeviceRegistry registry, int slot)
{
    // Register controller
    registry.Register(
        DevicePageId.Create(DevicePageClass.Storage, (byte)slot, 0),
        kind: "SmartPortController",
        name: $"SmartPort (Slot {slot})",
        wiringPath: $"main/slots/{slot}/smartport");
    
    // Register each device
    for (int unit = 1; unit <= _devices.Count; unit++)
    {
        var device = _devices[unit - 1];
        registry.Register(
            DevicePageId.Create(DevicePageClass.Storage, (byte)slot, (byte)unit),
            kind: $"SmartPort{device.DeviceType:X2}",
            name: device.DeviceName,
            wiringPath: $"main/slots/{slot}/smartport/unit{unit}");
    }
}
```

---

## Document History

| Version | Date       | Changes                                    |
|---------|------------|--------------------------------------------|
| 1.0     | 2025-12-28 | Initial specification                      |
| 2.0     | 2025-06-11 | Major revision with accurate information   |

---

## References

1. Apple IIgs Hardware Reference Manual, Chapter 7: SmartPort
2. Apple II SmartPort Technical Notes #1-7
3. Apple IIc Technical Reference Manual
4. ProDOS 8 Technical Reference Manual

---

## Appendix A: Complete Status Byte Definitions

### A.1 Device Status Byte (returned by STATUS code $00)

```
Bit 7 (DEVTYPE):  1 = Block device
                  0 = Character device

Bit 6 (WRITABLE): 1 = Device can be written to
                  0 = Read-only device

Bit 5 (READABLE): 1 = Device can be read from
                  0 = Write-only device (rare)

Bit 4 (ONLINE):   1 = Media present and ready
                  0 = No media or not ready

Bit 3 (FORMAT):   1 = Format command supported
                  0 = Format not supported

Bit 2 (PROTECT):  1 = Media is write-protected
                  0 = Media is writable

Bit 1 (INTR):     1 = Device is interrupting
                  0 = No pending interrupt

Bit 0 (OPEN):     1 = Device has been opened
                  0 = Device is closed
```

### A.2 Typical Status Byte Values

| Value | Meaning                                              |
|-------|------------------------------------------------------|
| $F8   | Block device, R/W, online, formattable               |
| $FC   | Block device, R/W, online, formattable, protected    |
| $E8   | Block device, R/W, offline (no disk)                 |
| $D8   | Block device, read-only, online                      |
| $18   | Character device, R/W, online                        |

---

## Appendix B: Example Device Implementation

### B.1 RAM Disk Device

```csharp
/// <summary>
/// A simple RAM disk SmartPort device.
/// </summary>
public sealed class SmartPortRamDisk : ISmartPortDevice
{
    private readonly byte[] _storage;
    
    public byte DeviceType => 0x00;  // Memory expansion
    public byte DeviceSubtype => 0x00;
    public string DeviceName => "RAMDISK";
    public uint BlockCount { get; }
    public ushort FirmwareVersion => 0x0100;
    public bool IsOnline => true;
    public bool IsWriteProtected => false;
    public bool DiskSwitched => false;
    
    public SmartPortRamDisk(uint blockCount)
    {
        BlockCount = blockCount;
        _storage = new byte[blockCount * 512];
    }
    
    public byte ReadBlock(uint blockNumber, Span<byte> buffer)
    {
        if (blockNumber >= BlockCount)
            return SmartPortError.VolumeTooLarge;
        
        _storage.AsSpan((int)(blockNumber * 512), 512).CopyTo(buffer);
        return SmartPortError.NoError;
    }
    
    public byte WriteBlock(uint blockNumber, ReadOnlySpan<byte> buffer)
    {
        if (blockNumber >= BlockCount)
            return SmartPortError.VolumeTooLarge;
        
        buffer.CopyTo(_storage.AsSpan((int)(blockNumber * 512), 512));
        return SmartPortError.NoError;
    }
    
    public byte Format()
    {
        Array.Clear(_storage);
        return SmartPortError.NoError;
    }
    
    public byte GetStatus(byte statusCode, Span<byte> buffer)
    {
        return statusCode switch
        {
            0x00 => GetDeviceStatus(buffer),
            0x03 => GetDIB(buffer),
            _ => SmartPortError.BadCommand
        };
    }
    
    private byte GetDeviceStatus(Span<byte> buffer)
    {
        buffer[0] = 0xF8;  // Block, R/W, online, formattable
        buffer[1] = (byte)(BlockCount & 0xFF);
        buffer[2] = (byte)((BlockCount >> 8) & 0xFF);
        buffer[3] = (byte)((BlockCount >> 16) & 0xFF);
        return SmartPortError.NoError;
    }
    
    private byte GetDIB(Span<byte> buffer)
    {
        GetDeviceStatus(buffer);
        
        // Device name
        buffer[4] = 7;  // Length
        "RAMDISK".AsSpan().CopyTo(MemoryMarshal.Cast<byte, char>(buffer.Slice(5, 16)));
        
        // Device type
        buffer[21] = DeviceType;
        buffer[22] = DeviceSubtype;
        
        // Version
        buffer[23] = (byte)(FirmwareVersion & 0xFF);
        buffer[24] = (byte)((FirmwareVersion >> 8) & 0xFF);
        
        return SmartPortError.NoError;
    }
    
    public byte Control(byte controlCode, ReadOnlySpan<byte> controlList)
    {
        return controlCode switch
        {
            0x00 => Init(),  // Reset
            _ => SmartPortError.BadCommand
        };
    }
    
    public byte Init()
    {
        return SmartPortError.NoError;
    }
}
