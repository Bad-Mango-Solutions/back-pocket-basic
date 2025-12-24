// <copyright file="StepCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Commands;

using System.Globalization;

/// <summary>
/// Executes one or more CPU instructions in single-step mode.
/// </summary>
/// <remarks>
/// When invoked without arguments, executes a single instruction.
/// When invoked with a count, executes that many instructions.
/// Displays disassembly and register state after each step.
/// </remarks>
public sealed class StepCommand : CommandHandlerBase
{
    private const int DefaultStepCount = 1;
    private const int MaxStepCount = 10000;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepCommand"/> class.
    /// </summary>
    public StepCommand()
        : base("step", "Execute one or more CPU instructions")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["s", "si"];

    /// <inheritdoc/>
    public override string Usage => "step [count]";

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

        if (debugContext.Cpu.Halted)
        {
            return CommandResult.Error("CPU is halted. Use 'reset' to restart.");
        }

        int stepCount = DefaultStepCount;
        if (args.Length > 0)
        {
            if (!TryParseNumber(args[0], out stepCount))
            {
                return CommandResult.Error($"Invalid step count: '{args[0]}'. Expected a positive integer.");
            }

            if (stepCount < 1)
            {
                return CommandResult.Error("Step count must be at least 1.");
            }

            if (stepCount > MaxStepCount)
            {
                return CommandResult.Error($"Step count exceeds maximum ({MaxStepCount}). Use 'run' for larger counts.");
            }
        }

        int totalCycles = 0;
        for (int i = 0; i < stepCount; i++)
        {
            if (debugContext.Cpu.Halted)
            {
                debugContext.Output.WriteLine($"CPU halted after {i} instruction(s).");
                break;
            }

            // Display instruction before execution
            if (debugContext.Disassembler is not null)
            {
                var pc = debugContext.Cpu.GetPC();
                var instruction = debugContext.Disassembler.DisassembleInstruction(pc);
                debugContext.Output.WriteLine($"${instruction.Address:X4}: {instruction.FormatBytes(),-12} {instruction.FormatInstruction()}");
            }

            int cycles = debugContext.Cpu.Step();
            totalCycles += cycles;

            if (debugContext.Cpu.IsStopRequested)
            {
                debugContext.Output.WriteLine($"Stop requested after {i + 1} instruction(s).");
                debugContext.Cpu.ClearStopRequest();
                break;
            }
        }

        debugContext.Output.WriteLine($"Executed {stepCount} instruction(s), {totalCycles} cycle(s).");

        // Show final PC location
        if (debugContext.Cpu is not null)
        {
            debugContext.Output.WriteLine($"PC = ${debugContext.Cpu.GetPC():X4}");
        }

        return CommandResult.Ok();
    }

    private static bool TryParseNumber(string value, out int result)
    {
        // Try hex format first
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("$", StringComparison.Ordinal))
        {
            var hexValue = value.StartsWith("$", StringComparison.Ordinal) ? value[1..] : value[2..];
            return int.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}