# Emulator Architecture Specification v1.0 (Part 2)

## Part V: Signal Bus (Interrupts & Control Lines)

### 5.1 Signal Model

```csharp
public enum SignalLine
{
    IRQ,        // Maskable interrupt (directly from IFlag/I flag)
    NMI,        // Non-maskable (edge-sensitive)
    Reset,      // System reset
    RDY,        // Ready (clock stretching)
    DmaReq,     // DMA request
    Sync        // Instruction fetch indicator (output)
}

/// <summary>
/// Signal hub manages device-to-CPU lines.
/// Devices assert/deassert; CPU samples.
/// </summary>
public interface ISignalBus
{
    void Assert(SignalLine line, int deviceId);
    void Deassert(SignalLine line, int deviceId);
    bool IsAsserted(SignalLine line);
    
    // Edge detection for NMI
    bool ConsumeNmiEdge();
    
    // Observability
    event Action<SignalLine, bool, int, ulong>? SignalChanged;
}

/// <summary>
/// Implementation tracks multiple asserters per line.
/// </summary>
public sealed class SignalBus : ISignalBus
{
    private readonly HashSet<int>[] _asserters;
    private bool _nmiEdgePending;
    private bool _nmiPreviousLevel;
    
    public SignalBus()
    {
        _asserters = new HashSet<int>[Enum.GetValues<SignalLine>().Length];
        for (int i = 0; i < _asserters.Length; i++)
            _asserters[i] = new HashSet<int>();
    }
    
    public void Assert(SignalLine line, int deviceId)
    {
        bool wasAsserted = IsAsserted(line);
        _asserters[(int)line].Add(deviceId);
        bool nowAsserted = IsAsserted(line);
        
        // NMI edge detection (low-to-high transition)
        if (line == SignalLine.NMI && !wasAsserted && nowAsserted)
            _nmiEdgePending = true;
        
        if (wasAsserted != nowAsserted)
            SignalChanged?.Invoke(line, nowAsserted, deviceId, /* cycle */0);
    }
    
    public void Deassert(SignalLine line, int deviceId)
    {
        bool wasAsserted = IsAsserted(line);
        _asserters[(int)line].Remove(deviceId);
        bool nowAsserted = IsAsserted(line);
        
        if (wasAsserted != nowAsserted)
            SignalChanged?.Invoke(line, nowAsserted, deviceId, 0);
    }
    
    public bool IsAsserted(SignalLine line)
        => _asserters[(int)line].Count > 0;
    
    public bool ConsumeNmiEdge()
    {
        bool pending = _nmiEdgePending;
        _nmiEdgePending = false;
        return pending;
    }
    
    public event Action<SignalLine, bool, int, ulong>? SignalChanged;
}
```

---

## Part VI: Scheduler & Timing

From `bus-scheduler-spec.md`:

### 6.1 Core Concepts

```csharp
/// <summary>
/// Cycle is the single authoritative unit of simulated time.
/// </summary>
public readonly record struct Cycle(ulong Value)
{
    public static Cycle Zero => new(0);
    public static Cycle operator +(Cycle a, Cycle b) => new(a.Value + b.Value);
    public static Cycle operator -(Cycle a, Cycle b) => new(a.Value - b.Value);
    public static bool operator <(Cycle a, Cycle b) => a.Value < b.Value;
    public static bool operator >(Cycle a, Cycle b) => a.Value > b.Value;
    public static bool operator <=(Cycle a, Cycle b) => a.Value <= b.Value;
    public static bool operator >=(Cycle a, Cycle b) => a.Value >= b.Value;
}

/// <summary>
/// Event classification for profiling and diagnostics.
/// </summary>
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

### 6.2 Scheduler Interface

```csharp
public interface IScheduler
{
    /// <summary>Gets the current cycle count.</summary>
    Cycle Now { get; }
    
    /// <summary>Schedule an event at an absolute cycle.</summary>
    EventHandle ScheduleAt(Cycle due, ScheduledEventKind kind, int priority, 
                           Action<IEventContext> callback, object? tag = null);
    
    /// <summary>Schedule an event relative to now.</summary>
    EventHandle ScheduleAfter(Cycle delta, ScheduledEventKind kind, int priority,
                              Action<IEventContext> callback, object? tag = null);
    
    /// <summary>Cancel a pending event.</summary>
    bool Cancel(EventHandle handle);
    
    /// <summary>Advance time, dispatching due events.</summary>
    void Advance(Cycle delta);
    
    /// <summary>Dispatch all events due at current cycle.</summary>
    void DispatchDue();
    
    /// <summary>Get next event time (for WAI fast-forward).</summary>
    Cycle? PeekNextDue();
    
    /// <summary>Jump to next event and dispatch (WAI support).</summary>
    /// <returns>True if an event was dispatched; false if no events pending.</returns>
    bool JumpToNextEventAndDispatch();
    
    /// <summary>
    /// Resets the scheduler to cycle 0 and cancels all pending events.
    /// Called during machine reset.
    /// </summary>
    void Reset();
    
    /// <summary>Gets the number of pending events (for diagnostics).</summary>
    int PendingEventCount { get; }
}

public readonly record struct EventHandle(ulong Id);

public interface IEventContext
{
    Cycle Now { get; }
    IScheduler Scheduler { get; }
    ISignalBus Signals { get; }
    IMemoryBus Bus { get; }
}
```

### 6.3 CPU-Scheduler Integration

```csharp
public enum CpuRunState
{
    Running,
    WaitingForInterrupt,  // WAI instruction
    Stopped,              // STP instruction or stop requested
    Halted                // Fatal error or unrecoverable state
}

public readonly record struct CpuStepResult(
    CpuRunState State,
    Cycle CyclesConsumed
);

/// <summary>
/// Extended CPU interface with stop/halt coordination.
/// </summary>
public interface ICpu
{
    CpuFamily Family { get; }
    CpuMode CurrentMode { get; }
    bool Halted { get; }
    ulong CycleCount { get; }
    
    /// <summary>Executes a single instruction.</summary>
    CpuStepResult Step();
    
    /// <summary>
    /// Resets the CPU to initial state.
    /// Reads the reset vector and sets PC accordingly.
    /// </summary>
    void Reset();
    
    void SignalIRQ();
    void SignalNMI();
    
    // ─── Stop/Halt Coordination ─────────────────────────────────────────
    
    /// <summary>
    /// Requests the CPU to stop execution at the next safe point.
    /// The CPU will complete the current instruction before stopping.
    /// </summary>
    void RequestStop();
    
    /// <summary>
    /// Clears a previous stop request, allowing execution to continue.
    /// </summary>
    void ClearStopRequest();
    
    /// <summary>
    /// Gets whether a stop has been requested.
    /// </summary>
    bool IsStopRequested { get; }
}
```

---

## Part VII: Device & Peripheral Architecture

### 7.1 Peripheral Interface

```csharp
/// <summary>
/// A peripheral device that can be installed in a slot.
/// </summary>
public interface IPeripheral : IScheduledDevice
{
    string Name { get; }
    string DeviceType { get; }  // "DiskII", "MockingBoard", etc.
    
    /// <summary>MMIO region (slot I/O space $C0n0-$C0nF).</summary>
    IBusTarget? MMIORegion { get; }
    
    /// <summary>Firmware ROM region ($Cn00-$CnFF).</summary>
    IBusTarget? ROMRegion { get; }
    
    /// <summary>Expansion ROM region ($C800-$CFFF when selected).</summary>
    IBusTarget? ExpansionROMRegion { get; }
    
    void Reset();
}

/// <summary>
/// Device that participates in the scheduler.
/// </summary>
public interface IScheduledDevice
{
    /// <summary>
    /// Initializes the device with access to system services.
    /// Called after all devices are created but before the machine runs.
    /// </summary>
    /// <param name="context">Event context providing access to scheduler, signals, and bus.</param>
    void Initialize(IEventContext context);
}
```

### 7.2 Slot Manager (Apple II Peripheral Bus)

```csharp
/// <summary>
/// Manages the 7 expansion slots of an Apple II.
/// </summary>
public interface ISlotManager
{
    /// <summary>Installed cards by slot (1-7).</summary>
    IReadOnlyDictionary<int, IPeripheral> Slots { get; }
    
    /// <summary>Currently selected slot for $C800-$CFFF.</summary>
    int? ActiveExpansionSlot { get; }
    
    void Install(int slot, IPeripheral card);
    void Remove(int slot);
    IPeripheral? GetCard(int slot);
    
    /// <summary>Select a slot for expansion ROM access.</summary>
    void SelectExpansionSlot(int slot);
    
    /// <summary>Deselects expansion ROM (returns to floating bus).</summary>
    void DeselectExpansionSlot();
    
    void Reset();
}
```

### 7.3 Apple II Memory Map

For Pocket2e, the bus must implement these regions:

```
$0000-$01FF : Zero Page + Stack
$0200-$03FF : Input buffer, misc
$0400-$07FF : Text Page 1 / Lo-res Page 1
$0800-$0BFF : Text Page 2 / Lo-res Page 2
$0C00-$1FFF : Free RAM
$2000-$3FFF : Hi-res Page 1
$4000-$5FFF : Hi-res Page 2
$6000-$BFFF : Free RAM (Applesoft, programs)
$C000-$C0FF : Soft switches (I/O page)
$C100-$C7FF : Peripheral card ROM ($Cn00 for slot n)
$C800-$CFFF : Expansion ROM (selected slot)
$D000-$FFFF : ROM / Language Card RAM
```

### 7.4 Soft Switch Handler (Composite Device Page)

The $C000-$C0FF page is a "composite page" that dispatches internally:

```csharp
/// <summary>
/// Handles the Apple II soft switch / I/O page.
/// </summary>
public sealed class AppleIISoftSwitchPage : IBusTarget
{
    private readonly IVideoController _video;
    private readonly IKeyboard _keyboard;
    private readonly ISpeaker _speaker;
    private readonly IAnnunciators _annunciators;
    private readonly ISlotManager _slots;
    
    public byte Read8(uint physicalAddress, in BusAccess context)
    {
        byte offset = (byte)(physicalAddress & 0xFF);
        
        return offset switch
        {
            // Keyboard
            0x00 => _keyboard.ReadKeyData(),
            0x10 => _keyboard.ReadKeyStrobe(),
            
            // Video switches
            0x50 => _video.SetGraphics(),
            0x51 => _video.SetText(),
            0x52 => _video.SetFullScreen(),
            0x53 => _video.SetMixed(),
            0x54 => _video.SetPage1(),
            0x55 => _video.SetPage2(),
            0x56 => _video.SetLoRes(),
            0x57 => _video.SetHiRes(),
            
            // Speaker
            0x30 => _speaker.Toggle(),
            
            // Annunciators
            >= 0x58 and <= 0x5F => _annunciators.Access(offset),
            
            // Slot I/O ($C080-$C0FF, 16 bytes per slot)
            >= 0x80 => ReadSlotIO(offset),
            
            _ => FloatingBus()
        };
    }
    
    private byte ReadSlotIO(byte offset)
    {
        int slot = (offset - 0x80) >> 4;  // 0-7
        int subOffset = offset & 0x0F;
        
        var card = _slots.GetCard(slot);
        if (card?.MMIORegion is { } mmio)
            return mmio.Read8((uint)subOffset, default);
        
        return FloatingBus();
    }
    
    private byte FloatingBus() => 0xFF;  // Or last data bus value
    
    // Write8 similar pattern...
}
```

### 7.5 Machine Interface

The machine interface provides a unified abstraction for the assembled emulator system.

```csharp
/// <summary>
/// Base interface for an assembled emulator machine.
/// Provides access to all components and lifecycle methods.
/// </summary>
public interface IMachine
{
    // ─── Core Components ────────────────────────────────────────────────
    
    /// <summary>Gets the CPU instance.</summary>
    ICpu Cpu { get; }
    
    /// <summary>Gets the main memory bus.</summary>
    IMemoryBus Bus { get; }
    
    /// <summary>Gets the signal bus for interrupts and control lines.</summary>
    ISignalBus Signals { get; }
    
    /// <summary>Gets the cycle-accurate scheduler.</summary>
    IScheduler Scheduler { get; }
    
    /// <summary>Gets the device registry for metadata lookups.</summary>
    IDeviceRegistry Registry { get; }
    
    // ─── Machine State ──────────────────────────────────────────────────
    
    /// <summary>Gets the current machine state.</summary>
    MachineState State { get; }
    
    /// <summary>Gets the total cycles executed since last reset.</summary>
    ulong TotalCycles { get; }
    
    // ─── Lifecycle Methods ──────────────────────────────────────────────
    
    /// <summary>
    /// Performs a hard reset: initializes all devices and loads reset vector.
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Runs the machine until stopped, halted, or breakpoint hit.
    /// </summary>
    void Run();
    
    /// <summary>
    /// Executes a single CPU instruction and advances the scheduler.
    /// </summary>
    /// <returns>Result of the step including cycles consumed and state.</returns>
    CpuStepResult Step();
    
    /// <summary>
    /// Requests the machine to stop at the next safe point.
    /// </summary>
    void RequestStop();
    
    /// <summary>
    /// Clears a previous stop request, allowing Run() to continue.
    /// </summary>
    void ClearStopRequest();
    
    // ─── Events ─────────────────────────────────────────────────────────
    
    /// <summary>Raised when the machine state changes.</summary>
    event Action<MachineState>? StateChanged;
    
    /// <summary>Raised when a breakpoint is hit.</summary>
    event Action<Addr>? BreakpointHit;
}

/// <summary>
/// Machine execution state.
/// </summary>
public enum MachineState
{
    /// <summary>Machine is not running, ready for commands.</summary>
    Stopped,
    
    /// <summary>Machine is actively executing instructions.</summary>
    Running,
    
    /// <summary>Machine is paused at a breakpoint.</summary>
    AtBreakpoint,
    
    /// <summary>CPU is waiting for interrupt (WAI instruction).</summary>
    WaitingForInterrupt,
    
    /// <summary>Machine has halted (STP instruction or fatal error).</summary>
    Halted,
    
    /// <summary>Machine is being reset.</summary>
    Resetting
}

/// <summary>
/// Extended machine interface for Pocket2e/Pocket2c systems with slots.
/// </summary>
public interface IPocket2Machine : IMachine
{
    /// <summary>Gets the slot manager for peripheral cards.</summary>
    ISlotManager Slots { get; }
    
    /// <summary>Gets the video controller.</summary>
    IVideoController Video { get; }
    
    /// <summary>Gets the keyboard controller.</summary>
    IKeyboard Keyboard { get; }
    
    /// <summary>Gets the speaker.</summary>
    ISpeaker Speaker { get; }
}
```

### 7.6 CPU Construction Pattern

CPUs require access to the bus and signal bus at construction time.

```csharp
/// <summary>
/// Factory interface for CPU creation.
/// </summary>
public interface ICpuFactory
{
    /// <summary>Gets the CPU family this factory creates.</summary>
    CpuFamily Family { get; }
    
    /// <summary>Creates a CPU instance connected to the given infrastructure.</summary>
    /// <param name="bus">The memory bus for all memory operations.</param>
    /// <param name="signals">The signal bus for interrupts.</param>
    /// <returns>A new CPU instance.</returns>
    ICpu Create(IMemoryBus bus, ISignalBus signals);
}

/// <summary>
/// Example: 65C02 CPU constructor pattern.
/// </summary>
public sealed class Cpu65C02 : ICpu
{
    private readonly IMemoryBus _bus;
    private readonly ISignalBus _signals;
    private Registers _registers;
    private bool _stopRequested;
    
    /// <summary>
    /// Creates a 65C02 CPU connected to the given bus and signal infrastructure.
    /// </summary>
    /// <param name="bus">Memory bus for all memory operations.</param>
    /// <param name="signals">Signal bus for interrupt handling.</param>
    public Cpu65C02(IMemoryBus bus, ISignalBus signals)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _signals = signals ?? throw new ArgumentNullException(nameof(signals));
        _registers = new Registers(compat: true);
    }
    
    // ICpu implementation...
}
```

### 7.7 Device Initialization Order

Devices must be initialized in a specific order to ensure all dependencies are available.

**Initialization Sequence:**
1. **Create infrastructure** (registry, scheduler, signals)
   - These are independent and can be created in any order
   - No external dependencies

2. **Create memory bus**
   - Depends on: nothing
   - Creates empty page table

3. **Create devices** (RAM, ROM, I/O controllers)
   - Depends on: nothing (devices don't connect to bus yet)
   - Devices are created but not initialized

4. **Wire devices to bus** (map pages)
   - Depends on: bus, devices
   - Establishes address mappings

5. **Create CPU**
   - Depends on: bus, signals
   - CPU holds references but doesn't execute

6. **Create event context**
   - Depends on: scheduler, signals, bus
   - Bundles references for device initialization

7. **Initialize devices with event context**
   - Depends on: event context, all wiring complete
   - Devices can schedule initial events, set up callbacks

8. **Assemble machine**
   - Depends on: all components created and initialized
   - Creates the IMachine wrapper

```csharp
/// <summary>
/// Standard event context implementation.
/// </summary>
public sealed class EventContext : IEventContext
{
    public EventContext(IScheduler scheduler, ISignalBus signals, IMemoryBus bus)
    {
        Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        Signals = signals ?? throw new ArgumentNullException(nameof(signals));
        Bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }
    
    public Cycle Now => Scheduler.Now;
    public IScheduler Scheduler { get; }
    public ISignalBus Signals { get; }
    public IMemoryBus Bus { get; }
}
```

---

## Part VIII: Compatibility Personalities (65832)

From the privileged spec: the 65832 can run Apple II code in sandboxed contexts.

### 8.1 Compatibility Context

```csharp
/// <summary>
/// A compatibility personality for running legacy code.
/// </summary>
public enum CompatibilityPersonality : uint
{
    None = 0,           // No legacy ROM mapping
    AppleIIe = 1,       // Apple IIe Enhanced
    AppleIIc = 2,       // Apple IIc
    AppleIIgs = 3       // Apple IIgs (M1 mode)
}

/// <summary>
/// COMPATID system register controls which personality is active.
/// </summary>
public sealed class CompatibilityController
{
    public CompatibilityPersonality CurrentPersonality { get; private set; }
    
    /// <summary>
    /// Maps a 64KB compat window for a legacy guest.
    /// The guest sees its traditional memory layout;
    /// the MMU translates to host physical addresses.
    /// </summary>
    public void SetupCompatWindow(uint compatBase, CompatibilityPersonality personality)
    {
        CurrentPersonality = personality;
        // Map RAM region:  compatBase + $0000 - $BFFF
        // Map I/O page:    compatBase + $C000 - $CFFF (device page)
        // Map ROM region:  compatBase + $D000 - $FFFF
    }
}
```

### 8.2 Device Page Object Model

```csharp
/// <summary>
/// A device page object for the compat I/O region.
/// Each guest gets its own instance.
/// </summary>
public interface IDevicePageObject
{
    int DevicePageId { get; }  // Encoded as Class|Instance|Page
    DevicePageClass Class { get; }
    
    byte Read8(uint offset, in BusAccess context);
    void Write8(uint offset, byte value, in BusAccess context);
    
    /// <summary>
    /// Called when access fails (invalid offset, unsupported width).
    /// Raises DeviceFault.
    /// </summary>
    void RejectAccess(in BusAccess context);
}
```

### 8.3 ROM Routine Interception (Trap Handlers)

The emulator needs to intercept calls to well-known ROM entry points and provide native
implementations. This serves multiple purposes:

1. **Legal compliance**: Avoid distributing or requiring copyrighted ROM images
2. **Performance**: Native implementations can be faster than cycle-accurate emulation
3. **Enhanced functionality**: Add features not in original ROMs (e.g., better error messages)
4. **Debugging**: Trap handlers can provide instrumentation hooks

#### 8.3.1 Design Philosophy

ROM interception uses a **trap mechanism** triggered on instruction fetch at specific addresses.
When the CPU fetches an opcode from a trapped address:

1. The trap handler executes instead of the ROM code
2. The handler performs the equivalent operation natively
3. The handler returns control as if the ROM routine had executed (RTS/RTI semantics)
4. Cycle counts are adjusted to approximate original timing (optional strict mode)

This is transparent to the guest code - it sees the expected behavior without knowing
the implementation is native.

#### 8.3.2 Trap Registry Interface

```csharp
/// <summary>
/// Result of a trap handler execution.
/// </summary>
public readonly record struct TrapResult(
    bool Handled,           // True if trap was handled; false to fall through to ROM
    Cycle CyclesConsumed,   // Cycles to charge for this operation
    Addr? ReturnAddress     // Override return address (null = use stack RTS)
);

/// <summary>
/// Delegate for trap handler implementations.
/// </summary>
/// <param name="cpu">CPU state for register access.</param>
/// <param name="bus">Memory bus for RAM access.</param>
/// <param name="context">Event context for scheduling/signals.</param>
/// <returns>Result indicating whether trap was handled.</returns>
public delegate TrapResult TrapHandler(ICpu cpu, IMemoryBus bus, IEventContext context);

/// <summary>
/// Classification of trap types for diagnostics and filtering.
/// </summary>
public enum TrapCategory
{
    Firmware,       // Core firmware routines (reset, IRQ handlers)
    Monitor,        // Monitor/debugger routines
    BasicInterp,    // BASIC interpreter routines
    BasicRuntime,   // BASIC runtime (math, strings, I/O)
    Dos,            // DOS/ProDOS entry points
    PrinterDriver,  // Printer output routines
    DiskDriver,     // Disk I/O routines
    Custom          // User-defined traps
}

/// <summary>
/// Metadata for a registered trap.
/// </summary>
public readonly record struct TrapInfo(
    Addr Address,
    string Name,
    TrapCategory Category,
    string? Description,
    bool Enabled
);

/// <summary>
/// Registry for ROM routine interception handlers.
/// </summary>
public interface ITrapRegistry
{
    /// <summary>
    /// Registers a trap handler at a specific address.
    /// </summary>
    /// <param name="address">The ROM address to intercept.</param>
    /// <param name="name">Human-readable name for the trap.</param>
    /// <param name="category">Classification of the trap.</param>
    /// <param name="handler">The native implementation.</param>
    /// <param name="description">Optional description for tooling.</param>
    void Register(Addr address, string name, TrapCategory category, 
                  TrapHandler handler, string? description = null);
    
    /// <summary>
    /// Unregisters a trap at the specified address.
    /// </summary>
    bool Unregister(Addr address);
    
    /// <summary>
    /// Checks if an address has a trap and executes it if so.
    /// Called by the CPU on instruction fetch.
    /// </summary>
    /// <param name="address">The fetch address.</param>
    /// <param name="cpu">CPU for register access.</param>
    /// <param name="bus">Memory bus.</param>
    /// <param name="context">Event context.</param>
    /// <returns>Trap result, or default with Handled=false if no trap.</returns>
    TrapResult TryExecute(Addr address, ICpu cpu, IMemoryBus bus, IEventContext context);
    
    /// <summary>
    /// Enables or disables a trap without removing it.
    /// </summary>
    void SetEnabled(Addr address, bool enabled);
    
    /// <summary>
    /// Enables or disables all traps in a category.
    /// </summary>
    void SetCategoryEnabled(TrapCategory category, bool enabled);
    
    /// <summary>
    /// Gets information about all registered traps.
    /// </summary>
    IEnumerable<TrapInfo> GetAll();
    
    /// <summary>
    /// Gets information about a specific trap.
    /// </summary>
    TrapInfo? GetInfo(Addr address);
    
    /// <summary>
    /// Checks if any trap is registered at the address (for fast-path skip).
    /// </summary>
    bool HasTrap(Addr address);
}
```

#### 8.3.3 CPU Integration

The CPU checks for traps during instruction fetch:

```csharp
public CpuStepResult Step()
{
    Addr pc = _registers.PC.GetAddr();
    
    // Check for trap before fetching opcode
    if (_trapRegistry.HasTrap(pc))
    {
        var result = _trapRegistry.TryExecute(pc, this, _bus, _eventContext);
        if (result.Handled)
        {
            // Trap handled the call - advance cycles and continue
            _cycleCount += result.CyclesConsumed.Value;
            
            // If trap specified return address, use it; otherwise simulate RTS
            if (result.ReturnAddress is { } retAddr)
                _registers.PC.SetAddr(retAddr);
            else
                SimulateRts();  // Pop return address from stack
            
            return new CpuStepResult(CpuRunState.Running, result.CyclesConsumed);
        }
        // Fall through to normal execution if not handled
    }
    
    // Normal instruction fetch and execution
    byte opcode = FetchOpcode();
    // ... execute instruction ...
}
```

#### 8.3.4 Well-Known Trap Points (Pocket2e)

Common ROM entry points that benefit from native implementation:

| Address | Name         | Category      | Description                           |
|---------|--------------|---------------|---------------------------------------|
| $FB1E   | PREAD        | Firmware      | Read paddle/joystick position         |
| $FB39   | INIT         | Firmware      | Initialize text screen                |
| $FB5B   | PCADJ        | Firmware      | Print character adjust                |
| $FBC1   | BASCALC      | Firmware      | Calculate text base address           |
| $FC58   | HOME         | Monitor       | Clear screen and home cursor          |
| $FC9C   | CLREOL       | Monitor       | Clear to end of line                  |
| $FCA8   | WAIT         | Monitor       | Delay loop                            |
| $FDED   | COUT         | Monitor       | Output character to screen            |
| $FD0C   | RDKEY        | Monitor       | Read key from keyboard                |
| $FD6A   | GETLN        | Monitor       | Get line of input                     |
| $FE89   | SETKBD       | Monitor       | Set keyboard as input device          |
| $FE93   | SETVID       | Monitor       | Set video as output device            |
| $D683   | CHKCOM       | BasicInterp   | Check for comma in BASIC              |
| $D6A5   | FRMNUM       | BasicInterp   | Evaluate numeric expression           |
| $DFE3   | PRINT        | BasicRuntime  | PRINT statement handler               |
| $E2F2   | LINPRT       | BasicRuntime  | Print integer value                   |
| $E752   | FOUT         | BasicRuntime  | Float to string conversion            |
| $EAF9   | FIN          | BasicRuntime  | String to float conversion            |

#### 8.3.5 Trap Handler Example

```csharp
/// <summary>
/// Native implementation of HOME ($FC58) - clear screen and home cursor.
/// </summary>
public static TrapResult HomeHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
{
    // Get video controller from machine context
    var video = context.GetService<IVideoController>();
    
    // Clear the screen
    video.ClearScreen();
    
    // Home the cursor (set CV=0, CH=0)
    // CV is at $25, CH is at $24
    var access = new BusAccess(
        Address: 0x24,
        Value: 0,
        WidthBits: 8,
        Mode: CpuMode.Compat,
        EmulationE: true,
        Intent: AccessIntent.DataWrite,
        SourceId: 0,
        Cycle: context.Now.Value,
        Flags: AccessFlags.None);
    
    bus.Write8(access, 0);  // CH = 0
    bus.Write8(access with { Address = 0x25 }, 0);  // CV = 0
    
    // Return with appropriate cycle count (HOME takes ~2ms on real hardware)
    return new TrapResult(
        Handled: true,
        CyclesConsumed: new Cycle(2048),  // Approximate
        ReturnAddress: null  // Use normal RTS
    );
}

/// <summary>
/// Native implementation of COUT ($FDED) - output character.
/// </summary>
public static TrapResult CoutHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
{
    // Character is in A register
    byte ch = cpu.GetRegisterA();
    
    var video = context.GetService<IVideoController>();
    video.OutputCharacter(ch);
    
    // COUT is fast - just a few cycles
    return new TrapResult(Handled: true, CyclesConsumed: new Cycle(12), ReturnAddress: null);
}
```

#### 8.3.6 Trap Registration During Machine Build

```csharp
public IPocket2Machine Build()
{
    // ... create components ...
    
    // Register trap handlers
    var traps = new TrapRegistry();
    
    // Firmware traps
    traps.Register(0xFB1E, "PREAD", TrapCategory.Firmware, PreadHandler, 
                   "Read paddle position");
    traps.Register(0xFBC1, "BASCALC", TrapCategory.Firmware, BascalcHandler,
                   "Calculate text base address");
    
    // Monitor traps
    traps.Register(0xFC58, "HOME", TrapCategory.Monitor, HomeHandler,
                   "Clear screen and home cursor");
    traps.Register(0xFDED, "COUT", TrapCategory.Monitor, CoutHandler,
                   "Output character to screen");
    traps.Register(0xFD0C, "RDKEY", TrapCategory.Monitor, RdkeyHandler,
                   "Read key from keyboard");
    
    // BASIC traps (optional - for ROM-free operation)
    if (_romFreeMode)
    {
        traps.Register(0xD683, "CHKCOM", TrapCategory.BasicInterp, ChkcomHandler);
        traps.Register(0xDFE3, "PRINT", TrapCategory.BasicRuntime, PrintHandler);
        // ... more BASIC traps ...
    }
    
    // Create CPU with trap registry
    var cpu = new Cpu65C02(bus, signals, traps);
    
    // ...
}
```

#### 8.3.7 ROM-Free Operation Mode

For fully ROM-free operation (no copyrighted code required), the emulator can:

1. **Provide stub ROM**: Minimal ROM with just vectors pointing to trapped addresses
2. **Trap all entry points**: Every documented ROM routine has a native handler
3. **Fallback behavior**: Unknown addresses return to a "not implemented" trap

```csharp
/// <summary>
/// Stub ROM for ROM-free operation.
/// Contains only vectors and trap landing pads.
/// </summary>
public sealed class StubRom : IBusTarget
{
    private readonly byte[] _rom;
    
    public StubRom()
    {
        _rom = new byte[0x4000];  // 16KB
        
        // Set reset vector to point to our trapped RESET handler
        _rom[0x3FFC - 0x0000] = 0x62;  // Low byte of $FA62
        _rom[0x3FFD - 0x0000] = 0xFA;  // High byte
        
        // Set IRQ/BRK vector
        _rom[0x3FFE - 0x0000] = 0x00;
        _rom[0x3FFF - 0x0000] = 0xFA;
        
        // Fill ROM area with BRK instructions (opcode $00)
        // Any untrapped access will trigger BRK → trap handler
        Array.Fill(_rom, (byte)0x00);
        
        // Or use $DB (STP on 65C02) to halt on unimplemented routines
        // Array.Fill(_rom, (byte)0xDB);
    }
    
    public byte Read8(Addr physicalAddress, in BusAccess access)
        => _rom[physicalAddress & 0x3FFF];
    
    // ... IBusTarget implementation ...
}
```

#### 8.3.8 Performance Considerations

- **Fast-path check**: `HasTrap(address)` should be O(1) using a hash set or bitmap
- **Hot addresses**: Most-called traps (COUT, RDKEY) should have minimal overhead
- **Selective trapping**: Categories can be disabled for debugging or timing accuracy
- **Cycle accuracy**: Trap handlers can optionally match original ROM timing

---

**Continued in [Part 3](Architecture%20Spec%20v1.0%20Part%203.md):**
- Part IX: Observability & Tracing
- Part X: Boot & Reset Sequence
- Part XI: Implementation Roadmap
- Part XII: Coding Standards & Conventions

---

## Document History

| Version | Date       | Changes                                      |
| ------- | ---------- | -------------------------------------------- |
| 1.0     | 2025-12-26 | Initial consolidated specification           |
| 1.1     | 2025-01-13 | Split sections IX-XII into Part 3            |
| 1.2     | 2025-01-13 | Added sections 7.5-7.7 (Machine, CPU, Init)  |
| 1.3     | 2025-01-13 | Added section 8.3 (ROM Trap Handlers)        |