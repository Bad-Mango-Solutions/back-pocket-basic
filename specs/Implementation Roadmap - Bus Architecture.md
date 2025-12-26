# Implementation Roadmap: Bus Architecture

**Document Purpose:** Planning document for implementing the page-based bus architecture.  
**This is NOT part of the specification.** It captures the breakdown of work into PR-ready issues.

**Date:** 2025-12-26  
**Branch:** jpactor/spec-revision

---

## Current State Assessment

### What Exists

The `BadMango.Emulator.Bus` project has a solid foundation of **interfaces and types**:

| Component | Status | Notes |
|-----------|--------|-------|
| `IMemoryBus` | ✅ Interface defined | Full API per spec, no implementation |
| `IBusTarget` | ✅ Interface defined | With default wide-access implementations |
| `BusAccess` | ✅ Struct defined | Complete context type |
| `BusFault` | ✅ Struct defined | Fault model implemented |
| `BusResult<T>` | ✅ Struct defined | Result types for try-style APIs |
| `PageEntry` | ✅ Struct defined | Includes privilege levels |
| `PagePerms` | ✅ Enum defined | R/W/X flags |
| `TargetCaps` | ✅ Enum defined | SupportsPeek, SupportsWide, etc. |
| `FaultKind` | ✅ Enum defined | Unmapped, Permission, Nx, etc. |
| `AccessIntent` | ✅ Enum defined | DataRead, DataWrite, Fetch, Debug |
| `AccessFlags` | ✅ Enum defined | Decompose, Atomic, NoSideFx |
| `ISignalBus` | ✅ Interface + impl | `SignalBus` exists |
| `IScheduler` | ✅ Interface defined | No implementation yet |
| `IDeviceRegistry` | ✅ Interface defined | `DeviceRegistry` exists |
| `DeviceInfo` | ✅ Struct defined | Basic metadata |
| `RamTarget` | ✅ Class exists | Simple RAM implementation |
| `RomTarget` | ✅ Class exists | Simple ROM implementation |

### What's Missing

| Component | Status | Priority |
|-----------|--------|----------|
| `MainBus` implementation | ❌ Not started | **Critical** |
| `ICompositeTarget` interface | ❌ Not in spec impl | High |
| Dynamic remapping APIs | ❌ Not in current interface | High |
| `Scheduler` implementation | ❌ Not started | High |
| `ITrapRegistry` | ❌ Not started | Medium |
| CPU integration with bus | ❌ CPU uses `IMemory` | **Critical** |
| Machine builder pattern | ❌ Not started | Medium |

### Existing CPU Architecture

The `Cpu65C02` class currently uses:
- `IMemory` interface (simple read/write)
- Direct memory access without page translation
- No bus fault handling
- No try-style APIs

This will need significant refactoring to use `IMemoryBus`.

---

## Phased Implementation Plan

### Guiding Principles

1. **Each PR must leave the build green** - No broken intermediate states
2. **New code alongside old** - Don't break existing CPU until ready to switch
3. **Interface-first** - Define contracts, then implement
4. **Test-driven** - Each PR includes tests for new functionality
5. **Incremental integration** - CPU migration happens last

---

## Phase 1: Bus Infrastructure (Foundation)

### Issue 1.1: MainBus Implementation

**Goal:** Implement `IMemoryBus` with page table routing.

**Deliverables:**
- [ ] `MainBus` class implementing `IMemoryBus`
- [ ] Page table array with O(1) lookup
- [ ] Read8/Write8 direct methods
- [ ] TryRead8/TryWrite8 with permission checks
- [ ] MapPage/MapPageRange control plane
- [ ] Unit tests for all routing scenarios

**Does NOT include:**
- Wide access (Read16/32) - separate issue
- Cross-page decomposition - separate issue
- Tracing/observability - separate issue

**Acceptance Criteria:**
- Can map RAM pages and read/write through bus
- Permission violations return correct faults
- Unmapped pages return Unmapped fault
- All existing tests still pass

**Estimated Size:** Medium (2-3 days)

---

### Issue 1.2: Wide Access and Decomposition

**Goal:** Implement 16-bit and 32-bit access with smart decomposition.

**Depends on:** Issue 1.1

**Deliverables:**
- [ ] Read16/Write16 with atomic vs decomposed logic
- [ ] Read32/Write32 with atomic vs decomposed logic
- [ ] Cross-page boundary detection
- [ ] AccessFlags.Atomic and AccessFlags.Decompose handling
- [ ] TryRead16/TryWrite16/TryRead32/TryWrite32
- [ ] Unit tests for all combinations

**Acceptance Criteria:**
- Cross-page access correctly decomposes
- Atomic flag uses wide target method when supported
- Decompose flag forces byte-wise access
- Compat mode defaults to decompose

**Estimated Size:** Medium (2 days)

---

### Issue 1.3: Dynamic Remapping API

**Goal:** Add page remapping for bank switching support.

**Depends on:** Issue 1.1

**Deliverables:**
- [ ] `RemapPage(pageIndex, newTarget, newPhysBase)` method
- [ ] `RemapPage(pageIndex, newEntry)` method
- [ ] `RemapPageRange` method
- [ ] `GetPageEntryByIndex(pageIndex)` for inspection
- [ ] Unit tests for remapping scenarios

**Acceptance Criteria:**
- Can remap page to different target at runtime
- Remapping is atomic (no torn reads)
- Existing mappings preserved when remapping others

**Estimated Size:** Small (1 day)

---

### Issue 1.4: Composite Target Interface

**Goal:** Support pages with multiple sub-regions (I/O page pattern).

**Depends on:** Issue 1.1

**Deliverables:**
- [ ] `ICompositeTarget` interface
- [ ] `ResolveTarget(offset, intent)` method
- [ ] `GetSubRegionTag(offset)` method
- [ ] Update MainBus to detect and use composite targets
- [ ] Unit tests with mock composite target

**Acceptance Criteria:**
- MainBus delegates to sub-target based on offset
- Sub-region tag available for tracing
- Normal targets still work unchanged

**Estimated Size:** Small (1 day)

---

## Phase 2: Scheduler and Timing

### Issue 2.1: Scheduler Implementation

**Goal:** Implement the cycle-accurate event scheduler.

**Deliverables:**
- [ ] `Scheduler` class implementing `IScheduler`
- [ ] Priority queue for events (by cycle, then sequence)
- [ ] `Schedule`, `ScheduleAfter` methods
- [ ] `Drain`, `RunUntil` methods
- [ ] `Cancel` method
- [ ] `Reset` method (per spec gap resolution)
- [ ] `PendingEventCount` property
- [ ] Unit tests for ordering and dispatch

**Acceptance Criteria:**
- Events dispatch in deterministic order
- Same inputs produce same event sequence
- Cancel removes pending events
- Reset clears all events and resets cycle

**Estimated Size:** Medium (2 days)

---

### Issue 2.2: Event Context and Device Integration

**Goal:** Create the event context plumbing for device initialization.

**Depends on:** Issues 1.1, 2.1

**Deliverables:**
- [ ] `IEventContext` interface (if not present)
- [ ] `EventContext` implementation
- [ ] `IScheduledDevice` interface
- [ ] Update device initialization pattern
- [ ] Integration tests

**Acceptance Criteria:**
- Devices can schedule events during initialization
- Event context provides access to scheduler, signals, bus
- Initialization order documented and enforced

**Estimated Size:** Small (1 day)

---

## Phase 3: Device Infrastructure

### Issue 3.1: Enhanced Device Registry

**Goal:** Support structured device IDs per spec section 9.2-9.3.

**Deliverables:**
- [ ] `DevicePageClass` enum
- [ ] `DevicePageId` struct with Class/Instance/Page encoding
- [ ] Update `DeviceInfo` with PageId support
- [ ] Update `IDeviceRegistry` with new methods
- [ ] `GetByClass`, `TryGetByPageId` methods
- [ ] Unit tests

**Acceptance Criteria:**
- Can register devices with structured IDs
- Can look up by class or page ID
- Backward compatible with simple int IDs

**Estimated Size:** Small (1 day)

---

### Issue 3.2: Peripheral and Slot Manager Interfaces

**Goal:** Define interfaces for Apple II peripheral architecture.

**Deliverables:**
- [ ] `IPeripheral` interface
- [ ] `ISlotManager` interface
- [ ] Slot constants (1-7, I/O offsets)
- [ ] Unit tests for slot management

**Does NOT include:**
- Actual peripheral implementations
- Soft switch page (separate issue)

**Acceptance Criteria:**
- Can install/remove peripherals in slots
- Slot selection for expansion ROM works
- Reset propagates to all peripherals

**Estimated Size:** Small (1 day)

---

### Issue 3.3: Soft Switch Composite Page

**Goal:** Implement the $C000-$CFFF I/O page handler.

**Depends on:** Issues 1.4, 3.2

**Deliverables:**
- [ ] `Pocket2eSoftSwitchPage` implementing `ICompositeTarget`
- [ ] Sub-region dispatch for soft switches, slot I/O, slot ROM
- [ ] Stub implementations for video, keyboard, speaker interfaces
- [ ] Unit tests for dispatch logic

**Does NOT include:**
- Actual video/keyboard/speaker implementations
- Language card bank switching logic

**Acceptance Criteria:**
- Correctly routes $C000-$C0FF to soft switches
- Routes $C100-$C7FF to slot ROM regions
- Routes $C800-$CFFF to expansion ROM

**Estimated Size:** Medium (2 days)

---

## Phase 4: Machine Assembly

### Issue 4.1: Machine Interface and Builder

**Goal:** Implement IMachine and builder pattern per spec.

**Depends on:** Phases 1-3

**Deliverables:**
- [ ] `IMachine` interface
- [ ] `IPocket2Machine` interface
- [ ] `MachineState` enum
- [ ] `Pocket2eMachineBuilder` class
- [ ] Integration tests for machine assembly

**Acceptance Criteria:**
- Can build a Pocket2e machine with builder
- Reset/Run/Step lifecycle works
- State transitions raise events

**Estimated Size:** Medium (2-3 days)

---

## Phase 5: CPU Migration (The Big One)

### Issue 5.1: IMemory to IMemoryBus Adapter

**Goal:** Create an adapter to allow existing CPU to use new bus.

**This is a bridge strategy** - allows incremental migration.

**Deliverables:**
- [ ] `MemoryBusAdapter : IMemory` that wraps `IMemoryBus`
- [ ] Creates appropriate `BusAccess` for each operation
- [ ] Translates faults to existing error handling
- [ ] Tests prove existing CPU works through adapter

**Acceptance Criteria:**
- Existing Cpu65C02 works unchanged through adapter
- All existing CPU tests pass
- No behavior changes visible to CPU

**Estimated Size:** Small (1 day)

---

### Issue 5.2: CPU Bus Integration (Direct)

**Goal:** Update Cpu65C02 to use IMemoryBus directly.

**Depends on:** Issue 5.1 proving adapter works

**Deliverables:**
- [ ] Cpu65C02 constructor accepts `IMemoryBus`
- [ ] Create `BusAccess` for each memory operation
- [ ] Handle `BusFault` results appropriately
- [ ] Update instruction handlers to use bus
- [ ] All existing tests updated and passing

**This is the largest single issue.**

**Acceptance Criteria:**
- CPU uses bus for all memory access
- Faults handled correctly (abort on unmapped, etc.)
- Cycle counts include bus cycles
- All existing tests pass

**Estimated Size:** Large (1 week)

---

### Issue 5.3: Trap Registry and ROM Interception

**Goal:** Implement ROM routine interception per spec 8.3.

**Depends on:** Issue 5.2

**Deliverables:**
- [ ] `ITrapRegistry` interface
- [ ] `TrapRegistry` implementation
- [ ] `TrapHandler` delegate
- [ ] `TrapResult` struct
- [ ] CPU checks traps on instruction fetch
- [ ] Example trap handlers (HOME, COUT)
- [ ] Unit tests

**Acceptance Criteria:**
- Traps fire on instruction fetch at registered addresses
- Can enable/disable by category
- Trap handlers can modify CPU state

**Estimated Size:** Medium (2-3 days)

---

## Phase 6: Testing and Polish

### Issue 6.1: Integration Test Suite

**Goal:** End-to-end tests for machine operation.

**Deliverables:**
- [ ] Boot sequence test (reset vector fetch)
- [ ] Memory mapping test (RAM, ROM, I/O)
- [ ] Interrupt handling test
- [ ] Bank switching test
- [ ] Trap handler test

**Estimated Size:** Medium (2 days)

---

### Issue 6.2: Observability and Tracing

**Goal:** Implement trace ring buffer and debug support.

**Deliverables:**
- [ ] `BusTraceEvent` struct
- [ ] `TraceRingBuffer` class
- [ ] Trace emission in MainBus
- [ ] Enable/disable tracing
- [ ] Unit tests

**Estimated Size:** Small (1-2 days)

---

## Summary Timeline

| Phase | Issues | Est. Duration | Dependencies |
|-------|--------|---------------|--------------|
| **Phase 1** | 1.1-1.4 | ~6 days | None |
| **Phase 2** | 2.1-2.2 | ~3 days | Phase 1 partial |
| **Phase 3** | 3.1-3.3 | ~4 days | Phase 1 complete |
| **Phase 4** | 4.1 | ~3 days | Phases 1-3 |
| **Phase 5** | 5.1-5.3 | ~10 days | Phase 4 |
| **Phase 6** | 6.1-6.2 | ~4 days | Phase 5 |
| **Total** | 14 issues | ~30 days | |

---

## Risk Assessment

### High Risk: CPU Migration (Phase 5)

The CPU migration is the riskiest part. Mitigation strategies:

1. **Adapter first** (Issue 5.1) - Proves bus works without changing CPU
2. **Feature flags** - Could keep both code paths temporarily
3. **Incremental instruction migration** - Could migrate instruction groups
4. **Comprehensive test coverage** - Existing tests act as safety net

### Medium Risk: Timing Accuracy

The scheduler and cycle counting must be correct for timing-sensitive software.

1. **Unit tests for cycle counting** - Verify each instruction's cycles
2. **Integration tests** - Verify scheduler dispatch order
3. **Reference comparison** - Compare against known-good emulator

### Low Risk: Interface Changes

The interfaces are mostly defined; implementation follows spec.

---

## Recommendations

1. **Start with Issue 1.1 (MainBus)** - It's the foundation everything else needs
2. **Don't skip the adapter (Issue 5.1)** - It de-risks the CPU migration
3. **Keep existing tests running** - They're your safety net
4. **Review spec gaps during implementation** - May find more issues

---

## Questions to Resolve

### 1. Should we support multiple bus instances (for multi-machine debugging)?

**Answer: Yes, but defer explicit support.**

The current architecture already supports this implicitly:
- `IMemoryBus` is an interface, not a singleton
- `IScheduler` and `ISignalBus` are similarly interface-based
- `IMachine` wraps all components, so multiple machines = multiple `IMachine` instances

For Phase 1-5, we don't need to do anything special. Each machine instance owns its own bus, scheduler, and devices. The debugger can attach to multiple machines by holding references to multiple `IMachine` instances.

**Future consideration:** If we want a "hypervisor mode" where one 65832 supervises multiple compatibility guests, that's a Phase 7+ feature that builds on the existing architecture.

---

### 2. Do we need serialization support for save states?

**Answer: Yes, plan for it but defer implementation.**

Save states require:
- **CPU state** (`CpuState` struct is already designed for this)
- **RAM contents** (straightforward - just serialize the backing array)
- **Device state** (each device needs a `Serialize`/`Deserialize` method)
- **Scheduler state** (pending events and current cycle)
- **Bus mappings** (current page table state for bank switching)

**Recommendation:**
1. Add `ISerializable` marker interface to `IBusTarget` now (empty, for future)
2. Ensure all state is value types or explicitly serializable
3. Defer actual implementation to Phase 6 or later

The `RegionManager.CreateSnapshot()` / `RestoreSnapshot()` pattern we already have is a good foundation.

---

### 3. What's the story for sound output timing?

**Answer: Schedule audio ticks through `IScheduler`.**

The Apple II speaker works by toggling bit at `$C030`. For accurate audio:

1. **During emulation:** Each toggle of `$C030` records the cycle count when it occurred
2. **Scheduler event:** Schedule a periodic `AudioTick` event (e.g., every 1024 cycles)
3. **Audio buffer:** The `ISpeaker` device accumulates toggle times and synthesizes audio samples
4. **Host output:** A separate audio thread consumes the buffer and plays through the host audio API

**Implementation notes:**
- The `ScheduledEventKind.AudioTick` already exists in the spec
- The speaker device maintains a ring buffer of (cycle, state) pairs
- Audio synthesis is decoupled from emulation - host audio runs at its own rate
- For "fast-forward" or uncapped speed, audio can be skipped or time-compressed

**Phase placement:** Part of Issue 3.3 (Soft Switch Composite Page) for the toggle tracking; actual audio output is a separate UI/host integration issue.

---

### 4. How do we handle the Language Card's "R×2" soft switch pattern?

**Answer: State machine in the soft switch handler.**

The Language Card $C080-$C08F switches have a quirk: write-enable requires **two consecutive reads** from certain addresses. This is a state machine:

```csharp
public sealed class LanguageCardController
{
    private bool _preWrite;      // True after first read of a write-enable sequence
    private Addr _lastReadAddr;  // Address of the last read
    
    public byte HandleRead(Addr offset)
    {
        // Check if this is a "second read" that enables writing
        bool isSecondRead = _preWrite && IsWriteEnableAddress(offset) 
                            && offset == _lastReadAddr;
        
        if (isSecondRead)
        {
            _writeEnabled = true;
            _preWrite = false;
        }
        else if (IsWriteEnableAddress(offset))
        {
            _preWrite = true;
            _lastReadAddr = offset;
        }
        else
        {
            _preWrite = false;
        }
        
        // Update bank selection and read/write state
        UpdateBankState(offset);
        
        // Return floating bus or last data bus value
        return 0xFF;
    }
    
    private bool IsWriteEnableAddress(Addr offset)
    {
        // $C081, $C083, $C085, $C087, $C089, $C08B, $C08D, $C08F
        return (offset & 0x01) != 0;
    }
}
```

**Key points:**
- The "R×2" behavior is specific to enabling **writes** to LC RAM
- A single read disables writes; two consecutive reads enable writes
- Bank selection (bank 1 vs bank 2) happens on every access
- This state resets on any non-LC-switch access (implementation choice, or can persist)

**Phase placement:** Part of Issue 3.3 (Soft Switch Composite Page), but the actual bank switching (remapping $D000-$FFFF) uses Issue 1.3's `RemapPage` APIs.

---

*Document last updated: 2025-01-13*
