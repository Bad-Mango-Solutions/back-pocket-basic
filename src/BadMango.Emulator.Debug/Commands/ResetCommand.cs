// <copyright file="ResetCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Commands;

/// <summary>
/// Resets the CPU to its initial state.
/// </summary>
/// <remarks>
/// <para>
/// Performs a CPU reset, which:
/// - Sets PC to the reset vector ($FFFC-$FFFD).
/// - Initializes processor status flags.
/// - Clears the halt state.
/// </para>
/// <para>
/// By default, performs a soft reset (CPU only). With the --hard flag,
/// also clears memory to zeros.
/// </para>
/// </remarks>
public sealed class ResetCommand : CommandHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetCommand"/> class.
    /// </summary>
    public ResetCommand()
        : base("reset", "Reset the CPU (soft or hard)")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["rst"];

    /// <inheritdoc/>
    public override string Usage => "reset [--hard]";

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

        bool hardReset = args.Any(arg =>
            arg.Equals("--hard", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-h", StringComparison.OrdinalIgnoreCase));

        if (hardReset && debugContext.Memory is not null)
        {
            debugContext.Memory.Clear();
            debugContext.Output.WriteLine("Memory cleared.");
        }

        debugContext.Cpu.Reset();
        debugContext.Cpu.ClearStopRequest();

        var pc = debugContext.Cpu.GetPC();
        debugContext.Output.WriteLine($"CPU reset. PC = ${pc:X4}");

        if (hardReset)
        {
            debugContext.Output.WriteLine("Hard reset completed.");
        }
        else
        {
            debugContext.Output.WriteLine("Soft reset completed.");
        }

        return CommandResult.Ok();
    }
}