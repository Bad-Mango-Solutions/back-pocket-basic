# Emulator Architecture Specification v1.0 - Appendix

## Appendix A: Quick Reference

### A.1 Key Types Summary

| Type          | Purpose                                |
| ------------- | -------------------------------------- |
| `BusAccess`   | Complete context for any bus operation |
| `BusResult`   | Result of bus write with fault info    |
| `BusResult<T>`| Result of bus read with value or fault |
| `BusFault`    | Complete fault information             |
| `PageEntry`   | 4KB page routing entry                 |
| `IBusTarget`  | Device that handles reads/writes       |
| `IMemoryBus`  | Main bus interface for CPU             |
| `ISignalBus`  | IRQ/NMI/Reset line management          |
| `IScheduler`  | Cycle-accurate event scheduling        |
| `IPeripheral` | Expansion card interface               |
| `IMachine`    | Assembled emulator machine interface   |
| `ITrapRegistry` | ROM routine interception registry    |
| `TrapHandler` | Native ROM routine implementation      |

### A.2 Memory Map Quick Reference

**Pocket2e (64KB / 128KB):**
```
$0000-$BFFF : RAM (48KB main)
$C000-$C0FF : Soft switches (I/O)
$C100-$CFFF : Slot ROM / Expansion
$D000-$FFFF : ROM or LC RAM
```
With auxiliary memory (128KB Apple IIe):
```
Bank 0: Main 64KB (as above)
Bank 1: Auxiliary 64KB (accessed via soft switches)
```

**PocketGS (16MB - Apple IIgs / 65C816):**
```
Bank $00:
  $00/0000-$00/BFFF : RAM (48KB, shadowed to Bank $E0-$E1)
  $00/C000-$00/C0FF : Soft switches (I/O)
  $00/C100-$00/CFFF : Slot ROM / Expansion ROM
  $00/D000-$00/FFFF : ROM or LC RAM (Bank 1/2)

Bank $01:
  $01/0000-$01/FFFF : Auxiliary memory (64KB)

Banks $02-$7F:
  Expansion RAM (up to 8MB)
  $02/0000-$7F/FFFF : Additional RAM banks

Banks $80-$DF:
  Reserved / Expansion

Banks $E0-$E1:
  $E0/0000-$E1/FFFF : Mega II shadowed RAM (128KB)
  - Text pages, graphics pages shadowed here
  - Super Hi-Res at $E1/2000-$E1/9FFF

Bank $FE:
  $FE/0000-$FE/FFFF : ROM (Toolbox, etc.)

Bank $FF:
  $FF/0000-$FF/FFFF : ROM (Monitor, Applesoft, etc.)
  $FF/FC00-$FF/FFFF : Vectors and ROM entry points
```

Key IIgs Memory Regions:
| Address           | Size   | Description                        |
|-------------------|--------|------------------------------------|
| $00/0000-$00/00FF | 256B   | Direct Page (Zero Page)            |
| $00/0100-$00/01FF | 256B   | Stack                              |
| $00/0400-$00/07FF | 1KB    | Text Page 1 / Lo-Res 1             |
| $00/2000-$00/3FFF | 8KB    | Hi-Res Page 1                      |
| $00/4000-$00/5FFF | 8KB    | Hi-Res Page 2                      |
| $E1/2000-$E1/9FFF | 32KB   | Super Hi-Res Graphics              |
| $E1/9E00-$E1/9FFF | 512B   | Super Hi-Res SCBs                  |
| $E1/9D00-$E1/9DFF | 256B   | Super Hi-Res Color Palettes        |

**PocketME (4GB - 65832):**
```
$00000000-$0003FFFF : Boot ROM (256KB)
$00040000-...        : RAM
$FFFC0000-$FFFFFFFF : High ROM alias
```

### A.3 Soft Switch Summary (Pocket2e)

The $C000-$C0FF I/O page contains memory-mapped soft switches for hardware control.

#### Keyboard ($C000-$C01F)

| Address | Name      | R/W | Function                              |
|---------|-----------|-----|---------------------------------------|
| $C000   | KBD       | R   | Keyboard data (bit 7 = key available) |
| $C010   | KBDSTRB   | R/W | Clear keyboard strobe (any access)    |

#### Cassette (legacy) ($C020-$C02F)

| Address | Name      | R/W | Function                              |
|---------|-----------|-----|---------------------------------------|
| $C020   | TAPEOUT   | W   | Toggle cassette output                |

#### Speaker ($C030-$C03F)

| Address | Name      | R/W | Function                              |
|---------|-----------|-----|---------------------------------------|
| $C030   | SPKR      | R/W | Toggle speaker (any access clicks)    |

#### Utility Strobes ($C040-$C04F)

| Address | Name      | R/W | Function                              |
|---------|-----------|-----|---------------------------------------|
| $C040   | STROBE    | R/W | Game I/O strobe                       |

#### Graphics Mode Switches ($C050-$C05F)

| Address | Name      | R/W | Function                              | State    |
|---------|-----------|-----|---------------------------------------|----------|
| $C050   | TXTCLR    | R/W | Enable graphics mode                  | GR on    |
| $C051   | TXTSET    | R/W | Enable text mode                      | TEXT on  |
| $C052   | MIXCLR    | R/W | Full screen (no text window)          | FULL     |
| $C053   | MIXSET    | R/W | Mixed mode (4 lines text at bottom)   | MIXED    |
| $C054   | LOWSCR    | R/W | Display page 1                        | PAGE1    |
| $C055   | HISCR     | R/W | Display page 2                        | PAGE2    |
| $C056   | LORES     | R/W | Select lo-res graphics                | LORES    |
| $C057   | HIRES     | R/W | Select hi-res graphics                | HIRES    |

#### Annunciator Outputs ($C058-$C05F)

| Address | Name      | R/W | Function                              |
|---------|-----------|-----|---------------------------------------|
| $C058   | AN0OFF    | R/W | Annunciator 0 off                     |
| $C059   | AN0ON     | R/W | Annunciator 0 on                      |
| $C05A   | AN1OFF    | R/W | Annunciator 1 off                     |
| $C05B   | AN1ON     | R/W | Annunciator 1 on                      |
| $C05C   | AN2OFF    | R/W | Annunciator 2 off                     |
| $C05D   | AN2ON     | R/W | Annunciator 2 on                      |
| $C05E   | AN3OFF    | R/W | Annunciator 3 off (also DHIRES off)   |
| $C05F   | AN3ON     | R/W | Annunciator 3 on (also DHIRES on)     |

#### Pushbutton / Joystick Inputs ($C060-$C06F)

| Address | Name      | R/W | Function                              |
|---------|-----------|-----|---------------------------------------|
| $C060   | TAPEIN    | R   | Cassette input (legacy)               |
| $C061   | PB0       | R   | Pushbutton 0 / Open-Apple (bit 7)     |
| $C062   | PB1       | R   | Pushbutton 1 / Solid-Apple (bit 7)    |
| $C063   | PB2       | R   | Pushbutton 2 / Shift key (bit 7)      |
| $C064   | PADDL0    | R   | Paddle 0 analog input                 |
| $C065   | PADDL1    | R   | Paddle 1 analog input                 |
| $C066   | PADDL2    | R   | Paddle 2 analog input                 |
| $C067   | PADDL3    | R   | Paddle 3 analog input                 |

#### Paddle Trigger ($C070-$C07F)

| Address | Name      | R/W | Function                              |
|---------|-----------|-----|---------------------------------------|
| $C070   | PTRIG     | R/W | Trigger paddle timers (any access)    |

#### Language Card / Bank Switching ($C080-$C08F)

| Address | Name        | R/W | Function                                |
|---------|-------------|-----|-----------------------------------------|
| $C080   | RDRAM       | R   | Read RAM, no write; bank 2              |
| $C081   | ROMIN       | R×2 | Read ROM, write RAM; bank 2             |
| $C082   | RDROM       | R   | Read ROM, no write; bank 2              |
| $C083   | RWRAM       | R×2 | Read/write RAM; bank 2                  |
| $C084   | RDRAM       | R   | Read RAM, no write; bank 2 (alias)      |
| $C085   | ROMIN       | R×2 | Read ROM, write RAM; bank 2 (alias)     |
| $C086   | RDROM       | R   | Read ROM, no write; bank 2 (alias)      |
| $C087   | RWRAM       | R×2 | Read/write RAM; bank 2 (alias)          |
| $C088   | RDRAM       | R   | Read RAM, no write; bank 1              |
| $C089   | ROMIN       | R×2 | Read ROM, write RAM; bank 1             |
| $C08A   | RDROM       | R   | Read ROM, no write; bank 1              |
| $C08B   | RWRAM       | R×2 | Read/write RAM; bank 1                  |
| $C08C   | RDRAM       | R   | Read RAM, no write; bank 1 (alias)      |
| $C08D   | ROMIN       | R×2 | Read ROM, write RAM; bank 1 (alias)     |
| $C08E   | RDROM       | R   | Read ROM, no write; bank 1 (alias)      |
| $C08F   | RWRAM       | R×2 | Read/write RAM; bank 1 (alias)          |

**Note:** R×2 means the switch requires two consecutive reads to enable write.

#### Slot I/O Space ($C090-$C0FF)

Each slot has 16 bytes of device-specific I/O:

| Address Range | Slot | Description                           |
|---------------|------|---------------------------------------|
| $C090-$C09F   | 1    | Slot 1 device I/O                     |
| $C0A0-$C0AF   | 2    | Slot 2 device I/O                     |
| $C0B0-$C0BF   | 3    | Slot 3 device I/O                     |
| $C0C0-$C0CF   | 4    | Slot 4 device I/O                     |
| $C0D0-$C0DF   | 5    | Slot 5 device I/O                     |
| $C0E0-$C0EF   | 6    | Slot 6 device I/O (typically Disk II) |
| $C0F0-$C0FF   | 7    | Slot 7 device I/O                     |

**Common Slot 6 (Disk II) Registers:**

| Offset | Name      | Function                              |
|--------|-----------|---------------------------------------|
| +$00   | PH0OFF    | Stepper phase 0 off                   |
| +$01   | PH0ON     | Stepper phase 0 on                    |
| +$02   | PH1OFF    | Stepper phase 1 off                   |
| +$03   | PH1ON     | Stepper phase 1 on                    |
| +$04   | PH2OFF    | Stepper phase 2 off                   |
| +$05   | PH2ON     | Stepper phase 2 on                    |
| +$06   | PH3OFF    | Stepper phase 3 off                   |
| +$07   | PH3ON     | Stepper phase 3 on                    |
| +$08   | MOTOROFF  | Drive motor off                       |
| +$09   | MOTORON   | Drive motor on                        |
| +$0A   | DRV0EN    | Select drive 1                        |
| +$0B   | DRV1EN    | Select drive 2                        |
| +$0C   | Q6L       | Read/sense data latch                 |
| +$0D   | Q6H       | Read/sense write protect              |
| +$0E   | Q7L       | Read mode                             |
| +$0F   | Q7H       | Write mode                            |

#### Apple IIe Auxiliary Memory Switches (80-column/Double Hi-Res)

| Address | Name      | R/W | Function                              |
|---------|-----------|-----|---------------------------------------|
| $C000   | 80STOREOFF| W   | Disable 80STORE mode                  |
| $C001   | 80STOREON | W   | Enable 80STORE (PAGE2 selects aux)    |
| $C002   | RDMAINRAM | W   | Read from main 48K                    |
| $C003   | RDAUXRAM  | W   | Read from auxiliary 48K               |
| $C004   | WRMAINRAM | W   | Write to main 48K                     |
| $C005   | WRAUXRAM  | W   | Write to auxiliary 48K                |
| $C006   | SETSLOTCX | W   | Slot ROM at $C100-$CFFF               |
| $C007   | SETINTCX  | W   | Internal ROM at $C100-$CFFF           |
| $C008   | SETSTDZP  | W   | Main zero page and stack              |
| $C009   | SETALTZP  | W   | Auxiliary zero page and stack         |
| $C00A   | SETINTC3  | W   | Internal ROM at $C300                 |
| $C00B   | SETSLOTC3 | W   | Slot ROM at $C300                     |
| $C00C   | 80COLOFF  | W   | Disable 80-column mode                |
| $C00D   | 80COLON   | W   | Enable 80-column mode                 |
| $C00E   | ALTCHAROFF| W   | Primary character set                 |
| $C00F   | ALTCHARON | W   | Alternate character set (MouseText)   |

#### Apple IIe Status Reads

| Address | Name      | R   | Function (bit 7 = status)             |
|---------|-----------|-----|---------------------------------------|
| $C011   | RDLCBNK2  | R   | Language card bank 2 selected?        |
| $C012   | RDLCRAM   | R   | Reading language card RAM?            |
| $C013   | RDRAMRD   | R   | Reading from auxiliary RAM?           |
| $C014   | RDRAMWRT  | R   | Writing to auxiliary RAM?             |
| $C015   | RDCXROM   | R   | Using internal slot ROM?              |
| $C016   | RDALTZP   | R   | Using auxiliary zero page?            |
| $C017   | RDC3ROM   | R   | Using internal C3 ROM?                |
| $C018   | RD80STORE | R   | 80STORE enabled?                      |
| $C019   | RDVBLBAR  | R   | Vertical blanking? (bit 7 low = VBL)  |
| $C01A   | RDTEXT    | R   | Text mode enabled?                    |
| $C01B   | RDMIXED   | R   | Mixed mode enabled?                   |
| $C01C   | RDPAGE2   | R   | Page 2 displayed?                     |
| $C01D   | RDHIRES   | R   | Hi-res mode enabled?                  |
| $C01E   | RDALTCHAR | R   | Alternate character set enabled?      |
| $C01F   | RD80COL   | R   | 80-column mode enabled?               |

---

## Appendix B: Building a Pocket2e Machine

This appendix provides a complete example of assembling and starting a Pocket2e (Apple IIe-class)
emulator from the architectural components defined in this specification.

### B.1 Overview

Building a runnable emulator involves these phases:

1. **Create core infrastructure** (registry, scheduler, signal bus)
2. **Create memory bus** with page table
3. **Create and register devices** (RAM, ROM, I/O)
4. **Wire devices to bus** via page mappings
5. **Create CPU** and connect to bus/signals
6. **Load ROM image**
7. **Reset and run**

### B.2 Complete Build Example

```csharp
/// <summary>
/// Factory for building a complete Pocket2e machine.
/// </summary>
public sealed class Pocket2eMachineBuilder
{
    // Configuration
    private byte[]? _romImage;
    private readonly List<(int slot, IPeripheral card)> _slotCards = [];
    
    /// <summary>
    /// Sets the ROM image (typically 16KB Applesoft + Monitor).
    /// </summary>
    public Pocket2eMachineBuilder WithRom(byte[] romImage)
    {
        _romImage = romImage;
        return this;
    }
    
    /// <summary>
    /// Installs a peripheral card in the specified slot (1-7).
    /// </summary>
    public Pocket2eMachineBuilder WithSlotCard(int slot, IPeripheral card)
    {
        _slotCards.Add((slot, card));
        return this;
    }
    
    /// <summary>
    /// Builds and returns the complete machine.
    /// </summary>
    public IPocket2eMachine Build()
    {
        ArgumentNullException.ThrowIfNull(_romImage);
        
        // ??? Phase 1: Core Infrastructure ???????????????????????????????
        var registry = new DeviceRegistry();
        var scheduler = new Scheduler();
        var signals = new SignalBus();
        
        // ??? Phase 2: Memory Bus ????????????????????????????????????????
        var bus = new MainBus(addressSpaceBits: 16);  // 64KB address space
        
        // ??? Phase 3: Create Devices ????????????????????????????????????
        
        // Main RAM (48KB: $0000-$BFFF)
        int ramId = registry.GenerateId();
        registry.Register(ramId, "Ram", "Main 48KB RAM", "main/ram");
        var mainRam = new RamDevice(size: 0xC000);  // 48KB
        
        // Language Card RAM (16KB: $D000-$FFFF, bank-switched)
        int lcRamId = registry.GenerateId();
        registry.Register(lcRamId, "Ram", "Language Card RAM", "main/lcram");
        var lcRam = new LanguageCardRam(size: 0x4000);  // 16KB (2x 4KB banks + 8KB)
        
        // ROM (16KB: $D000-$FFFF when selected)
        int romId = registry.GenerateId();
        registry.Register(romId, "Rom", "System ROM", "main/rom");
        var rom = new RomDevice(_romImage);
        
        // Soft Switch / I/O Page ($C000-$C0FF)
        int ioPageId = registry.GenerateId();
        registry.Register(ioPageId, "SoftSwitch", "I/O Page", "main/io");
        var softSwitches = new AppleIISoftSwitchPage(lcRam);  // [GAP: needs video, keyboard, etc.]
        
        // Slot Manager
        var slotManager = new SlotManager();
        
        // ??? Phase 4: Wire Devices to Bus ???????????????????????????????
        
        // Map main RAM: $0000-$BFFF (12 pages × 4KB = 48KB)
        bus.MapPageRange(
            startPage: 0x00,
            pageCount: 12,      // Pages 0-11 ($0000-$BFFF)
            deviceId: ramId,
            tag: RegionTag.MainRAM,
            perms: PagePerms.RWX,
            caps: TargetCaps.SupportsPeek | TargetCaps.SupportsWide,
            target: mainRam,
            physBase: 0);
        
        // Map I/O page: $C000-$C0FF (part of page 12)
        // [GAP: Need composite page support for partial-page mapping]
        bus.MapPage(
            pageIndex: 0x0C,    // Page 12 ($C000-$CFFF)
            new PageEntry(
                DeviceId: ioPageId,
                RegionTag: RegionTag.IOPage,
                Perms: PagePerms.RW,  // No execute on I/O
                Caps: TargetCaps.SideEffects,
                Target: softSwitches,  // [GAP: Composite page dispatches internally]
                PhysBase: 0));
        
        // Map ROM: $D000-$FFFF (pages 13-15, 12KB)
        // [GAP: Language card bank switching changes these mappings dynamically]
        bus.MapPageRange(
            startPage: 0x0D,
            pageCount: 3,       // Pages 13-15 ($D000-$FFFF)
            deviceId: romId,
            tag: RegionTag.ROM,
            perms: PagePerms.RX,  // Read + Execute, no write
            caps: TargetCaps.SupportsPeek,
            target: rom,
            physBase: 0);
        
        // Install slot cards
        foreach (var (slot, card) in _slotCards)
        {
            slotManager.Install(slot, card);
            
            // Register the card
            int cardId = registry.GenerateId();
            registry.Register(cardId, card.DeviceType, card.Name, $"main/slots/{slot}");
            
            // [GAP: Wire card's MMIO ($C0n0-$C0nF) and ROM ($Cn00-$CnFF) regions]
        }
        
        // ??? Phase 5: Create CPU ????????????????????????????????????????
        
        var cpu = new Cpu65C02(bus, signals);
        
        // ??? Phase 6: Initialize Event Context ??????????????????????????
        
        var eventContext = new EventContext(scheduler, signals, bus);
        
        // Initialize devices that need scheduler access
        softSwitches.Initialize(eventContext);
        foreach (var (_, card) in _slotCards)
        {
            card.Initialize(eventContext);
        }
        
        // ??? Phase 7: Assemble Machine ??????????????????????????????????
        
        return new Pocket2eMachine(
            cpu: cpu,
            bus: bus,
            signals: signals,
            scheduler: scheduler,
            registry: registry,
            slotManager: slotManager);
    }
}
```

### B.3 Machine Interface and Reset Sequence

```csharp
/// <summary>
/// Interface for a runnable Pocket2e machine.
/// </summary>
public interface IPocket2eMachine
{
    ICpu Cpu { get; }
    IMemoryBus Bus { get; }
    ISignalBus Signals { get; }
    IScheduler Scheduler { get; }
    IDeviceRegistry Registry { get; }
    
    /// <summary>
    /// Performs a hard reset: initializes all devices and loads reset vector.
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Runs the machine until stopped or breakpoint hit.
    /// </summary>
    void Run();
    
    /// <summary>
    /// Executes a single CPU instruction.
    /// </summary>
    CpuStepResult Step();
    
    /// <summary>
    /// Requests the machine to stop at the next safe point.
    /// </summary>
    void RequestStop();
}

/// <summary>
/// Implementation of the Pocket2e machine.
/// </summary>
public sealed class Pocket2eMachine : IPocket2eMachine
{
    public ICpu Cpu { get; }
    public IMemoryBus Bus { get; }
    public ISignalBus Signals { get; }
    public IScheduler Scheduler { get; }
    public IDeviceRegistry Registry { get; }
    
    private volatile bool _stopRequested;
    
    public void Reset()
    {
        // 1. Assert reset line to all devices
        Signals.Assert(SignalLine.Reset, deviceId: 0);  // CPU is device 0
        
        // 2. Reset all registered devices
        Scheduler.Reset();
        
        // 3. Reset CPU (clears registers, sets initial state)
        Cpu.Reset();
        
        // 4. Read reset vector from $FFFC-$FFFD
        var accessLow = new BusAccess(
            Address: 0xFFFC,
            Value: 0,
            WidthBits: 8,
            Mode: CpuMode.Compat,
            EmulationE: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
        
        var accessHigh = accessLow with { Address = 0xFFFD };
        
        byte pcLow = Bus.Read8(accessLow);
        byte pcHigh = Bus.Read8(accessHigh);
        ushort resetVector = (ushort)(pcLow | (pcHigh << 8));
        
        // 5. Set PC to reset vector
        Cpu.SetPC(resetVector);
        
        // 6. Deassert reset line
        Signals.Deassert(SignalLine.Reset, deviceId: 0);
        
        // 7. Reset scheduler to cycle 0
        Scheduler.Reset();
    }
    
    public void Run()
    {
        _stopRequested = false;
        
        while (!_stopRequested && !Cpu.Halted)
        {
            var result = Step();
            
            if (result.State == CpuRunState.WaitingForInterrupt)
            {
                // Fast-forward to next scheduled event
                if (!Scheduler.JumpToNextEventAndDispatch())
                {
                    // No events pending and CPU waiting - machine is idle
                    break;
                }
            }
        }
    }
    
    public CpuStepResult Step()
    {
        // 1. Execute one CPU instruction
        var result = Cpu.Step();
        
        // 2. Advance scheduler to next event
        Scheduler.Advance(result.CyclesConsumed);
        
        // 3. Dispatch any due events
        Scheduler.DispatchDue();
        
        return result;
    }
    
    public void RequestStop()
    {
        _stopRequested = true;
        Cpu.RequestStop();
    }
}
```

### B.4 Usage Example

```csharp
// Build the machine
var machine = new Pocket2eMachineBuilder()
    .WithRom(File.ReadAllBytes("apple2e.rom"))
    .WithSlotCard(6, new DiskIIController())
    .Build();

// Reset loads the reset vector into PC
machine.Reset();

// PC now contains the address from $FFFC-$FFFD (typically $FA62 for Apple IIe)
Console.WriteLine($"Reset vector: ${machine.Cpu.GetPC():X4}");

// Run until stopped
machine.Run();
```

### B.5 Identified Specification Gaps

The following gaps were identified while developing this example.
**All gaps have been resolved in the specification.**

#### Gap 1: Composite Page Dispatch ? RESOLVED

**Problem:** The I/O page ($C000-$CFFF) contains multiple sub-regions:
- $C000-$C0FF: Soft switches
- $C100-$C7FF: Slot ROM ($Cn00 for slot n)
- $C800-$CFFF: Expansion ROM (selected slot)

**Resolution:** Added `ICompositeTarget` interface in Part 1, Section 4.6:
```csharp
public interface ICompositeTarget : IBusTarget
{
    IBusTarget? ResolveTarget(Addr offset, AccessIntent intent);
    RegionTag GetSubRegionTag(Addr offset);
}
```

#### Gap 2: Dynamic Page Remapping (Bank Switching) ? RESOLVED

**Problem:** Language card and auxiliary memory switching change which physical memory
backs virtual addresses $D000-$FFFF dynamically based on soft switch state.

**Resolution:** Added dynamic remapping methods to `IMemoryBus` in Part 1, Section 4.7:
```csharp
void RemapPage(int pageIndex, IBusTarget newTarget, Addr newPhysBase);
void RemapPage(int pageIndex, PageEntry newEntry);
void RemapPageRange(int startPage, int pageCount, IBusTarget newTarget, Addr newPhysBase);
```

#### Gap 3: CPU Interface for PC Access ? NOT NEEDED

**Problem:** Reset sequence needs to set PC after reading reset vector.

**Resolution:** The CPU's `Reset()` method already handles reading the reset vector
and setting PC internally. No additional API is needed.

#### Gap 4: Scheduler Reset ? RESOLVED

**Problem:** On machine reset, scheduler should clear pending events and reset cycle counter.

**Resolution:** Added `Reset()` method to `IScheduler` in Part 2, Section 6.2:
```csharp
void Reset();  // Resets to cycle 0 and cancels all pending events
int PendingEventCount { get; }  // For diagnostics
```

#### Gap 5: Machine-Level Abstraction ? RESOLVED

**Problem:** No defined interface for the assembled machine as a whole.

**Resolution:** Added `IMachine` base interface and `IPocket2Machine` in Part 2, Section 7.5:
```csharp
public interface IMachine
{
    ICpu Cpu { get; }
    IMemoryBus Bus { get; }
    ISignalBus Signals { get; }
    IScheduler Scheduler { get; }
    IDeviceRegistry Registry { get; }
    MachineState State { get; }
    
    void Reset();
    void Run();
    CpuStepResult Step();
    void RequestStop();
}

public interface IPocket2Machine : IMachine
{
    ISlotManager Slots { get; }
    IVideoController Video { get; }
    IKeyboard Keyboard { get; }
    ISpeaker Speaker { get; }
}
```

#### Gap 6: Device Initialization Order ? RESOLVED

**Problem:** Some devices need access to the scheduler/event context before the machine runs.

**Resolution:** Documented the initialization sequence in Part 2, Section 7.7:
1. Create infrastructure (registry, scheduler, signals)
2. Create memory bus
3. Create devices
4. Wire devices to bus
5. Create CPU
6. Create event context
7. Initialize devices with event context
8. Assemble machine

Also added `EventContext` implementation.

#### Gap 7: CPU Construction Parameters ? RESOLVED

**Problem:** CPU needs references to bus and signal bus at construction time.

**Resolution:** Documented CPU factory pattern in Part 2, Section 7.6:
```csharp
public interface ICpuFactory
{
    CpuFamily Family { get; }
    ICpu Create(IMemoryBus bus, ISignalBus signals);
}

// Constructor pattern:
public Cpu65C02(IMemoryBus bus, ISignalBus signals);
```

#### Gap 8: Stop/Halt Coordination ? RESOLVED

**Problem:** Both CPU and machine need coordinated stop mechanisms.

**Resolution:** Added stop coordination to `ICpu` in Part 2, Section 6.3:
```csharp
void RequestStop();
void ClearStopRequest();
bool IsStopRequested { get; }
```

---

## Document History

| Version | Date       | Changes                                  |
| ------- | ---------- | ---------------------------------------- |
| 1.0     | 2025-12-26 | Initial consolidated specification       |
| 1.1     | 2025-01-13 | Added complete soft switch reference A.3 |
| 1.2     | 2025-01-13 | Added Appendix B: Machine building example and gap analysis |
| 1.3     | 2025-01-13 | Resolved all specification gaps (B.5) |

---

This appendix provides quick reference tables for the Architecture Specification.