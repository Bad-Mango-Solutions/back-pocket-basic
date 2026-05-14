// <copyright file="DiskInsertCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Formats;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Mounts a removable disk image into a live controller drive at runtime (PRD §6.5 FR-DC4).
/// </summary>
/// <remarks>
/// <para>
/// Implements <c>disk insert &lt;slot&gt;:&lt;drive&gt; &lt;path&gt; [--write-protect]</c>.
/// Resolves the image path through <see cref="IDebugPathResolver"/> (FR-DC0), opens it
/// through the same <see cref="DiskImageFactory"/> that runtime controllers use, and
/// invokes <see cref="IDiskController.Mount"/>.
/// </para>
/// <para>
/// Fails with a clear error when the targeted slot is empty or holds a non-disk card,
/// when the drive index is out of range, when the image format does not match the
/// controller (e.g. attempting to mount a 3.5" / block-only image on a 5.25"
/// <see cref="IDiskController"/> that consumes <see cref="I525Media"/>), or when the
/// image cannot be opened. The <c>--write-protect</c> flag opens the image read-only so
/// the resulting medium reports <see cref="I525Media.IsReadOnly"/>.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskInsertCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskInsertCommand"/> class.
    /// </summary>
    public DiskInsertCommand()
        : base("disk-insert", "Mount a disk image into a live controller drive")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["diskinsert"];

    /// <inheritdoc/>
    public override string Usage => "disk-insert <slot>:<drive> <path> [--write-protect]";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Opens the supplied image through the same DiskImageFactory used by profile " +
        "loading and mounts it into the indicated controller drive at runtime. The drive " +
        "must be removable (Disk II drives and SmartPort 3.5\" units are; hard-disk units " +
        "are not). Image-format mismatches (e.g. a 3.5\"/block-only image targeting a " +
        "5.25\" Disk II drive) are rejected with a clear error.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new CommandOption(
            "--write-protect",
            null,
            "flag",
            "Open the image read-only so the mounted medium reports write-protect.",
            null),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "disk-insert 6:1 game.dsk",
        "disk-insert 6:2 library://disks/utilities.dsk --write-protect",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Opens the image file and mounts it into the controller. The mount is deferred " +
        "to the next scheduler turn (PRD FR-R1) so the controller never observes a " +
        "half-mounted drive mid-byte.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["disk", "disk-list", "disk-eject", "disk-flush"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: " + this.Usage);
        }

        var target = args[0];
        var rawPath = args[1];
        var writeProtect = false;

        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--write-protect":
                    writeProtect = true;
                    break;
                default:
                    return CommandResult.Error($"Unknown option: '{args[i]}'.");
            }
        }

        if (!DiskRuntimeHelpers.TryParseSlotDrive(target, out var slot, out var drive, out var parseError))
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

        var debugContext = (IDebugContext)context;
        var factory = debugContext.DiskImageFactory;
        if (factory is null)
        {
            return CommandResult.Error("DiskImageFactory not available on debug context.");
        }

        // FR-DC0: resolve every <path> through IDebugPathResolver before touching disk.
        var resolver = debugContext.PathResolver;
        var path = rawPath;
        if (resolver is not null)
        {
            if (!resolver.TryResolve(rawPath, out var resolved))
            {
                return CommandResult.Error($"Cannot resolve path: '{rawPath}'.");
            }

            path = resolved!;
        }

        if (!File.Exists(path))
        {
            return CommandResult.Error($"File not found: '{path}'.");
        }

        DiskImageOpenResult open;
        try
        {
            open = factory.Open(path, forceReadOnly: writeProtect);
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or FileNotFoundException)
        {
            return CommandResult.Error($"Cannot open '{path}': {ex.Message}");
        }

        I525Media? media = open switch
        {
            Image525AndBlockResult both => both.TrackMedia,
            Image525Result trackOnly => trackOnly.Media,
            _ => null,
        };

        if (media is null)
        {
            // The DiskImageOpenResult owns the file handle; release it before bailing out
            // so we don't leak a backend on a rejected mount.
            open.Dispose();
            return CommandResult.Error(
                $"Image '{path}' has no 5.25\" track view (format: {open.Format}); cannot mount on slot {slot} ({controller!.GetType().Name}).");
        }

        try
        {
            controller!.Mount(driveIndex, media, path);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            open.Dispose();
            return CommandResult.Error($"Mount rejected: {ex.Message}");
        }

        // The mounted media references the file backend that 'open' owns; disposing
        // 'open' would close the file handle out from under the controller. Hand 'open'
        // to the debug-context-owned MountedDiskRegistry so its lifetime follows the
        // mount: re-mounting the same drive disposes the prior entry, eject disposes
        // this one, and tearing down the debug context disposes anything still held.
        // Defend against a torn-down registry (e.g. after DebugContext.Dispose) so the
        // user sees a CommandResult.Error rather than an unhandled ObjectDisposedException.
        if (debugContext.MountedDisks.IsDisposed)
        {
            // Roll back the mount we just performed and release the file handle so we
            // don't leave a dangling reference on the controller.
            try
            {
                controller.Eject(driveIndex);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                // Best-effort rollback; the controller error is reported below.
                _ = ex;
            }

            open.Dispose();
            return CommandResult.Error("Mount tracking is unavailable: the debug context has been disposed.");
        }

        try
        {
            debugContext.MountedDisks.Track(slot, driveIndex, open);
        }
        catch (ObjectDisposedException)
        {
            // Lost a teardown race after the IsDisposed check above. Roll back the same
            // way as the explicit IsDisposed branch.
            try
            {
                controller.Eject(driveIndex);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                _ = ex;
            }

            open.Dispose();
            return CommandResult.Error("Mount tracking is unavailable: the debug context was disposed during insert.");
        }

        var summary = writeProtect
            ? $"Inserted '{path}' (write-protected) into slot {slot} drive {drive}."
            : $"Inserted '{path}' into slot {slot} drive {drive}.";
        context.Output.WriteLine(summary);
        return CommandResult.Ok();
    }
}