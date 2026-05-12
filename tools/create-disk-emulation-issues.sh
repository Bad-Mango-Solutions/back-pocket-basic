#!/usr/bin/env bash
# tools/create-disk-emulation-issues.sh
#
# One-shot script that materializes the PRD's §11 issue breakdown
# (DISK-EMULATION-PRD.md) on GitHub for repo Bad-Mango-Solutions/back-pocket-basic.
#
# What it does:
#   1. Creates an Epic issue: "[EPIC] Disk Emulation (PRD)".
#   2. Creates 11 net-new child issues (PRD §11 rows 4, 5, 7, 8, 9, 11, 12, 13,
#      14, 15, 16). Row 17 is intentionally skipped.
#   3. Rewrites the bodies of three pre-existing issues to align them with the
#      PRD and the new epic:
#        - #203 -> rows 1+2+3 (storage abstraction, IBlockMedia/I525Media, GCR + skew)
#        - #194 -> row 6 (DiskIIController)
#        - #195 -> row 10 (SmartPortController)
#   4. Wires dependencies (Depends on / Blocks) between issues using the
#      numbers GitHub assigns at creation time (two-pass).
#   5. Updates the Epic body with a checklist of every child issue.
#
# What it deliberately does NOT do (per Josh's instructions):
#   - Apply labels (you'll set those manually).
#   - Set milestones (you'll set those manually).
#   - Assign anyone (every issue is left unassigned).
#
# Requirements:
#   - gh CLI authenticated against github.com with `repo` scope on
#     Bad-Mango-Solutions/back-pocket-basic.
#
# Idempotency:
#   This script is NOT idempotent. Running it twice will create duplicate
#   issues. Run it exactly once. If something goes wrong partway through, see
#   the "Recovery" section at the bottom of this file.
#
# Usage:
#   ./tools/create-disk-emulation-issues.sh                # dry-run (default)
#   ./tools/create-disk-emulation-issues.sh --execute      # actually create/edit

set -euo pipefail

REPO="Bad-Mango-Solutions/back-pocket-basic"
PRD_PATH="DISK-EMULATION-PRD.md"

EXECUTE=0
if [[ "${1:-}" == "--execute" ]]; then
  EXECUTE=1
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "error: gh CLI is required" >&2
  exit 1
fi

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

# create_issue <title> <body-file> -> echoes new issue number
create_issue() {
  local title="$1"
  local body_file="$2"
  if (( EXECUTE )); then
    gh issue create --repo "$REPO" --title "$title" --body-file "$body_file" \
      | sed -n 's#.*/issues/\([0-9][0-9]*\).*#\1#p' | head -n1
  else
    # Fake number for dry-run; deterministic-ish by title.
    printf 'DRY%04d\n' "$(( ( RANDOM % 9000 ) + 1000 ))"
  fi
}

# edit_issue <number> <body-file>
edit_issue() {
  local n="$1"
  local body_file="$2"
  if (( EXECUTE )); then
    gh issue edit "$n" --repo "$REPO" --body-file "$body_file" >/dev/null
  fi
  echo "  edited #$n (body from $(basename "$body_file"))"
}

# retitle_issue <number> <new-title>
retitle_issue() {
  local n="$1"
  local title="$2"
  if (( EXECUTE )); then
    gh issue edit "$n" --repo "$REPO" --title "$title" >/dev/null
  fi
  echo "  retitled #$n -> $title"
}

WORK="$(mktemp -d -t disk-issues.XXXXXX)"
trap 'rm -rf "$WORK"' EXIT

# Fixed numbers for already-existing issues (per Josh).
N203=203   # rows 1+2+3 (storage abstraction)
N194=194   # row 6      (DiskIIController)
N195=195   # row 10     (SmartPortController)

# ---------------------------------------------------------------------------
# Standard issue body template.
#
# Args: 1=row#  2=summary  3=prd-section-refs  4=fr-list  5=acceptance
#       6=depends-on  7=blocks  8=epic-number  9=extra-dod-lines (may be empty)
# ---------------------------------------------------------------------------
render_body() {
  local row="$1" summary="$2" sections="$3" frs="$4" acc="$5"
  local deps="$6" blocks="$7" epic="$8" extra_dod="$9"
  cat <<EOF
## Summary

$summary

## PRD Reference

- Source: [\`$PRD_PATH\`](../blob/main/$PRD_PATH)
- §11 row: **$row**
- Spec sections: $sections
- Functional requirements: $frs

## Acceptance Criteria

$acc

## Dependencies

- **Depends on:** $deps
- **Blocks:** $blocks

## Definition of Done

- [ ] Implementation satisfies every FR listed above
- [ ] Unit tests added/updated; \`dotnet test\` is green
- [ ] XML doc comments on all new public types and members
- [ ] StyleCop / analyzer warnings are zero on touched files (no suppressions)
$extra_dod

## Epic

Part of #$epic
EOF
}

# ---------------------------------------------------------------------------
# Pass 0 — Create the epic with a placeholder body. We update its checklist
#          after every child issue exists.
# ---------------------------------------------------------------------------

EPIC_TITLE='[EPIC] Disk Emulation (PRD)'
cat > "$WORK/epic-placeholder.md" <<EOF
## Summary

Tracking epic for the **Disk Emulation** initiative described in
[\`$PRD_PATH\`](../blob/main/$PRD_PATH). This epic groups every implementation
issue derived from PRD §11 and serves as the single rollup for delivery
status.

## Goals (PRD §2)

- **G1** Boot DOS 3.3 from a 5.25" image (Disk II).
- **G2** Boot ProDOS from a 5.25" image (Disk II).
- **G3** Boot ProDOS from a hard-disk image (SmartPort, slot 7).
- **G4** Boot ProDOS from a 3.5" UniDisk image (SmartPort).
- **G5** Read \`.woz\` (WOZ 1 / WOZ 2) bitstream images.
- **G6** Insert/eject removable media at runtime via the debug console.
- **G7** Create blank disk images from the debug console.
- **G8** Storage subsystem is mockable end-to-end.

## Priority sequencing (PRD §5)

P0 → P1 → P2 → P3 → P4 → P5.

## Child issues

_(populated by \`tools/create-disk-emulation-issues.sh\` after creation)_

## Out of scope

PRD §3 (DOS 3.2, WOZ writes, SCSI, GS/OS, network volumes, GUI image creation,
copy-protection authoring). Avalonia mount/eject UI (PRD §11 row 17) is
deferred and intentionally has no issue.
EOF

echo "Creating epic..."
EPIC=$(create_issue "$EPIC_TITLE" "$WORK/epic-placeholder.md")
echo "  epic = #$EPIC"

# ---------------------------------------------------------------------------
# Pass 1 — Create the 11 net-new child issues with placeholder dependency
#          lines. We rewrite their bodies in Pass 2 once every number is known.
#
# Title-to-row map (and target variable):
#   ROW4  -> R4   "Implement \`disk create\` and \`disk info\` debug commands"
#   ROW5  -> R5   "Plumb \`config\` JSON through SlotCardProfile / DeviceFactoryRegistry"
#   ROW7  -> R7   "Implement \`disk list\` / \`disk insert\` / \`disk eject\` / \`disk flush\` debug commands"
#   ROW8  -> R8   "Wire DOS 3.3 P0 boot path end-to-end (Disk II 5.25\")"
#   ROW9  -> R9   "Wire ProDOS P1 boot path end-to-end (Disk II 5.25\")"
#   ROW11 -> R11  "Wire ProDOS hard-disk P2 boot path end-to-end (SmartPort, slot 7)"
#   ROW12 -> R12  "Extend \`disk insert\` / \`disk eject\` to SmartPort 3.5\" UniDisk volumes"
#   ROW13 -> R13  "Wire UniDisk 3.5\" P3 boot path (SmartPort)"
#   ROW14 -> R14  "Add WOZ (WOZ 1 + WOZ 2) reader and bit-stream LSS path in DiskIIController (read-only)"
#   ROW15 -> R15  "Update machine-profile.schema.json and Machine Profile Configuration Guide for diskii / smartport"
#   ROW16 -> R16  "Add example profiles (\`pocket2e-a2-enh-disk.json\`, \`pocket2e-a2-enh-hd.json\`)"
# ---------------------------------------------------------------------------

PLACEHOLDER='_(filled in by tools/create-disk-emulation-issues.sh after every issue exists)_'
make_placeholder_body() {
  local title="$1"
  cat <<EOF
## Summary

$title — see [\`$PRD_PATH\`](../blob/main/$PRD_PATH).

This body is a placeholder; the script will rewrite it with the full
acceptance criteria and dependency wiring once every issue number is known.

## Epic

Part of #$EPIC
EOF
}

declare -A TITLES
TITLES[R4]='Implement `disk create` and `disk info` debug commands (raw / dos33 / prodos / bootable)'
TITLES[R5]='Plumb `config` JSON through `SlotCardProfile` and `DeviceFactoryRegistry` to slot-card factories'
TITLES[R7]='Implement `disk list` / `disk insert` / `disk eject` / `disk flush` debug commands against `DiskIIController`'
TITLES[R8]='Wire DOS 3.3 P0 boot path end-to-end (Disk II 5.25")'
TITLES[R9]='Wire ProDOS P1 boot path end-to-end (Disk II 5.25")'
TITLES[R11]='Wire ProDOS hard-disk P2 boot path end-to-end (SmartPort, slot 7)'
TITLES[R12]='Extend `disk insert` / `disk eject` to SmartPort 3.5" UniDisk volumes (removable units only)'
TITLES[R13]='Wire UniDisk 3.5" P3 boot path (SmartPort)'
TITLES[R14]='Add WOZ (WOZ 1 + WOZ 2) reader and bit-stream LSS path in `DiskIIController` (read-only)'
TITLES[R15]='Update `schemas/machine-profile.schema.json` and `Machine Profile Configuration Guide.md` for `diskii` / `smartport` shapes'
TITLES[R16]='Add example profiles (`pocket2e-a2-enh-disk.json`, `pocket2e-a2-enh-hd.json`)'

declare -A NUM
ORDER=(R4 R5 R7 R8 R9 R11 R12 R13 R14 R15 R16)

echo "Creating ${#ORDER[@]} child issues (placeholders)..."
for key in "${ORDER[@]}"; do
  make_placeholder_body "${TITLES[$key]}" > "$WORK/$key.placeholder.md"
  NUM[$key]=$(create_issue "${TITLES[$key]}" "$WORK/$key.placeholder.md")
  echo "  $key (${TITLES[$key]:0:60}...) = #${NUM[$key]}"
done

# ---------------------------------------------------------------------------
# Pass 2 — Render full bodies for every issue (children + the three existing
#          ones), then edit each in place. Dependencies use the now-known
#          numbers.
#
# Dependency graph (derived from PRD §11 ordering & §6 functional refs):
#
#   #203 (rows 1+2+3) : depends-on (foundational); blocks R4, #194, R14
#   R4   (row 4)      : depends-on #203;           blocks #194, R7, R8, R9, R11, R13
#   R5   (row 5)      : depends-on (none);         blocks #194, #195
#   #194 (row 6)      : depends-on #203, R4, R5;   blocks R7, R8, R9, R14
#   R7   (row 7)      : depends-on #194;           blocks R8, R9, R12
#   R8   (row 8)      : depends-on R4, #194, R7;   blocks (none)
#   R9   (row 9)      : depends-on R4, #194, R7;   blocks R15, R16
#   #195 (row 10)     : depends-on R5;             blocks R11, R12, R13, R15
#   R11  (row 11)     : depends-on R4, #195;       blocks R16
#   R12  (row 12)     : depends-on R7, #195;       blocks R13
#   R13  (row 13)     : depends-on #195, R12;      blocks R16
#   R14  (row 14)     : depends-on #203, #194;     blocks (none)
#   R15  (row 15)     : depends-on R5, #195, R9;   blocks (none)
#   R16  (row 16)     : depends-on #194, #195, R11, R13, R15; blocks (none)
# ---------------------------------------------------------------------------

# Lookup helper: link list "#a, #b, #c (none)" rendering.
links() {
  local out=""
  for n in "$@"; do
    [[ -z "$n" ]] && continue
    out+="#$n, "
  done
  if [[ -z "$out" ]]; then
    echo "_none_"
  else
    echo "${out%, }"
  fi
}

echo "Rewriting bodies with full content + dependency wiring..."

# ---- #203 : rows 1+2+3 (storage abstraction, block/525 media, GCR + skew) ----
retitle_issue "$N203" \
  'Storage abstraction: `IStorageBackend` + `IBlockMedia` / `I525Media` + `DiskImageFactory` + GCR 6-and-2 nibblizer (PRD §11 rows 1–3)'
render_body \
  "1, 2, 3 (combined)" \
  "Introduce a new \`BadMango.Emulator.Storage\` assembly providing the byte-level storage backend, the format-neutral \`IBlockMedia\` and \`I525Media\` surfaces, the \`DiskImageFactory\` for raw sector/block formats, and the GCR 6-and-2 nibblizer with DOS/ProDOS skew tables and \`.dsk\` ordering sniffer. This is the foundation every controller and debug command depends on." \
  "PRD §6.1 (FR-S1 – FR-S7); PRD §10 decisions 4 and 5" \
  "FR-S1, FR-S2, FR-S3, FR-S4, FR-S5, FR-S6, FR-S7" \
"**Row 1 — Storage backends (\`BadMango.Emulator.Storage\`)**
- New assembly hosts \`IStorageBackend\` with \`Read\` / \`Write\` / \`Flush\` / \`Length\` / \`CanWrite\` over \`Span<byte>\` / \`ReadOnlySpan<byte>\`.
- Implementations: \`RamStorageBackend\`, \`FileStorageBackend\`, \`RamCachedStorageBackend\` (write-through and write-back modes with a dirty-block bitmap).
- Unit tests pass for RAM, file, and cached backends, including write-through and write-back semantics.

**Row 2 — \`IBlockMedia\`, \`I525Media\`, \`DiskGeometry\`, \`DiskImageFactory\`**
- \`IBlockMedia\` (\`BlockSize\`, \`BlockCount\`, \`ReadBlock\`, \`WriteBlock\`, \`IsReadOnly\`, \`Flush\`).
- \`I525Media\` (quarter-track addressing 0–139, \`ReadTrack\`, \`WriteTrack\`, \`OptimalTrackLength\`, \`IsReadOnly\`, \`Flush\`, \`Geometry\`).
- \`DiskImageFactory.Open(path)\` returns the appropriate media interface(s); detection by extension first, magic-byte sniff for headered formats; result is pattern-matchable.
- Raw-only writers/readers for \`.dsk\`, \`.do\`, \`.po\`, \`.2mg\`/\`.2img\`, \`.nib\`, \`.hdv\`. \`.d13\` recognized and refused with a clear error. \`.woz\` is out of scope here (row 14).
- Sector-image adapters expose **both** an \`I525Media\` view (via the row-3 nibblizer) and an \`IBlockMedia\` view (via inverse reordering).
- Write-through is the default for file-backed images; write-back is configurable with explicit flush. Write-protect honored from image flags (e.g. 2MG flag bit 0) and runtime mount flags.
- Round-trip tests pass for fixture images of every supported sector/block format.

**Row 3 — GCR 6-and-2 nibblizer + skew + \`.dsk\` sniffer**
- 6-and-2 encoder/decoder with full address-field generation (volume, track, sector, checksum, prologue/epilogue).
- DOS 3.3 and ProDOS sector-skew tables; both directions.
- \`.dsk\` ordering sniffer per PRD §10 decision 5: read sector at track-17 / block-2, look for DOS 3.3 VTOC signature first, then ProDOS root-directory signature; fall back to DOS order on inconclusive sniff. Surface the chosen order through metadata so \`disk info\` (row 4) can report it.
- Property-based round-trip tests cover every (volume, track, sector) triple; ambiguous \`.dsk\` resolves to the correct order against DOS- and ProDOS-formatted fixtures.

**Mockability seam (PRD §7)**
- All four interfaces are Moq-friendly: no sealed types on the seam, no static singletons. Verified by a smoke test that constructs an \`I525Media\` Moq and feeds it through the factory adapter path." \
  "$(links)" \
  "$(links "${NUM[R4]}" "$N194" "${NUM[R14]}")" \
  "$EPIC" \
  "- [ ] New \`BadMango.Emulator.Storage\` project added to \`BackPocketBasic.slnx\`" \
  > "$WORK/N203.md"
edit_issue "$N203" "$WORK/N203.md"

# ---- R4 : disk create / disk info ----
render_body \
  "4" \
  "Add the \`disk create\` and \`disk info\` debug-console subcommands so that every downstream issue (controllers, boot paths, integration tests) can author its own fixture images via the console rather than checking binaries into the repo. Per PRD §11, this issue intentionally lands before any controller code." \
  "PRD §6.5 (FR-DC1, FR-DC2); PRD §11 bootstrapping note" \
  "FR-DC1, FR-DC2" \
"- New subcommands live in \`BadMango.Emulator.Debug.Infrastructure/Commands/DeviceCommands/\`, each implements \`ICommandHandler\`, each carries \`[DeviceDebugCommand]\`, and each is auto-registered by the existing Autofac \`DeviceDebugCommandsModule\` (no new registrar).
- \`disk create <path> [--size 5.25|3.5|32M|<blocks>] [--format raw|dos33|prodos] [--bootable <bootimage>] [--volume-name <name>] [--volume-number <n>]\`:
  - \`raw\` (default): zero-filled image at the requested geometry. Container chosen from extension (\`.dsk\` / \`.do\` / \`.po\` / \`.2mg\` / \`.hdv\`).
  - \`dos33\`: writes a DOS 3.3 VTOC and catalog track on a 35-track 5.25\" image. \`--volume-name\` ignored; \`--volume-number\` defaults to 254.
  - \`prodos\`: writes a ProDOS volume directory + bitmap sized for the chosen geometry. \`--volume-name\` defaults to \`BLANK\`.
  - \`--bootable <bootimage>\`: copies the boot sector/blocks from \`<bootimage>\` (any format \`DiskImageFactory\` accepts) into the new image. Until a real Disk II controller exists (#$N194), the \`<bootimage>\` argument is required for bootable output.
  - Output is round-trippable: \`DiskImageFactory.Open(path)\` accepts whatever \`disk create\` wrote.
- \`disk info <path>\`: without mounting anything, reports the format \`DiskImageFactory\` would pick, geometry, write-protect flag, sniffed \`.dsk\` ordering (per PRD §10 decision 5), and (for \`.2mg\`) header metadata.
- Neither subcommand requires a running machine; both resolve only \`DiskImageFactory\` and \`IDebugPathResolver\` from \`ICommandContext\` / \`IDebugContext\`.
- Tests for both subcommands cover happy + error paths; \`disk create\` outputs are validated by re-opening the produced image through \`DiskImageFactory\` and asserting \`disk info\` reports the expected metadata." \
  "$(links "$N203")" \
  "$(links "$N194" "${NUM[R7]}" "${NUM[R8]}" "${NUM[R9]}" "${NUM[R11]}" "${NUM[R13]}")" \
  "$EPIC" \
  "" \
  > "$WORK/R4.md"
edit_issue "${NUM[R4]}" "$WORK/R4.md"

# ---- R5 : plumb config JSON ----
render_body \
  "5" \
  "Extend the slot-card profile/factory plumbing so that every slot card can receive an optional \`config\` JSON blob from its profile entry, matching the shape the motherboard device entries already use. This is what lets \`diskii\` (#$N194) and \`smartport\` (#$N195) accept their per-card configuration from a profile." \
  "PRD §6.4 (FR-C1, FR-C2); PRD §10 decision 1" \
  "FR-C1, FR-C2" \
"- \`SlotCardProfile\` carries an optional \`Config\` of type \`JsonElement?\` (matching \`MotherboardDeviceEntry.Config\`).
- \`MachineBuilder.FromProfile.ConfigureSlotCard\` passes the config blob through to the registered slot-card factory.
- \`DeviceFactoryRegistry\` exposes a single slot-card factory delegate of shape \`Func<MachineBuilder, JsonElement?, ISlotCard>\` (PRD §10 decision 1). Existing parameterless registrations are adapted to ignore the config argument; **no overload soup**.
- The existing \`Extended80Column\` migration test continues to pass.
- A new test registers a fake \`diskii\`-style factory that requires the config blob and asserts the registry hands it through.
- All \`ISlotCard\`-related public types updated to keep XML doc accurate." \
  "$(links)" \
  "$(links "$N194" "$N195")" \
  "$EPIC" \
  "" \
  > "$WORK/R5.md"
edit_issue "${NUM[R5]}" "$WORK/R5.md"

# ---- #194 : row 6 (DiskIIController) ----
retitle_issue "$N194" \
  'Implement `DiskIIController` (replace `DiskIIControllerStub` body; keep stub for the no-config / no-image factory path) (PRD §11 row 6)'
render_body \
  "6" \
  "Replace the body of \`DiskIIControllerStub\` with a working \`DiskIIController : ISlotCard\`. The stub stays in the codebase for the no-config / no-image factory path and for tests that want a no-image card; the new controller is selected when the profile entry provides a \`config.drives\` block." \
  "PRD §6.2 (FR-D1 – FR-D10); PRD §6.6 (FR-R1 – FR-R4); PRD §7 (FR-T1 – FR-T3, FR-T5)" \
  "FR-D1, FR-D2, FR-D3, FR-D4, FR-D5, FR-D6, FR-D7, FR-D8, FR-D9, FR-D10, FR-R1, FR-R2, FR-R3, FR-R4" \
"- Two drives per controller (drive 1, drive 2). Each drive holds: head quarter-track (0–139), spin position (byte offset within current track), motor state, write-protect, currently-mounted \`I525Media\` (nullable for empty drive).
- All 16 soft switches at \`\$C0n0–\$C0nF\` retain their current state-machine semantics and additionally drive the data path. Soft-switch table from \`Disk II Controller Device Specification.md\` §2.2 is normative.
- Phase stepper updates head position on valid phase-overlap sequences; clamps at track 0 and \`2 * geometry.trackCount - 2\`. Invalid sequences produce no movement.
- Q6/Q7 dispatch:
  - \`Q6=0, Q7=0\` — read data latch (nibble at current spin position).
  - \`Q6=1, Q7=0\` — read write-protect status (high bit = WP).
  - \`Q6=0, Q7=1\` — enable write mode; subsequent \`\$C0nD\` writes shift bits onto the track.
  - \`Q6=1, Q7=1\` — load write latch with shift-register contents.
- Timing model uses on-demand recompute: on \`\$C0nC\` read, advance spin position by \`(scheduler.Now - lastUpdateCycle) / cyclesPerByte\` bytes, modulo track length. A single rescheduling event prevents drift while CPU is halted. **No per-cycle polling.**
- Motor on/off is software-controlled with no automatic timeout. Motor on starts a settling timer (~1 ms) before valid data is returned. Track step adds a settling delay (~30 ms emulator-time) during which reads return the last-latched byte.
- Writes to nibble-backed images write nibbles directly. Writes to sector-backed images mark the track dirty; on motor-off, drive-deselect, eject, or \`Flush\`, dirty tracks are parsed back into sectors and written to the underlying \`IStorageBackend\`. Parse failures are surfaced as warnings (image still receives the nibble payload via the nibble cache).
- Boot ROM (\`\$Cn00–\$CnFF\`) is loaded from a user-supplied 256-byte P5A image via the profile's existing \`rom-images\` mechanism. When unset, the controller falls back to the existing \`DiskIIBootRomStub\` and logs a single clear warning; boot will not succeed but the rest of the emulator continues to run.
- Controller advertises itself in \`IDeviceRegistry\` with a path like \`Slot/6/DiskII\`, and exposes (for debug surfaces) per-drive \`motor\`, \`phase\`, \`quarterTrack\`, \`selected\`, \`writeProtect\`, \`mountedImagePath\`.
- **Runtime safety:** mount/eject is async-safe with respect to the running CPU (deferred to next scheduler turn); eject always flushes first and rejects on flush failure; hot-swap resets per-drive state; insertions during an active motor cycle reset the settling timer.
- **Mockability:** controller accepts an \`I525Media\` (or factory) and an \`IEventContext\` so tests construct it without any filesystem (FR-T2).
- **Tests:** existing \`DiskIIControllerStubTests\` continue to pass against the stub; a parallel \`DiskIIControllerTests\` covers the real controller using mocked media; phase stepper, motor settling, Q6/Q7 dispatch, and address-field nibble visibility on \`\$C0EC\` for a synthetic image are all asserted." \
  "$(links "$N203" "${NUM[R4]}" "${NUM[R5]}")" \
  "$(links "${NUM[R7]}" "${NUM[R8]}" "${NUM[R9]}" "${NUM[R14]}")" \
  "$EPIC" \
  "" \
  > "$WORK/N194.md"
edit_issue "$N194" "$WORK/N194.md"

# ---- R7 : disk list / insert / eject / flush ----
render_body \
  "7" \
  "Add the runtime-only \`disk\` subcommands (\`list\`, \`insert\`, \`eject\`, \`flush\`) that operate on a live \`DiskIIController\`. Naturally extends to SmartPort 3.5\" UniDisk in row 12 (#${NUM[R12]})." \
  "PRD §6.5 (FR-DC3, FR-DC4, FR-DC5, FR-DC6); PRD §6.6 (FR-R2, FR-R3, FR-R4)" \
  "FR-DC3, FR-DC4, FR-DC5, FR-DC6" \
"- All four subcommands live next to row 4 commands in \`BadMango.Emulator.Debug.Infrastructure/Commands/DeviceCommands/\`, implement \`ICommandHandler\`, carry \`[DeviceDebugCommand]\`, and are auto-discovered by the existing Autofac module.
- \`disk list\` — prints every installed controller, drive/unit, mount state, write-protect, image path, and geometry.
- \`disk insert <slot>:<drive> <path> [--write-protect]\` — mounts a removable image at runtime. Fails with a clear error for non-removable units and for image-format mismatches (e.g. a 5.25\" image in a 3.5\" drive).
- \`disk eject <slot>:<drive>\` — flushes dirty state, dismounts. Fails clearly for non-removable units; if flush fails (e.g. read-only file underneath), eject is rejected with an error and the image stays mounted.
- \`disk flush <slot>:<drive>\` — force-flush without ejecting.
- Each subcommand resolves the live controllers via \`ICommandContext\` / \`IDebugContext\`.
- Happy and error paths are covered by tests that use mocked controllers (FR-T8)." \
  "$(links "$N194")" \
  "$(links "${NUM[R8]}" "${NUM[R9]}" "${NUM[R12]}")" \
  "$EPIC" \
  "" \
  > "$WORK/R7.md"
edit_issue "${NUM[R7]}" "$WORK/R7.md"

# ---- R8 : DOS 3.3 P0 boot ----
render_body \
  "8" \
  "Wire the **P0** boot path: a user-supplied DOS 3.3 master disk boots to the Applesoft prompt in slot 6, drive 1, against the real \`DiskIIController\` from #$N194." \
  "PRD §2 G1; PRD §5 P0; PRD §7 (FR-T6 P0)" \
  "G1, FR-T6 (P0)" \
"- A user-supplied DOS 3.3 master (\`.dsk\` / \`.do\` / \`.po\` / \`.2mg\` / \`.nib\`) mounted in slot 6 / drive 1 boots to the Applesoft \`]\` prompt.
- Boot integration test: a synthetic minimal DOS 3.3 image (built via \`disk create --format dos33 --bootable <user-supplied master>\` from #${NUM[R4]}) — assert CPU lands in \`\$0801\` with VTOC reachable.
- Profile shape from PRD §6.4 FR-C3 (\`{ \"slot\": 6, \"type\": \"diskii\", \"config\": { \"rom\": ..., \"drives\": [...] } }\`) is exercised end-to-end in this test.
- No regressions in existing controller unit tests." \
  "$(links "${NUM[R4]}" "$N194" "${NUM[R7]}")" \
  "$(links)" \
  "$EPIC" \
  "" \
  > "$WORK/R8.md"
edit_issue "${NUM[R8]}" "$WORK/R8.md"

# ---- R9 : ProDOS P1 boot (Disk II 5.25") ----
render_body \
  "9" \
  "Wire the **P1** boot path: a user-supplied ProDOS 2.4.3 5.25\" image boots from slot 6 (Disk II)." \
  "PRD §2 G2; PRD §5 P1; PRD §7 (FR-T6 P1)" \
  "G2, FR-T6 (P1)" \
"- A user-supplied ProDOS 2.4.3 5.25\" image mounted in slot 6 / drive 1 boots to the ProDOS launcher.
- Boot integration test: a synthetic minimal ProDOS 5.25\" image — assert ProDOS dispatcher signature visible at the documented offsets.
- Confirms the GCR/skew round-trip and DOS-vs-ProDOS sector ordering from #$N203 stay correct under a real boot stream." \
  "$(links "${NUM[R4]}" "$N194" "${NUM[R7]}")" \
  "$(links "${NUM[R15]}" "${NUM[R16]}")" \
  "$EPIC" \
  "" \
  > "$WORK/R9.md"
edit_issue "${NUM[R9]}" "$WORK/R9.md"

# ---- #195 : row 10 (SmartPortController) ----
retitle_issue "$N195" \
  'Implement `SmartPortController` + clean-room SmartPort firmware ROM + dispatcher trap (PRD §11 row 10)'
render_body \
  "10" \
  "Implement \`SmartPortController : ISlotCard\` with a clean-room SmartPort firmware ROM (signature bytes + trampoline + trap opcode — no Apple-copyrighted bytes) and a managed-code dispatcher invoked via the existing \`TrapRegistry\` mechanism (same pattern as \`GameIOController\`). This unlocks ProDOS hard-disk and 3.5\" UniDisk paths." \
  "PRD §6.3 (FR-SP1 – FR-SP6); PRD §6.4 (FR-C4); PRD §7 (FR-T7)" \
  "FR-SP1, FR-SP2, FR-SP3, FR-SP4, FR-SP5, FR-SP6, FR-C4" \
"- Default slot 7 for hard-disk usage; configurable to slot 5 for 3.5\"/UniDisk usage.
- Slot ROM at \`\$Cn00–\$CnFF\` contains the four ProDOS block-device signature bytes (\`\$Cn01=\$20\`, \`\$Cn03=\$00\`, \`\$Cn05=\$03\`, \`\$Cn07=\$00\`) and a dispatch trampoline whose ProDOS entry sits at \`\$CnFC\` and whose SmartPort entry sits at (ProDOS entry) + 3 per \`SmartPort Specification.md\` §3.1.
- Dispatcher implemented via a \`TrapRegistry\` entry on an illegal opcode (same pattern as \`GameIOController\`); managed code reads inline parameters and dispatches to a new \`SmartPortCommandHandler\`.
- Commands implemented: \`STATUS\` (\$00), \`READ_BLOCK\` (\$01), \`WRITE_BLOCK\` (\$02), \`FORMAT\` (\$03), \`CONTROL\` (\$04), \`INIT\` (\$05), \`READ\` (\$08), \`WRITE\` (\$09). Errors return SmartPort error codes per spec §5; success returns carry-clear, \`A=0\`.
- ProDOS legacy entry (READ_BLOCK / WRITE_BLOCK via the ProDOS dispatch byte) supported for hard-disk autoboot.
- Volume capacity ≤ 32 MB (65 535 ProDOS blocks) per volume. Up to 8 units per controller. Unit 0 returns bus-level \`STATUS\`.
- Profile shape from FR-C4 (\`{ \"slot\": 7, \"type\": \"smartport\", \"config\": { \"volumes\": [...] } }\`) is consumed via the row-5 plumbing (#${NUM[R5]}).
- **All ROM bytes are originally authored for this project.** No Apple-copyrighted content ships (PRD §8 NFR-6).
- **Mockability:** controller accepts an \`IReadOnlyList<IBlockMedia>\` keyed by unit number (FR-T2).
- **Tests (FR-T7):** each command exercised against an \`IBlockMedia\` mock; parameter-list decoding asserted; error codes asserted. STATUS + READ_BLOCK happy-path tests are mandatory at minimum." \
  "$(links "${NUM[R5]}")" \
  "$(links "${NUM[R11]}" "${NUM[R12]}" "${NUM[R13]}" "${NUM[R15]}")" \
  "$EPIC" \
  "" \
  > "$WORK/N195.md"
edit_issue "$N195" "$WORK/N195.md"

# ---- R11 : ProDOS HD P2 boot ----
render_body \
  "11" \
  "Wire the **P2** boot path: a user-supplied ProDOS 2.4.3 hard-disk image (\`.po\` / \`.2mg\` / \`.hdv\`, single volume, ≤ 32 MB) in slot 7 autoboots when no 5.25\" disk is present." \
  "PRD §2 G3; PRD §5 P2; PRD §7 (FR-T6 P2)" \
  "G3, FR-T6 (P2)" \
"- A user-supplied ProDOS 2.4.3 hard-disk image mounted as a non-removable unit in slot 7 autoboots to the ProDOS launcher.
- IIe Enhanced ROM autoboot scans high slots first, so SmartPort in slot 7 boots ahead of Disk II in slot 6 unless a 5.25\" disk is mounted with motor pending — verify this precedence in the test.
- Boot integration test: a synthetic ProDOS HDV — assert dispatcher signature visible after autoboot.
- Single volume per device, ≤ 32 MB / 65 535 blocks (PRD §10 decision 4)." \
  "$(links "${NUM[R4]}" "$N195")" \
  "$(links "${NUM[R16]}")" \
  "$EPIC" \
  "" \
  > "$WORK/R11.md"
edit_issue "${NUM[R11]}" "$WORK/R11.md"

# ---- R12 : extend insert/eject to SmartPort 3.5" ----
render_body \
  "12" \
  "Extend the existing \`disk insert\` and \`disk eject\` debug-console subcommands (#${NUM[R7]}) so they also operate on SmartPort 3.5\" UniDisk volumes (removable units only). Non-removable hard-disk units must continue to reject mount/eject with a clear error." \
  "PRD §6.5 (FR-DC4, FR-DC5); PRD §6.6 (FR-R2, FR-R3, FR-R4)" \
  "FR-DC4, FR-DC5" \
"- \`disk insert <slot>:<drive> <path>\` mounts a 3.5\" image (\`.po\` / \`.2mg\` / \`.hdv\`, 800 KB / 1600 blocks) into a removable SmartPort unit declared in the profile.
- \`disk eject <slot>:<drive>\` flushes dirty state and dismounts a removable SmartPort unit.
- Both subcommands reject non-removable (hard-disk) units with a clear, distinguishable error message.
- Format mismatches (e.g. a 5.25\" image in a 3.5\" drive) are rejected with a clear error.
- Hot-swap resets per-drive state (FR-R3); insertions during an active operation are deferred to the next scheduler turn (FR-R1)." \
  "$(links "${NUM[R7]}" "$N195")" \
  "$(links "${NUM[R13]}")" \
  "$EPIC" \
  "" \
  > "$WORK/R12.md"
edit_issue "${NUM[R12]}" "$WORK/R12.md"

# ---- R13 : UniDisk P3 boot ----
render_body \
  "13" \
  "Wire the **P3** boot path: a user-supplied 800 KB ProDOS 3.5\" UniDisk image boots through SmartPort." \
  "PRD §2 G4; PRD §5 P3; PRD §7 (FR-T6 P3)" \
  "G4, FR-T6 (P3)" \
"- A user-supplied ProDOS 2.4.3 800 KB image (1600 blocks, \`.po\` / \`.2mg\` / \`.hdv\`) boots via SmartPort (slot 5 or slot 7 per profile).
- Boot integration test: a synthetic 1600-block UniDisk image — boot via SmartPort and assert the launcher signature.
- Removable unit (\`removable: true\`) — exercises the \`disk insert\` extension from #${NUM[R12]}." \
  "$(links "$N195" "${NUM[R12]}")" \
  "$(links "${NUM[R16]}")" \
  "$EPIC" \
  "" \
  > "$WORK/R13.md"
edit_issue "${NUM[R13]}" "$WORK/R13.md"

# ---- R14 : WOZ reader + bitstream LSS path ----
render_body \
  "14" \
  "Add a WOZ (WOZ 1 + WOZ 2) bitstream reader and a bit-stream LSS path in \`DiskIIController\` capable of booting a known-protected commercial title (PRD §2 G5). **Read-only** in v1 — writes to WOZ-backed drives surface a clear \"read-only\" error (PRD §10 decision 3)." \
  "PRD §2 G5; PRD §5 P4; PRD §6.1 FR-S5; PRD §10 decision 3" \
  "G5, FR-S5 (WOZ portion), FR-T4 (WOZ TMAP)" \
"- WOZ 1 and WOZ 2 container parsers (INFO, TMAP, TRKS, META, WRIT chunks; CRC verified).
- Bitstream LSS path in \`DiskIIController\` consumes WOZ bit cells directly (preserves bit-cell timing and weak/randomized bits).
- A known WOZ fixture of a copy-protected title (e.g. Ultima IV) boots and passes its protection check.
- Writes to WOZ-backed drives surface a clear \"read-only\" error and do not corrupt the file.
- WOZ TMAP parser unit tests cover quarter-track → TRK index resolution including the synced-empty-track sentinel.
- Read-only in v1; sidecar \`.nib\` write-back is **out of scope** (PRD §10 decision 3)." \
  "$(links "$N203" "$N194")" \
  "$(links)" \
  "$EPIC" \
  "" \
  > "$WORK/R14.md"
edit_issue "${NUM[R14]}" "$WORK/R14.md"

# ---- R15 : schema + guide ----
render_body \
  "15" \
  "Update \`schemas/machine-profile.schema.json\` and \`specs/reference/Machine Profile Configuration Guide.md\` so editors validate the new \`diskii\` and \`smartport\` profile shapes and so users have authoritative documentation." \
  "PRD §6.4 (FR-C6); PRD §9 (NFR-7)" \
  "FR-C6, NFR-7" \
"- \`schemas/machine-profile.schema.json\` validates the \`diskii\` shape from FR-C3 (\`slot\`, \`type: \"diskii\"\`, \`config.rom\`, \`config.drives[].unit\`, \`config.drives[].image\` (optional), \`config.drives[].writeProtect\` (optional, default \`false\`)).
- Same schema validates the \`smartport\` shape from FR-C4 (\`slot\`, \`type: \"smartport\"\`, \`config.volumes[].unit\`, \`config.volumes[].image\` (optional for removable), \`config.volumes[].removable\` (default \`false\`)).
- A schema-validation test exercises both shapes (positive and negative cases).
- \`Machine Profile Configuration Guide.md\` documents both shapes with an example for each, plus the \`removable\` rule from FR-C5 (removable may be declared in profile or mounted at runtime; non-removable must be declared in profile)." \
  "$(links "${NUM[R5]}" "$N195" "${NUM[R9]}")" \
  "$(links "${NUM[R16]}")" \
  "$EPIC" \
  "" \
  > "$WORK/R15.md"
edit_issue "${NUM[R15]}" "$WORK/R15.md"

# ---- R16 : example profiles ----
render_body \
  "16" \
  "Add the two example machine profiles called out by PRD §9 NFR-8 so users have a working starting point for both Disk II and SmartPort hard-disk configurations." \
  "PRD §9 (NFR-8); PRD §6.4 (FR-C3, FR-C4)" \
  "NFR-8" \
"- \`profiles/pocket2e-a2-enh-disk.json\` — IIe Enhanced + Disk II in slot 6 with two drive bays, declaring its boot ROM via \`rom-images\`.
- \`profiles/pocket2e-a2-enh-hd.json\` — IIe Enhanced + SmartPort in slot 7 with a single non-removable hard-disk volume.
- Both profiles validate against the updated schema from #${NUM[R15]}.
- Both profiles boot their declared images end-to-end against user-supplied ROMs (covered by the boot integration tests added in #${NUM[R8]}, #${NUM[R9]}, #${NUM[R11]}, #${NUM[R13]} as applicable).
- README/CHANGELOG updated per PRD §9 NFR-9 with a \"disk\" section describing the example profiles." \
  "$(links "$N194" "$N195" "${NUM[R11]}" "${NUM[R13]}" "${NUM[R15]}")" \
  "$(links)" \
  "$EPIC" \
  "" \
  > "$WORK/R16.md"
edit_issue "${NUM[R16]}" "$WORK/R16.md"

# ---------------------------------------------------------------------------
# Pass 3 — Final epic body with the full child checklist (in PRD §11 order).
# ---------------------------------------------------------------------------

cat > "$WORK/epic-final.md" <<EOF
## Summary

Tracking epic for the **Disk Emulation** initiative described in
[\`$PRD_PATH\`](../blob/main/$PRD_PATH). This epic groups every implementation
issue derived from PRD §11 and serves as the single rollup for delivery
status.

## Goals (PRD §2)

- **G1** Boot DOS 3.3 from a 5.25" image (Disk II).
- **G2** Boot ProDOS from a 5.25" image (Disk II).
- **G3** Boot ProDOS from a hard-disk image (SmartPort, slot 7).
- **G4** Boot ProDOS from a 3.5" UniDisk image (SmartPort).
- **G5** Read \`.woz\` (WOZ 1 / WOZ 2) bitstream images.
- **G6** Insert/eject removable media at runtime via the debug console.
- **G7** Create blank disk images from the debug console.
- **G8** Storage subsystem is mockable end-to-end.

## Priority sequencing (PRD §5)

P0 → P1 → P2 → P3 → P4 → P5.

## Child issues (PRD §11 order)

- [ ] #$N203 — Storage abstraction + \`IBlockMedia\` / \`I525Media\` + GCR + skew (rows 1+2+3)
- [ ] #${NUM[R4]} — \`disk create\` and \`disk info\` debug commands (row 4)
- [ ] #${NUM[R5]} — Plumb \`config\` JSON through \`SlotCardProfile\` / \`DeviceFactoryRegistry\` (row 5)
- [ ] #$N194 — \`DiskIIController\` (row 6)
- [ ] #${NUM[R7]} — \`disk list\` / \`insert\` / \`eject\` / \`flush\` debug commands (row 7)
- [ ] #${NUM[R8]} — DOS 3.3 P0 boot path (row 8)
- [ ] #${NUM[R9]} — ProDOS P1 boot path, Disk II 5.25" (row 9)
- [ ] #$N195 — \`SmartPortController\` + clean-room firmware ROM + dispatcher trap (row 10)
- [ ] #${NUM[R11]} — ProDOS hard-disk P2 boot path, SmartPort slot 7 (row 11)
- [ ] #${NUM[R12]} — Extend \`disk insert\` / \`eject\` to SmartPort 3.5" UniDisk (row 12)
- [ ] #${NUM[R13]} — UniDisk 3.5" P3 boot path (row 13)
- [ ] #${NUM[R14]} — WOZ reader + bit-stream LSS path, read-only (row 14)
- [ ] #${NUM[R15]} — Schema + Machine Profile Configuration Guide updates (row 15)
- [ ] #${NUM[R16]} — Example profiles (row 16)

## Out of scope

PRD §3 (DOS 3.2, WOZ writes, SCSI, GS/OS, network volumes, GUI image creation,
copy-protection authoring). Avalonia mount/eject UI (PRD §11 row 17) is
deferred and intentionally has no issue.
EOF

echo "Updating epic body with final child checklist..."
edit_issue "$EPIC" "$WORK/epic-final.md"

echo
if (( EXECUTE )); then
  echo "Done. Created epic #$EPIC and child issues:"
  for key in "${ORDER[@]}"; do echo "  $key -> #${NUM[$key]}"; done
  echo "Edited existing issues: #$N203, #$N194, #$N195"
  echo
  echo "Reminder: labels and milestones are intentionally not set — configure manually."
else
  echo "Dry run complete. Re-run with --execute to apply changes:"
  echo "  $0 --execute"
fi

# ---------------------------------------------------------------------------
# Recovery
# ---------------------------------------------------------------------------
# If this script fails partway through:
#   - The epic and any already-created child issues will remain on GitHub. Note
#     their numbers from the script output, then either delete the partial
#     issues via the GitHub UI and re-run, OR finish the remaining work
#     manually using the rendered bodies that this script produced in its
#     temp dir (see the trap above — comment out the cleanup line to inspect).
#   - The three pre-existing issues (#194, #195, #203) only have their bodies
#     and titles rewritten; original content is preserved in their issue
#     history and can be restored manually.
