# PRD: Disk Emulation for Pocket2e

| Field | Value |
|---|---|
| Status | Draft for engineering review |
| Owner | Josh |
| Companion plan | [`DISK-IMPLEMENTATION-PLAN.md`](./DISK-IMPLEMENTATION-PLAN.md) |
| Related specs | `specs/os/Disk II Controller Device Specification.md`, `specs/os/Disk Image Abstraction API Spec.md`, `specs/os/SmartPort Specification.md`, `specs/os/Unified Block Device Backing API for Apple II Emulator.md` |

---

## 1. Summary

Add full emulated disk-storage support to the Pocket2e emulator so that an unmodified Apple IIe-Enhanced ROM can boot from 5.25" floppy images (Disk II), 3.5" floppy images (UniDisk via SmartPort), and ProDOS hard-disk images (SmartPort). The emulator must read/write the common Apple II image formats (`.dsk`, `.do`, `.po`, `.2mg`, `.hdv`, `.nib`, and eventually `.woz`), support runtime insert/eject of removable media from a debug console command (and later the Avalonia UI), and offer a console command to author blank disk images (uninitialized / initialized / bootable). Profile-driven storage configuration declares controllers, drives, and non-ejectable media; ejectable media is mounted/unmounted at runtime.

## 2. Goals

| # | Goal | Acceptance |
|---|---|---|
| G1 | Boot DOS 3.3 from a 5.25" `.dsk`/`.do`/`.po`/`.2mg` image. | A user-supplied DOS 3.3 master boots to Applesoft prompt in slot 6, drive 1. |
| G2 | Boot ProDOS from a 5.25" image (Disk II). | A user-supplied ProDOS 2.4.3 5.25" boot disk boots to the launcher. |
| G3 | Boot ProDOS from a hard-disk image (SmartPort, slot 7). | A user-supplied ProDOS 2.4.3 hard-disk image (`.po`/`.2mg`/`.hdv`, ≤ 32 MB) autoboots when no 5.25" disk is present. |
| G4 | Boot ProDOS from a 3.5" UniDisk image (SmartPort 800 KB). | A user-supplied ProDOS 2.4.3 800 KB image boots. |
| G5 | Read `.woz` (WOZ 1 / WOZ 2) bitstream images well enough to boot Ultima IV. | A WOZ image of a known-protected commercial title boots and passes its protection check. |
| G6 | Insert/eject removable media at runtime via a debug console command. | `disk insert <slot>:<drive> <path>` and `disk eject <slot>:<drive>` succeed against a running machine. |
| G7 | Create blank disk images from the debug console. | `disk create` produces uninitialized, ProDOS/DOS-initialized, or bootable images at correct sizes. |
| G8 | Storage subsystem is mockable. | Every disk-aware emulator path takes its dependency through an interface (`IBlockMedia`, `I525Media`, `IStorageBackend`, `IDiskController`); tests can use Moq without filesystem I/O. |

## 3. Non-goals (for v1)

- 13-sector DOS 3.2 (`.d13`) image decoding — recognize and reject with a clear message.
- WOZ *writes* / WOZ authoring (Phase 5 keeps WOZ read-only).
- Apple II SCSI card emulation.
- Apple Pascal native filesystem inspection.
- GS/OS, ADTPro, FloppyEMU, network volumes.
- Disk image *creation* in GUI (debug console only for v1).
- Copy-protection authoring tools.

## 4. Users and primary scenarios

| Persona | Scenario |
|---|---|
| Retro gamer (Josh's friends) | "Mount Oregon Trail and boot it" → `disk insert 6:1 oregon-trail.dsk`, run, die from dysentery. |
| Retro gamer (Josh) | "Boot Ultima IV from hard disk" → profile declares a ProDOS HDV in slot 7, autoboot lands on the launcher. |
| Emulator developer | "Test that the Disk II controller correctly latches the volume byte in the address field" → unit test instantiates `DiskIIController` with a mocked `I525Media`, asserts shift-register reads. |
| Power user | "Make me a blank 800 KB ProDOS-formatted volume" → `disk create --size 800K --format prodos --label WORK out.po`. |

## 5. Priority order (drives delivery sequencing)

This order supersedes the phasing in the companion plan. WOZ moves *after* SmartPort + UniDisk.

| Priority | Capability | Image formats |
|---|---|---|
| P0 | Bootable DOS 3.3 Disk II 5.25" | `.dsk`, `.do`, `.po`, `.2mg`, `.nib` |
| P1 | Bootable ProDOS Disk II 5.25" | (same set) |
| P2 | Bootable ProDOS Hard Disk (SmartPort, slot 7) | `.po`, `.2mg`, `.hdv` |
| P3 | Bootable ProDOS 3.5" UniDisk (SmartPort, slot 5 or 7) | `.po`, `.2mg`, `.hdv` (800 KB / 1600 blocks) |
| P4 | WOZ bitstream images | `.woz` (WOZ 1 + WOZ 2) |
| P5 | Avalonia UI (mount/eject menus, drive LEDs) | — |

For P0–P3, a freely-redistributable bootable utility disk (ProDOS 2.4.3, blank-format master, etc.) is an acceptable demonstration; we do not need to ship Apple-copyrighted images.

## 6. Functional requirements

### 6.1 Storage abstraction (new `BadMango.Emulator.Storage` assembly)

- **FR-S1** `IStorageBackend` — byte-level random-access over `Span<byte>` / `ReadOnlySpan<byte>` with `Read`, `Write`, `Flush`, `Length`, `CanWrite`. Implementations: `RamStorageBackend`, `FileStorageBackend`, `RamCachedStorageBackend` (write-through and write-back modes, dirty-block bitmap).
- **FR-S2** `IBlockMedia` — block-level API (`BlockSize`, `BlockCount`, `ReadBlock`, `WriteBlock`, `IsReadOnly`, `Flush`).
- **FR-S3** `I525Media` — 5.25" track-level API. Exposes quarter-track addressing (0…139), `ReadTrack`, `WriteTrack`, `OptimalTrackLength`, `IsReadOnly`, `Flush`, plus a `Geometry` (track count, sector count, sector order).
- **FR-S4** `DiskImageFactory.Open(path)` returns the appropriate media interface(s). Format detection by extension first, then magic-byte sniffing for headered formats. Returns a strongly-typed result that callers can pattern-match on.
- **FR-S5** Image readers for: `.dsk`, `.do`, `.po`, `.2mg`/`.2img`, `.nib`, `.hdv`, `.woz` (P4). `.d13` is recognized and refused with a clear error.
- **FR-S6** Sector-image adapters expose **both** `I525Media` (via runtime GCR 6-and-2 nibblizer applying the correct DOS↔ProDOS sector skew) and `IBlockMedia` (via the inverse reordering). Calling code chooses which view it needs.
- **FR-S7** Write-back to file-backed images uses write-through by default; configurable to write-back with explicit flush. Write-protect honored from image flags (e.g. 2MG flag bit 0) and from runtime mount flags.

### 6.2 Disk II controller

- **FR-D1** Replace the body of `DiskIIControllerStub` with a working `DiskIIController : ISlotCard`. The stub remains in the codebase for tests that want a no-image card; the new controller is selected when the profile entry provides a `config.drives` block.
- **FR-D2** Two drives per controller (`drive 1`, `drive 2`). Each drive holds: head quarter-track (0–139), spin position (byte offset within current track), motor state, write-protect, currently-mounted `I525Media` (nullable for empty drive).
- **FR-D3** All 16 soft switches at `$C0n0–$C0nF` retain their current state-machine semantics and additionally drive the data path. Soft-switch table from `Disk II Controller Device Specification.md` §2.2 is normative.
- **FR-D4** Phase stepper updates the head position on valid phase-overlap sequences, clamps at track 0 and `2 * geometry.trackCount - 2`. Invalid sequences produce no movement.
- **FR-D5** Q6/Q7 dispatch:
  - `Q6=0, Q7=0`: read data latch (nibble at current spin position).
  - `Q6=1, Q7=0`: read write-protect status (high bit = WP).
  - `Q6=0, Q7=1`: enable write mode; subsequent `$C0nD` writes shift bits onto the track.
  - `Q6=1, Q7=1`: load write latch with shift-register contents.
- **FR-D6** Timing model uses an on-demand recompute: on `$C0nC` read, advance spin position by `(scheduler.Now - lastUpdateCycle) / cyclesPerByte` bytes, modulo track length. A single rescheduling event prevents drift while CPU is halted. **No per-cycle polling.**
- **FR-D7** Motor on/off is software-controlled with no automatic timeout. Motor on starts a settling timer (~1 ms) before valid data is returned. Track step adds a small settling delay (~30 ms emulator-time) during which reads return the last-latched byte.
- **FR-D8** Writes to nibble-backed images write nibbles directly. Writes to sector-backed images mark the track dirty; on motor-off, drive-deselect, eject, or `Flush`, dirty tracks are parsed back into sectors and written to the underlying `IStorageBackend`. Parse failures are surfaced as warnings (the image still gets the nibble payload via the nibble cache).
- **FR-D9** Boot ROM (`$Cn00–$CnFF`) is loaded from a user-supplied 256-byte P5A image via the profile's existing `rom-images` mechanism. When unset, the controller falls back to the existing `DiskIIBootRomStub` and logs a single clear warning; boot will not succeed but the rest of the emulator continues to run.
- **FR-D10** Controller advertises itself in `IDeviceRegistry` with a path like `Slot/6/DiskII`, and exposes (for debug surfaces) per-drive `motor`, `phase`, `quarterTrack`, `selected`, `writeProtect`, `mountedImagePath`.

### 6.3 SmartPort controller (P2 + P3)

- **FR-SP1** `SmartPortController : ISlotCard`. Default slot 7 for hard-disk usage; configurable to slot 5 for 3.5"/UniDisk usage.
- **FR-SP2** Slot ROM at `$Cn00–$CnFF` contains the four ProDOS block-device signature bytes (`$Cn01=$20`, `$Cn03=$00`, `$Cn05=$03`, `$Cn07=$00`) and a dispatch trampoline whose ProDOS entry sits at `$CnFC` and whose SmartPort entry sits at `(ProDOS entry) + 3` per `SmartPort Specification.md` §3.1. The dispatcher is implemented via a `TrapRegistry` entry on an illegal opcode (same pattern as `GameIOController`); managed code reads inline parameters and dispatches to `SmartPortCommandHandler`.
- **FR-SP3** Commands implemented: `STATUS` ($00), `READ_BLOCK` ($01), `WRITE_BLOCK` ($02), `FORMAT` ($03), `CONTROL` ($04), `INIT` ($05), `READ` ($08), `WRITE` ($09). Errors return SmartPort error codes per spec §5; success returns carry-clear, `A=0`.
- **FR-SP4** ProDOS legacy entry (READ_BLOCK / WRITE_BLOCK via the ProDOS dispatch byte) is supported for hard-disk autoboot. The IIe Enhanced ROM autoboot scans high slots first, so SmartPort in slot 7 will boot ahead of Disk II in slot 6 unless a 5.25" disk is mounted with motor pending.
- **FR-SP5** Volume capacity ≤ 32 MB (65 535 ProDOS blocks) per volume. Up to 8 units per controller. Unit 0 returns bus-level `STATUS`.
- **FR-SP6** All ROM bytes are originally authored for this project (signature bytes + trampoline + trap opcode). No Apple-copyrighted ROM content ships.

### 6.4 Profile JSON configuration

- **FR-C1** Extend `SlotCardProfile` to carry an optional `Config` `JsonElement?` (matching the existing `MotherboardDeviceEntry.Config` shape).
- **FR-C2** `MachineBuilder.FromProfile.ConfigureSlotCard` passes the config blob through to the registered slot-card factory. Update `DeviceFactoryRegistry` to accept a `Func<MachineBuilder, JsonElement?, ISlotCard>` overload while preserving the existing parameterless-ctor path for backward compatibility.
- **FR-C3** Disk II profile shape:
  ```json
  { "slot": 6, "type": "diskii",
    "config": {
      "rom": "disk2-p5a-rom",
      "drives": [
        { "unit": 1, "image": "library://disks/oregon-trail.dsk", "writeProtect": false },
        { "unit": 2 }
      ]
    } }
  ```
  `image` is optional (empty drive); `rom` is the name of a `rom-images` entry; `writeProtect` defaults to false.
- **FR-C4** SmartPort profile shape:
  ```json
  { "slot": 7, "type": "smartport",
    "config": {
      "volumes": [
        { "unit": 1, "image": "library://disks/prodos-243.po", "removable": false },
        { "unit": 2, "image": "library://disks/work-volume.2mg", "removable": false }
      ]
    } }
  ```
  Hard-disk volumes default to `removable: false` (non-ejectable). UniDisk 3.5" volumes set `removable: true` and may omit `image` (empty drive).
- **FR-C5** Removable media may be declared in the profile *or* mounted at runtime. Non-removable (hard-disk) media must be declared in the profile.
- **FR-C6** Update `schemas/machine-profile.schema.json` so editors validate the new shapes.

### 6.5 Debug console commands

All `disk` subcommands live in `BadMango.Emulator.Debug.Infrastructure/Commands/DeviceCommands/` (next to the existing per-device command folders) and implement `ICommandHandler`. Each subcommand class carries `[DeviceDebugCommand]` so the existing Autofac `DeviceDebugCommandsModule` auto-discovers and registers it. Subcommands that operate on a running machine resolve the live controllers via `ICommandContext` / `IDebugContext`; `disk create` and `disk info` do not require a running machine and resolve only `DiskImageFactory` + `IDebugPathResolver`.

Subcommands implemented eagerly:

- **FR-DC1** `disk create <path> [--size 5.25|3.5|32M|<blocks>] [--format raw|dos33|prodos] [--bootable <bootimage>] [--volume-name <name>] [--volume-number <n>]` — **lands first** (issue 1 in §11). Produces test images well before any controller code exists.
  - `raw` (default): zero-filled image at the requested geometry. Container chosen from extension (`.dsk`/`.do`/`.po`/`.2mg`/`.hdv`).
  - `dos33`: writes a DOS 3.3 VTOC and catalog track on a 35-track 5.25" image. `--volume-name` is ignored; `--volume-number` defaults to 254.
  - `prodos`: writes a ProDOS volume directory + bitmap sized for the chosen geometry. `--volume-name` defaults to `BLANK`.
  - `--bootable <bootimage>`: copies the boot sector/blocks from `<bootimage>` (any format `DiskImageFactory` accepts) into the new image. Used by tests and by power users; the source image is *not* required to be one of Apple's — a freely-redistributable ProDOS 2.4.3 master is the expected reference.
  - Output is round-trippable: `DiskImageFactory.Open(path)` must accept whatever `disk create` wrote.
- **FR-DC2** `disk info <path>` — without mounting anything, reports the format `DiskImageFactory` would pick, geometry, write-protect flag, and (for 2MG) header metadata. Lands alongside `disk create` because every `disk create` test will want to round-trip through `disk info`.

Subcommands implemented just-in-time, gated by emulator support arriving:

- **FR-DC3** `disk list` — prints every installed controller, drive/unit, mount state, write-protect, image path, geometry. Lands with issue 6 (`DiskIIController`).
- **FR-DC4** `disk insert <slot>:<drive> <path> [--write-protect]` — mounts a removable image at runtime. Fails with a clear error for non-removable units and for image-format mismatches (e.g. a 5.25" image in a 3.5" drive). Lands with issue 6 (Disk II) and extends naturally to SmartPort 3.5" with issue 11.
- **FR-DC5** `disk eject <slot>:<drive>` — flushes dirty state, dismounts. Fails clearly for non-removable units. Lands with issue 6.
- **FR-DC6** `disk flush <slot>:<drive>` — force-flush without ejecting. Lands with issue 6.

### 6.6 Initialization, mount, and runtime safety

- **FR-R1** Mounting an image is async-safe with respect to the running CPU — implemented by deferring the swap to the next scheduler turn, so the controller never observes a half-mounted drive mid-byte.
- **FR-R2** Ejecting always flushes dirty tracks first. If flush fails (e.g. read-only file underneath), the eject is rejected with an error and the image stays mounted.
- **FR-R3** Hot-swapping resets per-drive state (spin position, dirty cache, latch) so the next sector read starts cleanly.
- **FR-R4** Insertions during an active motor cycle are allowed but reset the settling timer.

## 7. Testing requirements

Mockability is a first-class requirement. Every observable interaction with storage goes through an interface; every observable interaction with the bus or scheduler goes through interfaces the project already has.

- **FR-T1** `IBlockMedia`, `I525Media`, `IStorageBackend`, and a new `IDiskController` interface all have Moq-friendly virtual surfaces (no sealed types on the seam, no static singletons).
- **FR-T2** **Unit-test seams**:
  - `DiskIIController` accepts an `I525Media` (or factory) and an `IEventContext` so tests construct it without any filesystem.
  - `SmartPortController` accepts an `IReadOnlyList<IBlockMedia>` keyed by unit number.
  - `LSSEngine` / nibblizer / WOZ parser are pure types with no side effects.
- **FR-T3** Existing `DiskIIControllerStubTests` continue to pass against the stub; a parallel `DiskIIControllerTests` covers the real controller using mocked media.
- **FR-T4** **Storage unit tests**: GCR 6-and-2 round-trip per sector, DOS-vs-ProDOS skew mapping, 2MG header parser, WOZ TMAP parser (P4), backend write-through/write-back semantics.
- **FR-T5** **Controller unit tests**: phase stepper hits the right quarter-track for each phase sequence; motor settling delay returns last-latched byte; Q6/Q7 dispatch table; address-field nibble sequence visible on `$C0EC` for a synthetic image.
- **FR-T6** **Boot integration tests** (one per priority):
  - P0: synthetic minimal DOS 3.3 image — assert CPU lands in `$0801` with VTOC reachable.
  - P1: synthetic minimal ProDOS 5.25" image — assert ProDOS dispatcher signature visible.
  - P2: synthetic ProDOS HDV — boot from slot 7, assert dispatcher signature.
  - P3: synthetic 1600-block UniDisk image — boot via SmartPort.
- **FR-T7** **SmartPort command tests**: each command exercised against an `IBlockMedia` mock; parameter-list decoding asserted; error codes asserted.
- **FR-T8** **Debug-console tests**: each `disk` subcommand exercised in isolation against mocked controllers; `disk create` outputs validated by re-opening the produced image through `DiskImageFactory`.

## 8. Non-functional requirements

- **NFR-1** Disk reads must not regress baseline CPU throughput by more than 2 % for non-disk workloads (timing is event-driven, no per-cycle polling).
- **NFR-2** Mount/eject latency under 50 ms for a 32 MB image on a typical dev machine.
- **NFR-3** No allocations on the hot path (`$C0EC` reads). Use stackalloc / pooled buffers.
- **NFR-4** Determinism: identical inputs (image bytes + CPU instruction stream + scheduler ordering) produce identical disk reads. Critical for reproducible debugging.
- **NFR-5** All public types ship XML doc comments; new code respects existing StyleCop rules.
- **NFR-6** No Apple-copyrighted ROM bytes ship in the repository; the loader validates lengths (and optionally CRC) of user-supplied ROM images.

## 9. Out-of-band requirements

- **NFR-7** Document the new profile shapes in `specs/reference/Machine Profile Configuration Guide.md`.
- **NFR-8** Add at least one example profile that boots the project's chosen test image set under `profiles/`.
- **NFR-9** Add a `disk` section to whatever README/CHANGELOG governs user-facing changes.

## 10. Resolved design decisions

The following points were initially open and are now decided. Future revisions of this PRD should treat them as fixed unless explicitly reopened.

1. **Configured-factory shape — DECIDED.** `DeviceFactoryRegistry` exposes a single slot-card factory delegate of shape `Func<MachineBuilder, JsonElement?, ISlotCard>`. Existing parameterless registrations are adapted to ignore the config argument. One pattern, no overload soup.
2. **Debug-console host — DECIDED.** All `disk` subcommands live in `BadMango.Emulator.Debug.Infrastructure/Commands/DeviceCommands/` (mirroring the existing `DeviceCommands` folder), each implements `ICommandHandler`, and each carries `[DeviceDebugCommand]`. They are auto-registered by the existing Autofac `DeviceDebugCommandsModule`. No new registrar is introduced.
3. **WOZ writability — DECIDED.** Read-only in P4. Sidecar `.nib` write-back is out of scope.
4. **Block image size limits — DECIDED.** Single ProDOS volume per device in v1; cap at 32 MB / 65 535 blocks. APM-partitioned multi-volume `.2mg` is out of scope.
5. **`.dsk` ordering auto-detect — DECIDED.** When the extension is `.dsk` (ambiguous), sniff the image: read sector at the track-17 / block-2 location, look for a DOS 3.3 VTOC signature first, then a ProDOS root-directory signature. Fall back to DOS order on inconclusive sniff. Surface the chosen order through `disk info`.

## 11. Issue breakdown (suggested)

Each row is a candidate GitHub issue. Order is intentional: `disk create` + `disk info` land first so every downstream issue can author its own test images, then the storage abstraction, then controllers in priority order, then everything else.

| # | Title | Acceptance |
|---|---|---|
| 1 | Add `BadMango.Emulator.Storage` project with `IStorageBackend` + backends | Unit tests pass for RAM / file / cached backends, including write-through and write-back. |
| 2 | Add `IBlockMedia`, `I525Media`, `DiskGeometry`, `DiskImageFactory` (raw-only writers for `.dsk`/`.do`/`.po`/`.2mg`/`.hdv`/`.nib`) | Factory opens and round-trips fixture images of every supported sector/block format. |
| 3 | Implement GCR 6-and-2 nibblizer + DOS/ProDOS skew tables + `.dsk` ordering sniffer | Round-trip property tests pass for every (volume, track, sector) triple; ambiguous `.dsk` resolves to the correct order against DOS- and ProDOS-formatted fixtures. |
| 4 | Implement `disk create` + `disk info` debug commands (raw / dos33 / prodos / bootable) | New images are recognized by `DiskImageFactory`; `disk info` reports the right format, geometry, and (for `.dsk`) sniffed ordering. **Tests for issues 5+ can now author their own fixture images via the console.** |
| 5 | Extend `SlotCardProfile`/`DeviceFactoryRegistry` to plumb `config` JSON to slot-card factories (single `Func<MachineBuilder, JsonElement?, ISlotCard>` shape) | `Extended80Column` migration test still green; new `diskii` factory consumes config. |
| 6 | Implement `DiskIIController` replacing stub guts; keep stub for the no-config / no-image factory path | Controller unit tests + synthetic-boot test pass against mocked `I525Media`. |
| 7 | Implement `disk list` / `disk insert` / `disk eject` / `disk flush` debug commands against `DiskIIController` | Commands operate on live controllers; happy + error paths covered by tests using mocked controllers. |
| 8 | Wire DOS 3.3 P0 boot path end-to-end | A `disk create --format dos33 --bootable <user-supplied DOS-3.3-master.dsk>` image boots to the Applesoft prompt in slot 6. |
| 9 | Wire ProDOS P1 boot path end-to-end (Disk II 5.25") | A ProDOS 2.4.3 5.25" image boots from slot 6. |
| 10 | Implement `SmartPortController` + SmartPortFirmwareRom (clean room) + dispatcher trap | SmartPort command unit tests pass; STATUS + READ_BLOCK against a mocked `IBlockMedia` work. |
| 11 | Wire ProDOS hard-disk P2 boot path end-to-end | A ProDOS 2.4.3 HDV in slot 7 autoboots. Single-volume, ≤ 32 MB. |
| 12 | Extend `disk insert` / `disk eject` to SmartPort 3.5" UniDisk volumes (removable units only) | Mount/eject works through the same console commands; non-removable units reject mount/eject with a clear error. |
| 13 | Wire UniDisk 3.5" P3 boot path | An 800 KB ProDOS image boots through SmartPort. |
| 14 | Add WOZ (WOZ 1 + WOZ 2) reader; bit-stream LSS path in `DiskIIController` (read-only) | A known WOZ fixture boots; read-only protection checks pass. Writes to WOZ-backed drives surface a clear "read-only" error. |
| 15 | Update `schemas/machine-profile.schema.json` and `Machine Profile Configuration Guide.md` | Schema validates new `diskii` and `smartport` shapes; guide documents them. |
| 16 | Add example profiles (`pocket2e-a2-enh-disk.json`, `pocket2e-a2-enh-hd.json`) | Profiles boot their declared images end-to-end against user-supplied ROMs. |
| 17 | (Future) Avalonia mount/eject UI + drive LEDs | Out of scope for this PRD beyond the controller hooks already required. |

**Bootstrapping note for issue 4.** Until issue 6 lands, `disk create --bootable` requires a `<bootimage>` argument (a real master disk supplied by the user); after issue 6 ships, the test suite can produce bootable test images from a single user-supplied master and a deterministic seed. Synthesis of a known-good boot sector without a master is a non-goal.

## 12. Glossary

- **Disk II** — Apple's original 5.25" floppy controller. Slot 6 by convention. Hardware-level bit interface; firmware-cycle-counted.
- **SmartPort** — Apple's block-device firmware protocol for the IIc/IIgs and SmartPort cards on the IIe. Used for hard disks and 3.5" UniDisk.
- **GCR 6-and-2** — Group Code Recording, the 16-sector DOS 3.3 / ProDOS encoding. 256 raw bytes → 343 nibbles per sector.
- **Sector skew** — Logical→physical sector remapping. DOS 3.3 and ProDOS use different skews on the same physical media.
- **Quarter-track** — 5.25" head positions number from 0 to 139 (4 positions per of 35 tracks). Copy protection often parks the head between whole tracks.
- **2MG (`.2mg`)** — Universal Apple II disk image container, 64-byte header + data + optional comment + creator data. Specifies DOS / ProDOS / nibble payload.
- **WOZ** — Modern bitstream image format; preserves bit-cell timing and weak/randomized bits. Required for full copy-protection compatibility.
- **HDV (`.hdv`)** — Headerless raw 512-byte-block dump used for hard-disk images.
- **P5A / P6A** — The two PROMs on the Disk II controller card. P5A is the 256-byte boot ROM at `$CS00`. P6A is the on-card LSS state machine, not CPU-visible.
- **LSS** — Logic State Sequencer, the small finite-state machine inside the Disk II controller that walks bit cells off the disk into the data latch.
