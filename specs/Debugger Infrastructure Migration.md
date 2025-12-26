# Debugger Infrastructure Migration

**Document Purpose:** Planning document for updating the console debugger to support the new bus architecture.  
**This is NOT part of the specification.** It captures the migration path for debug tooling.

**Date:** 2025-01-13  
**Depends on:** Implementation Roadmap - Bus Architecture (Phases 1-5)

---

## Current State

### Debug Infrastructure Components

The debugger lives in `BadMango.Emulator.Debug.Infrastructure` and consists of:

| Component | Purpose | Bus Dependency |
|-----------|---------|----------------|
| `IDebugContext` | Provides CPU, Memory, Disassembler to commands | Uses `IMemory` |
| `DebugContext` | Concrete implementation | Holds `IMemory` reference |
| `MachineFactory` | Creates CPU + Memory + Disassembler | Creates `BasicMemory` |
| `TracingDebugListener` | Captures instruction execution | No direct bus dependency |
| `TraceRecord` | Stores trace data | No bus dependency |
| 15 Command handlers | Debug commands (mem, poke, step, etc.) | Use `IMemory` via context |

### Current Memory Access Pattern

Commands access memory through `IDebugContext.Memory`:

```csharp
// Current pattern (MemCommand.cs, PokeCommand.cs, etc.)
if (debugContext.Memory is null)
    return CommandResult.Error("No memory attached.");

byte value = debugContext.Memory.Read(address);
debugContext.Memory.Write(address, value);
```

### Current Machine Creation

`MachineFactory.CreateSystem()` creates:
1. `BasicMemory` implementing `IMemory`
2. `Cpu65C02` taking `IMemory` in constructor
3. `Disassembler` taking `IMemory`
4. `MachineInfo` metadata

---

## Migration Goals

### Primary Goals

1. **Support both old and new systems** - During migration, debugger must work with:
   - Legacy systems using `IMemory`
   - New systems using `IMemoryBus`

2. **Expose bus capabilities** - New commands for:
   - Page table inspection
   - Bus tracing
   - Fault analysis

3. **Preserve existing commands** - All current commands continue working

### Secondary Goals

1. **Machine abstraction** - Commands interact with `IMachine`, not raw components
2. **Multi-machine support** - Debug multiple machines simultaneously
3. **Enhanced tracing** - Include bus events in trace output

---

## Migration Phases

### Phase D1: IDebugContext Extension

**Goal:** Add optional `IMemoryBus` and `IMachine` to debug context without breaking existing code.

**Changes to `IDebugContext`:**

```csharp
public interface IDebugContext : ICommandContext
{
    // Existing (kept for backward compatibility)
    ICpu? Cpu { get; }
    IMemory? Memory { get; }
    IDisassembler? Disassembler { get; }
    MachineInfo? MachineInfo { get; }
    TracingDebugListener? TracingListener { get; }
    bool IsSystemAttached { get; }
    
    // New (Phase D1)
    /// <summary>
    /// Gets the memory bus for bus-aware debugging.
    /// </summary>
    /// <remarks>
    /// When non-null, provides access to the page-based memory system
    /// including page table inspection and bus-level tracing.
    /// Legacy systems may have Memory but not Bus.
    /// </remarks>
    IMemoryBus? Bus { get; }
    
    /// <summary>
    /// Gets the machine instance for high-level machine control.
    /// </summary>
    /// <remarks>
    /// When non-null, provides access to Run/Step/Reset through the
    /// machine abstraction rather than direct CPU manipulation.
    /// </remarks>
    IMachine? Machine { get; }
    
    /// <summary>
    /// Gets whether bus-level debugging is available.
    /// </summary>
    bool IsBusAttached { get; }
}
```

**Changes to `DebugContext`:**

```csharp
public sealed class DebugContext : IDebugContext
{
    // New properties
    public IMemoryBus? Bus { get; private set; }
    public IMachine? Machine { get; private set; }
    public bool IsBusAttached => Bus is not null;
    
    // New attach methods
    public void AttachBus(IMemoryBus bus) { ... }
    public void AttachMachine(IMachine machine) { ... }
    
    // Extended AttachSystem overload
    public void AttachSystem(IMachine machine, IDisassembler disassembler)
    {
        Machine = machine;
        Cpu = machine.Cpu;
        Bus = machine.Bus;
        // Memory adapter for backward compatibility
        Memory = new MemoryBusAdapter(machine.Bus);
        Disassembler = disassembler;
    }
}
```

**Estimated effort:** Small (0.5 days)

---

### Phase D2: MemoryBusAdapter for Backward Compatibility

**Goal:** Allow existing commands to work with `IMemoryBus` through an `IMemory` adapter.

This is the same adapter from Issue 5.1 in the Bus Roadmap. The debugger reuses it.

```csharp
/// <summary>
/// Adapts IMemoryBus to IMemory for backward compatibility.
/// </summary>
public sealed class MemoryBusAdapter : IMemory
{
    private readonly IMemoryBus _bus;
    private readonly int _sourceId;
    
    public MemoryBusAdapter(IMemoryBus bus, int sourceId = 0)
    {
        _bus = bus;
        _sourceId = sourceId;
    }
    
    public byte Read(int address)
    {
        var access = new BusAccess(
            Address: (Addr)address,
            Value: 0,
            WidthBits: 8,
            Mode: CpuMode.Compat,
            EmulationE: true,
            Intent: AccessIntent.Debug,  // Debug access - no side effects
            SourceId: _sourceId,
            Cycle: 0,
            Flags: AccessFlags.NoSideFx);
        
        return _bus.Read8(access);
    }
    
    public void Write(int address, byte value)
    {
        var access = new BusAccess(
            Address: (Addr)address,
            Value: value,
            WidthBits: 8,
            Mode: CpuMode.Compat,
            EmulationE: true,
            Intent: AccessIntent.Debug,
            SourceId: _sourceId,
            Cycle: 0,
            Flags: AccessFlags.NoSideFx);
        
        _bus.Write8(access, value);
    }
    
    // Other IMemory methods...
}
```

**Key insight:** Debug accesses use `AccessIntent.Debug` and `AccessFlags.NoSideFx` to avoid triggering I/O side effects during memory inspection.

**Estimated effort:** Small (0.5 days, shared with Issue 5.1)

---

### Phase D3: MachineFactory Update

**Goal:** Support creating both legacy and new-style machines.

```csharp
public static class MachineFactory
{
    /// <summary>
    /// Creates a legacy debug system (current behavior).
    /// </summary>
    public static (ICpu, IMemory, IDisassembler, MachineInfo) CreateLegacySystem(
        MachineProfile profile) { ... }
    
    /// <summary>
    /// Creates a new bus-based machine.
    /// </summary>
    public static (IMachine, IDisassembler, MachineInfo) CreateMachine(
        MachineProfile profile)
    {
        // Use Pocket2eMachineBuilder (from Issue 4.1)
        var builder = new Pocket2eMachineBuilder();
        
        // Configure from profile
        builder.WithRamSize(profile.Memory.Size);
        // ... other configuration
        
        var machine = builder.Build();
        var disassembler = CreateDisassembler(machine);
        var info = MachineInfo.FromProfile(profile);
        
        return (machine, disassembler, info);
    }
}
```

**Estimated effort:** Small (0.5 days)

---

### Phase D4: New Debug Commands

**Goal:** Add commands that expose bus-specific functionality.

#### `pages` Command - Page Table Inspector

```csharp
/// <summary>
/// Displays page table entries for address ranges.
/// </summary>
/// <remarks>
/// Usage: pages [start] [count]
/// Examples:
///   pages           - Show all mapped pages
///   pages C000      - Show page containing $C000
///   pages C000 4    - Show 4 pages starting at $C000
/// </remarks>
public sealed class PagesCommand : CommandHandlerBase
{
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        if (context is not IDebugContext { Bus: not null } debugContext)
            return CommandResult.Error("Bus not available.");
        
        var bus = debugContext.Bus;
        
        // Parse arguments
        int startPage = args.Length > 0 ? ParseAddress(args[0]) >> 12 : 0;
        int count = args.Length > 1 ? int.Parse(args[1]) : 16;
        
        for (int i = startPage; i < startPage + count && i < bus.PageCount; i++)
        {
            var entry = bus.GetPageEntry((Addr)(i << 12));
            FormatPageEntry(debugContext.Output, i, entry);
        }
        
        return CommandResult.Ok();
    }
    
    private void FormatPageEntry(TextWriter output, int pageIndex, PageEntry entry)
    {
        // Format: Page $0C: Target=SoftSwitch  Perms=RW-  Caps=SideFx
        string perms = $"{(entry.CanRead ? 'R' : '-')}" +
                       $"{(entry.CanWrite ? 'W' : '-')}" +
                       $"{(entry.CanExecute ? 'X' : '-')}";
        
        output.WriteLine($"Page ${pageIndex:X2}: " +
                        $"Target={entry.RegionTag,-12} " +
                        $"Perms={perms} " +
                        $"PhysBase=${entry.PhysicalBase:X8}");
    }
}
```

#### `buslog` Command - Bus Trace Control

```csharp
/// <summary>
/// Controls bus-level tracing.
/// </summary>
/// <remarks>
/// Usage: buslog [on|off|show|clear]
/// Examples:
///   buslog on       - Enable bus tracing
///   buslog off      - Disable bus tracing
///   buslog show 20  - Show last 20 bus events
///   buslog clear    - Clear trace buffer
/// </remarks>
public sealed class BusLogCommand : CommandHandlerBase
{
    // Implementation depends on bus tracing infrastructure (Issue 6.2)
}
```

#### `fault` Command - Last Fault Inspector

```csharp
/// <summary>
/// Displays information about the last bus fault.
/// </summary>
/// <remarks>
/// Usage: fault
/// Shows: Address, FaultKind, Intent, and context
/// </remarks>
public sealed class FaultCommand : CommandHandlerBase
{
    // Shows last BusFault details
}
```

**Estimated effort:** Medium (2 days)

---

### Phase D5: Enhanced Tracing

**Goal:** Integrate bus events into `TracingDebugListener` output.

#### Extended TraceRecord

```csharp
public struct TraceRecord
{
    // Existing fields
    public Addr PC;
    public byte Opcode;
    public CpuInstructions Instruction;
    // ... etc
    
    // New fields (Phase D5)
    /// <summary>
    /// Memory accesses made during this instruction.
    /// </summary>
    public IReadOnlyList<BusTraceEvent>? BusEvents { get; init; }
    
    /// <summary>
    /// Bus fault that occurred, if any.
    /// </summary>
    public BusFault? Fault { get; init; }
}
```

#### Enhanced Trace Output

```
$1000: A9 01    LDA #$01       ; A=01 X=00 Y=00 SP=FF P=34 Cyc=2
$1002: 8D 30 C0 STA $C030      ; A=01 X=00 Y=00 SP=FF P=34 Cyc=6
       ?? W $C030 (SoftSwitch/Speaker) = $01
$1005: AD 00 C0 LDA $C000      ; A=8D X=00 Y=00 SP=FF P=B4 Cyc=10
       ?? R $C000 (SoftSwitch/Keyboard) ? $8D
```

**Estimated effort:** Medium (1-2 days)

---

### Phase D6: Update Existing Commands

**Goal:** Update existing commands to prefer bus when available.

#### Commands Requiring Updates

| Command | Change Required |
|---------|-----------------|
| `mem` | Use `AccessIntent.Debug` when bus available |
| `poke` | Use `AccessIntent.Debug` when bus available |
| `load` | No change (uses IMemory abstraction) |
| `save` | No change (uses IMemory abstraction) |
| `dasm` | No change (uses IDisassembler) |
| `step` | Prefer `IMachine.Step()` when available |
| `run` | Prefer `IMachine.Run()` when available |
| `reset` | Prefer `IMachine.Reset()` when available |

**Pattern for updated commands:**

```csharp
public override CommandResult Execute(ICommandContext context, string[] args)
{
    if (context is not IDebugContext debugContext)
        return CommandResult.Error("Debug context required.");
    
    // Prefer machine abstraction when available
    if (debugContext.Machine is not null)
    {
        // Use machine API
        debugContext.Machine.Step();
    }
    else if (debugContext.Cpu is not null)
    {
        // Fall back to legacy CPU API
        debugContext.Cpu.Step();
    }
    else
    {
        return CommandResult.Error("No CPU attached.");
    }
    
    return CommandResult.Ok();
}
```

**Estimated effort:** Small (1 day)

---

## Dependency Graph

```
Bus Roadmap Issues          Debugger Phases
?????????????????????       ???????????????
Issue 1.1 (MainBus) ???????? Phase D1 (IDebugContext extension)
                         ?
Issue 5.1 (Adapter) ???????? Phase D2 (MemoryBusAdapter)
                         ?
Issue 4.1 (Machine) ???????? Phase D3 (MachineFactory update)
                         ?
Issue 6.2 (Tracing) ???????? Phase D5 (Enhanced tracing)
                         ?
                         ??? Phase D4 (New commands)
                              ?
                         Phase D6 (Update existing commands)
```

**Key insight:** Phases D1-D3 can begin as soon as Issue 1.1 is complete. Phase D4 can proceed in parallel. Phases D5-D6 depend on more bus infrastructure.

---

## Summary Timeline

| Phase | Description | Dependencies | Est. Duration |
|-------|-------------|--------------|---------------|
| D1 | IDebugContext extension | Issue 1.1 | 0.5 days |
| D2 | MemoryBusAdapter | Issue 5.1 (shared) | 0.5 days |
| D3 | MachineFactory update | Issue 4.1 | 0.5 days |
| D4 | New debug commands | D1 complete | 2 days |
| D5 | Enhanced tracing | Issue 6.2, D1 | 1-2 days |
| D6 | Update existing commands | D1, D2 | 1 day |
| **Total** | | | **~6 days** |

---

## Testing Strategy

### Unit Tests

1. **Adapter tests** - Verify `MemoryBusAdapter` correctly translates operations
2. **Context tests** - Verify new properties on `IDebugContext`
3. **Command tests** - Each new command gets its own test class

### Integration Tests

1. **Legacy mode** - Existing tests continue to pass with `IMemory`
2. **Bus mode** - New tests exercise bus-based debugging
3. **Mixed mode** - Test switching between legacy and bus systems

### Manual Testing

1. **Interactive REPL** - Test all commands manually
2. **Trace output** - Verify enhanced trace format
3. **Page inspection** - Verify `pages` command output

---

## Risk Assessment

### Low Risk: Backward Compatibility

The adapter pattern ensures existing code keeps working. No breaking changes to `IDebugContext`.

### Medium Risk: Trace Performance

Enhanced tracing with bus events could impact performance. Mitigation:
- Bus event capture is optional (off by default)
- Use ring buffer with fixed capacity
- Lazy formatting (format only when displayed)

### Low Risk: Command Consistency

New commands follow existing patterns. No new concepts for users.

---

## Open Questions

### 1. Should `pages` show unmapped pages?

**Recommendation:** Yes, but mark them clearly. Useful for debugging mapping issues.

### 2. Should bus tracing be a separate listener?

**Recommendation:** Yes. Create `BusTracingListener` that attaches to the bus, separate from `TracingDebugListener` which attaches to the CPU. The debug context can hold both.

### 3. Multi-machine support scope?

**Recommendation:** Defer to Phase 7+. For now, one machine per debug session. The architecture supports multiple machines, but the REPL and commands assume one.

---

*Document last updated: 2025-01-13*
