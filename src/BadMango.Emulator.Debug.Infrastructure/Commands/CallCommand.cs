// <copyright file="CallCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Diagnostics;
using System.Globalization;

/// <summary>
/// Simulates a JSR to an address and returns when the subroutine completes.
/// </summary>
/// <remarks>
/// <para>
/// Executes a subroutine by pushing a fake return address onto the stack and
/// setting the PC to the target address. Execution continues until an RTS
/// instruction returns to the original stack frame.
/// </para>
/// <para>
/// This command is useful for testing subroutines, invoking trapped ROM entry
/// points, and executing code without entering a halted state that requires reset.
/// </para>
/// <para>
/// <strong>Side Effects:</strong> Modifies CPU registers, stack, and potentially memory.
/// May trigger soft switches and I/O device state changes if execution accesses
/// the I/O page ($C000-$CFFF).
/// </para>
/// </remarks>
public sealed class CallCommand : CommandHandlerBase
{
    /// <summary>
    /// Default timeout in milliseconds for call execution.
    /// </summary>
    public const int DefaultTimeoutMs = 5000;

    /// <summary>
    /// Default maximum instructions to execute (unlimited by default).
    /// </summary>
    public const int DefaultInstructionLimit = int.MaxValue;

    /// <summary>
    /// Sentinel address used as fake return address (BRK instruction location).
    /// </summary>
    private const ushort SentinelAddress = 0xFFF0;

    /// <summary>
    /// RTS opcode for detecting subroutine return.
    /// </summary>
    private const byte RtsOpcode = 0x60;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallCommand"/> class.
    /// </summary>
    public CallCommand()
        : base("call", "Execute subroutine and return to REPL")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["jsr"];

    /// <inheritdoc/>
    public override string Usage => "call <address> [--trace] [--limit <n>] [--timeout <ms>] [--quiet]";

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

        if (args.Length == 0)
        {
            return CommandResult.Error("Address required. Usage: call <address> [options]");
        }

        if (!TryParseAddress(args[0], out uint targetAddress))
        {
            return CommandResult.Error($"Invalid address: '{args[0]}'. Use hex format ($1234 or 0x1234) or decimal.");
        }

        // Parse options
        var options = ParseOptions(args);

        var cpu = debugContext.Cpu;

        // Record current stack pointer as the call frame boundary
        byte callFrameSP = cpu.Registers.SP.GetByte();

        // Push fake return address onto stack (address - 1 because RTS adds 1)
        // We push SentinelAddress - 1 so RTS will return to SentinelAddress
        ushort returnAddress = SentinelAddress - 1;
        cpu.Write8((uint)(0x0100 + cpu.Registers.SP.GetByte()), (byte)(returnAddress >> 8)); // Push high byte
        cpu.Registers.SP.SetByte((byte)(cpu.Registers.SP.GetByte() - 1));
        cpu.Write8((uint)(0x0100 + cpu.Registers.SP.GetByte()), (byte)(returnAddress & 0xFF)); // Push low byte
        cpu.Registers.SP.SetByte((byte)(cpu.Registers.SP.GetByte() - 1));

        // Set PC to target address
        cpu.SetPC(targetAddress);

        debugContext.Output.WriteLine($"Calling subroutine at ${targetAddress:X4}...");

        if (options.EnableTrace)
        {
            debugContext.Output.WriteLine("Trace mode enabled.");
        }

        // Configure tracing if requested
        var tracingListener = debugContext.TracingListener;
        bool tracingWasEnabled = tracingListener?.IsEnabled ?? false;

        if (options.EnableTrace && tracingListener is not null)
        {
            tracingListener.ResetInstructionCount();
            if (!options.Quiet)
            {
                tracingListener.SetConsoleOutput(debugContext.Output);
            }

            tracingListener.IsEnabled = true;
        }

        var stopwatch = Stopwatch.StartNew();
        int instructionCount = 0;
        long cycleCount = 0;
        string stopReason = "unknown";
        bool callCompleted = false;

        try
        {
            cpu.ClearStopRequest();

            while (instructionCount < options.InstructionLimit)
            {
                // Check timeout
                if (stopwatch.ElapsedMilliseconds >= options.TimeoutMs)
                {
                    stopReason = "timeout";
                    break;
                }

                if (cpu.Halted)
                {
                    stopReason = $"CPU halted ({cpu.HaltReason})";
                    break;
                }

                if (cpu.IsStopRequested)
                {
                    stopReason = "stop requested";
                    cpu.ClearStopRequest();
                    break;
                }

                // Get the opcode before stepping (to detect RTS)
                byte opcode = cpu.Peek8(cpu.GetPC());

                var result = cpu.Step();
                instructionCount++;
                cycleCount += (long)result.CyclesConsumed.Value;

                // Check if we've returned from the call
                // RTS was executed and SP is now at or above the original call frame
                if (opcode == RtsOpcode && cpu.Registers.SP.GetByte() >= callFrameSP)
                {
                    stopReason = "subroutine returned";
                    callCompleted = true;
                    break;
                }
            }

            if (instructionCount >= options.InstructionLimit)
            {
                stopReason = "instruction limit reached";
            }

            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine($"Call completed: {stopReason}");
            debugContext.Output.WriteLine($"  Instructions executed: {instructionCount:N0}");
            debugContext.Output.WriteLine($"  Cycles consumed: {cycleCount:N0}");
            debugContext.Output.WriteLine($"  Elapsed time: {stopwatch.ElapsedMilliseconds}ms");
            debugContext.Output.WriteLine($"  Final PC = ${cpu.GetPC():X4}");

            if (!callCompleted && stopReason != "subroutine returned")
            {
                debugContext.Output.WriteLine();
                debugContext.Output.WriteLine("Warning: Subroutine did not return normally.");
            }

            // Display final register state
            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine("Final Register State:");
            var regs = cpu.GetRegisters();
            debugContext.Output.WriteLine($"  A=${regs.A.GetByte():X2}  X=${regs.X.GetByte():X2}  Y=${regs.Y.GetByte():X2}  SP=${regs.SP.GetByte():X2}");
            debugContext.Output.WriteLine($"  P={FormatFlags(regs.P)}");

            return CommandResult.Ok();
        }
        finally
        {
            // Restore tracing state
            if (tracingListener is not null)
            {
                tracingListener.IsEnabled = tracingWasEnabled;
                tracingListener.SetConsoleOutput(null);
            }
        }
    }

    private static CallOptions ParseOptions(string[] args)
    {
        var options = new CallOptions
        {
            EnableTrace = false,
            Quiet = false,
            InstructionLimit = DefaultInstructionLimit,
            TimeoutMs = DefaultTimeoutMs,
        };

        for (int i = 1; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg.Equals("--trace", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-t", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableTrace = true;
            }
            else if (arg.Equals("--quiet", StringComparison.OrdinalIgnoreCase) ||
                     arg.Equals("-q", StringComparison.OrdinalIgnoreCase))
            {
                options.Quiet = true;
            }
            else if (arg.StartsWith("--limit", StringComparison.OrdinalIgnoreCase))
            {
                if (arg.Contains('=', StringComparison.Ordinal))
                {
                    var valueStr = arg[(arg.IndexOf('=', StringComparison.Ordinal) + 1)..];
                    if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int limit))
                    {
                        options.InstructionLimit = limit;
                    }
                }
                else if (i + 1 < args.Length &&
                         int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int limit))
                {
                    options.InstructionLimit = limit;
                    i++; // Skip the next argument
                }
            }
            else if (arg.StartsWith("--timeout", StringComparison.OrdinalIgnoreCase))
            {
                if (arg.Contains('=', StringComparison.Ordinal))
                {
                    var valueStr = arg[(arg.IndexOf('=', StringComparison.Ordinal) + 1)..];
                    if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int timeout))
                    {
                        options.TimeoutMs = timeout;
                    }
                }
                else if (i + 1 < args.Length &&
                         int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int timeout))
                {
                    options.TimeoutMs = timeout;
                    i++; // Skip the next argument
                }
            }
        }

        return options;
    }

    private static bool TryParseAddress(string value, out uint result)
    {
        result = 0;

        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            return uint.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static string FormatFlags(Core.Cpu.ProcessorStatusFlags flags)
    {
        char n = (flags & Core.Cpu.ProcessorStatusFlags.N) != 0 ? 'N' : 'n';
        char v = (flags & Core.Cpu.ProcessorStatusFlags.V) != 0 ? 'V' : 'v';
        char d = (flags & Core.Cpu.ProcessorStatusFlags.D) != 0 ? 'D' : 'd';
        char i = (flags & Core.Cpu.ProcessorStatusFlags.I) != 0 ? 'I' : 'i';
        char z = (flags & Core.Cpu.ProcessorStatusFlags.Z) != 0 ? 'Z' : 'z';
        char c = (flags & Core.Cpu.ProcessorStatusFlags.C) != 0 ? 'C' : 'c';
        return $"{n}{v}-1{d}{i}{z}{c}";
    }

    private sealed class CallOptions
    {
        public bool EnableTrace { get; set; }

        public bool Quiet { get; set; }

        public int InstructionLimit { get; set; }

        public int TimeoutMs { get; set; }
    }
}