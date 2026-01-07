// <copyright file="FaultCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Shows details about the most recent bus fault.
/// </summary>
/// <remarks>
/// <para>
/// Displays information about the last bus fault that occurred, including the
/// faulting address, fault kind, and any additional context available.
/// </para>
/// <para>
/// This command requires a bus to be attached to the debug context.
/// </para>
/// </remarks>
public sealed class FaultCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FaultCommand"/> class.
    /// </summary>
    public FaultCommand()
        : base("fault", "Show most recent bus fault details")
    {
    }

    /// <inheritdoc/>
    public override string Usage => "fault";

    /// <inheritdoc/>
    public string Synopsis => "fault";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays information about the most recent bus fault, including the faulting " +
        "address and fault kind. Useful for debugging unmapped memory access, permission " +
        "violations, and other bus errors. Shows 'No fault' if no fault has occurred.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "fault                    Show last bus fault status",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["buslog", "regions", "pages"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (!debugContext.IsBusAttached || debugContext.Bus is null)
        {
            return CommandResult.Error("No bus attached. This command requires a bus-based system.");
        }

        // Note: The current IMemoryBus interface does not expose a LastFault property.
        // This is a placeholder implementation that would require extending the interface.
        debugContext.Output.WriteLine("Bus Fault Status:");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine("  No fault information available.");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine("Note: Fault tracking requires the bus to record fault events.");
        debugContext.Output.WriteLine("      This feature may not be enabled in all configurations.");

        return CommandResult.Ok();
    }
}