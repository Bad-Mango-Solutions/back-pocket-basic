// <copyright file="RunCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

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
public sealed class RunCommand : ExecutionCommandBase
{
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
    public override string Usage => "run [instruction_limit] [--cycles=<limit>] [--trace] [--trace-file=<path>] [--trace-buffer]";

    /// <inheritdoc/>
    public override string Synopsis => "run [limit] [options]";

    /// <inheritdoc/>
    public override string DetailedDescription =>
        "Executes CPU instructions continuously until the processor halts (STP or WAI), " +
        "a breakpoint is hit, a stop is requested, or configured limits are reached. " +
        "Use instruction and cycle limits to prevent infinite loops. " +
        "Optional tracing captures execution history for debugging.";

    /// <inheritdoc/>
    public override IReadOnlyList<CommandOption> Options { get; } =
    [
        new("--trace", "-t", "flag", "Enable instruction tracing", "off"),
        new("--trace-buffer", "-tb", "flag", "Buffer trace output instead of streaming", "off"),
        new("--trace-file", null, "path", "Write trace output to specified file", null),
        new("--trace-last", null, "int", "Show only last N trace records", "100"),
        new("--trace-buffer-size", null, "int", "Maximum buffered trace records", "10000"),
        new("--cycles", null, "int", "Maximum cycles to execute", "10000000"),
        new("--instructions", null, "int", "Maximum instructions to execute", "1000000"),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples { get; } =
    [
        "run                          Execute until halt or default limits",
        "run 1000                     Execute up to 1000 instructions",
        "run --trace                  Execute with instruction tracing",
        "run --trace-buffer --trace-last=50   Buffer and show last 50 instructions",
    ];

    /// <inheritdoc/>
    public override string? SideEffects =>
        "Modifies CPU state (PC, registers, flags). May modify memory and trigger " +
        "I/O device state changes depending on executed code.";

    /// <inheritdoc/>
    public override IReadOnlyList<string> SeeAlso { get; } = ["step", "stop", "call", "reset"];

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
        var options = ParseRunOptions(args);

        debugContext.Output.WriteLine($"Running from PC=${debugContext.Cpu.GetPC():X4}...");

        // Execute the instruction loop - RunCommand terminates only on halt/limits
        var result = ExecuteInstructionLoop(
            debugContext,
            options,
            (_, _) => false); // Never terminate early - rely on halt detection

        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"Stopped: {result.StopReason}");
        debugContext.Output.WriteLine($"  Instructions executed: {result.InstructionCount:N0}");
        debugContext.Output.WriteLine($"  Cycles consumed: {result.CycleCount:N0}");
        debugContext.Output.WriteLine($"  Final PC = ${debugContext.Cpu.GetPC():X4}");

        // Output buffered trace if requested
        if (options.EnableTrace && options.BufferTrace && debugContext.TracingListener is not null)
        {
            OutputBufferedTrace(debugContext, debugContext.TracingListener, options.TraceLastN);
        }

        return CommandResult.Ok();
    }

    private static ExecutionOptions ParseRunOptions(string[] args)
    {
        var options = ParseCommonOptions(args);

        // Parse positional argument as instruction limit
        foreach (var arg in args)
        {
            if (!arg.StartsWith("-", StringComparison.Ordinal) &&
                TryParseNumber(arg, out long limit) &&
                limit <= int.MaxValue)
            {
                options.InstructionLimit = (int)limit;
                break;
            }
        }

        return options;
    }
}