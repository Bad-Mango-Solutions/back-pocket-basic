// <copyright file="DiskListCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using System.Globalization;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Lists every installed disk controller and its drives' mount state (PRD §6.5 FR-DC3).
/// </summary>
/// <remarks>
/// <para>
/// Resolves the live <see cref="ISlotManager"/> from the attached
/// <see cref="IDebugContext.Machine"/>, walks slots 1..7 looking for cards that implement
/// <see cref="IDiskController"/>, and prints one row per drive showing slot, drive number,
/// mount state, write-protect, image path, motor / quarter-track and (where available)
/// the medium's geometry.
/// </para>
/// <para>
/// Reports image paths exactly as <see cref="DriveSnapshot.MountedImagePath"/> records them
/// — typically the absolute resolved path supplied at mount time. No <see cref="IDebugPathResolver"/>
/// round-trip is required because the image path was already resolved when the medium was
/// originally inserted (FR-DC0).
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskListCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>Maximum slot number scanned (Apple II expansion slots 1..7).</summary>
    private const int MaxSlot = 7;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskListCommand"/> class.
    /// </summary>
    public DiskListCommand()
        : base("disk-list", "List installed disk controllers and per-drive mount state")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["disklist"];

    /// <inheritdoc/>
    public override string Usage => "disk-list";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Lists every installed disk controller (Disk II today; SmartPort 3.5\" later), one " +
        "row per drive, showing the slot, drive number, mount state, write-protect flag, " +
        "the path of the mounted image, the motor / quarter-track position, and the " +
        "medium geometry where available. Requires a running machine.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "disk-list",
        "disk list",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "None. Snapshot is read-only.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["disk", "disk-insert", "disk-eject", "disk-flush", "devicemap"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length > 0)
        {
            return CommandResult.Error("disk-list takes no arguments.");
        }

        if (!DiskRuntimeHelpers.TryGetSlotManager(context, out var slotManager, out var error))
        {
            return CommandResult.Error(error!);
        }

        var output = context.Output;
        output.WriteLine();
        output.WriteLine("Disk controllers:");

        var found = 0;
        for (var slot = 1; slot <= MaxSlot; slot++)
        {
            var card = slotManager!.GetCard(slot);
            if (card is not IDiskController disk)
            {
                continue;
            }

            found++;
            output.WriteLine();
            output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "  Slot {0}: {1} ({2} drive{3})",
                slot,
                card.Name,
                disk.DriveCount,
                disk.DriveCount == 1 ? string.Empty : "s"));

            for (var d = 0; d < disk.DriveCount; d++)
            {
                DriveSnapshot snap;
                try
                {
                    snap = disk.GetDriveSnapshot(d);
                }
                catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
                {
                    output.WriteLine($"    Drive {d + 1}: <unreadable: {ex.Message}>");
                    continue;
                }

                if (!snap.HasMedia)
                {
                    output.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "    Drive {0}: empty (selected={1}, motor={2}, quarter-track={3})",
                        d + 1,
                        YesNo(snap.Selected),
                        YesNo(snap.MotorOn),
                        snap.QuarterTrack));
                    continue;
                }

                output.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "    Drive {0}: mounted '{1}'",
                    d + 1,
                    snap.MountedImagePath ?? "<unknown>"));
                output.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "      write-protect={0} selected={1} motor={2} quarter-track={3}",
                    YesNo(snap.WriteProtect),
                    YesNo(snap.Selected),
                    YesNo(snap.MotorOn),
                    snap.QuarterTrack));
                if (snap.Geometry is { } geom)
                {
                    output.WriteLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "      geometry: {0} tracks × {1} sectors × {2} bytes ({3}, {4} bytes total)",
                        geom.TrackCount,
                        geom.SectorsPerTrack,
                        geom.BytesPerSector,
                        geom.SectorOrder,
                        geom.TotalBytes));
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