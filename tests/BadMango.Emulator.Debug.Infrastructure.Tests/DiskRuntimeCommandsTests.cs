// <copyright file="DiskRuntimeCommandsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;
using BadMango.Emulator.Storage.Backends;
using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

using Moq;

/// <summary>
/// Unit tests for the runtime <see cref="DiskListCommand"/>, <see cref="DiskInsertCommand"/>,
/// <see cref="DiskEjectCommand"/>, and <see cref="DiskFlushCommand"/> debug subcommands
/// (PRD §6.5 FR-DC3..FR-DC6).
/// </summary>
[TestFixture]
public sealed class DiskRuntimeCommandsTests
{
    private string tempRoot = null!;
    private DebugContext debugContext = null!;
    private StringWriter outputWriter = null!;
    private StringWriter errorWriter = null!;
    private Mock<IMachine> machine = null!;
    private Mock<ISlotManager> slotManager = null!;

    /// <summary>Sets up per-test fixtures including a mocked machine + slot manager.</summary>
    [SetUp]
    public void SetUp()
    {
        this.tempRoot = Path.Combine(Path.GetTempPath(), $"bms-disk-rt-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempRoot);

        this.outputWriter = new StringWriter();
        this.errorWriter = new StringWriter();

        var dispatcher = new CommandDispatcher();
        this.debugContext = new DebugContext(dispatcher, this.outputWriter, this.errorWriter);
        this.debugContext.AttachDiskImageFactory(new DiskImageFactory());

        this.slotManager = new Mock<ISlotManager>();
        this.machine = new Mock<IMachine>();
        this.machine.Setup(m => m.GetComponent<ISlotManager>()).Returns(this.slotManager.Object);
        this.debugContext.AttachMachine(this.machine.Object);
    }

    /// <summary>Cleans up per-test temp files.</summary>
    [TearDown]
    public void TearDown()
    {
        this.outputWriter.Dispose();
        this.errorWriter.Dispose();
        this.debugContext.Dispose();
        try
        {
            if (Directory.Exists(this.tempRoot))
            {
                Directory.Delete(this.tempRoot, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort.
        }
    }

    /// <summary>All four subcommand classes carry the auto-registration attribute.</summary>
    [Test]
    public void RuntimeSubcommands_AreMarkedForAutoRegistration()
    {
        var attr = typeof(BadMango.Emulator.Devices.DeviceDebugCommandAttribute);
        Assert.Multiple(() =>
        {
            Assert.That(typeof(DiskListCommand).GetCustomAttributes(attr, inherit: false), Is.Not.Empty);
            Assert.That(typeof(DiskInsertCommand).GetCustomAttributes(attr, inherit: false), Is.Not.Empty);
            Assert.That(typeof(DiskEjectCommand).GetCustomAttributes(attr, inherit: false), Is.Not.Empty);
            Assert.That(typeof(DiskFlushCommand).GetCustomAttributes(attr, inherit: false), Is.Not.Empty);
        });
    }

    /// <summary>The parent <c>disk</c> command routes the four runtime subcommands.</summary>
    [Test]
    public void DiskCommand_RoutesRuntimeSubcommands()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        ctl.Setup(c => c.GetDriveSnapshot(It.IsAny<int>())).Returns(EmptySnapshot());

        var parent = new DiskCommand();
        var list = parent.Execute(this.debugContext, ["list"]);

        Assert.Multiple(() =>
        {
            Assert.That(list.Success, Is.True, list.Message);
            Assert.That(this.outputWriter.ToString(), Does.Contain("Slot 6"));
        });
    }

    /// <summary><c>disk list</c> with no installed disk controllers prints a friendly stub.</summary>
    [Test]
    public void DiskList_NoControllers_PrintsNoneInstalled()
    {
        var result = new DiskListCommand().Execute(this.debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(this.outputWriter.ToString(), Does.Contain("No disk controllers installed"));
        });
    }

    /// <summary><c>disk list</c> rejects extra positional arguments.</summary>
    [Test]
    public void DiskList_RejectsExtraArgs()
    {
        var result = new DiskListCommand().Execute(this.debugContext, ["6:1"]);
        Assert.That(result.Success, Is.False);
    }

    /// <summary><c>disk list</c> prints a row per drive (mounted and empty) with snapshot fields.</summary>
    [Test]
    public void DiskList_PrintsPerDriveSnapshot()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);

        ctl.Setup(c => c.GetDriveSnapshot(0)).Returns(new DriveSnapshot(
            Selected: true,
            MotorOn: false,
            PhaseLatch: 0,
            QuarterTrack: 12,
            WriteProtect: true,
            HasMedia: true,
            MountedImagePath: "/tmp/img.dsk"));
        ctl.Setup(c => c.GetDriveSnapshot(1)).Returns(EmptySnapshot());

        var result = new DiskListCommand().Execute(this.debugContext, []);
        Assert.That(result.Success, Is.True, result.Message);

        var output = this.outputWriter.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Slot 6"));
            Assert.That(output, Does.Contain("Drive 1: mounted '/tmp/img.dsk'"));
            Assert.That(output, Does.Contain("write-protect=yes"));
            Assert.That(output, Does.Contain("quarter-track=12"));
            Assert.That(output, Does.Contain("Drive 2: empty"));
        });
    }

    /// <summary><c>disk list</c> errors clearly when no machine is attached.</summary>
    [Test]
    public void DiskList_NoMachine_ReturnsError()
    {
        var dispatcher = new CommandDispatcher();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var bare = new DebugContext(dispatcher, output, error);

        var result = new DiskListCommand().Execute(bare, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("No machine attached"));
        });
    }

    /// <summary><c>disk insert</c> mounts a 5.25" image and reports success.</summary>
    [Test]
    public void DiskInsert_MountsImage_AndCallsMount()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);

        var path = this.WriteBlankSectorImage(".dsk");

        var result = new DiskInsertCommand().Execute(this.debugContext, ["6:1", path]);

        Assert.That(result.Success, Is.True, result.Message);
        ctl.Verify(c => c.Mount(0, It.IsAny<I525Media>(), path), Times.Once);
        Assert.That(this.outputWriter.ToString(), Does.Contain($"Inserted '{path}'"));
    }

    /// <summary><c>disk insert --write-protect</c> opens the image read-only.</summary>
    [Test]
    public void DiskInsert_WriteProtect_MountsReadOnlyMedia()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        I525Media? captured = null;
        ctl.Setup(c => c.Mount(It.IsAny<int>(), It.IsAny<I525Media>(), It.IsAny<string>()))
            .Callback<int, I525Media, string?>((_, m, _) => captured = m);

        var path = this.WriteBlankSectorImage(".dsk");

        var result = new DiskInsertCommand().Execute(this.debugContext, ["6:1", path, "--write-protect"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.IsReadOnly, Is.True);
            Assert.That(this.outputWriter.ToString(), Does.Contain("write-protected"));
        });
    }

    /// <summary><c>disk insert</c> rejects a 3.5" / block-only image targeted at a 5.25" controller.</summary>
    [Test]
    public void DiskInsert_BlockOnlyImage_ReturnsFormatMismatchError()
    {
        var (card, ctl) = MakeMockController(slot: 7, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(7)).Returns(card.Object);

        // .hdv produces a pure ImageBlockResult (no I525Media view).
        var path = Path.Combine(this.tempRoot, $"hdv-{Guid.NewGuid():N}.hdv");
        File.WriteAllBytes(path, new byte[1600 * 512]);

        var result = new DiskInsertCommand().Execute(this.debugContext, ["7:1", path]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("no 5.25\" track view"));
        });
        ctl.Verify(c => c.Mount(It.IsAny<int>(), It.IsAny<I525Media>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary><c>disk insert</c> rejects a malformed slot:drive token.</summary>
    /// <param name="token">The malformed slot:drive token under test.</param>
    [TestCase("")]
    [TestCase("6")]
    [TestCase("6:")]
    [TestCase(":1")]
    [TestCase("0:1")]
    [TestCase("8:1")]
    [TestCase("6:0")]
    [TestCase("xx:1")]
    public void DiskInsert_BadSlotDrive_ReturnsError(string token)
    {
        var path = this.WriteBlankSectorImage(".dsk");
        var result = new DiskInsertCommand().Execute(this.debugContext, [token, path]);
        Assert.That(result.Success, Is.False, $"Token '{token}' should have failed parsing.");
    }

    /// <summary><c>disk insert</c> reports a clear error when the targeted slot is empty.</summary>
    [Test]
    public void DiskInsert_EmptySlot_ReturnsError()
    {
        this.slotManager.Setup(m => m.GetCard(It.IsAny<int>())).Returns((ISlotCard?)null);
        var path = this.WriteBlankSectorImage(".dsk");

        var result = new DiskInsertCommand().Execute(this.debugContext, ["6:1", path]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Slot 6 is empty"));
        });
    }

    /// <summary><c>disk insert</c> rejects a non-disk slot card with a clear error.</summary>
    [Test]
    public void DiskInsert_NonDiskCard_ReturnsError()
    {
        var card = new Mock<ISlotCard>();
        card.SetupGet(c => c.Name).Returns("Mockingbird");
        this.slotManager.Setup(m => m.GetCard(4)).Returns(card.Object);
        var path = this.WriteBlankSectorImage(".dsk");

        var result = new DiskInsertCommand().Execute(this.debugContext, ["4:1", path]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("not a disk controller"));
        });
    }

    /// <summary><c>disk insert</c> reports a clear error when the file does not exist.</summary>
    [Test]
    public void DiskInsert_MissingFile_ReturnsError()
    {
        var (card, _) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);

        var path = Path.Combine(this.tempRoot, "no-such.dsk");
        var result = new DiskInsertCommand().Execute(this.debugContext, ["6:1", path]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("File not found"));
        });
    }

    /// <summary><c>disk eject</c> forwards to <see cref="IDiskController.Eject"/> on success.</summary>
    [Test]
    public void DiskEject_Succeeds_CallsControllerAndReportsOk()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        ctl.Setup(c => c.Eject(0)).Returns(true);

        var result = new DiskEjectCommand().Execute(this.debugContext, ["6:1"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(this.outputWriter.ToString(), Does.Contain("Ejected slot 6 drive 1"));
        });
        ctl.Verify(c => c.Eject(0), Times.Once);
    }

    /// <summary>
    /// When <see cref="IDiskController.Eject"/> returns <see langword="false"/> for an empty
    /// drive, the command surfaces an "already empty" error.
    /// </summary>
    [Test]
    public void DiskEject_EmptyDrive_ReturnsAlreadyEmpty()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        ctl.Setup(c => c.Eject(0)).Returns(false);
        ctl.Setup(c => c.GetDriveSnapshot(0)).Returns(EmptySnapshot());

        var result = new DiskEjectCommand().Execute(this.debugContext, ["6:1"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("already empty"));
        });
    }

    /// <summary>
    /// When <see cref="IDiskController.Eject"/> returns <see langword="false"/> while the
    /// snapshot still reports media present, the command surfaces an FR-R2 flush-rejection
    /// error rather than misreporting success.
    /// </summary>
    [Test]
    public void DiskEject_FlushRejected_ReturnsClearError()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        ctl.Setup(c => c.Eject(0)).Returns(false);
        ctl.Setup(c => c.GetDriveSnapshot(0)).Returns(new DriveSnapshot(
            Selected: false,
            MotorOn: false,
            PhaseLatch: 0,
            QuarterTrack: 0,
            WriteProtect: false,
            HasMedia: true,
            MountedImagePath: "/tmp/x.dsk"));

        var result = new DiskEjectCommand().Execute(this.debugContext, ["6:1"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("rejected"));
            Assert.That(result.Message, Does.Contain("flush failed"));
        });
    }

    /// <summary><c>disk flush</c> forwards to <see cref="IDiskController.Flush"/>.</summary>
    [Test]
    public void DiskFlush_CallsController()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        ctl.Setup(c => c.GetDriveSnapshot(1)).Returns(new DriveSnapshot(
            Selected: false,
            MotorOn: false,
            PhaseLatch: 0,
            QuarterTrack: 0,
            WriteProtect: false,
            HasMedia: true,
            MountedImagePath: "/tmp/y.dsk"));

        var result = new DiskFlushCommand().Execute(this.debugContext, ["6:2"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(this.outputWriter.ToString(), Does.Contain("Flushed slot 6 drive 2"));
        });
        ctl.Verify(c => c.Flush(1), Times.Once);
    }

    /// <summary><c>disk flush</c> rejects a flush against an empty drive.</summary>
    [Test]
    public void DiskFlush_EmptyDrive_ReturnsError()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        ctl.Setup(c => c.GetDriveSnapshot(0)).Returns(EmptySnapshot());

        var result = new DiskFlushCommand().Execute(this.debugContext, ["6:1"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("nothing to flush"));
        });
        ctl.Verify(c => c.Flush(It.IsAny<int>()), Times.Never);
    }

    /// <summary><c>disk eject</c> rejects out-of-range drive numbers with a clear error.</summary>
    [Test]
    public void DiskEject_DriveOutOfRange_ReturnsError()
    {
        var (card, _) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);

        var result = new DiskEjectCommand().Execute(this.debugContext, ["6:3"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Drive must be in 1..2"));
        });
    }

    /// <summary>
    /// <c>disk insert</c> registers the open result with the debug context's
    /// <see cref="MountedDiskRegistry"/> so the file handle can be released later.
    /// </summary>
    [Test]
    public void DiskInsert_TracksOpenResultInRegistry()
    {
        var (card, _) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        var path = this.WriteBlankSectorImage(".dsk");

        Assert.That(this.debugContext.MountedDisks.Count, Is.Zero);
        var result = new DiskInsertCommand().Execute(this.debugContext, ["6:1", path]);
        Assert.That(result.Success, Is.True, result.Message);
        Assert.That(this.debugContext.MountedDisks.Count, Is.EqualTo(1));
    }

    /// <summary>
    /// Re-inserting on the same drive disposes the prior open result rather than leaking
    /// its file handle.
    /// </summary>
    [Test]
    public void DiskInsert_ReplacingPriorMount_DisposesPriorOpen()
    {
        var (card, _) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        var first = this.WriteBlankSectorImage(".dsk");
        var second = this.WriteBlankSectorImage(".dsk");

        Assert.That(new DiskInsertCommand().Execute(this.debugContext, ["6:1", first]).Success, Is.True);
        Assert.That(new DiskInsertCommand().Execute(this.debugContext, ["6:1", second]).Success, Is.True);

        // Still exactly one tracked entry for this drive — the prior open was disposed.
        Assert.That(this.debugContext.MountedDisks.Count, Is.EqualTo(1));

        // And the prior file is no longer locked: we can take an exclusive (FileShare.None)
        // handle on it. If the prior open had leaked, FileStorageBackend's FileShare.None
        // hold would still be active and this would throw IOException.
        Assert.DoesNotThrow(() =>
        {
            using var fs = new FileStream(first, FileMode.Open, FileAccess.Read, FileShare.None);
        });
    }

    /// <summary>
    /// A successful eject releases the registry entry (and therefore the file handle) for
    /// the targeted drive.
    /// </summary>
    [Test]
    public void DiskEject_Success_ReleasesRegistryEntry()
    {
        var (card, ctl) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        ctl.Setup(c => c.Eject(0)).Returns(true);

        var path = this.WriteBlankSectorImage(".dsk");
        Assert.That(new DiskInsertCommand().Execute(this.debugContext, ["6:1", path]).Success, Is.True);
        Assert.That(this.debugContext.MountedDisks.Count, Is.EqualTo(1));

        Assert.That(new DiskEjectCommand().Execute(this.debugContext, ["6:1"]).Success, Is.True);
        Assert.That(this.debugContext.MountedDisks.Count, Is.Zero);

        // Underlying file is no longer locked by the now-disposed open result.
        Assert.DoesNotThrow(() =>
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        });
    }

    /// <summary>
    /// Disposing the <see cref="DebugContext"/> disposes every retained open result so
    /// the host filesystem no longer holds the image files.
    /// </summary>
    [Test]
    public void DebugContext_Dispose_ReleasesAllRetainedOpens()
    {
        var path = this.WriteBlankSectorImage(".dsk");

        // Use a freshly-scoped context so disposing it does not interfere with TearDown.
        var dispatcher = new CommandDispatcher();
        using (var output = new StringWriter())
        using (var error = new StringWriter())
        {
            var ctx = new DebugContext(dispatcher, output, error);
            ctx.AttachDiskImageFactory(new DiskImageFactory());
            var localManager = new Mock<ISlotManager>();
            var localMachine = new Mock<IMachine>();
            localMachine.Setup(m => m.GetComponent<ISlotManager>()).Returns(localManager.Object);
            ctx.AttachMachine(localMachine.Object);

            var (card, _) = MakeMockController(slot: 6, driveCount: 2);
            localManager.Setup(m => m.GetCard(6)).Returns(card.Object);

            Assert.That(new DiskInsertCommand().Execute(ctx, ["6:1", path]).Success, Is.True);
            Assert.That(ctx.MountedDisks.Count, Is.EqualTo(1));

            ctx.Dispose();
            Assert.That(ctx.MountedDisks.Count, Is.Zero);
        }

        // After the context is disposed, the file is no longer locked.
        Assert.DoesNotThrow(() =>
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        });
    }

    /// <summary>
    /// <see cref="DebugContext.DetachSystem"/> clears the registry (releasing handles)
    /// but leaves it usable for a subsequent re-attach.
    /// </summary>
    [Test]
    public void DetachSystem_ClearsRegistry_ButLeavesItUsable()
    {
        var (card, _) = MakeMockController(slot: 6, driveCount: 2);
        this.slotManager.Setup(m => m.GetCard(6)).Returns(card.Object);
        var path = this.WriteBlankSectorImage(".dsk");

        Assert.That(new DiskInsertCommand().Execute(this.debugContext, ["6:1", path]).Success, Is.True);
        Assert.That(this.debugContext.MountedDisks.Count, Is.EqualTo(1));

        this.debugContext.DetachSystem();
        Assert.That(this.debugContext.MountedDisks.Count, Is.Zero);

        // Reattach and re-insert: registry must still accept new entries.
        this.debugContext.AttachMachine(this.machine.Object);
        var second = this.WriteBlankSectorImage(".dsk");
        Assert.That(new DiskInsertCommand().Execute(this.debugContext, ["6:1", second]).Success, Is.True);
        Assert.That(this.debugContext.MountedDisks.Count, Is.EqualTo(1));
    }

    private static (Mock<ISlotCard> Card, Mock<IDiskController> Controller) MakeMockController(int slot, int driveCount)
    {
        // Single mock object that implements both ISlotCard (for GetCard) and IDiskController
        // (for the runtime subcommands) so the cast in DiskRuntimeHelpers.TryGetController
        // succeeds — mirrors the production DiskIIController shape.
        var cardMock = new Mock<ISlotCard>();
        var ctlMock = cardMock.As<IDiskController>();
        cardMock.SetupGet(c => c.Name).Returns("Disk II Controller");
        cardMock.SetupGet(c => c.SlotNumber).Returns(slot);
        ctlMock.SetupGet(c => c.SlotNumber).Returns(slot);
        ctlMock.SetupGet(c => c.DriveCount).Returns(driveCount);
        return (cardMock, ctlMock);
    }

    private static DriveSnapshot EmptySnapshot() => new(
        Selected: false,
        MotorOn: false,
        PhaseLatch: 0,
        QuarterTrack: 0,
        WriteProtect: false,
        HasMedia: false,
        MountedImagePath: null);

    /// <summary>
    /// Writes a blank 35-track DOS 3.3 ordered sector image to a temp file and returns its
    /// path. The file is large enough for <see cref="DiskImageFactory"/> to recognise it
    /// as a 5.25" sector image so the runtime <c>disk insert</c> path can mount it.
    /// </summary>
    private string WriteBlankSectorImage(string ext)
    {
        var path = Path.Combine(this.tempRoot, $"img-{Guid.NewGuid():N}{ext}");
        File.WriteAllBytes(path, new byte[35 * 16 * 256]);
        return path;
    }
}