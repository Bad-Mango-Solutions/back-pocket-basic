// <copyright file="DiskCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using BadMango.Emulator.Devices;

/// <summary>
/// Top-level <c>disk</c> command that delegates to the offline (<c>create</c>, <c>info</c>)
/// and runtime (<c>list</c>, <c>insert</c>, <c>eject</c>, <c>flush</c>) subcommand handlers.
/// </summary>
/// <remarks>
/// <para>
/// Each subcommand is also auto-registered as a standalone command handler
/// (<c>disk-create</c>, <c>disk-info</c>, <c>disk-list</c>, <c>disk-insert</c>,
/// <c>disk-eject</c>, <c>disk-flush</c>) by the <c>DeviceDebugCommandsModule</c>. This
/// parent exists so that the documented <c>disk &lt;subcommand&gt; ...</c> CLI syntax
/// works out of the box.
/// </para>
/// <para>
/// <c>create</c> and <c>info</c> do not require a running machine and resolve only the
/// <see cref="Storage.Formats.DiskImageFactory"/> and <see cref="IDebugPathResolver"/>
/// from the supplied context. <c>list</c>, <c>insert</c>, <c>eject</c>, and <c>flush</c>
/// operate on the live <see cref="Bus.Interfaces.ISlotManager"/> exposed via
/// <see cref="IDebugContext.Machine"/> and therefore require a running machine.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskCommand : CommandHandlerBase, ICommandHelp
{
    private readonly DiskCreateCommand createCommand = new();
    private readonly DiskInfoCommand infoCommand = new();
    private readonly DiskListCommand listCommand = new();
    private readonly DiskInsertCommand insertCommand = new();
    private readonly DiskEjectCommand ejectCommand = new();
    private readonly DiskFlushCommand flushCommand = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskCommand"/> class.
    /// </summary>
    public DiskCommand()
        : base("disk", "Author, inspect, and live-mount disk images (create / info / list / insert / eject / flush)")
    {
    }

    /// <inheritdoc/>
    public override string Usage => "disk <create|info|list|insert|eject|flush> [args]";

    /// <inheritdoc/>
    public string Synopsis => this.Usage;

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Authors, inspects, and live-mounts disk images using the same DiskImageFactory " +
        "that runtime controllers use, so authored images round-trip through the same code " +
        "path. Use 'disk create' to write a new fixture image, 'disk info' to report the " +
        "format/geometry/container metadata of an existing image without mounting it, " +
        "'disk list' to print every installed controller and per-drive mount state, and " +
        "'disk insert' / 'disk eject' / 'disk flush' to swap removable media at runtime.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "disk create blank.dsk",
        "disk create blank.po --format prodos --volume-name BLANK",
        "disk create boot.dsk --format dos33 --bootable master.dsk",
        "disk create huge.hdv --size 32M --format prodos --volume-name BIG",
        "disk info game.2mg",
        "disk list",
        "disk insert 6:1 game.dsk",
        "disk insert 6:2 library://disks/utilities.dsk --write-protect",
        "disk eject 6:1",
        "disk flush 6:2",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "'disk create' writes a new file at the supplied path (or refuses to overwrite an " +
        "existing file). 'disk info' and 'disk list' are read-only. 'disk insert' opens an " +
        "image and mounts it on the targeted controller. 'disk eject' and 'disk flush' " +
        "write any dirty cached tracks back to the underlying file.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } =
        ["disk-create", "disk-info", "disk-list", "disk-insert", "disk-eject", "disk-flush"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            return CommandResult.Error(
                "Usage: disk <create|info|list|insert|eject|flush> [args]. Try 'help disk' for details.");
        }

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Length > 1 ? args[1..] : [];

        return subcommand switch
        {
            "create" => this.createCommand.Execute(context, subArgs),
            "info" => this.infoCommand.Execute(context, subArgs),
            "list" => this.listCommand.Execute(context, subArgs),
            "insert" => this.insertCommand.Execute(context, subArgs),
            "eject" => this.ejectCommand.Execute(context, subArgs),
            "flush" => this.flushCommand.Execute(context, subArgs),
            _ => CommandResult.Error(
                $"Unknown 'disk' subcommand: '{subcommand}'. " +
                "Use 'create', 'info', 'list', 'insert', 'eject', or 'flush'."),
        };
    }
}