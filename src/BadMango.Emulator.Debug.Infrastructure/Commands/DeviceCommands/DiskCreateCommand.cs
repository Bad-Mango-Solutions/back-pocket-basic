// <copyright file="DiskCreateCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using System.Buffers.Binary;
using System.Globalization;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Authors a new fixture disk image at the requested path.
/// </summary>
/// <remarks>
/// <para>
/// Implements <c>disk create &lt;path&gt; [--size 5.25|3.5|32M|&lt;blocks&gt;]
/// [--format raw|dos33|prodos] [--bootable &lt;bootimage&gt;] [--volume-name &lt;name&gt;]
/// [--volume-number &lt;n&gt;]</c>.
/// </para>
/// <para>
/// The container is selected from the file extension (<c>.dsk</c> / <c>.do</c> /
/// <c>.po</c> / <c>.2mg</c>/<c>.2img</c> / <c>.hdv</c>). The output is round-trippable
/// through <see cref="Storage.Formats.DiskImageFactory"/>.
/// </para>
/// <para>
/// The <c>raw</c> format produces a zero-filled image at the requested geometry.
/// The <c>dos33</c> format additionally writes a DOS 3.3 VTOC and an empty catalog
/// track on a 35-track 5.25" image. The <c>prodos</c> format writes a ProDOS volume
/// directory plus the volume bitmap sized for the chosen geometry.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskCreateCommand : CommandHandlerBase, ICommandHelp
{
    private const int FivePointTwoFiveBytes = 35 * 16 * 256; // 143360
    private const int BlockSize = 512;
    private const int FivePointTwoFiveBlocks = FivePointTwoFiveBytes / BlockSize; // 280
    private const int ThreePointFiveBlocks = 1600; // 800K
    private const int ThirtyTwoMBlocks = 65535; // ProDOS volume max
    private const int TwoImgHeaderLength = 64;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskCreateCommand"/> class.
    /// </summary>
    public DiskCreateCommand()
        : base("disk-create", "Create a new fixture disk image")
    {
    }

    private enum ContainerKind
    {
        Unsupported,
        DskAmbiguous,
        Do,
        Po,
        TwoImg,
        Hdv,
    }

    private enum FormatKind
    {
        Raw,
        Dos33,
        ProDos,
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["diskcreate"];

    /// <inheritdoc/>
    public override string Usage =>
        "disk-create <path> [--size 5.25|3.5|32M|<blocks>] [--format raw|dos33|prodos] " +
        "[--bootable <bootimage>] [--volume-name <name>] [--volume-number <n>]";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Creates a new disk image at the supplied path. The container (raw sector image, " +
        "2MG, or HDV) is chosen by file extension. The default format is 'raw' (zero-filled). " +
        "'dos33' additionally writes a DOS 3.3 VTOC and an empty catalog track on a 35-track " +
        "5.25\" image; 'prodos' writes a ProDOS volume directory plus a sized volume bitmap. " +
        "When '--bootable' is supplied, the boot blocks of the supplied source image are " +
        "copied into the new image; until a Disk II controller is implemented, --bootable " +
        "is the only way to produce a bootable output.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new CommandOption(
            "--size",
            null,
            "5.25|3.5|32M|<blocks>",
            "Image geometry. '5.25' = 280 blocks (140K), '3.5' = 1600 blocks (800K), '32M' = 65535 blocks. Defaults to 5.25 for sector containers and 32M for .hdv.",
            null),
        new CommandOption(
            "--format",
            null,
            "raw|dos33|prodos",
            "Filesystem to author. 'raw' zero-fills only; 'dos33' writes a VTOC + empty catalog on a 35-track image; 'prodos' writes a volume directory and bitmap.",
            "raw"),
        new CommandOption(
            "--bootable",
            null,
            "path",
            "Path to a source disk image whose boot region (track 0 / blocks 0..1) is copied into the new image. Required for bootable output until a Disk II controller exists.",
            null),
        new CommandOption(
            "--volume-name",
            null,
            "string",
            "ProDOS volume name (1..15 chars, A-Z / 0-9 / '.'). Ignored for 'raw' and 'dos33'.",
            "BLANK"),
        new CommandOption(
            "--volume-number",
            null,
            "int",
            "DOS 3.3 volume number (1..254). Ignored for 'raw' and 'prodos'.",
            "254"),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "disk-create blank.dsk",
        "disk-create blank.do --format dos33 --volume-number 254",
        "disk-create blank.po --format prodos --volume-name BLANK",
        "disk-create boot.dsk --format dos33 --bootable master.dsk",
        "disk-create big.hdv --size 32M --format prodos --volume-name BIG",
        "disk-create wrapped.2mg --format prodos --volume-name BLANK",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Writes a new file at the supplied path. Refuses to overwrite an existing file.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["disk", "disk-info"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            return CommandResult.Error("Path required. " + this.Usage);
        }

        DiskImageFactory? factory = (context as IDebugContext)?.DiskImageFactory;
        if (factory is null)
        {
            return CommandResult.Error("DiskImageFactory not available on debug context.");
        }

        string rawPath = args[0];
        string? sizeArg = null;
        string formatArg = "raw";
        string? bootableArg = null;
        string? volumeName = null;
        int? volumeNumber = null;

        for (int i = 1; i < args.Length; i++)
        {
            var opt = args[i];
            switch (opt.ToLowerInvariant())
            {
                case "--size":
                    if (++i >= args.Length)
                    {
                        return CommandResult.Error("--size requires a value.");
                    }

                    sizeArg = args[i];
                    break;
                case "--format":
                    if (++i >= args.Length)
                    {
                        return CommandResult.Error("--format requires a value.");
                    }

                    formatArg = args[i].ToLowerInvariant();
                    break;
                case "--bootable":
                    if (++i >= args.Length)
                    {
                        return CommandResult.Error("--bootable requires a path argument.");
                    }

                    bootableArg = args[i];
                    break;
                case "--volume-name":
                    if (++i >= args.Length)
                    {
                        return CommandResult.Error("--volume-name requires a value.");
                    }

                    volumeName = args[i];
                    break;
                case "--volume-number":
                    if (++i >= args.Length)
                    {
                        return CommandResult.Error("--volume-number requires a value.");
                    }

                    if (!int.TryParse(args[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var vn) || vn < 1 || vn > 254)
                    {
                        return CommandResult.Error($"--volume-number must be an integer in 1..254; got '{args[i]}'.");
                    }

                    volumeNumber = vn;
                    break;
                default:
                    return CommandResult.Error($"Unknown option: '{opt}'.");
            }
        }

        var resolved = ResolvePath((context as IDebugContext)?.PathResolver, rawPath);
        if (resolved.Error is not null)
        {
            return CommandResult.Error(resolved.Error);
        }

        var path = resolved.Path!;

        if (File.Exists(path))
        {
            return CommandResult.Error($"Refusing to overwrite existing file: '{path}'.");
        }

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var container = ContainerFromExtension(ext);
        if (container == ContainerKind.Unsupported)
        {
            return CommandResult.Error($"Unsupported extension '{ext}'. Use .dsk / .do / .po / .2mg / .2img / .hdv.");
        }

        var formatResult = ParseFormat(formatArg);
        if (formatResult.Error is not null)
        {
            return CommandResult.Error(formatResult.Error);
        }

        var format = formatResult.Format;

        var sizeResult = ResolveSize(sizeArg, container, format);
        if (sizeResult.Error is not null)
        {
            return CommandResult.Error(sizeResult.Error);
        }

        // Validate format vs container compatibility.
        var compat = ValidateCompatibility(container, format, sizeResult.Geometry, sizeResult.BlockCount);
        if (compat is not null)
        {
            return CommandResult.Error(compat);
        }

        // Optionally read bootable source.
        byte[]? bootData = null;
        if (bootableArg is not null)
        {
            var boot = ResolvePath((context as IDebugContext)?.PathResolver, bootableArg);
            if (boot.Error is not null)
            {
                return CommandResult.Error(boot.Error);
            }

            try
            {
                bootData = ReadBootData(factory, boot.Path!, container);
            }
            catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException or NotSupportedException or InvalidOperationException)
            {
                return CommandResult.Error($"Cannot read boot image '{bootableArg}': {ex.Message}");
            }
        }

        // Author the image bytes in memory then write atomically.
        byte[] bytes;
        try
        {
            bytes = AuthorImage(container, format, sizeResult.Geometry, sizeResult.BlockCount, volumeName, volumeNumber, bootData);
        }
        catch (ArgumentException ex)
        {
            return CommandResult.Error(ex.Message);
        }

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Atomic write: stage the bytes in a sibling temp file then rename into place,
            // so a crash or full disk mid-write leaves the target file absent rather than
            // partially-written. Using the same directory keeps the rename on one volume.
            var stagingDir = string.IsNullOrEmpty(dir) ? "." : dir;
            var tempPath = Path.Combine(stagingDir, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllBytes(tempPath, bytes);
                File.Move(tempPath, path);
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch (IOException)
                    {
                        // Best effort.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Best effort.
                    }
                }

                throw;
            }
        }
        catch (IOException ex)
        {
            return CommandResult.Error($"Error writing '{path}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return CommandResult.Error($"Access denied writing '{path}': {ex.Message}");
        }

        var summary = $"Created '{path}' ({bytes.Length} bytes, {DescribeFormat(container, format, sizeResult.BlockCount)}).";
        context.Output.WriteLine(summary);
        return CommandResult.Ok();
    }

    private static (string? Path, string? Error) ResolvePath(IDebugPathResolver? resolver, string raw)
    {
        if (resolver is null)
        {
            return (raw, null);
        }

        if (!resolver.TryResolve(raw, out var resolved))
        {
            return (null, $"Cannot resolve path: '{raw}'.");
        }

        return (resolved, null);
    }

    private static ContainerKind ContainerFromExtension(string ext) => ext switch
    {
        ".dsk" => ContainerKind.DskAmbiguous,
        ".do" => ContainerKind.Do,
        ".po" => ContainerKind.Po,
        ".2mg" or ".2img" => ContainerKind.TwoImg,
        ".hdv" => ContainerKind.Hdv,
        _ => ContainerKind.Unsupported,
    };

    private static (FormatKind Format, string? Error) ParseFormat(string s) => s switch
    {
        "raw" => (FormatKind.Raw, null),
        "dos33" => (FormatKind.Dos33, null),
        "prodos" => (FormatKind.ProDos, null),
        _ => (FormatKind.Raw, $"--format must be one of 'raw', 'dos33', 'prodos'; got '{s}'."),
    };

    private static (DiskGeometry? Geometry, int BlockCount, string? Error) ResolveSize(string? sizeArg, ContainerKind container, FormatKind format)
    {
        // Default size by container if none given.
        sizeArg ??= container switch
        {
            ContainerKind.DskAmbiguous or ContainerKind.Do or ContainerKind.Po => "5.25",
            ContainerKind.TwoImg => "5.25",
            ContainerKind.Hdv => "32M",
            _ => "5.25",
        };

        switch (sizeArg.ToLowerInvariant())
        {
            case "5.25":
                return (DiskGeometry.Standard525Dos, FivePointTwoFiveBlocks, null);
            case "3.5":
                return (null, ThreePointFiveBlocks, null);
            case "32m":
                return (null, ThirtyTwoMBlocks, null);
            default:
                if (int.TryParse(sizeArg, NumberStyles.Integer, CultureInfo.InvariantCulture, out var blocks)
                    && blocks > 0
                    && blocks <= ThirtyTwoMBlocks)
                {
                    return (null, blocks, null);
                }

                return (null, 0, $"--size must be one of '5.25', '3.5', '32M' or a block count in 1..{ThirtyTwoMBlocks}; got '{sizeArg}'.");
        }
    }

    private static string? ValidateCompatibility(ContainerKind container, FormatKind format, DiskGeometry? geom, int blockCount)
    {
        var is525 = geom is not null;

        switch (container)
        {
            case ContainerKind.Do:
                if (!is525)
                {
                    return ".do images must be 5.25\" sized (use --size 5.25).";
                }

                if (format == FormatKind.ProDos)
                {
                    return ".do is DOS-ordered; --format prodos is not supported on .do (use .po or .2mg).";
                }

                break;
            case ContainerKind.Po:
                if (!is525)
                {
                    return ".po images must be 5.25\" sized (use --size 5.25).";
                }

                if (format == FormatKind.Dos33)
                {
                    return ".po is ProDOS-ordered; --format dos33 is not supported on .po (use .do or .2mg).";
                }

                break;
            case ContainerKind.DskAmbiguous:
                if (!is525)
                {
                    return ".dsk images must be 5.25\" sized (use --size 5.25).";
                }

                break;
            case ContainerKind.Hdv:
                if (is525)
                {
                    return ".hdv is a block image; --size 5.25 is not supported on .hdv.";
                }

                if (format == FormatKind.Dos33)
                {
                    return ".hdv is a block image; --format dos33 is not supported on .hdv.";
                }

                break;
            case ContainerKind.TwoImg:
                if (format == FormatKind.Dos33 && !is525)
                {
                    return "2MG with --format dos33 must be 5.25\" sized (use --size 5.25).";
                }

                break;
        }

        return null;
    }

    private static byte[] AuthorImage(
        ContainerKind container,
        FormatKind format,
        DiskGeometry? geom,
        int blockCount,
        string? volumeName,
        int? volumeNumber,
        byte[]? bootData)
    {
        // Compute payload size and the on-disk sector order (only meaningful for 5.25"
        // sector containers; .hdv is purely a block image).
        SectorOrder? sectorOrder;
        int payloadLength;

        if (geom is not null)
        {
            payloadLength = FivePointTwoFiveBytes;
            sectorOrder = container switch
            {
                ContainerKind.Do => SectorOrder.Dos33,
                ContainerKind.Po => SectorOrder.ProDos,
                ContainerKind.DskAmbiguous => format == FormatKind.ProDos ? SectorOrder.ProDos : SectorOrder.Dos33,
                ContainerKind.TwoImg => format == FormatKind.ProDos ? SectorOrder.ProDos : SectorOrder.Dos33,
                _ => SectorOrder.Dos33,
            };
        }
        else
        {
            payloadLength = checked(blockCount * BlockSize);
            sectorOrder = container == ContainerKind.TwoImg ? SectorOrder.ProDos : null;
        }

        var payload = new byte[payloadLength];

        // Copy boot data (if any) before stamping format-specific metadata, so the
        // boot data lives in physical track 0 / blocks 0..1 and the metadata still
        // overrides the ProDOS or DOS structures it owns.
        if (bootData is not null)
        {
            var copyLen = Math.Min(bootData.Length, payload.Length);
            Buffer.BlockCopy(bootData, 0, payload, 0, copyLen);
        }

        switch (format)
        {
            case FormatKind.Raw:
                break;
            case FormatKind.Dos33:
                if (geom is null || sectorOrder != SectorOrder.Dos33)
                {
                    throw new ArgumentException("--format dos33 requires a 5.25\" DOS-ordered container.");
                }

                WriteDos33Structures(payload, volumeNumber ?? 254, bootData is not null);
                break;
            case FormatKind.ProDos:
                if (sectorOrder == SectorOrder.Dos33)
                {
                    throw new ArgumentException("--format prodos requires a ProDOS-ordered container.");
                }

                WriteProDosStructures(payload, volumeName ?? "BLANK", geom is null ? blockCount : FivePointTwoFiveBlocks, sectorOrder);
                break;
        }

        if (container == ContainerKind.TwoImg)
        {
            return WrapTwoImg(payload, format, sectorOrder, volumeNumber);
        }

        return payload;
    }

    private static void WriteDos33Structures(byte[] dosOrderedImage, int volumeNumber, bool bootable)
    {
        // VTOC at track 17, sector 0 (DOS-logical 0 == file offset 17 * 16 * 256 = 69632).
        const int track = 17;
        const int sector = 0;
        var vtocOffset = ((track * 16) + sector) * 256;
        var vtoc = dosOrderedImage.AsSpan(vtocOffset, 256);
        vtoc.Clear();
        vtoc[0x00] = 0x04;
        vtoc[0x01] = 0x11; // first catalog T
        vtoc[0x02] = 0x0F; // first catalog S
        vtoc[0x03] = 0x03; // DOS release
        vtoc[0x06] = (byte)volumeNumber;
        vtoc[0x27] = 0x7A;
        vtoc[0x30] = 18; // last allocated track (just past catalog)
        vtoc[0x31] = 1; // direction
        vtoc[0x34] = 0x23; // 35 tracks
        vtoc[0x35] = 0x10; // 16 sectors
        vtoc[0x36] = 0x00;
        vtoc[0x37] = 0x01; // 256 bytes / sector

        // Track allocation bitmap at offset 0x38; 4 bytes per track (high byte = sectors 15..8, low byte = sectors 7..0; 1 = free).
        for (var t = 0; t < 35; t++)
        {
            var bm = vtoc.Slice(0x38 + (t * 4), 4);
            bool fullyUsed;
            if (t == track)
            {
                fullyUsed = true; // VTOC + catalog track
            }
            else if (bootable && t <= 2)
            {
                fullyUsed = true; // boot loader + DOS image
            }
            else
            {
                fullyUsed = false;
            }

            bm[0] = (byte)(fullyUsed ? 0x00 : 0xFF);
            bm[1] = (byte)(fullyUsed ? 0x00 : 0xFF);
            bm[2] = 0;
            bm[3] = 0;
        }

        // Empty catalog chain at track 17, sectors 15 -> 14 -> ... -> 1 -> 0 (end).
        for (var s = 15; s >= 1; s--)
        {
            var off = ((track * 16) + s) * 256;
            var cat = dosOrderedImage.AsSpan(off, 256);
            cat.Clear();
            cat[0x00] = 0x00;
            cat[0x01] = (byte)track; // next track
            cat[0x02] = (byte)(s - 1); // next sector
        }

        // Catalog sector 0 is unused (end of chain marker is the previous sector pointing to 17/0
        // with the following: T=0, S=0 in real INIT layouts; we leave it as zero).
    }

    private static void WriteProDosStructures(byte[] payload, string volumeName, int totalBlocks, SectorOrder? sectorOrder)
    {
        if (volumeName.Length is < 1 or > 15)
        {
            throw new ArgumentException("ProDOS volume name must be 1..15 characters.", nameof(volumeName));
        }

        foreach (var c in volumeName)
        {
            if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.'))
            {
                throw new ArgumentException(
                    $"ProDOS volume name contains invalid character '{c}'. Must contain only A-Z, 0-9, '.'.",
                    nameof(volumeName));
            }
        }

        // Standard ProDOS layout: blocks 2..5 = volume directory (4 blocks), block 6+ = volume bitmap.
        const int firstDirBlock = 2;
        const int dirBlockCount = 4;
        var bitmapPointer = firstDirBlock + dirBlockCount; // block 6
        var bitmapBlockCount = (totalBlocks + (BlockSize * 8) - 1) / (BlockSize * 8);

        // Volume directory key block (block 2).
        var keyBlock = new byte[BlockSize];
        keyBlock[0] = 0; // prev = 0
        keyBlock[1] = 0;
        keyBlock[2] = (byte)(firstDirBlock + 1); // next = block 3
        keyBlock[3] = 0;

        // Volume header entry (offset 4..)
        keyBlock[4] = (byte)(0xF0 | volumeName.Length); // storage_type=F (vol header) | name_length
        for (var i = 0; i < volumeName.Length; i++)
        {
            keyBlock[5 + i] = (byte)volumeName[i];
        }

        // 0x14..0x1B reserved (zeros)
        // 0x1C..0x1F creation date+time (zeros)
        keyBlock[0x20] = 0; // ProDOS version
        keyBlock[0x21] = 0; // min ProDOS version
        keyBlock[0x22] = 0xC3; // access (read/write/destroy/rename)
        keyBlock[0x23] = 0x27; // entry length = 39
        keyBlock[0x24] = 0x0D; // entries per block = 13
        keyBlock[0x25] = 0; // file count (LSB)
        keyBlock[0x26] = 0;
        keyBlock[0x27] = (byte)(bitmapPointer & 0xFF);
        keyBlock[0x28] = (byte)((bitmapPointer >> 8) & 0xFF);
        keyBlock[0x29] = (byte)(totalBlocks & 0xFF);
        keyBlock[0x2A] = (byte)((totalBlocks >> 8) & 0xFF);

        WriteProDosBlock(payload, firstDirBlock, keyBlock, sectorOrder);

        // Subsequent directory blocks (3, 4, 5): chained, prev/next pointers only.
        for (var i = 1; i < dirBlockCount; i++)
        {
            var blk = new byte[BlockSize];
            var prev = firstDirBlock + i - 1;
            var next = (i == dirBlockCount - 1) ? 0 : firstDirBlock + i + 1;
            blk[0] = (byte)(prev & 0xFF);
            blk[1] = (byte)((prev >> 8) & 0xFF);
            blk[2] = (byte)(next & 0xFF);
            blk[3] = (byte)((next >> 8) & 0xFF);
            WriteProDosBlock(payload, firstDirBlock + i, blk, sectorOrder);
        }

        // Volume bitmap: each bit = one block (1=free, 0=used). Bit 7 of byte 0 is block 0.
        var bitmap = new byte[bitmapBlockCount * BlockSize];
        for (var b = 0; b < totalBlocks; b++)
        {
            var byteIndex = b / 8;
            var bitIndex = 7 - (b % 8);
            bitmap[byteIndex] |= (byte)(1 << bitIndex);
        }

        // Mark the boot blocks (0, 1), the directory blocks (2..5), and the bitmap blocks themselves as used.
        for (var b = 0; b < bitmapPointer + bitmapBlockCount && b < totalBlocks; b++)
        {
            var byteIndex = b / 8;
            var bitIndex = 7 - (b % 8);
            bitmap[byteIndex] &= (byte)~(1 << bitIndex);
        }

        for (var i = 0; i < bitmapBlockCount; i++)
        {
            var blk = new byte[BlockSize];
            Buffer.BlockCopy(bitmap, i * BlockSize, blk, 0, BlockSize);
            WriteProDosBlock(payload, bitmapPointer + i, blk, sectorOrder);
        }
    }

    /// <summary>
    /// Writes a 512-byte ProDOS block into the payload at the appropriate file offset
    /// for the underlying sector order.
    /// </summary>
    private static void WriteProDosBlock(byte[] payload, int blockIndex, byte[] block, SectorOrder? sectorOrder)
    {
        if (sectorOrder is null)
        {
            // Pure block image (.hdv): blocks are contiguous from the start of the payload.
            Buffer.BlockCopy(block, 0, payload, blockIndex * BlockSize, BlockSize);
            return;
        }

        // 5.25" sector image: each ProDOS block = two ProDOS-logical sectors within one track
        // (sectors 2N and 2N+1, where N = blockIndex % 8). The file offset depends on the
        // backing image's sector order.
        var track = blockIndex / 8;
        var lowerLogical = (blockIndex % 8) * 2;
        var upperLogical = lowerLogical + 1;

        WriteProDosLogicalSector(payload, track, lowerLogical, block.AsSpan(0, 256), sectorOrder.Value);
        WriteProDosLogicalSector(payload, track, upperLogical, block.AsSpan(256, 256), sectorOrder.Value);
    }

    private static void WriteProDosLogicalSector(byte[] payload, int track, int proDosLogical, ReadOnlySpan<byte> source, SectorOrder backing)
    {
        // Convert ProDOS-logical sector to physical, then to backing-image logical for the file offset.
        // Uses the public Storage.Gcr.SectorSkew helpers so the skew tables are not duplicated.
        var physical = SectorSkew.LogicalToPhysical(SectorOrder.ProDos, proDosLogical);
        var backingLogical = SectorSkew.PhysicalToLogical(backing, physical);

        var offset = ((track * 16) + backingLogical) * 256;
        source.CopyTo(payload.AsSpan(offset, 256));
    }

    private static byte[] WrapTwoImg(byte[] payload, FormatKind format, SectorOrder? sectorOrder, int? volumeNumber)
    {
        var image = new byte[TwoImgHeaderLength + payload.Length];
        var span = image.AsSpan();
        span[0] = (byte)'2';
        span[1] = (byte)'I';
        span[2] = (byte)'M';
        span[3] = (byte)'G';
        span[4] = (byte)'B';
        span[5] = (byte)'M';
        span[6] = (byte)'S';
        span[7] = (byte)'L';
        BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(8, 2), TwoImgHeaderLength);

        // Format code: 0 = DOS sector order, 1 = ProDOS sector order, 2 = nibble.
        // For raw 2MG we pick based on sector order (defaulting to ProDOS for block-only payloads).
        int twoImgFormat = (format == FormatKind.Dos33 || sectorOrder == SectorOrder.Dos33) ? 0 : 1;
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0x0C, 4), twoImgFormat);

        // Flags: optionally embed the DOS volume number for DOS payloads.
        uint flags = 0;
        if (twoImgFormat == 0 && volumeNumber is int vn)
        {
            flags |= 0x00000100u | (uint)(vn & 0xFF);
        }

        BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(0x10, 4), flags);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0x18, 4), TwoImgHeaderLength);
        BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0x1C, 4), payload.Length);
        payload.CopyTo(image.AsSpan(TwoImgHeaderLength));
        return image;
    }

    private static byte[] ReadBootData(DiskImageFactory factory, string sourcePath, ContainerKind destContainer)
    {
        // Dispose the open result before returning so the boot source file's handle is
        // released; otherwise the caller (and end users) cannot subsequently rename or
        // delete that file until the process exits.
        using var open = factory.Open(sourcePath, forceReadOnly: true);

        // Decide how many bytes of boot to copy based on the destination container.
        // 5.25" containers cover an entire track (4096 bytes = 16 sectors); pure block
        // containers (.hdv) just need blocks 0 and 1 (1024 bytes).
        var copyBlocks = (destContainer == ContainerKind.Hdv) ? 2 : 8;
        var data = new byte[copyBlocks * BlockSize];

        switch (open)
        {
            case Image525AndBlockResult both:
                {
                    var blockMedia = both.BlockMedia;
                    var n = Math.Min(copyBlocks, blockMedia.BlockCount);
                    for (var b = 0; b < n; b++)
                    {
                        blockMedia.ReadBlock(b, data.AsSpan(b * BlockSize, BlockSize));
                    }

                    return data;
                }

            case ImageBlockResult blockOnly:
                {
                    var n = Math.Min(copyBlocks, blockOnly.Media.BlockCount);
                    for (var b = 0; b < n; b++)
                    {
                        blockOnly.Media.ReadBlock(b, data.AsSpan(b * BlockSize, BlockSize));
                    }

                    return data;
                }

            case Image525Result:
                throw new InvalidOperationException(
                    "Boot images presented as nibble-only (.nib / 2MG-nibble) are not supported as --bootable sources.");

            default:
                throw new InvalidOperationException("Unrecognised boot image result.");
        }
    }

    private static string DescribeFormat(ContainerKind container, FormatKind format, int blockCount)
    {
        var ext = container switch
        {
            ContainerKind.DskAmbiguous => "dsk",
            ContainerKind.Do => "do",
            ContainerKind.Po => "po",
            ContainerKind.TwoImg => "2mg",
            ContainerKind.Hdv => "hdv",
            _ => "?",
        };
        return $"container={ext}, format={format.ToString().ToLowerInvariant()}, blocks={blockCount}";
    }
}