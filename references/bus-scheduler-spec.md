## Scheduler specification for a cycle-based emulator in C#

This spec defines a **deterministic, cycle-domain event scheduler** suitable for 65C02/65816-style systems, including **WAI fast-forward**, device timers, and interrupt delivery. It intentionally avoids implementation details (heaps, wheels, etc.) and focuses on **interfaces, data structures, and required behaviors**.

---

# Core concepts

## Timebase

- **Cycle** is the single authoritative unit of simulated time.
- Time is represented as an **unsigned 64-bit** integer (`ulong`) and is **monotonic**.
- All scheduler operations are defined relative to a **current cycle** value.

```csharp
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
```

### Required behavior

- The scheduler **MUST NOT** allow `Now` to decrease.
- The scheduler **MUST** be able to advance time by:
  - **Incremental stepping** (normal CPU execution), and
  - **Jumping to next event** (WAI / idle fast-forward).

---

# Event model

## Event identity and handles

Events must be cancellable and reschedulable without ambiguity.

```csharp
public readonly record struct EventId(ulong Value);

public readonly record struct EventHandle(EventId Id);
```

### Required behavior

- Every scheduled event gets a unique `EventId` for the scheduler’s lifetime (wraparound behavior is implementation-defined; must be safe for long runs).
- Cancelling an `EventHandle` **MUST** prevent its callback from being invoked in the future.

---

## Event kinds

The scheduler supports typed events to keep profiling and introspection clean.

```csharp
public enum ScheduledEventKind
{
    DeviceTimer,
    InterruptLineChange,
    DmaPhase,
    AudioTick,
    VideoTick,
    DeferredWork,
    Custom
}
```

### Required behavior

- `Kind` is for **classification/diagnostics**, not routing logic.
- The scheduler **MUST** preserve determinism regardless of `Kind`.

---

## Event record

```csharp
public readonly record struct ScheduledEvent(
    EventId Id,
    Cycle Due,
    ScheduledEventKind Kind,
    int Priority,
    object? Tag
);
```

### Fields

- **Id:** unique identifier.
- **Due:** cycle at which the event becomes runnable.
- **Kind:** classification.
- **Priority:** tie-breaker among same-cycle events (higher first).
- **Tag:** optional, used for debugging/introspection (e.g., `"VIA.T1"`).

### Required behavior

- Events become runnable when `Due <= Now`.
- Among events with the same `Due`, order is:
  1. **Higher `Priority` first**
  2. **Stable insertion order** (earlier schedule wins) if `Priority` equal

This is crucial for reproducibility across runs and platforms.

---

## Event callback and execution context

Event callbacks should not reach into scheduler internals; they receive a context object.

```csharp
public interface IEventSink
{
    EventHandle ScheduleAt(Cycle due, ScheduledEventKind kind, int priority, Action<IEventContext> callback, object? tag = null);
    EventHandle ScheduleAfter(Cycle delta, ScheduledEventKind kind, int priority, Action<IEventContext> callback, object? tag = null);
    bool Cancel(EventHandle handle);
}

public interface IEventContext
{
    Cycle Now { get; }
    IEventSink Events { get; }
    IInterruptController Interrupts { get; }
    IBusFacade Bus { get; }          // Minimal: optional for your architecture; can be a thin facade.
    ITracer? Tracer { get; }         // Optional observability.
}
```

### Required behavior

- Callbacks **MAY** schedule/cancel events (including events due at the current cycle).
- The scheduler **MUST** guarantee that callbacks see a coherent `Now`.
- The scheduler **MUST** prevent re-entrancy hazards by defining a clear rule:
  - Events scheduled for `Due == Now` during dispatch are **eligible to run in the same dispatch pass**, but only after the current callback completes and respecting ordering rules.

---

# Scheduler interface

```csharp
public interface IScheduler : IEventSink
{
    Cycle Now { get; }

    /// <summary>
    /// Advance time by a fixed number of cycles, dispatching all events that become due.
    /// Intended for normal CPU-driven progression.
    /// </summary>
    void Advance(Cycle delta);

    /// <summary>
    /// Dispatch all events that are due at the current cycle.
    /// Does not advance time.
    /// </summary>
    void DispatchDue();

    /// <summary>
    /// Returns the earliest due time of any pending event, or null if none exist.
    /// </summary>
    Cycle? PeekNextDue();

    /// <summary>
    /// Jump forward to the next pending event time (if any), set Now to it, then DispatchDue().
    /// Intended for WAI/idle fast-forward.
    /// </summary>
    bool JumpToNextEventAndDispatch();

    /// <summary>
    /// Jump forward to a specific cycle (must be >= Now), dispatching any events that become due.
    /// </summary>
    void JumpTo(Cycle target);

    /// <summary>
    /// Remove all events and reset time to zero (optional; if provided must be deterministic).
    /// </summary>
    void Reset();
}
```

---

# Interrupt integration

## Interrupt controller contract

The scheduler shouldn’t “know CPU rules”; it just helps deliver line changes and timing. Use a separate controller that devices drive and the CPU samples.

```csharp
public enum InterruptLine
{
    Irq,
    Nmi,
    Reset
}

public interface IInterruptController
{
    /// <summary>
    /// Set or clear an interrupt line at the current scheduler time.
    /// </summary>
    void SetLine(InterruptLine line, bool asserted, object? sourceTag = null);

    /// <summary>
    /// Query current interrupt line state (level).
    /// </summary>
    bool IsAsserted(InterruptLine line);

    /// <summary>
    /// Optional: edge tracking for NMI, etc.
    /// </summary>
    bool ConsumeEdge(InterruptLine line);
}
```

### Required behavior

- Devices can assert/deassert lines from scheduled callbacks.
- The CPU determines *when* it checks and *how* it vectors.
- For level-sensitive IRQ:
  - `IsAsserted(Irq)` reflects current level.
- For edge-sensitive NMI:
  - `ConsumeEdge(Nmi)` returns whether an edge occurred since last consume (exact edge semantics are CPU-specific; keep it configurable if you support multiple cores).

---

# Device-facing scheduling

## Schedulable devices

Devices receive access to scheduler through context or constructor injection.

```csharp
public interface IScheduledDevice
{
    /// <summary>
    /// Called after machine construction to allow initial event scheduling.
    /// </summary>
    void Initialize(IEventSink events, IInterruptController interrupts);

    /// <summary>
    /// Optional: used for save-states/introspection; not required for correctness.
    /// </summary>
    string Name { get; }
}
```

### Required behavior

- Devices should not assume real-time; they schedule in **cycles**.
- Periodic devices (timers, video scanlines) should reschedule themselves deterministically.

---

# CPU integration

## CPU stepping contract

The scheduler does not execute CPU; the machine loop does. But the CPU must report cycle consumption and support WAI behavior.

```csharp
public enum CpuRunState
{
    Running,
    WaitingForInterrupt
}

public readonly record struct CpuStepResult(
    CpuRunState State,
    Cycle CyclesConsumed
);

public interface ICpuCore
{
    CpuRunState State { get; }

    /// <summary>
    /// Execute until the next instruction boundary (or equivalent micro-step boundary),
    /// returning cycle cost and new state.
    /// </summary>
    CpuStepResult Step(IEventContext context);
}
```

### Required behavior

- Normal execution:
  - CPU returns `CyclesConsumed > 0`
  - Machine calls `scheduler.Advance(CyclesConsumed)`
- WAI:
  - CPU returns `State = WaitingForInterrupt`
  - CPU should not consume further cycles until woken

---

## WAI fast-forward behavior

The **machine loop** (not the CPU core) uses the scheduler to fast-forward:

### Required behavior (normative)

When CPU enters `WaitingForInterrupt`:

1. The machine must determine whether wake conditions already exist at `Now`:
   - e.g., `IRQ asserted && I flag clear`, `NMI edge pending`, `RESET asserted`
2. If wake condition exists, resume immediately without advancing time.
3. Otherwise, the machine must call:
   - `JumpToNextEventAndDispatch()`
4. After dispatch, re-check wake conditions.
5. Repeat until CPU can wake, or no events remain (implementation-defined: may stall, throw, or yield to host).

This avoids spinning and accurately represents “clock running, CPU idle”.

---

# Dispatch semantics

## Determinism and ordering

### Required behavior

- Dispatch must be **deterministic** given the same initial state and inputs.
- If multiple events are due at the same cycle:
  1. Higher priority first
  2. Stable insertion order next
- Events scheduled for `Due < Now` are legal only if caused by a bug; the scheduler must:
  - Clamp to `Now`, **or**
  - Throw, **or**
  - Record an error and still dispatch
  Choose one policy and make it consistent (recommended: clamp to `Now` + trace an error).

---

## Event starvation rules

### Required behavior

- If callbacks schedule more same-cycle work (`Due == Now`) indefinitely, the scheduler must not deadlock silently.
- Provide a defined policy:
  - `MaxSameCycleDispatch` guard (configurable), after which the scheduler throws or yields a diagnostic.

```csharp
public readonly record struct SchedulerLimits(
    int MaxEventsPerDispatchPass,
    int MaxSameCycleEvents
);
```

---

# Introspection and tracing

## Optional diagnostic surface

```csharp
public interface ISchedulerDiagnostics
{
    int PendingEventCount { get; }
    IReadOnlyList<ScheduledEvent> SnapshotPending(int max = 256);
}
```

### Required behavior

- Diagnostics must not affect ordering or timing.
- Snapshot is best-effort and may omit callbacks for safety.

---

# Save-state compatibility

To support save states, scheduled events need serializable metadata. The callback itself usually isn’t serializable, so callbacks should be routed by a token.

## Optional callback routing abstraction

```csharp
public readonly record struct EventToken(int Value);

public interface IEventRouter
{
    void Invoke(EventToken token, IEventContext context, object? payload);
}
```

Then:

```csharp
public readonly record struct RoutedEvent(
    EventId Id,
    Cycle Due,
    ScheduledEventKind Kind,
    int Priority,
    EventToken Token,
    object? Payload,
    object? Tag
);
```

### Required behavior

- If you implement save-states, you must ensure:
  - Rehydrated events preserve `Due`, ordering, and meaning.
- If you don’t implement save-states now, you can keep `Action<IEventContext>` and defer this.

---

# Machine loop contract

A minimal deterministic machine loop (conceptually) looks like:

1. **CPU step** → returns `CyclesConsumed` and `State`
2. `scheduler.Advance(CyclesConsumed)` (dispatching events that become due)
3. If CPU enters `WaitingForInterrupt`, run the WAI loop described above

The scheduler is responsible for **time and event ordering**, not CPU semantics.

---

# Common patterns (normative examples)

## Periodic timer device

- On write that arms the timer:
  - schedule `DeviceTimer` at `Now + countdown`
- Callback:
  - assert IRQ line via `Interrupts.SetLine(Irq, true, tag)`
  - if free-running, schedule next expiration deterministically

## Video scanline

- Schedule next scanline at fixed cycle intervals
- On callback:
  - update raster counters
  - schedule next scanline
  - optionally assert VBL IRQ at configured line

---

# Configuration surface

```csharp
public readonly record struct SchedulerConfig(
    SchedulerLimits Limits,
    bool AllowPastDueEventsToClamp,
    bool RunSameCycleEventsInSamePass
);
```

### Required behavior

- Defaults must favor determinism and safety:
  - `RunSameCycleEventsInSamePass = true`
  - reasonable limits enabled
  - clamp-or-throw policy chosen and documented

---

## What you’ll get if you follow this spec

- **WAI is free**: you jump to the next causally relevant moment.
- **Determinism**: identical inputs produce identical traces.
- **Extensibility**: DMA, audio, video, timers all become “just events”.
- **Introspection**: you can always answer “what happens next and why?”

If you want, I can tailor this spec to your existing bus/region-stack/capability-token architecture (e.g., splitting `IEventContext.Bus` into data/control/observe planes, or modeling DMA as a privileged bus master with explicit cycle claims).
