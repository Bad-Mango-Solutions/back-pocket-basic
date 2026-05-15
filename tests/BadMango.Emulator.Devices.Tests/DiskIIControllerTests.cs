// <copyright file="DiskIIControllerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Storage.Backends;
using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;
using BadMango.Unit.Components;

using Moq;

using Serilog;

/// <summary>
/// Unit tests for the working <see cref="DiskIIController"/> (PRD §11 row 6).
/// </summary>
[TestFixture]
public class DiskIIControllerTests
{
    private static readonly byte[] BlankBootRom = new byte[DiskIIBootRom.RomSize];

    /// <summary>
    /// Verifies that the controller advertises itself as a Disk II slot card so the
    /// machine builder registers it under <c>Slot/{n}/DiskII</c> (FR-D10).
    /// </summary>
    [Test]
    public void Controller_AdvertisesDiskIIDeviceType()
    {
        var card = new DiskIIController(NewLoggerMock().Object);

        Assert.Multiple(() =>
        {
            Assert.That(card.DeviceType, Is.EqualTo("DiskII"));
            Assert.That(card.Kind, Is.EqualTo(PeripheralKind.SlotCard));
            Assert.That(card.Name, Is.EqualTo("Disk II Controller"));
            Assert.That(card.DriveCount, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Verifies that with no boot ROM supplied the controller publishes no slot ROM
    /// (deferring to other cards or the system boot ROM); with a user-supplied P5A
    /// image it publishes that ROM at <c>$Cn00–$CnFF</c> (FR-D9).
    /// </summary>
    [Test]
    public void BootRom_OnlyPublishedWhenSupplied()
    {
        var noRom = new DiskIIController(NewLoggerMock().Object);
        Assert.That(noRom.ROMRegion, Is.Null);

        var bytes = new byte[DiskIIBootRom.RomSize];
        bytes[0] = 0x20; // sentinel
        var withRom = new DiskIIController(NewLoggerMock().Object, new DiskIIBootRom(bytes));
        Assert.That(withRom.ROMRegion, Is.Not.Null);
    }

    /// <summary>
    /// Verifies that an even-then-odd phase sequence steps the head one half-track
    /// (two quarter-tracks) per valid magnet transition (FR-D4).
    /// </summary>
    [Test]
    public void PhaseStepper_AdjacentPhaseSequence_StepsOneHalfTrackPerStep()
    {
        var (controller, dispatcher, ctx, _, media) = BuildHarness();
        controller.Mount(0, media.Object);

        // Energize phase 1, then phase 2 (forward step), then phase 3 (forward), then phase 0 (forward).
        // Each "next phase up" moves the head two quarter-tracks forward.
        // Initial QT == 0 → after these four energizations we should be at QT 8.
        // Sequence: turn on phase 1 ($C0E3), turn on phase 2 ($C0E5),
        // turn on phase 3 ($C0E7), turn on phase 0 ($C0E1).
        _ = dispatcher.Read(0xE3, in ctx);
        _ = dispatcher.Read(0xE5, in ctx);
        _ = dispatcher.Read(0xE7, in ctx);
        _ = dispatcher.Read(0xE1, in ctx);

        var snap = controller.GetDriveSnapshot(0);
        Assert.That(snap.QuarterTrack, Is.EqualTo(8));
    }

    /// <summary>
    /// Verifies that a reverse phase sequence steps the head backward and clamps at 0 (FR-D4).
    /// </summary>
    [Test]
    public void PhaseStepper_ClampsAtTrackZero()
    {
        var (controller, dispatcher, ctx, _, media) = BuildHarness();
        controller.Mount(0, media.Object);

        // Step backwards from 0 — should clamp.
        _ = dispatcher.Read(0xE7, in ctx); // phase 3 on (one half-track back from phase 0)
        _ = dispatcher.Read(0xE5, in ctx); // phase 2 on (back again)

        var snap = controller.GetDriveSnapshot(0);
        Assert.That(snap.QuarterTrack, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that opposite-phase moves (e.g. phase 0 → phase 2) do not move the
    /// head — the magnet pair has no defined direction (FR-D4 invalid sequence).
    /// </summary>
    [Test]
    public void PhaseStepper_OppositePhase_DoesNotMove()
    {
        var (controller, dispatcher, ctx, _, media) = BuildHarness();
        controller.Mount(0, media.Object);

        _ = dispatcher.Read(0xE5, in ctx); // phase 2 on while at phase 0 → opposite, no move

        var snap = controller.GetDriveSnapshot(0);
        Assert.That(snap.QuarterTrack, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies the Q6/Q7 write-protect dispatch path: Q6=1, Q7=0 returns the
    /// current drive's WP status in the high bit (FR-D5).
    /// </summary>
    [Test]
    public void Q6Q7_WriteProtectSense_ReturnsHighBit()
    {
        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness();
        var writable = new Mock<I525Media>();
        writable.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        writable.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);
        writable.SetupGet(m => m.IsReadOnly).Returns(false);
        controller.Mount(0, writable.Object);

        var readOnly = new Mock<I525Media>();
        readOnly.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        readOnly.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);
        readOnly.SetupGet(m => m.IsReadOnly).Returns(true);
        controller.Mount(1, readOnly.Object);

        scheduler.Advance(new Cycle(2)); // dispatch deferred mount

        // Drive 1 is selected by default — writable → WP bit clear.
        _ = dispatcher.Read(0xED, in ctx); // Q6H
        _ = dispatcher.Read(0xEE, in ctx); // Q7L
        var wpWritable = dispatcher.Read(0xED, in ctx);
        Assert.That(wpWritable & 0x80, Is.EqualTo(0));

        // Switch to drive 2 and re-read.
        _ = dispatcher.Read(0xEB, in ctx); // select drive 2
        _ = dispatcher.Read(0xED, in ctx); // Q6H
        _ = dispatcher.Read(0xEE, in ctx); // Q7L
        var wpReadOnly = dispatcher.Read(0xED, in ctx);
        Assert.That(wpReadOnly & 0x80, Is.EqualTo(0x80));
    }

    /// <summary>
    /// Verifies that during the post-motor-on settling window, reads return the
    /// last-latched byte (or floating $FF) rather than live disk data (FR-D7).
    /// </summary>
    [Test]
    public void MotorSettling_ReturnsLastLatchedByteUntilExpiry()
    {
        var media = new ConstantTrackMedia((byte)0xAA);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 500);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        // Motor on at t=2.
        _ = dispatcher.Read(0xE9, in ctx);

        // While the settle timer is active (advance only 100 cycles), $C0EC should
        // not advance to live data — should report floating $FF (no byte yet latched).
        scheduler.Advance(new Cycle(100));
        var duringSettle = dispatcher.Read(0xEC, in ctx);
        Assert.That(duringSettle, Is.EqualTo(0xFF));

        // After the settle elapses, $C0EC must return the actual track byte.
        scheduler.Advance(new Cycle(1000));
        var postSettle = dispatcher.Read(0xEC, in ctx);
        Assert.That(postSettle, Is.EqualTo(0xAA));
    }

    /// <summary>
    /// Verifies that motor on/off control responds to <c>$C0n8/$C0n9</c> and that
    /// the snapshot's <c>MotorOn</c> field reflects the selected drive only.
    /// </summary>
    [Test]
    public void MotorControl_TogglesAndOnlySelectedDriveReportsMotorOn()
    {
        var (controller, dispatcher, ctx, _, media) = BuildHarness();
        controller.Mount(0, media.Object);
        controller.Mount(1, media.Object);

        _ = dispatcher.Read(0xE9, in ctx); // motor on

        var snap0 = controller.GetDriveSnapshot(0);
        var snap1 = controller.GetDriveSnapshot(1);
        Assert.Multiple(() =>
        {
            Assert.That(controller.IsMotorOn, Is.True);
            Assert.That(snap0.MotorOn, Is.True);
            Assert.That(snap1.MotorOn, Is.False);
            Assert.That(snap0.Selected, Is.True);
            Assert.That(snap1.Selected, Is.False);
        });

        _ = dispatcher.Read(0xE8, in ctx); // motor off
        Assert.That(controller.IsMotorOn, Is.False);
    }

    /// <summary>
    /// Verifies that <c>$C0EC</c> reads emit the GCR address-field prologue
    /// (<c>D5 AA 96</c>) for a synthetic sector image (FR-D5 / FR-T5).
    /// </summary>
    [Test]
    public void Q6L_SeesAddressFieldPrologueOnSyntheticImage()
    {
        // Use a real SectorImageMedia so the GCR encoder emits true nibble data.
        var backing = new RamStorageBackend(DiskGeometry.Standard525Dos.TotalBytes);
        var sector = new SectorImageMedia(backing, DiskGeometry.Standard525Dos);
        var media = sector.As525Media();

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx); // motor on
        scheduler.Advance(new Cycle(10));

        // Stream up to one full track and confirm the canonical address prologue
        // sequence (D5 AA 96) appears at least once.
        bool found = false;
        byte b0 = 0, b1 = 0;
        for (int i = 0; i < GcrEncoder.StandardTrackLength + 64; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            var b2 = dispatcher.Read(0xEC, in ctx);
            if (b0 == GcrEncoder.AddressPrologue1
                && b1 == GcrEncoder.AddressPrologue2
                && b2 == GcrEncoder.AddressPrologue3)
            {
                found = true;
                break;
            }

            b0 = b1;
            b1 = b2;
        }

        Assert.That(found, Is.True, "Expected to see the GCR address-field prologue D5 AA 96 within a track scan.");
    }

    /// <summary>
    /// Verifies that <see cref="DiskIIController.Mount"/> applies immediately when the
    /// controller is idle (motor off), so a pre-boot insert is observable through
    /// <see cref="DiskIIController.GetDriveSnapshot"/> without requiring the scheduler
    /// to tick. This is the contract behind the debug-console <c>disk insert</c>
    /// command working before the machine has booted.
    /// </summary>
    [Test]
    public void Mount_AppliesImmediately_WhenControllerIsIdle()
    {
        var media = new Mock<I525Media>();
        media.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        media.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);

        var (controller, _, _, _, _) = BuildHarness();

        // Motor is off (the harness builds an idle controller); the mount must be
        // visible without any scheduler.Advance().
        controller.Mount(0, media.Object, "library://disks/test.dsk");

        var snap = controller.GetDriveSnapshot(0);
        Assert.Multiple(() =>
        {
            Assert.That(snap.HasMedia, Is.True);
            Assert.That(snap.MountedImagePath, Is.EqualTo("library://disks/test.dsk"));
        });
    }

    /// <summary>
    /// Verifies that <see cref="DiskIIController.Mount"/> defers the actual swap to
    /// the next scheduler turn when the motor is on (FR-R1) so the controller never
    /// observes a half-mounted drive mid-byte while a transfer is in flight.
    /// </summary>
    [Test]
    public void Mount_IsDeferredToNextSchedulerTurn_WhenMotorIsOn()
    {
        var media = new Mock<I525Media>();
        media.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        media.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness();
        _ = dispatcher.Read(0xE9, in ctx); // motor on
        Assume.That(controller.IsMotorOn, Is.True);

        controller.Mount(0, media.Object, "library://disks/test.dsk");

        // Before the scheduler runs, the drive should still be empty.
        var preSnap = controller.GetDriveSnapshot(0);
        Assert.That(preSnap.HasMedia, Is.False);

        scheduler.Advance(new Cycle(2));

        var postSnap = controller.GetDriveSnapshot(0);
        Assert.Multiple(() =>
        {
            Assert.That(postSnap.HasMedia, Is.True);
            Assert.That(postSnap.MountedImagePath, Is.EqualTo("library://disks/test.dsk"));
        });
    }

    /// <summary>
    /// Verifies that <see cref="DiskIIController.Mount"/> over a drive that already
    /// has a disk mounted implicitly ejects the prior disk and replaces it with the
    /// new medium (the operator-facing equivalent of physically swapping the
    /// diskette). The new media and image path replace the prior values.
    /// </summary>
    [Test]
    public void Mount_OverExistingDisk_ImplicitlyEjectsAndReplaces()
    {
        var first = new Mock<I525Media>();
        first.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        first.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);

        var second = new Mock<I525Media>();
        second.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        second.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);

        var (controller, _, _, _, _) = BuildHarness();
        controller.Mount(0, first.Object, "library://disks/first.dsk");
        Assume.That(controller.GetDriveSnapshot(0).MountedImagePath, Is.EqualTo("library://disks/first.dsk"));

        controller.Mount(0, second.Object, "library://disks/second.dsk");

        var snap = controller.GetDriveSnapshot(0);
        Assert.Multiple(() =>
        {
            Assert.That(snap.HasMedia, Is.True);
            Assert.That(snap.MountedImagePath, Is.EqualTo("library://disks/second.dsk"));
        });
    }

    /// <summary>
    /// Verifies that <see cref="DiskIIController.Mount"/> over an existing dirty disk
    /// whose flush fails is rejected with a clear <see cref="InvalidOperationException"/>
    /// (FR-R2 applied to the implicit eject), and that the prior disk remains mounted
    /// untouched so the operator can react.
    /// </summary>
    [Test]
    public void Mount_OverExistingDirtyDisk_RejectsWhenFlushFails()
    {
        var thrower = new ThrowingWriteMedia();

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, thrower);
        scheduler.Advance(new Cycle(2));

        // Dirty the cached track so the implicit-eject flush will throw. We leave the
        // motor on intentionally — the implicit-eject probe-flush in Mount runs up
        // front and must short-circuit before any state changes (deferred or not).
        _ = dispatcher.Read(0xE9, in ctx); // motor on
        scheduler.Advance(new Cycle(10));
        _ = dispatcher.Read(0xEF, in ctx); // Q7H — write enable
        _ = dispatcher.Read(0xED, in ctx); // Q6H — load
        WriteSoft(dispatcher, 0xED, 0xAA, ctx);
        WriteSoft(dispatcher, 0xEC, 0x55, ctx); // Q6L write — shifts byte out

        var replacement = new Mock<I525Media>();
        replacement.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        replacement.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);

        Assert.Throws<InvalidOperationException>(() =>
            controller.Mount(0, replacement.Object, "library://disks/replacement.dsk"));

        // The prior disk must still be mounted; even if the deferred swap had been
        // queued, advancing the scheduler must not change the drive contents because
        // the implicit-eject flush rejected the mount before scheduling anything.
        scheduler.Advance(new Cycle(2));
        Assert.That(controller.GetDriveSnapshot(0).HasMedia, Is.True);
    }

    /// <summary>
    /// Verifies that <see cref="DiskIIController.Eject"/> returns <see langword="false"/>
    /// when the medium's flush throws and the medium remains mounted (FR-R2).
    /// </summary>
    [Test]
    public void Eject_RejectsWhenFlushFails()
    {
        var media = new ThrowingWriteMedia();

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        // Make a write so the track is dirty and the flush will throw.
        _ = dispatcher.Read(0xE9, in ctx); // motor on
        scheduler.Advance(new Cycle(10));
        _ = dispatcher.Read(0xEF, in ctx); // Q7H — write enable
        _ = dispatcher.Read(0xED, in ctx); // Q6H — load
        WriteSoft(dispatcher, 0xED, 0xAA, ctx);
        WriteSoft(dispatcher, 0xEC, 0x55, ctx); // Q6L write — shifts byte out

        bool ejected = controller.Eject(0);

        Assert.Multiple(() =>
        {
            Assert.That(ejected, Is.False);
            Assert.That(controller.GetDriveSnapshot(0).HasMedia, Is.True);
        });
    }

    /// <summary>
    /// Verifies that hot-swap mounts reset per-drive transient state
    /// (quarter-track, spin position, dirty cache) (FR-R3).
    /// </summary>
    [Test]
    public void HotSwap_ResetsPerDriveState()
    {
        var (controller, dispatcher, ctx, scheduler, media) = BuildHarness();
        controller.Mount(0, media.Object);
        scheduler.Advance(new Cycle(2));

        // Step the head a couple of half-tracks forward.
        _ = dispatcher.Read(0xE3, in ctx); // phase 1 on
        _ = dispatcher.Read(0xE5, in ctx); // phase 2 on
        Assert.That(controller.GetDriveSnapshot(0).QuarterTrack, Is.EqualTo(4));

        // Hot-swap to a fresh medium.
        var media2 = new Mock<I525Media>();
        media2.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        media2.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);
        controller.Mount(0, media2.Object);
        scheduler.Advance(new Cycle(2));

        var snap = controller.GetDriveSnapshot(0);
        Assert.Multiple(() =>
        {
            Assert.That(snap.HasMedia, Is.True);
            Assert.That(snap.QuarterTrack, Is.EqualTo(0));
            Assert.That(snap.PhaseLatch, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that the constructor rejects negative settle-cycle parameters
    /// (defensive guard on the public surface).
    /// </summary>
    [Test]
    public void Constructor_RejectsNegativeSettleCycles()
    {
        var logger = NewLoggerMock().Object;
        Assert.Multiple(() =>
        {
            Assert.That(() => new DiskIIController(logger, motorSettleCycles: -1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new DiskIIController(logger, trackStepSettleCycles: -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    /// <summary>
    /// Verifies that a null logger is rejected, since the controller takes its
    /// logger via DI and has no <see cref="System.Diagnostics.Trace"/> fallback.
    /// </summary>
    [Test]
    public void Constructor_RejectsNullLogger()
    {
        Assert.That(() => new DiskIIController(logger: null!), Throws.TypeOf<ArgumentNullException>());
    }

    /// <summary>
    /// Verifies that when a sector-image flush throws, the controller writes a
    /// warning entry on the injected Serilog logger rather than swallowing it
    /// silently (FR-D8 — surfaced as warnings).
    /// </summary>
    [Test]
    public void FlushFailure_LogsWarningOnInjectedLogger()
    {
        var media = new ThrowingWriteMedia();
        var loggerMock = NewLoggerMock();

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(
            motorSettleCycles: 0,
            loggerMock: loggerMock);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        // Stage a dirty byte so the next motor-off triggers a flush.
        _ = dispatcher.Read(0xE9, in ctx); // motor on
        scheduler.Advance(new Cycle(10));
        _ = dispatcher.Read(0xEF, in ctx); // Q7H — write enable
        _ = dispatcher.Read(0xED, in ctx); // Q6H — load
        WriteSoft(dispatcher, 0xED, 0xAA, ctx);
        WriteSoft(dispatcher, 0xEC, 0x55, ctx); // Q6L write — shift one byte out
        _ = dispatcher.Read(0xE8, in ctx); // motor off — triggers Drive525.Flush

        loggerMock.Verify(
            l => l.Warning(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.AtLeastOnce);
    }

    private static Mock<ILogger> NewLoggerMock() => Generator.Log();

    private static (DiskIIController Controller, IOPageDispatcher Dispatcher, BusAccess Context, Scheduler Scheduler, Mock<I525Media> Media) BuildHarness(
        int motorSettleCycles = DiskIIController.DefaultMotorSettleCycles,
        Mock<ILogger>? loggerMock = null)
    {
        var controller = new DiskIIController(
            logger: (loggerMock ?? NewLoggerMock()).Object,
            bootRom: new DiskIIBootRom(BlankBootRom),
            motorSettleCycles: motorSettleCycles);
        controller.SlotNumber = 6;

        var dispatcher = new IOPageDispatcher();
        dispatcher.InstallSlotHandlers(6, controller.IOHandlers!);

        var scheduler = new Scheduler();
        var bus = new Mock<IMemoryBus>();
        var signals = new Mock<ISignalBus>();
        var eventCtx = new EventContext(scheduler, signals.Object, bus.Object);
        scheduler.SetEventContext(eventCtx);
        controller.Initialize(eventCtx);

        var media = new Mock<I525Media>();
        media.SetupGet(m => m.Geometry).Returns(DiskGeometry.Standard525Dos);
        media.SetupGet(m => m.OptimalTrackLength).Returns(GcrEncoder.StandardTrackLength);
        media.SetupGet(m => m.IsReadOnly).Returns(false);
        media.Setup(m => m.ReadTrack(It.IsAny<int>(), It.IsAny<byte[]>()));

        var ctx = new BusAccess(
            Address: 0xC0E0,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);

        return (controller, dispatcher, ctx, scheduler, media);
    }

    private static void WriteSoft(IOPageDispatcher dispatcher, byte addr, byte value, in BusAccess templateCtx)
    {
        var writeCtx = templateCtx with { Intent = AccessIntent.DataWrite };
        dispatcher.Write(addr, value, in writeCtx);
    }

    /// <summary>
    /// Test fake whose <see cref="WriteTrack"/> always throws — used to verify that
    /// <see cref="DiskIIController.Eject"/> rejects when flush fails (FR-R2).
    /// </summary>
    private sealed class ThrowingWriteMedia : I525Media
    {
        public DiskGeometry Geometry => DiskGeometry.Standard525Dos;

        public int OptimalTrackLength => GcrEncoder.StandardTrackLength;

        public bool IsReadOnly => false;

        public void ReadTrack(int quarterTrack, Span<byte> destination) => destination.Clear();

        public void WriteTrack(int quarterTrack, ReadOnlySpan<byte> source)
            => throw new InvalidOperationException("backing read-only");

        public void Flush()
        {
        }
    }

    /// <summary>
    /// Test fake that reports every byte of every track as a single constant value.
    /// </summary>
    private sealed class ConstantTrackMedia : I525Media
    {
        private readonly byte fill;

        public ConstantTrackMedia(byte fill)
        {
            this.fill = fill;
        }

        public DiskGeometry Geometry => DiskGeometry.Standard525Dos;

        public int OptimalTrackLength => GcrEncoder.StandardTrackLength;

        public bool IsReadOnly => false;

        public void ReadTrack(int quarterTrack, Span<byte> destination) => destination.Fill(fill);

        public void WriteTrack(int quarterTrack, ReadOnlySpan<byte> source)
        {
        }

        public void Flush()
        {
        }
    }
}