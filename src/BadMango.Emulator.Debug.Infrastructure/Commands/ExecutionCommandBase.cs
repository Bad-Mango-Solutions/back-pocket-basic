// <copyright file="ExecutionCommandBase.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Diagnostics;
using System.Globalization;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Base class for commands that execute CPU instructions.
/// </summary>
/// <remarks>
/// <para>
/// Provides common functionality for instruction execution including:
/// - Instruction stepping loop with stop request handling.
/// - Trace output formatting.
/// - Trap detection and trace output.
/// - Timeout and instruction limit enforcement.
/// - Breakpoint checking.
/// - Common options parsing (--trace, --limit, --timeout, --quiet).
/// </para>
/// <para>
/// Derived classes implement specific termination conditions:
/// - <see cref="RunCommand"/>: Runs until STP, breakpoint, or stop requested.
/// - <see cref="CallCommand"/>: Runs until RTS unwinds to call frame.
/// </para>
/// </remarks>
public abstract class ExecutionCommandBase : CommandHandlerBase, ICommandHelp
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
    /// Default timeout in milliseconds.
    /// </summary>
    public const int DefaultTimeoutMs = 5000;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionCommandBase"/> class.
    /// </summary>
    /// <param name="name">The primary name of the command.</param>
    /// <param name="description">A brief description of the command.</param>
    protected ExecutionCommandBase(string name, string description)
        : base(name, description)
    {
    }

    /// <inheritdoc/>
    public abstract string Synopsis { get; }

    /// <inheritdoc/>
    public abstract string DetailedDescription { get; }

    /// <inheritdoc/>
    public abstract IReadOnlyList<CommandOption> Options { get; }

    /// <inheritdoc/>
    public abstract IReadOnlyList<string> Examples { get; }

    /// <inheritdoc/>
    public abstract string? SideEffects { get; }

    /// <inheritdoc/>
    public abstract IReadOnlyList<string> SeeAlso { get; }

    /// <summary>
    /// Configures tracing based on options.
    /// </summary>
    /// <param name="debugContext">The debug context.</param>
    /// <param name="tracingListener">The tracing listener.</param>
    /// <param name="options">Execution options.</param>
    protected static void ConfigureTracing(
        IDebugContext debugContext,
        TracingDebugListener tracingListener,
        ExecutionOptions options)
    {
        tracingListener.ResetInstructionCount();
        tracingListener.ClearBuffer();

        if (options.BufferTrace)
        {
            debugContext.Output.WriteLine("Trace mode enabled (buffered - will output last records on completion).");
            tracingListener.BufferOutput = true;
            tracingListener.MaxBufferedRecords = options.TraceBufferSize;
        }
        else if (!options.Quiet)
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
                debugContext.Error.WriteLine($"Warning: Could not open trace file '{options.TraceFilePath}': {ex.Message}");
            }
        }

        tracingListener.IsEnabled = true;
    }

    /// <summary>
    /// Outputs buffered trace records.
    /// </summary>
    /// <param name="debugContext">The debug context.</param>
    /// <param name="tracingListener">The tracing listener.</param>
    /// <param name="lastN">Number of last records to show, or 0 for all.</param>
    protected static void OutputBufferedTrace(
        IDebugContext debugContext,
        TracingDebugListener tracingListener,
        int lastN)
    {
        var records = tracingListener.GetBufferedRecords();
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"=== Trace Output ({records.Count:N0} records) ===");

        int showCount = lastN > 0 ? Math.Min(lastN, records.Count) : records.Count;
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

    /// <summary>
    /// Formats processor status flags as a string.
    /// </summary>
    /// <param name="flags">The processor status flags.</param>
    /// <returns>A formatted string representation of the flags.</returns>
    protected static string FormatFlags(Core.Cpu.ProcessorStatusFlags flags)
    {
        char n = (flags & Core.Cpu.ProcessorStatusFlags.N) != 0 ? 'N' : 'n';
        char v = (flags & Core.Cpu.ProcessorStatusFlags.V) != 0 ? 'V' : 'v';
        char d = (flags & Core.Cpu.ProcessorStatusFlags.D) != 0 ? 'D' : 'd';
        char i = (flags & Core.Cpu.ProcessorStatusFlags.I) != 0 ? 'I' : 'i';
        char z = (flags & Core.Cpu.ProcessorStatusFlags.Z) != 0 ? 'Z' : 'z';
        char c = (flags & Core.Cpu.ProcessorStatusFlags.C) != 0 ? 'C' : 'c';
        return $"{n}{v}-1{d}{i}{z}{c}";
    }

    /// <summary>
    /// Parses common execution options from arguments.
    /// </summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>Parsed execution options.</returns>
    protected static ExecutionOptions ParseCommonOptions(string[] args)
    {
        var options = new ExecutionOptions();

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
            else if (arg.Equals("--quiet", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("-q", StringComparison.OrdinalIgnoreCase))
            {
                options.Quiet = true;
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
            else if (arg.StartsWith("--limit=", StringComparison.OrdinalIgnoreCase) ||
                     arg.StartsWith("--instructions=", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = arg.Contains("--limit=", StringComparison.OrdinalIgnoreCase)
                    ? arg["--limit=".Length..]
                    : arg["--instructions=".Length..];
                if (TryParseNumber(valueStr, out long instrLimit) && instrLimit <= int.MaxValue)
                {
                    options.InstructionLimit = (int)instrLimit;
                }
            }
            else if (arg.StartsWith("--timeout=", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = arg["--timeout=".Length..];
                if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int timeout))
                {
                    options.TimeoutMs = timeout;
                }
            }
        }

        return options;
    }

    /// <summary>
    /// Tries to parse a number from a string, supporting hex and decimal formats.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="result">The parsed result.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    protected static bool TryParseNumber(string value, out long result)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("$", StringComparison.Ordinal))
        {
            var hexValue = value.StartsWith("$", StringComparison.Ordinal) ? value[1..] : value[2..];
            return long.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Tries to parse an address from a string.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="result">The parsed address.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    protected static bool TryParseAddress(string value, out uint result)
    {
        return TryParseAddress(value, null, out result);
    }

    /// <summary>
    /// Tries to parse an address from a string, with soft switch name resolution.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="machine">The machine to query for soft switch providers, or <see langword="null"/>.</param>
    /// <param name="result">The parsed address.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    protected static bool TryParseAddress(string value, IMachine? machine, out uint result)
    {
        return AddressParser.TryParse(value, machine, out result);
    }

    /// <summary>
    /// Executes the instruction loop with the given termination condition.
    /// </summary>
    /// <param name="debugContext">The debug context.</param>
    /// <param name="options">Execution options.</param>
    /// <param name="shouldTerminate">A function that returns true when execution should stop, along with the stop reason.</param>
    /// <returns>The execution result containing statistics.</returns>
    protected ExecutionResult ExecuteInstructionLoop(
        IDebugContext debugContext,
        ExecutionOptions options,
        Func<IDebugContext, byte, bool> shouldTerminate)
    {
        ArgumentNullException.ThrowIfNull(debugContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(shouldTerminate);

        var cpu = debugContext.Cpu!;
        var tracingListener = debugContext.TracingListener;
        bool tracingWasEnabled = tracingListener?.IsEnabled ?? false;

        // Configure tracing
        if (options.EnableTrace && tracingListener is not null)
        {
            ConfigureTracing(debugContext, tracingListener, options);
        }

        var stopwatch = Stopwatch.StartNew();
        int instructionCount = 0;
        long cycleCount = 0;
        string stopReason = "unknown";
        bool normalCompletion = false;

        try
        {
            cpu.ClearStopRequest();

            while (instructionCount < options.InstructionLimit && cycleCount < options.CycleLimit)
            {
                // Check timeout
                if (options.TimeoutMs > 0 && stopwatch.ElapsedMilliseconds >= options.TimeoutMs)
                {
                    stopReason = "timeout";
                    break;
                }

                if (cpu.Halted)
                {
                    stopReason = $"CPU halted ({cpu.HaltReason})";
                    normalCompletion = true;
                    break;
                }

                if (cpu.IsStopRequested)
                {
                    stopReason = "stop requested";
                    cpu.ClearStopRequest();
                    break;
                }

                // Get the opcode before stepping (for derived class termination check)
                byte opcode = cpu.Peek8(cpu.GetPC());

                var result = cpu.Step();
                instructionCount++;
                cycleCount += (long)result.CyclesConsumed.Value;

                // Check custom termination condition
                if (shouldTerminate(debugContext, opcode))
                {
                    stopReason = "completed";
                    normalCompletion = true;
                    break;
                }
            }

            // Determine final stop reason if limits were reached and no other reason was set
            if (stopReason == "unknown")
            {
                if (instructionCount >= options.InstructionLimit)
                {
                    stopReason = "instruction limit reached";
                }
                else if (cycleCount >= options.CycleLimit)
                {
                    stopReason = "cycle limit reached";
                }
            }

            return new(
                instructionCount,
                cycleCount,
                stopwatch.ElapsedMilliseconds,
                stopReason,
                normalCompletion);
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

    /// <summary>
    /// Options for instruction execution.
    /// </summary>
    protected sealed class ExecutionOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of instructions to execute.
        /// </summary>
        public int InstructionLimit { get; set; } = DefaultInstructionLimit;

        /// <summary>
        /// Gets or sets the maximum number of cycles to execute.
        /// </summary>
        public long CycleLimit { get; set; } = DefaultCycleLimit;

        /// <summary>
        /// Gets or sets the timeout in milliseconds (0 for no timeout).
        /// </summary>
        public int TimeoutMs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tracing is enabled.
        /// </summary>
        public bool EnableTrace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether trace output is buffered.
        /// </summary>
        public bool BufferTrace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether output should be suppressed.
        /// </summary>
        public bool Quiet { get; set; }

        /// <summary>
        /// Gets or sets the path to write trace output to.
        /// </summary>
        public string? TraceFilePath { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of trace records to buffer.
        /// </summary>
        public int TraceBufferSize { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the number of last trace records to display.
        /// </summary>
        public int TraceLastN { get; set; } = 100;
    }

    /// <summary>
    /// Result of instruction execution.
    /// </summary>
    /// <param name="InstructionCount">The number of instructions executed.</param>
    /// <param name="CycleCount">The number of cycles consumed.</param>
    /// <param name="ElapsedMs">The elapsed wall-clock time in milliseconds.</param>
    /// <param name="StopReason">The reason execution stopped.</param>
    /// <param name="NormalCompletion">Whether execution completed normally.</param>
    protected sealed record ExecutionResult(
        int InstructionCount,
        long CycleCount,
        long ElapsedMs,
        string StopReason,
        bool NormalCompletion);
}