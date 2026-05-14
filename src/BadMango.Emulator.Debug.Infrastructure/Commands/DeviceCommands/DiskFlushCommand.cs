// <copyright file="DiskFlushCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Forces a synchronous flush of any pending writes for a controller drive (PRD §6.5 FR-DC6).
/// </summary>
/// <remarks>
/// <para>
/// Implements <c>disk flush &lt;slot&gt;:&lt;drive&gt;</c>. Forwards to
/// <see cref="IDiskController.Flush"/>. Unlike <see cref="DiskEjectCommand"/>, the medium
/// stays mounted on completion.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskFlushCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskFlushCommand"/> class.
    /// </summary>
    public DiskFlushCommand()
        : base("disk-flush", "Flush a controller drive's pending writes without ejecting")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["diskflush"];

    /// <inheritdoc/>
    public override string Usage => "disk-flush <slot>:<drive>";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Forces a synchronous flush of any pending writes for the indicated drive without " +
        "dismounting the image. Useful before snapshotting the host filesystem or after a " +
        "burst of CPU writes when you want the underlying file to be up to date.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "disk-flush 6:1",
        "disk flush 6:2",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Writes any dirty cached tracks back to the underlying file. The medium stays mounted.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["disk", "disk-list", "disk-insert", "disk-eject"];

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

        var snapshot = controller!.GetDriveSnapshot(driveIndex);
        if (!snapshot.HasMedia)
        {
            return CommandResult.Error($"Slot {slot} drive {drive} is empty; nothing to flush.");
        }

        try
        {
            controller.Flush(driveIndex);
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException or IOException)
        {
            return CommandResult.Error($"Flush failed for slot {slot} drive {drive}: {ex.Message}");
        }

        context.Output.WriteLine($"Flushed slot {slot} drive {drive}.");
        return CommandResult.Ok();
    }
}