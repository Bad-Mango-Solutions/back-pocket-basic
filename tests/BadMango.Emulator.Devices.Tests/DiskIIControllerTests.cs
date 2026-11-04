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
    /// Verifies that <see cref="DiskIIController.LoadBootRom"/> publishes the boot ROM
    /// region after construction, so the profile-driven build path can wire the
    /// user-supplied ROM via <c>config.rom</c> without resorting to a constructor parameter.
    /// </summary>
    [Test]
    public void LoadBootRom_PublishesBootRomRegion()
    {
        var controller = new DiskIIController(NewLoggerMock().Object);
        Assert.That(controller.ROMRegion, Is.Null, "Pre-condition: no ROM at construction.");

        var bytes = new byte[DiskIIBootRom.RomSize];
        bytes[0] = 0xA9; // sentinel: LDA #imm
        bytes[1] = 0x55;

        controller.LoadBootRom(bytes);

        Assert.That(controller.ROMRegion, Is.Not.Null);

        // Read $Cn00..$Cn01 through the published bus target to confirm the bytes survive.
        var probe = new BusAccess(
            Address: 0xC600,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);
        Assert.Multiple(() =>
        {
            Assert.That(controller.ROMRegion!.Read8(0x00, in probe), Is.EqualTo((byte)0xA9));
            Assert.That(controller.ROMRegion!.Read8(0x01, in probe), Is.EqualTo((byte)0x55));
        });
    }

    /// <summary>
    /// Verifies that <see cref="DiskIIController.LoadBootRom"/> rejects a payload that is
    /// not exactly <see cref="DiskIIBootRom.RomSize"/> bytes, surfacing the same contract
    /// the constructor enforces so misconfigured profiles fail loudly rather than silently
    /// truncating or padding the ROM.
    /// </summary>
    [Test]
    public void LoadBootRom_RejectsWrongSize()
    {
        var controller = new DiskIIController(NewLoggerMock().Object);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => controller.LoadBootRom(null!));
            Assert.Throws<ArgumentException>(() => controller.LoadBootRom(new byte[DiskIIBootRom.RomSize - 1]));
            Assert.Throws<ArgumentException>(() => controller.LoadBootRom(new byte[DiskIIBootRom.RomSize + 1]));
        });
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
    /// Regression test for FR-D4 head-clamp: the stepper must allow the head to
    /// reach the outermost cylinder of a 35-track DOS 3.3 disk (track 34 = qt 136).
    /// An earlier implementation used <c>maxQuarter = 2 × trackCount − 2 = 68</c>,
    /// which clamped the head at qt=68 / track 17 / $11 and silently made the upper
    /// half of any disk unreachable — DOS reads would loop forever as RWTS detected
    /// an address-field track mismatch on every retry, surfacing as a permanent
    /// I/O ERROR for files whose TSList track was above $11 (e.g. LEMONADE on
    /// <c>game_01b.dsk</c>, which lives at T$17 / qt 92).
    /// </summary>
    [Test]
    public void PhaseStepper_CanReachOuterCylinder_OfThirtyFiveTrackDisk()
    {
        var (controller, dispatcher, ctx, _, media) = BuildHarness();
        controller.Mount(0, media.Object);

        // Standard 35-track DOS 3.3 geometry → qtCount = 140; the last whole-track
        // boundary is qt=136 (= track 34). The DOS seek pattern alternates phase-on
        // and phase-off pulses so the head can keep stepping; an "on" advances qt
        // by 2 (one half-track), the matching "off" releases the previous magnet.
        // Each four-phase ON cycle (1, 2, 3, 0) advances qt by 8 (two full tracks),
        // so 17 cycles reach qt=136 from qt=0.
        byte[] forwardOnPulses = { 0xE3, 0xE5, 0xE7, 0xE1 };  // phase 1/2/3/0 ON
        byte[] forwardOffPulses = { 0xE0, 0xE2, 0xE4, 0xE6 }; // phase 0/1/2/3 OFF
        for (int cycle = 0; cycle < 17; cycle++)
        {
            for (int p = 0; p < 4; p++)
            {
                _ = dispatcher.Read(forwardOnPulses[p], in ctx);
                _ = dispatcher.Read(forwardOffPulses[p], in ctx);
            }
        }

        var afterSeek = controller.GetDriveSnapshot(0);
        Assert.That(afterSeek.QuarterTrack, Is.EqualTo(136), "Head must reach track 34 (qt=136) on a 35-track disk.");

        // One more forward ON/OFF must clamp (qt=136 is the highest legal value).
        _ = dispatcher.Read(0xE3, in ctx);
        _ = dispatcher.Read(0xE0, in ctx);
        var clamped = controller.GetDriveSnapshot(0);
        Assert.That(clamped.QuarterTrack, Is.EqualTo(136), "Head must clamp at qt=136, not advance off the end of the media.");
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
    /// data latch with bit 7 cleared so DOS RWTS's <c>LDA $C08C,X / BPL *-3</c>
    /// polling loop spins (modeling unstable byte-ready during head warm-up).
    /// After the settle window elapses, live track data flows with bit 7 set
    /// (every valid GCR nibble has bit 7 set) so the polling loop falls through
    /// to the address-prologue match (FR-D7).
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

        // While the settle timer is active (advance only 100 cycles), $C0EC must
        // report bit 7 cleared so DOS's BPL polling loop spins. Returning a byte
        // with bit 7 set (e.g. the raw $FF floating bus) here would cause RWTS
        // to read the same stale nibble thousands of times per settle window and
        // burn through its retry budget.
        scheduler.Advance(new Cycle(100));
        var duringSettle = dispatcher.Read(0xEC, in ctx);
        Assert.That(duringSettle & 0x80, Is.EqualTo(0), "Reads during the motor-settle window must clear bit 7 so RWTS's BPL loop spins.");

        // After the settle elapses, $C0EC must return the actual track byte with
        // bit 7 set (every valid GCR nibble has bit 7 set).
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

    /// <summary>
    /// Verifies that the data-register's high bit (bit 7) models the Disk II
    /// "byte-ready" behavior the P5A boot ROM depends on: consecutive reads of
    /// <c>$C0EC</c> that occur faster than one nibble per <see cref="DiskIIController.CyclesPerByte"/>
    /// must report bit 7 cleared (so the boot ROM's <c>LDA $C08C,X / BPL *-3</c>
    /// loop spins) and only return the full nibble once the head has advanced
    /// to a fresh byte. Without this gating the BPL loop would fall through on
    /// every read (every GCR nibble has bit 7 set), trapping the boot ROM on
    /// whatever byte happens to be under the head when the loop starts.
    /// </summary>
    [Test]
    public void Q6L_ByteReadyHighBit_GatesRapidPollsBetweenBytes()
    {
        var backing = new RamStorageBackend(DiskGeometry.Standard525Dos.TotalBytes);
        var sector = new SectorImageMedia(backing, DiskGeometry.Standard525Dos);
        var media = sector.As525Media();

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx); // motor on
        scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));

        // First $C0EC read primes the latch with a fresh nibble.
        var first = dispatcher.Read(0xEC, in ctx);

        // A rapid follow-up read at the same spin position must clear bit 7 so
        // the boot ROM's BPL polling loop continues spinning, even when the
        // underlying nibble has bit 7 set (every valid GCR nibble does).
        scheduler.Advance(new Cycle(4));
        var poll = dispatcher.Read(0xEC, in ctx);

        Assert.Multiple(() =>
        {
            Assert.That(first & 0x80, Is.EqualTo(0x80), "Fresh nibble should report bit 7 set.");
            Assert.That(poll & 0x80, Is.EqualTo(0), "Repeated poll between bytes must report bit 7 cleared.");
            Assert.That(poll & 0x7F, Is.EqualTo(first & 0x7F), "Low 7 bits of the polled value should match the latched nibble.");
        });

        // After a full byte time elapses, the next read must surface a fresh
        // nibble with bit 7 set again so the BPL loop can fall through.
        scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
        var fresh = dispatcher.Read(0xEC, in ctx);
        Assert.That(fresh & 0x80, Is.EqualTo(0x80), "Next byte time should re-arm the byte-ready high bit.");
    }

    /// <summary>
    /// Regression test for the multi-track-load "I/O ERROR" bug: when the head
    /// is stepped to a new track, the post-step settle window must report bit 7
    /// cleared on $C0EC reads so DOS RWTS's <c>LDA $C08C,X / BPL *-3</c>
    /// address-prologue scan loop spins waiting for the head to stabilise.
    /// Before this fix, the settle window returned the previously latched byte
    /// unchanged (bit 7 set, because every valid GCR nibble has bit 7 set), so
    /// RWTS read the same stale non-$D5 nibble at full CPU speed for ~30 ms,
    /// burning its 48-retry budget and producing a spurious "I/O ERROR" on any
    /// file that crossed a track boundary (e.g. the 47-sector LEMONADE).
    /// </summary>
    [Test]
    public void TrackStepSettling_HoldsByteReadyLowUntilExpiry()
    {
        // Use a constant-nibble track so we can unambiguously detect that the
        // controller is *not* surfacing a real byte (bit 7 set) during settle.
        // 0xAA is what would be on the data latch from the previous track read.
        var media = new ConstantTrackMedia((byte)0xAA);

        const int TrackStepSettle = 30000;
        var controller = new DiskIIController(
            NewLoggerMock().Object,
            bootRom: new DiskIIBootRom(BlankBootRom),
            motorSettleCycles: 0,
            trackStepSettleCycles: TrackStepSettle);
        controller.SlotNumber = 6;
        var dispatcher = new IOPageDispatcher();
        dispatcher.InstallSlotHandlers(6, controller.IOHandlers!);
        var scheduler = new Scheduler();
        var bus = new Mock<IMemoryBus>();
        var signals = new Mock<ISignalBus>();
        var eventCtx = new EventContext(scheduler, signals.Object, bus.Object);
        scheduler.SetEventContext(eventCtx);
        controller.Initialize(eventCtx);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

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

        // Motor on (zero settle) and prime the data latch with a fresh byte so
        // bit 7 is set going into the simulated track step.
        _ = dispatcher.Read(0xE9, in ctx);
        scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
        var primed = dispatcher.Read(0xEC, in ctx);
        Assert.That(primed, Is.EqualTo(0xAA), "Sanity: latch should be primed with a live track byte (bit 7 set).");

        // Step the head: pulse phase 1 ($C0E3). This triggers OnTrackChanging
        // (nulls cached track) and arms SettleUntil = now + 30000.
        _ = dispatcher.Read(0xE3, in ctx);

        // Tight poll of $C0EC during the settle window — exactly what DOS RWTS
        // does when scanning for the address-field prologue $D5. Every read
        // must report bit 7 cleared so the BPL loop spins. Sample at 256-cycle
        // intervals across the full 30 ms settle window (well over 100 reads).
        for (var i = 0; i < 100; i++)
        {
            scheduler.Advance(new Cycle(256));
            var duringSettle = dispatcher.Read(0xEC, in ctx);
            Assert.That(
                duringSettle & 0x80,
                Is.EqualTo(0),
                $"During post-step settle (iteration {i}, t≈{(i + 1) * 256} of {TrackStepSettle} cycles), $C0EC must keep bit 7 cleared so RWTS's BPL polling loop spins instead of consuming stale nibbles.");
        }

        // Advance well past the settle deadline; the next read must produce a
        // real track byte with bit 7 set so RWTS can finally find $D5.
        scheduler.Advance(new Cycle(TrackStepSettle));
        var postSettle = dispatcher.Read(0xEC, in ctx);
        Assert.That(postSettle, Is.EqualTo(0xAA), "After the post-step settle expires, $C0EC must surface the live track byte (bit 7 set).");
    }

    // ─── Activity-snapshot instrumentation tests (FR-D10) ────────────────────

    /// <summary>
    /// Verifies that a freshly constructed controller reports zero activity across all
    /// counters and per-drive fields, so the diagnostic UI can rely on the snapshot
    /// reflecting only what has actually happened on the bus.
    /// </summary>
    [Test]
    public void GetActivitySnapshot_NewController_ReportsAllZeros()
    {
        var controller = new DiskIIController(NewLoggerMock().Object);
        var snap = controller.GetActivitySnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(snap.DataReadCount, Is.Zero);
            Assert.That(snap.FreshByteCount, Is.Zero);
            Assert.That(snap.StaleByteCount, Is.Zero);
            Assert.That(snap.SettleSuppressedReadCount, Is.Zero);
            Assert.That(snap.DataWriteCount, Is.Zero);
            Assert.That(snap.PhasePulseCount, Is.Zero);
            Assert.That(snap.PhaseNoOpCount, Is.Zero);
            Assert.That(snap.TrackChangeCount, Is.Zero);
            Assert.That(snap.MotorOnCount, Is.Zero);
            Assert.That(snap.MotorOffCount, Is.Zero);
            Assert.That(snap.DriveSelectCount, Is.Zero);
            Assert.That(snap.CacheLoadCount, Is.Zero);
            Assert.That(snap.CacheFlushCount, Is.Zero);
            Assert.That(snap.Drives, Has.Count.EqualTo(2));
            Assert.That(snap.Drives[0].ObservedAddressFields, Is.Zero);
            Assert.That(snap.Drives[0].LastObservedVolume, Is.Null);
            Assert.That(snap.Drives[1].ObservedAddressFields, Is.Zero);
        });
    }

    /// <summary>
    /// Verifies that motor on / off transitions, drive selection changes, phase pulses,
    /// no-op phase pulses, and track changes are all individually counted (FR-D10).
    /// </summary>
    [Test]
    public void GetActivitySnapshot_CountsBusEvents()
    {
        var (controller, dispatcher, ctx, scheduler, media) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media.Object);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx);   // motor on
        _ = dispatcher.Read(0xE8, in ctx);   // motor off
        _ = dispatcher.Read(0xE9, in ctx);   // motor on again
        _ = dispatcher.Read(0xEB, in ctx);   // select drive 2
        _ = dispatcher.Read(0xEA, in ctx);   // select drive 1
        _ = dispatcher.Read(0xE3, in ctx);   // phase 1 on → step head (qt 0→2)
        _ = dispatcher.Read(0xE5, in ctx);   // phase 2 on → step head (qt 2→4)
        _ = dispatcher.Read(0xE1, in ctx);   // phase 0 on while head at phase 2 → opposite phase → no-op

        var snap = controller.GetActivitySnapshot();
        Assert.Multiple(() =>
        {
            Assert.That(snap.MotorOnCount, Is.EqualTo(2));
            Assert.That(snap.MotorOffCount, Is.EqualTo(1));
            Assert.That(snap.DriveSelectCount, Is.EqualTo(2));
            Assert.That(snap.PhasePulseCount, Is.EqualTo(2));
            Assert.That(snap.PhaseNoOpCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(snap.TrackChangeCount, Is.EqualTo(2));
        });
    }

    /// <summary>
    /// Verifies that data-path reads are classified into fresh (SpinPosition advanced),
    /// stale (CPU polled before a new byte arrived), and settle-suppressed (head still
    /// settling) bins — these classifications are the key signal for diagnosing
    /// "I/O ERROR" failures where RWTS is either polling too fast or polling during
    /// a settle window.
    /// </summary>
    [Test]
    public void GetActivitySnapshot_ClassifiesReadsIntoFreshStaleAndSettle()
    {
        var media = new ConstantTrackMedia((byte)0xAA);
        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 500);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        // Motor on; first 500 cycles are settle window. The motor-on access itself
        // performs a data-path read which is classified as settle-suppressed because
        // SettleUntil is now in the future. Capture the baseline so we can assert on
        // deltas rather than absolute counts.
        _ = dispatcher.Read(0xE9, in ctx);

        var baseline = controller.GetActivitySnapshot();

        // Read during settle → should classify as settle-suppressed.
        for (var i = 0; i < 5; i++)
        {
            _ = dispatcher.Read(0xEC, in ctx);
        }

        var duringSettle = controller.GetActivitySnapshot();
        Assert.That(duringSettle.SettleSuppressedReadCount - baseline.SettleSuppressedReadCount, Is.EqualTo(5), "Reads during settle should be classified as settle-suppressed.");
        Assert.That(duringSettle.FreshByteCount, Is.EqualTo(baseline.FreshByteCount), "No fresh bytes should be reported during settle.");

        // Past settle, advance one byte time; that read is fresh.
        scheduler.Advance(new Cycle(500 + DiskIIController.CyclesPerByte));
        _ = dispatcher.Read(0xEC, in ctx);
        var afterFresh = controller.GetActivitySnapshot();
        Assert.That(afterFresh.FreshByteCount, Is.GreaterThanOrEqualTo(1), "Read after settle expires should yield a fresh byte.");

        // Immediately re-read without advancing time → CPU polled faster than bytes arrive → stale.
        _ = dispatcher.Read(0xEC, in ctx);
        _ = dispatcher.Read(0xEC, in ctx);
        var afterStale = controller.GetActivitySnapshot();
        Assert.That(afterStale.StaleByteCount, Is.GreaterThanOrEqualTo(2), "Back-to-back reads without time advance should be classified as stale.");
    }

    /// <summary>
    /// Verifies that the controller's live byte-stream parser decodes <c>$D5 $AA $96</c>
    /// address prologues followed by 4-and-4 vol/trk/sec/chk pairs and surfaces the
    /// decoded fields on <see cref="DiskDriveActivity"/>. This is the headline
    /// diagnostic signal for separating seek bugs (wrong track served) from timing
    /// bugs (no address field served at all).
    /// </summary>
    [Test]
    public void GetActivitySnapshot_StreamParser_DecodesValidAddressField()
    {
        // Encode a single address field for vol=$FE, trk=$11, sec=$05 and repeat it
        // across the entire track so the head is guaranteed to hit it.
        var encoded = Encode4And4AddressField(0xFE, 0x11, 0x05);
        var media = new RepeatingPatternMedia(encoded);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx); // motor on

        // Drive enough fresh bytes through the parser to see the prologue at least once.
        // The pattern is 11 bytes long; tracklen is 6656; a few hundred reads is plenty.
        for (var i = 0; i < 200; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            _ = dispatcher.Read(0xEC, in ctx);
        }

        var snap = controller.GetActivitySnapshot();
        var drive = snap.Drives[0];
        Assert.Multiple(() =>
        {
            Assert.That(drive.ObservedAddressFields, Is.GreaterThanOrEqualTo(1), "At least one address-field prologue should have been parsed from the live byte stream.");
            Assert.That(drive.LastObservedVolume, Is.EqualTo(0xFE));
            Assert.That(drive.LastObservedTrack, Is.EqualTo(0x11));
            Assert.That(drive.LastObservedSector, Is.EqualTo(0x05));
            Assert.That(drive.LastObservedChecksumValid, Is.True, "vol ^ trk ^ sec should match the parsed checksum byte.");
            Assert.That(drive.ObservedAddressFieldChecksumErrors, Is.Zero);
        });
    }

    /// <summary>
    /// Verifies that a corrupted address field (mismatched checksum) is observed but
    /// flagged via <see cref="DiskDriveActivity.LastObservedChecksumValid"/> and
    /// <see cref="DiskDriveActivity.ObservedAddressFieldChecksumErrors"/>.
    /// </summary>
    [Test]
    public void GetActivitySnapshot_StreamParser_FlagsChecksumMismatch()
    {
        // Construct an address field with deliberately wrong checksum byte.
        var encoded = Encode4And4AddressField(0xFE, 0x11, 0x05);

        // Replace the 8 data bytes (positions 3..10) checksum nibbles (positions 9, 10)
        // with values that decode to something other than vol^trk^sec.
        // Easiest path: corrupt the lo-nibble of the checksum pair (position 10).
        encoded[10] ^= 0x01;

        var media = new RepeatingPatternMedia(encoded);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx); // motor on
        for (var i = 0; i < 200; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            _ = dispatcher.Read(0xEC, in ctx);
        }

        var snap = controller.GetActivitySnapshot();
        var drive = snap.Drives[0];
        Assert.Multiple(() =>
        {
            Assert.That(drive.ObservedAddressFields, Is.GreaterThanOrEqualTo(1));
            Assert.That(drive.LastObservedChecksumValid, Is.False);
            Assert.That(drive.ObservedAddressFieldChecksumErrors, Is.GreaterThanOrEqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that bytes-served-on-current-track resets when the head steps to a new
    /// quarter-track, so the diagnostic UI can show RWTS's progress per-track without
    /// stale accumulation from previous track residency.
    /// </summary>
    [Test]
    public void GetActivitySnapshot_BytesServedOnCurrentTrack_ResetsOnTrackChange()
    {
        var media = new ConstantTrackMedia((byte)0xAA);
        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx); // motor on

        // Accumulate some bytes on track 0.
        for (var i = 0; i < 10; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            _ = dispatcher.Read(0xEC, in ctx);
        }

        var preStep = controller.GetActivitySnapshot();
        Assert.That(preStep.Drives[0].BytesServedOnCurrentTrack, Is.GreaterThanOrEqualTo(5));

        // Step the head.
        _ = dispatcher.Read(0xE3, in ctx);

        var postStep = controller.GetActivitySnapshot();
        Assert.That(postStep.Drives[0].BytesServedOnCurrentTrack, Is.Zero, "Track step should reset per-track byte counter.");
    }

    /// <summary>
    /// Verifies that the live stream parser recognises the data-field prologue
    /// (<c>$D5 $AA $AD</c>) following a well-formed address field and counts the
    /// 343-nibble field as decoded with a valid XOR-chain checksum and matching
    /// epilogue. This is the headline signal for confirming that the encoded data
    /// field RWTS actually sees on the wire is structurally valid.
    /// </summary>
    [Test]
    public void GetActivitySnapshot_StreamParser_DecodesValidDataField()
    {
        // Encode a full 6656-byte track using the production encoder; this lays
        // down 16 well-formed address+data pairs at the standard 48/14/5/349
        // sector stride, exercising the same prologues/gaps/epilogues RWTS sees.
        var trackPattern = BuildEncodedTrack(volume: 0xFE, track: 0x11);
        var media = new RepeatingPatternMedia(trackPattern);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx); // motor on

        // One full track is 6656 bytes; iterate enough fresh-byte reads to cover
        // a couple of revolutions so every prologue is observed.
        for (var i = 0; i < 14000; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            _ = dispatcher.Read(0xEC, in ctx);
        }

        var snap = controller.GetActivitySnapshot();
        var drive = snap.Drives[0];
        Assert.Multiple(() =>
        {
            Assert.That(drive.ObservedAddressFields, Is.GreaterThanOrEqualTo(16), "Two revolutions × 16 sectors should observe at least 16 address fields.");
            Assert.That(drive.ObservedAddressFieldChecksumErrors, Is.Zero);
            Assert.That(drive.ObservedDataPrologues, Is.GreaterThanOrEqualTo(16));
            Assert.That(drive.ObservedDataFieldDecodeSuccesses, Is.GreaterThanOrEqualTo(16));
            Assert.That(drive.ObservedDataFieldChecksumErrors, Is.Zero);
            Assert.That(drive.ObservedDataFieldDecodeErrors, Is.Zero);
            Assert.That(drive.ObservedDataFieldEpilogueMismatches, Is.Zero);

            // The standard track layout puts the data prologue exactly 5 bytes
            // after the address-field epilogue ($DE $AA $EB); the parser subtracts
            // both the leading data prologue and the trailing address epilogue
            // from its raw count so the reported gap equals that inter-field gap.
            Assert.That(drive.LastDataPrologueGapBytes, Is.EqualTo(5));
            Assert.That(drive.MinDataPrologueGapBytes, Is.EqualTo(5));
            Assert.That(drive.MaxDataPrologueGapBytes, Is.EqualTo(5));
        });
    }

    /// <summary>
    /// Verifies that a data field whose XOR-chain checksum has been corrupted is
    /// counted under <see cref="DiskDriveActivity.ObservedDataFieldChecksumErrors"/>
    /// rather than under decode-successes.
    /// </summary>
    [Test]
    public void GetActivitySnapshot_StreamParser_FlagsDataFieldChecksumMismatch()
    {
        var trackPattern = BuildEncodedTrack(volume: 0xFE, track: 0x11);

        // Flip one bit in the data-field checksum nibble (the 343rd byte after the
        // $D5 $AA $AD prologue of every sector). The standard layout places the
        // first data prologue at offset 48 + 14 + 5 = 67; the checksum nibble at
        // offset 67 + 3 + 342 = 412 (last of the 343 encoded bytes). XOR the
        // nibble with 0x02 — which swaps two valid 6-and-2 table entries — so
        // the read table still finds a valid 6-bit value but the chain residual
        // becomes non-zero (catches the checksum-error path rather than the
        // decode-error path).
        const int sectorStride = 416;
        const int dataFieldStart = 48 + 14 + 5;
        for (var physical = 0; physical < 16; physical++)
        {
            var checksumIndex = (physical * sectorStride) + dataFieldStart + 3 + 342;
            var original = trackPattern[checksumIndex];
            var corrupted = MapToDifferentValidNibble(original);
            trackPattern[checksumIndex] = corrupted;
        }

        var media = new RepeatingPatternMedia(trackPattern);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx); // motor on
        for (var i = 0; i < 14000; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            _ = dispatcher.Read(0xEC, in ctx);
        }

        var snap = controller.GetActivitySnapshot();
        var drive = snap.Drives[0];
        Assert.Multiple(() =>
        {
            Assert.That(drive.ObservedDataPrologues, Is.GreaterThanOrEqualTo(16));
            Assert.That(drive.ObservedDataFieldChecksumErrors, Is.GreaterThanOrEqualTo(16), "Every observed data field should have failed the XOR-chain checksum.");
            Assert.That(drive.ObservedDataFieldDecodeSuccesses, Is.Zero);
            Assert.That(drive.ObservedDataFieldDecodeErrors, Is.Zero, "The corruption was nibble-table-preserving, so no decode errors should be counted.");
        });
    }

    /// <summary>
    /// Verifies that a data field whose nibble stream contains a byte not in the
    /// 6-and-2 read table is rejected as a decode error (the path RWTS would also
    /// fail), distinct from the checksum-error path.
    /// </summary>
    [Test]
    public void GetActivitySnapshot_StreamParser_FlagsDataFieldDecodeError()
    {
        var trackPattern = BuildEncodedTrack(volume: 0xFE, track: 0x11);

        // Replace the first byte of every data field with $80 — clearly not a
        // valid 6-and-2 nibble (no entry in the write table starts below $96).
        const int sectorStride = 416;
        const int dataFieldStart = 48 + 14 + 5;
        for (var physical = 0; physical < 16; physical++)
        {
            var firstNibbleIndex = (physical * sectorStride) + dataFieldStart + 3;
            trackPattern[firstNibbleIndex] = 0x80;
        }

        var media = new RepeatingPatternMedia(trackPattern);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx); // motor on
        for (var i = 0; i < 14000; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            _ = dispatcher.Read(0xEC, in ctx);
        }

        var snap = controller.GetActivitySnapshot();
        var drive = snap.Drives[0];
        Assert.Multiple(() =>
        {
            Assert.That(drive.ObservedDataPrologues, Is.GreaterThanOrEqualTo(16));
            Assert.That(drive.ObservedDataFieldDecodeErrors, Is.GreaterThanOrEqualTo(16));
            Assert.That(drive.ObservedDataFieldDecodeSuccesses, Is.Zero);
            Assert.That(drive.ObservedDataFieldChecksumErrors, Is.Zero);
        });
    }

    /// <summary>
    /// Verifies that a structurally valid data field whose trailing two bytes are
    /// not <c>$DE $AA</c> is counted under
    /// <see cref="DiskDriveActivity.ObservedDataFieldEpilogueMismatches"/> while
    /// still counting toward decode-success when the checksum is valid (these are
    /// independent failure modes — RWTS validates each separately).
    /// </summary>
    [Test]
    public void GetActivitySnapshot_StreamParser_FlagsDataFieldEpilogueMismatch()
    {
        var trackPattern = BuildEncodedTrack(volume: 0xFE, track: 0x11);

        // Overwrite the first epilogue byte ($DE) of every data field with another
        // valid 6-and-2 nibble that isn't $DE; this preserves checksum integrity
        // (epilogue bytes are not part of the XOR chain) but breaks the epilogue
        // recognition check.
        const int sectorStride = 416;
        const int dataFieldStart = 48 + 14 + 5;
        for (var physical = 0; physical < 16; physical++)
        {
            var epilogueIndex = (physical * sectorStride) + dataFieldStart + 3 + 343;
            trackPattern[epilogueIndex] = 0xFF;
        }

        var media = new RepeatingPatternMedia(trackPattern);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(motorSettleCycles: 0);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx); // motor on
        for (var i = 0; i < 14000; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            _ = dispatcher.Read(0xEC, in ctx);
        }

        var snap = controller.GetActivitySnapshot();
        var drive = snap.Drives[0];
        Assert.Multiple(() =>
        {
            Assert.That(drive.ObservedDataPrologues, Is.GreaterThanOrEqualTo(16));
            Assert.That(drive.ObservedDataFieldDecodeSuccesses, Is.GreaterThanOrEqualTo(16), "Checksum is preserved so decode-success should still increment.");
            Assert.That(drive.ObservedDataFieldEpilogueMismatches, Is.GreaterThanOrEqualTo(16));
            Assert.That(drive.ObservedDataFieldChecksumErrors, Is.Zero);
        });
    }

    /// <summary>
    /// Verifies that the controller emits a Serilog Verbose record at every
    /// well-defined disk-event boundary — motor on/off, drive select, track
    /// step, and each address-field / data-field decode — so the user can
    /// capture a live trace of the byte stream the CPU is consuming by enabling
    /// the <c>DiskIIController</c> source context at <c>Verbose</c> level.
    /// This is the live-logging surface the user asked for; the after-the-fact
    /// <c>diskmon</c> snapshot is not sufficient to localise a per-file I/O
    /// failure inside a multi-second DOS retry storm.
    /// </summary>
    [Test]
    public void DiskActivity_LogsVerboseTraceAtEveryEventBoundary()
    {
        // Mock<ILogger> that reports Verbose enabled and records every Verbose call.
        var loggerMock = Generator.Log();
        loggerMock.Setup(l => l.IsEnabled(Serilog.Events.LogEventLevel.Verbose)).Returns(true);

        var trackPattern = BuildEncodedTrack(volume: 0xFE, track: 0x11);
        var media = new RepeatingPatternMedia(trackPattern);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(
            motorSettleCycles: 0,
            loggerMock: loggerMock);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        // Stimulus order: motor on, drive-stream long enough to observe at
        // least one address+data field pair (need >1 revolution = 6656 bytes
        // worth of fresh nibbles), then phase pulse for a track step, drive
        // toggle, motor off. The track step is deliberately last because it
        // schedules a 30 000-cycle settle that would otherwise mask data-path
        // observations.
        _ = dispatcher.Read(0xE9, in ctx); // motor on

        for (var i = 0; i < 14000; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            _ = dispatcher.Read(0xEC, in ctx);
        }

        // Phase 1 on, phase 0 off — single forward half-track step (qt 0→2).
        _ = dispatcher.Read(0xE1, in ctx); // phase 0 off (no-op from 0)
        _ = dispatcher.Read(0xE3, in ctx); // phase 1 on  → qt 0→2

        _ = dispatcher.Read(0xEB, in ctx); // select drive 2
        _ = dispatcher.Read(0xEA, in ctx); // select drive 1
        _ = dispatcher.Read(0xE8, in ctx); // motor off

        // Verify a Verbose trace line was emitted at each boundary. Scan
        // `loggerMock.Invocations` directly rather than `Verify(...)`: Serilog
        // exposes several generic `Verbose<T0…>(...)` overloads, and a `Verify`
        // expression like `l.Verbose(It.IsAny<string>(), It.IsAny<object[]>())`
        // only matches the `params object[]` overload — calls that bind to the
        // strongly-typed generic overloads slip past it. Walking the recorded
        // invocations checks "did any Verbose overload receive a template
        // containing X" which is exactly the contract we care about.
        bool VerboseEmitted(string token) => loggerMock.Invocations.Any(inv =>
            inv.Method.Name == "Verbose"
            && inv.Arguments.Count > 0
            && inv.Arguments[0] is string template
            && template.Contains(token, StringComparison.Ordinal));

        int VerboseCount(string token) => loggerMock.Invocations.Count(inv =>
            inv.Method.Name == "Verbose"
            && inv.Arguments.Count > 0
            && inv.Arguments[0] is string template
            && template.Contains(token, StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(VerboseEmitted("motor ON"), Is.True, "motor-on transition must emit a Verbose record");
            Assert.That(VerboseEmitted("motor OFF"), Is.True, "motor-off transition must emit a Verbose record");
            Assert.That(VerboseEmitted("track step"), Is.True, "track step must emit a Verbose record");
            Assert.That(VerboseCount("drive select"), Is.GreaterThanOrEqualTo(2), "every drive-select toggle must emit a Verbose record");
            Assert.That(VerboseEmitted("address field"), Is.True, "every decoded address field must emit a Verbose record");
            Assert.That(VerboseEmitted("data field"), Is.True, "every finalised data field must emit a Verbose record");
        });
    }

    /// <summary>
    /// Confirms the Verbose-guard is effective: when the injected logger reports
    /// <c>IsEnabled(Verbose) == false</c> (the default for production hosts that
    /// haven't opted in), the controller must not invoke <c>Verbose(...)</c> at
    /// all — the diagnostic logging carries zero per-event cost when disabled.
    /// </summary>
    [Test]
    public void DiskActivity_SkipsVerboseTraceWhenLevelDisabled()
    {
        // Default Generator.Log() mock returns false for IsEnabled — no explicit setup needed.
        var loggerMock = Generator.Log();

        var trackPattern = BuildEncodedTrack(volume: 0xFE, track: 0x11);
        var media = new RepeatingPatternMedia(trackPattern);

        var (controller, dispatcher, ctx, scheduler, _) = BuildHarness(
            motorSettleCycles: 0,
            loggerMock: loggerMock);
        controller.Mount(0, media);
        scheduler.Advance(new Cycle(2));

        _ = dispatcher.Read(0xE9, in ctx);
        for (var i = 0; i < 1000; i++)
        {
            scheduler.Advance(new Cycle(DiskIIController.CyclesPerByte));
            _ = dispatcher.Read(0xEC, in ctx);
        }

        _ = dispatcher.Read(0xE8, in ctx);

        // Scan invocations directly to catch every overload of Verbose<…>.
        Assert.That(
            loggerMock.Invocations.Any(inv => inv.Method.Name == "Verbose"),
            Is.False,
            "Verbose logging must be gated by IsEnabled so disabled hosts pay zero cost.");
    }

    /// <summary>
    /// Encodes a full 6656-byte track with deterministic per-sector content so the
    /// standard 48 / 14 / 5 / 349 sector stride is laid down for the live stream
    /// parser tests to consume.
    /// </summary>
    /// <param name="volume">Volume number used for every address field.</param>
    /// <param name="track">Track number used for every address field.</param>
    /// <returns>A 6656-byte nibble buffer ready for <c>RepeatingPatternMedia</c>.</returns>
    private static byte[] BuildEncodedTrack(byte volume, byte track)
    {
        var sectorData = new byte[16 * 256];
        for (var i = 0; i < sectorData.Length; i++)
        {
            sectorData[i] = (byte)i;
        }

        var dest = new byte[GcrEncoder.StandardTrackLength];
        GcrEncoder.EncodeTrack(volume, track, sectorData, dest);
        return dest;
    }

    /// <summary>
    /// Returns a 6-and-2 valid nibble that decodes to a different 6-bit value from
    /// the input. Used to flip a checksum byte without falling into the
    /// "invalid nibble" code path.
    /// </summary>
    /// <param name="original">A byte that must already be a valid 6-and-2 nibble.</param>
    /// <returns>A different valid 6-and-2 nibble byte.</returns>
    private static byte MapToDifferentValidNibble(byte original)
    {
        var readTable = GcrEncoder.GetReadTable();
        var writeTable = GcrEncoder.GetWriteTable();
        var sixBit = readTable[original];
        Assert.That(sixBit, Is.Not.EqualTo(0xFF), "Test precondition: original byte must be a valid 6-and-2 nibble.");

        // Use a different 6-bit value (XOR with 1 stays within [0, 64)).
        var alternate = (byte)(sixBit ^ 0x01);
        return writeTable[alternate];
    }

    /// <summary>
    /// Encodes an Apple GCR 4-and-4 address field (11 bytes total) for the given
    /// volume / track / sector triplet, with a checksum byte equal to vol XOR trk XOR sec.
    /// </summary>
    /// <param name="vol">Volume number.</param>
    /// <param name="trk">Track number.</param>
    /// <param name="sec">Sector number.</param>
    /// <returns>An 11-byte buffer: <c>$D5 $AA $96</c> followed by 8 4-and-4 nibble pairs.</returns>
    private static byte[] Encode4And4AddressField(byte vol, byte trk, byte sec)
    {
        var chk = (byte)(vol ^ trk ^ sec);
        var buf = new byte[11];
        buf[0] = 0xD5;
        buf[1] = 0xAA;
        buf[2] = 0x96;
        WritePair(buf, 3, vol);
        WritePair(buf, 5, trk);
        WritePair(buf, 7, sec);
        WritePair(buf, 9, chk);
        return buf;

        static void WritePair(byte[] b, int offset, byte v)
        {
            b[offset] = (byte)((v >> 1) | 0xAA);
            b[offset + 1] = (byte)(v | 0xAA);
        }
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

    /// <summary>
    /// Test fake that returns a track buffer formed by repeating a fixed byte pattern.
    /// Used by the address-field stream-parser tests to guarantee the head encounters
    /// a recognizable <c>$D5 $AA $96</c> prologue regardless of where it starts.
    /// </summary>
    private sealed class RepeatingPatternMedia : I525Media
    {
        private readonly byte[] pattern;

        public RepeatingPatternMedia(byte[] pattern)
        {
            this.pattern = pattern;
        }

        public DiskGeometry Geometry => DiskGeometry.Standard525Dos;

        public int OptimalTrackLength => GcrEncoder.StandardTrackLength;

        public bool IsReadOnly => false;

        public void ReadTrack(int quarterTrack, Span<byte> destination)
        {
            for (var i = 0; i < destination.Length; i++)
            {
                destination[i] = pattern[i % pattern.Length];
            }
        }

        public void WriteTrack(int quarterTrack, ReadOnlySpan<byte> source)
        {
        }

        public void Flush()
        {
        }
    }
}