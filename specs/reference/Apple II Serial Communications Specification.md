# Apple II Serial Communications Specification

## Document Information

| Field        | Value                                                  |
|--------------|--------------------------------------------------------|
| Version      | 1.0                                                    |
| Date         | 2025-12-28                                             |
| Status       | Initial Draft                                          |
| Applies To   | Pocket2e, Pocket2c, PocketGS                           |

---

## 1. Overview

Serial communication on the Apple II family evolved across models:

- **Apple II/II+**: Required expansion card (Apple Communications Card, Super Serial Card)
- **Apple IIc**: Two built-in serial ports
- **Apple IIgs**: Two built-in serial ports with enhanced capabilities

This specification covers the Super Serial Card (SSC), Apple IIc serial ports, and Apple
IIgs serial implementation.

### 1.1 Serial Port Capabilities

| Feature              | SSC           | IIc Port 1/2  | IIgs Port 1/2 |
|----------------------|---------------|---------------|---------------|
| Max baud rate        | 19200         | 19200         | 57600         |
| Hardware handshaking | CTS/DTR       | CTS/DTR       | CTS/DTR/RTS   |
| Interrupts           | Yes           | Yes           | Yes           |
| DMA                  | No            | No            | No            |
| Connector            | DB-25         | DIN-5         | DIN-8         |

---

## 2. Super Serial Card (Slot-Based)

The Super Serial Card (SSC) is the standard serial interface for the Apple IIe.

### 2.1 Hardware Overview

The SSC uses a 6551 ACIA (Asynchronous Communications Interface Adapter) chip:

- **ACIA address**: Base + slot offset ($C0n0-$C0nF where n = slot + 8)
- **ROM address**: $Cn00-$CnFF (256 bytes)
- **Expansion ROM**: $C800-$CFFF (2KB when selected)

### 2.2 Register Map

For SSC in slot 2 ($C0A0 base):

| Offset | Register        | Read/Write | Description                    |
|--------|-----------------|------------|--------------------------------|
| +$00   | Data            | R/W        | Transmit/Receive data          |
| +$01   | Status          | R          | Status register                |
| +$01   | Program Reset   | W          | Software reset                 |
| +$02   | Command         | R/W        | Command register               |
| +$03   | Control         | R/W        | Control register               |

### 2.3 Status Register ($C0A1)

| Bit | Name      | Meaning when set (1)                    |
|-----|-----------|-----------------------------------------|
| 7   | IRQ       | Interrupt pending                       |
| 6   | DSR       | Data Set Ready signal active            |
| 5   | DCD       | Data Carrier Detect signal active       |
| 4   | TDRE      | Transmit Data Register Empty            |
| 3   | RDRF      | Receive Data Register Full              |
| 2   | OVRN      | Overrun error occurred                  |
| 1   | FE        | Framing error occurred                  |
| 0   | PE        | Parity error occurred                   |

### 2.4 Command Register ($C0A2)

| Bit | Name      | Function                                |
|-----|-----------|-----------------------------------------|
| 7-5 | Parity    | Parity control (see table)              |
| 4   | REM       | Receiver echo mode                      |
| 3-2 | TIC       | Transmit interrupt control              |
| 1   | RIRD      | Receive interrupt request disable       |
| 0   | DTR       | Data Terminal Ready control             |

**Parity control**:
| Bits 7-5 | Parity Mode          |
|----------|----------------------|
| 000      | Odd parity           |
| 001      | Even parity          |
| 010      | Mark parity          |
| 011      | Space parity         |
| 1xx      | No parity            |

**Transmit interrupt control**:
| Bits 3-2 | Mode                           |
|----------|--------------------------------|
| 00       | Transmit interrupt disabled    |
| 01       | Transmit interrupt enabled     |
| 10       | RTS low, transmit interrupt    |
| 11       | RTS low, break on transmit     |

### 2.5 Control Register ($C0A3)

| Bit | Name      | Function                                |
|-----|-----------|-----------------------------------------|
| 7   | SBR       | Stop bit (0=1 stop, 1=2 stops)          |
| 6-5 | WL        | Word length (see table)                 |
| 4   | RCS       | Receiver clock source                   |
| 3-0 | SBR       | Baud rate (see table)                   |

**Word length**:
| Bits 6-5 | Data Bits |
|----------|-----------|
| 00       | 8 bits    |
| 01       | 7 bits    |
| 10       | 6 bits    |
| 11       | 5 bits    |

**Baud rate**:
| Bits 3-0 | Baud Rate |
|----------|-----------|
| 0000     | External  |
| 0001     | 50        |
| 0010     | 75        |
| 0011     | 109.92    |
| 0100     | 134.58    |
| 0101     | 150       |
| 0110     | 300       |
| 0111     | 600       |
| 1000     | 1200      |
| 1001     | 1800      |
| 1010     | 2400      |
| 1011     | 3600      |
| 1100     | 4800      |
| 1101     | 7200      |
| 1110     | 9600      |
| 1111     | 19200     |

---

## 3. Apple IIc Serial Ports

The Apple IIc has two built-in serial ports that emulate slot 1 and slot 2 functionality.

### 3.1 Port Mapping

| Port      | Slot Emulation | I/O Range       | Default Use    |
|-----------|----------------|-----------------|----------------|
| Serial 1  | Slot 1         | $C090-$C09F     | Modem          |
| Serial 2  | Slot 2         | $C0A0-$C0AF     | Printer        |

### 3.2 Hardware Differences

The IIc uses a Zilog Z8530 SCC (Serial Communications Controller) instead of the 6551 ACIA,
but the firmware provides backward compatibility with SSC software through translation.

### 3.3 Enhanced Features

The IIc serial ports support:
- **Firmware-based translation**: SSC register accesses translated to SCC commands
- **XON/XOFF flow control**: Software-based flow control
- **Modem support**: Built-in AT command handling (port 1)

---

## 4. Apple IIgs Serial Ports

The IIgs includes two Z8530 SCC-based serial ports with enhanced capabilities.

### 4.1 Register Map

The IIgs exposes the SCC through firmware-translated registers:

| Address | Register                              |
|---------|---------------------------------------|
| $C038   | SCC Channel A Control                 |
| $C039   | SCC Channel B Control                 |
| $C03A   | SCC Channel A Data                    |
| $C03B   | SCC Channel B Data                    |

### 4.2 Serial Toolbox

The IIgs provides a Serial Toolbox API for easier serial programming:

```assembly
; Serial Manager calls
SerOpen     = $0A00     ; Open a serial port
SerClose    = $0B00     ; Close a serial port
SerRead     = $0C00     ; Read from serial port
SerWrite    = $0D00     ; Write to serial port
SerStatus   = $0E00     ; Get port status
SerControl  = $0F00     ; Control port settings
```

### 4.3 Toolbox Structures

**SerOpen Parameter Block**:
```
+$00: Port number (1 or 2)
+$02: Input buffer pointer
+$06: Input buffer size
+$08: Output buffer pointer
+$0C: Output buffer size
```

**SerControl Commands**:
| Command | Function                              |
|---------|---------------------------------------|
| $0001   | Set baud rate                         |
| $0002   | Set data format                       |
| $0003   | Set handshake mode                    |
| $0004   | Set break                             |
| $0005   | Clear break                           |
| $0006   | Set DTR                               |
| $0007   | Clear DTR                             |

---

## 5. Serial Port Interface

```csharp
/// <summary>
/// Interface for serial port emulation.
/// </summary>
public interface ISerialPort : IPeripheral
{
    /// <summary>Gets or sets the baud rate.</summary>
    int BaudRate { get; set; }
    
    /// <summary>Gets or sets the data bits (5-8).</summary>
    int DataBits { get; set; }
    
    /// <summary>Gets or sets the parity mode.</summary>
    Parity Parity { get; set; }
    
    /// <summary>Gets or sets the number of stop bits.</summary>
    StopBits StopBits { get; set; }
    
    /// <summary>Gets whether receive data is available.</summary>
    bool DataAvailable { get; }
    
    /// <summary>Gets the current line status.</summary>
    SerialStatus Status { get; }
    
    /// <summary>Reads a byte from the receive buffer.</summary>
    byte Read();
    
    /// <summary>Writes a byte to the transmit buffer.</summary>
    void Write(byte data);
    
    /// <summary>Sets the DTR (Data Terminal Ready) signal.</summary>
    void SetDTR(bool active);
    
    /// <summary>Sets the RTS (Request to Send) signal.</summary>
    void SetRTS(bool active);
    
    /// <summary>Gets the state of the CTS (Clear to Send) signal.</summary>
    bool CTS { get; }
    
    /// <summary>Gets the state of the DSR (Data Set Ready) signal.</summary>
    bool DSR { get; }
    
    /// <summary>Gets the state of the DCD (Data Carrier Detect) signal.</summary>
    bool DCD { get; }
    
    /// <summary>
    /// Connects an external device to this port.
    /// </summary>
    void Connect(ISerialDevice device);
    
    /// <summary>
    /// Disconnects any connected device.
    /// </summary>
    void Disconnect();
    
    /// <summary>Raised when data is received.</summary>
    event Action? DataReceived;
    
    /// <summary>Raised when transmit buffer is empty.</summary>
    event Action? TransmitEmpty;
}

/// <summary>
/// Serial line status flags.
/// </summary>
[Flags]
public enum SerialStatus
{
    None = 0,
    TransmitEmpty = 0x10,
    DataReady = 0x08,
    OverrunError = 0x04,
    FramingError = 0x02,
    ParityError = 0x01
}

/// <summary>
/// Parity modes.
/// </summary>
public enum Parity
{
    None,
    Odd,
    Even,
    Mark,
    Space
}

/// <summary>
/// Stop bits configuration.
/// </summary>
public enum StopBits
{
    One = 1,
    Two = 2
}
```

---

## 6. Serial Device Interface

```csharp
/// <summary>
/// Interface for devices that connect to a serial port.
/// </summary>
public interface ISerialDevice
{
    /// <summary>Gets the device name.</summary>
    string Name { get; }
    
    /// <summary>Called when the port receives data for this device.</summary>
    void ReceiveData(byte data);
    
    /// <summary>Called when there is data to send to the port.</summary>
    event Action<byte>? SendData;
    
    /// <summary>Sets the CTS signal state for the connected port.</summary>
    void SetCTS(bool active);
    
    /// <summary>Sets the DSR signal state for the connected port.</summary>
    void SetDSR(bool active);
    
    /// <summary>Sets the DCD signal state for the connected port.</summary>
    void SetDCD(bool active);
    
    /// <summary>Called when DTR changes on the connected port.</summary>
    void OnDTRChanged(bool active);
    
    /// <summary>Called when RTS changes on the connected port.</summary>
    void OnRTSChanged(bool active);
}
```

---

## 7. Virtual Device Examples

### 7.1 Null Modem (Loopback)

```csharp
/// <summary>
/// Null modem device that loops data back.
/// </summary>
public class NullModem : ISerialDevice
{
    public string Name => "Null Modem";
    public event Action<byte>? SendData;
    
    public void ReceiveData(byte data)
    {
        // Echo data back
        SendData?.Invoke(data);
    }
    
    public void SetCTS(bool active) { }
    public void SetDSR(bool active) { }
    public void SetDCD(bool active) { }
    public void OnDTRChanged(bool active) => SetDSR(active);
    public void OnRTSChanged(bool active) => SetCTS(active);
}
```

### 7.2 TCP/IP Socket Bridge

```csharp
/// <summary>
/// Bridges serial port to a TCP socket (for BBS access, etc.).
/// </summary>
public class TcpSerialBridge : ISerialDevice, IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    
    public string Name => "TCP Bridge";
    public event Action<byte>? SendData;
    
    public async Task ConnectAsync(string host, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();
        SetDCD(true);
        _ = ReadLoopAsync();
    }
    
    private async Task ReadLoopAsync()
    {
        var buffer = new byte[1024];
        while (_stream != null)
        {
            int count = await _stream.ReadAsync(buffer);
            if (count == 0) break;
            for (int i = 0; i < count; i++)
                SendData?.Invoke(buffer[i]);
        }
        SetDCD(false);
    }
    
    public void ReceiveData(byte data)
    {
        _stream?.WriteByte(data);
    }
    
    // ... other methods ...
}
```

### 7.3 File-Based Device

```csharp
/// <summary>
/// Serial device that reads from / writes to files.
/// </summary>
public class FileSerialDevice : ISerialDevice
{
    private readonly StreamReader? _input;
    private readonly StreamWriter? _output;
    
    public string Name => "File Device";
    public event Action<byte>? SendData;
    
    public FileSerialDevice(string? inputPath, string? outputPath)
    {
        if (inputPath != null)
            _input = new StreamReader(inputPath);
        if (outputPath != null)
            _output = new StreamWriter(outputPath) { AutoFlush = true };
    }
    
    public void ReceiveData(byte data)
    {
        _output?.Write((char)data);
    }
    
    public void PumpInput()
    {
        if (_input != null && !_input.EndOfStream)
        {
            int ch = _input.Read();
            if (ch >= 0)
                SendData?.Invoke((byte)ch);
        }
    }
}
```

---

## 8. Implementation Notes

### 8.1 Baud Rate Timing

For accurate serial timing, use the scheduler:

```csharp
private void ScheduleNextBit()
{
    ulong cyclesPerBit = _cpuClockHz / (ulong)_baudRate;
    _scheduler.ScheduleAfter(
        new Cycle(cyclesPerBit),
        ScheduledEventKind.DeviceTimer,
        0,
        OnBitClock);
}
```

### 8.2 FIFO Buffers

Implement receive and transmit FIFOs:

```csharp
private readonly Queue<byte> _receiveBuffer = new(256);
private readonly Queue<byte> _transmitBuffer = new(256);

public void Write(byte data)
{
    if (_transmitBuffer.Count < 256)
        _transmitBuffer.Enqueue(data);
}

public byte Read()
{
    return _receiveBuffer.Count > 0 ? _receiveBuffer.Dequeue() : (byte)0;
}
```

### 8.3 Interrupt Generation

Generate interrupts when configured:

```csharp
private void CheckInterrupts()
{
    bool irq = false;
    
    if (_rxInterruptEnabled && DataAvailable)
        irq = true;
    if (_txInterruptEnabled && TransmitEmpty)
        irq = true;
    
    if (irq)
        _signals.Assert(SignalLine.IRQ, _deviceId);
    else
        _signals.Deassert(SignalLine.IRQ, _deviceId);
}
```

---

## 9. Common Use Cases

### 9.1 Terminal Emulation

Connecting to a remote BBS or UNIX system:

1. Configure port: 8N1, baud rate as needed
2. Connect TCP bridge to host:port
3. Forward keyboard input to serial port
4. Display received data on screen

### 9.2 Printer Output

Sending data to a printer:

1. Configure port to match printer (often 9600 8N1)
2. Use XON/XOFF or hardware flow control
3. Send text with appropriate line endings

### 9.3 File Transfer (XMODEM/ZMODEM)

Transferring files:

1. Establish connection to remote system
2. Start protocol handler on both ends
3. Transfer blocks with error checking

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |

---

## Appendix A: Bus Architecture Integration

This appendix provides implementation guidance for integrating serial communications
with the emulator's bus architecture.

### A.1 Serial Port as IPeripheral

The Super Serial Card implements `IPeripheral` for slot integration:

```csharp
/// <summary>
/// Super Serial Card implementing the peripheral interface.
/// </summary>
public sealed class SuperSerialCard : IPeripheral, ISchedulable
{
    private readonly byte[] _slotRom = new byte[256];
    private readonly byte[] _expansionRom = new byte[2048];
    private ISerialDevice? _connectedDevice;
    
    /// <inheritdoc/>
    public string Name => "Super Serial Card";
    
    /// <inheritdoc/>
    public string DeviceType => "SuperSerial";
    
    /// <inheritdoc/>
    public int SlotNumber { get; set; }
    
    /// <inheritdoc/>
    public IBusTarget? MMIORegion { get; }
    
    /// <inheritdoc/>
    public IBusTarget? ROMRegion { get; }
    
    /// <inheritdoc/>
    public IBusTarget? ExpansionROMRegion { get; }
    
    public SuperSerialCard()
    {
        MMIORegion = new AciaTarget(this);
        ROMRegion = new RomTarget(_slotRom);
        ExpansionROMRegion = new RomTarget(_expansionRom);
    }
    
    /// <inheritdoc/>
    public void OnExpansionROMSelected() { }
    
    /// <inheritdoc/>
    public void OnExpansionROMDeselected() { }
    
    /// <inheritdoc/>
    public void Reset()
    {
        ResetAcia();
    }
}
```

### A.2 6551 ACIA Target Implementation

The 6551 ACIA registers implement `IBusTarget`:

```csharp
/// <summary>
/// 6551 ACIA registers (4 bytes mapped to 16-byte slot I/O space).
/// </summary>
public sealed class AciaTarget : IBusTarget
{
    private readonly SuperSerialCard _card;
    private readonly Queue<byte> _receiveBuffer = new(256);
    private readonly Queue<byte> _transmitBuffer = new(256);
    
    private byte _statusRegister;
    private byte _commandRegister;
    private byte _controlRegister;
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.SideEffects;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        int offset = (int)(physicalAddress & 0x03);  // ACIA uses 4 registers
        
        return offset switch
        {
            0x00 => ReadDataRegister(access),
            0x01 => ReadStatusRegister(access),
            0x02 => _commandRegister,
            0x03 => _controlRegister,
            _ => 0xFF
        };
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return;
        
        int offset = (int)(physicalAddress & 0x03);
        
        switch (offset)
        {
            case 0x00:
                WriteDataRegister(value);
                break;
            case 0x01:
                // Write to status register = programmed reset
                ProgrammedReset();
                break;
            case 0x02:
                WriteCommandRegister(value);
                break;
            case 0x03:
                WriteControlRegister(value);
                break;
        }
    }
    
    private byte ReadDataRegister(in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return _receiveBuffer.Count > 0 ? _receiveBuffer.Peek() : (byte)0;
        
        // Clear RDRF flag and overrun error
        _statusRegister &= 0xF0;
        
        return _receiveBuffer.Count > 0 ? _receiveBuffer.Dequeue() : (byte)0;
    }
    
    private byte ReadStatusRegister(in BusAccess access)
    {
        byte status = _statusRegister;
        
        // Update dynamic flags
        if (_receiveBuffer.Count > 0)
            status |= 0x08;  // RDRF - Receive Data Register Full
        if (_transmitBuffer.Count < 256)
            status |= 0x10;  // TDRE - Transmit Data Register Empty
        
        // IRQ flag (bit 7) = any enabled interrupt source
        if (InterruptsPending())
            status |= 0x80;
        
        return status;
    }
    
    private void WriteDataRegister(byte value)
    {
        _transmitBuffer.Enqueue(value);
        
        // Schedule transmission
        _card.ScheduleTransmit();
    }
    
    private void WriteCommandRegister(byte value)
    {
        _commandRegister = value;
        
        // Update DTR based on bit 0
        _card.SetDTR((value & 0x01) != 0);
    }
    
    private void WriteControlRegister(byte value)
    {
        _controlRegister = value;
        
        // Extract baud rate from bits 0-3
        int baudIndex = value & 0x0F;
        _card.SetBaudRate(BaudRateFromIndex(baudIndex));
        
        // Extract word length from bits 5-6
        int wordLength = 8 - ((value >> 5) & 0x03);
        _card.SetDataBits(wordLength);
        
        // Stop bits from bit 7
        _card.SetStopBits((value & 0x80) != 0 ? 2 : 1);
    }
}
```

### A.3 Scheduler Integration for Baud Rate Timing

Serial transmission uses the scheduler for accurate timing:

```csharp
public sealed class SuperSerialCard : ISchedulable
{
    private readonly IScheduler _scheduler;
    private readonly ISignalBus _signals;
    private readonly int _deviceId;
    
    private int _baudRate = 9600;
    private ulong _cyclesPerBit;
    
    /// <inheritdoc/>
    public void Initialize(IEventContext context)
    {
        _scheduler = context.Scheduler;
        _signals = context.Signals;
        UpdateCyclesPerBit();
    }
    
    public void SetBaudRate(int baudRate)
    {
        _baudRate = baudRate;
        UpdateCyclesPerBit();
    }
    
    private void UpdateCyclesPerBit()
    {
        // Assuming 1 MHz CPU clock
        _cyclesPerBit = 1_000_000UL / (ulong)_baudRate;
    }
    
    public void ScheduleTransmit()
    {
        if (_transmitPending)
            return;
        
        _transmitPending = true;
        
        // Schedule after one character time (start + 8 data + stop = 10 bits)
        _scheduler.ScheduleAfter(this, _cyclesPerBit * 10);
    }
    
    /// <inheritdoc/>
    public ulong Execute(ulong currentCycle)
    {
        if (_transmitBuffer.Count > 0)
        {
            byte data = _transmitBuffer.Dequeue();
            
            // Send to connected device
            _connectedDevice?.ReceiveData(data);
            
            // Check for interrupt
            if (TransmitInterruptEnabled)
                _signals.Assert(SignalLine.IRQ, _deviceId);
            
            // Schedule next character if more data
            if (_transmitBuffer.Count > 0)
            {
                _scheduler.ScheduleAfter(this, _cyclesPerBit * 10);
            }
            else
            {
                _transmitPending = false;
            }
        }
        
        return _cyclesPerBit * 10;
    }
    
    public void ReceiveData(byte data)
    {
        _receiveBuffer.Enqueue(data);
        
        if (ReceiveInterruptEnabled)
            _signals.Assert(SignalLine.IRQ, _deviceId);
    }
}
```

### A.4 Composite Page Integration

Serial port registers are part of the I/O page:

```csharp
public sealed class AppleIIIOPage : ICompositeTarget
{
    private readonly ISlotManager _slots;
    
    /// <inheritdoc/>
    public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        // Slot device I/O ($C090-$C0FF)
        if (offset >= 0x90 && offset < 0x100)
        {
            int slot = ((offset - 0x80) >> 4);  // Extract slot number
            var card = _slots.GetCard(slot);
            return card?.MMIORegion;
        }
        
        // ...other handlers...
        return null;
    }
    
    /// <inheritdoc/>
    public RegionTag GetSubRegionTag(Addr offset)
    {
        if (offset >= 0x90 && offset < 0x100)
        {
            int slot = ((offset - 0x80) >> 4);
            return RegionTag.SlotIO | (RegionTag)slot;
        }
        
        return RegionTag.Unknown;
    }
}
```

### A.5 Apple IIc Built-in Serial Ports

The IIc serial ports are always present (no slot card needed):

```csharp
/// <summary>
/// Apple IIc built-in serial ports.
/// </summary>
public sealed class IIcSerialPorts
{
    private readonly SerialPort _port1;  // Modem
    private readonly SerialPort _port2;  // Printer
    
    public void ConfigureIOPage(AppleIIcIOPage ioPage)
    {
        // Port 1 at "slot 1" ($C090-$C09F)
        ioPage.RegisterHandler(0x90, 0x10, _port1.AciaTarget);
        
        // Port 2 at "slot 2" ($C0A0-$C0AF)
        ioPage.RegisterHandler(0xA0, 0x10, _port2.AciaTarget);
    }
}
```

### A.6 Virtual Serial Device Bridge

Connect emulated serial port to host resources:

```csharp
/// <summary>
/// Bridges emulated serial port to TCP socket.
/// </summary>
public sealed class TcpSerialBridge : ISerialDevice, IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly CancellationTokenSource _cts = new();
    
    public string Name => $"TCP:{_host}:{_port}";
    
    public event Action<byte>? SendData;
    
    public async Task ConnectAsync(string host, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(host, port);
        _stream = _client.GetStream();
        
        // Signal carrier detect
        SetDCD(true);
        
        // Start receive loop
        _ = ReceiveLoopAsync(_cts.Token);
    }
    
    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[1024];
        
        while (!ct.IsCancellationRequested && _stream != null)
        {
            try
            {
                int count = await _stream.ReadAsync(buffer, ct);
                if (count == 0)
                    break;
                
                for (int i = 0; i < count; i++)
                    SendData?.Invoke(buffer[i]);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        
        SetDCD(false);
    }
    
    public void ReceiveData(byte data)
    {
        _stream?.WriteByte(data);
    }
    
    public void OnDTRChanged(bool active)
    {
        if (!active)
            Disconnect();
    }
    
    private void Disconnect()
    {
        _cts.Cancel();
        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
    }
    
    public void Dispose()
    {
        Disconnect();
        _cts.Dispose();
    }
}
```

### A.7 Device Registry

```csharp
public void RegisterSerialDevices(IDeviceRegistry registry, int slot)
{
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.CompatIO, instance: (byte)slot, page: 0),
        kind: "SuperSerialCard",
        name: $"Super Serial Card (Slot {slot})",
        wiringPath: $"main/slots/{slot}/serial");
}

public void RegisterIIcSerialPorts(IDeviceRegistry registry)
{
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.CompatIO, instance: 1, page: 0),
        kind: "IIcSerial",
        name: "Apple IIc Serial Port 1 (Modem)",
        wiringPath: "main/serial/port1");
    
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.CompatIO, instance: 2, page: 0),
        kind: "IIcSerial",
        name: "Apple IIc Serial Port 2 (Printer)",
        wiringPath: "main/serial/port2");
}
```

### A.8 Trap Handler for Serial ROM Routines

```csharp
/// <summary>
/// Trap handler for SSC firmware routines.
/// </summary>
public sealed class SerialTraps
{
    private readonly SuperSerialCard _card;
    private readonly ISlotManager _slots;
    
    /// <summary>
    /// Trap for SSC initialization routine.
    /// </summary>
    public TrapResult InitHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        // Verify slot has SSC
        if (_slots.GetCard(_card.SlotNumber)?.DeviceType != "SuperSerial")
            return new TrapResult(Handled: false, default, null);
        
        // Select expansion ROM
        _slots.SelectExpansionSlot(_card.SlotNumber);
        
        // Initialize ACIA with default settings
        _card.Reset();
        _card.SetBaudRate(9600);
        _card.SetDataBits(8);
        _card.SetStopBits(1);
        _card.SetDTR(true);
        
        return new TrapResult(
            Handled: true,
            CyclesConsumed: new Cycle(100),
            ReturnAddress: null);
    }
    
    /// <summary>
    /// Trap for SSC output character routine.
    /// </summary>
    public TrapResult OutputHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        if (_slots.GetCard(_card.SlotNumber)?.DeviceType != "SuperSerial")
            return new TrapResult(Handled: false, default, null);
        
        // Character in accumulator
        byte ch = cpu.A;
        
        // Wait for transmit ready
        while (!_card.TransmitReady)
        {
            // This would normally be a busy wait
            // In trap, we can just send immediately
        }
        
        _card.TransmitByte(ch);
        
        return new TrapResult(
            Handled: true,
            CyclesConsumed: new Cycle(50),
            ReturnAddress: null);
    }
}
```

### A.9 Signal Bus for Flow Control

```csharp
public sealed class SuperSerialCard
{
    private readonly ISignalBus _signals;
    private readonly int _deviceId;
    
    public void UpdateFlowControl()
    {
        // CTS from connected device controls transmit
        if (!_cts && _transmitBuffer.Count > 0)
        {
            // Pause transmission
            _scheduler.Cancel(this);
            _transmitPending = false;
        }
        else if (_cts && _transmitBuffer.Count > 0 && !_transmitPending)
        {
            // Resume transmission
            ScheduleTransmit();
        }
    }
    
    public void SetCTS(bool active)
    {
        _cts = active;
        UpdateFlowControl();
    }
    
    public void SetDSR(bool active)
    {
        _dsr = active;
        // Update status register
    }
    
    public void SetDCD(bool active)
    {
        _dcd = active;
        // Update status register
        
        // DCD change can trigger interrupt
        if (DcdInterruptEnabled)
            _signals.Assert(SignalLine.IRQ, _deviceId);
    }
}
