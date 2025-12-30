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

#### 5.1.1 Apple II Interrupt Model

The Apple II has no interrupt controller. All slots share a single IRQ line in a wired-OR 
configuration. When any device asserts IRQ, the CPU's interrupt handler must poll each 
device's status register to identify the source(s).

The `ISignalBus` tracks asserting device IDs for observability and debugging, but this 
information is not available to emulated software—it must poll just as real software did.

**Device status register convention**: Each interrupt-capable card provides a status 
register in its slot I/O space ($C0n0-$C0nF) with a bit indicating pending interrupt. 
Reading this register typically acknowledges and clears the interrupt.

#### 5.1.2 Interrupt Service Conventions

Apple II peripheral cards have no standardized interrupt status register offset or handler 
entry point. Each card defines its own:

- **Status register location**: Somewhere in $C0n0-$C0nF, card-specific
- **Status bit position**: Often bit 7 (for BMI/BPL efficiency), but not guaranteed
- **Handler entry point**: In expansion ROM ($C800+), documented per-card

**Common patterns observed**:
- Super Serial Card: Status at offset +9, handler at $C803
- Mouse Card: Status at offset +4, handler at $C804
- Mockingboard: Uses 6522 VIA interrupts, multiple sources

The IRQ handler (in system ROM or DOS/ProDOS) must poll each device and **manually select 
the correct expansion ROM** before calling into slot firmware. This is why expansion ROM 
selection state matters for accurate interrupt handling.

For ProDOS compatibility, the emulator should support the MLI INSTALL_INTERRUPT ($40) and 
REMOVE_INTERRUPT ($41) calls, which maintain a table of registered interrupt handlers.

#### 5.1.3 IRQ Vector Architecture

The 6502 IRQ/BRK vector at $FFFE-$FFFF is in ROM and has a fixed value ($FA86 on Apple IIe).
The ROM handler at this address performs minimal dispatch:

1. Saves registers (A, X, Y)
2. Tests the B flag in the saved status register
3. If B=1 (BRK instruction): `JMP (BRKV)` through RAM vector at $03F0
4. If B=0 (hardware IRQ): `JMP (IRQV)` through RAM vector at $03FE

**RAM vectors** (in page 3) allow software to install custom handlers without ROM modification:

| Vector | Address | Purpose |
|--------|---------|---------|
| BRKV | $03F0-$03F1 | BRK instruction handler |
| IRQV | $03FE-$03FF | Hardware IRQ handler |

DOS, ProDOS, and applications patch these RAM vectors during initialization. The emulator
must ensure page 3 ($0300-$03FF) is writable RAM for proper interrupt handling.

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
    // ─── Identity ───────────────────────────────────────────────────────
    CpuFamily Family { get; }
    ArchitecturalMode CurrentMode { get; }
    
    // ─── State Access ───────────────────────────────────────────────────
    ref Registers Registers { get; }
    bool Halted { get; }
    bool IsStopRequested { get; }
    
    // ─── Context (Bus, Scheduler, Signals) ──────────────────────────────
    IEventContext Context { get; }
    
    // ─── Lifecycle ──────────────────────────────────────────────────────
    CpuStepResult Step();
    void Reset();
    void RequestStop();
    void ClearStopRequest();
    
    // ─── Debug/Observability ────────────────────────────────────────────
    InstructionTrace?  LastInstruction { get; }
}

/// <summary>
/// Debug information about the last executed instruction. 
/// Separated from core CPU state per architectural discussion.
/// </summary>
public readonly record struct InstructionTrace(
    byte OpCode,
    byte SubOpCode,
    CpuInstructions Instruction,
    CpuAddressingModes AddressingMode,
    byte OperandSize,
    OperandBuffer Operands,
    Addr? EffectiveAddress,
    Cycle StartCycle,
    Cycle InstructionCycles);
```

---

## Part VII: Device & Peripheral Architecture

This section describes the Apple II's device I/O architecture in detail. Understanding these
hardware mechanisms is essential for accurate emulation and for implementing ROM trap handlers
that interact correctly with the slot-based peripheral system.

### 7.1 The Apple II I/O Page ($C000-$CFFF)

The Apple II dedicates a 4KB region from $C000 to $CFFF for all hardware I/O. This region is
divided into several distinct sub-regions, each with different purposes and behaviors:

```
$C000-$C0FF : Soft Switches (256 bytes)
              - Keyboard, speaker, video mode, game I/O
              - Language card bank switching
              - Slot-specific device I/O ($C080-$C0FF)

$C100-$C7FF : Peripheral Card ROM (1,792 bytes)
              - 7 slots × 256 bytes each
              - $Cn00-$CnFF for slot n (n = 1-7)
              - Contains card identification and boot code

$C800-$CFFF : Expansion ROM Bank (2,048 bytes)
              - Shared by all slots
              - Only one slot's expansion ROM visible at a time
              - Selected by accessing $Cn00-$CnFF range
              - Deselected by accessing $CFFF
```

#### 7.1.1 Why This Layout Exists

The 6502's 16-bit address space (64KB) forced Apple's engineers to make compromises. They
couldn't give each peripheral card its own large ROM area, so they created a **banked ROM
scheme**:

1. **Small per-slot ROM ($Cn00-$CnFF)**: Each slot gets 256 bytes always visible. This is
   enough for a boot signature, identification bytes, and a small stub loader.

2. **Shared expansion ROM ($C800-$CFFF)**: A 2KB region that any slot can claim. Cards with
   more firmware (like the Disk II controller or Super Serial Card) store their main code here.

3. **Dynamic selection**: Accessing a slot's small ROM area ($Cn00-$CnFF) automatically makes
   that slot's expansion ROM appear at $C800-$CFFF. Reading $CFFF releases the expansion ROM
   back to a "neutral" state.

This clever design allows cards with 2KB+ of firmware to coexist with simpler cards that
need no expansion ROM at all.

#### 7.1.2 The Selection Protocol

The expansion ROM selection works through **address snooping**. When the CPU accesses any
address in the $C100-$C7FF range:

1. All cards see this access (they're connected to the address bus)
2. The card whose slot matches extracts its slot number from the address
3. That card asserts a signal claiming the $C800-$CFFF region
4. Any previously-selected card releases the region

This is implemented in hardware through slot-select logic. In emulation, we track which
slot "owns" the expansion ROM region:

```csharp
// When CPU accesses $Cn00-$CnFF (slot ROM):
int slot = (address >> 8) & 0x07;  // Extract slot number
if (slot >= 1 && slot <= 7)
{
    _activeExpansionSlot = slot;  // This slot now owns $C800-$CFFF
}

// When CPU accesses $CFFF:
if (address == 0xCFFF)
{
    _activeExpansionSlot = null;  // No slot owns expansion ROM
}
```

#### 7.1.3 Read vs. Write Behavior

The slot ROM and expansion ROM regions are read-only by design—they contain firmware burned
into EPROM on the physical cards. However, the **access itself** triggers the selection logic,
regardless of whether it's a read or write:

- **JSR $C600**: Jump to slot 6 ROM, selects slot 6's expansion ROM
- **LDA $C300**: Load from slot 3 ROM, selects slot 3's expansion ROM
- **STA $C800**: Write attempt (ignored by ROM), but address is still seen by selection logic

This means even failed write attempts to ROM addresses can change which expansion ROM is visible.
The emulator must model this correctly.

### 7.2 Soft Switch Sub-Regions ($C000-$C0FF)

The first 256 bytes of the I/O page contain the **soft switches**—memory-mapped registers that
control hardware state and read device status.

#### 7.2.1 System Switches ($C000-$C07F)

| Range       | Purpose                                              |
|-------------|------------------------------------------------------|
| $C000-$C00F | Keyboard data, 80-column control (IIe/IIc)           |
| $C010-$C01F | Keyboard strobe, status reads (IIe)                  |
| $C020-$C02F | Cassette output (legacy), utility strobes            |
| $C030-$C03F | Speaker toggle                                       |
| $C040-$C04F | Game I/O strobe                                      |
| $C050-$C05F | Graphics mode switches, annunciators                 |
| $C060-$C06F | Pushbuttons, paddle inputs                           |
| $C070-$C07F | Paddle trigger                                       |

#### 7.2.2 Language Card Switches ($C080-$C08F)

The Language Card (and built-in equivalent on IIe/IIc) provides 16KB of RAM that can overlay
the ROM at $D000-$FFFF. These 16 switch addresses control three independent settings:

1. **Read source**: ROM or RAM?
2. **Write enable**: Can writes go to Language Card RAM?
3. **Bank select**: Which 4KB bank at $D000-$DFFF?

The encoding is complex because it evolved from the original Language Card's design:

| Address | Read    | Write   | Bank |
|---------|---------|---------|------|
| $C080   | RAM     | Disable | 2    |
| $C081   | ROM     | Enable* | 2    |
| $C082   | ROM     | Disable | 2    |
| $C083   | RAM     | Enable* | 2    |
| $C088   | RAM     | Disable | 1    |
| $C089   | ROM     | Enable* | 1    |
| $C08A   | ROM     | Disable | 1    |
| $C08B   | RAM     | Enable* | 1    |

*Write-enable requires two consecutive reads of the same address (a "double read" protocol
designed to prevent accidental writes).

##### 7.2.2.1 Language Card and CPU Vectors

When Language Card RAM is enabled for reading ($C080, $C083, $C088, $C08B), the entire 
$D000-$FFFF range reads from LC RAM, **including the CPU vectors at $FFFA-$FFFF**.

The hardware provides **no special protection** for the vector area. Software that enables 
LC RAM reading must first copy the ROM contents (including vectors) to LC RAM, or the 
system will crash on the next interrupt.

**Emulation requirement**: Do not add special-case handling for the vector addresses. 
The emulator should return LC RAM content for $FFFA-$FFFF when LC read mode is active, 
even if that content is garbage. This matches real hardware behavior.

**Initialization**: Language Card RAM should be initialized to undefined values (random 
or zero) on cold boot. Well-behaved software always copies ROM to LC RAM before enabling 
LC read mode.

#### 7.2.3 Slot Device I/O ($C090-$C0FF)

Each slot gets 16 bytes of dedicated I/O space:

| Address Range | Slot | Common Usage                          |
|---------------|------|---------------------------------------|
| $C090-$C09F   | 1    | Printer cards                         |
| $C0A0-$C0AF   | 2    | Serial cards                          |
| $C0B0-$C0BF   | 3    | 80-column/RAM cards (IIe internal)    |
| $C0C0-$C0CF   | 4    | Mockingboard, mouse, clock            |
| $C0D0-$C0DF   | 5    | RAM cards, accelerators               |
| $C0E0-$C0EF   | 6    | **Disk II (most common)**             |
| $C0F0-$C0FF   | 7    | RAM cards, hard drives                |

The device I/O region is where the card's "active" hardware lives—registers that control
motors, read data latches, trigger operations. The ROM regions ($Cn00 and $C800) contain
code; the I/O region ($C0n0) contains the live device interface.

### 7.3 Peripheral Interface

```csharp
/// <summary>
/// A peripheral device that can be installed in an Apple II slot.
/// </summary>
/// <remarks>
/// <para>
/// Apple II peripheral cards can have up to three memory-mapped regions:
/// </para>
/// <list type="number">
/// <item>
/// <term>MMIO ($C0n0-$C0nF)</term>
/// <description>16 bytes of device-specific I/O registers. Directly controls
/// the hardware: motor on/off, read/write data, status bits.</description>
/// </item>
/// <item>
/// <term>Slot ROM ($Cn00-$CnFF)</term>
/// <description>256 bytes of identification and boot code. Contains the
/// signature bytes software uses to identify card type, plus minimal boot
/// stub code.</description>
/// </item>
/// <item>
/// <term>Expansion ROM ($C800-$CFFF)</term>
/// <description>2KB of shared ROM space. Selected when slot ROM is accessed,
/// deselected by reading $CFFF. Contains main firmware for complex cards.</description>
/// </item>
/// </list>
/// </remarks>
public interface IPeripheral : IScheduledDevice
{
    /// <summary>Gets the human-readable name of this card instance.</summary>
    string Name { get; }
    
    /// <summary>Gets the device type identifier (e.g., "DiskII", "MockingBoard").</summary>
    string DeviceType { get; }
    
    /// <summary>
    /// Gets the MMIO region handler for slot I/O space ($C0n0-$C0nF).
    /// Returns null if this card has no device I/O registers.
    /// </summary>
    IBusTarget? MMIORegion { get; }
    
    /// <summary>
    /// Gets the firmware ROM region handler ($Cn00-$CnFF).
    /// Returns null if this card has no slot ROM (unusual).
    /// </summary>
    IBusTarget? ROMRegion { get; }
    
    /// <summary>
    /// Gets the expansion ROM region handler ($C800-$CFFF when selected).
    /// Returns null if this card has no expansion ROM.
    /// </summary>
    IBusTarget? ExpansionROMRegion { get; }
    
    /// <summary>
    /// Gets the slot number this card is installed in (1-7).
    /// Set by the slot manager during installation.
    /// </summary>
    int SlotNumber { get; set; }
    
    /// <summary>
    /// Called when this card's expansion ROM becomes active (another slot was
    /// deselected, or this slot's ROM was accessed).
    /// </summary>
    void OnExpansionROMSelected();
    
    /// <summary>
    /// Called when this card's expansion ROM becomes inactive ($CFFF accessed,
    /// or another slot was selected).
    /// </summary>
    void OnExpansionROMDeselected();
    
    /// <summary>
    /// Resets the peripheral to power-on state.
    /// </summary>
    void Reset();
}

/// <summary>
/// Device that participates in the cycle-accurate scheduler.
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

### 7.4 Slot Manager

The slot manager coordinates the seven expansion slots and handles the expansion ROM selection
protocol.

```csharp
/// <summary>
/// Manages the 7 expansion slots of an Apple II and the expansion ROM selection logic.
/// </summary>
/// <remarks>
/// <para>
/// The slot manager is responsible for:
/// </para>
/// <list type="bullet">
/// <item><description>Tracking which cards are installed in which slots</description></item>
/// <item><description>Routing I/O accesses ($C0n0-$C0nF) to the correct card</description></item>
/// <item><description>Routing ROM accesses ($Cn00-$CnFF) to the correct card</description></item>
/// <item><description>Managing the expansion ROM bank ($C800-$CFFF) selection</description></item>
/// <item><description>Handling the $CFFF release trigger</description></item>
/// </list>
/// <para>
/// The expansion ROM selection protocol:
/// </para>
/// <list type="number">
/// <item><description>Any access to $Cn00-$CnFF selects slot n's expansion ROM</description></item>
/// <item><description>Any access to $CFFF deselects all expansion ROMs</description></item>
/// <item><description>Only one slot's expansion ROM can be visible at a time</description></item>
/// <item><description>When no slot is selected, $C800-$CFFF returns floating bus</description></item>
/// </list>
/// </remarks>
public interface ISlotManager
{
    /// <summary>Gets installed cards by slot number (1-7).</summary>
    IReadOnlyDictionary<int, IPeripheral> Slots { get; }
    
    /// <summary>
    /// Gets the currently selected slot for expansion ROM ($C800-$CFFF).
    /// Null if no slot is selected (returns floating bus).
    /// </summary>
    int? ActiveExpansionSlot { get; }
    
    /// <summary>
    /// Installs a peripheral card in the specified slot.
    /// </summary>
    /// <param name="slot">Slot number (1-7).</param>
    /// <param name="card">The peripheral card to install.</param>
    /// <exception cref="ArgumentOutOfRangeException">Slot not in range 1-7.</exception>
    /// <exception cref="InvalidOperationException">Slot already occupied.</exception>
    void Install(int slot, IPeripheral card);
    
    /// <summary>
    /// Removes a peripheral card from the specified slot.
    /// </summary>
    /// <param name="slot">Slot number (1-7).</param>
    void Remove(int slot);
    
    /// <summary>
    /// Gets the card installed in a slot, or null if empty.
    /// </summary>
    IPeripheral? GetCard(int slot);
    
    /// <summary>
    /// Selects a slot's expansion ROM for the $C800-$CFFF region.
    /// Called when CPU accesses $Cn00-$CnFF.
    /// </summary>
    /// <param name="slot">Slot number (1-7).</param>
    void SelectExpansionSlot(int slot);
    
    /// <summary>
    /// Deselects expansion ROM (called when CPU accesses $CFFF).
    /// Returns $C800-$CFFF to floating bus state.
    /// </summary>
    void DeselectExpansionSlot();
    
    /// <summary>
    /// Called when the bus handles an access to $C100-$C7FF.
    /// Determines the slot and triggers expansion ROM selection.
    /// </summary>
    /// <param name="address">Address in range $C100-$C7FF.</param>
    void HandleSlotROMAccess(Addr address);
    
    /// <summary>
    /// Resets all peripheral cards and clears expansion ROM selection.
    /// </summary>
    void Reset();
}
```

### 7.5 Apple II Memory Map

The complete Apple II memory map for Pocket2e (128KB Apple IIe with auxiliary memory):

```
$0000-$00FF : Zero Page (256 bytes)
              - Direct page addressing base
              - Can switch to auxiliary zero page via soft switch
              
$0100-$01FF : Stack (256 bytes)
              - 6502 stack grows downward from $01FF
              - Can switch to auxiliary stack via soft switch
              
$0200-$02FF : System use (512 bytes)
              - $0200-$02FF: Input buffer (GETLN)

$0300-$03FF : System Vectors and DOS Work Area (256 bytes)
              - $0300-$03CF: DOS/ProDOS work area
              - $0300-$035F: DOS 3.3 work area / ProDOS system globals
              - $0360-$03CF: ProDOS file system buffers
              - $03D0-$03D2: DOS warm start (JMP instruction)
              - $03D3-$03D5: DOS cold start (JMP instruction)
              - $03EA-$03EC: Ampersand (&) command vector
              - $03F0-$03F1: BRKV - BRK handler vector
              - $03F2-$03F3: SOFTEV - Soft reset vector
              - $03F4: PWREDUP - Power-up byte ($A5 = warm reset OK)
              - $03F5-$03F7: Ampersand (&) alternate vector
              - $03F8-$03FA: USR() function vector
              - $03FE-$03FF: IRQV - Hardware IRQ vector
              
              **Critical**: This page MUST be writable RAM. The ROM uses 
              indirect jumps through these vectors, and DOS/ProDOS patches 
              them to intercept system events.
              
$0400-$07FF : Text Page 1 / Lo-Res Page 1 (1KB)
              - Primary text display memory
              - Also used for 40×48 lo-res graphics
              - 80-column mode uses main + aux memory
              
$0800-$0BFF : Text Page 2 / Lo-Res Page 2 (1KB)
              - Secondary text display memory
              - Can switch between pages 1 and 2
              
$0C00-$1FFF : Free RAM (5KB)
              
$2000-$3FFF : Hi-Res Page 1 (8KB)
              - 280×192 high-resolution graphics
              - Double hi-res uses main + aux memory
              
$4000-$5FFF : Hi-Res Page 2 (8KB)
              - Secondary hi-res page
              
$6000-$BFFF : Free RAM (24KB)
              - Applesoft programs load here by default
              - HIMEM typically set to $9600 with DOS
              
$C000-$C0FF : Soft Switches / Device I/O (256 bytes)
              - See Section 7.2 for detailed breakdown
              
$C100-$C7FF : Peripheral Card ROM (1,792 bytes)
              - 7 × 256-byte slot ROM regions
              - Access triggers expansion ROM selection
              
$C800-$CFFF : Expansion ROM (2KB)
              - Shared, bank-switched region
              - Contents depend on selected slot
              - $CFFF access deselects all slots
              
$D000-$DFFF : ROM Bank 1 or Language Card Bank 1/2 (4KB)
              - Normally contains Integer BASIC
              - Language Card provides two 4KB banks
              
$E000-$FFFF : ROM or Language Card RAM (8KB)
              - Applesoft BASIC ($E000-$F7FF)
              - Monitor ($F800-$FFFF)
              - Can be switched to Language Card RAM
              - $FFFA-$FFFF: 6502 vectors (NMI, RESET, IRQ)
```

### 7.6 The Composite I/O Page Handler

The $C000-$CFFF region is more complex than a simple RAM or ROM mapping because it contains
multiple overlapping sub-systems. We implement this as a **composite page** that dispatches
to the appropriate handler based on address.

```csharp
/// <summary>
/// Handles the Apple II I/O page ($C000-$CFFF).
/// This is a composite bus target that dispatches to various sub-handlers.
/// </summary>
/// <remarks>
/// <para>
/// The I/O page is divided into several regions with different behaviors:
/// </para>
/// <list type="bullet">
/// <item><description>$C000-$C07F: System soft switches (keyboard, video, etc.)</description></item>
/// <item><description>$C080-$C08F: Language card bank switching</description></item>
/// <item><description>$C090-$C0FF: Slot device I/O (16 bytes per slot)</description></item>
/// <item><description>$C100-$C7FF: Slot ROM (256 bytes per slot)</description></item>
/// <item><description>$C800-$CFFF: Expansion ROM (selected slot)</description></item>
/// </list>
/// <para>
/// All accesses to this page may have side effects. Even reads from "meaningless"
/// addresses can trigger state changes (e.g., speaker toggle, mode switches).
/// </para>
/// </remarks>
public sealed class AppleIIIOPage : ICompositeTarget
{
    private readonly IVideoController _video;
    private readonly IKeyboard _keyboard;
    private readonly ISpeaker _speaker;
    private readonly IGameIO _gameIO;
    private readonly ILanguageCard _languageCard;
    private readonly ISlotManager _slots;
    private readonly IAuxiliaryMemory? _auxMemory;  // IIe only
    
    public TargetCaps Capabilities => TargetCaps.SideEffects | TargetCaps.TimingSense;
    
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        ushort offset = (ushort)(physicalAddress & 0x0FFF);
        
        // Dispatch based on address range
        return offset switch
        {
            // ─── System Soft Switches ($C000-$C07F) ─────────────────────
            < 0x010 => HandleKeyboard(offset, isWrite: false),
            < 0x020 => HandleKeyboardStrobe(offset, isWrite: false),
            < 0x030 => HandleCassette(offset, isWrite: false),
            < 0x040 => HandleSpeaker(offset),
            < 0x050 => HandleUtilityStrobe(offset),
            < 0x060 => HandleGraphicsMode(offset),
            < 0x070 => HandleGameIO(offset, isWrite: false),
            < 0x080 => HandlePaddleTrigger(offset),
            
            // ─── Language Card ($C080-$C08F) ────────────────────────────
            < 0x090 => HandleLanguageCard(offset, isWrite: false),
            
            // ─── Slot Device I/O ($C090-$C0FF) ──────────────────────────
            < 0x100 => HandleSlotIO(offset, isWrite: false, value: 0),
            
            // ─── Slot ROM ($C100-$C7FF) ─────────────────────────────────
            < 0x800 => HandleSlotROM(offset, in access),
            
            // ─── Expansion ROM ($C800-$CFFF) ────────────────────────────
            _ => HandleExpansionROM(offset, in access)
        };
    }
    
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        ushort offset = (ushort)(physicalAddress & 0x0FFF);
        
        // Many soft switches respond to writes the same as reads
        // Slot ROM and expansion ROM ignore writes (they're ROM)
        switch (offset)
        {
            case < 0x010: HandleKeyboard(offset, isWrite: true); break;
            case < 0x020: HandleKeyboardStrobe(offset, isWrite: true); break;
            case < 0x030: HandleCassette(offset, isWrite: true); break;
            case < 0x040: HandleSpeaker(offset); break;  // Toggle on any access
            case < 0x050: HandleUtilityStrobe(offset); break;
            case < 0x060: HandleGraphicsMode(offset); break;  // Mode changes on any access
            case < 0x070: HandleGameIO(offset, isWrite: true); break;
            case < 0x080: HandlePaddleTrigger(offset); break;
            case < 0x090: HandleLanguageCard(offset, isWrite: true); break;
            case < 0x100: HandleSlotIO(offset, isWrite: true, value); break;
            case < 0x800: 
                // Slot ROM: Ignore write, but still trigger expansion ROM selection!
                HandleSlotROM(offset, in access);
                break;
            default:
                // Expansion ROM: Ignore write, but check for $CFFF
                HandleExpansionROM(offset, in access);
                break;
        }
    }
    
    /// <summary>
    /// Handles slot ROM access and expansion ROM selection.
    /// </summary>
    private byte HandleSlotROM(ushort offset, in BusAccess access)
    {
        // Extract slot number from address: $Cn00 → slot n
        int slot = (offset >> 8) & 0x07;
        
        // Trigger expansion ROM selection for this slot
        // This happens even if the slot is empty!
        if (slot >= 1 && slot <= 7)
        {
            _slots.SelectExpansionSlot(slot);
        }
        
        // Return ROM data if card has ROM, otherwise floating bus
        var card = _slots.GetCard(slot);
        if (card?.ROMRegion is { } rom)
        {
            ushort romOffset = (ushort)(offset & 0x00FF);
            return rom.Read8(romOffset, access);
        }
        
        return FloatingBus();
    }
    
    /// <summary>
    /// Handles expansion ROM access.
    /// </summary>
    private byte HandleExpansionROM(ushort offset, in BusAccess access)
    {
        // Special case: $CFFF deselects expansion ROM
        if (offset == 0x0FFF)
        {
            _slots.DeselectExpansionSlot();
            return FloatingBus();
        }
        
        // Return data from selected slot's expansion ROM
        int? activeSlot = _slots.ActiveExpansionSlot;
        if (activeSlot is { } slot)
        {
            var card = _slots.GetCard(slot);
            if (card?.ExpansionROMRegion is { } expRom)
            {
                ushort expOffset = (ushort)(offset - 0x0800);
                return expRom.Read8(expOffset, access);
            }
        }
        
        // No expansion ROM selected or slot has no expansion ROM
        return FloatingBus();
    }
    
    /// <summary>
    /// Handles slot device I/O ($C0n0-$C0nF).
    /// </summary>
    private byte HandleSlotIO(ushort offset, bool isWrite, byte value)
    {
        // Extract slot number: $C090 → slot 1, $C0E0 → slot 6
        int slot = ((offset - 0x080) >> 4) + 1;
        int ioOffset = offset & 0x0F;
        
        // For slot 0 ($C080-$C08F), this is Language Card, handled separately
        if (slot < 1 || slot > 7)
            return FloatingBus();
        
        var card = _slots.GetCard(slot);
        if (card?.MMIORegion is { } mmio)
        {
            if (isWrite)
            {
                mmio.Write8((uint)ioOffset, value, default);
                return value;  // Write doesn't return meaningful data
            }
            else
            {
                return mmio.Read8((uint)ioOffset, default);
            }
        }
        
        return FloatingBus();
    }
    
    private byte FloatingBus()
    {
        // Simple implementation: return $FF
        // Accurate implementation: return value from video memory fetch
        return 0xFF;
    }
    
    // ... other handler methods for keyboard, video, etc. ...
}
```

### 7.7 Expansion ROM Lifecycle

Understanding the expansion ROM lifecycle is critical for implementing trap handlers that
work with peripheral card firmware.

#### 7.7.1 Selection Triggers

Expansion ROM selection occurs when:

1. **Direct ROM access**: `LDA $C600` (load from slot 6 ROM) → slot 6 expansion selected
2. **Code execution**: `JSR $C600` (call into slot 6 ROM) → slot 6 expansion selected
3. **Indirect through zero page**: If zero-page vector points to $Cn00 range

#### 7.7.2 Deselection Trigger

The **only** way to deselect expansion ROM is accessing $CFFF:

```assembly
; Standard idiom to "close" expansion ROM:
    LDA $CFFF    ; Read from $CFFF, deselects all slots
; or
    STA $CFFF    ; Write to $CFFF works too
```

Note: Selecting a different slot implicitly deselects the previous slot. Accessing $CFFF
deselects all slots, returning $C800-$CFFF to floating bus.

#### 7.7.3 Trap Handler Implications

ROM traps must be aware of the expansion ROM selection state:

1. **Trap addresses in slot ROM ($Cn00-$CnFF)**: These are always valid when their slot
   contains a card. Trapping $C600 for Disk II boot is always safe.

2. **Trap addresses in expansion ROM ($C800-$CFFF)**: These are **context-dependent**—the
   trap is only meaningful when the correct slot's expansion ROM is selected.

```csharp
/// <summary>
/// Example: Conditional trap for Disk II expansion ROM routine.
/// </summary>
public class DiskIIExpansionTrap
{
    private readonly int _diskSlot;
    private readonly ISlotManager _slots;
    
    public TrapResult Handle(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        // Only handle if Disk II slot's expansion ROM is active
        if (_slots.ActiveExpansionSlot != _diskSlot)
        {
            // Wrong expansion ROM is selected - don't trap, let ROM execute
            return new TrapResult(Handled: false, default, null);
        }
        
        // Correct expansion ROM - handle the trap
        // ... native implementation ...
        return new TrapResult(Handled: true, new Cycle(42), null);
    }
}
```

#### 7.7.4 The Internal ROM Soft Switches (Apple IIe/IIc)

The Apple IIe and IIc added complexity with soft switches that can replace slot ROM
with internal (motherboard) ROM:

| Switch   | Effect                                                 |
|----------|--------------------------------------------------------|
| $C006    | SETSLOTCX: Slot ROM at $C100-$CFFF (normal)            |
| $C007    | SETINTCX: Internal ROM at $C100-$CFFF (replaces slots) |
| $C00A    | SETINTC3: Internal ROM at $C300 only (80-col firmware) |
| $C00B    | SETSLOTC3: Slot ROM at $C300 (restore slot 3)          |

When internal ROM is selected:
- Accessing $C100-$C7FF reads internal firmware, not slot ROMs
- Expansion ROM selection is disabled
- $C800-$CFFF contains internal expansion ROM

The emulator must check these switches before routing slot ROM/expansion accesses:

```csharp
private bool IsInternalROMEnabled => _softSwitches.GetIntCxRom();
private bool IsInternalC3ROMEnabled => _softSwitches.GetIntC3Rom();

private byte HandleSlotROM(ushort offset, in BusAccess access)
{
    // Check if internal ROM overrides slot ROM
    if (IsInternalROMEnabled)
    {
        // $C100-$CFFF comes from internal ROM
        return _internalROM.Read8(offset, access);
    }
    
    int slot = (offset >> 8) & 0x07;
    
    // Special case: $C300 can be independently switched
    if (slot == 3 && IsInternalC3ROMEnabled)
    {
        return _internalROM.Read8(offset, access);
    }
    
    // Normal slot ROM access
    _slots.SelectExpansionSlot(slot);
    // ... return slot ROM data ...
}
```

### 7.8 Apple IIc Differences

The Apple IIc is a compact, integrated version of the Apple IIe. While it maintains software
compatibility, the hardware differs significantly:

#### 7.8.1 No Physical Slots

The Apple IIc has no external expansion slots. Instead, it includes integrated peripherals:

- **Built-in Disk II controller** (acts like slot 6)
- **Built-in serial ports** (act like slots 1 and 2)
- **Built-in mouse port** (acts like slot 4)
- **Memory expansion connector** (acts like slot 5)

The firmware simulates the slot ROM and I/O space for these internal devices, so software
that probes for cards at $C600 still finds the disk controller.

#### 7.8.2 Memory-Mapped ROM Selection

The IIc's built-in peripherals have their firmware in internal ROM, but the addressing scheme
follows the IIe pattern:

```
$C100-$C1FF : Serial Port 1 ROM (like slot 1)
$C200-$C2FF : Serial Port 2 ROM (like slot 2)
$C400-$C4FF : Mouse Port ROM (like slot 4)
$C500-$C5FF : Memory Expansion ROM (like slot 5)
$C600-$C6FF : Disk Controller ROM (like slot 6)
```

The expansion ROM bank ($C800-$CFFF) works identically—accessing a "slot" ROM selects that
device's expansion ROM.

#### 7.8.3 Firmware Switching

The IIc ROM (and later versions of IIe ROM) includes the INTCXROM soft switch that can map
internal firmware over the slot ROM addresses:

| Switch | Address | Effect                                    |
|--------|---------|-------------------------------------------|
| SETSLOTCXROM | $C006 | External/slot ROM visible at $C100-$CFFF |
| SETINTCXROM  | $C007 | Internal ROM visible at $C100-$CFFF      |

When INTCXROM is set, accessing $C300 (for example) returns the internal 80-column firmware
instead of probing for an external card.

#### 7.8.4 Emulation Implications

For Pocket2c (Apple IIc emulation):

1. **No slot manager needed**: All peripherals are internal
2. **Simplified I/O routing**: The "slot" ROM and I/O addresses route to internal devices
3. **ROM contains everything**: One ROM image includes all peripheral firmware
4. **Trap registration simplified**: All ROM addresses are always valid (no slot checking needed)

```csharp
/// <summary>
/// Apple IIc has simpler trap registration since all "slots" are internal.
/// </summary>
public void RegisterIIcTraps(ITrapRegistry traps)
{
    // Disk controller (internal slot 6)
    traps.Register(0xC600, "IIC_DISK_BOOT", TrapCategory.SlotFirmware, 
                   DiskBootHandler, "IIc built-in disk controller boot");
    
    // Serial port 1 (internal slot 1)
    traps.Register(0xC100, "IIC_SERIAL1_INIT", TrapCategory.SlotFirmware,
                   Serial1InitHandler, "IIc serial port 1 initialization");
    
    // No need for slot validation - these are always present
    // Expansion ROM traps still need INTCXROM check though
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

The emulator intercepts calls to well-known ROM entry points and provides native implementations.
This section explains the trap mechanism, its interaction with the Apple II's dynamic ROM banking,
and the considerations for implementing trap handlers correctly.

#### 8.3.1 Why Trap ROM Routines?

ROM trapping serves several purposes:

1. **Legal compliance**: Avoids distributing or requiring copyrighted ROM images. Apple's ROMs
   are still under copyright, and distributing them with an emulator creates legal risk.

2. **Performance**: Native implementations can be orders of magnitude faster than cycle-accurate
   emulation. A routine like WAIT ($FCA8), which is just a timing loop, takes thousands of
   cycles when emulated accurately—but we can skip it entirely with a trap.

3. **Enhanced functionality**: Native implementations can provide features the original ROMs
   lack, like better error messages, debugging hooks, or modern file system access.

4. **ROM-free operation**: With comprehensive trapping, the emulator can run without any
   physical ROM image, using native code for all ROM entry points.

5. **Debugging**: Trap handlers can log calls, capture parameters, and provide rich diagnostics.

#### 8.3.2 The Trap Mechanism

ROM interception uses a **trap on instruction fetch**. When the CPU fetches an opcode from a
trapped address:

```
┌─────────────────────────────────────────────────────────────┐
│                     Normal Execution                         │
│                                                              │
│   JSR $FDED  →  Fetch opcode at $FDED  →  Execute COUT      │
│               (from ROM)                   routine           │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    Trapped Execution                         │
│                                                              │
│   JSR $FDED  →  Fetch triggers trap  →  Native COUT        │
│               check                      handler executes    │
│                                         RTS simulation       │
└─────────────────────────────────────────────────────────────┘
```

The trap is transparent to guest code—it sees the expected behavior without knowing the
implementation is native.

#### 8.3.3 ROM Address Space Classification

Apple II ROM addresses fall into several categories with different trapping considerations:

| Address Range   | Type              | Trapping Safety                          |
|-----------------|-------------------|------------------------------------------|
| $D000-$F7FF     | Applesoft BASIC   | Always safe (fixed location)             |
| $F800-$FFFF     | Monitor ROM       | Always safe (fixed location)             |
| $Cn00-$CnFF     | Slot ROM          | Safe if slot has card                    |
| $C800-$CFFF     | Expansion ROM     | **Context-dependent**—must check active slot |

#### 8.3.4 Fixed ROM Traps (System ROM)

Traps in the $D000-$FFFF range are straightforward because this ROM is always at the same
address (unless Language Card RAM is enabled, which replaces it).

```csharp
// Safe to trap unconditionally:
traps.Register(0xFDED, "COUT", TrapCategory.Monitor, CoutHandler,
    "Output character to current output device");

traps.Register(0xFD0C, "RDKEY", TrapCategory.Monitor, RdkeyHandler,
    "Read character from current input device");

traps.Register(0xFC58, "HOME", TrapCategory.Monitor, HomeHandler,
    "Clear screen and home cursor");
```

**Language Card consideration**: When Language Card RAM is enabled for reading ($C080, $C083,
$C088, $C08B), the $D000-$FFFF range contains RAM, not ROM. Traps at these addresses would
intercept code that's **not** the system ROM.

Options for handling this:
1. **Ignore**: Assume users won't trap user-written code in LC RAM
2. **Check LC state**: Only trap if LC reading ROM
3. **Trap both**: Works if the trap behavior is appropriate for both cases

For Pocket2e, we choose option 1—it's rare for user code to use the exact same addresses
as ROM entry points.

#### 8.3.5 Slot ROM Traps ($Cn00-$CnFF)

Slot ROM traps are valid when the corresponding slot contains a card. The trap should verify
the slot is occupied:

```csharp
/// <summary>
/// Trap for Disk II boot routine at $C600.
/// </summary>
public TrapResult DiskIIBootTrap(ICpu cpu, IMemoryBus bus, IEventContext context)
{
    // Verify slot 6 has a Disk II controller
    var card = _slots.GetCard(6);
    if (card?.DeviceType != "DiskII")
    {
        // No Disk II in slot 6 - don't trap
        return new TrapResult(Handled: false, default, null);
    }
    
    // Native boot implementation
    return NativeBootFromDisk(cpu, bus, context);
}
```

**Important**: Accessing slot ROM triggers expansion ROM selection. If the trap returns
"handled," the CPU never actually accesses the ROM address, so the expansion ROM selection
**doesn't happen automatically**. The trap handler must explicitly select the expansion ROM
if the original code would have done so:

```csharp
public TrapResult DiskIIBootTrap(ICpu cpu, IMemoryBus bus, IEventContext context)
{
    // Simulate the effect of accessing $C600 - select slot 6 expansion ROM
    _slots.SelectExpansionSlot(6);
    
    // ... native implementation ...
}
```

#### 8.3.6 Expansion ROM Traps ($C800-$CFFF)

Expansion ROM traps are the most complex because the contents of $C800-$CFFF depend on
which slot is currently selected. A trap at $C803 might be valid Disk II firmware, or it
might be Super Serial Card firmware, or it might be nothing (no slot selected).

**Critical rule**: Expansion ROM traps must check which slot owns the expansion ROM space
before handling the trap.

```csharp
/// <summary>
/// Conditional trap for Disk II RWTS routine in expansion ROM.
/// </summary>
public class DiskIIRWTSTrap : ITrapHandler
{
    private readonly int _diskSlot;
    private readonly ISlotManager _slots;
    
    public TrapResult Execute(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        // ONLY handle if Disk II's expansion ROM is active
        if (_slots.ActiveExpansionSlot != _diskSlot)
        {
            // Different slot's expansion ROM is visible - not our code!
            return new TrapResult(Handled: false, default, null);
        }
        
        // Verify it's actually a Disk II controller
        var card = _slots.GetCard(_diskSlot);
        if (card?.DeviceType != "DiskII")
        {
            return new TrapResult(Handled: false, default, null);
        }
        
        // Now safe to execute native RWTS
        return NativeRWTS(cpu, bus, context);
    }
}
```

**Registration pattern for expansion ROM traps**:

```csharp
/// <summary>
/// Registers expansion ROM traps for a specific slot's card.
/// </summary>
public void RegisterExpansionROMTraps(int slot, string deviceType)
{
    switch (deviceType)
    {
        case "DiskII":
            // All Disk II expansion ROM traps are conditional on slot selection
            RegisterConditionalTrap(0xC800, slot, "DISK_BOOT2", DiskBoot2Handler);
            RegisterConditionalTrap(0xC8XX, slot, "DISK_RWTS", DiskRWTSHandler);
            // ... more Disk II traps ...
            break;
            
        case "SuperSerial":
            RegisterConditionalTrap(0xC800, slot, "SSC_INIT", SSCInitHandler);
            // ... more SSC traps ...
            break;
    }
}

private void RegisterConditionalTrap(Addr address, int slot, string name, 
                                     TrapHandler handler)
{
    // Wrap the handler with slot validation
    TrapResult ConditionalHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        if (_slots.ActiveExpansionSlot != slot)
            return new TrapResult(Handled: false, default, null);
        return handler(cpu, bus, context);
    }
    
    _trapRegistry.Register(address, name, TrapCategory.SlotFirmware, 
                           ConditionalHandler, $"Slot {slot} expansion ROM");
}
```

#### 8.3.7 Trap Registry Lifecycle

The trap registry must be updated when slots change:

```csharp
public void OnSlotInstall(int slot, IPeripheral card)
{
    // Register traps for the card's ROM entry points
    RegisterSlotROMTraps(slot, card);
    RegisterExpansionROMTraps(slot, card);
}

public void OnSlotRemove(int slot)
{
    // Unregister all traps for this slot
    UnregisterTrapsForSlot(slot);
}
```

#### 8.3.8 Handling the $CFFF Access

When the CPU accesses $CFFF (to deselect expansion ROM), any trap at that address needs
special consideration:

```csharp
/// <summary>
/// The $CFFF access is special - it deselects expansion ROM.
/// </summary>
public TrapResult CFFFAccessHandler(ICpu cpu, IMemoryBus bus, IEventContext context)
{
    // Deselect expansion ROM (this is the side effect of the access)
    _slots.DeselectExpansionSlot();
    
    // Return floating bus value (there's no meaningful data at $CFFF)
    // The trap is "handled" in the sense that we performed the side effect
    return new TrapResult(
        Handled: true, 
        CyclesConsumed: new Cycle(4),  // Approximate LDA absolute timing
        ReturnAddress: null  // Continue execution normally
    );
}
```

Alternatively, don't trap $CFFF at all—let the bus handler manage the side effect directly.

#### 8.3.9 Complete Trap Registry Interface

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
    SlotFirmware,   // Slot card ROM routines (Disk II, serial, etc.)
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
    bool Enabled,
    int? SlotDependency      // For expansion ROM traps: which slot must be active
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
    /// Registers a slot-dependent trap (for expansion ROM addresses).
    /// </summary>
    void RegisterSlotDependent(Addr address, int slot, string name, 
                               TrapCategory category, TrapHandler handler,
                               string? description = null);
    
    /// <summary>
    /// Unregisters a trap at the specified address.
    /// </summary>
    bool Unregister(Addr address);
    
    /// <summary>
    /// Unregisters all traps associated with a slot (for slot removal).
    /// </summary>
    void UnregisterSlotTraps(int slot);
    
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

#### 8.3.10 Well-Known Trap Points

##### Monitor ROM ($F800-$FFFF)

| Address | Name     | Description                           | Safe to Trap? |
|---------|----------|---------------------------------------|---------------|
| $F800   | PLOT     | Plot lo-res point                     | Yes           |
| $F819   | HLINE    | Draw horizontal lo-res line           | Yes           |
| $F828   | VLINE    | Draw vertical lo-res line             | Yes           |
| $F832   | CLRSCR   | Clear lo-res screen                   | Yes           |
| $F836   | CLRTOP   | Clear top of lo-res screen            | Yes           |
| $F847   | GBASCALC | Calculate lo-res base address         | Yes           |
| $F856   | SETCOL   | Set lo-res color                      | Yes           |
| $F871   | SCRN     | Read lo-res screen color              | Yes           |
| $FB1E   | PREAD    | Read paddle position                  | Yes           |
| $FB2F   | INIT     | Initialize text screen                | Yes           |
| $FB39   | SETTXT   | Set text mode                         | Yes           |
| $FB40   | SETGR    | Set lo-res graphics mode              | Yes           |
| $FBC1   | BASCALC  | Calculate text base address           | Yes           |
| $FC10   | PRBLNK   | Print 3 spaces                        | Yes           |
| $FC22   | VTAB     | Move cursor to line                   | Yes           |
| $FC42   | CLREOLZ  | Clear to end of line (with check)     | Yes           |
| $FC58   | HOME     | Clear screen and home cursor          | Yes           |
| $FC62   | CR       | Output carriage return                | Yes           |
| $FC66   | LF       | Output line feed                      | Yes           |
| $FC70   | SCROLL   | Scroll screen up one line             | Yes           |
| $FC9C   | CLREOL   | Clear to end of line                  | Yes           |
| $FCA8   | WAIT     | Delay loop (A × ~2.5μs per count)     | Yes           |
| $FD0C   | RDKEY    | Read key from keyboard                | Yes           |
| $FD35   | RDCHAR   | Read character with escape handling   | Yes           |
| $FD67   | GETLN    | Get line of input                     | Yes           |
| $FD6A   | GETLN1   | Get line (skip prompt)                | Yes           |
| $FD6F   | GETLNZ   | Get line (clear first)                | Yes           |
| $FD8E   | CROUT    | Output CR                             | Yes           |
| $FDDA   | PRBYTE   | Print byte as hex                     | Yes           |
| $FDE3   | PRHEX    | Print nibble as hex                   | Yes           |
| $FDED   | COUT     | Output character                      | Yes           |
| $FDF0   | COUT1    | Output character (no escapes)         | Yes           |
| $FE80   | SETINV   | Set inverse video                     | Yes           |
| $FE84   | SETNORM  | Set normal video                      | Yes           |
| $FE89   | SETKBD   | Set keyboard as input device          | Yes           |
| $FE93   | SETVID   | Set video as output device            | Yes           |

##### Applesoft BASIC ($D000-$F7FF)

| Address | Name     | Description                           | Safe to Trap? |
|---------|----------|---------------------------------------|---------------|
| $D365   | RESTART  | BASIC warm start                      | Yes           |
| $D683   | CHKCOM   | Check for comma                       | Yes           |
| $D6A5   | FRMNUM   | Evaluate numeric expression           | Yes           |
| $D6DA   | FRMEVL   | Evaluate expression                   | Yes           |
| $DD67   | VAR      | Get variable pointer                  | Yes           |
| $DFE3   | PRINT    | PRINT statement                       | Yes           |
| $E10C   | STROUT   | Print string                          | Yes           |
| $E2F2   | LINPRT   | Print line number                     | Yes           |
| $E752   | FOUT     | Float to string                       | Yes           |
| $EAF9   | FIN      | String to float                       | Yes           |
| $EB63   | FADD     | Floating add                          | Yes           |
| $EB90   | FSUB     | Floating subtract                     | Yes           |
| $EBA0   | LOG      | Natural log                           | Yes           |
| $EC23   | FMULT    | Floating multiply                     | Yes           |
| $ED36   | FDIV     | Floating divide                       | Yes           |

##### Disk II Controller ($C600-$C6FF, $C800-$CFFF when slot 6)

| Address | Name         | Description                       | Safe to Trap? |
|---------|--------------|-----------------------------------|---------------|
| $C600   | BOOT         | Cold boot entry                   | Slot 6 only   |
| $C620   | BOOT1        | Boot continuation                 | Slot 6 only   |
| $C800   | *varies*     | Expansion ROM base                | **Conditional** |
| $CXxx   | RWTS-related | Read/write track/sector           | **Conditional** |

**Note**: Disk II expansion ROM traps must verify `ActiveExpansionSlot == 6` before handling.

#### 8.3.11 CPU Integration

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
            // Trap handled the call
            _cycleCount += result.CyclesConsumed.Value;
            
            // Handle return semantics
            if (result.ReturnAddress is { } retAddr)
            {
                // Trap specified explicit return address
                _registers.PC.SetAddr(retAddr);
            }
            else
            {
                // Simulate RTS: pop return address from stack
                SimulateRts();
            }
            
            return new CpuStepResult(CpuRunState.Running, result.CyclesConsumed);
        }
        // Fall through to normal execution if not handled
    }
    
    // Normal instruction fetch and execution
    byte opcode = FetchOpcode();
    // ... execute instruction ...
}
```

#### 8.3.12 ROM-Free Operation Mode

For fully ROM-free operation, the emulator provides:

1. **Stub ROM**: A minimal ROM image with just vectors and trap landing pads
2. **Comprehensive traps**: Every documented ROM entry point has a native handler
3. **Fallback behavior**: Unknown addresses halt or trigger BRK

```csharp
/// <summary>
/// Stub ROM for ROM-free operation.
/// </summary>
public sealed class StubRom : IBusTarget
{
    private readonly byte[] _rom;
    
    public StubRom()
    {
        _rom = new byte[0x4000];  // 16KB ($C000-$FFFF in ROM space)
        
        // Set reset vector to trapped address
        SetVector(0xFFFC, 0xFA62);  // Standard Apple IIe reset vector
        
        // Set IRQ/BRK vector
        SetVector(0xFFFE, 0xFA40);  // Trapped BRK handler
        
        // Set NMI vector  
        SetVector(0xFFFA, 0xFA00);  // Trapped NMI handler
        
        // Fill with BRK ($00) or STP ($DB) for unimplemented routines
        // STP halts immediately; BRK allows trap handler to catch it
        Array.Fill(_rom, (byte)0xDB);  // Use STP on 65C02
        
        // Place actual trap landing addresses
        PlaceTrapLandingPads();
    }
    
    private void SetVector(ushort address, ushort value)
    {
        int offset = address - 0xC000;
        _rom[offset] = (byte)(value & 0xFF);
        _rom[offset + 1] = (byte)(value >> 8);
    }
    
    private void PlaceTrapLandingPads()
    {
        // For each trapped address, place a RTS instruction
        // The trap fires before the RTS is fetched
        foreach (var addr in TrapRegistry.GetAllAddresses())
        {
            if (addr >= 0xC000)
            {
                int offset = (int)(addr - 0xC000);
                _rom[offset] = 0x60;  // RTS
            }
        }
    }
    
    public byte Read8(Addr physicalAddress, in BusAccess access)
        => _rom[physicalAddress & 0x3FFF];
    
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        // ROM ignores writes
    }
}
```

---

## Document History

| Version | Date       | Changes                                                          |
| ------- | ---------- | ---------------------------------------------------------------- |
| 1.0     | 2025-12-26 | Initial consolidated specification                               |
| 1.1     | 2025-01-13 | Split sections IX-XII into Part 3                                |
| 1.2     | 2025-01-13 | Added sections 7.5-7.7 (Machine, CPU, Init)                      |
| 1.3     | 2025-01-13 | Added section 8.3 (ROM Trap Handlers)                            |
| 1.4     | 2025-12-28 | Major rewrite of Part VII: comprehensive Apple II I/O explanation|
|         |            | - Detailed $C000-$CFFF I/O page architecture                     |
|         |            | - Expansion ROM selection protocol explained                     |
|         |            | - Soft switch sub-regions documented                             |
|         |            | - Apple IIc differences (Section 7.8)                            |
| 1.5     | 2025-12-28 | Major rewrite of Section 8.3: ROM trapping with dynamic ROM      |
|         |            | - Trap safety classification by address range                    |
|         |            | - Expansion ROM trap context-dependency explained                |
|         |            | - Slot-dependent trap registration patterns                      |
|         |            | - Complete Monitor and Applesoft trap tables                     |