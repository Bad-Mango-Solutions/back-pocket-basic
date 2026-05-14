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
/// and clamps at 0 / <c>2 × trackCount − 2</c> (FR-D4).</description></item>
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
/// The class is annotated with <see cref="DeviceTypeAttribute"/> so that the auto-discovery
/// scan in <see cref="DeviceFactoryRegistry"/> recognises it as the canonical
/// <c>disk-ii-compatible</c> slot card. It does not have a public parameterless constructor
/// — it requires an injected <see cref="ILogger"/> and (typically) a boot ROM — so the
/// registry deliberately skips this type and instead auto-registers the simpler
/// <see cref="DiskIIControllerStub"/> as the fallback factory; the skip is reported via
/// <see cref="DeviceFactoryRegistry.SkippedDeviceTypes"/>. The configured / image-bearing
/// path registers a custom <c>Func&lt;MachineBuilder, JsonElement?, ISlotCard&gt;</c> that
/// constructs <see cref="DiskIIController"/> with its dependencies.
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

    private readonly SlotIOHandlers handlers = new();
    private readonly Drive525[] drives = new Drive525[DriveCountValue];
    private readonly IBusTarget? romRegion;
    private readonly IBusTarget expansionRomRegion;
    private readonly int motorSettleCycles;
    private readonly int trackStepSettleCycles;
    private readonly ILogger logger;

    // Controller-level state
    private int currentDrive;          // 0 or 1
    private bool motorOn;
    private bool q6High;
    private bool q7High;
    private byte dataLatch;            // last byte returned to / received from CPU
    private bool dataLatchValid;       // true once the head has produced (or written) a byte
    private byte writeShift;           // staged byte for write-load (Q6=1,Q7=1)

    private IEventContext? context;
    private EventHandle driftHandle;
    private bool driftScheduled;

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

        // FR-R1: defer the actual swap to the next scheduler turn so the controller
        // never observes a half-mounted drive mid-byte. If no scheduler is wired yet
        // (e.g. unit tests that haven't called Initialize), apply immediately —
        // tests are explicit about not running CPU during construction.
        if (context is null)
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
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            logger.Warning(ex, "DiskII (slot {Slot}): eject of drive {Drive} rejected — flush failed.", SlotNumber, driveIndex + 1);
            return false;
        }

        if (context is null)
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
            return;
        }

        var media = drive.Media;
        var qtCount = media is not null ? media.Geometry.QuarterTrackCount : 4 * 35;

        // FR-D4: clamp at track 0 and `2 * trackCount - 2`, i.e. the last even
        // quarter-track since the stepper moves in two-quarter-track increments.
        var trackCount = qtCount / 4;
        var maxQuarter = (2 * trackCount) - 2;
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
            drive.OnTrackChanging(logger);
            drive.QuarterTrack = newQuarter;
            ScheduleTrackStepSettle(drive);
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

        if (!on)
        {
            // FR-D8: motor-off triggers a flush.
            drives[currentDrive].Flush(logger);
            motorOn = false;
            CancelDriftEvent();
            return;
        }

        motorOn = true;
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
        drives[currentDrive].Flush(logger);
        currentDrive = index;
        if (motorOn && context is not null)
        {
            drives[currentDrive].LastUpdateCycle = context.Now;
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
            q7High = false;
        }

        return ReadDataPath(ctx);
    }

    private void Q7LAccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
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

        if (context is not null && drives[currentDrive].SettleUntil > context.Now)
        {
            // During settling, return the previously latched byte (or floating $FF
            // when nothing has been latched yet). FR-D7.
            return LatchedOrFloating();
        }

        // For non-data-read modes (write enable, write load), reads return whatever
        // is on the floating bus; we return the last latched byte for determinism.
        return LatchedOrFloating();
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
        drive.EnsureTrackLoaded();
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

        drive.EnsureTrackLoaded();

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
        }

        public void EnsureTrackLoaded()
        {
            if (Media is null)
            {
                CachedTrack = null;
                CachedQuarterTrack = -1;
                return;
            }

            if (CachedTrack is not null && CachedQuarterTrack == QuarterTrack)
            {
                return;
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
        }

        public void OnTrackChanging(ILogger logger)
        {
            // Flush dirty nibbles for the outgoing track before stepping away.
            Flush(logger);
            CachedTrack = null;
            CachedQuarterTrack = -1;
            IsTrackDirty = false;
        }

        public void Flush(ILogger logger)
        {
            try
            {
                FlushOrThrow();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException)
            {
                logger.Warning(ex, "DiskII: write-back of quarter-track {QuarterTrack} failed.", CachedQuarterTrack);
                IsTrackDirty = false;
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