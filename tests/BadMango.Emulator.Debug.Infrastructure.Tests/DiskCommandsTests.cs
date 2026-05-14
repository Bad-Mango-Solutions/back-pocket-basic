// <copyright file="DiskCommandsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;
using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Unit tests for <see cref="DiskCommand"/>, <see cref="DiskCreateCommand"/>, and
/// <see cref="DiskInfoCommand"/>.
/// </summary>
[TestFixture]
public sealed class DiskCommandsTests
{
    private string tempRoot = null!;
    private List<string> tempFiles = null!;
    private DebugContext debugContext = null!;
    private StringWriter outputWriter = null!;
    private StringWriter errorWriter = null!;

    /// <summary>Sets up per-test fixtures.</summary>
    [SetUp]
    public void SetUp()
    {
        this.tempRoot = Path.Combine(Path.GetTempPath(), $"bms-disk-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempRoot);
        this.tempFiles = [];

        this.outputWriter = new StringWriter();
        this.errorWriter = new StringWriter();

        var dispatcher = new CommandDispatcher();
        this.debugContext = new DebugContext(dispatcher, this.outputWriter, this.errorWriter);
        this.debugContext.AttachDiskImageFactory(new DiskImageFactory());
    }

    /// <summary>Cleans up per-test temp files.</summary>
    [TearDown]
    public void TearDown()
    {
        this.outputWriter.Dispose();
        this.errorWriter.Dispose();
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

        foreach (var f in this.tempFiles)
        {
            try
            {
                if (File.Exists(f))
                {
                    File.Delete(f);
                }
            }
            catch (IOException)
            {
                // Best effort.
            }
        }
    }

    /// <summary>Top-level <c>disk</c> command name and aliases.</summary>
    [Test]
    public void DiskCommand_Metadata_IsCorrect()
    {
        var cmd = new DiskCommand();
        Assert.Multiple(() =>
        {
            Assert.That(cmd.Name, Is.EqualTo("disk"));
            Assert.That(cmd.Description, Does.Contain("disk image"));
        });
    }

    /// <summary>Top-level <c>disk</c> with no args returns an error suggesting subcommands.</summary>
    [Test]
    public void DiskCommand_NoArgs_ReturnsError()
    {
        var cmd = new DiskCommand();
        var result = cmd.Execute(this.debugContext, []);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("create").And.Contain("info"));
        });
    }

    /// <summary>An unknown subcommand is rejected with a clear error.</summary>
    [Test]
    public void DiskCommand_UnknownSubcommand_ReturnsError()
    {
        var cmd = new DiskCommand();
        var result = cmd.Execute(this.debugContext, ["wibble"]);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("wibble"));
        });
    }

    /// <summary>The two subcommand classes carry the auto-registration attribute.</summary>
    [Test]
    public void DiskSubcommands_AreMarkedForAutoRegistration()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(DiskCommand).GetCustomAttributes(typeof(BadMango.Emulator.Devices.DeviceDebugCommandAttribute), inherit: false), Is.Not.Empty);
            Assert.That(typeof(DiskCreateCommand).GetCustomAttributes(typeof(BadMango.Emulator.Devices.DeviceDebugCommandAttribute), inherit: false), Is.Not.Empty);
            Assert.That(typeof(DiskInfoCommand).GetCustomAttributes(typeof(BadMango.Emulator.Devices.DeviceDebugCommandAttribute), inherit: false), Is.Not.Empty);
        });
    }

    /// <summary><c>disk-create</c> advertises every supported flag through its <c>Options</c> list so <c>help</c> works.</summary>
    [Test]
    public void DiskCreate_OptionsListAdvertisesAllFlags()
    {
        var names = new DiskCreateCommand().Options.Select(o => o.Name).ToList();
        Assert.That(names, Is.EquivalentTo(new[] { "--size", "--format", "--bootable", "--volume-name", "--volume-number", "--only-uppercase" }));
    }

    /// <summary><c>disk-create</c> writes via a temp file + rename, leaving no leftover staging file on success.</summary>
    [Test]
    public void DiskCreate_DoesNotLeaveStagingFile()
    {
        var path = this.TempPath(".dsk");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "dos33"]);
        Assert.That(c.Success, Is.True, c.Message);

        var leftover = Directory.GetFiles(this.tempRoot, "*.tmp", SearchOption.TopDirectoryOnly);
        Assert.That(leftover, Is.Empty, "Atomic write should not leave any *.tmp staging file behind.");
    }

    /// <summary><c>disk create</c> with no arguments is rejected with usage.</summary>
    [Test]
    public void DiskCreate_NoArgs_ReturnsError()
    {
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, []);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Path required"));
        });
    }

    /// <summary><c>disk create</c> rejects an unsupported file extension.</summary>
    [Test]
    public void DiskCreate_UnsupportedExtension_ReturnsError()
    {
        var path = this.TempPath(".woz");
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, [path]);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain(".woz"));
        });
    }

    /// <summary><c>disk create</c> refuses to overwrite an existing file.</summary>
    [Test]
    public void DiskCreate_RefusesToOverwrite()
    {
        var path = this.TempPath(".dsk");
        File.WriteAllBytes(path, [0]);
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, [path]);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Refusing to overwrite"));
        });
    }

    /// <summary><c>disk create</c> with bad option value reports a clear error.</summary>
    [Test]
    public void DiskCreate_BadVolumeNumber_ReturnsError()
    {
        var path = this.TempPath(".dsk");
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, [path, "--format", "dos33", "--volume-number", "999"]);
        Assert.That(result.Success, Is.False);
    }

    /// <summary><c>disk create</c> rejects --format prodos on a .do (DOS-ordered) container.</summary>
    [Test]
    public void DiskCreate_ProDosOnDoExtension_ReturnsError()
    {
        var path = this.TempPath(".do");
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, [path, "--format", "prodos"]);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("prodos"));
        });
    }

    /// <summary><c>disk create</c> default raw .dsk is opened back as a 5.25" sector image.</summary>
    [Test]
    public void DiskCreate_RawDsk_RoundTripsThroughFactory()
    {
        var path = this.TempPath(".dsk");
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, [path]);
        Assert.That(result.Success, Is.True, result.Message);

        Assert.That(new FileInfo(path).Length, Is.EqualTo(35 * 16 * 256));

        var open = new DiskImageFactory().Open(path, forceReadOnly: true);
        Assert.That(open, Is.InstanceOf<Image525AndBlockResult>());
    }

    /// <summary>DOS 3.3 format produces a valid VTOC that the order sniffer recognises as DOS.</summary>
    [Test]
    public void DiskCreate_Dos33Dsk_ProducesValidVtoc()
    {
        var path = this.TempPath(".dsk");
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, [path, "--format", "dos33", "--volume-number", "200"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (Image525AndBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        Assert.Multiple(() =>
        {
            Assert.That(open.Format, Is.EqualTo(DiskImageFormat.Dos33SectorImage));
            Assert.That(open.SectorOrder, Is.EqualTo(SectorOrder.Dos33));
            Assert.That(open.WasOrderSniffed, Is.True, "DOS 3.3 VTOC must be detectable by the .dsk sniffer.");
        });

        // Validate VTOC fields directly: DOS-logical sector 0 of track 17 is at file offset 17*16*256.
        var bytes = File.ReadAllBytes(path);
        var vtocOffset = 17 * 16 * 256;
        Assert.Multiple(() =>
        {
            Assert.That(bytes[vtocOffset + 0x01], Is.EqualTo(0x11));
            Assert.That(bytes[vtocOffset + 0x03], Is.EqualTo(0x03));
            Assert.That(bytes[vtocOffset + 0x06], Is.EqualTo(200));
            Assert.That(bytes[vtocOffset + 0x35], Is.EqualTo(0x10));
        });
    }

    /// <summary>ProDOS format on .po produces a valid root directory at block 2.</summary>
    [Test]
    public void DiskCreate_ProDosPo_ProducesValidRootDirectory()
    {
        var path = this.TempPath(".po");
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, [path, "--format", "prodos", "--volume-name", "TESTVOL"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (Image525AndBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        Assert.Multiple(() =>
        {
            Assert.That(open.Format, Is.EqualTo(DiskImageFormat.ProDosSectorImage));
            Assert.That(open.SectorOrder, Is.EqualTo(SectorOrder.ProDos));
        });

        // Read block 2 via the BlockMedia view and validate the volume header.
        var keyBlock = new byte[512];
        open.BlockMedia.ReadBlock(2, keyBlock);
        Assert.Multiple(() =>
        {
            Assert.That(keyBlock[0], Is.EqualTo(0));
            Assert.That(keyBlock[1], Is.EqualTo(0));
            Assert.That(keyBlock[4] & 0xF0, Is.EqualTo(0xF0));
            Assert.That(keyBlock[4] & 0x0F, Is.EqualTo(7), "Volume name length should match 'TESTVOL'.");

            var name = System.Text.Encoding.ASCII.GetString(keyBlock, 5, 7);
            Assert.That(name, Is.EqualTo("TESTVOL"));

            // total_blocks little-endian at offset 0x29..0x2A.
            var total = (ushort)(keyBlock[0x29] | (keyBlock[0x2A] << 8));
            Assert.That(total, Is.EqualTo(280));
        });
    }

    /// <summary>ProDOS .hdv at 32M produces a valid block image accepted by the factory.</summary>
    [Test]
    public void DiskCreate_ProDosHdv32M_RoundTripsThroughFactory()
    {
        var path = this.TempPath(".hdv");
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, [path, "--size", "32M", "--format", "prodos", "--volume-name", "BIG"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (ImageBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        Assert.Multiple(() =>
        {
            Assert.That(open.Format, Is.EqualTo(DiskImageFormat.HdvBlockImage));
            Assert.That(open.Media.BlockCount, Is.EqualTo(65535));
        });
    }

    /// <summary>2MG with ProDOS format wraps the payload with a valid 2MG header.</summary>
    [Test]
    public void DiskCreate_ProDosTwoImg_RoundTripsAndHasHeader()
    {
        var path = this.TempPath(".2mg");
        var cmd = new DiskCreateCommand();
        var result = cmd.Execute(this.debugContext, [path, "--format", "prodos", "--volume-name", "BLANK"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (Image525AndBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        Assert.That(open.Format, Is.EqualTo(DiskImageFormat.TwoImgProDos));

        // Header must parse correctly.
        var head = File.ReadAllBytes(path);
        var header = TwoImgHeader.Parse(head.AsSpan(0, 64));
        Assert.Multiple(() =>
        {
            Assert.That(header.HeaderLength, Is.EqualTo(64));
            Assert.That(header.Format, Is.EqualTo(1));
            Assert.That(header.DataLength, Is.EqualTo(35 * 16 * 256));
        });
    }

    /// <summary>Bootable copies the boot blocks of the source into the new image.</summary>
    [Test]
    public void DiskCreate_Bootable_CopiesBootSectorsFromSource()
    {
        // Author a source DOS 3.3 image with a unique boot signature in track 0 / sector 0.
        var sourcePath = this.TempPath(".dsk");
        var src = new DiskCreateCommand().Execute(this.debugContext, [sourcePath, "--format", "dos33"]);
        Assert.That(src.Success, Is.True, src.Message);

        // Stamp a recognizable byte at file offset 0 (track 0, DOS-logical sector 0 == physical sector 0).
        var srcBytes = File.ReadAllBytes(sourcePath);
        srcBytes[0] = 0xA9; // LDA #imm — looks like 6502 boot code.
        srcBytes[1] = 0xFE;
        File.WriteAllBytes(sourcePath, srcBytes);

        var destPath = this.TempPath(".dsk");
        var dest = new DiskCreateCommand().Execute(this.debugContext, [destPath, "--format", "dos33", "--bootable", sourcePath]);
        Assert.That(dest.Success, Is.True, dest.Message);

        var destBytes = File.ReadAllBytes(destPath);
        Assert.Multiple(() =>
        {
            Assert.That(destBytes[0], Is.EqualTo(0xA9), "Boot byte 0 should be copied.");
            Assert.That(destBytes[1], Is.EqualTo(0xFE), "Boot byte 1 should be copied.");

            // VTOC at track 17 is still intact (boot must not overwrite VTOC).
            Assert.That(destBytes[(17 * 16 * 256) + 0x01], Is.EqualTo(0x11));
        });
    }

    /// <summary><c>disk-info</c> requires a path argument.</summary>
    [Test]
    public void DiskInfo_NoArgs_ReturnsError()
    {
        var cmd = new DiskInfoCommand();
        var result = cmd.Execute(this.debugContext, []);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Path required"));
        });
    }

    /// <summary><c>disk-info</c> reports a clear error for a missing file.</summary>
    [Test]
    public void DiskInfo_MissingFile_ReturnsError()
    {
        var cmd = new DiskInfoCommand();
        var result = cmd.Execute(this.debugContext, [Path.Combine(this.tempRoot, "no-such.dsk")]);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("not found"));
        });
    }

    /// <summary><c>disk-info</c> reports format/geometry/sniff for a DOS 3.3 .dsk authored by <c>disk-create</c>.</summary>
    [Test]
    public void DiskInfo_OnDos33Dsk_ReportsExpectedMetadata()
    {
        var path = this.TempPath(".dsk");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "dos33"]);
        Assert.That(c.Success, Is.True, c.Message);

        var result = new DiskInfoCommand().Execute(this.debugContext, [path]);
        Assert.That(result.Success, Is.True, result.Message);

        var output = this.outputWriter.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("Dos33SectorImage"));
            Assert.That(output, Does.Contain("Sector order:     Dos33"));
            Assert.That(output, Does.Contain("35 tracks"));
            Assert.That(output, Does.Contain(".dsk sniffed:     yes"));
            Assert.That(output, Does.Contain("Write-protected:  no"));
        });
    }

    /// <summary><c>disk-info</c> reports 2MG header metadata for a 2MG image.</summary>
    [Test]
    public void DiskInfo_OnTwoImg_ReportsHeaderMetadata()
    {
        var path = this.TempPath(".2mg");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "prodos"]);
        Assert.That(c.Success, Is.True, c.Message);

        var result = new DiskInfoCommand().Execute(this.debugContext, [path]);
        Assert.That(result.Success, Is.True, result.Message);

        var output = this.outputWriter.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("TwoImgProDos"));
            Assert.That(output, Does.Contain("2MG creator:      BMSL"));
            Assert.That(output, Does.Contain("2MG format code:  1"));
        });
    }

    /// <summary>
    /// <c>disk-info</c> opens read-only so it doesn't take the file's exclusive write share.
    /// Another reader must still be able to open the file after <c>disk-info</c> returns.
    /// </summary>
    [Test]
    public void DiskInfo_DoesNotBlockOtherReaders()
    {
        var path = this.TempPath(".dsk");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "dos33"]);
        Assert.That(c.Success, Is.True, c.Message);

        var result = new DiskInfoCommand().Execute(this.debugContext, [path]);
        Assert.That(result.Success, Is.True, result.Message);

        // After disk-info, another reader (FileShare.Read) must still succeed. If disk-info
        // had taken a writable handle (FileShare.None), this would throw IOException.
        Assert.DoesNotThrow(() =>
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        });
    }

    /// <summary>
    /// <c>disk-info</c> must release the file handle before returning so the user can
    /// rename/delete the image afterwards. Verified by acquiring an exclusive
    /// (<see cref="FileShare.None"/>) writable handle, which fails if any other handle
    /// to the file is still open.
    /// </summary>
    [Test]
    public void DiskInfo_DoesNotLeaveFileOpen()
    {
        var path = this.TempPath(".dsk");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "dos33"]);
        Assert.That(c.Success, Is.True, c.Message);

        var result = new DiskInfoCommand().Execute(this.debugContext, [path]);
        Assert.That(result.Success, Is.True, result.Message);

        // FileShare.None requires no other open handle on the file. If disk-info
        // had failed to dispose its DiskImageOpenResult, this would throw IOException
        // (matching the user-reported "file is open in emudbg" rename failure).
        Assert.DoesNotThrow(() =>
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        });
    }

    /// <summary>
    /// <c>disk-create --bootable</c> opens the boot source through the factory and must
    /// release that handle before returning so the source file is not locked.
    /// </summary>
    [Test]
    public void DiskCreate_Bootable_DoesNotLeaveSourceFileOpen()
    {
        var sourcePath = this.TempPath(".dsk");
        var src = new DiskCreateCommand().Execute(this.debugContext, [sourcePath, "--format", "dos33"]);
        Assert.That(src.Success, Is.True, src.Message);

        var destPath = this.TempPath(".dsk");
        var dest = new DiskCreateCommand().Execute(this.debugContext, [destPath, "--format", "dos33", "--bootable", sourcePath]);
        Assert.That(dest.Success, Is.True, dest.Message);

        // After disk-create --bootable returns, exclusive access to the source must succeed.
        Assert.DoesNotThrow(() =>
        {
            using var fs = new FileStream(sourcePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        });
    }

    /// <summary><c>disk-info</c> reports HDV block image metadata.</summary>
    [Test]
    public void DiskInfo_OnHdv_ReportsBlockCount()
    {
        var path = this.TempPath(".hdv");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--size", "3.5", "--format", "prodos", "--volume-name", "TEST"]);
        Assert.That(c.Success, Is.True, c.Message);

        var result = new DiskInfoCommand().Execute(this.debugContext, [path]);
        Assert.That(result.Success, Is.True, result.Message);

        var output = this.outputWriter.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("HdvBlockImage"));
            Assert.That(output, Does.Contain("Block count:      1600"));
        });
    }

    /// <summary>
    /// <c>disk-info</c> on a 3.5" 800K ProDOS-formatted 2MG image must report a 3.5"
    /// block image rather than a 5.25" sector image (regression test for the bug where
    /// a 200-track "5.25" sector image (track + block views)" line was emitted instead).
    /// Also asserts that the ProDOS volume name is reported.
    /// </summary>
    [Test]
    public void DiskInfo_OnThreePointFiveProDosTwoImg_ReportsBlockImageAndVolumeName()
    {
        var path = this.TempPath(".2mg");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "prodos", "--size", "3.5", "--volume-name", "BLANK35"]);
        Assert.That(c.Success, Is.True, c.Message);

        var result = new DiskInfoCommand().Execute(this.debugContext, [path]);
        Assert.That(result.Success, Is.True, result.Message);

        var output = this.outputWriter.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("TwoImgProDos"));
            Assert.That(output, Does.Not.Contain("5.25\""), "3.5\" disks must not be reported as 5.25\".");
            Assert.That(output, Does.Contain("3.5\" block image"));
            Assert.That(output, Does.Contain("Block count:      1600"));
            Assert.That(output, Does.Contain("Volume name:      BLANK35"));
        });
    }

    /// <summary>
    /// <c>disk-info</c> reports the ProDOS volume name for a 5.25" ProDOS 2MG image.
    /// </summary>
    [Test]
    public void DiskInfo_OnFivePointTwoFiveProDosTwoImg_ReportsVolumeName()
    {
        var path = this.TempPath(".2mg");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "prodos", "--volume-name", "MYVOL"]);
        Assert.That(c.Success, Is.True, c.Message);

        var result = new DiskInfoCommand().Execute(this.debugContext, [path]);
        Assert.That(result.Success, Is.True, result.Message);

        var output = this.outputWriter.ToString();
        Assert.Multiple(() =>
        {
            Assert.That(output, Does.Contain("TwoImgProDos"));
            Assert.That(output, Does.Contain("5.25\" sector image"));
            Assert.That(output, Does.Contain("Volume name:      MYVOL"));
        });
    }

    /// <summary>
    /// <c>disk-info</c> reports the ProDOS volume name for a ProDOS-formatted .hdv image.
    /// </summary>
    [Test]
    public void DiskInfo_OnProDosHdv_ReportsVolumeName()
    {
        var path = this.TempPath(".hdv");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--size", "3.5", "--format", "prodos", "--volume-name", "BIGVOL"]);
        Assert.That(c.Success, Is.True, c.Message);

        var result = new DiskInfoCommand().Execute(this.debugContext, [path]);
        Assert.That(result.Success, Is.True, result.Message);

        var output = this.outputWriter.ToString();
        Assert.That(output, Does.Contain("Volume name:      BIGVOL"));
    }

    /// <summary>Both subcommands return an error when no DiskImageFactory is attached.</summary>
    [Test]
    public void Subcommands_RequireFactoryOnContext()
    {
        var dispatcher = new CommandDispatcher();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var bareContext = new DebugContext(dispatcher, output, error);

        var create = new DiskCreateCommand().Execute(bareContext, [this.TempPath(".dsk")]);
        var info = new DiskInfoCommand().Execute(bareContext, [this.TempPath(".dsk")]);

        Assert.Multiple(() =>
        {
            Assert.That(create.Success, Is.False);
            Assert.That(create.Message, Does.Contain("DiskImageFactory"));
            Assert.That(info.Success, Is.False);
            Assert.That(info.Message, Does.Contain("DiskImageFactory"));
        });
    }

    /// <summary>The parent <c>disk</c> command routes 'create' / 'info' to the matching subcommand.</summary>
    [Test]
    public void DiskCommand_RoutesSubcommands()
    {
        var path = this.TempPath(".po");
        var cmd = new DiskCommand();

        var create = cmd.Execute(this.debugContext, ["create", path, "--format", "prodos", "--volume-name", "ROUTED"]);
        Assert.That(create.Success, Is.True, create.Message);

        var info = cmd.Execute(this.debugContext, ["info", path]);
        Assert.That(info.Success, Is.True, info.Message);

        var output = this.outputWriter.ToString();
        Assert.That(output, Does.Contain("ProDosSectorImage"));
    }

    /// <summary>
    /// <c>--size</c> accepts <c>140K</c> / <c>140k</c> as case-insensitive aliases for
    /// <c>5.25</c> (280 blocks) and <c>800K</c> / <c>800k</c> as aliases for <c>3.5</c>
    /// (1600 blocks).
    /// </summary>
    /// <param name="sizeArg">The case-variant size alias to pass via <c>--size</c>.</param>
    [TestCase("140K")]
    [TestCase("140k")]
    public void DiskCreate_Size140KAlias_Produces525Image(string sizeArg)
    {
        var path = this.TempPath(".po");
        var result = new DiskCreateCommand().Execute(this.debugContext, [path, "--size", sizeArg, "--format", "prodos", "--volume-name", "BLANK"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (Image525AndBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        Assert.That(open.BlockMedia.BlockCount, Is.EqualTo(280));
    }

    /// <summary><c>--size 800K</c> (case-insensitive) produces an 800K (1600 block) image.</summary>
    /// <param name="sizeArg">The case-variant 800K alias to pass via <c>--size</c>.</param>
    [TestCase("800K")]
    [TestCase("800k")]
    public void DiskCreate_Size800KAlias_Produces35Image(string sizeArg)
    {
        var path = this.TempPath(".hdv");
        var result = new DiskCreateCommand().Execute(this.debugContext, [path, "--size", sizeArg, "--format", "prodos", "--volume-name", "BLANK"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (ImageBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        Assert.That(open.Media.BlockCount, Is.EqualTo(1600));
    }

    /// <summary>
    /// <c>--size NM</c> (1..32 megabytes, case-insensitive) selects an N-megabyte ProDOS
    /// volume. <c>32M</c> remains clamped at the ProDOS maximum of 65535 blocks.
    /// </summary>
    /// <param name="sizeArg">The megabyte alias to pass via <c>--size</c>.</param>
    /// <param name="expectedBlocks">The expected total block count of the resulting image.</param>
    [TestCase("1M", 2048)]
    [TestCase("2m", 4096)]
    [TestCase("4M", 8192)]
    [TestCase("8m", 16384)]
    [TestCase("16M", 32768)]
    [TestCase("32M", 65535)]
    [TestCase("32m", 65535)]
    public void DiskCreate_SizeMegabyteAlias_ProducesMatchingBlockCount(string sizeArg, int expectedBlocks)
    {
        var path = this.TempPath(".hdv");
        var result = new DiskCreateCommand().Execute(this.debugContext, [path, "--size", sizeArg, "--format", "prodos", "--volume-name", "BIG"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (ImageBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        Assert.That(open.Media.BlockCount, Is.EqualTo(expectedBlocks));
    }

    /// <summary>An out-of-range megabyte alias is rejected with a helpful error.</summary>
    /// <param name="sizeArg">An out-of-range megabyte alias.</param>
    [TestCase("0M")]
    [TestCase("33M")]
    [TestCase("64M")]
    public void DiskCreate_SizeMegabyteOutOfRange_ReturnsError(string sizeArg)
    {
        var path = this.TempPath(".hdv");
        var result = new DiskCreateCommand().Execute(this.debugContext, [path, "--size", sizeArg, "--format", "prodos", "--volume-name", "BIG"]);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("--size"));
        });
    }

    /// <summary>
    /// A lowercase ProDOS volume name is accepted, the on-disk bytes are stored as
    /// uppercase, and the Tech Note 25 case-bit field at offset 0x1A of the volume
    /// directory header (block-relative offset 0x1E..0x1F) is populated with the
    /// validity flag (bit 15) plus per-character lowercase bits.
    /// </summary>
    [Test]
    public void DiskCreate_LowercaseVolumeName_StoresUppercaseAndCaseBits()
    {
        var path = this.TempPath(".po");
        var result = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "prodos", "--volume-name", "MyVol"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (Image525AndBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        var keyBlock = new byte[512];
        open.BlockMedia.ReadBlock(2, keyBlock);

        var name = System.Text.Encoding.ASCII.GetString(keyBlock, 5, 5);
        var caseField = (ushort)(keyBlock[0x1E] | (keyBlock[0x1F] << 8));

        // Expected bits: M=0, y=1 (bit 13), V=0, o=1 (bit 11), l=1 (bit 10) plus the
        // valid-flag bit 15. That is 0x8000 | (1 << 13) | (1 << 11) | (1 << 10) = 0xAC00.
        const ushort expectedCaseField = 0x8000 | (1 << 13) | (1 << 11) | (1 << 10);

        Assert.Multiple(() =>
        {
            Assert.That(name, Is.EqualTo("MYVOL"), "On-disk volume name bytes must be uppercase.");
            Assert.That(keyBlock[4] & 0x0F, Is.EqualTo(5));
            Assert.That(caseField, Is.EqualTo(expectedCaseField));
        });
    }

    /// <summary>
    /// An all-uppercase volume name leaves the Tech Note 25 case-bit field zero so older
    /// ProDOS tools that pre-date the case-bit convention continue to round-trip.
    /// </summary>
    [Test]
    public void DiskCreate_UppercaseVolumeName_LeavesCaseFieldZero()
    {
        var path = this.TempPath(".po");
        var result = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "prodos", "--volume-name", "PLAIN"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (Image525AndBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        var keyBlock = new byte[512];
        open.BlockMedia.ReadBlock(2, keyBlock);

        var caseField = (ushort)(keyBlock[0x1E] | (keyBlock[0x1F] << 8));
        Assert.That(caseField, Is.EqualTo(0));
    }

    /// <summary>
    /// <c>--only-uppercase</c> rejects a lowercase volume name with a clear error and
    /// does not write the destination file.
    /// </summary>
    [Test]
    public void DiskCreate_OnlyUppercase_RejectsLowercaseVolumeName()
    {
        var path = this.TempPath(".po");
        var result = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "prodos", "--volume-name", "MyVol", "--only-uppercase"]);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("--only-uppercase"));
            Assert.That(File.Exists(path), Is.False, "No file should be written when the name is rejected.");
        });
    }

    /// <summary>
    /// <c>--only-uppercase</c> with an uppercase name still writes a zero case-bit field,
    /// matching the legacy on-disk layout.
    /// </summary>
    [Test]
    public void DiskCreate_OnlyUppercaseWithUppercaseName_LeavesCaseFieldZero()
    {
        var path = this.TempPath(".po");
        var result = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "prodos", "--volume-name", "PLAIN", "--only-uppercase"]);
        Assert.That(result.Success, Is.True, result.Message);

        var open = (Image525AndBlockResult)new DiskImageFactory().Open(path, forceReadOnly: true);
        var keyBlock = new byte[512];
        open.BlockMedia.ReadBlock(2, keyBlock);

        var caseField = (ushort)(keyBlock[0x1E] | (keyBlock[0x1F] << 8));
        Assert.That(caseField, Is.EqualTo(0));
    }

    /// <summary>
    /// <c>disk-info</c> renders the original mixed-case volume name back from the on-disk
    /// uppercase bytes plus the Tech Note 25 case-bit field.
    /// </summary>
    [Test]
    public void DiskInfo_OnLowercaseVolumeName_RestoresMixedCase()
    {
        var path = this.TempPath(".po");
        var c = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "prodos", "--volume-name", "MyVol"]);
        Assert.That(c.Success, Is.True, c.Message);

        var info = new DiskInfoCommand().Execute(this.debugContext, [path]);
        Assert.That(info.Success, Is.True, info.Message);

        var output = this.outputWriter.ToString();
        Assert.That(output, Does.Contain("Volume name:      MyVol"));
    }

    /// <summary>An invalid first character (digit) in the volume name is rejected.</summary>
    [Test]
    public void DiskCreate_VolumeNameStartingWithDigit_ReturnsError()
    {
        var path = this.TempPath(".po");
        var result = new DiskCreateCommand().Execute(this.debugContext, [path, "--format", "prodos", "--volume-name", "1bad"]);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("must start with a letter"));
        });
    }

    private string TempPath(string ext)
    {
        var path = Path.Combine(this.tempRoot, $"img-{Guid.NewGuid():N}{ext}");
        this.tempFiles.Add(path);
        return path;
    }
}