// <copyright file="HaltCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Halts the machine completely, equivalent to the STP instruction effect.
/// </summary>
/// <remarks>
/// <para>
/// This command forces the CPU into a halted state similar to executing
/// the STP (Stop) instruction. Unlike 'pause' which preserves state for
/// resumption, 'halt' requires a 'reset' to restart the machine.
/// </para>
/// <para>
/// This is useful when you want to ensure the machine is completely stopped
/// and cannot be accidentally resumed without an explicit reset.
/// </para>
/// </remarks>
public sealed class HaltCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HaltCommand"/> class.
    /// </summary>
    public HaltCommand()
        : base("halt", "Stop machine completely (requires reset to restart)")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = [];

    /// <inheritdoc/>
    public override string Usage => "halt";

    /// <inheritdoc/>
    public string Synopsis => "halt";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Forces the CPU into a halted state, equivalent to executing the STP " +
        "(Stop) instruction. The machine cannot be resumed; a 'reset' is required " +
        "to restart execution. This provides a hard stop that prevents any further " +
        "code execution until the machine is explicitly reset.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "halt                    Force complete machine stop",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Forces the CPU into STP halt state. Requires 'reset' to restart.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["reset", "pause", "stop"];

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

        debugContext.Machine.Halt();

        return CommandResult.Ok("Machine halted. Use 'reset' to restart.");
    }
}