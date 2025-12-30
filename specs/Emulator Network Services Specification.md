# Emulator Network Services Specification

## Document Information

| Field        | Value                                              |
|--------------|----------------------------------------------------|
| Version      | 1.0                                                |
| Date         | 2025-12-28                                         |
| Status       | Initial Draft                                      |
| Applies To   | Pocket2e, Pocket2c, PocketGS                       |

---

## 1. Overview

This specification defines a **high-level network API** for Apple II emulation that bridges
guest software to the host system's network stack. Unlike hardware-accurate emulation of
specific network cards (see [Apple II Ethernet Networking Specification](Apple%20II%20Ethernet%20Networking%20Specification.md)),
this approach provides a clean, trap-based interface that is:

1. **Simple to implement**: No complex chip emulation required
2. **Easy to use from guest code**: High-level socket API
3. **Efficient**: Direct host network access without protocol translation
4. **Portable**: Works regardless of host network configuration

### 1.1 Design Philosophy

The Emulator Network Services (ENS) follows the same philosophy as other emulator traps:

- **Trap-based interface**: Guest software calls ROM routines that trigger native handlers
- **High-level abstraction**: Sockets, not packets; connections, not hardware registers
- **Zero guest-side drivers**: Works with any guest software that uses the ENS API
- **Host-bridged**: All network operations execute on the host system

### 1.2 Comparison with Hardware Emulation

| Feature                  | ENS (This Spec)      | Uthernet Emulation   |
|--------------------------|----------------------|----------------------|
| Implementation effort    | Low                  | High                 |
| Guest software required  | ENS-aware apps       | Uthernet drivers     |
| Historical accuracy      | None                 | High                 |
| Performance              | Excellent            | Good                 |
| Existing software compat | New apps only        | Contiki, Marinetti   |
| Network protocols        | TCP, UDP, DNS        | Raw Ethernet/IP      |

---

## 2. Architecture

### 2.1 Component Overview

```
???????????????????????????????????????????????????????????????
?                     Guest Software                           ?
?                  (BASIC, Assembly, etc.)                     ?
???????????????????????????????????????????????????????????????
                          ? JSR $C800 (ENS ROM)
                          ?
???????????????????????????????????????????????????????????????
?                   ENS ROM Interface                          ?
?              (Trap vectors at $C800-$C8FF)                   ?
???????????????????????????????????????????????????????????????
                          ? Trap to native handler
                          ?
???????????????????????????????????????????????????????????????
?               Emulator Network Services                      ?
?                  (Native C# code)                            ?
???????????????????????????????????????????????????????????????
?  Socket Manager  ?  DNS Resolver  ?  Connection Pool         ?
???????????????????????????????????????????????????????????????
                          ? .NET networking
                          ?
???????????????????????????????????????????????????????????????
?                   Host Network Stack                         ?
?                  (TCP/IP, DNS, etc.)                         ?
???????????????????????????????????????????????????????????????
```

### 2.2 Memory Layout

ENS uses a dedicated page in the I/O space:

| Address Range   | Purpose                                    |
|-----------------|--------------------------------------------|
| $C800-$C8FF     | ENS ROM (when selected)                    |
| $C0F0-$C0FF     | ENS soft switches / status registers       |

### 2.3 Data Buffers

ENS uses guest memory buffers for data transfer:

| Buffer          | Default Address | Size    | Purpose                |
|-----------------|-----------------|---------|------------------------|
| Command Block   | $0300-$031F     | 32 bytes| Command parameters     |
| TX Buffer       | $0320-$07FF     | 1248 B  | Outgoing data          |
| RX Buffer       | $0800-$0BFF     | 1024 B  | Incoming data          |
| String Buffer   | $0C00-$0CFF     | 256 B   | Hostnames, URLs        |

These addresses are configurable via the command block.

---

## 3. Command Interface

### 3.1 Command Block Structure

All ENS commands use a command block at a configurable address (default $0300):

```
Offset  Size  Field           Description
------  ----  -----------     ----------------------------------
+$00    1     Command         Command number (see section 3.2)
+$01    1     Socket          Socket handle (0-7)
+$02    1     Status          Result status (set by ENS)
+$03    1     Flags           Command-specific flags
+$04    2     Length          Data length (bytes)
+$06    2     TxBuffer        TX buffer address
+$08    2     RxBuffer        RX buffer address
+$0A    2     Port            Local or remote port
+$0C    4     Address         IP address (big-endian)
+$10    2     StringPtr       Pointer to string (hostname, etc.)
+$12    2     Timeout         Timeout in 1/60 second ticks
+$14    2     BytesReady      Bytes available to read
+$16    2     BytesSent       Bytes actually sent
+$18    8     Reserved        For future expansion
```

### 3.2 Command Numbers

| Cmd  | Name           | Description                              |
|------|----------------|------------------------------------------|
| $00  | ENS_STATUS     | Get ENS status and version               |
| $01  | ENS_INIT       | Initialize ENS subsystem                 |
| $02  | ENS_SHUTDOWN   | Shutdown ENS and close all sockets       |
| $10  | SOCK_OPEN      | Open a new socket                        |
| $11  | SOCK_CLOSE     | Close a socket                           |
| $12  | SOCK_CONNECT   | Connect to remote host (TCP)             |
| $13  | SOCK_LISTEN    | Listen for connections (TCP)             |
| $14  | SOCK_ACCEPT    | Accept incoming connection               |
| $15  | SOCK_SEND      | Send data                                |
| $16  | SOCK_RECV      | Receive data                             |
| $17  | SOCK_SENDTO    | Send datagram (UDP)                      |
| $18  | SOCK_RECVFROM  | Receive datagram (UDP)                   |
| $19  | SOCK_STATUS    | Get socket status                        |
| $1A  | SOCK_POLL      | Check for activity on socket             |
| $20  | DNS_RESOLVE    | Resolve hostname to IP address           |
| $21  | DNS_REVERSE    | Reverse lookup IP to hostname            |
| $30  | HTTP_GET       | Simple HTTP GET request                  |
| $31  | HTTP_POST      | Simple HTTP POST request                 |

### 3.3 Status Codes

| Code | Name              | Description                          |
|------|-------------------|--------------------------------------|
| $00  | ENS_OK            | Operation successful                 |
| $01  | ENS_ERR_PARAM     | Invalid parameter                    |
| $02  | ENS_ERR_NOSOCK    | No socket available                  |
| $03  | ENS_ERR_BADSOCK   | Invalid socket handle                |
| $04  | ENS_ERR_NOTCONN   | Socket not connected                 |
| $05  | ENS_ERR_TIMEOUT   | Operation timed out                  |
| $06  | ENS_ERR_REFUSED   | Connection refused                   |
| $07  | ENS_ERR_NETDOWN   | Network unavailable                  |
| $08  | ENS_ERR_HOSTUNR   | Host unreachable                     |
| $09  | ENS_ERR_RESET     | Connection reset                     |
| $0A  | ENS_ERR_DNS       | DNS resolution failed                |
| $0B  | ENS_ERR_BUFFER    | Buffer too small                     |
| $0C  | ENS_ERR_WOULDBLK  | Would block (non-blocking mode)      |
| $FF  | ENS_ERR_INTERNAL  | Internal error                       |

### 3.4 Socket Types

| Value | Type      | Description                              |
|-------|-----------|------------------------------------------|
| $01   | SOCK_TCP  | Stream socket (TCP)                      |
| $02   | SOCK_UDP  | Datagram socket (UDP)                    |

---

## 4. Command Details

### 4.1 ENS_STATUS ($00)

Returns ENS version and network status.

**Input**: None

**Output**:
```
+$02: Status = ENS_OK
+$04: Version (low = minor, high = major)
+$06: Flags
      Bit 0: Network available
      Bit 1: IPv6 supported
      Bit 2-7: Reserved
+$0C: Host IP address
```

### 4.2 SOCK_OPEN ($10)

Opens a new socket.

**Input**:
```
+$03: Flags = Socket type (SOCK_TCP or SOCK_UDP)
+$0A: Port = Local port (0 = auto-assign)
```

**Output**:
```
+$01: Socket = Assigned socket handle (0-7)
+$02: Status = ENS_OK or error
+$0A: Port = Assigned local port
```

### 4.3 SOCK_CONNECT ($12)

Connects a TCP socket to a remote host.

**Input**:
```
+$01: Socket = Socket handle
+$0A: Port = Remote port
+$0C: Address = Remote IP (or 0 if using hostname)
+$10: StringPtr = Hostname pointer (if Address = 0)
+$12: Timeout = Connection timeout
```

**Output**:
```
+$02: Status = ENS_OK or error
```

### 4.4 SOCK_SEND ($15)

Sends data on a connected socket.

**Input**:
```
+$01: Socket = Socket handle
+$04: Length = Bytes to send
+$06: TxBuffer = Data buffer address
```

**Output**:
```
+$02: Status = ENS_OK or error
+$16: BytesSent = Bytes actually sent
```

### 4.5 SOCK_RECV ($16)

Receives data from a connected socket.

**Input**:
```
+$01: Socket = Socket handle
+$04: Length = Max bytes to receive
+$08: RxBuffer = Receive buffer address
+$12: Timeout = Receive timeout (0 = non-blocking)
```

**Output**:
```
+$02: Status = ENS_OK, ENS_ERR_WOULDBLK, or error
+$04: Length = Bytes received
```

### 4.6 DNS_RESOLVE ($20)

Resolves a hostname to an IP address.

**Input**:
```
+$10: StringPtr = Hostname pointer (null-terminated)
+$12: Timeout = Resolution timeout
```

**Output**:
```
+$02: Status = ENS_OK or error
+$0C: Address = Resolved IP address
```

### 4.7 HTTP_GET ($30)

Performs a simple HTTP GET request.

**Input**:
```
+$04: Length = Max response length
+$08: RxBuffer = Response buffer address
+$10: StringPtr = URL pointer (null-terminated)
+$12: Timeout = Request timeout
```

**Output**:
```
+$02: Status = ENS_OK or error
+$03: Flags = HTTP status code / 100 (2=2xx, 4=4xx, 5=5xx)
+$04: Length = Response body length
```

---

## 5. ROM Interface

### 5.1 Entry Points

The ENS ROM provides these entry points:

| Address | Name           | Description                          |
|---------|----------------|--------------------------------------|
| $C800   | ENS_ENTRY      | Main entry point (command in A)      |
| $C803   | ENS_VERSION    | Returns version in A.X               |
| $C806   | ENS_POLL       | Quick poll, returns status in A      |

### 5.2 Calling Convention

```assembly
; Example: Connect to a server
        LDA #<CmdBlock      ; Set up command block pointer
        STA $06
        LDA #>CmdBlock
        STA $07
        
        LDA #SOCK_OPEN      ; Open socket
        STA CmdBlock+$00
        LDA #SOCK_TCP
        STA CmdBlock+$03
        JSR $C800
        BCS Error
        
        LDA CmdBlock+$01    ; Save socket handle
        STA MySocket
        
        LDA #SOCK_CONNECT   ; Connect
        STA CmdBlock+$00
        LDA #80             ; Port 80
        STA CmdBlock+$0A
        LDA #0
        STA CmdBlock+$0B
        LDA #<Hostname      ; Use hostname
        STA CmdBlock+$10
        LDA #>Hostname
        STA CmdBlock+$11
        JSR $C800
        BCS Error
        
        ; Connected!
        RTS
        
Error:  LDA CmdBlock+$02    ; Get error code
        ; Handle error...
        RTS

CmdBlock: .res 32
MySocket: .byte 0
Hostname: .asciiz "example.com"
```

### 5.3 Return Convention

- **Carry clear**: Operation successful
- **Carry set**: Error occurred (check Status field)
- **A register**: Command-specific result

---

## 6. BASIC Integration

### 6.1 Extended BASIC Commands

ENS can be exposed through extended BASIC commands:

```basic
10 REM Open a TCP connection
20 SOCK = &NET.OPEN("TCP")
30 &NET.CONNECT SOCK, "irc.example.com", 6667
40 REM Send data
50 &NET.SEND SOCK, "NICK AppleUser" + CHR$(13) + CHR$(10)
60 REM Receive data
70 A$ = &NET.RECV$(SOCK, 1024)
80 PRINT A$
90 REM Close
100 &NET.CLOSE SOCK
```

### 6.2 BASIC Functions

| Function              | Description                              |
|-----------------------|------------------------------------------|
| `NET.OPEN(type$)`     | Opens socket, returns handle             |
| `NET.CLOSE(sock)`     | Closes socket                            |
| `NET.CONNECT(s,h$,p)` | Connects to host:port                    |
| `NET.LISTEN(s,p)`     | Listens on port                          |
| `NET.SEND(s,d$)`      | Sends string data                        |
| `NET.RECV$(s,max)`    | Receives data as string                  |
| `NET.POLL(s)`         | Returns bytes available                  |
| `NET.STATUS(s)`       | Returns socket status                    |
| `NET.RESOLVE$(h$)`    | Resolves hostname to IP string           |
| `NET.GET$(url$)`      | Simple HTTP GET                          |

---

## 7. Implementation

### 7.1 ENS Service Interface

```csharp
/// <summary>
/// Emulator Network Services interface.
/// </summary>
public interface INetworkServices
{
    /// <summary>Gets whether network is available.</summary>
    bool IsNetworkAvailable { get; }
    
    /// <summary>Gets the ENS version.</summary>
    Version Version { get; }
    
    /// <summary>Opens a new socket.</summary>
    EnsResult<int> OpenSocket(SocketType type, ushort localPort = 0);
    
    /// <summary>Closes a socket.</summary>
    EnsResult CloseSocket(int socket);
    
    /// <summary>Connects a TCP socket to a remote endpoint.</summary>
    Task<EnsResult> ConnectAsync(int socket, string host, ushort port, TimeSpan timeout);
    
    /// <summary>Listens for incoming connections.</summary>
    EnsResult Listen(int socket, ushort port, int backlog = 1);
    
    /// <summary>Accepts an incoming connection.</summary>
    Task<EnsResult<int>> AcceptAsync(int socket, TimeSpan timeout);
    
    /// <summary>Sends data on a connected socket.</summary>
    Task<EnsResult<int>> SendAsync(int socket, ReadOnlyMemory<byte> data);
    
    /// <summary>Receives data from a socket.</summary>
    Task<EnsResult<int>> ReceiveAsync(int socket, Memory<byte> buffer, TimeSpan timeout);
    
    /// <summary>Sends a UDP datagram.</summary>
    Task<EnsResult<int>> SendToAsync(int socket, ReadOnlyMemory<byte> data, string host, ushort port);
    
    /// <summary>Receives a UDP datagram.</summary>
    Task<EnsResult<(int bytes, string host, ushort port)>> ReceiveFromAsync(
        int socket, Memory<byte> buffer, TimeSpan timeout);
    
    /// <summary>Gets socket status.</summary>
    EnsResult<SocketStatus> GetSocketStatus(int socket);
    
    /// <summary>Polls socket for available data.</summary>
    EnsResult<int> Poll(int socket);
    
    /// <summary>Resolves a hostname to IP address.</summary>
    Task<EnsResult<IPAddress>> ResolveAsync(string hostname, TimeSpan timeout);
    
    /// <summary>Performs a simple HTTP GET request.</summary>
    Task<EnsResult<HttpResponse>> HttpGetAsync(string url, TimeSpan timeout);
    
    /// <summary>Shuts down all network services.</summary>
    void Shutdown();
}

/// <summary>
/// Result of an ENS operation.
/// </summary>
public readonly record struct EnsResult(EnsStatus Status)
{
    public bool IsSuccess => Status == EnsStatus.Ok;
    
    public static EnsResult Ok => new(EnsStatus.Ok);
    public static EnsResult Error(EnsStatus status) => new(status);
}

/// <summary>
/// Result of an ENS operation with a value.
/// </summary>
public readonly record struct EnsResult<T>(EnsStatus Status, T Value)
{
    public bool IsSuccess => Status == EnsStatus.Ok;
    
    public static EnsResult<T> Ok(T value) => new(EnsStatus.Ok, value);
    public static EnsResult<T> Error(EnsStatus status) => new(status, default!);
}

/// <summary>
/// ENS status codes.
/// </summary>
public enum EnsStatus : byte
{
    Ok = 0x00,
    InvalidParameter = 0x01,
    NoSocketAvailable = 0x02,
    InvalidSocket = 0x03,
    NotConnected = 0x04,
    Timeout = 0x05,
    ConnectionRefused = 0x06,
    NetworkDown = 0x07,
    HostUnreachable = 0x08,
    ConnectionReset = 0x09,
    DnsError = 0x0A,
    BufferTooSmall = 0x0B,
    WouldBlock = 0x0C,
    InternalError = 0xFF
}
```

### 7.2 Socket Manager

```csharp
/// <summary>
/// Manages ENS sockets.
/// </summary>
public sealed class EnsSocketManager : INetworkServices
{
    private const int MaxSockets = 8;
    private readonly EnsSocket?[] _sockets = new EnsSocket?[MaxSockets];
    
    public EnsResult<int> OpenSocket(SocketType type, ushort localPort = 0)
    {
        // Find free slot
        int handle = Array.FindIndex(_sockets, s => s == null);
        if (handle < 0)
            return EnsResult<int>.Error(EnsStatus.NoSocketAvailable);
        
        try
        {
            var socket = type switch
            {
                SocketType.Tcp => new EnsSocket(new TcpClient()),
                SocketType.Udp => new EnsSocket(new UdpClient(localPort)),
                _ => throw new ArgumentException("Invalid socket type")
            };
            
            _sockets[handle] = socket;
            return EnsResult<int>.Ok(handle);
        }
        catch
        {
            return EnsResult<int>.Error(EnsStatus.InternalError);
        }
    }
    
    public async Task<EnsResult> ConnectAsync(int socket, string host, ushort port, TimeSpan timeout)
    {
        if (!TryGetSocket(socket, out var ens))
            return EnsResult.Error(EnsStatus.InvalidSocket);
        
        if (ens.TcpClient == null)
            return EnsResult.Error(EnsStatus.InvalidParameter);
        
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            await ens.TcpClient.ConnectAsync(host, port, cts.Token);
            ens.Stream = ens.TcpClient.GetStream();
            return EnsResult.Ok;
        }
        catch (OperationCanceledException)
        {
            return EnsResult.Error(EnsStatus.Timeout);
        }
        catch (SocketException ex)
        {
            return EnsResult.Error(MapSocketException(ex));
        }
    }
    
    public async Task<EnsResult<int>> SendAsync(int socket, ReadOnlyMemory<byte> data)
    {
        if (!TryGetSocket(socket, out var ens))
            return EnsResult<int>.Error(EnsStatus.InvalidSocket);
        
        if (ens.Stream == null)
            return EnsResult<int>.Error(EnsStatus.NotConnected);
        
        try
        {
            await ens.Stream.WriteAsync(data);
            return EnsResult<int>.Ok(data.Length);
        }
        catch (Exception)
        {
            return EnsResult<int>.Error(EnsStatus.ConnectionReset);
        }
    }
    
    public async Task<EnsResult<int>> ReceiveAsync(int socket, Memory<byte> buffer, TimeSpan timeout)
    {
        if (!TryGetSocket(socket, out var ens))
            return EnsResult<int>.Error(EnsStatus.InvalidSocket);
        
        if (ens.Stream == null)
            return EnsResult<int>.Error(EnsStatus.NotConnected);
        
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            int bytes = await ens.Stream.ReadAsync(buffer, cts.Token);
            
            if (bytes == 0)
                return EnsResult<int>.Error(EnsStatus.ConnectionReset);
            
            return EnsResult<int>.Ok(bytes);
        }
        catch (OperationCanceledException)
        {
            return EnsResult<int>.Error(timeout == TimeSpan.Zero 
                ? EnsStatus.WouldBlock 
                : EnsStatus.Timeout);
        }
    }
    
    public async Task<EnsResult<IPAddress>> ResolveAsync(string hostname, TimeSpan timeout)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            var addresses = await Dns.GetHostAddressesAsync(hostname, cts.Token);
            
            var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            if (ipv4 == null)
                return EnsResult<IPAddress>.Error(EnsStatus.DnsError);
            
            return EnsResult<IPAddress>.Ok(ipv4);
        }
        catch
        {
            return EnsResult<IPAddress>.Error(EnsStatus.DnsError);
        }
    }
    
    private bool TryGetSocket(int handle, out EnsSocket socket)
    {
        socket = null!;
        if (handle < 0 || handle >= MaxSockets)
            return false;
        
        socket = _sockets[handle]!;
        return socket != null;
    }
    
    private static EnsStatus MapSocketException(SocketException ex)
    {
        return ex.SocketErrorCode switch
        {
            SocketError.ConnectionRefused => EnsStatus.ConnectionRefused,
            SocketError.HostUnreachable => EnsStatus.HostUnreachable,
            SocketError.NetworkUnreachable => EnsStatus.NetworkDown,
            SocketError.TimedOut => EnsStatus.Timeout,
            SocketError.ConnectionReset => EnsStatus.ConnectionReset,
            _ => EnsStatus.InternalError
        };
    }
}

/// <summary>
/// Internal socket wrapper.
/// </summary>
internal sealed class EnsSocket : IDisposable
{
    public TcpClient? TcpClient { get; }
    public UdpClient? UdpClient { get; }
    public NetworkStream? Stream { get; set; }
    
    public EnsSocket(TcpClient tcp) => TcpClient = tcp;
    public EnsSocket(UdpClient udp) => UdpClient = udp;
    
    public void Dispose()
    {
        Stream?.Dispose();
        TcpClient?.Dispose();
        UdpClient?.Dispose();
    }
}
```

### 7.3 Trap Handler

```csharp
/// <summary>
/// ENS ROM trap handler.
/// </summary>
public sealed class EnsRomTrap
{
    private readonly INetworkServices _network;
    private readonly IMemoryBus _bus;
    
    public TrapResult Execute(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        // Read command block address from $06-$07
        ushort cmdBlock = bus.Read16(0x0006);
        
        // Read command
        byte command = bus.Read8(cmdBlock);
        
        var result = command switch
        {
            0x00 => HandleStatus(cmdBlock),
            0x10 => HandleSockOpen(cmdBlock),
            0x11 => HandleSockClose(cmdBlock),
            0x12 => HandleSockConnect(cmdBlock, context),
            0x15 => HandleSockSend(cmdBlock, context),
            0x16 => HandleSockRecv(cmdBlock, context),
            0x20 => HandleDnsResolve(cmdBlock, context),
            0x30 => HandleHttpGet(cmdBlock, context),
            _ => EnsStatus.InvalidParameter
        };
        
        // Set status in command block
        bus.Write8((ushort)(cmdBlock + 0x02), (byte)result);
        
        // Set carry based on result
        cpu.SetCarry(result != EnsStatus.Ok);
        
        return new TrapResult(
            Handled: true,
            CyclesConsumed: new Cycle(100),
            ReturnAddress: null);
    }
    
    private EnsStatus HandleSockOpen(ushort cmdBlock)
    {
        byte flags = _bus.Read8((ushort)(cmdBlock + 0x03));
        ushort port = _bus.Read16((ushort)(cmdBlock + 0x0A));
        
        var type = (flags & 0x0F) switch
        {
            0x01 => SocketType.Tcp,
            0x02 => SocketType.Udp,
            _ => (SocketType)0
        };
        
        var result = _network.OpenSocket(type, port);
        
        if (result.IsSuccess)
        {
            _bus.Write8((ushort)(cmdBlock + 0x01), (byte)result.Value);
        }
        
        return result.Status;
    }
    
    private EnsStatus HandleSockConnect(ushort cmdBlock, IEventContext context)
    {
        byte socket = _bus.Read8((ushort)(cmdBlock + 0x01));
        ushort port = _bus.Read16((ushort)(cmdBlock + 0x0A));
        uint ipAddr = _bus.Read32((ushort)(cmdBlock + 0x0C));
        ushort strPtr = _bus.Read16((ushort)(cmdBlock + 0x10));
        ushort timeout = _bus.Read16((ushort)(cmdBlock + 0x12));
        
        string host;
        if (ipAddr != 0)
        {
            host = new IPAddress(BitConverter.GetBytes(ipAddr)).ToString();
        }
        else
        {
            host = ReadString(strPtr);
        }
        
        var timeoutSpan = TimeSpan.FromMilliseconds(timeout * 1000 / 60);
        
        // Execute async operation synchronously for trap
        var result = _network.ConnectAsync(socket, host, port, timeoutSpan)
            .GetAwaiter().GetResult();
        
        return result.Status;
    }
    
    private string ReadString(ushort address)
    {
        var sb = new StringBuilder();
        byte b;
        while ((b = _bus.Read8(address++)) != 0 && sb.Length < 256)
        {
            sb.Append((char)b);
        }
        return sb.ToString();
    }
}
```

---

## 8. Bus Architecture Integration

### 8.1 ENS as IBusTarget

```csharp
/// <summary>
/// ENS soft switch registers.
/// </summary>
public sealed class EnsSoftSwitches : IBusTarget
{
    private readonly INetworkServices _network;
    
    /// <inheritdoc/>
    public TargetCaps Capabilities => TargetCaps.SideEffects;
    
    /// <inheritdoc/>
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        int offset = (int)(physicalAddress & 0x0F);
        
        return offset switch
        {
            0x00 => _network.IsNetworkAvailable ? (byte)0x80 : (byte)0x00,
            0x01 => (byte)_network.Version.Major,
            0x02 => (byte)_network.Version.Minor,
            _ => 0xFF
        };
    }
    
    /// <inheritdoc/>
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        // Status registers are read-only
    }
}
```

### 8.2 Composite Page Integration

```csharp
public sealed class AppleIIIOPage : ICompositeTarget
{
    private readonly EnsSoftSwitches _ensSwitches;
    
    /// <inheritdoc/>
    public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        return offset switch
        {
            >= 0xF0 and <= 0xFF => _ensSwitches,  // ENS status registers
            _ => null
        };
    }
}
```

### 8.3 Device Registry

```csharp
public void RegisterEnsDevices(IDeviceRegistry registry)
{
    registry.Register(
        registry.GenerateId(),
        DevicePageId.Create(DevicePageClass.Network, instance: 0, page: 0),
        kind: "EnsController",
        name: "Emulator Network Services",
        wiringPath: "main/network/ens");
}
```

---

## 9. Security Considerations

### 9.1 Network Access Control

ENS should provide configuration options for:

- **Allow/deny network access**: Master switch for all network operations
- **Allowed hosts whitelist**: Restrict connections to specific hosts
- **Allowed ports whitelist**: Restrict to specific port ranges
- **Outbound only**: Disable listening capabilities

### 9.2 Configuration Example

```json
{
  "ens": {
    "enabled": true,
    "allowInbound": false,
    "allowedHosts": ["*"],
    "allowedPorts": [80, 443, 23, 6667],
    "maxSockets": 8,
    "defaultTimeout": 30
  }
}
```

---

## Document History

| Version | Date       | Changes                            |
|---------|------------|------------------------------------|
| 1.0     | 2025-12-28 | Initial specification              |
