// <copyright file="ResumeCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Resumes machine execution from a paused state.
/// </summary>
/// <remarks>
/// <para>
/// This command continues machine execution from where it was suspended
/// by the 'pause' command. The CPU and scheduler resume from their
/// preserved state.
/// </para>
/// <para>
/// If the machine is not in a paused state, this command has no effect.
/// </para>
/// </remarks>
public sealed class ResumeCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResumeCommand"/> class.
    /// </summary>
    public ResumeCommand()
        : base("resume", "Continue machine execution from paused state")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["continue", "cont"];

    /// <inheritdoc/>
    public override string Usage => "resume";

    /// <inheritdoc/>
    public string Synopsis => "resume";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Resumes machine execution from a paused state. The CPU continues " +
        "executing from where it was suspended, with all state preserved. " +
        "Execution runs in the background, keeping the debugger responsive. " +
        "Use 'pause' to suspend again or 'halt' to force a complete stop.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "resume                  Continue execution",
        "continue                Alias for resume",
        "cont                    Alias for resume",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Resumes CPU execution. Memory and device state may be modified by executed code.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["pause", "boot", "halt", "run"];

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

        if (debugContext.Machine.State != Bus.MachineState.Paused)
        {
            return CommandResult.Error(
                $"Machine is not paused (current state: {debugContext.Machine.State}). " +
                "Use 'boot' to start or 'run' for single-threaded execution.");
        }

        // Resume asynchronously (fire and forget - the machine runs in background)
        _ = debugContext.Machine.ResumeAsync();

        return CommandResult.Ok($"Resumed execution from PC=${debugContext.Cpu?.GetPC():X4}.");
    }
}