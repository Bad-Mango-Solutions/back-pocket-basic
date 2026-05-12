# Disk Emulation Implementation Plan

Status: Draft for review
Author drafted by: planning pass — May 2026
Target: boot an unmodified Apple IIe Enhanced ROM into DOS 3.3 (Oregon Trail) and ProDOS (Ultima IV, hard disk titles) from period-correct images.

---

## 1. What we already have

The emulator is in much better shape for this work than a typical greenfield project, because the bus, slot, and scheduler infrastructure is already done and battle-tested by the keyboard, video, speaker, Language Card, Aux memory, Thunderclock, and PocketWatch RTC.

Concretely:

- **`ISlotCard` / `SlotManager` / `IOPageDispatcher`** already route `$C0n0-$C0nF` I/O, `$Cn00-$CnFF` slot ROM, and `$C800-$CFFF` expansion ROM correctly, including `$CFFF` deselect (`src/BadMango.Emulator.Bus/Interfaces/ISlotManager.cs`). This is exactly what Disk II / SmartPort cards need; we never have to touch slot plumbing.
- **`IScheduler` + `IScheduledDevice` + `IEventContext`** give us cycle-accurate, deterministic, priority-ordered event dispatch with `ScheduleAt`/`ScheduleAfter`/`Cancel` (`src/BadMango.Emulator.Bus/Scheduler.cs`). This is the right primitive for rotating a floppy at ~250 kbit/s and aging the data latch — no per-cycle polling needed.
- **`DiskIIControllerStub` + `DiskIIBootRomStub` + `DiskIIExpansionRomStub`** already exist (`src/BadMango.Emulator.Devices/DiskII*.cs`). The stub correctly wires all 16 `$C0nX` soft switches (phases 0-3 on/off, motor on/off, drive 1/2 select, Q6L/Q6H/Q7L/Q7H) and tracks phase/motor/drive/Q6/Q7 state, but reads always return `0xFF` and writes are dropped — there is no shift register, no track buffer, no image. The stub is the perfect skeleton to fill in.
- **Auto-discovery via `[DeviceType]` + `DeviceFactoryRegistry`** wires new slot cards into JSON profiles automatically — once a card has a parameterless ctor and the attribute, profile authors just write `{ "slot": 6, "type": "diskii" }` and it works.
- **`Pocket2eMachineBuilderExtensions.WithCard(slot, card)`** is the programmatic install path. We already use it for Thunderclock/PocketWatch.
- **Extensive design specs already exist** in `specs/os/`:
  - `Disk II Controller Device Specification.md` — soft switches, Q6/Q7, phase stepping, ROM entry points, timing, image formats, IP rules. Already aligned with the architecture.
  - `Disk Image Abstraction API Spec.md` — `IMedia` / `IMediaMetadata` / `IStorageBackend` design with `TwoImgDisk` exemplar.
  - `Disk Image Tooling APIs.md`
  - `SmartPort Specification.md` — full v2.0 protocol description.
  - `Unified Block Device Backing API for Apple II Emulator.md` — `IBlockDevice`, SmartPort/SCSI façades, slot rules.
  - `Technical Specification ProDOS-to-FATexFAT Compatibility Layer.md` and `ProDOS 8 Reference.md` / `DOS 3.3 Reference.md`.

The specs are good. We mostly need to execute against them, not redesign.

### What's missing

1. No storage / image abstraction in code (specs only).
2. The Disk II stub doesn't actually do anything when MOTORON goes high.
3. No SmartPort firmware, no 3.5", no hard disk / block device.
4. No way to attach an image file to a profile entry (`"slot": 6, "type": "diskii", "drives": [...]`).
5. No real Disk II P5A/P6A boot ROM is loaded (the stub fakes a 256-byte ROM). Without the real ROM, the IIe Monitor's `C600G` / boot trampoline can't execute. The IP rules in the spec say we ship a stub and require the user to supply a ROM.
6. UI: no way to mount/eject a disk from the running emulator.

---

## 2. Goals and non-goals

### In scope (must-have for "die from dysentery" / Ultima IV)

- 5.25" Disk II controller in slot 6 with two drives, exposing genuine GCR nibbles on `$C0EC` (Q6L/Q7L read) under correct rotational timing.
- Read/write support for `.dsk`, `.do`, `.po`, `.2mg` (sector-based), plus `.nib` (already-nibblized).
- Read-only `.woz` (WOZ1 + WOZ2) — needed for any disk with copy protection; many commercial titles use it.
- ProDOS/SmartPort hard disk in slot 7 (or 5) backed by `.po` / `.2mg` / `.hdv` block images. SmartPort firmware in the slot ROM, with the four ProDOS entry signature bytes correct so the IIe ROM's autostart will boot from it.
- A loader for the real Apple Disk II P5A boot ROM (256 bytes) supplied by the user via the existing ROM-image profile mechanism; until they supply one, the stub stays in place and boot is disabled with a clear message.
- Profile JSON support for attaching images at boot.
- UI hooks: mount/eject from the existing Avalonia UI; on-disk write-back (write-through, since these games will write to disk).
- Disassembly-friendly debug surface: drive state, current track / quarter-track, motor state, head position, last byte latched, drive LED state.

### Out of scope (for the first cut)

- SCSI card / Apple II SCSI driver (the spec calls out slot 7; we'll do SmartPort there first because it's simpler and ProDOS sees it identically).
- 13-sector DOS 3.2 (`.d13`).
- Copy-protected disk *creation* (we read WOZ; we don't write WOZ).
- GS/OS, ADTPro, FloppyEMU, network volumes.
- Apple Pascal disk layout (works through ProDOS-order images though, since they're the same blocks).

---

## 3. Architecture: how the pieces fit

```
                     ┌──────────────────────────────┐
                     │   IIe Enhanced Monitor ROM   │  (user-supplied)
                     │  $C600G  PR#6  PR#7 ...      │
                     └─────────────┬────────────────┘
                                   │
                  ┌────────────────┴─────────────────┐
                  │            MainBus               │
                  │   (slot dispatcher, soft switch) │
                  └────┬──────────────────────┬──────┘
                       │                      │
        ┌──────────────▼───────┐    ┌─────────▼───────────┐
        │ DiskIIController     │    │ SmartPortController │
        │  (slot 6)            │    │ (slot 5 or 7)       │
        │   - 16 soft switches │    │  - dispatch ROM     │
        │   - real P5A ROM     │    │  - ProDOS signature │
        │   - 2 drives         │    │  - n volumes        │
        └──────────┬───────────┘    └───────────┬─────────┘
                   │                            │
        ┌──────────▼──────────┐      ┌──────────▼─────────────┐
        │  Disk525Drive       │      │  SmartPortVolume       │
        │  - phase, head pos  │      │  - block-level         │
        │  - rotating bit ptr │      │  - 512-byte blocks     │
        │  - nibble track buf │      │                        │
        └──────────┬──────────┘      └──────────┬─────────────┘
                   │                            │
        ┌──────────▼──────────────────────────────▼──────────┐
        │            IMedia (logical block/track API)         │
        │  - I525Media (track-level, nibbles)                │
        │  - IBlockMedia (linear 512-byte blocks)            │
        └──────────────────────┬──────────────────────────────┘
                               │
        ┌──────────────────────▼──────────────────────────────┐
        │         IStorageBackend (bytes on disk/RAM)         │
        │  FileStorageBackend / RamStorageBackend / Cached   │
        └─────────────────────────────────────────────────────┘
```

Two media interfaces, not one. `IBlockMedia` is for 512-byte-block consumers (SmartPort, ProDOS hard disk, anything that doesn't care about nibbles). `I525Media` is for the Disk II controller, which needs nibble-level data per track and quarter-track addressing. Sector-based 5.25" images are adapted to `I525Media` by an on-the-fly nibblizer; raw `.nib` images present nibbles directly; `.woz` presents a real bitstream.

This is the same split the specs already proposed; we are just making the names concrete.

---

## 4. Phased plan

The plan is sequenced so each phase ends with something runnable and testable, and so we get to a bootable DOS 3.3 disk as early as possible.

### Phase 0 — Storage and image abstractions (no emulator changes)

New project: **`src/BadMango.Emulator.Storage/`** (referenced by `Devices`).

Add:
- `IStorageBackend` (Read/Write/Flush over `Span<byte>`).
  - `RamStorageBackend`, `FileStorageBackend`, `RamCachedStorageBackend` (write-through + write-back, dirty-bitmap based, modeled on the spec).
- `IBlockMedia` (`BlockSize`, `BlockCount`, `IsReadOnly`, `ReadBlock`, `WriteBlock`, `Flush`).
- `I525Media` (`TrackCount`, quarter-track API: `ReadTrack(int quarterTrack, Span<byte> nibbleBuf)`, `WriteTrack(...)`, `OptimalTrackLength`).
- `DiskGeometry` (35-track / 16-sector vs 40-track, etc).
- `DiskImageFactory` — magic-byte / extension sniffing, returns either `IBlockMedia`, `I525Media`, or both (sector-based images can present both views).

Image format implementations:
- `DosOrderImage` — `.dsk` / `.do`, 143360 bytes, applies the DOS 3.3 logical-to-physical interleave. Provides both `IBlockMedia` (via ProDOS-order reordering) and `I525Media` (via the runtime nibblizer).
- `ProdosOrderImage` — `.po`, 143360 bytes (linear). Same dual presentation as above; for `I525Media` it applies the inverse interleave before nibblizing.
- `TwoImgImage` — parses the 64-byte header, dispatches to DOS / ProDOS / Nibble per the format field, respects write-protect and volume flags. Used for `.2mg` and `.2img`.
- `NibImage` — 35 tracks × 6656 bytes raw nibbles. `I525Media` only.
- `WozImage` — WOZ1 + WOZ2 parser. Reads `INFO`, `TMAP`, `TRKS`. Maps `TMAP[qtrack]` to its track block, exposes the bitstream length per WOZ2. Read-only in phase 1. `I525Media` only; presents the bitstream packed into nibbles for the controller to read at the correct rate.
- `HdvImage` — raw 512-byte block dump (no header). `IBlockMedia` only.
- `D13Image` — *recognition only*. Throws "unsupported" with a friendly message at load time.

Runtime nibblizer (in `BadMango.Emulator.Storage.Encoding`):
- 6-and-2 GCR encode/decode tables.
- `NibblizeSector(buf16x256, volume, track, sector)` → fills the 343-byte data field.
- `BuildTrack(byte[16][256], volume, track, sectorOrder)` → produces the ~6,400-byte canonical nibble track with address fields, sync gaps, prologues, epilogues.
- `ParseTrack(nibStream)` → inverse, for sector image writes.

Tests live in `tests/BadMango.Emulator.Storage.Tests/`. We test:
- Round-trip of every sector through `BuildTrack` → `ParseTrack`.
- DOS 3.3 logical/physical mapping with the table from `specs/os/Disk II Controller Device Specification.md` §4.3.
- 2MG header parser against fixtures.
- WOZ TMAP fixture parse.
- Storage backend write-through / write-back semantics.

Acceptance for Phase 0: `DiskImageFactory.Open("oregon-trail.dsk")` returns an `I525Media` and an `IBlockMedia` that both pass property-based round-trip tests. No emulator wiring yet.

### Phase 1 — Real Disk II controller

Replace the body of `DiskIIControllerStub.cs` with a working implementation, and rename to `DiskIIController` (keep the stub class behind an `IFF_TESTING` shim only if existing tests still need it; simpler: update the tests).

Key components inside `BadMango.Emulator.Devices/DiskII/`:

- `DiskIIController : ISlotCard` (replaces stub).
- `Disk525Drive` — per-drive state: head quarter-track (0–139), spin position (byte offset within current track), motor state, write-protect, currently-mounted `I525Media`. Two of these live inside the controller; `DRIVE1` / `DRIVE2` selects which is active.
- `DiskIIRom : IBusTarget` — loads the user-supplied 256-byte P5A image at `$Cn00`. Until a real ROM is supplied, fall back to the existing `DiskIIBootRomStub` but log a clear warning and disable boot.
- `LSSEngine` — Logic State Sequencer model. For Phase 1 we do *not* implement the actual P6 PROM state machine; we use a simpler nibble-stream model that's accurate enough for unprotected commercial software (this is how AppleWin's "fast" Disk II mode works). When we add WOZ in Phase 2 we can either keep this or fold in a proper P6 emulation. The choice is parameterized.

Timing model (the important part):
- Motor on → schedule a single repeating event via `IScheduler.ScheduleAfter` that advances the rotational position by N nibbles every M cycles, where M ≈ 32 (one byte ≈ 32 CPU cycles at 1.023 MHz). When the byte is "ready" we update the latch register.
- Motor off → cancel the event.
- We don't actually fire an event per byte; we keep a `(lastUpdateCycle, lastPositionNibbles)` pair and compute the latch on demand inside the `$C0EC` read handler:
  ```
  elapsed = scheduler.Now - lastUpdateCycle
  bytesAdvanced = elapsed / cyclesPerByte
  position = (lastPosition + bytesAdvanced) % trackLength
  ```
  Plus a single "wake-up to invalidate the cache" event so we don't accumulate phantom drift while the CPU is in WAI. This is dramatically cheaper than per-byte scheduling and is what every modern Apple II emulator does.

Q6/Q7 dispatch table (from §2.3 of the Disk II spec):
- `Q6=0,Q7=0` → read latch (return the nibble at current position).
- `Q6=1,Q7=0` → read write-protect status (high bit = WP).
- `Q6=0,Q7=1` → write enable; data shifted on `$C0ED` writes.
- `Q6=1,Q7=1` → load write latch.

Phase stepper:
- Maintain `currentQuarterTrack` (0–139). Each phase on/off transition is logged; when two adjacent phases overlap we compute the quarter-track target and step.
- Clamp at 0 and at `2 * trackCount - 2` (so 35-track disks clamp around quarter-track 68; head bangs into stop, no advance).
- A track step costs ~3-12 ms IRL; we model it as immediate for now but schedule a settling delay of ~30 ms before the data latch returns valid data again. Boot ROM tolerates this.

Write path:
- `$C0ED` write while Q6=1, Q7=1 → push a nibble into the write shift register.
- Bytes are written into the current track buffer; we mark the track dirty.
- On motor off + drive deselect, or eject, dirty tracks are flushed: for `.dsk`/`.do`/`.po`/`.2mg` we re-parse nibble→sectors and write back the changed sectors only; for `.nib`/`.woz` we write the raw nibble buffer.

Profile JSON:
```json
{ "slot": 6, "type": "diskii",
  "config": {
    "rom": "library://roms/disk2-p5a.rom",
    "drives": [
      { "image": "library://disks/oregon-trail-side-a.dsk", "writeProtect": false },
      { "image": "library://disks/oregon-trail-side-b.dsk", "writeProtect": false }
    ]
  } }
```
Because the existing factory registry requires a parameterless ctor, we'll change to a *configured factory*: `RegisterSlotCardFactory("diskii", (builder, configNode) => new DiskIIController(...))`. This is a small extension to `DeviceFactoryRegistry` — about 30 lines — and matches the pattern the character ROM and 80-column card already need (they take a `config` block in the JSON; see `pocket2e-a2-enh.json`).

Tests:
- Soft-switch state machine (phases, motor, drive, Q6, Q7) — keep and extend the existing `DiskIIControllerStubTests`.
- "Track 0 sector 0 read" against a fixture image — assert we see the expected sequence of nibbles in `$C0EC` reads while the boot ROM polls them.
- "Boot a tiny test DOS image" — synthesize a 35-track image with a known first sector that contains `BRK` + identifying bytes; run a stub program that mimics the C600 ROM's `LDA $C0EC` loop; assert it reads the right nibbles.
- Phase stepper hits the right quarter-track for each sequence in the bring-up table.

Acceptance for Phase 1: with a user-supplied P5A ROM, the IIe ROM's `C600G` boot trampoline successfully reads boot block 0 from a DOS 3.3 `.dsk` and jumps into it. Oregon Trail boots to the title screen.

### Phase 2 — WOZ support and protection-tolerance

Most of the framework lands in Phase 1; this phase is the work to make copy-protected disks (Ultima IV is fine without it, but lots of period software needs it) read correctly.

- WOZ1 + WOZ2 chunk parser (already half-specified in the existing spec doc).
- Track block presents *bits* not nibbles; the controller has to do its own bit shifting (the LSS in the Disk II is essentially looking at MSB-first bits with rules about leading zeros). Replace the simple "nibble per ~32 cycles" model with a "bit per ~4 cycles" model for WOZ-backed disks. Sector-backed (`.dsk`/etc.) disks continue to use the simpler nibble model.
- Optional: P6 PROM state-machine emulation. Public PROM dumps are around and the spec is documented. Not strictly necessary if we keep the nibble-stream fast path for unprotected disks.
- Quarter-track fidelity: WOZ TMAP can point multiple quarter-tracks at the same track block, or at "no media" for unformatted areas. Honor it.

Acceptance for Phase 2: Ultima IV `.woz` boots and saves. A `.woz` of a known-protected disk passes its protection check.

### Phase 3 — SmartPort hard disk

We do SmartPort instead of SCSI because:
- ProDOS sees them identically (block reads via the standard entry point).
- SmartPort is simpler — no SCSI CDBs, no LUNs/IDs beyond unit numbers, no card-resident OS.
- The IIe Enhanced ROM's autostart will pick up any card whose slot ROM has the four ProDOS signature bytes; SmartPort cards qualify.

New code in `src/BadMango.Emulator.Devices/SmartPort/`:

- `SmartPortController : ISlotCard` (default slot 5; configurable to slot 7).
- `SmartPortFirmwareRom : IBusTarget` — a tiny ROM that contains:
  - At offset `$00`: `$A9 $20`, at `$03`: `$00`, at `$05`: `$03`, at `$07`: `$00` — the four ProDOS-block-device signature bytes the IIe ROM looks for.
  - At offset `$FA-$FF`: ProDOS dispatch byte and SmartPort dispatch byte (entry = ProDOS+3, per the SmartPort spec §3.1).
  - The actual dispatch entry points are trampolines that fall through to a magic instruction — we'll use a single illegal opcode + a `TrapRegistry` entry (the bus already has trap support) so we can hand control to managed code instead of writing 6502 inside ROM. Alternatively, a small hand-written 6502 stub that just calls the SmartPort command handler we register on a softswitch in the card's I/O page. We'll pick the trap approach — it's the same mechanism already used for the GameIOController and produces no copyrighted ROM bytes.
- `SmartPortCommandHandler` — implements the seven SmartPort commands we care about: `STATUS` ($00), `READ_BLOCK` ($01), `WRITE_BLOCK` ($02), `FORMAT` ($03), `CONTROL` ($04), `INIT` ($05), `READ` ($08), `WRITE` ($09). Reads cmd byte and parameter list from the stack-passed inline parameters (`PLA` + `PLA` to get return address, parameters follow). Validates unit number, dispatches to `IBlockMedia` on the target volume, returns error code in A with carry-clear-on-success.
- Each `SmartPortVolume` wraps an `IBlockMedia` and a unit number.

Profile JSON:
```json
{ "slot": 7, "type": "smartport",
  "config": {
    "volumes": [
      { "unit": 1, "image": "library://disks/u4-prodos.2mg" },
      { "unit": 2, "image": "library://disks/ultima4-master.po" }
    ]
  } }
```

Disk II in slot 6 + SmartPort in slot 7 boots ProDOS off slot 7 by default on the IIe (highest slot wins for autostart).

Tests:
- SmartPort STATUS returns correct block count.
- READ_BLOCK 0 of a known image returns the right bytes.
- ProDOS-style autobootable image (we can use a free ProDOS 2.4.3 boot image — DOS 3.3 is Apple's; ProDOS 2.x is freely redistributable under Apple's 2016 license).

Acceptance for Phase 3: with a real IIe ROM + DiskII P5A ROM + a ProDOS 2.4.3 boot image attached to slot 7, the machine boots straight into the ProDOS launcher, and `BLOAD`/`CATALOG` work. Ultima IV from a `.2mg` mounts and boots.

### Phase 4 — UniDisk 3.5" (optional polish)

3.5" UniDisk drives in the Apple IIc/IIgs use the SmartPort protocol natively — they look like 1600-block (800K) SmartPort volumes. If we have SmartPort done, **we already support 3.5" disks** as long as the image is `.po`/`.2mg`/`.hdv` with the right block count.

The only thing missing is reading legacy MFM-format 3.5" `.dsk` images and `.woz` 3.5" bitstreams (WOZ 2.x supports 3.5" tracks). Those are very rare for IIe-era software; we'll defer them.

### Phase 5 — UI integration

- Avalonia: a "Disks" menu with mount/eject per drive, recent disks, write-protect toggle, "create blank ProDOS 800K" / "create blank DOS 3.3 5.25"" actions.
- Drive LED display in the status bar (lights up while motor is on).
- Drag-and-drop a disk file onto the window.

This is fairly mechanical UI work; I'll spec it more thoroughly when we get there.

---

## 5. Concrete file-by-file work list

Net-new files (rough — final names land in PR):
- `src/BadMango.Emulator.Storage/BadMango.Emulator.Storage.csproj`
- `src/BadMango.Emulator.Storage/IStorageBackend.cs`
- `src/BadMango.Emulator.Storage/RamStorageBackend.cs`
- `src/BadMango.Emulator.Storage/FileStorageBackend.cs`
- `src/BadMango.Emulator.Storage/RamCachedStorageBackend.cs`
- `src/BadMango.Emulator.Storage/IBlockMedia.cs`
- `src/BadMango.Emulator.Storage/I525Media.cs`
- `src/BadMango.Emulator.Storage/DiskGeometry.cs`
- `src/BadMango.Emulator.Storage/DiskImageFactory.cs`
- `src/BadMango.Emulator.Storage/Images/{DosOrderImage,ProdosOrderImage,TwoImgImage,NibImage,WozImage,HdvImage,D13Image}.cs`
- `src/BadMango.Emulator.Storage/Encoding/{GcrTables,Nibblizer,SectorOrder}.cs`
- `src/BadMango.Emulator.Devices/DiskII/{DiskIIController,Disk525Drive,DiskIIRom,LSSEngine}.cs`
- `src/BadMango.Emulator.Devices/SmartPort/{SmartPortController,SmartPortFirmwareRom,SmartPortCommandHandler,SmartPortVolume}.cs`
- `profiles/pocket2e-a2-enh-disk.json` (new profile preset that includes Disk II + SmartPort)
- Tests under `tests/BadMango.Emulator.Storage.Tests/` and additions to `tests/BadMango.Emulator.Devices.Tests/`.

Modified:
- `src/BadMango.Emulator.Devices/DeviceFactoryRegistry.cs` — support `Func<MachineBuilder, JsonElement, ISlotCard>` configured factories.
- `src/BadMango.Emulator.Bus/MachineBuilder.FromProfile.cs` — pass the `config` node through to slot card factories.
- `schemas/machine-profile.schema.json` — add `diskii` and `smartport` shapes.
- `src/BadMango.Emulator.Devices/DiskIIControllerStub.cs` — replace contents (or delete; new `DiskIIController` supersedes).
- `src/BadMango.Emulator.Devices/DiskII{Boot,Expansion}RomStub.cs` — keep as fallbacks when no user ROM is supplied; rename to make their role explicit.

Existing tests to update:
- `DiskIIControllerStubTests.cs` — adapt to `DiskIIController`; keep all the state-machine assertions and add image-aware ones.

---

## 6. ROM/IP handling

The Disk II P5A boot ROM (256 bytes), the IIe Enhanced Monitor ROM, and the SmartPort firmware (if we ever ship a real one) are all Apple-copyrighted. The Disk II spec already lays out the rules:

- We **do not** ship any of these bytes.
- We ship stubs that fail closed with a clear message ("No Disk II boot ROM configured; the controller is present but cannot boot. Set `config.rom` in your profile to a 256-byte P5A image.").
- The profile JSON already supports `library://` paths and `embedded://` resources — the user drops their P5A dump into the library root, the profile points at it, the loader validates the length (and optionally a known checksum), and we're off.
- For SmartPort we generate our *own* dispatch ROM (clean room: signature bytes + minimal stubs + trap handoff). That ROM contains only behavior that's part of the public SmartPort interface contract; we don't copy Apple's bytes.

The clean-room guidance in §3.5 of the Disk II spec applies if we ever want to ship a working open-source replacement P5A. That's a follow-up project.

---

## 7. Risks and where I'd want a second look

1. **Timing accuracy for fast nibble-mode disks.** The `lastUpdateCycle + cyclesPerByte` trick works for nearly all DOS 3.3 and ProDOS software. It will *not* work for tightly-protected disks that count cycles between nibbles. Phase 2's WOZ bit-stream path handles those — but if Ultima IV is shipped on a `.dsk` rather than `.woz` and it does have a check, we'll need Phase 2 first. Worth checking which dump format you have.

2. **Replacing the stub vs. evolving it.** The stub is well-tested for soft-switch state. The cleanest path is to keep `DiskIIControllerStub` as a no-image fallback (for tests that just want the card present) and add `DiskIIController` as a new class. Both implement `ISlotCard` exactly the same way. Profile JSON picks based on whether `config.drives` is present.

3. **The configured-factory change.** Today's `DeviceFactoryRegistry` requires a parameterless constructor. We need a way to plumb the `config` node. The 80-column card already takes config (`expansion-rom`); look at how that's currently done before I commit to the API shape — if it goes through a side channel, we should reuse that channel rather than invent a new one.

4. **Profile schema migration.** Adding `config.drives` is additive, so old profiles keep working. But we should update `schemas/machine-profile.schema.json` so editor validation doesn't lie.

5. **Disk II quarter-track count.** Some IIe titles use up to 40 tracks. Our 35-track clamp is the safe default but we should keep `geometry.trackCount` configurable per-image.

6. **Write-back semantics for `.woz`.** Real WOZ writes are dangerous because the format encodes bitstream timing. Phase 2 keeps WOZ read-only. Save-game-bearing WOZ titles (rare on 5.25") would need Phase 6 work.

7. **CPU sync.** The current scheduler is driven by CPU cycle advance signals — that's perfect for us. The one thing to double-check is that when the CPU is halted in the debugger, our scheduled motor-tick events also pause. Reading `Scheduler.cs` it looks like they do (no wall-clock anywhere), but worth confirming with a debugger-pause test.

---

## 8. Suggested execution order

Given the goal ("die from dysentery" + Ultima IV), I'd actually do this:

1. Phase 0 storage layer (1–2 days of work).
2. Phase 1 Disk II controller, sector-image path only, no `.woz` yet (3–5 days). At the end of Phase 1, Oregon Trail boots from `.dsk` or `.do`.
3. Sanity-check Ultima IV. If your Ultima IV image is `.2mg` ProDOS-order: it likely boots without Phase 2. If it's `.woz` or `.nib` and uses the protection check: jump to Phase 2.
4. Phase 3 SmartPort (3–4 days). Now ProDOS hard disks work; Ultima IV from a 32MB `.2mg` is trivially mountable.
5. Phase 2 WOZ on demand.
6. Phase 5 UI when the rest is solid.

This sequencing gets you to "playing Oregon Trail with friends" inside about a week of focused work, and "Ultima IV from hard disk" in under two.
