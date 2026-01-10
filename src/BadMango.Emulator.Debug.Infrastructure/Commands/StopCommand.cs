// <copyright file="StopCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Requests the CPU to stop execution at the next instruction boundary.
/// </summary>
/// <remarks>
/// This command sets a flag that causes the CPU to stop at the next
/// safe point (between instructions). It is typically used in conjunction
/// with the 'run' command to interrupt a running program.
/// </remarks>
public sealed class StopCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StopCommand"/> class.
    /// </summary>
    public StopCommand()
        : base("stop", "Request CPU to stop execution")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["break"];

    /// <inheritdoc/>
    public override string Usage => "stop";

    /// <inheritdoc/>
    public string Synopsis => "stop";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Sets a flag that causes the CPU to stop at the next instruction boundary. " +
        "Typically used to interrupt a running program started with 'run'. The CPU " +
        "will complete the current instruction before stopping, ensuring a clean halt.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "stop                    Request the CPU to stop",
        "break                   Alias for stop",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Sets the CPU stop request flag. The CPU will halt at the next instruction boundary.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["run", "step", "reset"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Cpu is null)
        {
            return CommandResult.Error("No CPU attached to debug context.");
        }

        debugContext.Cpu.RequestStop();

        return CommandResult.Ok("Stop requested. CPU will halt at next instruction boundary.");
    }
}