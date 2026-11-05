// <copyright file="DiskIIController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

using Serilog;
using Serilog.Events;

/// <summary>
/// Working Disk II controller — replaces the body of <see cref="DiskIIControllerStub"/>
/// for the configured / image-bearing factory path while leaving the stub for the
/// no-config / no-image case.
/// </summary>
/// <remarks>
/// <para>
/// Implements PRD §6.2 FR-D1…D10, §6.6 FR-R1…R4, and §7 FR-T1…T3 / FR-T5:
/// </para>
/// <list type="bullet">
/// <item><description>Two drives per controller, each holding head quarter-track, spin
/// position, motor state, write-protect, and the currently mounted <see cref="I525Media"/>
/// (FR-D2).</description></item>
/// <item><description>16 soft switches at <c>$C0n0–$C0nF</c> using the table from
/// <c>Disk II Controller Device Specification.md</c> §2.2 (FR-D3).</description></item>
/// <item><description>Phase stepper that updates the head on valid phase-overlap sequences
/// and clamps at 0 / <c>4 × trackCount − 4</c> (FR-D4).</description></item>
/// <item><description>Q6/Q7 dispatch covering read-data, write-protect sense, write-mode
/// enable, and write-load (FR-D5).</description></item>
/// <item><description>On-demand spin-position recompute on <c>$C0nC</c> reads — no
/// per-cycle polling — with a single rescheduling event preventing drift (FR-D6).</description></item>
/// <item><description>Software motor control with no automatic timeout, ~1 ms motor
/// settling, ~30 ms track-step settling (FR-D7).</description></item>
/// <item><description>Sector-backed writes mark the track dirty; flush on motor-off,
/// drive-deselect, eject, or <see cref="Flush"/>; nibble-backed writes go straight
/// through (FR-D8). Parse failures are logged via the injected Serilog logger.</description></item>
/// <item><description>Boot ROM loaded from a user-supplied 256-byte P5A image (FR-D9).</description></item>
/// <item><description>Per-drive debug surface via <see cref="GetDriveSnapshot"/> (FR-D10).</description></item>
/// <item><description>Async-safe mount/eject deferred to the next scheduler turn (FR-R1);
/// eject flushes first and rejects on flush failure (FR-R2); hot-swap resets per-drive
/// state (FR-R3); mid-motor insertion resets the settling timer (FR-R4).</description></item>
/// </list>
/// <para>
/// The class is annotated with <see cref="DeviceTypeAttribute"/> so the auto-discovery
/// scan in <see cref="DeviceFactoryRegistry"/> registers it as the canonical
/// <c>disk-ii-compatible</c> slot card factory. Although the constructor has no
/// parameterless overload (it requires an injected <see cref="ILogger"/> plus optional
/// boot ROM and settle-cycle parameters that all carry default values), the registry
/// resolves the logger via <see cref="DeviceFactoryRegistry.LoggerFactory"/> at
/// invocation time and supplies defaults for the rest, so the controller is
/// available out-of-the-box from any auto-discovery call site.
/// </para>
/// </remarks>
[DeviceType("disk-ii-compatible")]
public sealed class DiskIIController : ISlotCard, IDiskController
{
    /// <summary>
    /// Number of CPU cycles required for one nibble byte to pass under the read head
    /// (4 µs per bit × 8 bits at the nominal Apple II 1 MHz clock).
    /// </summary>
    public const int CyclesPerByte = 32;

    /// <summary>
    /// Default cycle count modeling motor spin-up settling (~1 ms at 1 MHz).
    /// </summary>
    public const int DefaultMotorSettleCycles = 1000;

    /// <summary>
    /// Default cycle count modeling track-step head settling (~30 ms at 1 MHz).
    /// </summary>
    public const int DefaultTrackStepSettleCycles = 30000;

    private const int DriveCountValue = 2;
    private const byte FloatingByte = 0xFF;

    /// <summary>
    /// Number of bytes captured after a data-field prologue: 343 6-and-2 encoded
    /// nibbles followed by two epilogue bytes (the third epilogue byte, <c>$EB</c>,
    /// is not required for RWTS acceptance and is not validated).
    /// </summary>
    private const int DataFieldCaptureLength = 345;

    /// <summary>
    /// 256-byte GCR 6-and-2 inverse translate table cached locally so the live
    /// stream parser never allocates per-byte. Indexed by the on-disk nibble value;
    /// returns the original 6-bit data value, or <c>0xFF</c> for nibbles that aren't
    /// valid 6-and-2 encodings.
    /// </summary>
    private static readonly byte[] ReadTable = GcrEncoder.GetReadTable();

    private readonly SlotIOHandlers handlers = new();
    private readonly Drive525[] drives = new Drive525[DriveCountValue];
    private readonly IBusTarget expansionRomRegion;
    private readonly int motorSettleCycles;
    private readonly int trackStepSettleCycles;
    private readonly ILogger logger;

    private IBusTarget? romRegion;

    // Controller-level state
    private int currentDrive;          // 0 or 1
    private bool motorOn;
    private bool q6High;
    private bool q7High;
    private byte dataLatch;            // last byte returned to / received from CPU
    private bool dataLatchValid;       // true once the head has produced (or written) a byte
    private byte writeShift;           // staged byte for write-load (Q6=1,Q7=1)
    private int lastReadSpinPosition = -1; // SpinPosition observed at the previous data-read; used to gate the byte-ready high bit

    private IEventContext? context;
    private EventHandle driftHandle;
    private bool driftScheduled;

    // ─── Activity counters (FR-D10 diagnostic surface) ──────────────────
    // These are intentionally raw long fields rather than properties to keep the
    // per-access hot path branch-free; GetActivitySnapshot reads them under no
    // lock because the controller is single-threaded with respect to the bus.
    private long dataReadCount;
    private long freshByteCount;
    private long settleSuppressedReadCount;
    private long staleByteCount;
    private long dataWriteCount;
    private long phasePulseCount;
    private long phaseNoOpCount;
    private long trackChangeCount;
    private long motorOnCount;
    private long motorOffCount;
    private long driveSelectCount;
    private long cacheLoadCount;
    private long cacheFlushCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskIIController"/> class.
    /// </summary>
    /// <param name="logger">
    /// Serilog logger used for non-fatal diagnostic output (missing boot code at
    /// <c>$Cn00</c>, sector-image write-back parse failures, etc.). Required so
    /// the controller can be wired through Autofac without a fallback global
    /// logger; tests should pass <c>Generator.Log().Object</c> from
    /// <c>BadMango.Unit.Components</c>.
    /// </param>
    /// <param name="bootRom">
    /// Optional <see cref="DiskIIBootRom"/> exposed at <c>$Cn00–$CnFF</c>. When
    /// <see langword="null"/>, no slot ROM is published; another card (e.g. the
    /// Extended 80-column ROM) may then own the slot ROM space, or the system boot
    /// ROM will surface a warning at <see cref="Initialize"/> time (FR-D9).
    /// </param>
    /// <param name="motorSettleCycles">Cycles to wait after motor-on before reads return live data (defaults to ~1 ms at 1 MHz).</param>
    /// <param name="trackStepSettleCycles">Cycles a track-step adds to head settle (defaults to ~30 ms at 1 MHz).</param>
    /// <exception cref="ArgumentNullException">If <paramref name="logger"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If a settle-cycle parameter is negative.</exception>
    public DiskIIController(
        ILogger logger,
        DiskIIBootRom? bootRom = null,
        int motorSettleCycles = DefaultMotorSettleCycles,
        int trackStepSettleCycles = DefaultTrackStepSettleCycles)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfNegative(motorSettleCycles);
        ArgumentOutOfRangeException.ThrowIfNegative(trackStepSettleCycles);

        this.logger = logger.ForContext<DiskIIController>();
        this.motorSettleCycles = motorSettleCycles;
        this.trackStepSettleCycles = trackStepSettleCycles;
        romRegion = bootRom;
        expansionRomRegion = new DiskIIExpansionRomStub();

        for (var i = 0; i < drives.Length; i++)
        {
            drives[i] = new Drive525();
        }

        handlers.Set(0x00, PhaseAccess, PhaseAccessWrite);
        handlers.Set(0x01, PhaseAccess, PhaseAccessWrite);
        handlers.Set(0x02, PhaseAccess, PhaseAccessWrite);
        handlers.Set(0x03, PhaseAccess, PhaseAccessWrite);
        handlers.Set(0x04, PhaseAccess, PhaseAccessWrite);
        handlers.Set(0x05, PhaseAccess, PhaseAccessWrite);
        handlers.Set(0x06, PhaseAccess, PhaseAccessWrite);
        handlers.Set(0x07, PhaseAccess, PhaseAccessWrite);
        handlers.Set(0x08, MotorOffAccess, MotorOffAccessWrite);
        handlers.Set(0x09, MotorOnAccess, MotorOnAccessWrite);
        handlers.Set(0x0A, SelectDrive1Access, SelectDrive1AccessWrite);
        handlers.Set(0x0B, SelectDrive2Access, SelectDrive2AccessWrite);
        handlers.Set(0x0C, Q6LAccess, Q6LAccessWrite);
        handlers.Set(0x0D, Q6HAccess, Q6HAccessWrite);
        handlers.Set(0x0E, Q7LAccess, Q7LAccessWrite);
        handlers.Set(0x0F, Q7HAccess, Q7HAccessWrite);
    }

    /// <inheritdoc />
    public string Name => "Disk II Controller";

    /// <inheritdoc />
    public string DeviceType => "DiskII";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.SlotCard;

    /// <inheritdoc />
    public int SlotNumber { get; set; }

    /// <inheritdoc />
    public SlotIOHandlers? IOHandlers => handlers;

    /// <inheritdoc />
    public IBusTarget? ROMRegion => romRegion;

    /// <inheritdoc />
    public IBusTarget? ExpansionROMRegion => expansionRomRegion;

    /// <inheritdoc />
    public int DriveCount => DriveCountValue;

    /// <summary>
    /// Gets a value indicating whether the motor is currently energised.
    /// </summary>
    /// <value><see langword="true"/> when the motor is on (settle timer may still be active).</value>
    public bool IsMotorOn => motorOn;

    /// <summary>
    /// Gets the zero-based index of the currently selected drive (<c>0</c> = drive 1, <c>1</c> = drive 2).
    /// </summary>
    /// <value>The selected drive index.</value>
    public int SelectedDriveIndex => currentDrive;

    /// <summary>
    /// Gets a value indicating whether the Q6 latch is high.
    /// </summary>
    /// <value><see langword="true"/> when Q6 is high.</value>
    public bool IsQ6High => q6High;

    /// <summary>
    /// Gets a value indicating whether the Q7 latch is high.
    /// </summary>
    /// <value><see langword="true"/> when Q7 is high.</value>
    public bool IsQ7High => q7High;

    /// <inheritdoc />
    public void Initialize(IEventContext eventContext)
    {
        ArgumentNullException.ThrowIfNull(eventContext);
        context = eventContext;

        // FR-D9: if no user-supplied boot ROM was provided, peek at $Cn00 to see if
        // another card has already populated the slot ROM space. If it's still $FF
        // (effectively unpopulated), surface a single warning but continue.
        if (romRegion is null && SlotNumber is >= 1 and <= 7)
        {
            try
            {
                var addr = (uint)(0xC000 + (SlotNumber << 8));
                var probe = new BusAccess(
                    Address: addr,
                    Value: 0,
                    WidthBits: 8,
                    Mode: BusAccessMode.Decomposed,
                    EmulationFlag: true,
                    Intent: AccessIntent.DebugRead,
                    SourceId: 0,
                    Cycle: eventContext.Now,
                    Flags: AccessFlags.NoSideEffects);
                var first = eventContext.Bus.Read8(in probe);
                if (first == 0xFF)
                {
                    logger.Warning(
                        "DiskII (slot {Slot}): no boot ROM supplied and ${Addr:X4} reads $FF; system boot ROM will not boot from this slot.",
                        SlotNumber,
                        addr);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
            {
                // Bus may not have the slot ROM region mapped at all in some test setups; that's fine.
            }
        }
    }

    /// <summary>
    /// Loads the user-supplied 256-byte P5A boot ROM image, replacing whatever ROM (if any)
    /// was provided to the constructor. Intended for the profile-driven build path where
    /// the slot-card factory itself has no access to the <c>rom-images</c> table; the
    /// <see cref="MachineBuilder"/> resolves the ROM from <c>config.rom</c> and pushes
    /// the bytes in via this method before the <see cref="ISlotManager"/> installs the card.
    /// </summary>
    /// <param name="bootRomBytes">A 256-byte buffer containing the P5A boot ROM payload.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="bootRomBytes"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// If <paramref name="bootRomBytes"/> is not exactly <see cref="DiskIIBootRom.RomSize"/> bytes.
    /// </exception>
    public void LoadBootRom(byte[] bootRomBytes)
    {
        ArgumentNullException.ThrowIfNull(bootRomBytes);
        romRegion = new DiskIIBootRom(bootRomBytes);
    }

    /// <inheritdoc />
    public void OnExpansionROMSelected()
    {
        // No-op: expansion ROM is a passive RomTarget, no select-side effects modeled.
    }

    /// <inheritdoc />
    public void OnExpansionROMDeselected()
    {
        // No-op: see OnExpansionROMSelected.
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Flush any pending writes for both drives so a reset never silently drops data.
        for (var i = 0; i < drives.Length; i++)
        {
            try
            {
                drives[i].Flush(logger);
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException)
            {
                logger.Warning(ex, "DiskII (slot {Slot}): drive {Drive} flush during reset failed.", SlotNumber, i + 1);
            }
        }

        currentDrive = 0;
        motorOn = false;
        q6High = false;
        q7High = false;
        dataLatch = 0;
        dataLatchValid = false;
        writeShift = 0;
        lastReadSpinPosition = -1;
        CancelDriftEvent();

        for (var i = 0; i < drives.Length; i++)
        {
            drives[i].ResetTransientState();
        }
    }

    /// <inheritdoc />
    public void Mount(int driveIndex, I525Media media, string? imagePath = null)
    {
        ValidateDriveIndex(driveIndex);
        ArgumentNullException.ThrowIfNull(media);

        // Re-insert over an existing mount implies an eject of the current disk first
        // (the operator-facing equivalent of physically swapping the diskette). FR-R2:
        // the implicit eject must flush the current disk; if that flush fails, reject
        // the mount and leave the prior disk untouched so the operator can react.
        var existingDrive = drives[driveIndex];
        if (existingDrive.Media is not null)
        {
            try
            {
                existingDrive.FlushOrThrow();
            }
            catch (Exception ex)
            {
                logger.Warning(
                    ex,
                    "DiskII (slot {Slot}): implicit eject of drive {Drive} for re-insert rejected — flush of currently mounted disk failed.",
                    SlotNumber,
                    driveIndex + 1);
                throw new InvalidOperationException(
                    $"Cannot mount over existing disk in drive {driveIndex + 1}: flush of currently mounted disk failed: {ex.Message}",
                    ex);
            }

            // Flush succeeded — physically eject the existing disk so the drive is
            // empty before the new mount is applied (immediately or deferred).
            ApplyEject(driveIndex);
        }

        // FR-R1: defer the actual swap to the next scheduler turn so the controller
        // never observes a half-mounted drive mid-byte. The half-byte hazard only
        // exists while the controller is actively transferring (motor on with a wired
        // scheduler); in any other state we apply the mount immediately so it is
        // observable without requiring a scheduler tick. This matters before the
        // machine has booted (the scheduler is wired but not running) — otherwise the
        // pre-boot insert would be queued behind a tick that never fires before the
        // boot path samples drive 1, and the machine would fall back to the diskless
        // Applesoft prompt instead of booting from the inserted image.
        if (context is null || !motorOn)
        {
            ApplyMount(driveIndex, media, imagePath);
            return;
        }

        var ctx = context;
        ctx.Scheduler.ScheduleAfter(
            Cycle.One,
            ScheduledEventKind.DeferredWork,
            priority: 0,
            callback: _ => ApplyMount(driveIndex, media, imagePath),
            tag: this);
    }

    /// <inheritdoc />
    public bool Eject(int driveIndex)
    {
        ValidateDriveIndex(driveIndex);
        var drive = drives[driveIndex];
        if (drive.Media is null)
        {
            return false;
        }

        // FR-R2: flush first; if flush fails, reject the eject. We do a *probe*
        // flush here so callers immediately learn that the eject was refused, but
        // the authoritative flush happens inside the deferred callback so any
        // writes the CPU performs in the gap between Eject() and ApplyEject (the
        // FR-R1 "no half-state mid-byte" window) are also persisted.
        try
        {
            drive.FlushOrThrow();
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "DiskII (slot {Slot}): eject of drive {Drive} rejected — flush failed.", SlotNumber, driveIndex + 1);
            return false;
        }

        if (context is null || !motorOn)
        {
            ApplyEject(driveIndex);
            return true;
        }

        var ctx = context;
        ctx.Scheduler.ScheduleAfter(
            Cycle.One,
            ScheduledEventKind.DeferredWork,
            priority: 0,
            callback: _ => ApplyEject(driveIndex),
            tag: this);
        return true;
    }

    /// <inheritdoc />
    public void Flush(int driveIndex)
    {
        ValidateDriveIndex(driveIndex);
        drives[driveIndex].Flush(logger);
    }

    /// <inheritdoc />
    public DriveSnapshot GetDriveSnapshot(int driveIndex)
    {
        ValidateDriveIndex(driveIndex);
        var drive = drives[driveIndex];
        return new DriveSnapshot(
            Selected: driveIndex == currentDrive,
            MotorOn: motorOn && driveIndex == currentDrive,
            PhaseLatch: drive.PhaseLatch,
            QuarterTrack: drive.QuarterTrack,
            WriteProtect: drive.Media?.IsReadOnly ?? false,
            HasMedia: drive.Media is not null,
            MountedImagePath: drive.ImagePath,
            Geometry: drive.Media?.Geometry);
    }

    /// <inheritdoc />
    public I525Media? GetMedia(int driveIndex)
    {
        ValidateDriveIndex(driveIndex);
        return drives[driveIndex].Media;
    }

    /// <inheritdoc />
    public DiskActivitySnapshot GetActivitySnapshot()
    {
        var perDrive = new DiskDriveActivity[DriveCountValue];
        for (var i = 0; i < DriveCountValue; i++)
        {
            var d = drives[i];
            perDrive[i] = new DiskDriveActivity(
                ObservedAddressFields: d.ObservedAddressFields,
                ObservedAddressFieldChecksumErrors: d.ObservedAddressFieldChecksumErrors,
                LastObservedVolume: d.HasObservedAddressField ? d.LastObservedVolume : null,
                LastObservedTrack: d.HasObservedAddressField ? d.LastObservedTrack : null,
                LastObservedSector: d.HasObservedAddressField ? d.LastObservedSector : null,
                LastObservedChecksum: d.HasObservedAddressField ? d.LastObservedChecksum : null,
                LastObservedChecksumValid: d.HasObservedAddressField ? d.LastObservedChecksumValid : null,
                BytesServedOnCurrentTrack: d.BytesServedOnCurrentTrack,
                ObservedDataPrologues: d.ObservedDataPrologues,
                ObservedDataFieldDecodeSuccesses: d.ObservedDataFieldDecodeSuccesses,
                ObservedDataFieldChecksumErrors: d.ObservedDataFieldChecksumErrors,
                ObservedDataFieldDecodeErrors: d.ObservedDataFieldDecodeErrors,
                ObservedDataFieldEpilogueMismatches: d.ObservedDataFieldEpilogueMismatches,
                LastDataPrologueGapBytes: d.HasMeasuredDataPrologueGap ? d.LastDataPrologueGapBytes : null,
                MinDataPrologueGapBytes: d.HasMeasuredDataPrologueGap ? d.MinDataPrologueGapBytes : null,
                MaxDataPrologueGapBytes: d.HasMeasuredDataPrologueGap ? d.MaxDataPrologueGapBytes : null);
        }

        return new DiskActivitySnapshot(
            dataReadCount: dataReadCount,
            freshByteCount: freshByteCount,
            settleSuppressedReadCount: settleSuppressedReadCount,
            staleByteCount: staleByteCount,
            dataWriteCount: dataWriteCount,
            phasePulseCount: phasePulseCount,
            phaseNoOpCount: phaseNoOpCount,
            trackChangeCount: trackChangeCount,
            motorOnCount: motorOnCount,
            motorOffCount: motorOffCount,
            driveSelectCount: driveSelectCount,
            cacheLoadCount: cacheLoadCount,
            cacheFlushCount: cacheFlushCount,
            drives: perDrive);
    }

    private static void ValidateDriveIndex(int driveIndex)
    {
        if ((uint)driveIndex >= DriveCountValue)
        {
            throw new ArgumentOutOfRangeException(nameof(driveIndex), driveIndex, $"Drive index must be 0..{DriveCountValue - 1}.");
        }
    }

    // ─── Phase stepper ─────────────────────────────────────────────────
    private byte PhaseAccess(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            UpdatePhase((byte)(offset & 0x0F));
        }

        return ReadDataPath(ctx);
    }

    private void PhaseAccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            UpdatePhase((byte)(offset & 0x0F));
        }
    }

    private void UpdatePhase(byte relativeOffset)
    {
        // Even offsets de-energize the phase, odd offsets energize it.
        int phase = relativeOffset >> 1;
        bool turnOn = (relativeOffset & 1) != 0;
        var drive = drives[currentDrive];
        var oldLatch = drive.PhaseLatch;
        int newLatch;
        if (turnOn)
        {
            newLatch = oldLatch | (1 << phase);
        }
        else
        {
            newLatch = oldLatch & ~(1 << phase);
        }

        drive.PhaseLatch = newLatch;

        if (!turnOn || newLatch == oldLatch)
        {
            return;
        }

        // FR-D4: classic four-phase stepper. The newly energized phase pulls the head
        // toward the closest matching quarter-track; only valid (adjacent) sequences
        // produce movement. Compare the newly active phase index against the current
        // logical phase derived from the head position.
        int currentPhase = (drive.QuarterTrack >> 1) & 3;
        int diff = (phase - currentPhase) & 3;
        int delta = diff switch
        {
            1 => 2,    // forward one half-track (two quarter-tracks)
            3 => -2,   // backward one half-track
            _ => 0,    // 0 = same phase, 2 = opposite (no net movement)
        };

        if (delta == 0)
        {
            phaseNoOpCount++;
            return;
        }

        var media = drive.Media;
        var qtCount = media is not null ? media.Geometry.QuarterTrackCount : 4 * 35;

        // FR-D4: clamp at track 0 and the last even quarter-track that still maps
        // to a real cylinder. For a 35-track disk this is qt=136 (= track 34); for
        // a 36-track image (the occasional extra outer cylinder shipped by a few
        // period titles and copy-protection schemes) this is qt=140 (= track 35).
        // The stepper moves in two-quarter-track increments, so the highest
        // reachable even index is `qtCount - 4` — the last whole-track boundary —
        // which is driven by the mounted media's geometry (Geometry.QuarterTrackCount)
        // rather than a hard-coded 35-track constant, so the four extra quads on a
        // 36-track image are reachable. An earlier formula clamped at
        // `2 * trackCount - 2` which limited the head to qt=68 (= track 17 / $11)
        // on standard media, making the upper half of the disk unreachable —
        // anything beyond DOS catalog track $11 silently became a permanent
        // I/O ERROR because RWTS read back the address-field track of the clamp
        // position instead of the requested track.
        var maxQuarter = qtCount - 4;
        var newQuarter = drive.QuarterTrack + delta;
        if (newQuarter < 0)
        {
            newQuarter = 0;
        }
        else if (newQuarter > maxQuarter)
        {
            newQuarter = maxQuarter;
        }

        if (newQuarter != drive.QuarterTrack)
        {
            // Track changed → flush the previous track if it was dirty (FR-D8) and
            // trigger a track-step settling delay (FR-D7).
            if (drive.OnTrackChanging(logger))
            {
                cacheFlushCount++;
            }

            var oldQuarter = drive.QuarterTrack;
            drive.QuarterTrack = newQuarter;
            drive.BytesServedOnCurrentTrack = 0;
            phasePulseCount++;
            trackChangeCount++;
            ScheduleTrackStepSettle(drive);

            if (logger.IsEnabled(LogEventLevel.Verbose))
            {
                logger.Verbose(
                    "DiskII (slot {Slot}): drive {Drive} track step qt={OldQuarterTrack}→{NewQuarterTrack} (delta {Delta}).",
                    SlotNumber,
                    currentDrive + 1,
                    oldQuarter,
                    newQuarter,
                    delta);
            }
        }
        else
        {
            phaseNoOpCount++;
        }
    }

    private void ScheduleTrackStepSettle(Drive525 drive)
    {
        if (context is null)
        {
            drive.SettleUntil = (Cycle)(ulong)trackStepSettleCycles;
            return;
        }

        drive.SettleUntil = context.Now + (Cycle)(ulong)trackStepSettleCycles;
    }

    // ─── Motor / drive select ───────────────────────────────────────────
    private byte MotorOffAccess(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SetMotor(false);
        }

        return ReadDataPath(ctx);
    }

    private void MotorOffAccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SetMotor(false);
        }
    }

    private byte MotorOnAccess(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SetMotor(true);
        }

        return ReadDataPath(ctx);
    }

    private void MotorOnAccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SetMotor(true);
        }
    }

    private void SetMotor(bool on)
    {
        if (motorOn == on)
        {
            return;
        }

        // Any motor-state transition invalidates the byte-ready spin-position
        // record: the spin advance model restarts when the motor turns on/off.
        lastReadSpinPosition = -1;

        if (!on)
        {
            // FR-D8: motor-off triggers a flush.
            if (drives[currentDrive].Flush(logger))
            {
                cacheFlushCount++;
            }

            motorOn = false;
            motorOffCount++;
            CancelDriftEvent();

            if (logger.IsEnabled(LogEventLevel.Verbose))
            {
                logger.Verbose(
                    "DiskII (slot {Slot}): motor OFF (active drive {Drive}, qt={QuarterTrack}).",
                    SlotNumber,
                    currentDrive + 1,
                    drives[currentDrive].QuarterTrack);
            }

            return;
        }

        motorOn = true;
        motorOnCount++;
        var drive = drives[currentDrive];
        if (context is not null)
        {
            drive.SettleUntil = context.Now + (Cycle)(ulong)motorSettleCycles;
            drive.LastUpdateCycle = context.Now;
            ScheduleDriftEvent(drive);
        }
        else
        {
            drive.SettleUntil = (Cycle)(ulong)motorSettleCycles;
        }

        if (logger.IsEnabled(LogEventLevel.Verbose))
        {
            logger.Verbose(
                "DiskII (slot {Slot}): motor ON (active drive {Drive}, qt={QuarterTrack}, settle={SettleCycles} cycles).",
                SlotNumber,
                currentDrive + 1,
                drive.QuarterTrack,
                motorSettleCycles);
        }
    }

    private byte SelectDrive1Access(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SelectDrive(0);
        }

        return ReadDataPath(ctx);
    }

    private void SelectDrive1AccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SelectDrive(0);
        }
    }

    private byte SelectDrive2Access(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SelectDrive(1);
        }

        return ReadDataPath(ctx);
    }

    private void SelectDrive2AccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SelectDrive(1);
        }
    }

    private void SelectDrive(int index)
    {
        if (currentDrive == index)
        {
            return;
        }

        // FR-D8: drive deselect flushes the outgoing drive.
        if (drives[currentDrive].Flush(logger))
        {
            cacheFlushCount++;
        }

        var oldDrive = currentDrive;
        currentDrive = index;
        driveSelectCount++;

        // Changing drives invalidates the spin-position recorded for byte-ready gating:
        // the new drive's position has no relation to the old one.
        lastReadSpinPosition = -1;

        if (motorOn && context is not null)
        {
            drives[currentDrive].LastUpdateCycle = context.Now;
        }

        if (logger.IsEnabled(LogEventLevel.Verbose))
        {
            logger.Verbose(
                "DiskII (slot {Slot}): drive select {OldDrive}→{NewDrive} (qt={QuarterTrack}).",
                SlotNumber,
                oldDrive + 1,
                currentDrive + 1,
                drives[currentDrive].QuarterTrack);
        }
    }

    // ─── Q6/Q7 + data path ──────────────────────────────────────────────
    private byte Q6LAccess(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            // $C0nC: Q6 → 0. With Q7=0 this is the data-read address; with Q7=1 it
            // shifts the prepared write byte onto the track.
            if (q7High)
            {
                ShiftWriteByte();
            }

            q6High = false;
            AdvanceSpinAndLatch();
        }

        return ReadDataPath(ctx);
    }

    private void Q6LAccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            // On real Disk II hardware, writing to $C0EC with Q7 high simply clocks
            // the *already-latched* byte (loaded via Q6=1,Q7=1) out to the head; the
            // value of the STA itself is ignored on the data path. Reusing it here
            // would silently corrupt the on-disk contents.
            if (q7High)
            {
                ShiftWriteByte();
            }

            q6High = false;
            AdvanceSpinAndLatch();
        }
    }

    private byte Q6HAccess(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            q6High = true;
        }

        return ReadDataPath(ctx);
    }

    private void Q6HAccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            // Q6=1, Q7=1: load write latch with shift-register contents.
            if (q7High)
            {
                writeShift = value;
            }

            q6High = true;
        }
    }

    private byte Q7LAccess(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            // On a true high→low transition (entering read mode), invalidate the
            // byte-ready spin-position record so the first data-read after a write
            // session is not suppressed by a stale value. The guard is intentionally
            // checked before clearing q7High so that it tests the *previous* state.
            if (q7High)
            {
                lastReadSpinPosition = -1;
            }

            q7High = false;
        }

        return ReadDataPath(ctx);
    }

    private void Q7LAccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            // Same high→low transition guard as Q7LAccess.
            if (q7High)
            {
                lastReadSpinPosition = -1;
            }

            q7High = false;
        }
    }

    private byte Q7HAccess(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            q7High = true;
        }

        return ReadDataPath(ctx);
    }

    private void Q7HAccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            q7High = true;

            // Q6=1, Q7=1 is the "load write latch" path (FR-D5).
            if (q6High)
            {
                writeShift = value;
            }
        }
    }

    private byte ReadDataPath(in BusAccess ctx)
    {
        // The data path returns the current latch byte for any soft-switch read.
        // Q6=1, Q7=0 returns the write-protect status (high bit).
        if (q6High && !q7High)
        {
            var drive = drives[currentDrive];
            return drive.Media?.IsReadOnly == true ? (byte)0x80 : (byte)0x00;
        }

        if (!motorOn)
        {
            return FloatingByte;
        }

        var current = drives[currentDrive];
        if (context is not null && current.SettleUntil > context.Now)
        {
            // During settling (motor warm-up or post-step head settling), real
            // Disk II hardware does not produce a stable byte-ready signal: the
            // head may be over track gaps or unstable nibbles. Model this by
            // clearing bit 7 of whatever is currently on the data latch so that
            // DOS RWTS's `LDA $C08C,X / BPL *-3` polling loop spins waiting for
            // a fresh byte instead of immediately consuming the stale latch
            // contents. This is essential for multi-track loads: a track step
            // sets SettleUntil = now + trackStepSettleCycles (~30 ms), and RWTS
            // begins polling for address prologues almost immediately afterward;
            // returning the latched byte with bit 7 set would cause RWTS to read
            // the same non-$D5 nibble thousands of times per settle window,
            // burning its 48-retry budget and producing a spurious "I/O ERROR"
            // on any file that crosses tracks. FR-D7.
            if (!ctx.IsSideEffectFree && !q6High && !q7High)
            {
                dataReadCount++;
                settleSuppressedReadCount++;
            }

            return (byte)(LatchedOrFloating() & 0x7F);
        }

        // Model the Disk II shift-register byte-ready behavior in read mode
        // (Q6=0, Q7=0). On real hardware the data register's high bit is set only
        // when a complete nibble has just been clocked in; consecutive reads
        // between byte boundaries see bit 7 cleared because the next byte is
        // still shifting in. The P5A boot ROM relies on this with its
        // `LDA $C08C,X / BPL *-3` timing loop. We approximate this by tracking
        // whether SpinPosition advanced since the previous read: when it did,
        // the byte is "fresh" and bit 7 reflects the actual nibble (every GCR
        // nibble has bit 7 set); when it didn't, the firmware is polling faster
        // than bytes arrive and we report bit 7 cleared so the BPL loop spins.
        if (!q6High && !q7High && dataLatchValid && !ctx.IsSideEffectFree)
        {
            dataReadCount++;
            if (current.SpinPosition == lastReadSpinPosition)
            {
                staleByteCount++;
                return (byte)(dataLatch & 0x7F);
            }

            lastReadSpinPosition = current.SpinPosition;
            freshByteCount++;
            current.BytesServedOnCurrentTrack++;
            ObserveStreamByte(current, dataLatch);
            return dataLatch;
        }

        // For non-data-read modes (write enable, write load), reads return whatever
        // is on the floating bus; we return the last latched byte for determinism.
        return LatchedOrFloating();
    }

    /// <summary>
    /// Feeds a freshly clocked nibble into the per-drive stream parser. Maintains a
    /// sliding three-byte window matching both the GCR address-field prologue
    /// (<c>$D5 $AA $96</c>) and the GCR data-field prologue (<c>$D5 $AA $AD</c>).
    /// </summary>
    /// <param name="drive">The drive whose stream is being observed.</param>
    /// <param name="value">The fresh byte being served to the CPU.</param>
    /// <remarks>
    /// <para>
    /// On an address-prologue hit the next eight bytes are decoded as 4-and-4
    /// volume / track / sector / checksum; on a data-prologue hit the next 345 bytes
    /// (343 data + 2 epilogue) are captured, run through the 6-and-2 XOR-chain
    /// decode using <see cref="GcrEncoder.GetReadTable"/>, and classified as
    /// success / decode-error (bad 6-and-2 nibble) / checksum-error (non-zero XOR
    /// residual) / epilogue-mismatch (bytes after the 343 weren't <c>$DE $AA</c>).
    /// The bytes between an address-field decode and the next data prologue are
    /// recorded as a paired gap so the <c>diskmon</c> surface can show whether
    /// the data prologue lands inside RWTS's ~60-byte scan window.
    /// </para>
    /// <para>
    /// Parses what the CPU actually sees, not what is on disk: if the byte-ready
    /// timing model serves duplicate or skipped bytes, the parser sees that too,
    /// which is exactly what makes this signal useful for diagnosing seek / settle
    /// bugs where the byte stream the CPU consumes differs from the encoded image.
    /// </para>
    /// </remarks>
    private void ObserveStreamByte(Drive525 drive, byte value)
    {
        // Shift the 3-byte sliding window forward.
        drive.SlidingW2 = drive.SlidingW1;
        drive.SlidingW1 = drive.SlidingW0;
        drive.SlidingW0 = value;

        // If we are tracking the gap to the next data prologue, count this fresh byte.
        if (drive.DataPrologueGapActive)
        {
            // Cap at int.MaxValue so the counter never wraps; any sane gap is < 10000.
            if (drive.DataPrologueGapBytes < int.MaxValue)
            {
                drive.DataPrologueGapBytes++;
            }
        }

        if (drive.AddressParseStage > 0)
        {
            // Collecting 8 bytes after $D5 $AA $96.
            drive.AddressParseBuffer[drive.AddressParseStage - 1] = value;
            drive.AddressParseStage++;
            if (drive.AddressParseStage > 8)
            {
                // 4-and-4 decode: each pair (hi, lo) of nibbles encodes one byte as
                // ((hi << 1) | 1) & lo. Bytes are vol, trk, sec, chk in that order.
                var buf = drive.AddressParseBuffer;
                var vol = (byte)(((buf[0] << 1) | 1) & buf[1]);
                var trk = (byte)(((buf[2] << 1) | 1) & buf[3]);
                var sec = (byte)(((buf[4] << 1) | 1) & buf[5]);
                var chk = (byte)(((buf[6] << 1) | 1) & buf[7]);
                drive.LastObservedVolume = vol;
                drive.LastObservedTrack = trk;
                drive.LastObservedSector = sec;
                drive.LastObservedChecksum = chk;
                var expected = (byte)(vol ^ trk ^ sec);
                drive.LastObservedChecksumValid = expected == chk;
                drive.HasObservedAddressField = true;
                drive.ObservedAddressFields++;
                if (!drive.LastObservedChecksumValid)
                {
                    drive.ObservedAddressFieldChecksumErrors++;
                }

                drive.AddressParseStage = 0;

                if (logger.IsEnabled(LogEventLevel.Verbose))
                {
                    logger.Verbose(
                        "DiskII (slot {Slot}): drive {Drive} qt={QuarterTrack} address field vol=${Volume:X2} trk=${Track:X2} sec=${Sector:X2} chk=${Checksum:X2} ({ChecksumStatus}).",
                        SlotNumber,
                        currentDrive + 1,
                        drive.QuarterTrack,
                        vol,
                        trk,
                        sec,
                        chk,
                        drive.LastObservedChecksumValid ? "ok" : "BAD");
                }

                // Start a fresh gap measurement: bytes from here to the next data
                // prologue. Reset any in-flight data-field parse — RWTS only honours
                // the data field that follows the most recent address field.
                drive.DataPrologueGapActive = true;
                drive.DataPrologueGapBytes = 0;
                drive.DataParseStage = 0;
            }

            return;
        }

        if (drive.DataParseStage > 0)
        {
            // Collecting 345 bytes after $D5 $AA $AD: 343 encoded nibbles + 2 epilogue.
            drive.DataParseBuffer[drive.DataParseStage - 1] = value;
            drive.DataParseStage++;
            if (drive.DataParseStage > DataFieldCaptureLength)
            {
                FinaliseDataField(drive);
                drive.DataParseStage = 0;
            }

            return;
        }

        if (drive.SlidingW2 == 0xD5 && drive.SlidingW1 == 0xAA)
        {
            if (drive.SlidingW0 == 0x96)
            {
                drive.AddressParseStage = 1;
            }
            else if (drive.SlidingW0 == 0xAD)
            {
                drive.ObservedDataPrologues++;
                if (drive.DataPrologueGapActive)
                {
                    var gap = drive.DataPrologueGapBytes;

                    // The gap counter started incrementing on the byte after the
                    // 8th 4-and-4 address byte and includes the trailing $DE $AA
                    // $EB address-field epilogue (3 bytes), the inter-field gap
                    // proper, and the leading $D5 $AA $AD data prologue (3 bytes)
                    // itself. Subtract 6 so the reported gap equals the bytes
                    // RWTS would actually scan past after the address field's
                    // epilogue (the standard track layout puts the data prologue
                    // exactly 5 bytes after the address epilogue; RWTS's scan
                    // window is ~60 bytes, so any gap > ~50 here is a smoking gun
                    // for the encoder laying fields outside the scan window).
                    var paired = gap > 6 ? gap - 6 : 0;
                    drive.LastDataPrologueGapBytes = paired;
                    if (!drive.HasMeasuredDataPrologueGap || paired < drive.MinDataPrologueGapBytes)
                    {
                        drive.MinDataPrologueGapBytes = paired;
                    }

                    if (!drive.HasMeasuredDataPrologueGap || paired > drive.MaxDataPrologueGapBytes)
                    {
                        drive.MaxDataPrologueGapBytes = paired;
                    }

                    drive.HasMeasuredDataPrologueGap = true;
                    drive.DataPrologueGapActive = false;
                }

                drive.DataParseStage = 1;
            }
        }
    }

    /// <summary>
    /// Runs the GCR 6-and-2 XOR-chain decode on the 343 nibbles captured after a
    /// <c>$D5 $AA $AD</c> data prologue, validates the trailing two-byte epilogue,
    /// and updates the per-drive data-field counters accordingly.
    /// </summary>
    /// <param name="drive">The drive whose captured data field should be decoded.</param>
    private void FinaliseDataField(Drive525 drive)
    {
        var buf = drive.DataParseBuffer;

        // First decode all 343 nibbles via the inverse 6-and-2 read table. Any byte
        // that isn't a valid 6-and-2 nibble (read table returns 0xFF) means RWTS
        // would have rejected the field outright.
        var readTable = ReadTable;
        for (var i = 0; i < 343; i++)
        {
            if (readTable[buf[i]] == 0xFF)
            {
                drive.ObservedDataFieldDecodeErrors++;
                if (logger.IsEnabled(LogEventLevel.Verbose))
                {
                    logger.Verbose(
                        "DiskII (slot {Slot}): drive {Drive} qt={QuarterTrack} data field after vol=${Volume:X2} trk=${Track:X2} sec=${Sector:X2} → DECODE-ERROR (invalid nibble ${BadNibble:X2} at offset {Offset}).",
                        SlotNumber,
                        currentDrive + 1,
                        drive.QuarterTrack,
                        drive.LastObservedVolume,
                        drive.LastObservedTrack,
                        drive.LastObservedSector,
                        buf[i],
                        i);
                }

                return;
            }
        }

        // XOR-chain decode: each nibble's 6-bit value is XOR'd with a running
        // accumulator; the chain's final residual must be zero for the checksum to
        // verify.
        byte last = 0;
        for (var i = 0; i < 343; i++)
        {
            last ^= readTable[buf[i]];
        }

        var checksumValid = last == 0;
        var epilogueValid = buf[343] == 0xDE && buf[344] == 0xAA;

        if (checksumValid)
        {
            drive.ObservedDataFieldDecodeSuccesses++;
        }
        else
        {
            drive.ObservedDataFieldChecksumErrors++;
        }

        if (!epilogueValid)
        {
            drive.ObservedDataFieldEpilogueMismatches++;
        }

        if (logger.IsEnabled(LogEventLevel.Verbose))
        {
            string outcome = checksumValid
                ? (epilogueValid ? "ok" : "ok (epilogue-mismatch)")
                : (epilogueValid ? "CHECKSUM-ERROR" : "CHECKSUM-ERROR + epilogue-mismatch");

            logger.Verbose(
                "DiskII (slot {Slot}): drive {Drive} qt={QuarterTrack} data field after vol=${Volume:X2} trk=${Track:X2} sec=${Sector:X2} → {Outcome} (epilogue ${EpilogueByte0:X2} ${EpilogueByte1:X2}).",
                SlotNumber,
                currentDrive + 1,
                drive.QuarterTrack,
                drive.LastObservedVolume,
                drive.LastObservedTrack,
                drive.LastObservedSector,
                outcome,
                buf[343],
                buf[344]);
        }
    }

    private byte LatchedOrFloating() => dataLatchValid ? dataLatch : FloatingByte;

    private void AdvanceSpinAndLatch()
    {
        if (!motorOn || q7High)
        {
            return;
        }

        var drive = drives[currentDrive];
        if (drive.Media is null)
        {
            dataLatch = FloatingByte;
            dataLatchValid = true;
            return;
        }

        if (context is not null && drive.SettleUntil > context.Now)
        {
            // Still settling — head is not yet stable; data latch keeps the previous byte.
            return;
        }

        // FR-D6: on-demand spin advance.
        if (drive.EnsureTrackLoaded())
        {
            cacheLoadCount++;
        }

        if (context is not null)
        {
            var now = context.Now;
            var elapsed = (ulong)(now - drive.LastUpdateCycle);
            var byteAdvance = elapsed / CyclesPerByte;
            if (byteAdvance > 0)
            {
                var tlen = drive.CachedTrack!.Length;
                drive.SpinPosition = (int)(((ulong)drive.SpinPosition + byteAdvance) % (ulong)tlen);
                drive.LastUpdateCycle += new Cycle(byteAdvance * CyclesPerByte);
            }
        }

        dataLatch = drive.CachedTrack![drive.SpinPosition];
        dataLatchValid = true;

        ScheduleDriftEvent(drive);
    }

    private void ShiftWriteByte()
    {
        var drive = drives[currentDrive];
        if (drive.Media is null || drive.Media.IsReadOnly)
        {
            return;
        }

        if (context is not null && drive.SettleUntil > context.Now)
        {
            return;
        }

        if (drive.EnsureTrackLoaded())
        {
            cacheLoadCount++;
        }

        // Advance the spin position for the staged write, then commit one byte to the
        // nibble cache and mark the track dirty (FR-D8 — sector images defer parse-
        // back to flush time; nibble images get the byte on flush as a raw write).
        if (context is not null)
        {
            var elapsed = (ulong)(context.Now - drive.LastUpdateCycle);
            var byteAdvance = elapsed / CyclesPerByte;
            if (byteAdvance > 0)
            {
                drive.SpinPosition = (int)(((ulong)drive.SpinPosition + byteAdvance) % (ulong)drive.CachedTrack!.Length);
                drive.LastUpdateCycle += new Cycle(byteAdvance * CyclesPerByte);
            }
        }

        drive.CachedTrack![drive.SpinPosition] = writeShift;
        drive.IsTrackDirty = true;
        drive.SpinPosition = (drive.SpinPosition + 1) % drive.CachedTrack!.Length;
        dataWriteCount++;
    }

    private void ScheduleDriftEvent(Drive525 drive)
    {
        if (context is null || driftScheduled)
        {
            return;
        }

        // FR-D6: a single rescheduling event prevents drift while the CPU is halted.
        // Schedule one event one full track ahead so we recompute even if the CPU
        // never touches $C0nC for a long period.
        var trackBytes = drive.CachedTrack?.Length ?? GcrEncoder.StandardTrackLength;
        driftHandle = context.Scheduler.ScheduleAfter(
            (Cycle)(ulong)(trackBytes * CyclesPerByte),
            ScheduledEventKind.DeviceTimer,
            priority: 0,
            callback: DriftRecompute,
            tag: this);
        driftScheduled = true;
    }

    private void DriftRecompute(IEventContext ctx)
    {
        driftScheduled = false;
        if (!motorOn)
        {
            return;
        }

        var drive = drives[currentDrive];
        if (drive.Media is null || drive.CachedTrack is null)
        {
            return;
        }

        var elapsed = (ulong)(ctx.Now - drive.LastUpdateCycle);
        var byteAdvance = elapsed / CyclesPerByte;
        if (byteAdvance > 0)
        {
            drive.SpinPosition = (int)(((ulong)drive.SpinPosition + byteAdvance) % (ulong)drive.CachedTrack.Length);
            drive.LastUpdateCycle += new Cycle(byteAdvance * CyclesPerByte);
        }

        ScheduleDriftEvent(drive);
    }

    private void CancelDriftEvent()
    {
        if (driftScheduled && context is not null)
        {
            context.Scheduler.Cancel(driftHandle);
        }

        driftScheduled = false;
    }

    private void ApplyMount(int driveIndex, I525Media media, string? imagePath)
    {
        var drive = drives[driveIndex];

        // FR-R3: hot-swap resets per-drive state so the next sector read starts cleanly.
        drive.Flush(logger);
        drive.ResetTransientState();
        drive.Media = media;
        drive.ImagePath = imagePath;

        // Reset byte-ready gating: the new disk's spin position has no relation
        // to what was recorded for the previous disk or drive session.
        if (driveIndex == currentDrive)
        {
            lastReadSpinPosition = -1;
        }

        // FR-R4: insertion during an active motor cycle resets the settling timer.
        if (motorOn && driveIndex == currentDrive && context is not null)
        {
            drive.SettleUntil = context.Now + (Cycle)(ulong)motorSettleCycles;
            drive.LastUpdateCycle = context.Now;
        }
    }

    private void ApplyEject(int driveIndex)
    {
        var drive = drives[driveIndex];

        // Re-flush in case the CPU dirtied the cached track between Eject() and
        // this deferred callback (FR-R1/FR-R2 — no half-state mid-byte means we
        // also can't drop a freshly written nibble at the swap point). We've
        // already validated up-front that flush succeeds, so a failure here is
        // unexpected; log it as a warning rather than crashing the scheduler.
        try
        {
            drive.FlushOrThrow();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            logger.Warning(ex, "DiskII (slot {Slot}): deferred-eject flush of drive {Drive} failed; data may be lost.", SlotNumber, driveIndex + 1);
        }

        drive.ResetTransientState();
        drive.Media = null;
        drive.ImagePath = null;

        // Reset byte-ready gating for the now-empty drive.
        if (driveIndex == currentDrive)
        {
            lastReadSpinPosition = -1;
        }
    }

    /// <summary>
    /// Per-drive state for the Disk II controller (FR-D2).
    /// </summary>
    private sealed class Drive525
    {
        public I525Media? Media { get; set; }

        public string? ImagePath { get; set; }

        public int QuarterTrack { get; set; }

        public int SpinPosition { get; set; }

        public int PhaseLatch { get; set; }

        public byte[]? CachedTrack { get; set; }

        public int CachedQuarterTrack { get; set; } = -1;

        public bool IsTrackDirty { get; set; }

        public Cycle LastUpdateCycle { get; set; }

        public Cycle SettleUntil { get; set; }

        // ─── Instrumentation: stream-observed address-field parser state ─────
        public byte SlidingW0 { get; set; }

        public byte SlidingW1 { get; set; }

        public byte SlidingW2 { get; set; }

        /// <summary>Gets or sets the 8-byte rolling buffer used to capture the vol/trk/sec/chk pairs that follow a <c>$D5 $AA $96</c> prologue.</summary>
        public byte[] AddressParseBuffer { get; } = new byte[8];

        /// <summary>Gets or sets the address-field decode stage: <c>0</c> = idle (watching for prologue); <c>1..8</c> = collecting bytes after prologue.</summary>
        public int AddressParseStage { get; set; }

        public bool HasObservedAddressField { get; set; }

        public int LastObservedVolume { get; set; }

        public int LastObservedTrack { get; set; }

        public int LastObservedSector { get; set; }

        public int LastObservedChecksum { get; set; }

        public bool LastObservedChecksumValid { get; set; }

        public long ObservedAddressFields { get; set; }

        public long ObservedAddressFieldChecksumErrors { get; set; }

        public long BytesServedOnCurrentTrack { get; set; }

        // ─── Instrumentation: stream-observed data-field parser state ────────

        /// <summary>Gets the 345-byte rolling buffer used to capture the 343 nibbles + 2 epilogue bytes that follow a <c>$D5 $AA $AD</c> data prologue.</summary>
        public byte[] DataParseBuffer { get; } = new byte[DataFieldCaptureLength];

        /// <summary>Gets or sets the data-field decode stage: <c>0</c> = idle (watching for prologue); <c>1..345</c> = collecting bytes after prologue.</summary>
        public int DataParseStage { get; set; }

        /// <summary>Gets or sets a value indicating whether a paired address→data gap is currently being measured (set after a successful address-field decode, cleared on the next data prologue).</summary>
        public bool DataPrologueGapActive { get; set; }

        /// <summary>Gets or sets the running byte count for the in-flight paired address→data gap.</summary>
        public int DataPrologueGapBytes { get; set; }

        /// <summary>Gets or sets a value indicating whether any paired address→data gap has been recorded.</summary>
        public bool HasMeasuredDataPrologueGap { get; set; }

        public int LastDataPrologueGapBytes { get; set; }

        public int MinDataPrologueGapBytes { get; set; }

        public int MaxDataPrologueGapBytes { get; set; }

        public long ObservedDataPrologues { get; set; }

        public long ObservedDataFieldDecodeSuccesses { get; set; }

        public long ObservedDataFieldChecksumErrors { get; set; }

        public long ObservedDataFieldDecodeErrors { get; set; }

        public long ObservedDataFieldEpilogueMismatches { get; set; }

        public void ResetTransientState()
        {
            QuarterTrack = 0;
            SpinPosition = 0;
            PhaseLatch = 0;
            CachedTrack = null;
            CachedQuarterTrack = -1;
            IsTrackDirty = false;
            LastUpdateCycle = Cycle.Zero;
            SettleUntil = Cycle.Zero;
            SlidingW0 = SlidingW1 = SlidingW2 = 0;
            AddressParseStage = 0;
            HasObservedAddressField = false;
            BytesServedOnCurrentTrack = 0;
            DataParseStage = 0;
            DataPrologueGapActive = false;
            DataPrologueGapBytes = 0;
            HasMeasuredDataPrologueGap = false;
        }

        /// <summary>
        /// Loads the cached nibble buffer for the current quarter-track if it isn't
        /// already loaded.
        /// </summary>
        /// <returns><see langword="true"/> if a load was actually performed; <see langword="false"/> if the cache was already valid for the current quarter-track.</returns>
        public bool EnsureTrackLoaded()
        {
            if (Media is null)
            {
                CachedTrack = null;
                CachedQuarterTrack = -1;
                return false;
            }

            if (CachedTrack is not null && CachedQuarterTrack == QuarterTrack)
            {
                return false;
            }

            // Reload the cached nibble buffer for the new quarter-track.
            var len = Media.OptimalTrackLength;
            CachedTrack ??= new byte[len];
            if (CachedTrack.Length != len)
            {
                CachedTrack = new byte[len];
            }

            Media.ReadTrack(QuarterTrack, CachedTrack);
            CachedQuarterTrack = QuarterTrack;
            IsTrackDirty = false;
            SpinPosition %= len;
            return true;
        }

        /// <summary>
        /// Handles a track change: flushes any dirty nibble cache and invalidates the
        /// cached track buffer so the next read reloads from the medium.
        /// </summary>
        /// <param name="logger">Serilog logger used to report flush failures.</param>
        /// <returns><see langword="true"/> if a dirty flush was performed; <see langword="false"/> otherwise.</returns>
        public bool OnTrackChanging(ILogger logger)
        {
            // Flush dirty nibbles for the outgoing track before stepping away.
            var flushed = Flush(logger);
            CachedTrack = null;
            CachedQuarterTrack = -1;
            IsTrackDirty = false;
            return flushed;
        }

        /// <summary>
        /// Writes any dirty cached nibbles back to the medium, swallowing the typical
        /// I/O / parse exception set into <paramref name="logger"/>.
        /// </summary>
        /// <param name="logger">Serilog logger used to report flush failures.</param>
        /// <returns><see langword="true"/> if a flush was performed; <see langword="false"/> if nothing was dirty.</returns>
        public bool Flush(ILogger logger)
        {
            if (Media is null || !IsTrackDirty || CachedTrack is null || CachedQuarterTrack < 0)
            {
                return false;
            }

            try
            {
                FlushOrThrow();
                return true;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
            {
                logger.Warning(ex, "DiskII: write-back of quarter-track {QuarterTrack} failed.", CachedQuarterTrack);
                IsTrackDirty = false;
                return false;
            }
        }

        public void FlushOrThrow()
        {
            if (Media is null || !IsTrackDirty || CachedTrack is null || CachedQuarterTrack < 0)
            {
                return;
            }

            Media.WriteTrack(CachedQuarterTrack, CachedTrack);
            Media.Flush();
            IsTrackDirty = false;
        }
    }
}