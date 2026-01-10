// <copyright file="BootCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Boots the machine by performing a reset and starting execution.
/// </summary>
/// <remarks>
/// <para>
/// This command combines a reset operation with starting execution.
/// The machine is reset to its initial state, then begins running
/// in the background, allowing the debugger to remain responsive.
/// </para>
/// <para>
/// After boot, use 'pause' to suspend execution, 'resume' to continue,
/// or 'halt' to force a complete stop.
/// </para>
/// </remarks>
public sealed class BootCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BootCommand"/> class.
    /// </summary>
    public BootCommand()
        : base("boot", "Reset and start machine running")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["startup"];

    /// <inheritdoc/>
    public override string Usage => "boot";

    /// <inheritdoc/>
    public string Synopsis => "boot";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Resets the machine to its initial state and immediately starts execution. " +
        "This is equivalent to pressing the power/reset button on a real computer. " +
        "The CPU loads its reset vector and begins executing from the reset handler. " +
        "Execution runs in the background, keeping the debugger responsive.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "boot                    Reset and start the machine",
        "startup                 Alias for boot",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Resets all CPU state (registers, flags, PC) and begins execution. " +
        "Memory and device state may be modified by executed code.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["reset", "pause", "resume", "halt"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Machine is null)
        {
            return CommandResult.Error("No machine attached to debug context.");
        }

        // Start boot asynchronously (fire and forget - the machine runs in background)
        _ = debugContext.Machine.BootAsync();

        return CommandResult.Ok("Machine booted and running. Use 'pause' to suspend execution.");
    }
}