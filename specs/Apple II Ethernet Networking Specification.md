# Apple II Ethernet Networking Specification

## Document Information

| Field        | Value                                              |
|--------------|----------------------------------------------------|
| Version      | 1.1                                                |
| Date         | 2025-12-28                                         |
| Status       | Initial Draft                                      |
| Applies To   | Pocket2e, Pocket2c, PocketGS                       |

---

## 1. Overview

This specification describes **hardware-accurate emulation** of Uthernet Ethernet cards
for the Apple II family. This approach emulates the actual W5100/W5500 network interface
chips, enabling compatibility with existing network software written for these cards.

> **Note**: For a simpler, high-level network API that doesn't require hardware emulation,
> see the [Emulator Network Services Specification](Emulator%20Network%20Services%20Specification.md).
> ENS provides an easier implementation path for new software, while this specification
> enables running existing Uthernet-compatible software like Contiki and Marinetti.

### 1.1 When to Use This Specification

Use hardware-accurate Uthernet emulation when:
- Running existing software that requires Uthernet drivers (Contiki, Marinetti)
- Historical accuracy is important
- Testing or developing Uthernet-compatible software

Use the ENS specification instead when:
- Developing new network-enabled software
- Simplicity and ease of implementation are priorities
- Low-level hardware compatibility is not required

### 1.2 Historical Context

| Year | Development                                        |
|------|----------------------------------------------------|
| 1985 | First Ethernet cards for Apple II (rare)           |
| 1987 | AppleTalk to Ethernet bridges available            |
| 2000s| Uthernet card released (Wiznet W5100 based)        |
| 2010s| Uthernet II released (improved Wiznet chip)        |

### 1.3 Why Ethernet Emulation Matters

Ethernet emulation enables:
- Network access from vintage software
- File sharing with modern systems
- Internet access (Contiki, Marinetti)
- BBS access via Telnet

---

## 2. Uthernet Card Architecture

The Uthernet series uses Wiznet network interface chips:

### 2.1 Uthernet (W5100)

| Feature          | Specification                            |
|------------------|------------------------------------------|
| Chip             | Wiznet W5100                             |
| Speed            | 10/100 Mbps                              |
| Sockets          | 4 simultaneous TCP/UDP                   |
| Buffer           | 16KB (8KB TX, 8KB RX)                    |
| Interface        | Memory-mapped I/O                        |

### 2.2 Uthernet II (W5500)

| Feature          | Specification                            |
|------------------|------------------------------------------|
| Chip             | Wiznet W5500                             |
| Speed            | 10/100 Mbps                              |
| Sockets          | 8 simultaneous TCP/UDP                   |
| Buffer           | 32KB per socket configurable             |
| Interface        | SPI (via slot I/O)                       |

---

## 3. Memory-Mapped Interface

### 3.1 Register Mapping (Slot n)

For Uthernet in slot 3 ($C0B0 base):

| Offset | Register                 | Access |
|--------|--------------------------|--------|
| +$04   | Mode Register            | R/W    |
| +$05   | Address High             | R/W    |
| +$06   | Address Low              | R/W    |
| +$07   | Data Port                | R/W    |

### 3.2 Indirect Addressing Mode

The W5100 uses indirect addressing:

```csharp
public byte ReadRegister(ushort address)
{
    // Set address
    _slotIO[0x05] = (byte)(address >> 8);
    _slotIO[0x06] = (byte)(address & 0xFF);
    
    // Read data
    return _slotIO[0x07];
}

public void WriteRegister(ushort address, byte value)
{
    // Set address
    _slotIO[0x05] = (byte)(address >> 8);
    _slotIO[0x06] = (byte)(address & 0xFF);
    
    // Write data
    _slotIO[0x07] = value;
}
```

---

## 4. W5100 Register Map

### 4.1 Common Registers ($0000-$002F)

| Address | Register    | Description                         |
|---------|-------------|-------------------------------------|
| $0000   | MR          | Mode Register                       |
| $0001   | GAR[0-3]    | Gateway Address (4 bytes)           |
| $0005   | SUBR[0-3]   | Subnet Mask (4 bytes)               |
| $0009   | SHAR[0-5]   | Source Hardware Address (6 bytes)   |
| $000F   | SIPR[0-3]   | Source IP Address (4 bytes)         |
| $0017   | IR          | Interrupt Register                  |
| $0018   | IMR         | Interrupt Mask Register             |
| $0019   | RTR[0-1]    | Retry Time Register                 |
| $001B   | RCR         | Retry Count Register                |
| $001C   | RMSR        | RX Memory Size Register             |
| $001D   | TMSR        | TX Memory Size Register             |

### 4.2 Socket Registers ($0400+n×$100)

Each socket has 256 bytes of registers:

| Offset | Register    | Description                         |
|--------|-------------|-------------------------------------|
| +$00   | Sn_MR       | Socket n Mode Register              |
| +$01   | Sn_CR       | Socket n Command Register           |
| +$02   | Sn_IR       | Socket n Interrupt Register         |
| +$03   | Sn_SR       | Socket n Status Register            |
| +$04   | Sn_PORT[0-1]| Socket n Source Port                |
| +$06   | Sn_DHAR[0-5]| Socket n Destination MAC            |
| +$0C   | Sn_DIPR[0-3]| Socket n Destination IP             |
| +$10   | Sn_DPORT[0-1]| Socket n Destination Port          |
| +$14   | Sn_MSSR[0-1]| Socket n Max Segment Size           |
| +$20   | Sn_TX_FSR[0-1]| Socket n TX Free Size             |
| +$22   | Sn_TX_RD[0-1]| Socket n TX Read Pointer           |
| +$24   | Sn_TX_WR[0-1]| Socket n TX Write Pointer          |
| +$26   | Sn_RX_RSR[0-1]| Socket n RX Received Size         |
| +$28   | Sn_RX_RD[0-1]| Socket n RX Read Pointer           |

### 4.3 Socket Mode Values

| Value | Mode                                       |
|-------|--------------------------------------------|
| $00   | Closed                                     |
| $01   | TCP                                        |
| $02   | UDP                                        |
| $03   | IP Raw                                     |
| $04   | MAC Raw                                    |

### 4.4 Socket Commands

| Value | Command                                    |
|-------|--------------------------------------------|
| $01   | OPEN - Initialize socket                   |
| $02   | LISTEN - Wait for TCP connection           |
| $04   | CONNECT - Connect to server                |
| $08   | DISCON - Disconnect                        |
| $10   | CLOSE - Close socket                       |
| $20   | SEND - Send data                           |
| $40   | RECV - Confirm data reception              |

### 4.5 Socket Status Values

| Value | Status                                     |
|-------|--------------------------------------------|
| $00   | SOCK_CLOSED                                |
| $13   | SOCK_INIT                                  |
| $14   | SOCK_LISTEN                                |
| $17   | SOCK_ESTABLISHED                           |
| $1C   | SOCK_CLOSE_WAIT                            |
| $22   | SOCK_UDP                                   |

---

## 5. TX/RX Buffer Memory

### 5.1 Buffer Layout

| Address Range   | Size | Purpose                         |
|-----------------|------|---------------------------------|
| $4000-$5FFF     | 8KB  | TX Buffer                       |
| $6000-$7FFF     | 8KB  | RX Buffer                       |

### 5.2 Buffer Size Configuration

The TMSR and RMSR registers configure buffer allocation:

| Value | Per-Socket Size | Socket 0 | Socket 1 | Socket 2 | Socket 3 |
|-------|-----------------|----------|----------|----------|----------|
| $55   | 2KB each        | 2KB      | 2KB      | 2KB      | 2KB      |
| $0A   | 4KB/4KB/0/0     | 4KB      | 4KB      | 0        | 0        |
| $03   | 8KB/0/0/0       | 8KB      | 0        | 0        | 0        |

### 5.3 Buffer Access

```csharp
public void SendData(int socket, ReadOnlySpan<byte> data)
{
    // Get write pointer
    ushort wrPtr = ReadWord(Sn_TX_WR(socket));
    
    // Calculate physical address
    ushort physAddr = TxBufferBase(socket) + (ushort)(wrPtr & TxBufferMask);
    
    // Write data (with wrapping)
    foreach (byte b in data)
    {
        WriteRegister(physAddr, b);
        physAddr++;
        if (physAddr >= TxBufferEnd(socket))
            physAddr = TxBufferBase(socket);
    }
    
    // Update write pointer
    WriteWord(Sn_TX_WR(socket), (ushort)(wrPtr + data.Length));
    
    // Issue SEND command
    WriteRegister(Sn_CR(socket), CMD_SEND);
}
```

---

## 6. Network Stack Integration

### 6.1 Marinetti (IIgs)

Marinetti is a TCP/IP stack for the Apple IIgs that supports Uthernet:

```assembly
; Marinetti TCP/IP calls
TCPIPStartup    = $36FC
TCPIPShutDown   = $37FC
TCPIPConnect    = $38FC
TCPIPDisconnect = $39FC
TCPIPSendData   = $3AFC
TCPIPReceive    = $3BFC
TCPIPGetMyIP    = $3CFC
```

### 6.2 Contiki

Contiki is a lightweight OS with TCP/IP support for 8-bit systems:

- Web browser
- Telnet client
- Email client
- IRC client

### 6.3 ProDOS Network

Third-party ProDOS extensions provide network file access.

---

## 7. Ethernet Interface

```csharp
/// <summary>
/// Interface for Ethernet card emulation.
/// </summary>
public interface IEthernetCard : IPeripheral
{
    /// <summary>Gets the MAC address.</summary>
    byte[] MacAddress { get; }
    
    /// <summary>Gets or sets the IP address.</summary>
    byte[] IpAddress { get; set; }
    
    /// <summary>Gets or sets the subnet mask.</summary>
    byte[] SubnetMask { get; set; }
    
    /// <summary>Gets or sets the gateway address.</summary>
    byte[] Gateway { get; set; }
    
    /// <summary>Gets the link status.</summary>
    bool IsLinkUp { get; }
    
    /// <summary>Gets socket information.</summary>
    SocketInfo GetSocketInfo(int socket);
    
    /// <summary>Connects to host system network.</summary>
    void Connect(INetworkAdapter adapter);
    
    /// <summary>Disconnects from network.</summary>
    void Disconnect();
}

/// <summary>
/// Socket information.
/// </summary>
public record SocketInfo(
    int Number,
    SocketMode Mode,
    SocketStatus Status,
    ushort LocalPort,
    byte[] RemoteIp,
    ushort RemotePort,
    int TxFreeSize,
    int RxDataSize
);

/// <summary>
/// Socket modes.
/// </summary>
public enum SocketMode
{
    Closed = 0,
    Tcp = 1,
    Udp = 2,
    IpRaw = 3,
    MacRaw = 4
}

/// <summary>
/// Socket status.
/// </summary>
public enum SocketStatus
{
    Closed = 0x00,
    Init = 0x13,
    Listen = 0x14,
    Established = 0x17,
    CloseWait = 0x1C,
    Udp = 0x22
}
```

---

## 8. Network Adapter Interface

```csharp
/// <summary>
/// Interface to host system network for Ethernet emulation.
/// </summary>
public interface INetworkAdapter
{
    /// <summary>Gets the adapter MAC address.</summary>
    byte[] MacAddress { get; }
    
    /// <summary>Opens a TCP connection.</summary>
    Task<INetworkConnection> ConnectTcpAsync(
        byte[] remoteIp, ushort remotePort, ushort localPort);
    
    /// <summary>Creates a TCP listener.</summary>
    Task<INetworkListener> ListenTcpAsync(ushort port);
    
    /// <summary>Opens a UDP socket.</summary>
    Task<IUdpSocket> OpenUdpAsync(ushort port);
    
    /// <summary>Sends raw IP packet.</summary>
    Task SendRawAsync(ReadOnlyMemory<byte> packet);
    
    /// <summary>Receives raw IP packet.</summary>
    event Action<ReadOnlyMemory<byte>>? RawPacketReceived;
}

/// <summary>
/// TCP connection handle.
/// </summary>
public interface INetworkConnection : IAsyncDisposable
{
    /// <summary>Gets whether the connection is open.</summary>
    bool IsConnected { get; }
    
    /// <summary>Sends data.</summary>
    Task<int> SendAsync(ReadOnlyMemory<byte> data);
    
    /// <summary>Receives data.</summary>
    Task<int> ReceiveAsync(Memory<byte> buffer);
    
    /// <summary>Closes the connection.</summary>
    Task CloseAsync();
    
    /// <summary>Raised when data is available.</summary>
    event Action? DataAvailable;
    
    /// <summary>Raised when connection is closed.</summary>
    event Action? Disconnected;
}
```

---

## 9. Implementation Notes

### 9.1 Socket State Machine

Implement W5100 socket state transitions:

```csharp
private void ProcessSocketCommand(int socket, byte command)
{
    var state = _socketStatus[socket];
    
    switch (command)
    {
        case CMD_OPEN:
            if (state == Status.Closed)
            {
                switch (_socketMode[socket])
                {
                    case Mode.Tcp:
                        _socketStatus[socket] = Status.Init;
                        break;
                    case Mode.Udp:
                        _socketStatus[socket] = Status.Udp;
                        break;
                }
            }
            break;
            
        case CMD_LISTEN:
            if (state == Status.Init && _socketMode[socket] == Mode.Tcp)
            {
                _socketStatus[socket] = Status.Listen;
                StartListening(socket);
            }
            break;
            
        case CMD_CONNECT:
            if (state == Status.Init && _socketMode[socket] == Mode.Tcp)
            {
                StartConnection(socket);
            }
            break;
            
        case CMD_SEND:
            if (state == Status.Established || state == Status.Udp)
            {
                TransmitData(socket);
            }
            break;
            
        case CMD_CLOSE:
            CloseSocket(socket);
            _socketStatus[socket] = Status.Closed;
            break;
    }
}
```

### 9.2 Buffer Circular Access

Handle buffer wrapping correctly:

```csharp
private void WriteToTxBuffer(int socket, ushort offset, byte[] data)
{
    ushort bufferBase = GetTxBufferBase(socket);
    ushort bufferSize = GetTxBufferSize(socket);
    ushort physAddr = (ushort)(bufferBase + (offset % bufferSize));
    
    foreach (byte b in data)
    {
        _memory[physAddr] = b;
        physAddr++;
        if (physAddr >= bufferBase + bufferSize)
            physAddr = bufferBase;  // Wrap around
    }
}
```

### 9.3 Host Network Bridge

Bridge to host network using .NET sockets:

```csharp
private async Task HandleTcpConnectAsync(int socket)
{
    var remoteIp = new IPAddress(GetSocketRemoteIp(socket));
    int remotePort = GetSocketRemotePort(socket);
    
    try
    {
        var client = new TcpClient();
        await client.ConnectAsync(remoteIp, remotePort);
        _socketConnections[socket] = client;
        _socketStatus[socket] = Status.Established;
        
        // Start receive loop
        _ = ReceiveLoopAsync(socket, client);
    }
    catch (Exception)
    {
        _socketStatus[socket] = Status.Closed;
        SetSocketInterrupt(socket, INT_TIMEOUT);
    }
}
```

### 9.4 Interrupt Handling

Generate interrupts for network events:

```csharp
private void SetSocketInterrupt(int socket, byte flags)
{
    _socketInterrupt[socket] |= flags;
    
    // Update main interrupt register if socket interrupt enabled
    if ((_interruptMask & (1 << socket)) != 0)
    {
        _interrupt |= (byte)(1 << socket);
        
        // Signal CPU interrupt
        _signals.Assert(SignalLine.IRQ, _deviceId);
    }
}

private void ClearSocketInterrupt(int socket, byte flags)
{
    _socketInterrupt[socket] &= (byte)~flags;
    
    // Clear main interrupt if no socket interrupts pending
    if (_socketInterrupt[socket] == 0)
    {
        _interrupt &= (byte)~(1 << socket);
        
        if (_interrupt == 0)
            _signals.Deassert(SignalLine.IRQ, _deviceId);
    }
}
```

---

## 10. Common Use Cases

### 10.1 Telnet Client

```csharp
// Open TCP socket to BBS
WriteRegister(Sn_MR(0), MODE_TCP);
WriteWord(Sn_PORT(0), 23);  // Local port
WriteBytes(Sn_DIPR(0), serverIp);
WriteWord(Sn_DPORT(0), 23);  // Telnet port

WriteRegister(Sn_CR(0), CMD_OPEN);
WaitForStatus(0, STATUS_INIT);

WriteRegister(Sn_CR(0), CMD_CONNECT);
WaitForStatus(0, STATUS_ESTABLISHED);
```

### 10.2 Web Server

```csharp
// Listen for HTTP connections
WriteRegister(Sn_MR(0), MODE_TCP);
WriteWord(Sn_PORT(0), 80);

WriteRegister(Sn_CR(0), CMD_OPEN);
WaitForStatus(0, STATUS_INIT);

WriteRegister(Sn_CR(0), CMD_LISTEN);
WaitForStatus(0, STATUS_LISTEN);

// Wait for connection
WaitForStatus(0, STATUS_ESTABLISHED);
// Handle HTTP request...
```

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |

---

## Appendix A: Bus Architecture Integration

This appendix provides implementation guidance for integrating Ethernet networking
with the emulator's bus architecture.

### A.1 Uthernet Card as IPeripheral

The Uthernet card implements `IPeripheral` for slot integration:

```csharp
/// <summary>
/// Uthernet (W5100) Ethernet card.
/// </summary>
public sealed class UthernetCard : IPeripheral, ISchedulable
{
    private readonly byte[] _slotRom = new byte[256];
    private readonly W5100Emulator _w5100;
    private INetworkAdapter? _networkAdapter;
    
    /// <inheritdoc/>
    public string Name => "Uthernet";
    
    /// <inheritdoc/>
    public string DeviceType => "Uthernet";
    
    /// <inheritdoc/>
    public int SlotNumber { get; set; }
    
    /// <inheritdoc/>
    public IBusTarget? MMIORegion { get; }
    
    /// <inheritdoc/>
    public IBusTarget? ROMRegion { get; }
    
    /// <inheritdoc/>
    public IBusTarget? ExpansionROMRegion => null;
    
    public UthernetCard()
    {
        _w5100 = new W5100Emulator();
        MMIORegion = new UthernetMMIO(this, _w5100);
        ROMRegion = new RomTarget(_slotRom);
    }
    
    public void Connect(INetworkAdapter adapter)
    {
        _networkAdapter = adapter;
        _w5100.SetNetworkAdapter(adapter);
    }
}
```

### A.2 W5100 Register Target

```csharp
/// <summary>
/// Uthernet MMIO registers implementing indirect addressing.
/// </summary>
public sealed class UthernetMMIO : IBusTarget
{
    private readonly UthernetCard _card;
    private readonly W5100Emulator _w5100;
    
    private byte _modeRegister;
    private ushort _addressPointer;
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.SideEffects;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        int offset = (int)(physicalAddress & 0x0F);
        
        return offset switch
        {
            0x04 => _modeRegister,
            0x05 => (byte)(_addressPointer >> 8),     // Address high
            0x06 => (byte)(_addressPointer & 0xFF),   // Address low
            0x07 => ReadDataPort(access),             // Data port
            _ => 0xFF
        };
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        if (access.IsSideEffectFree)
            return;
        
        int offset = (int)(physicalAddress & 0x0F);
        
        switch (offset)
        {
            case 0x04:
                _modeRegister = value;
                break;
            case 0x05:
                _addressPointer = (ushort)((_addressPointer & 0x00FF) | (value << 8));
                break;
            case 0x06:
                _addressPointer = (ushort)((_addressPointer & 0xFF00) | value);
                break;
            case 0x07:
                WriteDataPort(value);
                break;
        }
    }
    
    private byte ReadDataPort(in BusAccess access)
    {
        byte value = _w5100.ReadRegister(_addressPointer);
        
        if (!access.IsSideEffectFree)
        {
            // Auto-increment address pointer
            _addressPointer++;
        }
        
        return value;
    }
    
    private void WriteDataPort(byte value)
    {
        _w5100.WriteRegister(_addressPointer, value);
        _addressPointer++;
    }
}
```

### A.3 W5100 Emulator Core

```csharp
/// <summary>
/// W5100 network chip emulation.
/// </summary>
public sealed class W5100Emulator
{
    private readonly byte[] _commonRegisters = new byte[0x30];
    private readonly Socket[] _sockets = new Socket[4];
    private readonly byte[] _txBuffer = new byte[8192];
    private readonly byte[] _rxBuffer = new byte[8192];
    
    private INetworkAdapter? _adapter;
    
    public W5100Emulator()
    {
        for (int i = 0; i < 4; i++)
            _sockets[i] = new Socket(i);
    }
    
    public void SetNetworkAdapter(INetworkAdapter adapter)
    {
        _adapter = adapter;
        
        // Copy MAC address to SHAR
        var mac = adapter.MacAddress;
        Array.Copy(mac, 0, _commonRegisters, 0x09, 6);
    }
    
    public byte ReadRegister(ushort address)
    {
        return address switch
        {
            < 0x0030 => _commonRegisters[address],
            >= 0x0400 and < 0x0800 => ReadSocketRegister(address),
            >= 0x4000 and < 0x6000 => _txBuffer[address - 0x4000],
            >= 0x6000 and < 0x8000 => _rxBuffer[address - 0x6000],
            _ => 0x00
        };
    }
    
    public void WriteRegister(ushort address, byte value)
    {
        switch (address)
        {
            case < 0x0030:
                WriteCommonRegister(address, value);
                break;
            case >= 0x0400 and < 0x0800:
                WriteSocketRegister(address, value);
                break;
            case >= 0x4000 and < 0x6000:
                _txBuffer[address - 0x4000] = value;
                break;
        }
    }
    
    private void WriteSocketRegister(ushort address, byte value)
    {
        int socket = (address - 0x0400) >> 8;
        int offset = address & 0xFF;
        
        if (offset == 0x01)  // Command register
            ProcessSocketCommand(socket, value);
        else
            _sockets[socket].Registers[offset] = value;
    }
    
    private void ProcessSocketCommand(int socketNum, byte command)
    {
        var socket = _sockets[socketNum];
        
        switch (command)
        {
            case 0x01:  // OPEN
                OpenSocket(socket);
                break;
            case 0x02:  // LISTEN
                ListenSocket(socket);
                break;
            case 0x04:  // CONNECT
                ConnectSocket(socket);
                break;
            case 0x10:  // CLOSE
                CloseSocket(socket);
                break;
            case 0x20:  // SEND
                SendData(socket);
                break;
            case 0x40:  // RECV
                ConfirmReceive(socket);
                break;
        }
    }
}
```

### A.4 Socket State Machine

```csharp
public sealed class Socket
{
    public int Number { get; }
    public byte[] Registers { get; } = new byte[256];
    public SocketStatus Status { get; set; } = SocketStatus.Closed;
    public INetworkConnection? Connection { get; set; }
    
    public Socket(int number)
    {
        Number = number;
    }
    
    public byte Mode => Registers[0x00];
    public ushort SourcePort => (ushort)((Registers[0x04] << 8) | Registers[0x05]);
    public byte[] DestinationIP => Registers[0x0C..0x10];
    public ushort DestinationPort => (ushort)((Registers[0x10] << 8) | Registers[0x11]);
    
    public ushort TxWritePointer
    {
        get => (ushort)((Registers[0x24] << 8) | Registers[0x25]);
        set
        {
            Registers[0x24] = (byte)(value >> 8);
            Registers[0x25] = (byte)(value & 0xFF);
        }
    }
    
    public ushort RxReadPointer
    {
        get => (ushort)((Registers[0x28] << 8) | Registers[0x29]);
        set
        {
            Registers[0x28] = (byte)(value >> 8);
            Registers[0x29] = (byte)(value & 0xFF);
        }
    }
}
```

### A.5 Network Operations with Async Bridge

```csharp
public sealed class W5100Emulator
{
    private async void ConnectSocket(Socket socket)
    {
        if (_adapter == null || socket.Mode != 0x01)  // TCP mode
        {
            socket.Status = SocketStatus.Closed;
            return;
        }
        
        try
        {
            var connection = await _adapter.ConnectTcpAsync(
                socket.DestinationIP,
                socket.DestinationPort,
                socket.SourcePort);
            
            socket.Connection = connection;
            socket.Status = SocketStatus.Established;
            
            // Set up receive handler
            connection.DataAvailable += () => OnSocketDataAvailable(socket);
            connection.Disconnected += () => OnSocketDisconnected(socket);
            
            // Start receive loop
            _ = ReceiveLoopAsync(socket);
        }
        catch
        {
            socket.Status = SocketStatus.Closed;
            SetSocketInterrupt(socket.Number, 0x08);  // Timeout
        }
    }
    
    private async Task ReceiveLoopAsync(Socket socket)
    {
        var buffer = new byte[2048];
        
        while (socket.Connection?.IsConnected == true)
        {
            try
            {
                int count = await socket.Connection.ReceiveAsync(buffer);
                if (count == 0)
                    break;
                
                // Copy to RX buffer
                CopyToRxBuffer(socket, buffer.AsSpan(0, count));
                
                // Update RX received size
                UpdateRxReceivedSize(socket, count);
                
                // Set data received interrupt
                SetSocketInterrupt(socket.Number, 0x04);
            }
            catch
            {
                break;
            }
        }
        
        socket.Status = SocketStatus.CloseWait;
        SetSocketInterrupt(socket.Number, 0x02);  // Disconnect
    }
}
```

### A.6 Scheduler Integration

```csharp
public sealed class UthernetCard : ISchedulable
{
    private readonly IScheduler _scheduler;
    private readonly ISignalBus _signals;
    private readonly int _deviceId;
    
    private const ulong PollIntervalCycles = 10_000;  // Check every 10ms
    
    /// <inheritdoc/>
    public void Initialize(IEventContext context)
    {
        _scheduler = context.Scheduler;
        _signals = context.Signals;
        
        // Start polling for network events
        _scheduler.ScheduleAfter(this, PollIntervalCycles);
    }
    
    /// <inheritdoc/>
    public ulong Execute(ulong currentCycle)
    {
        // Check for pending interrupts
        if (_w5100.HasPendingInterrupts())
        {
            _signals.Assert(SignalLine.IRQ, _deviceId);
        }
        
        // Schedule next poll
        _scheduler.ScheduleAfter(this, PollIntervalCycles);
        return 0;
    }
}
```

### A.7 Host Network Adapter

```csharp
/// <summary>
/// Network adapter bridging to host TCP/IP stack.
/// </summary>
public sealed class HostNetworkAdapter : INetworkAdapter
{
    public byte[] MacAddress { get; } = GenerateMacAddress();
    
    public async Task<INetworkConnection> ConnectTcpAsync(
        byte[] remoteIp, 
        ushort remotePort, 
        ushort localPort)
    {
        var client = new TcpClient();
        var ip = new IPAddress(remoteIp);
        
        await client.ConnectAsync(ip, remotePort);
        
        return new TcpConnectionWrapper(client);
    }
    
    public async Task<INetworkListener> ListenTcpAsync(ushort port)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        
        return new TcpListenerWrapper(listener);
    }
    
    public async Task<IUdpSocket> OpenUdpAsync(ushort port)
    {
        var client = new UdpClient(port);
        return new UdpSocketWrapper(client);
    }
    
    private static byte[] GenerateMacAddress()
    {
        // Generate a locally administered MAC address
        var mac = new byte[6];
        Random.Shared.NextBytes(mac);
        mac[0] = (byte)((mac[0] & 0xFE) | 0x02);  // Set local bit, clear multicast
        return mac;
    }
}

/// <summary>
/// Wrapper for TcpClient implementing INetworkConnection.
/// </summary>
public sealed class TcpConnectionWrapper : INetworkConnection
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    
    public bool IsConnected => _client.Connected;
    
    public event Action? DataAvailable;
    public event Action? Disconnected;
    
    public TcpConnectionWrapper(TcpClient client)
    {
        _client = client;
        _stream = client.GetStream();
    }
    
    public async Task<int> SendAsync(ReadOnlyMemory<byte> data)
    {
        await _stream.WriteAsync(data);
        return data.Length;
    }
    
    public async Task<int> ReceiveAsync(Memory<byte> buffer)
    {
        return await _stream.ReadAsync(buffer);
    }
    
    public async Task CloseAsync()
    {
        _stream.Close();
        _client.Close();
        Disconnected?.Invoke();
    }
    
    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
    }
}
```

### A.8 Device Registry

```csharp
public void RegisterEthernetDevices(IDeviceRegistry registry, int slot)
{
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.Network, instance: (byte)slot, page: 0),
        kind: "Uthernet",
        name: $"Uthernet (Slot {slot})",
        wiringPath: $"main/slots/{slot}/ethernet");
}
```

### A.9 Composite Page Integration

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
            int slot = ((offset - 0x80) >> 4);
            var card = _slots.GetCard(slot);
            return card?.MMIORegion;
        }
        
        return null;
    }
    
    /// <inheritdoc/>
    public RegionTag GetSubRegionTag(Addr offset)
    {
        if (offset >= 0x90 && offset < 0x100)
        {
            int slot = ((offset - 0x80) >> 4);
            var card = _slots.GetCard(slot);
            
            return card?.DeviceType switch
            {
                "Uthernet" => RegionTag.Network,
                _ => RegionTag.SlotIO
            };
        }
        
        return RegionTag.Unknown;
    }
}
```

### A.10 Signal Bus for Network Interrupts

```csharp
public sealed class W5100Emulator
{
    private readonly ISignalBus _signals;
    private readonly int _deviceId;
    private byte _interruptRegister;
    private byte _interruptMask = 0xFF;
    
    private void SetSocketInterrupt(int socket, byte flags)
    {
        // Set socket interrupt flags
        var s = _sockets[socket];
        s.Registers[0x02] |= flags;  // Sn_IR
        
        // Update main interrupt register
        _interruptRegister |= (byte)(1 << socket);
        
        // Assert IRQ if enabled
        if ((_interruptMask & (1 << socket)) != 0)
        {
            _signals.Assert(SignalLine.IRQ, _deviceId);
        }
    }
    
    public void ClearInterrupt(byte flags)
    {
        _interruptRegister &= (byte)~flags;
        
        if (_interruptRegister == 0)
            _signals.Deassert(SignalLine.IRQ, _deviceId);
    }
}
