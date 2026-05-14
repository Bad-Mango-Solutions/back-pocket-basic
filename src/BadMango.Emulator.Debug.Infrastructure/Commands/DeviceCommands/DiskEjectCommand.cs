// <copyright file="DiskEjectCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Flushes and dismounts a disk image from a live controller drive (PRD §6.5 FR-DC5).
/// </summary>
/// <remarks>
/// <para>
/// Implements <c>disk eject &lt;slot&gt;:&lt;drive&gt;</c>. Forwards to
/// <see cref="IDiskController.Eject"/>, which flushes any dirty state first and rejects
/// the eject if the flush fails (PRD FR-R2). When the controller returns
/// <see langword="false"/> the command surfaces a clear error rather than reporting
/// success.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskEjectCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskEjectCommand"/> class.
    /// </summary>
    public DiskEjectCommand()
        : base("disk-eject", "Flush and dismount a disk image from a controller drive")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["diskeject"];

    /// <inheritdoc/>
    public override string Usage => "disk-eject <slot>:<drive>";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Forces an immediate flush of any pending writes for the indicated drive and then " +
        "dismounts the image. Fails with a clear error when the drive is empty, when the " +
        "underlying flush fails (in which case the image stays mounted, per PRD FR-R2), " +
        "or when the unit is non-removable (e.g. a hard-disk volume).";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "disk-eject 6:1",
        "disk eject 6:2",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Flushes pending writes to disk and dismounts the medium. The actual swap is " +
        "deferred to the next scheduler turn (PRD FR-R1).";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["disk", "disk-list", "disk-insert", "disk-flush"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length != 1)
        {
            return CommandResult.Error("Usage: " + this.Usage);
        }

        if (!DiskRuntimeHelpers.TryParseSlotDrive(args[0], out var slot, out var drive, out var parseError))
        {
            return CommandResult.Error(parseError!);
        }

        if (!DiskRuntimeHelpers.TryGetSlotManager(context, out var slotManager, out var managerError))
        {
            return CommandResult.Error(managerError!);
        }

        if (!DiskRuntimeHelpers.TryGetController(slotManager!, slot, drive, out var controller, out var driveIndex, out var ctlError))
        {
            return CommandResult.Error(ctlError!);
        }

        bool ejected;
        try
        {
            ejected = controller!.Eject(driveIndex);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
        {
            return CommandResult.Error($"Eject failed for slot {slot} drive {drive}: {ex.Message}");
        }

        if (!ejected)
        {
            // Eject returns false when the drive was empty OR when the flush failed and
            // the eject was rejected per FR-R2. Distinguish via the snapshot.
            var snapshot = controller.GetDriveSnapshot(driveIndex);
            return snapshot.HasMedia
                ? CommandResult.Error($"Eject of slot {slot} drive {drive} rejected: flush failed (image remains mounted).")
                : CommandResult.Error($"Slot {slot} drive {drive} is already empty.");
        }

        context.Output.WriteLine($"Ejected slot {slot} drive {drive}.");
        return CommandResult.Ok();
    }
}