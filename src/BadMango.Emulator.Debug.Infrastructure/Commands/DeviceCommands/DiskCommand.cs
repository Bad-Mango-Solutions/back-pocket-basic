// <copyright file="DiskCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using BadMango.Emulator.Devices;

/// <summary>
/// Top-level <c>disk</c> command that delegates to the <c>create</c> and <c>info</c>
/// subcommand handlers (see <see cref="DiskCreateCommand"/> and <see cref="DiskInfoCommand"/>).
/// </summary>
/// <remarks>
/// <para>
/// The two subcommands are also auto-registered as standalone command handlers
/// (<c>disk-create</c> and <c>disk-info</c>) by the <c>DeviceDebugCommandsModule</c>.
/// This parent exists so that the documented <c>disk create &lt;path&gt;</c> /
/// <c>disk info &lt;path&gt;</c> CLI syntax works out of the box.
/// </para>
/// <para>
/// Neither subcommand requires a running machine; both resolve only the
/// <see cref="Storage.Formats.DiskImageFactory"/> and <see cref="IDebugPathResolver"/>
/// from the supplied <see cref="ICommandContext"/> / <see cref="IDebugContext"/>.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class DiskCommand : CommandHandlerBase, ICommandHelp
{
    private readonly DiskCreateCommand createCommand = new();
    private readonly DiskInfoCommand infoCommand = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskCommand"/> class.
    /// </summary>
    public DiskCommand()
        : base("disk", "Author and inspect disk images (create / info)")
    {
    }

    /// <inheritdoc/>
    public override string Usage => "disk <create|info> [args]";

    /// <inheritdoc/>
    public string Synopsis => "disk <create|info> [args]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Authors and inspects disk images using the same DiskImageFactory that runtime " +
        "controllers use, so authored images round-trip through the same code path. " +
        "Use 'disk create' to write a new fixture image and 'disk info' to report the " +
        "format, geometry and container metadata of an existing image without mounting it.";

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
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "'disk create' writes a new file at the supplied path (or refuses to overwrite an " +
        "existing file). 'disk info' is read-only.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["disk-create", "disk-info"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: disk <create|info> [args]. Try 'help disk' for details.");
        }

        var subcommand = args[0].ToLowerInvariant();
        var subArgs = args.Length > 1 ? args[1..] : [];

        return subcommand switch
        {
            "create" => this.createCommand.Execute(context, subArgs),
            "info" => this.infoCommand.Execute(context, subArgs),
            _ => CommandResult.Error(
                $"Unknown 'disk' subcommand: '{subcommand}'. Use 'create' or 'info'."),
        };
    }
}