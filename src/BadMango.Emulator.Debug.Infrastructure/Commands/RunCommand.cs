// <copyright file="RunCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Debug.Infrastructure;

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
    public override string Usage => "run [instruction_limit] [--cycles=<limit>] [--trace] [--trace-file=<path>] [--trace-buffer]";

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

        // Configure tracing if requested
        var tracingListener = debugContext.TracingListener;
        bool tracingWasEnabled = tracingListener?.IsEnabled ?? false;

        if (options.EnableTrace && tracingListener is not null)
        {
            ConfigureTracing(debugContext, tracingListener, options);
        }

        try
        {
            debugContext.Cpu.ClearStopRequest();

            int instructionCount = 0;
            long cycleCount = 0;
            string stopReason = "unknown";

            while (instructionCount < options.InstructionLimit && cycleCount < options.CycleLimit)
            {
                if (debugContext.Cpu.Halted)
                {
                    stopReason = $"CPU halted ({debugContext.Cpu.HaltReason})";
                    break;
                }

                if (debugContext.Cpu.IsStopRequested)
                {
                    stopReason = "stop requested";
                    debugContext.Cpu.ClearStopRequest();
                    break;
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

            // Output buffered trace if requested
            if (options.EnableTrace && options.BufferTrace && tracingListener is not null)
            {
                OutputBufferedTrace(debugContext, tracingListener, options);
            }

            return CommandResult.Ok();
        }
        finally
        {
            // Restore tracing state
            if (tracingListener is not null)
            {
                tracingListener.IsEnabled = tracingWasEnabled;
                tracingListener.SetConsoleOutput(null);
                tracingListener.CloseFileOutput();
                tracingListener.BufferOutput = false;
            }
        }
    }

    private static void ConfigureTracing(IDebugContext debugContext, TracingDebugListener tracingListener, RunOptions options)
    {
        tracingListener.ResetInstructionCount();
        tracingListener.ClearBuffer();

        if (options.BufferTrace)
        {
            debugContext.Output.WriteLine("Trace mode enabled (buffered - will output last records on completion).");
            tracingListener.BufferOutput = true;
            tracingListener.MaxBufferedRecords = options.TraceBufferSize;
        }
        else
        {
            debugContext.Output.WriteLine("Trace mode enabled (streaming to console).");
            tracingListener.BufferOutput = false;
            tracingListener.SetConsoleOutput(debugContext.Output);
        }

        if (!string.IsNullOrEmpty(options.TraceFilePath))
        {
            try
            {
                tracingListener.SetFileOutput(options.TraceFilePath);
                debugContext.Output.WriteLine($"Trace file: {options.TraceFilePath}");
            }
            catch (IOException ex)
            {
                debugContext.Error.WriteLine($"Warning: Could not open trace file: {ex.Message}");
            }
        }

        tracingListener.IsEnabled = true;
    }

    private static void OutputBufferedTrace(IDebugContext debugContext, TracingDebugListener tracingListener, RunOptions options)
    {
        var records = tracingListener.GetBufferedRecords();
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"=== Trace Output ({records.Count:N0} records) ===");

        // Determine how many records to show
        int showCount = options.TraceLastN > 0 ? Math.Min(options.TraceLastN, records.Count) : records.Count;
        int startIndex = records.Count - showCount;

        if (startIndex > 0)
        {
            debugContext.Output.WriteLine($"(Showing last {showCount:N0} of {records.Count:N0} records)");
        }

        for (int i = startIndex; i < records.Count; i++)
        {
            debugContext.Output.WriteLine(TracingDebugListener.FormatTraceRecord(records[i]));
        }

        debugContext.Output.WriteLine("=== End Trace ===");
    }

    private static RunOptions ParseOptions(string[] args)
    {
        var options = new RunOptions
        {
            InstructionLimit = DefaultInstructionLimit,
            CycleLimit = DefaultCycleLimit,
            EnableTrace = false,
            BufferTrace = false,
            TraceFilePath = null,
            TraceBufferSize = 10000,
            TraceLastN = 100,
        };

        foreach (var arg in args)
        {
            if (arg.Equals("--trace", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableTrace = true;
            }
            else if (arg.Equals("--trace-buffer", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("-tb", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableTrace = true;
                options.BufferTrace = true;
            }
            else if (arg.StartsWith("--trace-file=", StringComparison.OrdinalIgnoreCase))
            {
                options.TraceFilePath = arg["--trace-file=".Length..];
                options.EnableTrace = true;
            }
            else if (arg.StartsWith("--trace-last=", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = arg["--trace-last=".Length..];
                if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lastN))
                {
                    options.TraceLastN = lastN;
                }
            }
            else if (arg.StartsWith("--trace-buffer-size=", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = arg["--trace-buffer-size=".Length..];
                if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int bufferSize))
                {
                    options.TraceBufferSize = bufferSize;
                }
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

        public bool BufferTrace { get; set; }

        public string? TraceFilePath { get; set; }

        public int TraceBufferSize { get; set; }

        public int TraceLastN { get; set; }
    }
}