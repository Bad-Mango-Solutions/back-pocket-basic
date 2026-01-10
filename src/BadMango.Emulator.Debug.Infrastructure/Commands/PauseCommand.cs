// <copyright file="PauseCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Pauses machine execution without fully stopping.
/// </summary>
/// <remarks>
/// <para>
/// This command suspends machine execution while preserving the CPU
/// and scheduler state. The machine can be resumed from this point
/// using the 'resume' command.
/// </para>
/// <para>
/// Unlike 'halt' which forces a hard stop requiring reset, 'pause'
/// allows execution to continue from where it was suspended.
/// </para>
/// </remarks>
public sealed class PauseCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PauseCommand"/> class.
    /// </summary>
    public PauseCommand()
        : base("pause", "Suspend machine execution")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["suspend", "freeze"];

    /// <inheritdoc/>
    public override string Usage => "pause";

    /// <inheritdoc/>
    public string Synopsis => "pause";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Suspends machine execution at the next instruction boundary. " +
        "The CPU and scheduler state are preserved, allowing execution " +
        "to be resumed with 'resume'. This is useful for examining state " +
        "while the machine is running without losing the current context.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "pause                   Suspend execution",
        "suspend                 Alias for pause",
        "freeze                  Alias for pause",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Suspends CPU execution. State is preserved and can be examined or modified.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["resume", "boot", "halt", "stop"];

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

        var stateBefore = debugContext.Machine.State;
        debugContext.Machine.Pause();
        var stateAfter = debugContext.Machine.State;

        if (stateBefore == stateAfter)
        {
            return CommandResult.Ok($"Machine state unchanged: {stateAfter}");
        }

        return CommandResult.Ok($"Machine paused at PC=${debugContext.Cpu?.GetPC():X4}. Use 'resume' to continue.");
    }
}