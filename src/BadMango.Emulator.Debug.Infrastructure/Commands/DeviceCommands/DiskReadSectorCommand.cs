// <copyright file="DiskReadSectorCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using System.Buffers;
using System.Globalization;
using System.Text;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Gcr;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Decodes and dumps a single sector from a live-mounted 5.25" drive.
/// </summary>
/// <remarks>
/// <para>
/// Implements <c>disk read-sector &lt;slot&gt;:&lt;drive&gt; &lt;track&gt; &lt;sector&gt;
/// [--logical]</c>. Reads the requested track's raw nibble stream through
/// <see cref="IDiskController.GetMedia"/>, runs it through <see cref="GcrEncoder.DecodeTrack"/>,
/// and prints the requested sector as a hex+ASCII dump.
/// </para>
/// <para>
/// By default <c>&lt;sector&gt;</c> is interpreted as a physical sector index (0..15) in
/// on-disk order, matching the result of <see cref="GcrEncoder.DecodeTrack"/>. Passing
/// <c>--logical</c> reinterprets <c>&lt;sector&gt;</c> as a logical sector index in the
/// medium's <see cref="DiskGeometry.SectorOrder"/> (DOS 3.3 or ProDOS) and translates it
/// to physical via <see cref="SectorSkew.LogicalToPhysical"/> before pulling the bytes.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskReadSectorCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskReadSectorCommand"/> class.
    /// </summary>
    public DiskReadSectorCommand()
        : base("disk-read-sector", "Decode and dump a single sector from a mounted 5.25\" drive")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["diskreadsector"];

    /// <inheritdoc/>
    public override string Usage => "disk-read-sector <slot>:<drive> [<track> <sector>] [--logical] [--compare] [--volume]";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Reads the requested track's raw nibble stream, runs it through the standard " +
        "6-and-2 GCR decoder, and prints the decoded contents of one sector as a 16-byte-" +
        "per-line hex+ASCII dump. By default <sector> is a physical sector index (0..15) " +
        "matching the on-disk track layout; pass --logical to interpret it as a DOS 3.3 / " +
        "ProDOS logical sector and let the medium's geometry translate it to physical. " +
        "Sectors that the decoder could not recover (bad checksum, missing prologue) are " +
        "reported as an error instead of silently printing zeros. " +
        "Pass --compare to also read the same sector directly from the mounted image bytes " +
        "and compare the decoded result; mismatches are reported per offset. Pass --volume " +
        "to compare every sector on every track and report a summary of decode failures and " +
        "mismatches; <track> and <sector> are ignored in --volume mode.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new CommandOption(
            "--logical",
            null,
            "flag",
            "Interpret <sector> as a logical sector index in the medium's sector order.",
            null),
        new CommandOption(
            "--compare",
            null,
            "flag",
            "Also read the same sector directly from the mounted image and compare against the decoded bytes.",
            null),
        new CommandOption(
            "--volume",
            null,
            "flag",
            "Compare every sector on every track and report a summary; ignores <track> and <sector>.",
            null),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "disk read-sector 6:1 0 0",
        "disk read-sector 6:1 17 0 --logical",
        "disk read-sector 6:1 0 0 --compare",
        "disk read-sector 6:1 --volume",
        "disk-read-sector 6:1 1 5",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Read-only: reads the medium's nibble buffer and decodes one sector. Does not " +
        "advance the controller's head position, motor state, or any drive-side latches.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } =
        ["disk", "disk-list", "disk-dump-track"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length < 1)
        {
            return CommandResult.Error("Usage: " + this.Usage);
        }

        if (!DiskRuntimeHelpers.TryParseSlotDrive(args[0], out var slot, out var drive, out var parseError))
        {
            return CommandResult.Error(parseError!);
        }

        var logical = false;
        var compare = false;
        var volume = false;
        var positional = new List<string>();
        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--logical":
                    logical = true;
                    break;
                case "--compare":
                    compare = true;
                    break;
                case "--volume":
                    volume = true;
                    break;
                default:
                    if (args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        return CommandResult.Error($"Unknown option: '{args[i]}'.");
                    }

                    positional.Add(args[i]);
                    break;
            }
        }

        int track = 0;
        int sector = 0;
        if (!volume)
        {
            if (positional.Count < 2)
            {
                return CommandResult.Error("Usage: " + this.Usage);
            }

            if (!int.TryParse(positional[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out track) || track < 0)
            {
                return CommandResult.Error($"Track must be a non-negative integer; got '{positional[0]}'.");
            }

            if (!int.TryParse(positional[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out sector) || sector < 0)
            {
                return CommandResult.Error($"Sector must be a non-negative integer; got '{positional[1]}'.");
            }
        }

        if (!DiskRuntimeHelpers.TryGetSlotManager(context, out var slotManager, out var managerError))
        {
            return CommandResult.Error(managerError!);
        }

        if (!DiskRuntimeHelpers.TryGetController(slotManager!, slot, drive, out var controller, out var driveIndex, out var ctlError))
        {
            return CommandResult.Error(ctlError!);
        }

        var media = controller!.GetMedia(driveIndex);
        if (media is null)
        {
            return CommandResult.Error($"Slot {slot} drive {drive} is empty; nothing to read.");
        }

        var geometry = media.Geometry;
        var trackLength = media.OptimalTrackLength;
        if (trackLength != GcrEncoder.StandardTrackLength)
        {
            return CommandResult.Error(
                $"Cannot decode sectors: medium reports OptimalTrackLength={trackLength} but decoder requires {GcrEncoder.StandardTrackLength}.");
        }

        SectorImageMedia? sectorImage = null;
        if (compare || volume)
        {
            if (!DiskRuntimeHelpers.TryGetSectorImage(context, slot, driveIndex, out sectorImage, out var imgError))
            {
                return CommandResult.Error(imgError!);
            }
        }

        if (volume)
        {
            return ExecuteVolumeCompare(context, slot, drive, media, sectorImage!, geometry, trackLength);
        }

        if (track >= geometry.TrackCount)
        {
            return CommandResult.Error(
                $"Track must be in 0..{geometry.TrackCount - 1}; got {track}.");
        }

        if (sector >= geometry.SectorsPerTrack)
        {
            return CommandResult.Error(
                $"Sector must be in 0..{geometry.SectorsPerTrack - 1}; got {sector}.");
        }

        var physicalSector = logical
            ? SectorSkew.LogicalToPhysical(geometry.SectorOrder, sector)
            : sector;

        var nibbleBuffer = ArrayPool<byte>.Shared.Rent(trackLength);
        var sectorBuffer = ArrayPool<byte>.Shared.Rent(geometry.SectorsPerTrack * GcrEncoder.BytesPerSector);
        try
        {
            var nibbles = nibbleBuffer.AsSpan(0, trackLength);
            media.ReadTrack(track * 4, nibbles);

            var sectors = sectorBuffer.AsSpan(0, geometry.SectorsPerTrack * GcrEncoder.BytesPerSector);
            var foundMask = GcrEncoder.DecodeTrack(nibbles, sectors);

            if ((foundMask & (1 << physicalSector)) == 0)
            {
                return CommandResult.Error(
                    logical
                        ? $"Failed to decode logical sector {sector} (physical {physicalSector}) on track {track}: missing or corrupt on the nibble stream."
                        : $"Failed to decode physical sector {sector} on track {track}: missing or corrupt on the nibble stream.");
            }

            var orderLabel = geometry.SectorOrder.ToString();
            var header = logical
                ? $"Slot {slot} drive {drive}: track {track}, logical sector {sector} (physical {physicalSector}, {orderLabel}), {GcrEncoder.BytesPerSector} bytes"
                : $"Slot {slot} drive {drive}: track {track}, physical sector {sector}, {GcrEncoder.BytesPerSector} bytes";
            context.Output.WriteLine(header);

            var decoded = sectors.Slice(physicalSector * GcrEncoder.BytesPerSector, GcrEncoder.BytesPerSector);
            HexDump(context.Output, decoded);

            if (compare)
            {
                Span<byte> imageBytes = stackalloc byte[GcrEncoder.BytesPerSector];
                sectorImage!.ReadSectorPhysical(track, physicalSector, imageBytes);
                var mismatches = CountMismatches(decoded, imageBytes);
                if (mismatches == 0)
                {
                    context.Output.WriteLine($"Compare: OK ({GcrEncoder.BytesPerSector}/{GcrEncoder.BytesPerSector} bytes match image).");
                }
                else
                {
                    context.Output.WriteLine($"Compare: MISMATCH ({mismatches} of {GcrEncoder.BytesPerSector} bytes differ).");
                    WriteMismatchDetails(context.Output, decoded, imageBytes);
                    return CommandResult.Error("Decoded sector does not match the mounted image.");
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(nibbleBuffer);
            ArrayPool<byte>.Shared.Return(sectorBuffer);
        }

        return CommandResult.Ok();
    }

    private static CommandResult ExecuteVolumeCompare(
        ICommandContext context,
        int slot,
        int drive,
        I525Media media,
        SectorImageMedia sectorImage,
        DiskGeometry geometry,
        int trackLength)
    {
        var nibbleBuffer = ArrayPool<byte>.Shared.Rent(trackLength);
        var sectorBuffer = ArrayPool<byte>.Shared.Rent(geometry.SectorsPerTrack * GcrEncoder.BytesPerSector);
        try
        {
            context.Output.WriteLine(
                $"Slot {slot} drive {drive}: comparing all {geometry.TrackCount} tracks x {geometry.SectorsPerTrack} sectors against mounted image...");

            int decodeFailures = 0;
            int sectorMismatches = 0;
            int totalSectors = 0;
            Span<byte> imageBytes = stackalloc byte[GcrEncoder.BytesPerSector];

            for (var track = 0; track < geometry.TrackCount; track++)
            {
                var nibbles = nibbleBuffer.AsSpan(0, trackLength);
                media.ReadTrack(track * 4, nibbles);

                var sectors = sectorBuffer.AsSpan(0, geometry.SectorsPerTrack * GcrEncoder.BytesPerSector);
                var foundMask = GcrEncoder.DecodeTrack(nibbles, sectors);

                for (var phys = 0; phys < geometry.SectorsPerTrack; phys++)
                {
                    totalSectors++;
                    if ((foundMask & (1 << phys)) == 0)
                    {
                        decodeFailures++;
                        context.Output.WriteLine($"  T{track:D2} S{phys:D2}: decode failure (missing/corrupt).");
                        continue;
                    }

                    sectorImage.ReadSectorPhysical(track, phys, imageBytes);
                    var decoded = sectors.Slice(phys * GcrEncoder.BytesPerSector, GcrEncoder.BytesPerSector);
                    var mismatches = CountMismatches(decoded, imageBytes);
                    if (mismatches != 0)
                    {
                        sectorMismatches++;
                        context.Output.WriteLine($"  T{track:D2} S{phys:D2}: {mismatches} byte(s) differ.");
                    }
                }
            }

            var clean = totalSectors - decodeFailures - sectorMismatches;
            context.Output.WriteLine(
                $"Summary: {clean}/{totalSectors} sectors clean; {decodeFailures} decode failures; {sectorMismatches} mismatches.");

            if (decodeFailures != 0 || sectorMismatches != 0)
            {
                return CommandResult.Error("Volume compare detected decode failures or mismatches.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(nibbleBuffer);
            ArrayPool<byte>.Shared.Return(sectorBuffer);
        }

        return CommandResult.Ok();
    }

    private static int CountMismatches(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var n = Math.Min(a.Length, b.Length);
        var count = 0;
        for (var i = 0; i < n; i++)
        {
            if (a[i] != b[i])
            {
                count++;
            }
        }

        return count;
    }

    private static void WriteMismatchDetails(TextWriter writer, ReadOnlySpan<byte> decoded, ReadOnlySpan<byte> image)
    {
        var n = Math.Min(decoded.Length, image.Length);
        var shown = 0;
        const int maxShown = 16;
        for (var i = 0; i < n && shown < maxShown; i++)
        {
            if (decoded[i] != image[i])
            {
                writer.WriteLine(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"  @{i:X4}: decoded={decoded[i]:X2} image={image[i]:X2}"));
                shown++;
            }
        }

        if (shown == maxShown)
        {
            writer.WriteLine("  ... (additional mismatches suppressed)");
        }
    }

    private static void HexDump(TextWriter writer, ReadOnlySpan<byte> data)
    {
        const int bytesPerLine = 16;
        var line = new StringBuilder(80);
        for (var i = 0; i < data.Length; i += bytesPerLine)
        {
            var slice = data.Slice(i, Math.Min(bytesPerLine, data.Length - i));
            line.Clear();
            line.Append(CultureInfo.InvariantCulture, $"{i:X4}: ");
            for (var j = 0; j < bytesPerLine; j++)
            {
                if (j < slice.Length)
                {
                    line.Append(CultureInfo.InvariantCulture, $"{slice[j]:X2} ");
                }
                else
                {
                    line.Append("   ");
                }
            }

            line.Append(' ');
            for (var j = 0; j < slice.Length; j++)
            {
                var b = slice[j];
                line.Append(b is >= 0x20 and < 0x7F ? (char)b : '.');
            }

            writer.WriteLine(line.ToString());
        }
    }
}