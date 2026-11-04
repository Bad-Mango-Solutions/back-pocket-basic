// <copyright file="DiskMonCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using System.Globalization;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Text-mode disk activity monitor (FR-D10 diagnostic surface).
/// </summary>
/// <remarks>
/// <para>
/// Walks every installed <see cref="IDiskController"/> on the active machine, snapshots
/// its activity counters, and renders a compact summary to the console: per-controller
/// soft-switch / phase / motor / cache statistics, plus per-drive head position, byte
/// counts, and the most recently observed sector-address field as seen on the live byte
/// stream the CPU is reading.
/// </para>
/// <para>
/// This is the disk-side analogue of <c>statmon</c>'s CPU surface, and is intended as
/// the first instrument the user reaches for when an "I/O ERROR" or seek failure is
/// suspected. The address-field section in particular ("last vol/trk/sec served")
/// distinguishes between "RWTS never saw a valid address prologue" (encoding /
/// timing problem), "RWTS saw a valid prologue but on the wrong track" (seek problem),
/// and "RWTS saw the right prologue but couldn't decode the data field" (data-field
/// encoding problem) without needing to attach a CPU trace.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskMonCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>Maximum slot number scanned (Apple II expansion slots 1..7).</summary>
    private const int MaxSlot = 7;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskMonCommand"/> class.
    /// </summary>
    public DiskMonCommand()
        : base("diskmon", "Show live disk-controller activity counters")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["disk-mon", "diskstats"];

    /// <inheritdoc/>
    public override string Usage => "diskmon";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Snapshots every installed disk controller and prints its activity counters: " +
        "data-path reads (fresh / stale / settle-suppressed), phase pulses, track " +
        "changes, motor and drive-select transitions, cache loads and flushes, plus, " +
        "for each drive, the most recently observed sector-address field (volume, " +
        "track, sector, checksum-valid) and a full data-field accounting " +
        "(prologues seen, XOR-chain decode successes, checksum / decode / epilogue " +
        "failures, and the address→data byte gap) as decoded from the live byte " +
        "stream served to the CPU. Counters are monotonic since controller " +
        "construction; call repeatedly to observe rate of change.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "diskmon",
        "diskstats",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "None. Read-only snapshot.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["statmon", "disk", "disk-list", "devicemap"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length > 0)
        {
            return CommandResult.Error("diskmon takes no arguments.");
        }

        if (!DiskRuntimeHelpers.TryGetSlotManager(context, out var slotManager, out var error))
        {
            return CommandResult.Error(error!);
        }

        var output = context.Output;
        output.WriteLine();
        output.WriteLine("Disk controller activity:");

        var found = 0;
        for (var slot = 1; slot <= MaxSlot; slot++)
        {
            var card = slotManager!.GetCard(slot);
            if (card is not IDiskController disk)
            {
                continue;
            }

            found++;
            DiskActivitySnapshot snapshot;
            try
            {
                snapshot = disk.GetActivitySnapshot();
            }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
            {
                output.WriteLine();
                output.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "  Slot {0}: {1} (activity unavailable: {2})",
                    slot,
                    card.Name,
                    ex.Message));
                continue;
            }

            output.WriteLine();
            output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "  Slot {0}: {1}",
                slot,
                card.Name));

            output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "    data path : reads={0} fresh={1} stale={2} settle={3} writes={4}",
                snapshot.DataReadCount,
                snapshot.FreshByteCount,
                snapshot.StaleByteCount,
                snapshot.SettleSuppressedReadCount,
                snapshot.DataWriteCount));
            output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "    stepper   : phase-pulses={0} phase-noops={1} track-changes={2}",
                snapshot.PhasePulseCount,
                snapshot.PhaseNoOpCount,
                snapshot.TrackChangeCount));
            output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "    transport : motor-on={0} motor-off={1} drive-select={2}",
                snapshot.MotorOnCount,
                snapshot.MotorOffCount,
                snapshot.DriveSelectCount));
            output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "    cache     : loads={0} flushes={1}",
                snapshot.CacheLoadCount,
                snapshot.CacheFlushCount));

            for (var d = 0; d < disk.DriveCount; d++)
            {
                DriveSnapshot drive;
                try
                {
                    drive = disk.GetDriveSnapshot(d);
                }
                catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
                {
                    output.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "    Drive {0}: <unreadable: {1}>",
                        d + 1,
                        ex.Message));
                    continue;
                }

                var activity = d < snapshot.Drives.Count
                    ? snapshot.Drives[d]
                    : default;

                output.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "    Drive {0}: {1} selected={2} motor={3} qt={4} bytes-on-track={5}",
                    d + 1,
                    drive.HasMedia ? "mounted" : "empty",
                    YesNo(drive.Selected),
                    YesNo(drive.MotorOn),
                    drive.QuarterTrack,
                    activity.BytesServedOnCurrentTrack));
                output.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "      address fields observed: {0} (checksum errors: {1})",
                    activity.ObservedAddressFields,
                    activity.ObservedAddressFieldChecksumErrors));
                if (activity.LastObservedVolume is { } vol
                    && activity.LastObservedTrack is { } trk
                    && activity.LastObservedSector is { } sec)
                {
                    var chk = activity.LastObservedChecksum ?? 0;
                    var valid = activity.LastObservedChecksumValid ?? false;
                    output.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "      last address field: vol=${0:X2} trk=${1:X2} sec=${2:X2} chk=${3:X2} ({4})",
                        vol,
                        trk,
                        sec,
                        chk,
                        valid ? "checksum ok" : "checksum MISMATCH"));
                }
                else
                {
                    output.WriteLine("      last address field: <none observed since reset>");
                }

                output.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "      data fields  : prologues={0} decoded-ok={1} checksum-err={2} decode-err={3} epilogue-mismatch={4}",
                    activity.ObservedDataPrologues,
                    activity.ObservedDataFieldDecodeSuccesses,
                    activity.ObservedDataFieldChecksumErrors,
                    activity.ObservedDataFieldDecodeErrors,
                    activity.ObservedDataFieldEpilogueMismatches));
                if (activity.LastDataPrologueGapBytes is { } lastGap
                    && activity.MinDataPrologueGapBytes is { } minGap
                    && activity.MaxDataPrologueGapBytes is { } maxGap)
                {
                    output.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "      addr→data gap (bytes): last={0} min={1} max={2}",
                        lastGap,
                        minGap,
                        maxGap));
                }
                else
                {
                    output.WriteLine("      addr→data gap (bytes): <no paired gap measured>");
                }
            }
        }

        if (found == 0)
        {
            output.WriteLine();
            output.WriteLine("  No disk controllers installed.");
        }

        output.WriteLine();
        return CommandResult.Ok();
    }

    private static string YesNo(bool value) => value ? "yes" : "no";
}