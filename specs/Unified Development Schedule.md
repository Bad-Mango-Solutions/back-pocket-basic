# Unified Development Schedule

**Document Purpose:** Synchronized development schedule for Bus Architecture, Debug Infrastructure, and Emulator UI.  
**Version:** 1.0  
**Date:** 2025-12-26

---

## Executive Summary

This document coordinates three major development efforts:

| Workstream | Primary Document | Est. Duration | Dependencies |
|------------|-----------------|---------------|--------------|
| **Bus Architecture** | Implementation Roadmap - Bus Architecture.md | ~30 days | Foundation |
| **Debug Infrastructure** | Debugger Infrastructure Migration.md | ~6 days | Bus Phase 1+ |
| **Emulator UI** | Emulator UI Specification.md | ~10+ weeks | Bus Complete |

**Total Timeline:** Approximately 16-20 weeks for full implementation.

---

## Dependency Overview

```
                    ┌────────────────────────────────────────────────────────────┐
                    │                    BUS ARCHITECTURE                        │
                    │  Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 │
                    └────┬──────────┬──────────┬──────────┬──────────┬───────────┘
                         │          │          │          │          │
    ┌────────────────────┼──────────┼──────────┼──────────┼──────────┼─────────┐
    │                    ▼          │          ▼          ▼          ▼         │
    │  DEBUG         D1-D2-D3       │         D4      D5-D6      Complete      │
    │  INFRASTRUCTURE   │           │          │          │          │         │
    └───────────────────┼───────────┼──────────┼──────────┼──────────┼─────────┘
                        │           │          │          │          │
    ┌───────────────────┼───────────┼──────────┼──────────┼──────────┼─────────┐
    │                   │           │          │          │          ▼         │
    │  EMULATOR UI      │       UI-P1 ──────────────────────────→ UI-P2 → P3   │
    │                   │           │                              │           │
    └───────────────────┴───────────┴──────────────────────────────┴───────────┘

Legend:
  → Sequential dependency
  ▼ Can start after predecessor
```

---

## Week-by-Week Schedule

### Week 1: Bus Foundation

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 1.1: MainBus - Page table structure | — | — |
| 2 | Issue 1.1: MainBus - Read8/Write8 | — | — |
| 3 | Issue 1.1: MainBus - TryRead8/TryWrite8 | — | — |
| 4 | Issue 1.1: MainBus - MapPage/MapPageRange | — | — |
| 5 | Issue 1.1: MainBus - Unit tests | — | — |

**Milestone:** MainBus core implementation complete.

---

### Week 2: Bus Extensions + Debug Foundation

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 1.2: Wide Access - Read16/Write16 | Phase D1: IDebugContext extension | — |
| 2 | Issue 1.2: Wide Access - Read32/Write32 | Phase D1: Complete | — |
| 3 | Issue 1.2: Cross-page decomposition | Phase D2: MemoryBusAdapter | — |
| 4 | Issue 1.2: Unit tests | Phase D2: Complete | — |
| 5 | Issue 1.3: Dynamic Remapping | Phase D3: MachineFactory update | — |

**Milestone:** Wide access complete. Debug context can attach to bus.

---

### Week 3: Composite Targets + Scheduler

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 1.3: Remapping tests | Phase D3: Complete | UI Project Setup |
| 2 | Issue 1.4: ICompositeTarget interface | Phase D4: pages command | Avalonia skeleton |
| 3 | Issue 1.4: MainBus composite support | Phase D4: pages command tests | MainWindow shell |
| 4 | Issue 1.4: Unit tests | Phase D4: buslog command | Navigation structure |
| 5 | Issue 2.1: Scheduler - Priority queue | Phase D4: fault command | — |

**Milestone:** Bus Phase 1 complete. New debug commands available. UI shell exists.

---

### Week 4: Scheduler + Device Infrastructure

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 2.1: Schedule/ScheduleAfter | — | Machine Manager: Profile list |
| 2 | Issue 2.1: Drain/RunUntil/Cancel | — | Machine Manager: Profile details |
| 3 | Issue 2.1: Reset, unit tests | — | Machine Manager: Instance list |
| 4 | Issue 2.2: IEventContext | — | Machine Manager: Actions |
| 5 | Issue 2.2: IScheduledDevice | — | Machine Manager: Unit tests |

**Milestone:** Scheduler complete. Machine Manager basic UI ready.

---

### Week 5: Device Registry + Slot Management

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 3.1: DevicePageClass enum | — | VideoDisplay: Bitmap setup |
| 2 | Issue 3.1: DevicePageId struct | — | VideoDisplay: Text 40 mode |
| 3 | Issue 3.1: DeviceRegistry updates | — | VideoDisplay: Text 80 mode |
| 4 | Issue 3.2: IPeripheral interface | — | VideoDisplay: Lo-Res mode |
| 5 | Issue 3.2: ISlotManager interface | — | VideoDisplay: Hi-Res mode |

**Milestone:** Device infrastructure defined. VideoDisplay renders all basic modes.

---

### Week 6: Soft Switch Page

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 3.3: Pocket2eSoftSwitchPage | — | VideoDisplay: Double Hi-Res |
| 2 | Issue 3.3: Soft switch dispatch | — | VideoDisplay: Scaling modes |
| 3 | Issue 3.3: Slot I/O routing | — | VideoDisplay: Color palettes |
| 4 | Issue 3.3: Expansion ROM routing | — | Input: Keyboard handling |
| 5 | Issue 3.3: Unit tests | — | Input: Apple key mapping |

**Milestone:** Soft switch page complete. Display and input functional.

---

### Week 7: Machine Assembly + Debug Tracing

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 4.1: IMachine interface | Phase D5: TraceRecord extension | Input: Mouse handling |
| 2 | Issue 4.1: IPocket2Machine | Phase D5: Bus event integration | Input: Paste support |
| 3 | Issue 4.1: MachineState enum | Phase D5: Enhanced output format | Settings: Display prefs |
| 4 | Issue 4.1: Pocket2eMachineBuilder | Phase D5: Unit tests | Settings: Input prefs |
| 5 | Issue 4.1: Integration tests | Phase D5: Complete | Settings: Persistence |

**Milestone:** Machine builder complete. Enhanced debug tracing available. Settings system working.

---

### Week 8: CPU Migration - Adapter

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 5.1: MemoryBusAdapter | Phase D6: mem command update | Storage Manager: Library view |
| 2 | Issue 5.1: BusAccess creation | Phase D6: poke command update | Storage Manager: Disk list |
| 3 | Issue 5.1: Fault translation | Phase D6: step/run/reset update | Storage Manager: ROM list |
| 4 | Issue 5.1: Existing CPU tests | Phase D6: Unit tests | Storage Manager: Import |
| 5 | Issue 5.1: Adapter tests | Phase D6: Complete | Storage Manager: Export |

**Milestone:** CPU works through adapter. All debug commands updated. Storage Manager browsing works.

---

### Weeks 9-10: CPU Direct Integration

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| W9-1 | Issue 5.2: CPU constructor update | — | Disk Editor: Catalog view |
| W9-2 | Issue 5.2: BusAccess per operation | — | Disk Editor: Sector view |
| W9-3 | Issue 5.2: Load/Store instructions | — | Disk Editor: Hex editing |
| W9-4 | Issue 5.2: Arithmetic instructions | — | Disk Editor: File extract |
| W9-5 | Issue 5.2: Branch/Jump instructions | — | Disk Editor: Tests |
| W10-1 | Issue 5.2: Stack instructions | — | Disk creation wizard |
| W10-2 | Issue 5.2: Fault handling | — | ROM verification UI |
| W10-3 | Issue 5.2: Cycle counting | — | — |
| W10-4 | Issue 5.2: Existing tests update | — | — |
| W10-5 | Issue 5.2: New bus tests | — | — |

**Milestone:** CPU fully integrated with bus. Storage Manager fully functional.

---

### Week 11: Trap Registry

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 5.3: ITrapRegistry interface | — | Debug Console: Terminal view |
| 2 | Issue 5.3: TrapRegistry implementation | — | Debug Console: Command input |
| 3 | Issue 5.3: CPU trap check on fetch | — | Debug Console: Output display |
| 4 | Issue 5.3: Example trap handlers | — | Debug Console: Register panel |
| 5 | Issue 5.3: Unit tests | — | Debug Console: Stack view |

**Milestone:** ROM interception working. Debug Console UI complete.

---

### Week 12: Integration Testing + Observability

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 6.1: Boot sequence test | — | Debug Console: Breakpoints |
| 2 | Issue 6.1: Memory mapping test | — | Debug Console: Memory view |
| 3 | Issue 6.1: Interrupt handling test | — | Debug Console: Disassembly |
| 4 | Issue 6.1: Bank switching test | — | Integration: Connect debug to UI |
| 5 | Issue 6.1: Trap handler test | — | Integration tests |

**Milestone:** Bus integration tests pass. Debug Console integrated.

---

### Week 13: Tracing + UI Polish

| Day | Bus Architecture | Debug Infrastructure | UI |
|-----|-----------------|---------------------|-----|
| 1 | Issue 6.2: BusTraceEvent struct | — | Assembly Editor: Basic UI |
| 2 | Issue 6.2: TraceRingBuffer | — | Assembly Editor: Syntax highlighting |
| 3 | Issue 6.2: MainBus trace emission | — | Assembly Editor: Symbol browser |
| 4 | Issue 6.2: Enable/disable tracing | — | Assembly Editor: Build output |
| 5 | Issue 6.2: Unit tests | — | Assembly Editor: Tests |

**Milestone:** Bus tracing complete. Assembly Editor basic functionality.

---

### Weeks 14-16: UI Phase 2 & 3

| Week | Focus | Deliverables |
|------|-------|--------------|
| 14 | Editor Integration | Build integration, Load into machine, Error markers |
| 15 | Theming & Polish | Light/dark themes, Accessibility, Documentation |
| 16 | Testing & Stabilization | UI tests, Integration tests, Bug fixes |

**Milestone:** UI Phase 1-3 complete. Release candidate ready.

---

## Critical Path

The critical path runs through the Bus Architecture:

```
Issue 1.1 → Issue 1.2 → Issue 2.1 → Issue 4.1 → Issue 5.2 → Issue 6.1
   ↓
(Debug D1-D3 parallel)
   ↓
(UI P1 parallel after Issue 4.1)
```

**Key constraints:**
1. Debug infrastructure cannot start until Issue 1.1 complete
2. UI VideoDisplay needs bus integration (can work with adapter)
3. Debug enhanced tracing (D5) depends on Issue 6.2
4. Full UI integration depends on Issue 5.2 (CPU bus integration)

---

## Resource Allocation

### Recommended Team Structure

| Role | Responsibility | Weeks Active |
|------|---------------|--------------|
| **Bus Engineer** | Bus Architecture phases 1-6 | Weeks 1-13 |
| **Debug Engineer** | Debug Infrastructure phases D1-D6 | Weeks 2-8 |
| **UI Engineer** | Emulator UI phases 1-3 | Weeks 3-16 |

### Solo Developer Path

If working alone, prioritize:

1. **Weeks 1-2:** Bus Phase 1 (foundation for everything)
2. **Week 3:** Debug D1-D3 (get debugger working with bus early)
3. **Weeks 4-7:** Bus Phases 2-4 (complete infrastructure)
4. **Weeks 8-10:** Bus Phase 5 (CPU migration - largest risk)
5. **Weeks 11-13:** Bus Phase 6 + Debug D4-D6
6. **Weeks 14-20:** UI (can be done incrementally)

---

## Risk Mitigation

### High-Risk Items

| Risk | Mitigation | Owner |
|------|-----------|-------|
| CPU migration (Issue 5.2) | Adapter first (5.1), comprehensive tests | Bus Engineer |
| UI display performance | Use WriteableBitmap, profile early | UI Engineer |
| Debug trace performance | Ring buffer, lazy formatting | Debug Engineer |

### Schedule Buffer

Built-in buffer: 4 weeks between estimated completion (Week 16) and typical project timeline expectations.

Recommended use:
- 1 week for CPU migration overruns
- 1 week for integration issues
- 2 weeks for testing/polish

---

## Milestones Summary

| Milestone | Target Week | Deliverables |
|-----------|-------------|--------------|
| **M1: Bus Foundation** | Week 1 | MainBus core, page table routing |
| **M2: Debug Attach** | Week 2 | Debug context with bus support |
| **M3: Bus Phase 1** | Week 3 | Complete bus infrastructure |
| **M4: Scheduler** | Week 4 | Cycle-accurate event scheduling |
| **M5: Device Infrastructure** | Week 5 | Registry, slots, peripherals |
| **M6: Soft Switches** | Week 6 | I/O page functional |
| **M7: Machine Builder** | Week 7 | Create machine from profile |
| **M8: Adapter Bridge** | Week 8 | CPU works through adapter |
| **M9: CPU Integration** | Week 10 | CPU directly uses bus |
| **M10: Trap Registry** | Week 11 | ROM interception working |
| **M11: Integration Tests** | Week 12 | All bus tests passing |
| **M12: Tracing** | Week 13 | Bus observability complete |
| **M13: UI Complete** | Week 16 | Full UI functionality |

---

## Success Criteria

### Bus Architecture Complete

- [ ] All 14 issues implemented and tested
- [ ] CPU uses IMemoryBus for all memory access
- [ ] Bank switching (Language Card) functional
- [ ] Trap handlers intercept ROM calls
- [ ] Bus tracing captures all access

### Debug Infrastructure Complete

- [ ] All 6 phases implemented
- [ ] Existing commands work with bus
- [ ] New commands (pages, buslog, fault) functional
- [ ] Enhanced tracing shows bus events
- [ ] Both legacy and bus modes supported

### Emulator UI Complete

- [ ] Machine Manager creates/manages instances
- [ ] VideoDisplay renders all Apple IIe modes
- [ ] Input correctly maps to Apple II keyboard
- [ ] Storage Manager imports/exports images
- [ ] Debug Console integrates with debug infrastructure
- [ ] Assembly Editor compiles and loads code

---

## Appendix: Issue Cross-Reference

### Bus Architecture Issues

| Issue | Description | Est. Duration | Dependencies |
|-------|-------------|---------------|--------------|
| 1.1 | MainBus Implementation | 2-3 days | None |
| 1.2 | Wide Access and Decomposition | 2 days | 1.1 |
| 1.3 | Dynamic Remapping API | 1 day | 1.1 |
| 1.4 | Composite Target Interface | 1 day | 1.1 |
| 2.1 | Scheduler Implementation | 2 days | None |
| 2.2 | Event Context and Device Integration | 1 day | 1.1, 2.1 |
| 3.1 | Enhanced Device Registry | 1 day | None |
| 3.2 | Peripheral and Slot Manager Interfaces | 1 day | None |
| 3.3 | Soft Switch Composite Page | 2 days | 1.4, 3.2 |
| 4.1 | Machine Interface and Builder | 2-3 days | Phases 1-3 |
| 5.1 | IMemory to IMemoryBus Adapter | 1 day | 1.1 |
| 5.2 | CPU Bus Integration (Direct) | 1 week | 5.1 |
| 5.3 | Trap Registry and ROM Interception | 2-3 days | 5.2 |
| 6.1 | Integration Test Suite | 2 days | Phase 5 |
| 6.2 | Observability and Tracing | 1-2 days | Phase 5 |

### Debug Infrastructure Phases

| Phase | Description | Est. Duration | Dependencies |
|-------|-------------|---------------|--------------|
| D1 | IDebugContext Extension | 0.5 days | Issue 1.1 |
| D2 | MemoryBusAdapter | 0.5 days | Issue 5.1 (shared) |
| D3 | MachineFactory Update | 0.5 days | Issue 4.1 |
| D4 | New Debug Commands | 2 days | D1 |
| D5 | Enhanced Tracing | 1-2 days | Issue 6.2, D1 |
| D6 | Update Existing Commands | 1 day | D1, D2 |

### UI Phases

| Phase | Description | Est. Duration | Dependencies |
|-------|-------------|---------------|--------------|
| P1 | Foundation | 4 weeks | Issue 4.1 (partial) |
| P2 | Storage & Debug | 3 weeks | Phase 5 complete |
| P3 | Editor & Polish | 3 weeks | P2 |
| P4 | PocketGS/PocketME | Future | P3 |

---

*Document last updated: 2025-12-26*
