// <copyright file="DiskDumpTrackCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using System.Buffers;
using System.Globalization;
using System.Text;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Dumps the raw GCR nibble stream for a single track of a live-mounted 5.25" drive.
/// </summary>
/// <remarks>
/// <para>
/// Implements <c>disk dump-track &lt;slot&gt;:&lt;drive&gt; [--track N | --quarter-track Q]
/// [--length N] [--offset N]</c>. Reads the medium directly via
/// <see cref="IDiskController.GetMedia"/> so the dump reflects exactly the nibble buffer
/// the controller's read path would observe.
/// </para>
/// <para>
/// When neither <c>--track</c> nor <c>--quarter-track</c> is supplied the command targets
/// the drive's current head position from <see cref="IDiskController.GetDriveSnapshot"/>.
/// Output is a standard 16-byte-per-line hex+ASCII dump, addresses anchored at 0.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskDumpTrackCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskDumpTrackCommand"/> class.
    /// </summary>
    public DiskDumpTrackCommand()
        : base("disk-dump-track", "Dump the raw GCR nibble stream for a track of a mounted 5.25\" drive")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["diskdumptrack"];

    /// <inheritdoc/>
    public override string Usage =>
        "disk-dump-track <slot>:<drive> [--track N | --quarter-track Q] [--offset N] [--length N]";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Reads the requested track's raw nibble stream directly from the live medium and " +
        "prints it as a 16-byte-per-line hex+ASCII dump. Useful for confirming that the " +
        "controller would observe a valid address prologue (D5 AA 96) at the current head " +
        "position, or for inspecting an arbitrary track without stepping the head. " +
        "If neither --track nor --quarter-track is supplied, the drive's current " +
        "quarter-track position is used.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new CommandOption(
            "--track",
            null,
            "int",
            "Whole-track index to dump (0..TrackCount-1); converted to quarter-track N*4.",
            null),
        new CommandOption(
            "--quarter-track",
            null,
            "int",
            "Quarter-track index to dump (0..QuarterTrackCount-1); overrides --track.",
            null),
        new CommandOption(
            "--offset",
            null,
            "int",
            "Byte offset within the track at which to start the dump (default 0).",
            "0"),
        new CommandOption(
            "--length",
            null,
            "int",
            "Maximum number of bytes to print (default: full track from --offset).",
            null),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "disk dump-track 6:1",
        "disk dump-track 6:1 --track 0",
        "disk dump-track 6:1 --track 0 --length 64",
        "disk dump-track 6:1 --quarter-track 2 --offset 256 --length 128",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Read-only: reads the medium's nibble buffer. Does not advance the controller's " +
        "head position, motor state, or any drive-side latches.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } =
        ["disk", "disk-list", "disk-read-sector"];

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

        int? track = null;
        int? quarterTrack = null;
        var offset = 0;
        int? length = null;

        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--track":
                    if (!TryReadIntArg(args, ref i, arg, out var t, out var err))
                    {
                        return CommandResult.Error(err!);
                    }

                    track = t;
                    break;
                case "--quarter-track":
                    if (!TryReadIntArg(args, ref i, arg, out var q, out err))
                    {
                        return CommandResult.Error(err!);
                    }

                    quarterTrack = q;
                    break;
                case "--offset":
                    if (!TryReadIntArg(args, ref i, arg, out var o, out err))
                    {
                        return CommandResult.Error(err!);
                    }

                    offset = o;
                    break;
                case "--length":
                    if (!TryReadIntArg(args, ref i, arg, out var l, out err))
                    {
                        return CommandResult.Error(err!);
                    }

                    length = l;
                    break;
                default:
                    return CommandResult.Error($"Unknown option: '{arg}'.");
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
            return CommandResult.Error($"Slot {slot} drive {drive} is empty; nothing to dump.");
        }

        var geometry = media.Geometry;
        var snapshot = controller.GetDriveSnapshot(driveIndex);

        int qt;
        if (quarterTrack is int qtSupplied)
        {
            qt = qtSupplied;
        }
        else if (track is int tSupplied)
        {
            qt = tSupplied * 4;
        }
        else
        {
            qt = snapshot.QuarterTrack;
        }

        if (qt < 0 || qt >= geometry.QuarterTrackCount)
        {
            return CommandResult.Error(
                $"Quarter-track must be in 0..{geometry.QuarterTrackCount - 1}; got {qt}.");
        }

        var trackLength = media.OptimalTrackLength;
        if (offset < 0 || offset > trackLength)
        {
            return CommandResult.Error($"Offset must be in 0..{trackLength}; got {offset}.");
        }

        var available = trackLength - offset;
        var bytesToPrint = length is int requested
            ? Math.Min(Math.Max(requested, 0), available)
            : available;

        var rented = ArrayPool<byte>.Shared.Rent(trackLength);
        try
        {
            var buffer = rented.AsSpan(0, trackLength);
            media.ReadTrack(qt, buffer);

            var wholeTrack = qt / 4;
            var quarterRemainder = qt % 4;
            var header = quarterRemainder == 0
                ? $"Slot {slot} drive {drive}: track {wholeTrack} (quarter-track {qt}), {bytesToPrint}/{trackLength} bytes from offset 0x{offset:X4}"
                : $"Slot {slot} drive {drive}: quarter-track {qt} (between tracks {wholeTrack} and {wholeTrack + 1}), {bytesToPrint}/{trackLength} bytes from offset 0x{offset:X4}";
            context.Output.WriteLine(header);

            if (bytesToPrint > 0)
            {
                HexDump(context.Output, buffer.Slice(offset, bytesToPrint), offset);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }

        return CommandResult.Ok();
    }

    private static bool TryReadIntArg(string[] args, ref int i, string optionName, out int value, out string? error)
    {
        value = 0;
        error = null;
        if (i + 1 >= args.Length)
        {
            error = $"Option '{optionName}' requires a value.";
            return false;
        }

        var raw = args[++i];
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            error = $"Option '{optionName}' expected an integer; got '{raw}'.";
            return false;
        }

        return true;
    }

    private static void HexDump(TextWriter writer, ReadOnlySpan<byte> data, int baseAddress)
    {
        const int bytesPerLine = 16;
        var line = new StringBuilder(80);
        for (var i = 0; i < data.Length; i += bytesPerLine)
        {
            var slice = data.Slice(i, Math.Min(bytesPerLine, data.Length - i));
            line.Clear();
            line.Append(CultureInfo.InvariantCulture, $"{baseAddress + i:X4}: ");
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