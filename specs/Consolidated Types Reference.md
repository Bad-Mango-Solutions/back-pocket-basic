# Consolidated Types Reference

**Document Purpose:** Consolidated reference of all interface, model, and enum types defined across specification documents.  
**Version:** 1.0  
**Date:** Generated from specification files

---

## Table of Contents

1. [CPU and Registers](#1-cpu-and-registers)
2. [Memory and Bus Architecture](#2-memory-and-bus-architecture)
3. [Signal Bus and Scheduling](#3-signal-bus-and-scheduling)
4. [Device and Peripheral Architecture](#4-device-and-peripheral-architecture)
5. [Video Display](#5-video-display)
6. [Storage (SmartPort)](#6-storage-smartport)
7. [Serial Communications](#7-serial-communications)
8. [Apple Desktop Bus (ADB)](#8-apple-desktop-bus-adb)
9. [Network Services](#9-network-services)
10. [Keyboard and Input](#10-keyboard-and-input)
11. [Machine and Debug](#11-machine-and-debug)
12. [UI Services](#12-ui-services)
13. [Settings and Configuration](#13-settings-and-configuration)

---

## 1. CPU and Registers

### Enums

```csharp
/// <summary>
/// Identifies which physical CPU variant the emulator is modeling.
/// </summary>
/// <remarks>
/// This is a static property of the CPU implementation that never changes at runtime.
/// <para>
/// CpuFamily describes the hardware—it's like asking "what chip is soldered to the board?"
/// A 65C816 chip always has CpuFamily.Cpu65C816, even when running in emulation mode
/// where it behaves like a 65C02.
/// </para>
/// <para>
/// This enum is used for:
/// </para>
/// <list type="bullet">
/// <item><description>CPU construction and capability discovery</description></item>
/// <item><description>Instruction decoder selection (which opcodes are valid)</description></item>
/// <item><description>Determining available registers and addressing modes</description></item>
/// </list>
/// </remarks>
public enum CpuFamily
{
    Cpu65C02,     // WDC 65C02 (Pocket2e target)
    Cpu65C816,    // WDC 65C816 (PocketGS target)
    Cpu65832,     // Speculative 32-bit (PocketME target)
}

/// <summary>
/// Reflects the current runtime execution mode of the CPU based on CP and E flags.
/// A 65816 CPU can switch between Mode65C02 and Mode65816 during execution.
/// A 65832 can run in all three modes.
/// </summary>
/// <remarks>
/// This enum is derived from the CP and E register flags:
/// - CP=1, E=1: Mode65C02
/// - CP=1, E=0: Mode65816  
/// - CP=0: Mode65832
/// </remarks>
public enum ArchitecturalMode
{
    Mode65C02,    // CP=1, E=1: Legacy 6502 semantics
    Mode65816,    // CP=1, E=0: 16-bit native mode
    Mode65832,    // CP=0: Native 32-bit mode
}

public enum CpuRunState
{
    Running,
    WaitingForInterrupt,  // WAI instruction
    Stopped,              // STP instruction or stop requested
    Halted,               // Fatal error or unrecoverable state
}

public enum PrivilegeLevel : byte
{
    User = 0,       // U: Unprivileged code
    Kernel = 1,     // K: OS kernel
    Hypervisor = 2, // H: Reserved for future
}
```

### Interfaces

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
    
    void RequestStop();
    void ClearStopRequest();
    bool IsStopRequested { get; }
}

public interface ICpuFactory
{
    CpuFamily Family { get; }
    ICpu Create(IMemoryBus bus, ISignalBus signals);
}
```

### Structs

```csharp
public readonly record struct CpuStepResult(
    CpuRunState State,
    Cycle CyclesConsumed
);

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Registers
{
    public readonly DWord ZR;
    public RegisterAccumulator A;
    public RegisterIndex X;
    public RegisterIndex Y;
    public RegisterDirectPage D;
    public RegisterStackPointer SP;
    public RegisterProgramCounter PC;
    public ProcessorStatusFlags P;
    public bool E;
    public bool CP;
    public byte DBR;
    public byte PBR;
    public DWord R0, R1, R2, R3, R4, R5, R6, R7;
    public SystemRegisters System;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SystemRegisters
{
    public DWord CR0;
    public DWord PTBR;
    public DWord VBAR;
    public DWord FAR;
    public DWord FSC;
    public DWord TLS;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterAccumulator
{
    public DWord acc;
    public readonly byte GetByte() => (byte)(acc & 0xFF);
    public readonly Word GetWord() => (Word)(acc & 0xFFFF);
    public readonly DWord GetDWord() => acc;
    public void SetByte(byte value) => acc = (acc & 0xFFFFFF00) | value;
    public void SetWord(Word value) => acc = (acc & 0xFFFF0000) | value;
    public void SetDWord(DWord value) => acc = value;
}

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

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RegisterDirectPage
{
    public Addr direct;
    public readonly Word GetWord() => (Word)(direct & 0xFFFF);
    public readonly Addr GetAddr() => direct;
    public void SetWord(Word value) => direct = (direct & 0xFFFF0000) | value;
    public void SetAddr(Addr value) => direct = value;
}

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

---

## 2. Memory and Bus Architecture

### Enums

```csharp
/// <summary>
/// Defines the bus access semantics for memory operations.
/// This determines whether the bus prefers atomic wide operations or
/// decomposes multi-byte accesses into individual byte cycles.
/// </summary>
/// <remarks>
/// This is distinct from CpuFamily (which CPU variant) and ArchitecturalMode (CPU runtime state).
/// BusAccessMode is a per-access property that affects only how the bus handles wide operations.
/// </remarks>
public enum BusAccessMode : byte
{
    /// <summary>
    /// Prefers atomic wide operations when the target supports them.
    /// Used by 65832 native mode for better performance with modern memory.
    /// </summary>
    Atomic = 0,
    
    /// <summary>
    /// Decomposes wide accesses into byte-wise cycles.
    /// Matches Apple II expectations where peripherals observe individual
    /// memory access cycles. Required for accurate emulation of devices
    /// that depend on seeing each byte access separately.
    /// </summary>
    Decomposed = 1,
}

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
    NoSideFx     = 1 << 0,
    LittleEndian = 1 << 1,
    Atomic       = 1 << 2,
    Decompose    = 1 << 3
}

public enum FaultKind : byte
{
    None = 0,
    Unmapped,
    Permission,
    Nx,
    Misaligned,
    DeviceFault
}

public enum FaultStatusCode : uint
{
    NotPresent = 0,
    ReadViolation = 1,
    WriteViolation = 2,
    ExecuteViolation = 3,
    PrivilegeViolation = 4,
    ReservedBitViolation = 5,
    DeviceFault = 6
}

[Flags]
public enum TargetCaps : uint
{
    None          = 0,
    SupportsPeek  = 1 << 0,
    SupportsWide  = 1 << 1,
    SideEffects   = 1 << 2,
    TimingSense   = 1 << 3
}

[Flags]
public enum PagePerms : byte
{
    None    = 0,
    Read    = 1 << 0,
    Write   = 1 << 1,
    Execute = 1 << 2,
    RW  = Read | Write,
    RX  = Read | Execute,
    RWX = Read | Write | Execute
}

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

[Flags]
public enum PageTableEntryFlags : uint
{
    Present     = 1 << 0,
    Readable    = 1 << 1,
    Writable    = 1 << 2,
    Executable  = 1 << 3,
    User        = 1 << 4,
    Accessed    = 1 << 5,
    Dirty       = 1 << 6,
    Global      = 1 << 7,
    Device      = 1 << 8,
}

public enum DevicePageClass : byte
{
    Invalid = 0x0,
    CompatIO = 0x1,
    SlotROM = 0x2,
    Framebuffer = 0x3,
    Storage = 0x4,
    Network = 0x5,
    Timer = 0x6,
    Debug = 0x7,
    Audio = 0x8,
    Input = 0x9,
    Dma = 0xA,
    SystemControl = 0xB
}
```

### Interfaces

```csharp
public interface IBusTarget
{
    TargetCaps Capabilities { get; }
    byte Read8(Addr physicalAddress, in BusAccess access);
    void Write8(Addr physicalAddress, byte value, in BusAccess access);
    Word Read16(Addr physicalAddress, in BusAccess access);
    void Write16(Addr physicalAddress, Word value, in BusAccess access);
    DWord Read32(Addr physicalAddress, in BusAccess access);
    void Write32(Addr physicalAddress, DWord value, in BusAccess access);
}

public interface ICompositeTarget : IBusTarget
{
    IBusTarget? ResolveTarget(Addr offset, AccessIntent intent);
    RegionTag GetSubRegionTag(Addr offset);
}

public interface IMemoryBus
{
    int PageShift { get; }
    Addr PageMask { get; }
    int PageCount { get; }
    
    byte Read8(in BusAccess access);
    void Write8(in BusAccess access, byte value);
    Word Read16(in BusAccess access);
    void Write16(in BusAccess access, Word value);
    DWord Read32(in BusAccess access);
    void Write32(in BusAccess access, DWord value);
    
    BusResult<byte> TryRead8(in BusAccess access);
    BusFault TryWrite8(in BusAccess access, byte value);
    BusResult<Word> TryRead16(in BusAccess access);
    BusFault TryWrite16(in BusAccess access, Word value);
    BusResult<DWord> TryRead32(in BusAccess access);
    BusFault TryWrite32(in BusAccess access, DWord value);
    
    PageEntry GetPageEntry(Addr address);
    ref readonly PageEntry GetPageEntryByIndex(int pageIndex);
    void MapPage(int pageIndex, PageEntry entry);
    void MapPageRange(int startPage, int pageCount, int deviceId,
                      RegionTag tag, PagePerms perms, TargetCaps caps,
                      IBusTarget target, Addr physBase);
    void RemapPage(int pageIndex, IBusTarget newTarget, Addr newPhysBase);
    void RemapPage(int pageIndex, PageEntry newEntry);
    void RemapPageRange(int startPage, int pageCount, 
                        IBusTarget newTarget, Addr newPhysBase);
}
```

### Structs

```csharp
public readonly record struct BusAccess(
    Addr Address,
    DWord Value,
    byte WidthBits,
    BusAccessMode AccessMode,    // How to handle wide operations (atomic vs decomposed)
    bool EmulationE,             // E flag state for compatibility behavior
    AccessIntent Intent,
    int SourceId,
    ulong Cycle,
    AccessFlags Flags
);

public readonly record struct BusFault(
    FaultKind Kind,
    Addr Address,
    byte WidthBits,
    AccessIntent Intent,
    BusAccessMode AccessMode,
    int SourceId,
    int DeviceId,
    RegionTag RegionTag,
    ulong Cycle
);

public readonly record struct BusResult<T>(T Value, BusFault Fault, ulong Cycles = 0)
    where T : struct;

public readonly record struct BusResult(BusFault Fault, ulong Cycles = 0);

public readonly record struct PageEntry(
    int DeviceId,
    RegionTag RegionTag,
    PagePerms Perms,
    TargetCaps Caps,
    IBusTarget Target,
    Addr PhysBase
);

public readonly record struct PageTableEntry(uint Raw);

public readonly struct DevicePageId
{
    public DevicePageClass Class { get; }
    public byte Instance { get; }
    public byte Page { get; }
    public uint RawValue { get; }
    public bool IsValid => Class != DevicePageClass.Invalid;
}

public readonly record struct DeviceInfo(
    int Id,
    DevicePageId PageId,
    string Kind,
    string Name,
    string WiringPath
);
```

---

## 3. Signal Bus and Scheduling

### Enums

```csharp
public enum SignalLine
{
    IRQ,
    NMI,
    Reset,
    RDY,
    DmaReq,
    Sync
}

public enum ScheduledEventKind
{
    DeviceTimer,
    InterruptLineChange,
    DmaPhase,
    AudioTick,
    VideoScanline,
    DeferredWork,
    Custom
}
```

### Interfaces

```csharp
public interface ISignalBus
{
    void Assert(SignalLine line, int deviceId);
    void Deassert(SignalLine line, int deviceId);
    bool IsAsserted(SignalLine line);
    bool ConsumeNmiEdge();
    event Action<SignalLine, bool, int, ulong>? SignalChanged;
}

public interface IScheduler
{
    Cycle Now { get; }
    EventHandle ScheduleAt(Cycle due, ScheduledEventKind kind, int priority, 
                           Action<IEventContext> callback, object? tag = null);
    EventHandle ScheduleAfter(Cycle delta, ScheduledEventKind kind, int priority,
                              Action<IEventContext> callback, object? tag = null);
    bool Cancel(EventHandle handle);
    void Advance(Cycle delta);
    void DispatchDue();
    Cycle? PeekNextDue();
    bool JumpToNextEventAndDispatch();
    void Reset();
    int PendingEventCount { get; }
}

public interface IEventContext
{
    Cycle Now { get; }
    IScheduler Scheduler { get; }
    ISignalBus Signals { get; }
    IMemoryBus Bus { get; }
}

public interface IScheduledDevice
{
    void Initialize(IEventContext context);
}

public interface ISchedulable
{
    ulong Execute(ulong currentCycle);
}

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

### Structs

```csharp
public readonly record struct Cycle(ulong Value);

public readonly record struct EventHandle(ulong Id);
```

---

## 4. Device and Peripheral Architecture

### Interfaces

```csharp
public interface IPeripheral : IScheduledDevice
{
    string Name { get; }
    string DeviceType { get; }
    IBusTarget? MMIORegion { get; }
    IBusTarget? ROMRegion { get; }
    IBusTarget? ExpansionROMRegion { get; }
    int SlotNumber { get; set; }
    void OnExpansionROMSelected();
    void OnExpansionROMDeselected();
    void Reset();
}

public interface ISlotManager
{
    IReadOnlyDictionary<int, IPeripheral> Slots { get; }
    int? ActiveExpansionSlot { get; }
    void Install(int slot, IPeripheral card);
    void Remove(int slot);
    IPeripheral? GetCard(int slot);
    void SelectExpansionSlot(int slot);
    void DeselectExpansionSlot();
    void HandleSlotROMAccess(Addr address);
    void Reset();
}

public interface IDevicePageObject
{
    int DevicePageId { get; }
    DevicePageClass Class { get; }
    byte Read8(uint offset, in BusAccess context);
    void Write8(uint offset, byte value, in BusAccess context);
    void RejectAccess(in BusAccess context);
}
```

### Classes

```csharp
public enum CompatibilityPersonality : uint
{
    None = 0,
    AppleIIe = 1,
    AppleIIc = 2,
    AppleIIgs = 3
}
```

---

## 5. Video Display

### Enums

```csharp
public enum DisplayMode
{
    Text40,
    Text80,
    LoRes,
    DoubleLoRes,
    HiRes,
    DoubleHiRes,
    Mixed,
    SuperHiRes320,
    SuperHiRes640
}

public enum ScalingMode
{
    Integer,
    AspectCorrect,
    Fill,
    Native
}

public enum ColorPalette
{
    Ntsc,
    Rgb,
    Green,
    Amber,
    White,
    Custom
}
```

### Interfaces

```csharp
public interface IVideoController : IScheduledDevice
{
    bool IsTextMode { get; }
    bool IsMixedMode { get; }
    bool IsHiResMode { get; }
    bool IsPage2 { get; }
    bool Is80ColumnMode { get; }
    bool IsDoubleHiResMode { get; }
    bool IsAltCharSet { get; }
    
    byte SetText();
    byte SetGraphics();
    byte SetFullScreen();
    byte SetMixed();
    byte SetPage1();
    byte SetPage2();
    byte SetLoRes();
    byte SetHiRes();
    
    void Enable80Column();
    void Disable80Column();
    void EnableAltCharSet();
    void DisableAltCharSet();
    void EnableDoubleHiRes();
    void DisableDoubleHiRes();
    
    bool IsVerticalBlanking { get; }
    int CurrentScanline { get; }
    void RenderFrame(Span<uint> buffer, int width, int height);
    
    event Action? VBlankStart;
    event Action? VBlankEnd;
}

public interface IIgsVideoController : IVideoController
{
    bool IsSuperHiResMode { get; }
    int BorderColor { get; set; }
    byte GetScanlineControlByte(int line);
    void SetScanlineControlByte(int line, byte value);
    ushort GetPaletteColor(int palette, int color);
    void SetPaletteColor(int palette, int color, ushort rgb);
    bool ScanlineInterruptsEnabled { get; set; }
    event Action<int>? ScanlineInterrupt;
    byte ShadowRegister { get; set; }
    bool IsTextShadowingEnabled { get; }
    bool IsHiResShadowingEnabled { get; }
    bool IsSuperHiResShadowingEnabled { get; }
}

public interface ICharacterGenerator
{
    byte GetCharacterRow(byte charCode, int scanline, bool flash, bool altCharSet);
}

public interface IDisplayService
{
    DisplayMode CurrentMode { get; }
    PixelSize NativeResolution { get; }
    double Scale { get; set; }
    ScalingMode ScalingMode { get; set; }
    double ScanlineIntensity { get; set; }
    ColorPalette Palette { get; set; }
    bool NtscArtifactColoring { get; set; }
    void AttachMachine(IMachine machine);
    void Detach();
    IMachine? AttachedMachine { get; }
    bool RenderFrame(WriteableBitmap target);
    event EventHandler<DisplayModeChangedEventArgs>? ModeChanged;
    event EventHandler? FrameReady;
}

public interface IDisplayRenderer
{
    void RenderFrame(Span<uint> buffer, IVideoController video, IMemoryBus bus);
    ColorPalette Palette { get; set; }
    IReadOnlyList<uint>? CustomPalette { get; set; }
    (int Width, int Height) RequiredBufferSize { get; }
}

public interface IPostProcessor
{
    void Apply(Span<uint> buffer, int width, int height, PostProcessingOptions options);
}
```

### Structs

```csharp
public record PostProcessingOptions
{
    public double ScanlineIntensity { get; init; } = 0.0;
    public double CrtCurvature { get; init; } = 0.0;
    public double BloomIntensity { get; init; } = 0.0;
    public double ColorFringing { get; init; } = 0.0;
    public double VignetteIntensity { get; init; } = 0.0;
}
```

---

## 6. Storage (SmartPort)

### Enums

```csharp
public enum SmartPortCommand : byte
{
    Status = 0x00,
    ReadBlock = 0x01,
    WriteBlock = 0x02,
    Format = 0x03,
    Control = 0x04,
    Init = 0x05,
    Open = 0x06,
    Close = 0x07,
    Read = 0x08,
    Write = 0x09,
    StatusExt = 0x40,
    ReadExt = 0x41,
    WriteExt = 0x42,
    FormatExt = 0x43,
    ControlExt = 0x44
}

public enum SmartPortError : byte
{
    NoError = 0x00,
    BadCommand = 0x01,
    BadParamCount = 0x04,
    InvalidUnit = 0x21,
    IOError = 0x27,
    NoDevice = 0x28,
    WriteProtected = 0x2B,
    DiskSwitched = 0x2D,
    DeviceOffline = 0x2E,
    VolumeTooLarge = 0x2F
}
```

### Interfaces

```csharp
public interface ISmartPortController : IPeripheral
{
    int DeviceCount { get; }
    ISmartPortDevice? GetDevice(int unitNumber);
    int AddDevice(ISmartPortDevice device);
    bool RemoveDevice(int unitNumber);
    SmartPortResult ExecuteCommand(SmartPortCommand command, in SmartPortParams parameters);
}

public interface ISmartPortDevice
{
    byte DeviceType { get; }
    string DeviceName { get; }
    uint BlockCount { get; }
    bool IsOnline { get; }
    bool IsWriteProtected { get; }
    SmartPortResult ReadBlock(uint blockNumber, Span<byte> buffer);
    SmartPortResult WriteBlock(uint blockNumber, ReadOnlySpan<byte> buffer);
    SmartPortResult Format();
    SmartPortResult GetStatus(byte statusCode, Span<byte> buffer);
    SmartPortResult Control(byte controlCode, ReadOnlySpan<byte> parameters);
}
```

### Structs

```csharp
public readonly record struct SmartPortResult(
    byte ErrorCode,
    int BytesTransferred = 0
)
{
    public bool IsSuccess => ErrorCode == 0;
    public static SmartPortResult Success(int bytes = 0) => new(0, bytes);
    public static SmartPortResult Error(byte code) => new(code);
}
```

---

## 7. Serial Communications

### Enums

```csharp
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

public enum Parity
{
    None,
    Odd,
    Even,
    Mark,
    Space
}

public enum StopBits
{
    One = 1,
    Two = 2
}
```

### Interfaces

```csharp
public interface ISerialPort : IPeripheral
{
    int BaudRate { get; set; }
    int DataBits { get; set; }
    Parity Parity { get; set; }
    StopBits StopBits { get; set; }
    bool DataAvailable { get; }
    SerialStatus Status { get; }
    byte Read();
    void Write(byte data);
    void SetDTR(bool active);
    void SetRTS(bool active);
    bool CTS { get; }
    bool DSR { get; }
    bool DCD { get; }
    void Connect(ISerialDevice device);
    void Disconnect();
    event Action? DataReceived;
    event Action? TransmitEmpty;
}

public interface ISerialDevice
{
    string Name { get; }
    void ReceiveData(byte data);
    event Action<byte>? SendData;
    void SetCTS(bool active);
    void SetDSR(bool active);
    void SetDCD(bool active);
    void OnDTRChanged(bool active);
    void OnRTSChanged(bool active);
}
```

---

## 8. Apple Desktop Bus (ADB)

### Interfaces

```csharp
public interface IAdbDevice
{
    byte DefaultAddress { get; }
    byte Address { get; set; }
    byte HandlerId { get; }
    bool HasPendingData { get; }
    byte[]? Talk(int register);
    void Listen(int register, byte[] data);
    void Flush();
    void Reset();
}

public interface IAdbController : IScheduledDevice
{
    IReadOnlyList<IAdbDevice> Devices { get; }
    void Connect(IAdbDevice device);
    void Disconnect(IAdbDevice device);
    AdbResult SendCommand(byte command, byte[]? data = null);
    (byte address, byte[] data)? Poll();
    void SendReset();
    bool SrqPending { get; }
}
```

### Structs

```csharp
public readonly record struct AdbResult(
    bool Success,
    byte[]? Data
);
```

---

## 9. Network Services

### Enums

```csharp
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

public enum SocketType
{
    Tcp = 0x01,
    Udp = 0x02
}
```

### Interfaces

```csharp
public interface INetworkServices
{
    bool IsNetworkAvailable { get; }
    Version Version { get; }
    EnsResult<int> OpenSocket(SocketType type, ushort localPort = 0);
    EnsResult CloseSocket(int socket);
    Task<EnsResult> ConnectAsync(int socket, string host, ushort port, TimeSpan timeout);
    EnsResult Listen(int socket, ushort port, int backlog = 1);
    Task<EnsResult<int>> AcceptAsync(int socket, TimeSpan timeout);
    Task<EnsResult<int>> SendAsync(int socket, ReadOnlyMemory<byte> data);
    Task<EnsResult<int>> ReceiveAsync(int socket, Memory<byte> buffer, TimeSpan timeout);
    Task<EnsResult<int>> SendToAsync(int socket, ReadOnlyMemory<byte> data, string host, ushort port);
    Task<EnsResult<(int bytes, string host, ushort port)>> ReceiveFromAsync(
        int socket, Memory<byte> buffer, TimeSpan timeout);
    EnsResult<SocketStatus> GetSocketStatus(int socket);
    EnsResult<int> Poll(int socket);
    Task<EnsResult<IPAddress>> ResolveAsync(string hostname, TimeSpan timeout);
    Task<EnsResult<HttpResponse>> HttpGetAsync(string url, TimeSpan timeout);
    void Shutdown();
}
```

### Structs

```csharp
public readonly record struct EnsResult(EnsStatus Status)
{
    public bool IsSuccess => Status == EnsStatus.Ok;
    public static EnsResult Ok => new(EnsStatus.Ok);
    public static EnsResult Error(EnsStatus status) => new(status);
}

public readonly record struct EnsResult<T>(EnsStatus Status, T Value)
{
    public bool IsSuccess => Status == EnsStatus.Ok;
    public static EnsResult<T> Ok(T value) => new(EnsStatus.Ok, value);
    public static EnsResult<T> Error(EnsStatus status) => new(status, default!);
}
```

---

## 10. Keyboard and Input

### Enums

```csharp
[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 0x01,
    Control = 0x02,
    CapsLock = 0x08,
    Option = 0x40,
    Command = 0x80
}
```

### Interfaces

```csharp
public interface IKeyboardController
{
    bool KeyAvailable { get; }
    byte ReadKey();
    void ClearStrobe();
    KeyModifiers GetModifiers();
    void InjectKey(byte keyCode);
    void Reset();
}

public interface IInputService
{
    KeyboardMapping KeyMapping { get; set; }
    void HandleKeyDown(IMachine machine, KeyEventArgs e);
    void HandleKeyUp(IMachine machine, KeyEventArgs e);
    void HandleMouseMove(IMachine machine, Point position, Size displaySize);
    void HandleMouseButton(IMachine machine, int button, bool pressed);
    Task InjectTextAsync(IMachine machine, string text);
}
```

---

## 11. Machine and Debug

### Enums

```csharp
public enum MachineState
{
    Created,
    Initializing,
    Ready,
    Running,
    Paused,
    Stopped,
    Error
}

public enum MachinePersonality
{
    Pocket2e,
    Pocket2c,
    PocketGS,
    PocketME
}

public enum TrapCategory
{
    Text,
    Graphics,
    Sound,
    Disk,
    SlotFirmware,
    System,
    Other
}
```

### Interfaces

```csharp
public interface IMachine
{
    ICpu Cpu { get; }
    IMemoryBus Bus { get; }
    ISignalBus Signals { get; }
    IScheduler Scheduler { get; }
    IDeviceRegistry Registry { get; }
    IVideoController Video { get; }
    MachineState State { get; }
    void Reset();
    void Run();
    CpuStepResult Step();
    void RequestStop();
}

public interface IPocket2Machine : IMachine
{
    ISlotManager Slots { get; }
    IKeyboard Keyboard { get; }
    ISpeaker Speaker { get; }
}

public interface ITrapRegistry
{
    void Register(ushort address, string name, TrapCategory category, 
                  TrapHandler handler, string description);
    void Enable(TrapCategory category);
    void Disable(TrapCategory category);
    bool TryGetTrap(ushort address, out TrapHandler handler);
}

public interface IDebugService
{
    Task<IDebugSession> AttachAsync(IMachine machine);
    Task DetachAsync(IDebugSession session);
    IReadOnlyList<IDebugSession> Sessions { get; }
}

public interface IDebugSession
{
    IMachine Machine { get; }
    ICommandDispatcher CommandDispatcher { get; }
    IDebugContext Context { get; }
    Task<CommandResult> ExecuteCommandAsync(string command);
    CpuState GetCpuState();
    ReadOnlyMemory<byte> ReadMemory(Addr address, int length);
    void SetBreakpoint(Addr address);
    void ClearBreakpoint(Addr address);
    event EventHandler<DebugStopEventArgs>? Stopped;
}
```

### Structs and Delegates

```csharp
public delegate TrapHandler TrapHandler(ICpu cpu, IMemoryBus bus, IEventContext context);

public readonly record struct TrapResult(
    bool Handled,
    Cycle CyclesConsumed,
    Addr? ReturnAddress
);

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

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct BootHandoff
{
    public const uint Magic = 0x4F484D42;  // "BMHO"
    public uint magic;
    public ushort versionMajor;
    public ushort versionMinor;
    public uint totalSize;
    public uint flags;
    public uint bootRomPhysBase;
    public uint bootRomSize;
    public uint ramPhysBase;
    public uint ramSize;
    public uint compatIdDefault;
    public uint cmdlineOffset;
    public uint cmdlineLength;
    public uint memMapOffset;
    public uint memMapCount;
    public uint romInvOffset;
    public uint romInvCount;
}
```

---

## 12. UI Services

### Enums

```csharp
public enum PopOutComponent
{
    VideoDisplay,
    DebugConsole,
    AssemblyEditor,
    HexEditor
}
```

### Interfaces

```csharp
public interface IMachineService
{
    IReadOnlyList<IMachine> Instances { get; }
    IReadOnlyList<MachineProfile> Profiles { get; }
    Task<IMachine> CreateInstanceAsync(MachineProfile profile);
    Task StartAsync(IMachine machine);
    Task StopAsync(IMachine machine);
    Task ResetAsync(IMachine machine, bool cold);
    Task PauseAsync(IMachine machine);
    Task ResumeAsync(IMachine machine);
    Task DestroyAsync(IMachine machine);
    Task SaveProfileAsync(MachineProfile profile);
    Task DeleteProfileAsync(string profileId);
    event EventHandler<MachineStateChangedEventArgs>? StateChanged;
}

public interface IStorageService
{
    string LibraryPath { get; }
    IReadOnlyList<DiskImageInfo> DiskImages { get; }
    IReadOnlyList<RomImageInfo> RomImages { get; }
    Task<DiskImageInfo> CreateDiskAsync(DiskFormat format, string name);
    Task<DiskImageInfo> ImportDiskAsync(string path);
    Task ExportDiskAsync(DiskImageInfo disk, string path, DiskFormat format);
    Task<IDiskEditor> OpenDiskEditorAsync(DiskImageInfo disk);
    Task<RomImageInfo> ImportRomAsync(string path, RomType type);
    Task<RomVerificationResult> VerifyRomAsync(RomImageInfo rom);
    Task RefreshLibraryAsync();
}

public interface IWindowManager
{
    IReadOnlyList<IPopOutWindow> PopOutWindows { get; }
    Task<IPopOutWindow> CreatePopOutAsync(PopOutComponent component, IMachine? machine = null);
    Task DockWindowAsync(IPopOutWindow window);
    Task RestoreWindowStatesAsync(string profileId);
    Task SaveWindowStatesAsync(string profileId);
    event EventHandler<PopOutWindowEventArgs>? WindowCreated;
    event EventHandler<PopOutWindowEventArgs>? WindowClosed;
}

public interface IPopOutWindow
{
    string WindowId { get; }
    PopOutComponent ComponentType { get; }
    IMachine? Machine { get; set; }
    WindowState State { get; }
    void BringToFront();
    Task CloseAsync(bool dockContent = false);
}

public interface IEventAggregator
{
    void Publish<TEvent>(TEvent eventData) where TEvent : class;
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}

public interface IEditorService
{
    ISourceDocument CreateDocument(string name, SourceLanguage language);
    Task<ISourceDocument> OpenDocumentAsync(string path);
    Task<AssemblyResult> AssembleAsync(ISourceDocument document, AssemblyOptions options);
    Task LoadIntoMachineAsync(AssemblyResult result, IMachine machine, Addr loadAddress);
    IReadOnlyList<AssemblerDialect> Dialects { get; }
}

public interface IShutdownCoordinator
{
    Task<bool> RequestShutdownAsync();
    Task<IReadOnlyList<UnsavedWorkItem>> GetUnsavedWorkAsync();
    void ForceShutdown();
}

public interface IHypervisorService
{
    IMachine HostMachine { get; }
    IReadOnlyList<IMachine> GuestMachines { get; }
    Task<IMachine> CreateGuestAsync(MachinePersonality personality);
    Task DestroyGuestAsync(IMachine guest);
    GuestResources GetResources(IMachine guest);
}
```

### Records

```csharp
public record MachineProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required MachinePersonality Personality { get; init; }
    public required CpuProfile Cpu { get; init; }
    public required MemoryProfile Memory { get; init; }
    public IReadOnlyList<PeripheralProfile> Peripherals { get; init; } = [];
    public IReadOnlyList<RomBinding> RomBindings { get; init; } = [];
    public IReadOnlyList<DiskBinding> DiskBindings { get; init; } = [];
}

public record WindowStateInfo
{
    public required PopOutComponent ComponentType { get; init; }
    public bool IsPopOut { get; init; }
    public Point? Position { get; init; }
    public Size? Size { get; init; }
    public string? MonitorId { get; init; }
    public bool IsMaximized { get; init; }
    public string? MachineProfileId { get; init; }
}

public record WindowLayoutState
{
    public int Version { get; init; } = 1;
    public IReadOnlyList<WindowStateInfo> Windows { get; init; } = [];
    public WindowStateInfo? MainWindow { get; init; }
}

public record MachineStateChangedEvent(IMachine Machine, MachineState NewState);
public record BreakpointHitEvent(IMachine Machine, Addr Address);
public record DisplayModeChangedEvent(IMachine Machine, DisplayMode NewMode);
public record WindowFocusRequestEvent(PopOutComponent ComponentType, string? MachineId);
```

---

## 13. Settings and Configuration

### Enums

```csharp
public enum DiskFormat
{
    Dos33_140K,
    Dos33_800K,
    ProDos_140K,
    ProDos_800K,
    ProDos_32MB,
    Nib,
    Woz
}

public enum SourceLanguage
{
    Assembly6502,
    Assembly65C02,
    Assembly65816,
    Assembly65832,
    BasicApplesoft
}

public enum PathPurpose
{
    LibraryRoot,
    DiskImages,
    RomImages,
    LogFiles,
    SaveStates
}

public enum PathValidationWarning
{
    DirectoryDoesNotExist,
    InsufficientPermissions,
    LowDiskSpace,
    NetworkPath,
    RelativePath
}
```

### Interfaces

```csharp
public interface ISettingsService
{
    AppSettings Current { get; }
    Task<AppSettings> LoadAsync();
    Task SaveAsync(AppSettings settings);
    Task<AppSettings> ResetToDefaultsAsync();
    Task ExportAsync(string path);
    Task<AppSettings> ImportAsync(string path);
    T GetValue<T>(string key);
    void SetValue<T>(string key, T value);
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}

public interface ISettingsPage
{
    string DisplayName { get; }
    string IconKey { get; }
    string? ParentCategory { get; }
    int SortOrder { get; }
    Task LoadAsync();
    Task SaveAsync();
    Task ResetToDefaultsAsync();
    bool HasChanges { get; }
}

public interface ISettingsPageRegistry
{
    void Register<TPage>() where TPage : class, ISettingsPage;
    IReadOnlyList<ISettingsPage> GetPages();
    IReadOnlyList<ISettingsPage> GetPagesForCategory(string category);
}

public interface IPathValidator
{
    PathValidationResult Validate(string path, PathPurpose purpose);
    string Normalize(string path);
    Task<bool> EnsureDirectoryExistsAsync(string path);
}

public interface ISettingsMigrator
{
    int CurrentVersion { get; }
    AppSettings Migrate(JsonElement oldSettings, int fromVersion);
    bool NeedsMigration(int settingsVersion);
}

public interface ISettingsMigrationStep
{
    int FromVersion { get; }
    int ToVersion { get; }
    JsonElement Apply(JsonElement settings);
}
```

### Records

```csharp
public record AppSettings
{
    public int Version { get; init; } = 1;
    public GeneralSettings General { get; init; } = new();
    public LibrarySettings Library { get; init; } = new();
    public DisplaySettings Display { get; init; } = new();
    public InputSettings Input { get; init; } = new();
    public DebugSettings Debug { get; init; } = new();
    public EditorSettings Editor { get; init; } = new();
    public WindowLayoutState WindowLayout { get; init; } = new();
    public IReadOnlyList<MachineProfile> Profiles { get; init; } = [];
    public string? LastProfileId { get; init; }
}

public record GeneralSettings
{
    public bool LoadLastProfile { get; init; } = true;
    public bool StartPaused { get; init; } = false;
    public bool RestoreWindowLayout { get; init; } = true;
    public string Language { get; init; } = "en-US";
    public string Theme { get; init; } = "Dark";
    public bool CheckForUpdates { get; init; } = true;
    public bool EnableTelemetry { get; init; } = false;
}

public record LibrarySettings
{
    public string LibraryRoot { get; init; } = "~/.backpocket";
    public string DiskImagesPath { get; init; } = "{Library}/disks";
    public string RomImagesPath { get; init; } = "{Library}/roms";
    public string LogFilesPath { get; init; } = "{Library}/logs";
    public string SaveStatesPath { get; init; } = "{Library}/saves";
    public bool AutoScanOnStartup { get; init; } = true;
    public bool WatchForChanges { get; init; } = true;
}

public record PathValidationResult
{
    public bool IsValid { get; init; }
    public string? NormalizedPath { get; init; }
    public string? ErrorMessage { get; init; }
    public PathValidationWarning[] Warnings { get; init; } = [];
}

public record SettingsChangedEventArgs
{
    public IReadOnlyList<string> ChangedKeys { get; init; } = [];
    public bool IsFullReload { get; init; }
}

public record HeadlessOptions
{
    public string? SettingsPath { get; init; }
    public IReadOnlyDictionary<string, string> Overrides { get; init; }
        = new Dictionary<string, string>();
    public bool NoGui { get; init; }
    public string? ProfileId { get; init; }
}
```

---

## Document History

| Version | Date       | Changes                               |
|---------|------------|---------------------------------------|
| 1.0     | 2025-12-28 | Initial consolidation from all specs  |
