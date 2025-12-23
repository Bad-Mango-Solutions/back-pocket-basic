What you want is a **bus that’s fast on the hot path**, but still **rich in introspection and cross-talk** (DMA, interrupts, timing, “who touched what,” and why). The mistake most emulators make is choosing either *a pure “read/write API”* (fast, opaque) or *a pure message/event bus* (observable, slow). The best design is **three planes** that share the same concepts but don’t share the same cost.

------

## Main bus architecture

### Data plane

The data plane answers: **“Given an address, what happens on a read/write right now?”**

- **Paged address translation:** map the CPU address space into fixed-size pages (your 4K pages idea fits perfectly).
- **Page entry points:** each page resolves to a *small* set of callable endpoints (read/write/peek), ideally avoiding virtual dispatch.
- **Side effects are allowed:** reads/writes can mutate device state (I/O regs, soft switches, etc.).

### Control plane

The control plane answers: **“How is the machine wired today?”**

- Device registration and slot insertion/removal
- Bank switching / overlay changes (ROM shadowing, language-card style, etc.)
- “Map this range to that device” operations
- Save-state wiring metadata

### Observability plane

The observability plane answers: **“What happened, and who did it?”** without poisoning the hot path.

- Trace stream (ring buffer) of bus transactions (address, value, kind, cycle, device id)
- Optional “watchpoints” and “breakpoints”
- Optional structured diagnostics (`DiagnosticSource` / `Activity`) *only when enabled*

------

## A concrete mapping model that maximizes information flow

### 1) Standardize every access into a single context struct

This is where your “maximize flow of information” goal becomes real: **every subsystem speaks the same “access language.”**

```csharp
public enum BusOp : byte { Read, Write, Peek, Poke } // Peek/Poke = no side effects

public readonly struct BusAccess
{
    public readonly uint Address;
    public readonly BusOp Op;
    public readonly byte Value;        // for writes
    public readonly ulong Cycle;
    public readonly ushort SourceId;   // CPU, DMA channel, debugger, etc.
    public readonly ushort Flags;      // width, emulation/native, privileged, etc.

    public BusAccess(uint address, BusOp op, byte value, ulong cycle, ushort sourceId, ushort flags)
        => (Address, Op, Value, Cycle, SourceId, Flags) = (address, op, value, cycle, sourceId, flags);
}
```

**Why this matters:** memory bus, peripheral bus, DMA engine, debugger, and tracers can all reason about the *same event* without guessing.

------

### 2) Make the page table the “spine” of your emulator

Use an array of `PageEntry` indexed by `Address >> PageShift`.

Each entry carries:

- **Fast read/write endpoints**
- **A device identity**
- **Optional “region tag”** (RAM/ROM/IO/Slot/Shadow) for tracing and policy

```csharp
public readonly struct PageEntry
{
    public readonly ushort DeviceId;
    public readonly ushort RegionTag;

    // Choose one:
    public readonly Func<uint, byte> Read;
    public readonly Action<uint, byte> Write;
    public readonly Func<uint, byte> Peek;
    public readonly Action<uint, byte> Poke;

    public PageEntry(ushort deviceId, ushort regionTag,
        Func<uint, byte> read, Action<uint, byte> write,
        Func<uint, byte> peek, Action<uint, byte> poke)
        => (DeviceId, RegionTag, Read, Write, Peek, Poke) = (deviceId, regionTag, read, write, peek, poke);
}
```

For maximum speed, you can later swap delegates for `delegate*` function pointers, but the *shape* stays the same.

------

### 3) Treat “peripheral bus” as a routed sub-bus, not a separate universe

Model slots as **bus segments** that are reached through the main bus mapping:

- Main bus maps `$C0n0–C0nF`, `$Cn00–$CnFF`, `$C800–$CFFF` (or your equivalent) to a **SlotMux device**
- SlotMux then routes to the inserted card’s internal register space / ROM space
- SlotMux is also where you centralize “slot personality” rules (IIe vs IIgs-like behaviors, priority, internal vs external firmware)

This gives you the best of both worlds:

- CPU still “sees” one address space
- Peripheral topology remains explicit and inspectable

------

### 4) Make composite pages a first-class feature

Composite pages are how you keep 4K granularity without losing II-family weirdness.

A composite page entry can internally dispatch by offset to:

- RAM window
- soft switch register block
- mirrored regions
- floating bus behavior
- “holes”

The key is: **dispatch happens inside the page**, so the main bus still does one page lookup.

------

## Cross-talk primitives you don’t want to forget

### Interrupts and lines

Don’t model interrupts as “the CPU asks devices.” Model them as **lines** owned by a **SignalHub**:

- `IRQ`, `NMI`, `RESET`, `RDY`, `DMAREQ`, etc.
- Devices assert/deassert lines
- CPU samples lines at defined points
- Tracer can log edge transitions

This avoids “spooky action at a distance” and makes timing bugs debuggable.

### DMA and bus mastering

DMA should produce **BusAccess transactions** with `SourceId = DMAx`, not special-case memory copying. That way:

- I/O side effects work
- Watchpoints work
- Trace shows the truth

### Clocking and scheduling

Have a single **machine scheduler** (cycle-based), and let devices register:

- `Tick(cycles)` for coarse devices
- “next event at cycle X” for precise devices (video, disk, serial)

A bus that “maximizes information flow” is one where the timeline is explicit, not implied.

------

## Observability that won’t ruin performance

### Ring-buffer trace, always compiled in, optionally enabled

Log a compact struct per access (when enabled), no allocations:

```csharp
public readonly struct BusTraceEvent
{
    public readonly ulong Cycle;
    public readonly uint Address;
    public readonly byte Value;
    public readonly BusOp Op;
    public readonly ushort SourceId;
    public readonly ushort DeviceId;
}
```

- When tracing off: one `if (traceEnabled)` guard, nothing else
- When tracing on: write into a fixed array + wrap index (`& (size-1)`)

### Debug UI / tooling uses channels, not the bus hot path

Expose trace snapshots via `Channel<BusTraceEvent>` or by polling the ring buffer, so the CPU core never blocks.

------

## One design choice that pays off forever

Make **“Peek/Poke” a required concept** everywhere.

- **Read/Write** = real bus cycles with side effects
- **Peek/Poke** = debugger, disassembler, prefetch, state inspection

This single split prevents an enormous class of “debugger broke the machine” and “disassembler toggled a soft switch” bugs, and it keeps your tooling honest.

------

## Two questions that will lock the design in cleanly

1. Do you want **multi-byte accesses** (16/32-bit) to be modeled as *multiple byte bus cycles* (Apple-ish) or *atomic wide cycles* (modern-ish) or *both* via flags?
2. Do you want “device identity” to be **stable and semantic** (Slot6Disk, MegaII, MMU) or purely structural (device instance id)? The former makes trace logs feel like a story instead of a hex dump.

If you answer those, I can suggest a tight set of interfaces (`IBusDevice`, `IBusSegment`, `ISignalDevice`, `ISchedulerDevice`) and a page-entry layout that won’t paint you into a corner.

Got it. With **(1) both atomic and byte-wise accesses via flags** and **(2) structural IDs with a name registry**, the “best” main-bus mapping is:

- **One unified address space** (always),
- **One page-table spine** (always),
- **Two execution semantics** (Native vs Compat) expressed as flags on every access,
- **Two access granularities** (wide-atomic vs decomposed byte cycles) chosen per initiator/op,
- **One observability story** that can narrate everything without infecting the hot path.

------

## Access model with mode and width baked in

### Access flags

Make *mode* and *width semantics* first-class, because they change how you decompose cycles, apply side effects, and model timing.

```csharp
[Flags]
public enum AccessFlags : uint
{
    None        = 0,

    // CPU behavior mode
    Compat      = 1 << 0,   // Apple II-like semantics
    Native      = 1 << 1,   // 65832 semantics

    // Width / decomposition
    Width8      = 1 << 2,
    Width16     = 1 << 3,
    Width32     = 1 << 4,

    Atomic      = 1 << 5,   // treat as one bus transaction if target supports it
    Decompose   = 1 << 6,   // force byte-wise cycles (Apple-ish)
    LittleEndian= 1 << 7,   // for wide ops

    // Side-effect policy
    NoSideFx    = 1 << 8,   // Peek/Poke style
}
```

The bus doesn’t infer; it **routes based on flags**.

------

## The page table is the spine

### Page entry carries “fast path + capability bits”

You want a single lookup: `page = pages[addr >> PageShift]`, then a direct call.

```csharp
[Flags]
public enum TargetCaps : uint
{
    None          = 0,
    SupportsPeek  = 1 << 0,
    SupportsPoke  = 1 << 1,
    SupportsWide  = 1 << 2,   // device can do atomic 16/32 safely
    SideEffects   = 1 << 3,
    TimingSense   = 1 << 4,
}

public readonly struct PageEntry
{
    public readonly int DeviceInstanceId;   // structural
    public readonly ushort RegionTag;       // RAM/ROM/IO/SLOT/etc (for tooling)
    public readonly TargetCaps Caps;

    public readonly Func<uint, byte> Read8;
    public readonly Action<uint, byte> Write8;

    // Optional: if not present, bus falls back to decomposition
    public readonly Func<uint, ushort>? Read16;
    public readonly Action<uint, ushort>? Write16;
    public readonly Func<uint, uint>? Read32;
    public readonly Action<uint, uint>? Write32;

    public PageEntry(int id, ushort tag, TargetCaps caps,
        Func<uint, byte> r8, Action<uint, byte> w8,
        Func<uint, ushort>? r16 = null, Action<uint, ushort>? w16 = null,
        Func<uint, uint>? r32 = null, Action<uint, uint>? w32 = null)
        => (DeviceInstanceId, RegionTag, Caps, Read8, Write8, Read16, Write16, Read32, Write32)
         = (id, tag, caps, r8, w8, r16, w16, r32, w32);
}
```

**Why this works for Q1:** the same page entry can service both wide-atomic and byte-decomposed operations; the bus chooses at runtime.

------

## Wide access policy: atomic when possible, decomposed when needed

### The rule set

1. **If `Decompose` is set:** always emit byte cycles (even for RAM).
2. **Else if `Atomic` is set:** attempt wide handler if `SupportsWide` and handler exists.
3. **Else:** choose based on `Compat` vs `Native` default policy (you control this centrally).
4. **Cross-page wide access:** either always decompose, or split into two atomic parts; pick one policy and make it consistent.

### A single entry point, two internal paths

```csharp
public readonly record struct BusOpCtx(
    uint Address, ulong Cycle, int SourceId, AccessFlags Flags);

public sealed class MainBus
{
    private readonly PageEntry[] _pt;
    private readonly ITraceSink _trace;

    public uint Read(in BusOpCtx ctx)
    {
        // Decide width from flags; dispatch to Read8/16/32
        // Decompose if forced or unsupported
        // Trace structural IDs + decoded names via registry (not on hot path)
        throw new NotImplementedException();
    }
}
```

In practice, implement `Read8/Write8` as the “always valid” primitives, and layer `Read16/Read32` as *optional accelerants*.

------

## Structural IDs with human-readable translation

You want device instance IDs to stay structural, but tooling to tell stories. The clean pattern is a **DeviceRegistry** that’s write-once at wiring time and read-many everywhere else.

```csharp
public sealed class DeviceRegistry
{
    private readonly Dictionary<int, DeviceInfo> _info = new();

    public void Register(int id, string kind, string name, string path)
        => _info[id] = new DeviceInfo(id, kind, name, path);

    public bool TryGet(int id, out DeviceInfo info) => _info.TryGetValue(id, out info);
}

public readonly record struct DeviceInfo(int Id, string Kind, string Name, string Path);
```

- **`Id`**: structural instance id
- **`Kind`**: “SlotCard”, “Ram”, “Rom”, “MegaII”, “SmartPortFacade”
- **`Name`**: user-facing label
- **`Path`**: wiring path (`"main/slots/6/disk2"`), which makes traces navigable

**Hot path rule:** traces store only `DeviceInstanceId` and `RegionTag`. The UI/debugger resolves IDs to names *after the fact*.

------

## Don’t forget the “other buses”: signals and scheduling

To maximize information flow between “memory bus” and “peripheral bus,” you also need two non-memory fabrics that are just as important:

### Signal fabric for IRQ/NMI/DMA/RDY

Use explicit lines managed by a hub, not callbacks into the CPU.

- Devices: `Assert(line)`, `Clear(line)`
- CPU: `Sample(line)` at defined boundaries
- Tracer: logs edges with `DeviceInstanceId`

### Scheduler fabric for time

A single cycle-based scheduler lets bus transactions become *time-anchored facts*.

- CPU advances time by executing
- Devices schedule next event (video, disk, serial)
- DMA uses the bus as a source with its own `SourceId`

This is how you avoid “timing is hidden in who calls whom.”

------

## Tooling-friendly tracing without allocations

Emit a compact struct per access when enabled:

```csharp
public readonly struct BusTraceEvent
{
    public readonly ulong Cycle;
    public readonly uint Address;
    public readonly uint Value;           // store widened, width in Flags
    public readonly AccessFlags Flags;
    public readonly int SourceId;
    public readonly int DeviceInstanceId;
    public readonly ushort RegionTag;
}
```

This gives you the “flow of information” you want: every transaction ties together **who**, **where**, **what width**, **which mode**, **which device**, **when**.

------

## Two concrete policy recommendations

1. **Compat default = decomposed; Native default = atomic when possible.**  
    That matches expectations: Apple II behavior tends to be byte/cycle-visible; your native world can be more modern and performant.
2. **Cross-page wide access always decomposes.**  
    It’s predictable, matches lots of real bus behavior, and prevents “half of a register write went to a different device page” surprises.

------

If you tell me your **page size (4K confirmed?)**, your **address width**, and whether you already have **separate `Peek/Poke` semantics**, I can give you a concrete `MainBus.Read16/Write16` implementation that (a) correctly decomposes with per-byte trace events when needed, (b) optionally emits a single “aggregate” trace event for atomic ops, and (c) keeps the per-access overhead branch-light.

That’s a solid set of constraints: 4K pages, a “wide” native world, and a compat world that’s not just *narrow* but *stateful* (E flag changes register width). The best bus mapping is to make **the bus width a property of the access**, not of the bus, and to make **Peek/Poke a first-class access policy** so tooling can observe without perturbing.

------

## Access context that captures mode and effective width

You’ll want the CPU to compute an **effective access descriptor** *per memory operation* and pass it to the bus. That’s where E/M/X flags belong—*not* inside devices, and not inferred by the bus.

### Core enums and flags

```csharp
public enum CpuMode : byte { Native, Compat }

public enum AccessIntent : byte
{
    DataRead, DataWrite,
    InstructionFetch,
    DebugRead, DebugWrite,     // tooling
    DmaRead, DmaWrite,
}

[Flags]
public enum AccessFlags : uint
{
    None        = 0,

    // Side-effect policy
    NoSideFx    = 1 << 0,  // Peek/Poke behavior (tooling, disasm, etc.)

    // Endianness + decomposition
    LittleEndian= 1 << 1,
    Atomic      = 1 << 2,  // request atomic wide op if supported
    Decompose   = 1 << 3,  // force byte-wise bus cycles
}
```

### The access descriptor the bus sees

```csharp
public readonly record struct BusAccess(
    uint Address,
    uint Value,          // write payload (low bits used by Width)
    byte WidthBits,      // 8/16/32
    CpuMode Mode,
    bool EmulationE,     // only meaningful in Compat
    AccessIntent Intent,
    int SourceId,
    ulong Cycle,
    AccessFlags Flags);
```

**Key point:** `WidthBits` is the *effective* width for this operation, after the CPU has applied E/M/X rules. The bus just enforces the consequences (atomic vs decomposed, cross-page rules, tracing).

------

## Page table design for 4K pages with “wide optional”

Keep your page table as the routing spine. Each entry routes to a target and advertises capabilities.

```csharp
[Flags]
public enum TargetCaps : uint
{
    None          = 0,
    SupportsPeek  = 1 << 0, // NoSideFx read/write supported
    SupportsWide  = 1 << 1, // atomic 16/32 safe for this region
    SideEffects   = 1 << 2, // reads/writes can mutate state
}

public interface IBusTarget
{
    byte  Read8(uint phys, in BusAccess a);
    void  Write8(uint phys, byte value, in BusAccess a);

    ushort Read16(uint phys, in BusAccess a) => (ushort)(Read8(phys, a) | (Read8(phys + 1, a) << 8));
    void   Write16(uint phys, ushort value, in BusAccess a)
    {
        Write8(phys, (byte)value, a);
        Write8(phys + 1, (byte)(value >> 8), a);
    }

    uint Read32(uint phys, in BusAccess a) { /* default via 4x Read8 */ throw new NotImplementedException(); }
    void Write32(uint phys, uint value, in BusAccess a) { /* default via 4x Write8 */ throw new NotImplementedException(); }
}

public readonly record struct PageEntry(
    int DeviceId,
    ushort RegionTag,
    TargetCaps Caps,
    IBusTarget Target,
    uint PhysBase);
```

You can later replace `IBusTarget` calls with function pointers for speed; the shape still holds.

------

## How the bus chooses atomic vs decomposed

### Policy that matches your machine

- **Compat mode default:** decomposed (byte-visible), unless you explicitly request atomic and the target supports it.
- **Native mode default:** atomic when possible (RAM, linear ROM), decomposed for I/O unless the target opts in.

### Cross-page wide access

With 4K pages, wide accesses can straddle a page boundary. Pick a hard rule:

- **Rule:** if an access crosses a page boundary, **always decompose into byte cycles**.
- **Reason:** it avoids the nastiest class of “half write goes to a different device” bugs and keeps behavior deterministic.

------

## Read and write entry points you’ll actually use

Give the bus a single internal primitive: `Read8/Write8` (always valid), then build `Read16/Read32` on top with your policy gates.

```csharp
public sealed class MainBus
{
    private readonly PageEntry[] _pt;
    private readonly int _pageShift = 12;         // 4K
    private readonly uint _pageMask = 0xFFF;

    public byte Read8(in BusAccess a)
    {
        ref readonly var pe = ref _pt[a.Address >> _pageShift];
        uint phys = pe.PhysBase + (a.Address & _pageMask);
        return pe.Target.Read8(phys, a);
    }

    public void Write8(in BusAccess a, byte value)
    {
        ref readonly var pe = ref _pt[a.Address >> _pageShift];
        uint phys = pe.PhysBase + (a.Address & _pageMask);
        pe.Target.Write8(phys, value, a);
    }

    public ushort Read16(in BusAccess a)
    {
        if (CrossesPage(a.Address, 2) || (a.Flags & AccessFlags.Decompose) != 0)
            return (ushort)(Read8(a) | (Read8(a with { Address = a.Address + 1 }) << 8));

        ref readonly var pe = ref _pt[a.Address >> _pageShift];
        if ((a.Flags & AccessFlags.Atomic) != 0 && (pe.Caps & TargetCaps.SupportsWide) != 0)
        {
            uint phys = pe.PhysBase + (a.Address & _pageMask);
            return pe.Target.Read16(phys, a);
        }

        // default: compat prefers decomposition, native may still decompose for I/O based on RegionTag/Caps
        return (ushort)(Read8(a) | (Read8(a with { Address = a.Address + 1 }) << 8));
    }

    private bool CrossesPage(uint addr, int bytes)
        => ((addr & _pageMask) + (uint)(bytes - 1)) > _pageMask;
}
```

You’ll do the same for `Write16/Read32/Write32`. This gives you:

- consistent semantics,
- cross-page safety,
- and per-target opt-in for atomic wide ops.

------

## Where the E flag belongs

The bus shouldn’t know *why* the width is 8 vs 16; it should only enforce what the access says.

So the CPU does:

- compute effective width based on:
  - compat vs native
  - E flag (and M/X if you model 65816 fully)
  - instruction semantics (e.g., stack operations are special)
- populate `BusAccess.WidthBits`
- call `Read8/Read16/Read32` accordingly

That keeps device modeling clean: a VIA or soft switch shouldn’t need to know about E—it just sees “8-bit write to this register.”

------

## Peek and poke semantics: add them now, before tooling grows teeth

Implement Peek/Poke as **the same address routing**, but with **NoSideFx** set and *strict conventions*:

- **Peek:** read as if on the bus, but must not toggle soft switches / clear IRQ flags / advance disk state.
- **Poke:** write without side effects (rare; mainly for debugger memory patching), or disallow for I/O regions.

Pragmatic rule that keeps you honest:

- RAM/ROM: Peek/Poke always allowed.
- I/O: Peek allowed only if device declares `SupportsPeek`; otherwise bus can return a “floating bus” value or throw a debug-only fault.
- Tooling chooses: “safe inspect” (Peek) vs “real poke” (Read/Write).

This will save you months later.

------

## One more “bus you haven’t thought of” that you’ll want soon

### Signal bus (IRQ/NMI/DMA/RDY) as explicit lines

Don’t let devices directly call CPU methods like `cpu.RaiseIrq()`. Use a `SignalHub` that records *who asserted what* and *when*. That becomes part of your observability story, and it makes compat timing issues tractable.

------

## Small next step that will crystallize everything

If you show me (or describe) your current `PageEntry` / “composite device page” representation, I’ll suggest the minimal refactor that adds:

- `BusAccess` with `Mode + EmulationE + WidthBits + NoSideFx`
- cross-page decomposition rule
- `Peek/Poke` conventions
- structural `DeviceId` + `DeviceRegistry` name translation

…without forcing you into a huge rewrite.

> **User:** I don't have a "PageEntry" or composite device page representation. I've been mostly focused on the CPU and instruction set implementation to this point. Let's wrap up, with a recap of what we've decided so far and leave a list of questions that can be answered when I feed this to GitHub Copilot, where I'm currently working to figure out the issue item that will guide the development work on this phase.

Perfect place to pause. You’ve done the *right* thing by getting the CPU solid first — now you’re at the natural seam where architecture wants to crystallize before code hardens.

Below is a **clean recap** of what we’ve converged on, followed by a **Copilot‑ready question list** you can paste directly into an issue or design doc to drive the next phase.

------

# ✅ Recap: What We’ve Decided So Far

## 🧠 1. The Bus Is the Spine, Not the CPU

- The CPU **does not own memory**.
- Every memory interaction flows through a **single main bus**.
- The bus is responsible for:
  - Address routing
  - Width semantics
  - Atomic vs decomposed behavior
  - Device dispatch
  - Observability

The CPU computes *intent*; the bus enforces *consequences*.

------

## 🧩 2. Page-Based Routing (4K Pages)

- The emulator will use **4 KB pages** as the fundamental routing unit.
- Each page resolves to:
  - A **target device**
  - A **physical base**
  - A **capability set** (supports wide access, side effects, etc.)
- Page lookup is O(1) and is the **hot path**.

This replaces ad‑hoc address decoding and enables:

- Composite device pages
- Slot façades
- Compatibility windows
- Future MMU features

------

## ⚙️ 3. Access Semantics Are Explicit

Every bus access carries a **fully described access context**, including:

- Address
- Effective width (8/16/32)
- CPU mode:
  - Native (65832)
  - Compat (Apple II)
- Emulation flag (`E`) for compat width behavior
- Intent (data, instruction fetch, DMA, debug)
- Flags:
  - Atomic vs decomposed
  - No‑side‑effects (Peek/Poke)
  - Endianness

The bus never guesses.

------

## 🔀 4. Atomic vs Decomposed Access Rules

- **Compat mode default:** decomposed byte cycles
- **Native mode default:** atomic wide access when supported
- **Cross‑page wide access:** always decomposed
- Targets may opt into atomic wide access via capability flags

This matches Apple II expectations *and* modern performance needs.

------

## 🧪 5. Peek / Poke Are First-Class

- Peek/Poke are **non‑side‑effecting** accesses
- Used by:
  - Debugger
  - Disassembler
  - State inspection
- Devices explicitly declare whether they support Peek/Poke
- Prevents tooling from mutating machine state accidentally

This is a future‑proofing move that saves pain later.

------

## 🧱 6. Devices Are Structural, Not Semantic

- Devices are identified by **structural instance IDs**
- Human‑readable names live in a **DeviceRegistry**
- Traces and logs store IDs only
- Tooling resolves IDs → names → wiring paths

This keeps the hot path lean and the tooling rich.

------

## 🔔 7. “Other Buses” Are Explicit

Two non‑memory fabrics are part of the design:

### Signal Fabric

- IRQ, NMI, DMA, RDY, etc.
- Devices assert/deassert lines
- CPU samples lines at defined points
- Signal transitions are traceable

### Scheduler / Time Fabric

- Single cycle‑based scheduler
- Devices schedule future events
- DMA and video are time‑aware participants
- Bus accesses are time‑anchored facts

No hidden timing.

------

# 🧭 What You *Don’t* Have Yet (And That’s OK)

- No `PageEntry` type yet
- No composite device page abstraction yet
- No bus implementation yet
- No device registry yet

That’s exactly what this phase is about.

------

# 📝 Copilot‑Ready Questions for the Next Phase

You can paste these directly into a GitHub issue or Copilot prompt.

------

### 🧩 Bus & Page Table Design

1. What should a `PageEntry` struct contain to support:
   - Fast routing
   - Device identity
   - Capability flags
   - Optional wide access handlers?
2. How should the page table be represented for a 32‑bit address space with 4K pages?
3. How should page entries be updated dynamically (bank switching, overlays, slot changes)?

------

### ⚙️ Access Semantics

1. How should a `BusAccess` or equivalent context struct be shaped to carry:
   - Width
   - Mode (Native vs Compat)
   - Emulation flag
   - Intent
   - Side‑effect policy?
2. Where should effective width be computed — CPU or bus?
3. How should cross‑page wide accesses be detected and decomposed?

------

### 🔀 Atomic vs Decomposed Policy

1. How should the bus decide between atomic and decomposed access?
2. How should devices advertise support for atomic wide access?
3. How should I/O regions differ from RAM/ROM in access policy?

------

### 🧪 Peek / Poke Semantics

1. How should Peek/Poke be represented in the access model?
2. How should devices declare Peek/Poke support?
3. What should happen if tooling attempts a Peek on a side‑effect‑only device?

------

### 🧱 Device Model

1. What minimal interface should all bus‑attached devices implement?
2. How should composite device pages dispatch sub‑regions internally?
3. How should slot façades be modeled without leaking slot logic into devices?

------

### 🔔 Signals & Timing

1. How should IRQ/NMI/DMA lines be modeled as explicit signals?
2. How should devices schedule future events relative to CPU cycles?
3. How should DMA generate bus accesses without bypassing observability?

------

### 🔍 Observability & Tooling

1. What minimal trace event should be emitted per bus access?
2. How should structural device IDs be translated into human‑readable names?
3. How can tracing be enabled without impacting hot‑path performance?

------

# 🧠 Final Framing for the Issue

> **Goal:** Introduce a page‑based main bus architecture that cleanly separates CPU intent from memory and device behavior, supports Apple II compatibility semantics, enables future MMU features, and provides rich observability without compromising performance.

That’s a *strong* issue statement.

When you’re ready, the next natural step is to sketch:

- `PageEntry`
- `BusAccess`
- `IBusTarget`
- `MainBus.Read8/Read16/Read32`

…and let Copilot help you fill in the mechanical parts while you keep steering the architecture.

This is exactly the right moment to slow down and design.