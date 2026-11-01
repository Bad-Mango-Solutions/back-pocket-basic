// <copyright file="DiskIIController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Cpu;

using BadMango.Emulator.Devices.Interfaces;
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

    /// <summary>
    /// AppleWin <c>SPINNING_CYCLES</c>: ~1 s of platter inertia after motor-off before
    /// the data path stops advancing (Disk.cpp <c>m_spinning</c>, GH#599).
    /// </summary>
    public const int DefaultSpinningCycles = 1_000_000;

    /// <summary>
    /// Minimum idle bus cycles before AppleWin-style Disk II mode applies a time-based
    /// nibble catch-up on the next full data-latch read (see AppleWin <c>Disk.cpp</c>
    /// <c>ReadWrite</c>, GH#125).
    /// </summary>
    private const int SpinCatchUpThresholdCycles = 40;

    /// <summary>
    /// Maximum cycles between two full data-latch reads that still qualify as a rapid
    /// BPL poll; within this window the latch is reported with bit 7 cleared and the
    /// head position is not clocked forward (AppleWin <c>kReadAccessThreshold</c>).
    /// End-of-cycle <see cref="Drive525.LastLatchReadCycle"/> anchoring makes the
    /// 7-cycle <c>LDY $C08C,X / BPL</c> taken loop measure gap 6 (partial), so the next
    /// poll at gap 12–14 full-clocks — without this, every 7-cycle poll would advance the
    /// head and ProDOS LC snapshot decrypt over-rotates by hundreds of nibbles.
    /// </summary>
    private const int RapidReadThresholdCycles = 6;

    /// <summary>
    /// Nibble advance from pass-2 motor-on to inner decrypt anchor on qt=16
    /// (bisect: start 4577 → inner at 5539). 4969 is the first <c>$A7</c> anchor
    /// with correct idx-$93 decrypt but fails outer EOR (A=$34, poll=$96); 5539 is
    /// the nearest anchor that also satisfies <c>A XOR key[poll]==0</c> at outer+256.
    /// </summary>
    private const int Pass2InnerSpinDelta = 962;

    /// <summary>
    /// Pass-2 bisect pre-inner scan over-clocks on gap-14 polls; distribute
    /// <see cref="Pass2BisectGap14SkipNumerator"/> skip slots across every
    /// <see cref="Pass2BisectGap14SkipDenominator"/> such reads (177/473 ≈ 37.4%).
    /// </summary>
    private const int Pass2BisectGap14SkipNumerator = 177;

    private const int Pass2BisectGap14SkipDenominator = 473;







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
    private readonly IBusTarget? expansionRomRegion;
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
    private bool lastDataReadWasFresh; // Set by AdvanceSpinAndLatch; consumed by ReadDataPath for byte-ready / parser
    private bool dataPathClockedThisAccess; // True only after $C0nC AdvanceSpinAndLatch; suppresses phase/motor false data-read hits

    /// <summary>
    /// Card-level last <c>$C0nC</c> access time for AppleWin-style time spin
    /// (<see cref="ApplyMotorTimeSpin"/>; Disk.cpp <c>m_diskLastCycle</c>).
    /// </summary>
    private Cycle diskLastCycle;

    /// <summary>
    /// Anchor for coast-down decay while the motor is off (AppleWin <c>Update</c>).
    /// </summary>
    private Cycle spinDecayAnchorCycle;

    private EventHandle spinDecayHandle;

    private IEventContext? context;
    private bool autostartTrapRegistered;
    private bool prodosSnapshotTrapRegistered;

    // Last $C0EC poll diagnostics (set during ProDOS LC snapshot pass 2).
    private ulong lastSnapshotPollReadGap;
    private byte lastSnapshotPollLatch;
    private int lastSnapshotPollSpinPosition;
    private bool lastSnapshotPollWasFresh;

    // Spin accounting (diagnostic; reset on motor-off / mount).
    private long timeSpinNibbleCount;
    private long settleIntegrateNibbleCount;
    private long latchClockOutCount;
    private long partialLatchReadCount;
    private long readGapBucketLe6;
    private long readGapBucket7to14;
    private long readGapBucket15to28;
    private long readGapBucketGt28;
    private int motorOnSpinPosition;

    // Pass-2 LC snapshot spin bisection (slot 6): active from late pass-1 motor-on through inner-entry.
    private bool spinBisectActive;

    /// <summary>
    /// Set once pass-2 inner-entry completes so post-snapshot motor-on does not re-arm
    /// <see cref="spinBisectActive"/> and apply bisect gap-14 corrections to normal I/O.
    /// </summary>
    private bool pass2SnapshotBisectDone;
    private ulong spinBisectStartCycle;
    private int spinBisectStartSpin;
    private long spinBisectReadSeq;
    private int pass2BisectGap14Seq;

    // Pass-2 outer sync poll diagnostics (armed at outer-entry, pass=$56).
    private int pass2OuterPollDiagRemaining;

    /// <summary>
    /// Bus time when the motor last turned off while the platter was still coasting; used to
    /// apply exactly one post-off rotation catch-up on re-energise instead of folding the
    /// off window into the next <see cref="ApplyMotorTimeSpin"/> gap (GH#125).
    /// </summary>
    private Cycle motorOffAnchorCycle;

    /// <summary>
    /// Apple IIe Enhanced autostart ROM executes <c>JMP ($03F0)</c> here after the slot scan.
    /// Disk II P6 boot ROM has <c>$C6FF=$00</c> (not bootable per generic scan) so the vector
    /// is often still zero; we redirect to <c>$Cn00</c> when a boot disk is mounted.
    /// </summary>
    private const Addr IieAutostartBootVectorJmp = 0xFA56;

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
        expansionRomRegion = null;

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

        // Register early so a disk insert + boot in the same batch always has the trap
        // installed before autostart reaches $FA56; media presence is checked at fire time.
        EnsureAutostartTrapRegistered();
        EnsureProdosSnapshotTrapRegistered();
    }

    /// <summary>
    /// Registers ProDOS LC snapshot diagnostic traps (slot 6 only).
    /// </summary>
    private void EnsureProdosSnapshotTrapRegistered()
    {
        if (context is not null)
        {
            RegisterProdosSnapshotTrapIfNeeded(context);
        }
    }

    /// <summary>
    /// Registers call traps for ProDOS 2.0.3 kernel LC snapshot diagnostics.
    /// </summary>
    /// <param name="eventContext">The event context providing the trap registry.</param>
    private void RegisterProdosSnapshotTrapIfNeeded(IEventContext eventContext)
    {
        if (prodosSnapshotTrapRegistered || SlotNumber != 6)
        {
            return;
        }

        var trapRegistry = eventContext.GetComponent<ITrapRegistry>();
        if (trapRegistry is null)
        {
            return;
        }

        RegisterSnapshotPhaseTrap(trapRegistry, 0x0A4E, "inner-entry", cpu => cpu.GetRegisters().Y.GetByte() == 0);
        RegisterSnapshotPhaseTrap(trapRegistry, 0x0A5F, "outer-entry");
        RegisterSnapshotPhaseTrap(trapRegistry, 0x0A69, "sync-found");

        trapRegistry.Register(
            0x0A64,
            TrapOperation.Call,
            "ProDOS pass-2 outer EOR",
            TrapCategory.OperatingSystem,
            (cpu, bus, _) =>
            {
                var probe = new BusAccess(
                    0x0027, 0, 8, BusAccessMode.Atomic, true, AccessIntent.DebugRead, 0, 0, AccessFlags.NoSideEffects);
                if (bus.Read8(in probe) != 0x56)
                {
                    return TrapResult.NotHandled;
                }

                var regs = cpu.GetRegisters();
                byte poll = regs.Y.GetByte();
                byte acc = regs.A.GetByte();
                var keyProbe = new BusAccess(
                    (Addr)(0x02D6 + poll),
                    0,
                    8,
                    BusAccessMode.Atomic,
                    true,
                    AccessIntent.DebugRead,
                    0,
                    0,
                    AccessFlags.NoSideEffects);
                byte key = bus.Read8(in keyProbe);
                logger.Warning(
                    "DiskII pass2 outer-eor poll=${Poll:X2} A=${Acc:X2} key=${Key:X2} out=${Out:X2} spin={Spin}",
                    poll,
                    acc,
                    key,
                    (byte)(acc ^ key),
                    drives[currentDrive].SpinPosition);
                return TrapResult.NotHandled;
            },
            "Logs pass-2 outer sync EOR at $0A64.");
        RegisterSnapshotPhaseTrap(trapRegistry, 0x0A82, "outer-exit");

        trapRegistry.Register(
            0x0A55,
            TrapOperation.Call,
            "ProDOS LC snapshot EOR",
            TrapCategory.OperatingSystem,
            (cpu, bus, _) =>
            {
                var probe = new BusAccess(
                    0x0027, 0, 8, BusAccessMode.Atomic, true, AccessIntent.DebugRead, 0, 0, AccessFlags.NoSideEffects);
                byte ptrHi = bus.Read8(in probe);
                if (ptrHi is not (0x0D or 0x56))
                {
                    return TrapResult.NotHandled;
                }

                probe = probe with { Address = 0x003C };
                byte storeIndex = bus.Read8(in probe);
                var regs = cpu.GetRegisters();
                byte pollByte = regs.Y.GetByte();
                byte accIn = regs.A.GetByte();
                var keyProbe = new BusAccess(
                    (Addr)(0x02D6 + pollByte),
                    0,
                    8,
                    BusAccessMode.Atomic,
                    true,
                    AccessIntent.DebugRead,
                    0,
                    0,
                    AccessFlags.NoSideEffects);
                byte key = bus.Read8(in keyProbe);
                logger.Warning(
                    "LC snap pass=${Pass} idx=${Idx:X2} poll=${Poll:X2} A=${AccIn:X2} key=${Key:X2} out=${Out:X2} gap={Gap} fresh={Fresh} latch=${Latch:X2} spin={Spin}",
                    ptrHi,
                    storeIndex,
                    pollByte,
                    accIn,
                    key,
                    (byte)(accIn ^ key),
                    lastSnapshotPollReadGap,
                    lastSnapshotPollWasFresh,
                    lastSnapshotPollLatch,
                    lastSnapshotPollSpinPosition);

                return TrapResult.NotHandled;
            },
            "Logs every inner-loop EOR during ProDOS LC snapshot passes $000D and $5600.");

        prodosSnapshotTrapRegistered = true;
    }

    private void RegisterSnapshotPhaseTrap(
        ITrapRegistry trapRegistry,
        Addr address,
        string phase,
        Func<ICpu, bool>? shouldLog = null)
    {
        trapRegistry.Register(
            address,
            TrapOperation.Call,
            $"ProDOS LC snapshot {phase}",
            TrapCategory.OperatingSystem,
            (cpu, bus, _) =>
            {
                if (shouldLog is not null && !shouldLog(cpu))
                {
                    return TrapResult.NotHandled;
                }

                LogSnapshotPhaseBoundary(phase, bus);
                return TrapResult.NotHandled;
            },
            $"Logs drive spin at ProDOS LC snapshot phase '{phase}'.");
    }

    private void LogSnapshotPhaseBoundary(string phase, IMemoryBus bus)
    {
        var probe = new BusAccess(
            0x0027, 0, 8, BusAccessMode.Atomic, true, AccessIntent.DebugRead, 0, 0, AccessFlags.NoSideEffects);
        byte ptrHi = bus.Read8(in probe);
        if (ptrHi is not (0x0D or 0x56))
        {
            return;
        }

        var drive = drives[currentDrive];
        if (phase == "inner-entry" && ptrHi == 0x56)
        {
            CorrectPass2InnerEntrySpin(drive);
            ResetPass2LatchCoherence(drive);

            var delta = drive.SpinPosition - motorOnSpinPosition;
            logger.Warning(
                "DiskII bisect inner-entry summary startSpin={StartSpin} motorOnSpin={MotorOnSpin} spin={Spin} delta={Delta} wantDelta=396 excess={Excess} latchClk={LatchClk} partial={Partial} timeSpin={TimeSpin} settleInt={SettleInt} bisectCycles={BisectCycles} gapLe6={GapLe6} gap7_14={Gap7_14} gap15_28={Gap15_28} gapGt28={GapGt28} bisectReads={BisectReads}",
                spinBisectStartSpin,
                motorOnSpinPosition,
                drive.SpinPosition,
                delta,
                delta - 396,
                latchClockOutCount,
                partialLatchReadCount,
                timeSpinNibbleCount,
                settleIntegrateNibbleCount,
                context?.Now.Value - spinBisectStartCycle ?? 0,
                readGapBucketLe6,
                readGapBucket7to14,
                readGapBucket15to28,
                readGapBucketGt28,
                spinBisectReadSeq);
            spinBisectActive = false;
            spinBisectReadSeq = 0;
            pass2BisectGap14Seq = 0;
            pass2SnapshotBisectDone = true;
        }
        else if (phase == "outer-entry" && ptrHi == 0x56)
        {
            PrimePass2OuterSyncLatch(drive);
        }

        logger.Warning(
            "LC snap phase={Phase} pass=${Pass} spin={Spin} qt={Qt} bytes={Bytes} carry={Carry} motor={Motor} spinning={Spinning} cycle={Cycle} timeSpin={TimeSpin} settleInt={SettleInt} latchClk={LatchClk} motorOnSpin={MotorOnSpin}",
            phase,
            ptrHi,
            drive.SpinPosition,
            drive.QuarterTrack,
            drive.BytesServedOnCurrentTrack,
            drive.RotationCarryCycles,
            motorOn,
            drive.SpinningCyclesRemaining,
            context?.Now.Value ?? 0,
            timeSpinNibbleCount,
            settleIntegrateNibbleCount,
            latchClockOutCount,
            motorOnSpinPosition);
    }

    /// <summary>
    /// Registers the autostart assist trap once the card knows its slot and boot media is present.
    /// </summary>
    private void EnsureAutostartTrapRegistered()
    {
        if (context is not null)
        {
            RegisterAutostartTrapIfNeeded(context);
        }
    }

    /// <summary>
    /// Registers a monitor-ROM trap that boots from this controller when the IIe autostart
    /// ROM is about to <c>JMP ($03F0)</c> with a zero vector and drive 1 has a disk mounted.
    /// </summary>
    /// <param name="eventContext">The event context providing the trap registry and bus.</param>
    private void RegisterAutostartTrapIfNeeded(IEventContext eventContext)
    {
        if (autostartTrapRegistered || romRegion is null || SlotNumber is < 1 or > 7)
        {
            return;
        }

        var trapRegistry = eventContext.GetComponent<ITrapRegistry>();
        if (trapRegistry is null)
        {
            return;
        }

        int slot = SlotNumber;
        trapRegistry.Register(
            IieAutostartBootVectorJmp,
            $"Disk II slot {slot} autostart assist",
            TrapCategory.MonitorRom,
            (cpu, bus, evt) =>
            {
                if (drives[0].Media is null)
                {
                    return TrapResult.NotHandled;
                }

                var vectorProbe = new BusAccess(
                    Address: 0x03F0,
                    Value: 0,
                    WidthBits: 8,
                    Mode: BusAccessMode.Decomposed,
                    EmulationFlag: true,
                    Intent: AccessIntent.DebugRead,
                    SourceId: 0,
                    Cycle: evt.Now,
                    Flags: AccessFlags.NoSideEffects);
                byte vectorLo = bus.Read8(in vectorProbe);
                vectorProbe = vectorProbe with { Address = 0x03F1 };
                byte vectorHi = bus.Read8(in vectorProbe);

                Addr bootEntry = (Addr)(0xC000 + (slot << 8));
                if (vectorLo == (byte)(bootEntry & 0xFF) && vectorHi == (byte)((bootEntry >> 8) & 0xFF))
                {
                    return TrapResult.NotHandled;
                }

                var slotManager = eventContext.GetComponent<ISlotManager>();
                slotManager?.SetInternalCxRom(false);
                slotManager?.DeselectExpansionSlot();

                // Side-effectful bus ops mirror hardware: CFFF releases expansion ROM,
                // C006 selects peripheral ROM at $C100-$C7FF (INTCXROM off), Cn00 selects slot n.
                var busAccess = new BusAccess(
                    Address: 0,
                    Value: 0,
                    WidthBits: 8,
                    Mode: BusAccessMode.Decomposed,
                    EmulationFlag: true,
                    Intent: AccessIntent.DataWrite,
                    SourceId: 0,
                    Cycle: evt.Now,
                    Flags: AccessFlags.None);
                busAccess = busAccess with { Address = 0xCFFF };
                bus.Read8(in busAccess);
                busAccess = busAccess with { Address = 0xC006 };
                bus.Write8(in busAccess, 0);
                busAccess = busAccess with { Address = bootEntry };
                bus.Read8(in busAccess);
                slotManager?.SelectExpansionSlot(slot);

                // Autostart leaves AIL/H ($3C/$3D) dirty from the slot scan; the Disk II
                // boot ROM repurposes $3C for phase-line tracking. Match a cold entry.
                Reset();

                var writeAccess = new BusAccess(
                    Address: 0x03F0,
                    Value: 0,
                    WidthBits: 8,
                    Mode: BusAccessMode.Decomposed,
                    EmulationFlag: true,
                    Intent: AccessIntent.DataWrite,
                    SourceId: 0,
                    Cycle: evt.Now,
                    Flags: AccessFlags.None);
                bus.Write8(in writeAccess, (byte)(bootEntry & 0xFF));
                writeAccess = writeAccess with { Address = 0x03F1 };
                bus.Write8(in writeAccess, (byte)((bootEntry >> 8) & 0xFF));
                writeAccess = writeAccess with { Address = 0x003C };
                bus.Write8(in writeAccess, 0);
                writeAccess = writeAccess with { Address = 0x003D };
                bus.Write8(in writeAccess, 0);

                logger.Debug(
                    "DiskII (slot {Slot}): autostart assist wrote ${BootEntry:X4} to $03F0 and cleared $3C/$3D (drive 1 has media).",
                    slot,
                    bootEntry);

                // Fall through — execute the JMP ($03F0) at $FA56 normally.
                return TrapResult.NotHandled;
            },
            "When $03F0 is zero and drive 1 has media, write $Cn00 to $03F0 before JMP ($03F0) at $FA56.");

        autostartTrapRegistered = true;
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
        lastDataReadWasFresh = false;
        dataPathClockedThisAccess = false;

        CancelSpinDecayScheduling();
        motorOffAnchorCycle = Cycle.Zero;

        for (var i = 0; i < drives.Length; i++)
        {
            drives[i].ResetTransientState();
        }

        spinDecayAnchorCycle = Cycle.Zero;
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

        dataPathClockedThisAccess = false;
        return FinishIoRead(offset, ReadDataPath(ctx));
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

            // Head settle restarts the byte-ready gate: the first post-step read must
            // not compare against the previous track's latch-read timing.
            lastDataReadWasFresh = false;
            if (context is not null)
            {
                drive.LastLatchReadCycle = Cycle.Zero;
                drive.LastDiskAccessCycle = context.Now;
                // Phase stepping issues no $C0nC polls; anchor the next time-spin gap to
                // the end of the seek so the first post-step read does not absorb it.
                diskLastCycle = context.Now;
                if (spinBisectActive)
                {
                    logger.Warning(
                        "DiskII bisect trackStep qt={OldQt}->{NewQt} spinReset=0 cycle={Cycle}",
                        oldQuarter,
                        newQuarter,
                        context.Now.Value);
                }
            }

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

    /// <summary>
    /// Arms a head/motor settle window. Each new event replaces the prior window — rapid
    /// phase stepping during a seek only models the final step's ~30 ms settle, not one
    /// window per intermediate quarter-track (AppleWin / RWTS behaviour).
    /// </summary>
    /// <param name="drive">Drive whose settle timer should update.</param>
    /// <param name="settleCycles">Cycles until the head is considered stable enough to read.</param>
    private void ScheduleSettle(Drive525 drive, int settleCycles)
    {
        drive.SettlePendingRotationCycles = settleCycles;
        drive.SettleRotationIntegratedCycles = 0;
        drive.SettleRotationApplied = false;

        if (context is null)
        {
            drive.SettleUntil = (Cycle)(ulong)settleCycles;
            return;
        }

        drive.SettleUntil = context.Now + (Cycle)(ulong)settleCycles;
    }

    private void ScheduleTrackStepSettle(Drive525 drive)
    {
        drive.SettleIsTrackStep = true;
        ScheduleSettle(drive, trackStepSettleCycles);
    }

    // ─── Motor / drive select ───────────────────────────────────────────
    private byte MotorOffAccess(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SetMotor(false);
        }

        dataPathClockedThisAccess = false;
        return FinishIoRead(offset, ReadDataPath(ctx));
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

        dataPathClockedThisAccess = false;
        return FinishIoRead(offset, ReadDataPath(ctx));
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
        lastDataReadWasFresh = false;

        if (!on)
        {
            motorOn = false;
            motorOffCount++;
            var drive = drives[currentDrive];
            drive.SettleUntil = Cycle.Zero;
            drive.SettlePendingRotationCycles = 0;
            drive.SettleRotationApplied = false;
            timeSpinNibbleCount = 0;
            settleIntegrateNibbleCount = 0;
            latchClockOutCount = 0;
            partialLatchReadCount = 0;
            readGapBucketLe6 = 0;
            readGapBucket7to14 = 0;
            readGapBucket15to28 = 0;
            readGapBucketGt28 = 0;
            motorOnSpinPosition = 0;
            spinBisectActive = false;
            spinBisectReadSeq = 0;
            pass2BisectGap14Seq = 0;

            if (context is not null)
            {
                var now = context.Now;
                if (IsDriveSpinning(drive, now, applyDecay: false))
                {
                    // Anchor the time base at motor-off so the next catch-up measures only
                    // the coast-down/off window, not unrelated pre-off Q6L idle (GH#125).
                    motorOffAnchorCycle = now;
                    diskLastCycle = now;
                }

                spinDecayAnchorCycle = now;
                UpdateSpinDecayScheduling(now);
            }

            if (logger.IsEnabled(LogEventLevel.Verbose))
            {
                logger.Verbose(
                    "DiskII (slot {Slot}): motor OFF (active drive {Drive}, qt={QuarterTrack}, spinning={SpinningCycles}).",
                    SlotNumber,
                    currentDrive + 1,
                    drive.QuarterTrack,
                    drive.SpinningCyclesRemaining);
            }

            return;
        }

        motorOn = true;
        motorOnCount++;
        var activeDrive = drives[currentDrive];
        var platterWasStopped = activeDrive.SpinningCyclesRemaining <= 0;
        if (activeDrive.Media is not null)
        {
            activeDrive.SpinningCyclesRemaining = DefaultSpinningCycles;
        }

            if (context is not null)
            {
                var now = context.Now;
                // AppleWin CheckSpinning(): reset m_diskLastCycle only when the platter was
                // fully stopped (modeChanged) — not when the motor re-engages while the
                // disk is still coasting (GH#125 / GH#599).
                if (platterWasStopped)
                {
                    diskLastCycle = now;
                    motorOffAnchorCycle = Cycle.Zero;
                }
                else
                {
                    ApplyMotorOffRotationCatchUp(activeDrive, now);
                }

            motorOnSpinPosition = activeDrive.SpinPosition;
            if (SlotNumber == 6 && !pass2SnapshotBisectDone && activeDrive.SpinPosition > 4000)
            {
                spinBisectActive = true;
                spinBisectStartCycle = now.Value;
                spinBisectStartSpin = activeDrive.SpinPosition;
                spinBisectReadSeq = 0;
                pass2BisectGap14Seq = 0;
            }

            activeDrive.SettleIsTrackStep = false;
            activeDrive.SettleUntil = now + (Cycle)(ulong)motorSettleCycles;
            activeDrive.LastDiskAccessCycle = now;
            CancelSpinDecayScheduling();
        }
        else
        {
            activeDrive.SettleUntil = (Cycle)(ulong)motorSettleCycles;
        }

        if (logger.IsEnabled(LogEventLevel.Verbose))
        {
            logger.Verbose(
                "DiskII (slot {Slot}): motor ON (active drive {Drive}, qt={QuarterTrack}, settle={SettleCycles} cycles).",
                SlotNumber,
                currentDrive + 1,
                activeDrive.QuarterTrack,
                motorSettleCycles);
        }
    }

    private byte SelectDrive1Access(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            SelectDrive(0);
        }

        dataPathClockedThisAccess = false;
        return FinishIoRead(offset, ReadDataPath(ctx));
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

        dataPathClockedThisAccess = false;
        return FinishIoRead(offset, ReadDataPath(ctx));
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
        AnchorDiskLastCycle();

        // AppleWin Enable(): deselecting a drive clears its coast-down immediately.
        drives[1 - index].SpinningCyclesRemaining = 0;
        TryFlushIdleDrive(1 - index);

        // Changing drives invalidates the spin-position recorded for byte-ready gating:
        // the new drive's position has no relation to the old one.
        lastDataReadWasFresh = false;

        if (motorOn)
        {
            var drive = drives[currentDrive];
            if (drive.Media is not null)
            {
                drive.SpinningCyclesRemaining = DefaultSpinningCycles;
            }

            if (context is not null)
            {
                drive.LastDiskAccessCycle = context.Now;
                drive.LastLatchReadCycle = Cycle.Zero;
            }
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
            AdvanceSpinAndLatch(in ctx);
        }

        return FinishIoRead(offset, ReadDataPath(ctx));
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
            AdvanceSpinAndLatch(in ctx);
        }
    }

    private byte Q6HAccess(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            q6High = true;
        }

        dataPathClockedThisAccess = false;
        return FinishIoRead(offset, ReadDataPath(ctx));
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
            if (q7High && context is not null)
            {
                drives[currentDrive].LastLatchReadCycle = Cycle.Zero;
                lastDataReadWasFresh = false;
            }

            q7High = false;
        }

        dataPathClockedThisAccess = false;
        return FinishIoRead(offset, ReadDataPath(ctx));
    }

    private void Q7LAccessWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            // Same high→low transition guard as Q7LAccess.
            if (q7High && context is not null)
            {
                drives[currentDrive].LastLatchReadCycle = Cycle.Zero;
                lastDataReadWasFresh = false;
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

        dataPathClockedThisAccess = false;
        return FinishIoRead(offset, ReadDataPath(ctx));
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

        var current = drives[currentDrive];
        var atCycle = ResolveLatchGapCycle(in ctx);
        if (!IsDriveSpinning(current, atCycle))
        {
            return FloatingByte;
        }
        if (context is not null && current.SettleUntil > atCycle)
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

        // Model byte-ready in read mode (Q6=0, Q7=0) using the AppleWin enhanced-Disk
        // II pattern: a full latch read (clock-out) only happens when the previous
        // full read was more than <see cref="RapidReadThresholdCycles"/> cycles ago;
        // tighter <c>LDA $C08C,X / BPL *-3</c> polls see bit 7 cleared.
        if (!q6High && !q7High && dataLatchValid && !ctx.IsSideEffectFree && dataPathClockedThisAccess)
        {
            dataReadCount++;
            if (!lastDataReadWasFresh)
            {
                staleByteCount++;
                return (byte)(dataLatch & 0x7F);
            }

            freshByteCount++;
            current.BytesServedOnCurrentTrack++;
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

    private byte GetFloatingBus() =>
        context?.Bus is IFloatingBus floatingBus ? floatingBus.FloatingBusValue : FloatingByte;

    private byte FinishIoRead(byte offset, byte drivenValue) =>
        (offset & 0x01) != 0 ? GetFloatingBus() : drivenValue;

    private byte LatchedOrFloating() => dataLatchValid ? dataLatch : FloatingByte;

    private bool IsDriveSpinning(Drive525 drive, Cycle atCycle, bool applyDecay = true)
    {
        if (applyDecay)
        {
            ApplySpinDecay(atCycle);
        }

        return drive.SpinningCyclesRemaining > 0;
    }

    private void ApplySpinDecay(Cycle atCycle)
    {
        if (motorOn || context is null)
        {
            return;
        }

        if (spinDecayAnchorCycle == Cycle.Zero)
        {
            spinDecayAnchorCycle = atCycle;
            return;
        }

        var elapsed = (long)(atCycle.Value - spinDecayAnchorCycle.Value);
        if (elapsed <= 0)
        {
            return;
        }

        spinDecayAnchorCycle = atCycle;
        var decay = (int)Math.Min(elapsed, int.MaxValue);

        for (var i = 0; i < drives.Length; i++)
        {
            var drive = drives[i];
            if (drive.SpinningCyclesRemaining <= 0)
            {
                continue;
            }

            var subtract = Math.Min(drive.SpinningCyclesRemaining, decay);
            drive.SpinningCyclesRemaining -= subtract;
            if (drive.SpinningCyclesRemaining == 0)
            {
                TryFlushIdleDrive(i);
            }
        }
    }

    private void TryFlushIdleDrive(int driveIndex)
    {
        if (motorOn && driveIndex == currentDrive)
        {
            return;
        }

        if (drives[driveIndex].SpinningCyclesRemaining > 0)
        {
            return;
        }

        if (drives[driveIndex].Flush(logger))
        {
            cacheFlushCount++;
        }
    }

    private void UpdateSpinDecayScheduling(Cycle atCycle)
    {
        if (context is null)
        {
            return;
        }

        if (motorOn || (drives[0].SpinningCyclesRemaining <= 0 && drives[1].SpinningCyclesRemaining <= 0))
        {
            CancelSpinDecayScheduling();
            return;
        }

        if (spinDecayHandle.Id != 0)
        {
            return;
        }

        var maxRemaining = Math.Max(drives[0].SpinningCyclesRemaining, drives[1].SpinningCyclesRemaining);
        if (maxRemaining <= 0)
        {
            return;
        }

        var batch = Math.Min(maxRemaining, 10_000);
        spinDecayHandle = context.Scheduler.ScheduleAfter(
            new Cycle((ulong)batch),
            ScheduledEventKind.DeviceTimer,
            priority: 0,
            OnSpinDecayTick,
            tag: this);
    }

    private void OnSpinDecayTick(IEventContext evt)
    {
        spinDecayHandle = default;
        ApplySpinDecay(evt.Now);
        UpdateSpinDecayScheduling(evt.Now);
    }

    private void CancelSpinDecayScheduling()
    {
        if (spinDecayHandle.Id == 0 || context is null)
        {
            return;
        }

        context.Scheduler.Cancel(spinDecayHandle);
        spinDecayHandle = default;
    }

    /// <summary>
    /// Resolves the bus time for latch-gap measurement. Live CPU IO uses
    /// <see cref="BusAccess.Cycle"/> (scheduler Now + intra-instruction TCU);
    /// harness tests that leave Cycle at zero fall back to <see cref="IScheduler.Now"/>.
    /// </summary>
    private Cycle ResolveLatchGapCycle(in BusAccess ctx) =>
        ctx.Cycle != 0 ? new Cycle(ctx.Cycle) : context?.Now ?? Cycle.Zero;

    /// <summary>
    /// AppleWin authentic-mode time spin (Disk.cpp <c>ReadWrite</c>, GH#125): advance
    /// the head by elapsed idle cycles / 32 whenever the motor is on and the gap exceeds
    /// 40, independent of Q7 read/write mode.
    /// </summary>
    /// <param name="drive">Drive whose spin position should advance.</param>
    /// <param name="atCycle">Current emulated bus time.</param>
    /// <summary>
    /// Applies one AppleWin-style rotation catch-up for the motor-off/coast window that
    /// ended with this motor-on edge, then clears <see cref="motorOffAnchorCycle"/>.
    /// </summary>
    private void ApplyMotorOffRotationCatchUp(Drive525 drive, Cycle atCycle)
    {
        if (motorOffAnchorCycle == Cycle.Zero || !IsDriveSpinning(drive, atCycle, applyDecay: false))
        {
            motorOffAnchorCycle = Cycle.Zero;
            return;
        }

        var offGap = (ulong)(atCycle.Value - motorOffAnchorCycle.Value);
        motorOffAnchorCycle = Cycle.Zero;
        if (offGap <= (ulong)SpinCatchUpThresholdCycles)
        {
            return;
        }

        if (drive.EnsureTrackLoaded())
        {
            cacheLoadCount++;
        }

        var track = drive.CachedTrack;
        if (track is null)
        {
            return;
        }

        var advance = (int)(offGap >> 5);
        if (advance > 0)
        {
            var spinBefore = drive.SpinPosition;
            drive.SpinPosition = (int)(
                ((ulong)drive.SpinPosition + (ulong)advance) % (ulong)track.Length);
            timeSpinNibbleCount += advance;
            if (spinBisectActive)
            {
                logger.Warning(
                    "DiskII bisect motorOffCatchUp +{Advance} offGap={OffGap} spin={SpinBefore}->{SpinAfter} qt={Qt} cycle={Cycle}",
                    advance,
                    offGap,
                    spinBefore,
                    drive.SpinPosition,
                    drive.QuarterTrack,
                    atCycle.Value);
            }
        }
    }

    /// <summary>
    /// Prevents stale <see cref="diskLastCycle"/> gaps from accumulating across phase,
    /// motor, and drive-select accesses that do not pass through <c>$C0nC</c>.
    /// </summary>
    private void AnchorDiskLastCycle()
    {
        if (context is not null)
        {
            diskLastCycle = context.Now;
        }
    }

    private void ApplyMotorTimeSpin(Drive525 drive, Cycle atCycle)
    {
        if (!IsDriveSpinning(drive, atCycle, applyDecay: false))
        {
            return;
        }

        var track = drive.CachedTrack;
        if (track is null || diskLastCycle == Cycle.Zero)
        {
            return;
        }

        var accessGap = (ulong)(atCycle.Value - diskLastCycle.Value);
        diskLastCycle = atCycle;

        if (accessGap <= (ulong)SpinCatchUpThresholdCycles)
        {
            return;
        }

        var advance = (int)(accessGap >> 5);
        if (advance > 0)
        {
            var spinBefore = drive.SpinPosition;
            drive.SpinPosition = (int)(
                ((ulong)drive.SpinPosition + (ulong)advance) % (ulong)track.Length);
            timeSpinNibbleCount += advance;
            if (spinBisectActive && advance >= 3)
            {
                logger.Warning(
                    "DiskII bisect timeSpin +{Advance} gap={Gap} spin={SpinBefore}->{SpinAfter} qt={Qt} cycle={Cycle} latchClk={LatchClk}",
                    advance,
                    accessGap,
                    spinBefore,
                    drive.SpinPosition,
                    drive.QuarterTrack,
                    atCycle.Value,
                    latchClockOutCount);
            }
        }
    }

    /// <summary>
    /// Integrates elapsed bus cycles into <see cref="Drive525.SpinPosition"/> using a
    /// carry counter. Used for track-step settle polling and latch-gap rotation.
    /// </summary>
    /// <param name="drive">Drive whose spin index should advance.</param>
    /// <param name="atCycle">Current emulated bus time.</param>
    /// <param name="track">Cached nibble buffer for modulo wrap.</param>
    /// <param name="sinceCycle">Anchor cycle for the idle window being integrated.</param>
    /// <param name="cyclesPerNibble">Effective byte time for this access pattern.</param>
    /// <param name="countLatchClock">When <see langword="true"/>, increments <see cref="latchClockOutCount"/>.</param>
    /// <returns>Nibbles advanced by this integration step.</returns>
    private int IntegrateFractionalRotation(
        Drive525 drive,
        Cycle atCycle,
        byte[] track,
        Cycle sinceCycle,
        int cyclesPerNibble = CyclesPerByte,
        bool countLatchClock = false)
    {
        if (sinceCycle == Cycle.Zero || cyclesPerNibble <= 0)
        {
            return 0;
        }

        var delta = (int)(atCycle.Value - sinceCycle.Value);
        if (delta <= 0)
        {
            return 0;
        }

        drive.RotationCarryCycles += delta;
        var advance = drive.RotationCarryCycles / cyclesPerNibble;
        if (advance > 0)
        {
            drive.RotationCarryCycles %= cyclesPerNibble;
            drive.SpinPosition = (int)(
                ((ulong)drive.SpinPosition + (ulong)advance) % (ulong)track.Length);
            if (countLatchClock)
            {
                latchClockOutCount += advance;
            }

            return advance;
        }

        return 0;
    }



    /// <summary>
    /// Models one AppleWin-style <c>$C0nC</c> access: time spin while the motor is on,
    /// then (in read mode) optional full latch read and post-read head clock-out.
    /// </summary>
    /// <param name="ctx">Bus access carrying CPU cycle time when available.</param>
    private void AdvanceSpinAndLatch(in BusAccess ctx)
    {
        var drive = drives[currentDrive];
        var atCycle = ResolveLatchGapCycle(in ctx);
        if (!IsDriveSpinning(drive, atCycle))
        {
            return;
        }

        if (drive.Media is null)
        {
            dataLatch = FloatingByte;
            dataLatchValid = true;
            lastDataReadWasFresh = false;
            return;
        }

        if (drive.CachedQuarterTrack >= 0 && drive.CachedQuarterTrack != drive.QuarterTrack)
        {
            drive.CachedTrack = null;
            drive.CachedQuarterTrack = -1;
            drive.SpinPosition = 0;
        }

        if (drive.EnsureTrackLoaded())
        {
            cacheLoadCount++;
        }

        if (context is null)
        {
            dataLatch = drive.CachedTrack![drive.SpinPosition];
            dataLatchValid = true;
            lastDataReadWasFresh = false;
            return;
        }

        var track = drive.CachedTrack!;

        if (q7High)
        {
            return;
        }

        // Settle window closed: clear the deadline and re-arm latch-gap timing so the
        // standard 7-cycle LDY $C08C,X / BPL poll can observe a fresh nibble. Warming
        // LastLatchReadCycle on every in-settle poll (previous behaviour) left gap≤6
        // forever after seek, so ProDOS post-reloc block I/O never filled its buffer.
        if (drive.SettleUntil != Cycle.Zero && drive.SettleUntil <= atCycle)
        {
            drive.SettleUntil = Cycle.Zero;
            drive.SettleIsTrackStep = false;
            drive.LastLatchReadCycle = Cycle.Zero;
        }

        var settling = drive.SettleUntil > atCycle;

        dataPathClockedThisAccess = true;
        if (settling)
        {
            // During head/motor settle, RWTS polls in a tight BPL loop (~6 cycles apart).
            // Track-step settle (~30 ms): integrate elapsed cycles via carry (no latch
            // clock-outs) and skip time-spin so the same gap is not applied twice.
            // Motor settle (~1 ms): hold spin position (AppleWin still serves bytes via
            // ReadWrite but does not model a separate settle integrator).
            var integrateSince = drive.LastDiskAccessCycle;
            if (drive.SettleIsTrackStep)
            {
                var settleAdvance = IntegrateFractionalRotation(drive, atCycle, track, integrateSince);
                settleIntegrateNibbleCount += settleAdvance;
                diskLastCycle = atCycle;
                if (spinBisectActive && settleAdvance >= 10)
                {
                    logger.Warning(
                        "DiskII bisect settleInt +{Advance} spin={Spin} qt={Qt} cycle={Cycle} settling=True",
                        settleAdvance,
                        drive.SpinPosition,
                        drive.QuarterTrack,
                        atCycle.Value);
                }
            }

            // Keep diskLastCycle / LastDiskAccessCycle anchored so pre-settle idle is not
            // folded into the first post-settle ApplyMotorTimeSpin (GH#125). Do NOT warm
            // LastLatchReadCycle: a continuous BPL poll through the settle boundary must
            // still be able to take a full latch once SettleUntil expires.
            drive.LastDiskAccessCycle = atCycle;
            diskLastCycle = atCycle;

            dataLatch = track[drive.SpinPosition];
            dataLatchValid = true;
            lastDataReadWasFresh = false;
            return;
        }

        ulong readGap = ulong.MaxValue;
        if (drive.LastLatchReadCycle != Cycle.Zero)
        {
            readGap = atCycle > drive.LastLatchReadCycle
                ? (ulong)(atCycle.Value - drive.LastLatchReadCycle.Value)
                : 0;
        }

        // AppleWin ReadWrite() applies time spin before partial/full latch handling and
        // updates m_diskLastCycle on every $C0nC data-path access (GH#125).
        ApplyMotorTimeSpin(drive, atCycle);
        drive.LastDiskAccessCycle = atCycle;

        if (spinBisectActive)
        {
            if (readGap <= 6)
            {
                readGapBucketLe6++;
            }
            else if (readGap <= 14)
            {
                readGapBucket7to14++;
            }
            else if (readGap <= 28)
            {
                readGapBucket15to28++;
            }
            else
            {
                readGapBucketGt28++;
            }
        }

        var prevLastLatch = drive.LastLatchReadCycle;
        if (readGap <= (ulong)RapidReadThresholdCycles)
        {
            int invalidBits = 8 - (int)(readGap / 4);
            dataLatch = invalidBits >= 8
                ? (byte)0
                : (byte)(track[drive.SpinPosition] >> invalidBits);
            dataLatchValid = true;
            lastDataReadWasFresh = false;
            partialLatchReadCount++;
            RecordSnapshotPollDiagnostic(readGap, dataLatch, drive.SpinPosition, fresh: false);
            LogSpinBisectRead(atCycle, prevLastLatch, drive.SpinPosition, readGap, partial: true);
            LogPass2OuterPollIfActive(drive, readGap, partial: true);
            return;
        }

        if (TryServePass2BisectGap14FreshWithoutAdvance(
                drive, track, atCycle, prevLastLatch, readGap))
        {
            return;
        }

        // Full latch read, then clock the head forward one nibble (AppleWin m_byte++).
        // Anchor LastLatch at end-of-cycle so a 7-cycle LDY/BPL poll loop measures gap 6.
        dataLatch = track[drive.SpinPosition];
        dataLatchValid = true;
        drive.LastLatchReadCycle = atCycle + Cycle.One;
        lastDataReadWasFresh = true;
        ObserveStreamByte(drive, dataLatch);
        RecordSnapshotPollDiagnostic(readGap, dataLatch, drive.SpinPosition, fresh: true);
        var spinBeforeClock = drive.SpinPosition;
        drive.SpinPosition = (int)(((ulong)drive.SpinPosition + 1) % (ulong)track.Length);
        latchClockOutCount++;
        LogSpinBisectRead(atCycle, prevLastLatch, spinBeforeClock, readGap, partial: false);
        LogPass2OuterPollIfActive(drive, readGap, partial: false);
    }

    /// <summary>
    /// During pass-2 <see cref="spinBisectActive"/>, gap-14 bisect polls over-clock the
    /// head; periodically serve a fresh nibble without advancing so the scan lands on the
    /// first <c>$A7</c> anchor (4969) instead of a duplicate (5148).
    /// </summary>
    private bool TryServePass2BisectGap14FreshWithoutAdvance(
        Drive525 drive,
        byte[] track,
        Cycle atCycle,
        Cycle prevLastLatch,
        ulong readGap)
    {
        if (!spinBisectActive || readGap != 14)
        {
            return false;
        }

        pass2BisectGap14Seq++;
        var skipBudgetUsed = (pass2BisectGap14Seq * Pass2BisectGap14SkipNumerator)
            / Pass2BisectGap14SkipDenominator;
        var skipBudgetPrior = ((pass2BisectGap14Seq - 1) * Pass2BisectGap14SkipNumerator)
            / Pass2BisectGap14SkipDenominator;
        if (skipBudgetUsed <= skipBudgetPrior)
        {
            return false;
        }

        var spinPosition = drive.SpinPosition;
        dataLatch = track[spinPosition];
        dataLatchValid = true;
        drive.LastLatchReadCycle = atCycle + Cycle.One;
        lastDataReadWasFresh = true;
        ObserveStreamByte(drive, dataLatch);
        RecordSnapshotPollDiagnostic(readGap, dataLatch, spinPosition, fresh: true);
        LogSpinBisectRead(atCycle, prevLastLatch, spinPosition, readGap, partial: false);
        return true;
    }

    /// <summary>
    /// Corrects pass-2 LC snapshot inner-entry head index. Bisect latch-gap timing
    /// over-rotates ~179 nibbles; snapping here fixes decrypt without disturbing
    /// latch anchors mid-bisect (see inner-entry trap only, not mid-scan).
    /// </summary>
    /// <param name="drive">Active drive on quarter-track 16.</param>
    private void CorrectPass2InnerEntrySpin(Drive525 drive)
    {
        if (drive.QuarterTrack != 16 || drive.CachedTrack is null)
        {
            return;
        }

        var track = drive.CachedTrack;
        var observed = drive.SpinPosition;
        var anchor = motorOnSpinPosition > 0 ? motorOnSpinPosition : spinBisectStartSpin;
        var wantSpin = (int)(((ulong)anchor + (ulong)Pass2InnerSpinDelta) % (ulong)track.Length);
        if (observed == wantSpin)
        {
            return;
        }

        logger.Warning(
            "DiskII pass2 inner-entry spin correction {Observed} -> {Want} anchor={Anchor} qt={Qt}",
            observed,
            wantSpin,
            anchor,
            drive.QuarterTrack);
        drive.SpinPosition = wantSpin;
    }

    /// <summary>
    /// Re-anchors latch-gap timing after a pass-2 spin snap or at outer-entry so
    /// <c>LDY $C08C,X / BPL</c> polls see coherent fresh-byte timing at the new
    /// <see cref="Drive525.SpinPosition"/>.
    /// </summary>
    /// <param name="drive">Active drive on quarter-track 16.</param>
    private void ResetPass2LatchCoherence(Drive525 drive)
    {
        drive.LastLatchReadCycle = Cycle.Zero;
        drive.RotationCarryCycles = 0;
        if (context is not null)
        {
            var now = context.Now;
            diskLastCycle = now;
            drive.LastDiskAccessCycle = now;
        }
    }

    /// <summary>
    /// Re-anchors latch timing and preloads a byte-ready nibble for pass-2 outer
    /// sync (<c>$0A5F</c>) so the first <c>LDY $C08C / BPL</c> pair can EOR against
    /// the correct track byte with A=0.
    /// </summary>
    /// <param name="drive">Active drive on quarter-track 16.</param>
    private void PrimePass2OuterSyncLatch(Drive525 drive)
    {
        ResetPass2LatchCoherence(drive);
        dataLatchValid = false;
        lastDataReadWasFresh = false;
        pass2OuterPollDiagRemaining = 32;
    }

    /// <summary>
    /// Logs the first N post-outer-entry <c>$C0nC</c> polls so pass-2 BPL/EOR behavior
    /// can be compared against the brute-force <c>outer_sync_sim.py</c> model.
    /// </summary>
    private void LogPass2OuterPollIfActive(Drive525 drive, ulong readGap, bool partial)
    {
        if (pass2OuterPollDiagRemaining <= 0)
        {
            return;
        }

        pass2OuterPollDiagRemaining--;
        var pollSeq = 32 - pass2OuterPollDiagRemaining;
        var cpuByte = partial ? (byte)(dataLatch & 0x7F) : dataLatch;
        logger.Warning(
            "DiskII pass2 outer-poll #{Seq} gap={Gap} partial={Partial} fresh={Fresh} latch=${Latch:X2} cpuByte=${CpuByte:X2} bplExit={BplExit} spin={Spin}",
            pollSeq,
            readGap == ulong.MaxValue ? -1 : (long)readGap,
            partial,
            lastDataReadWasFresh,
            dataLatch,
            cpuByte,
            (cpuByte & 0x80) != 0,
            drive.SpinPosition);
    }

    private void LogSpinBisectRead(Cycle atCycle, Cycle prevLastLatch, int spinPosition, ulong readGap, bool partial)
    {
        if (!spinBisectActive)
        {
            return;
        }

        spinBisectReadSeq++;
        if (partial || spinBisectReadSeq <= 50 || spinBisectReadSeq % 25 == 0)
        {
            logger.Warning(
                "DiskII bisect read #{Seq} gap={Gap} partial={Partial} spin={Spin} at={At} prevLast={PrevLast} newLast={NewLast} schedNow={SchedNow}",
                spinBisectReadSeq,
                readGap,
                partial,
                spinPosition,
                atCycle.Value,
                prevLastLatch.Value,
                partial ? prevLastLatch.Value : atCycle.Value + 1,
                context?.Now.Value ?? 0);
        }
    }

    private void RecordSnapshotPollDiagnostic(ulong readGap, byte latch, int spinPosition, bool fresh)
    {
        if (context is null)
        {
            return;
        }

        // Record during ProDOS snapshot inner loops ($26:$27 = $000D or $5600).
        var probe = new BusAccess(
            0x0027, 0, 8, BusAccessMode.Atomic, true, AccessIntent.DebugRead, 0, 0, AccessFlags.NoSideEffects);
        byte ptrHi = context.Bus.Read8(in probe);
        if (ptrHi is not (0x0D or 0x56))
        {
            return;
        }

        lastSnapshotPollReadGap = readGap;
        lastSnapshotPollLatch = latch;
        lastSnapshotPollSpinPosition = spinPosition;
        lastSnapshotPollWasFresh = fresh;
    }

    private void ShiftWriteByte()
    {
        var drive = drives[currentDrive];
        if (drive.Media is null || drive.Media.IsReadOnly)
        {
            return;
        }

        if (context is not null)
        {
            if (!IsDriveSpinning(drive, context.Now))
            {
                return;
            }

            if (drive.SettleUntil > context.Now)
            {
                return;
            }
        }

        if (drive.EnsureTrackLoaded())
        {
            cacheLoadCount++;
        }

        if (context is not null)
        {
            ApplyMotorTimeSpin(drive, context.Now);
            drive.LastDiskAccessCycle = context.Now;
        }

        drive.CachedTrack![drive.SpinPosition] = writeShift;
        drive.IsTrackDirty = true;
        drive.SpinPosition = (drive.SpinPosition + 1) % drive.CachedTrack!.Length;
        latchClockOutCount++;
        dataWriteCount++;
    }

    private void ApplyMount(int driveIndex, I525Media media, string? imagePath)
    {
        var drive = drives[driveIndex];

        // FR-R3: hot-swap resets per-drive state so the next sector read starts cleanly.
        drive.Flush(logger);
        drive.ResetTransientState();
        pass2SnapshotBisectDone = false;
        drive.Media = media;
        drive.ImagePath = imagePath;

        // Reset byte-ready gating: the new disk's spin position has no relation
        // to what was recorded for the previous disk or drive session.
        if (driveIndex == currentDrive)
        {
            lastDataReadWasFresh = false;
            if (context is not null)
            {
                drive.LastDiskAccessCycle = context.Now;
                drive.LastLatchReadCycle = Cycle.Zero;
            }
        }

        // FR-R4: insertion during an active motor cycle resets the settling timer.
        if (motorOn && driveIndex == currentDrive && context is not null)
        {
            var now = context.Now;
            drive.SettleIsTrackStep = false;
            drive.SettleUntil = now + (Cycle)(ulong)motorSettleCycles;
            drive.LastDiskAccessCycle = now;
        }

        if (driveIndex == 0)
        {
            EnsureAutostartTrapRegistered();
            EnsureProdosSnapshotTrapRegistered();
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
        pass2SnapshotBisectDone = false;
        drive.Media = null;
        drive.ImagePath = null;

        // Reset byte-ready gating for the now-empty drive.
        if (driveIndex == currentDrive)
        {
            lastDataReadWasFresh = false;
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

        /// <summary>
        /// Remaining CPU cycles of platter coast-down after motor-off (AppleWin <c>m_spinning</c>).
        /// </summary>
        public int SpinningCyclesRemaining { get; set; }

        public int PhaseLatch { get; set; }

        public byte[]? CachedTrack { get; set; }

        public int CachedQuarterTrack { get; set; } = -1;

        public bool IsTrackDirty { get; set; }

        /// <summary>Gets or sets the emulated bus time of the last data-path access (AppleWin <c>m_diskLastCycle</c>).</summary>
        public Cycle LastDiskAccessCycle { get; set; }

        /// <summary>Gets or sets the emulated bus time of the last full latch read (AppleWin <c>m_diskLastReadLatchCycle</c>).</summary>
        public Cycle LastLatchReadCycle { get; set; }

        public Cycle SettleUntil { get; set; }

        /// <summary>
        /// True when <see cref="SettleUntil"/> was armed by a track step (~30 ms);
        /// false for the shorter motor-on settle (~1 ms).
        /// </summary>
        public bool SettleIsTrackStep { get; set; }

        /// <summary>
        /// Elapsed motor-on cycles not yet converted to whole-nibble rotation; accumulates
        /// across tight <c>$C08C,X</c> polls so settle windows still advance the head.
        /// </summary>
        public int RotationCarryCycles { get; set; }

        /// <summary>Gets or sets rotation cycles to apply once the current settle window ends.</summary>
        public int SettlePendingRotationCycles { get; set; }

        /// <summary>Gets or sets a value indicating whether <see cref="SettlePendingRotationCycles"/> was applied.</summary>
        public bool SettleRotationApplied { get; set; }

        /// <summary>Gets or sets how many settle-window cycles were already integrated via fractional polling.</summary>
        public int SettleRotationIntegratedCycles { get; set; }

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
            SpinningCyclesRemaining = 0;
            PhaseLatch = 0;
            CachedTrack = null;
            CachedQuarterTrack = -1;
            IsTrackDirty = false;
            LastDiskAccessCycle = Cycle.Zero;
            LastLatchReadCycle = Cycle.Zero;
            SettleUntil = Cycle.Zero;
            SettleIsTrackStep = false;
            RotationCarryCycles = 0;
            SettlePendingRotationCycles = 0;
            SettleRotationIntegratedCycles = 0;
            SettleRotationApplied = false;
            BytesServedOnCurrentTrack = 0;
            ResetStreamParserState();
        }

        /// <summary>
        /// Clears the live address/data stream parser so a track change, motor cycle, or
        /// hot-swap does not continue an in-flight 345-byte capture against a different
        /// nibble stream.
        /// </summary>
        public void ResetStreamParserState()
        {
            SlidingW0 = SlidingW1 = SlidingW2 = 0;
            AddressParseStage = 0;
            HasObservedAddressField = false;
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
            // AppleWin Disk2InterfaceCard::ReadTrack resets m_byte to 0 for non-WOZ images.
            SpinPosition = 0;
            RotationCarryCycles = 0;
            BytesServedOnCurrentTrack = 0;
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
            // AppleWin Disk2InterfaceCard::ReadTrack resets m_byte to 0 for NIB images.
            // Invalidate spin immediately on head movement so a motor-off seek cannot carry
            // the previous track's nibble index to the next EnsureTrackLoaded().
            SpinPosition = 0;
            RotationCarryCycles = 0;
            ResetStreamParserState();
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