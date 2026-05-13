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

    private string TempPath(string ext)
    {
        var path = Path.Combine(this.tempRoot, $"img-{Guid.NewGuid():N}{ext}");
        this.tempFiles.Add(path);
        return path;
    }
}