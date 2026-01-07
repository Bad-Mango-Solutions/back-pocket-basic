// <copyright file="SwitchesCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Lists current soft switch state flags.
/// </summary>
/// <remarks>
/// <para>
/// Displays the current state of soft switches in the system. Soft switches
/// are memory-mapped I/O locations that control various hardware features
/// in Apple II-compatible systems.
/// </para>
/// <para>
/// This command requires a bus with an IOPageDispatcher or composite target
/// that exposes soft switch state.
/// </para>
/// </remarks>
public sealed class SwitchesCommand : CommandHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchesCommand"/> class.
    /// </summary>
    public SwitchesCommand()
        : base("switches", "List current soft switch state flags")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["sw", "softswitch"];

    /// <inheritdoc/>
    public override string Usage => "switches";

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

        debugContext.Output.WriteLine("Soft Switch State:");
        debugContext.Output.WriteLine();

        // Note: The actual soft switch state would be queried from the IOPageDispatcher
        // or a composite target. This is a placeholder showing the command structure.
        // Implementation would require the bus to expose switch state or use reflection
        // to inspect the IOPageDispatcher.
        debugContext.Output.WriteLine("  Switch state information is not available in this configuration.");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine("Note: Soft switch state requires an IOPageDispatcher or compatible");
        debugContext.Output.WriteLine("      composite target that exposes state flags.");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine("Common Apple II soft switches:");
        debugContext.Output.WriteLine("  $C000-$C00F  Keyboard and strobe");
        debugContext.Output.WriteLine("  $C010-$C01F  Keyboard clear and other signals");
        debugContext.Output.WriteLine("  $C050-$C05F  Graphics/text mode switches");
        debugContext.Output.WriteLine("  $C080-$C08F  Language card bank switches");

        return CommandResult.Ok();
    }
}