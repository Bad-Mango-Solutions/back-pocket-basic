// <copyright file="RunCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Commands;

using System.Globalization;

/// <summary>
/// Runs the CPU until it halts or reaches a limit.
/// </summary>
/// <remarks>
/// <para>
/// Executes instructions continuously until:
/// - The CPU halts (STP or WAI instruction).
/// - The instruction limit is reached.
/// - The cycle limit is reached.
/// - A stop is requested (via StopCommand or externally).
/// </para>
/// <para>
/// Guards against infinite loops by enforcing configurable limits.
/// Optional logging can trace execution for debugging purposes.
/// </para>
/// </remarks>
public sealed class RunCommand : CommandHandlerBase
{
    /// <summary>
    /// Default maximum instructions to execute before stopping.
    /// </summary>
    public const int DefaultInstructionLimit = 1_000_000;

    /// <summary>
    /// Default maximum cycles to execute before stopping.
    /// </summary>
    public const long DefaultCycleLimit = 10_000_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunCommand"/> class.
    /// </summary>
    public RunCommand()
        : base("run", "Run CPU until halt or limit reached")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["g", "go"];

    /// <inheritdoc/>
    public override string Usage => "run [instruction_limit] [--cycles=<limit>] [--trace]";

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

        // Parse options
        var options = ParseOptions(args);

        debugContext.Output.WriteLine($"Running from PC=${debugContext.Cpu.GetPC():X4}...");
        if (options.EnableTrace)
        {
            debugContext.Output.WriteLine("Trace mode enabled.");
        }

        debugContext.Cpu.ClearStopRequest();

        int instructionCount = 0;
        long cycleCount = 0;
        string stopReason = "unknown";

        while (instructionCount < options.InstructionLimit && cycleCount < options.CycleLimit)
        {
            if (debugContext.Cpu.Halted)
            {
                stopReason = "CPU halted";
                break;
            }

            if (debugContext.Cpu.IsStopRequested)
            {
                stopReason = "stop requested";
                debugContext.Cpu.ClearStopRequest();
                break;
            }

            // Optional trace output
            if (options.EnableTrace && debugContext.Disassembler is not null)
            {
                var pc = debugContext.Cpu.GetPC();
                var instruction = debugContext.Disassembler.DisassembleInstruction(pc);
                debugContext.Output.WriteLine($"  ${instruction.Address:X4}: {instruction.FormatBytes(),-12} {instruction.FormatInstruction()}");
            }

            int cycles = debugContext.Cpu.Step();
            instructionCount++;
            cycleCount += cycles;
        }

        // Determine final stop reason if limits were reached
        if (instructionCount >= options.InstructionLimit)
        {
            stopReason = "instruction limit reached";
        }
        else if (cycleCount >= options.CycleLimit)
        {
            stopReason = "cycle limit reached";
        }

        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"Stopped: {stopReason}");
        debugContext.Output.WriteLine($"  Instructions executed: {instructionCount:N0}");
        debugContext.Output.WriteLine($"  Cycles consumed: {cycleCount:N0}");
        debugContext.Output.WriteLine($"  Final PC = ${debugContext.Cpu.GetPC():X4}");

        return CommandResult.Ok();
    }

    private static RunOptions ParseOptions(string[] args)
    {
        var options = new RunOptions
        {
            InstructionLimit = DefaultInstructionLimit,
            CycleLimit = DefaultCycleLimit,
            EnableTrace = false,
        };

        foreach (var arg in args)
        {
            if (arg.Equals("--trace", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableTrace = true;
            }
            else if (arg.StartsWith("--cycles=", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = arg["--cycles=".Length..];
                if (TryParseNumber(valueStr, out long cycleLimit))
                {
                    options.CycleLimit = cycleLimit;
                }
            }
            else if (arg.StartsWith("--instructions=", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = arg["--instructions=".Length..];
                if (TryParseNumber(valueStr, out long instrLimit) && instrLimit <= int.MaxValue)
                {
                    options.InstructionLimit = (int)instrLimit;
                }
            }
            else if (TryParseNumber(arg, out long limit) && limit <= int.MaxValue)
            {
                // Positional argument is instruction limit
                options.InstructionLimit = (int)limit;
            }
        }

        return options;
    }

    private static bool TryParseNumber(string value, out long result)
    {
        // Try hex format first
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("$", StringComparison.Ordinal))
        {
            var hexValue = value.StartsWith("$", StringComparison.Ordinal) ? value[1..] : value[2..];
            return long.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private sealed class RunOptions
    {
        public int InstructionLimit { get; set; }

        public long CycleLimit { get; set; }

        public bool EnableTrace { get; set; }
    }
}