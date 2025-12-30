# Apple Desktop Bus (ADB) Specification

## Document Information

| Field        | Value                                         |
|--------------|-----------------------------------------------|
| Version      | 1.0                                           |
| Date         | 2025-12-28                                    |
| Status       | Initial Draft                                 |
| Applies To   | PocketGS (Apple IIgs)                         |

---

## 1. Overview

Apple Desktop Bus (ADB) is a low-speed serial bus used by the Apple IIgs (and later Macintosh
computers) for connecting input devices such as keyboards and mice. ADB was innovative for
its time, allowing daisy-chaining of up to 16 devices on a single bus.

### 1.1 Why ADB Matters

Prior to ADB, each input device required its own dedicated port. ADB provided:

1. **Single connector**: One port for all input devices
2. **Daisy-chaining**: Devices connect in series
3. **Self-identifying**: Devices report their type and capabilities
4. **Hot-pluggable**: Devices can be added/removed while powered (with care)
5. **Low power**: Devices draw power from the bus

### 1.2 ADB Timeline

| Year | Milestone                                      |
|------|------------------------------------------------|
| 1986 | Introduced with Apple IIgs                     |
| 1987 | Adopted by Macintosh SE and Macintosh II       |
| 1999 | Replaced by USB in new Apple products          |

---

## 2. Hardware Characteristics

### 2.1 Physical Specifications

| Parameter        | Value                                      |
|------------------|--------------------------------------------|
| Signal type      | Open-collector, active low                 |
| Voltage          | 5V nominal (4.5V-5.5V)                     |
| Data rate        | ~10 kbit/s                                 |
| Max cable length | 5 meters (total bus length)                |
| Max devices      | 16 (limited by 4-bit addressing)           |
| Connector        | Mini-DIN 4-pin                             |

### 2.2 Connector Pinout

| Pin | Signal  | Description                              |
|-----|---------|------------------------------------------|
| 1   | ADB     | Data (active low, open collector)        |
| 2   | PSW     | Power-on switch                          |
| 3   | +5V     | Power supply                             |
| 4   | GND     | Ground                                   |

---

## 3. Protocol Overview

### 3.1 Communication Model

ADB uses a command/response protocol where the host (IIgs) initiates all communication:

```
????????????                      ????????????
?   Host   ????? Command ??????????  Device  ?
?  (IIgs)  ????? Response ?????????          ?
????????????                      ????????????
```

### 3.2 Signal Timing

The ADB protocol uses precise timing for bit encoding:

| Element          | Duration (?s)        |
|------------------|----------------------|
| Attention        | 560-1040             |
| Sync             | 70                   |
| Bit cell         | 100                  |
| Bit 0            | Low 65, High 35      |
| Bit 1            | Low 35, High 65      |
| Stop bit         | Low 65, High >70     |
| Srq (optional)   | Low 140-260          |

### 3.3 Command Structure

Each ADB command consists of 8 bits:

```
Bits 7-4: Device address (0-15)
Bits 3-2: Command type
Bits 1-0: Register number

Command types:
  00 = Reserved
  01 = Flush
  10 = Reserved (Listen)
  11 = Talk
```

---

## 4. Device Addressing

### 4.1 Default Addresses

Each device type has a default address at power-on:

| Address | Device Type                                  |
|---------|----------------------------------------------|
| 0       | Reserved (host)                              |
| 1       | Reserved                                     |
| 2       | Keyboard (encoded)                           |
| 3       | Mouse/pointing device                        |
| 4       | Tablet                                       |
| 5       | Modem                                        |
| 6       | Reserved                                     |
| 7       | AppleTalk                                    |
| 8-14    | Available for assignment                     |
| 15      | Reserved (broadcast)                         |

### 4.2 Address Resolution

At startup, the host performs address resolution to handle devices with the same default
address:

1. Host sends Talk Register 3 to each default address
2. If collision (multiple devices respond), host reassigns one device
3. Process repeats until all devices have unique addresses

---

## 5. Device Registers

Each ADB device has four 2-byte registers (Register 0-3):

### 5.1 Register 0: Device Data

Contains the primary data from the device (keystrokes, mouse movements).

**Keyboard Register 0**:
```
Byte 0: Key 1 code (or $FF if none)
Byte 1: Key 2 code (or $FF if none)

Key code format:
  Bit 7: Key state (0 = down, 1 = up)
  Bits 6-0: Key code
```

**Mouse Register 0**:
```
Byte 0: Button + Y delta
  Bit 7: Button state (0 = down)
  Bits 6-0: Y delta (signed, -64 to +63)

Byte 1: X delta
  Bit 7: Reserved (0)
  Bits 6-0: X delta (signed, -64 to +63)
```

### 5.2 Register 1: Device-Specific

Contains device-specific configuration or data.

### 5.3 Register 2: Device-Specific

Contains additional device-specific data.

### 5.4 Register 3: Device Identification

Contains the device handler ID and current address:

```
Byte 0: Device handler ID
Byte 1: Current address and status
  Bits 7-4: Current device address
  Bits 3-0: Reserved/status flags
```

---

## 6. ADB Commands

### 6.1 Talk Command

Requests data from a device register.

```
Command: %AAAA1100 | RR
         ?????       ???? Register number (0-3)
           ?????????????? Device address (0-15)
```

**Response**: 2 bytes of register data (or none if register empty)

### 6.2 Listen Command

Sends data to a device register.

```
Command: %AAAA1000 | RR
Data:    2 bytes to write to register
```

**Response**: None (device acknowledges by releasing bus)

### 6.3 Flush Command

Clears the device's pending data.

```
Command: %AAAA0100
```

**Response**: None

### 6.4 SendReset Command

Resets all devices on the bus.

```
Command: Attention pulse > 3ms
```

**Response**: All devices reset to default state

---

## 7. Service Request (SRQ)

When a device has data ready, it can signal the host:

### 7.1 SRQ Mechanism

1. Device asserts SRQ during the stop bit of any command
2. Host detects the extended low pulse
3. Host polls devices to find the source

### 7.2 SRQ Detection

```csharp
private bool DetectSrq()
{
    // SRQ is indicated by a low pulse lasting 140-260?s
    // during the stop bit (which should be <70?s low)
    return _busLowDuration > 140;
}
```

---

## 8. IIgs ADB Implementation

### 8.1 ADB Microcontroller

The Apple IIgs uses a dedicated microcontroller (GLU) for ADB communication:

- **GLU (General Logic Unit)**: Handles low-level ADB protocol
- **Firmware**: Manages device polling and data buffering

### 8.2 Memory-Mapped Interface

The IIgs exposes ADB through the following registers:

| Address | Register    | Description                          |
|---------|-------------|--------------------------------------|
| $C026   | ADB Data    | Read/write ADB data byte             |
| $C027   | ADB Status  | ADB command/status register          |

### 8.3 ADB Commands via GLU

To send an ADB command from IIgs software:

```assembly
; Send Talk Register 0 to device 3 (mouse)
    LDA #$0C        ; Command: Device 3, Talk, Reg 0
    STA $C027       ; Write command to ADB
WaitCmd:
    LDA $C027       ; Read status
    BMI WaitCmd     ; Wait for command complete
    LDA $C026       ; Read first data byte
    STA MouseY
    LDA $C026       ; Read second data byte  
    STA MouseX
```

### 8.4 Interrupt Support

The IIgs can generate interrupts for ADB events:

```csharp
// Enable ADB SRQ interrupt
memory.Write(0xC025, memory.Read(0xC025) | 0x04);
```

---

## 9. Keyboard Implementation

### 9.1 Apple IIgs Keyboard

The standard IIgs keyboard is an ADB device at address 2:

**Key Codes (partial list)**:

| Code | Key        | Code | Key        |
|------|------------|------|------------|
| $00  | A          | $24  | 5          |
| $01  | S          | $25  | 6          |
| $02  | D          | $26  | 7          |
| $03  | F          | $27  | 8          |
| $04  | H          | $28  | 9          |
| $05  | G          | $29  | 0          |
| $06  | Z          | $2A  | -          |
| $07  | X          | $2B  | =          |
| $08  | C          | $2C  | [          |
| $09  | V          | $2D  | ]          |
| ...  | ...        | ...  | ...        |
| $35  | Escape     | $3A  | Option     |
| $36  | Control    | $3B  | Apple/Cmd  |
| $37  | Shift (L)  | $3C  | Shift (R)  |
| $38  | Caps Lock  | $3D  | Space      |
| $39  | Tab        | $3E  | Return     |

### 9.2 Keyboard Handler

```csharp
/// <summary>
/// ADB keyboard device implementation.
/// </summary>
public class AdbKeyboard : IAdbDevice
{
    private readonly Queue<byte> _keyBuffer = new();
    private byte _modifiers;
    
    public byte DefaultAddress => 2;
    public byte HandlerId => 0x01;  // Standard ADB keyboard
    
    public void KeyDown(byte keyCode)
    {
        // Key down: clear bit 7
        _keyBuffer.Enqueue((byte)(keyCode & 0x7F));
        _hasPendingData = true;
    }
    
    public void KeyUp(byte keyCode)
    {
        // Key up: set bit 7
        _keyBuffer.Enqueue((byte)(keyCode | 0x80));
        _hasPendingData = true;
    }
    
    public byte[] TalkRegister0()
    {
        byte key1 = _keyBuffer.Count > 0 ? _keyBuffer.Dequeue() : (byte)0xFF;
        byte key2 = _keyBuffer.Count > 0 ? _keyBuffer.Dequeue() : (byte)0xFF;
        _hasPendingData = _keyBuffer.Count > 0;
        return [key1, key2];
    }
}
```

---

## 10. Mouse Implementation

### 10.1 Apple IIgs Mouse

The standard IIgs mouse is an ADB device at address 3:

### 10.2 Mouse Handler

```csharp
/// <summary>
/// ADB mouse device implementation.
/// </summary>
public class AdbMouse : IAdbDevice
{
    private int _deltaX, _deltaY;
    private bool _buttonDown;
    
    public byte DefaultAddress => 3;
    public byte HandlerId => 0x01;  // Standard ADB mouse
    
    public void MoveDelta(int dx, int dy)
    {
        // Accumulate delta (clamped to -64..+63)
        _deltaX = Math.Clamp(_deltaX + dx, -64, 63);
        _deltaY = Math.Clamp(_deltaY + dy, -64, 63);
        _hasPendingData = true;
    }
    
    public void SetButton(bool down)
    {
        _buttonDown = down;
        _hasPendingData = true;
    }
    
    public byte[] TalkRegister0()
    {
        // Byte 0: Button + Y delta
        byte b0 = (byte)((_buttonDown ? 0 : 0x80) | (_deltaY & 0x7F));
        
        // Byte 1: X delta (bits 0-6)
        byte b1 = (byte)(_deltaX & 0x7F);
        
        // Clear accumulated delta
        _deltaX = 0;
        _deltaY = 0;
        _hasPendingData = false;
        
        return [b0, b1];
    }
}
```

---

## 11. ADB Controller Interface

```csharp
/// <summary>
/// Interface for an ADB device.
/// </summary>
public interface IAdbDevice
{
    /// <summary>Gets the default address for this device type.</summary>
    byte DefaultAddress { get; }
    
    /// <summary>Gets the current assigned address.</summary>
    byte Address { get; set; }
    
    /// <summary>Gets the device handler ID.</summary>
    byte HandlerId { get; }
    
    /// <summary>Gets whether the device has pending data.</summary>
    bool HasPendingData { get; }
    
    /// <summary>Handles a Talk command.</summary>
    byte[]? Talk(int register);
    
    /// <summary>Handles a Listen command.</summary>
    void Listen(int register, byte[] data);
    
    /// <summary>Handles a Flush command.</summary>
    void Flush();
    
    /// <summary>Resets the device to default state.</summary>
    void Reset();
}

/// <summary>
/// Interface for the ADB controller.
/// </summary>
public interface IAdbController : IScheduledDevice
{
    /// <summary>Gets all connected devices.</summary>
    IReadOnlyList<IAdbDevice> Devices { get; }
    
    /// <summary>Connects a device to the bus.</summary>
    void Connect(IAdbDevice device);
    
    /// <summary>Disconnects a device from the bus.</summary>
    void Disconnect(IAdbDevice device);
    
    /// <summary>Sends a command to a device.</summary>
    AdbResult SendCommand(byte command, byte[]? data = null);
    
    /// <summary>Polls for pending data from any device.</summary>
    (byte address, byte[] data)? Poll();
    
    /// <summary>Sends a bus reset.</summary>
    void SendReset();
    
    /// <summary>Gets whether an SRQ is pending.</summary>
    bool SrqPending { get; }
}

/// <summary>
/// Result of an ADB operation.
/// </summary>
public readonly record struct AdbResult(
    bool Success,
    byte[]? Data
);
```

---

## 12. Implementation Notes

### 12.1 Polling Strategy

The IIgs firmware polls ADB devices during vertical blank:

```csharp
public void OnVBlank()
{
    // Poll keyboard first (most important)
    var kbdData = PollDevice(_keyboardAddress, 0);
    if (kbdData != null)
        ProcessKeyboardData(kbdData);
    
    // Poll mouse
    var mouseData = PollDevice(_mouseAddress, 0);
    if (mouseData != null)
        ProcessMouseData(mouseData);
}
```

### 12.2 Address Resolution

At startup, perform address resolution:

```csharp
public void ResolveAddresses()
{
    // For each default address that has multiple devices
    for (int addr = 2; addr <= 14; addr++)
    {
        var devices = _devices.Where(d => d.Address == addr).ToList();
        
        // Reassign duplicates to free addresses
        for (int i = 1; i < devices.Count; i++)
        {
            int freeAddr = FindFreeAddress();
            if (freeAddr >= 0)
                devices[i].Address = (byte)freeAddr;
        }
    }
}
```

### 12.3 Timing Accuracy

For most emulation purposes, exact ADB timing is not critical. The important aspects are:

1. Command/response ordering
2. Correct data formatting
3. SRQ generation when data is pending

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |

---

## Appendix A: Bus Architecture Integration

This appendix provides implementation guidance for integrating Apple Desktop Bus (ADB)
with the emulator's bus architecture.

### A.1 ADB Controller as IBusTarget

The ADB controller registers implement `IBusTarget`:

```csharp
/// <summary>
/// ADB controller registers for Apple IIgs.
/// </summary>
public sealed class AdbControllerTarget : IBusTarget
{
    private readonly IAdbController _controller;
    private readonly Queue<byte> _dataBuffer = new();
    private byte _status;
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.SideEffects;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        byte offset = (byte)(physicalAddress & 0xFF);
        
        return offset switch
        {
            0x26 => ReadDataRegister(access),   // ADB Data
            0x27 => ReadStatusRegister(access), // ADB Status
            _ => 0x00
        };
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return;
        
        byte offset = (byte)(physicalAddress & 0xFF);
        
        switch (offset)
        {
            case 0x26:
                WriteDataRegister(value);
                break;
            case 0x27:
                WriteCommandRegister(value);
                break;
        }
    }
    
    private byte ReadDataRegister(in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return _dataBuffer.Count > 0 ? _dataBuffer.Peek() : (byte)0;
        
        return _dataBuffer.Count > 0 ? _dataBuffer.Dequeue() : (byte)0;
    }
    
    private byte ReadStatusRegister(in BusAccess access)
    {
        byte status = _status;
        
        // Bit 7: Command/data pending
        if (_dataBuffer.Count > 0)
            status |= 0x80;
        
        // Bit 5: SRQ pending
        if (_controller.SrqPending)
            status |= 0x20;
        
        return status;
    }
    
    private void WriteCommandRegister(byte command)
    {
        // Send command to ADB controller
        var result = _controller.SendCommand(command);
        
        if (result.Success && result.Data != null)
        {
            foreach (byte b in result.Data)
                _dataBuffer.Enqueue(b);
        }
        
        // Update status based on result
        _status = result.Success ? (byte)0x00 : (byte)0x40;
    }
}
```

### A.2 Composite Page Integration

ADB registers are part of the IIgs I/O page:

```csharp
public sealed class IIgsIOPage : ICompositeTarget
{
    private readonly AdbControllerTarget _adbTarget;
    
    /// <inheritdoc/>
    public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        return offset switch
        {
            0x26 or 0x27 => _adbTarget,  // ADB registers
            // ...other handlers...
            _ => null
        };
    }
    
    /// <inheritdoc/>
    public RegionTag GetSubRegionTag(Addr offset)
    {
        return offset switch
        {
            0x26 or 0x27 => RegionTag.AdbController,
            _ => RegionTag.Unknown
        };
    }
}
```

### A.3 Scheduler Integration for Polling

ADB polling is scheduled during VBlank:

```csharp
public sealed class AdbController : IAdbController, ISchedulable
{
    private readonly IScheduler _scheduler;
    private readonly ISignalBus _signals;
    private readonly int _deviceId;
    
    private const ulong PollIntervalCycles = 17030;  // Once per frame at 60Hz
    
    /// <inheritdoc/>
    public void Initialize(IEventContext context)
    {
        _scheduler = context.Scheduler;
        _signals = context.Signals;
        
        // Start polling during VBlank
        _scheduler.ScheduleAfter(this, PollIntervalCycles);
    }
    
    /// <inheritdoc/>
    public ulong Execute(ulong currentCycle)
    {
        // Poll all devices for pending data
        foreach (var device in _devices)
        {
            if (device.HasPendingData)
            {
                // Queue data for next status read
                var data = device.Talk(0);
                if (data != null)
                    QueueDeviceData(device.Address, data);
            }
        }
        
        // Check for SRQ and generate interrupt if enabled
        if (SrqPending && _interruptsEnabled)
            _signals.Assert(SignalLine.IRQ, _deviceId);
        
        // Schedule next poll
        _scheduler.ScheduleAfter(this, PollIntervalCycles);
        return 0;  // Polling doesn't consume CPU cycles
    }
}
```

### A.4 Keyboard Device Implementation

```csharp
/// <summary>
/// ADB keyboard device implementing IAdbDevice.
/// </summary>
public sealed class AdbKeyboard : IAdbDevice
{
    private readonly Queue<byte> _keyBuffer = new(32);
    
    /// <inheritdoc/>
    public byte DefaultAddress => 2;
    
    /// <inheritdoc/>
    public byte Address { get; set; } = 2;
    
    /// <inheritdoc/>
    public byte HandlerId => 0x01;
    
    /// <inheritdoc/>
    public bool HasPendingData => _keyBuffer.Count > 0;
    
    /// <summary>
    /// Called by input system when a key is pressed.
    /// </summary>
    public void KeyDown(byte adbKeyCode)
    {
        // Key down: clear bit 7
        _keyBuffer.Enqueue((byte)(adbKeyCode & 0x7F));
    }
    
    /// <summary>
    /// Called by input system when a key is released.
    /// </summary>
    public void KeyUp(byte adbKeyCode)
    {
        // Key up: set bit 7
        _keyBuffer.Enqueue((byte)(adbKeyCode | 0x80));
    }
    
    /// <inheritdoc/>
    public byte[]? Talk(int register)
    {
        if (register != 0)
            return GetRegisterData(register);
        
        if (_keyBuffer.Count == 0)
            return null;
        
        byte key1 = _keyBuffer.Dequeue();
        byte key2 = _keyBuffer.Count > 0 ? _keyBuffer.Dequeue() : (byte)0xFF;
        
        return [key1, key2];
    }
    
    /// <inheritdoc/>
    public void Listen(int register, byte[] data)
    {
        if (register == 3 && data.Length >= 2)
        {
            // Address reassignment
            Address = (byte)((data[1] >> 4) & 0x0F);
        }
    }
    
    /// <inheritdoc/>
    public void Flush() => _keyBuffer.Clear();
    
    /// <inheritdoc/>
    public void Reset()
    {
        Address = DefaultAddress;
        _keyBuffer.Clear();
    }
}
```

### A.5 Mouse Device Implementation

```csharp
/// <summary>
/// ADB mouse device implementing IAdbDevice.
/// </summary>
public sealed class AdbMouse : IAdbDevice
{
    private int _deltaX, _deltaY;
    private bool _buttonDown;
    private readonly object _lock = new();
    
    /// <inheritdoc/>
    public byte DefaultAddress => 3;
    
    /// <inheritdoc/>
    public byte Address { get; set; } = 3;
    
    /// <inheritdoc/>
    public byte HandlerId => 0x01;
    
    /// <inheritdoc/>
    public bool HasPendingData
    {
        get
        {
            lock (_lock)
                return _deltaX != 0 || _deltaY != 0;
        }
    }
    
    /// <summary>
    /// Called by input system when mouse moves.
    /// </summary>
    public void MoveDelta(int dx, int dy)
    {
        lock (_lock)
        {
            _deltaX = Math.Clamp(_deltaX + dx, -64, 63);
            _deltaY = Math.Clamp(_deltaY + dy, -64, 63);
        }
    }
    
    /// <summary>
    /// Called by input system when button state changes.
    /// </summary>
    public void SetButton(bool down) => _buttonDown = down;
    
    /// <inheritdoc/>
    public byte[]? Talk(int register)
    {
        if (register != 0)
            return GetRegisterData(register);
        
        lock (_lock)
        {
            if (_deltaX == 0 && _deltaY == 0 && !_buttonDown)
                return null;
            
            // Byte 0: Button (bit 7 inverted) + Y delta (bits 0-6)
            byte b0 = (byte)((_buttonDown ? 0 : 0x80) | (_deltaY & 0x7F));
            
            // Byte 1: X delta (bits 0-6)
            byte b1 = (byte)(_deltaX & 0x7F);
            
            // Clear accumulated delta
            _deltaX = 0;
            _deltaY = 0;
            
            return [b0, b1];
        }
    }
    
    /// <inheritdoc/>
    public void Flush()
    {
        lock (_lock)
        {
            _deltaX = 0;
            _deltaY = 0;
        }
    }
    
    /// <inheritdoc/>
    public void Reset()
    {
        Address = DefaultAddress;
        Flush();
        _buttonDown = false;
    }
}
```

### A.6 Signal Bus Integration for Interrupts

```csharp
public sealed class AdbController : IAdbController
{
    private readonly ISignalBus _signals;
    private readonly int _deviceId;
    private bool _interruptsEnabled;
    
    public void EnableInterrupts(bool enabled)
    {
        _interruptsEnabled = enabled;
        
        if (!enabled)
            _signals.Deassert(SignalLine.IRQ, _deviceId);
    }
    
    private void CheckAndRaiseInterrupt()
    {
        if (_interruptsEnabled && SrqPending)
        {
            _signals.Assert(SignalLine.IRQ, _deviceId);
        }
    }
    
    public void AcknowledgeInterrupt()
    {
        _signals.Deassert(SignalLine.IRQ, _deviceId);
    }
}
```

### A.7 Device Registry

```csharp
public void RegisterAdbDevices(IDeviceRegistry registry)
{
    // ADB Controller
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.Input, instance: 0, page: 0),
        kind: "AdbController",
        name: "Apple Desktop Bus Controller",
        wiringPath: "main/adb/controller");
    
    // Keyboard
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.Input, instance: 0, page: 1),
        kind: "AdbKeyboard",
        name: "ADB Keyboard",
        wiringPath: "main/adb/keyboard");
    
    // Mouse
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.Input, instance: 0, page: 2),
        kind: "AdbMouse",
        name: "ADB Mouse",
        wiringPath: "main/adb/mouse");
}
```

### A.8 Host Input Mapping

Map host input events to ADB devices:

```csharp
public sealed class AdbInputMapper
{
    private readonly AdbKeyboard _keyboard;
    private readonly AdbMouse _mouse;
    private readonly Dictionary<Key, byte> _keyMap;
    
    public void HandleKeyDown(Key key)
    {
        if (_keyMap.TryGetValue(key, out byte adbCode))
            _keyboard.KeyDown(adbCode);
    }
    
    public void HandleKeyUp(Key key)
    {
        if (_keyMap.TryGetValue(key, out byte adbCode))
            _keyboard.KeyUp(adbCode);
    }
    
    public void HandleMouseMove(int dx, int dy)
    {
        _mouse.MoveDelta(dx, dy);
    }
    
    public void HandleMouseButton(bool down)
    {
        _mouse.SetButton(down);
    }
    
    private void InitializeKeyMap()
    {
        _keyMap = new Dictionary<Key, byte>
        {
            { Key.A, 0x00 },
            { Key.S, 0x01 },
            { Key.D, 0x02 },
            { Key.F, 0x03 },
            // ... complete key mapping
            { Key.Escape, 0x35 },
            { Key.LeftControl, 0x36 },
            { Key.LeftShift, 0x37 },
            { Key.CapsLock, 0x38 },
            { Key.Tab, 0x39 },
            { Key.Space, 0x3D },
            { Key.Enter, 0x3E }
        };
    }
}
