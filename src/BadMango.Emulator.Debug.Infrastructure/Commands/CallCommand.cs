// <copyright file="CallCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Diagnostics;

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
public sealed class CallCommand : ExecutionCommandBase
{
    /// <summary>
    /// Sentinel address used as fake return address (BRK instruction location).
    /// </summary>
    /// <remarks>
    /// This address is chosen to be in the high ROM area ($FFF0) which is typically
    /// unused during subroutine execution. When RTS pops this address, it indicates
    /// the called subroutine has returned. The actual value doesn't matter as long
    /// as it's not a valid code location that would be reached during normal execution.
    /// </remarks>
    private const ushort SentinelAddress = 0xFFF0;

    /// <summary>
    /// Base address of the 6502 hardware stack page ($0100-$01FF).
    /// </summary>
    private const ushort StackPageBase = 0x0100;

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
    public override string Synopsis => "call <address> [options]";

    /// <inheritdoc/>
    public override string DetailedDescription =>
        "Simulates a JSR (Jump to Subroutine) to the specified address by pushing a " +
        "fake return address onto the stack and stepping through instructions until " +
        "an RTS returns to the original call frame. Useful for testing subroutines " +
        "and invoking trapped ROM entry points without entering a halted state.";

    /// <inheritdoc/>
    public override IReadOnlyList<CommandOption> Options { get; } =
    [
        new("--trace", "-t", "flag", "Enable instruction tracing during execution", "off"),
        new("--limit", null, "int", "Maximum instructions to execute", "unlimited"),
        new("--timeout", null, "int", "Maximum wall-clock time in milliseconds", "5000"),
        new("--quiet", "-q", "flag", "Suppress per-instruction output", "off"),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples { get; } =
    [
        "call $FC58                    Execute HOME routine",
        "call $FDED --trace            Call COUT with tracing",
        "call $C600 --limit 1000       Boot from slot 6 (limited)",
    ];

    /// <inheritdoc/>
    public override string? SideEffects =>
        "Modifies CPU registers, stack, and potentially memory. May trigger " +
        "soft switches and I/O device state changes if execution accesses " +
        "the I/O page ($C000-$CFFF).";

    /// <inheritdoc/>
    public override IReadOnlyList<string> SeeAlso { get; } = ["run", "step", "peek", "poke", "break"];

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
        var options = ParseCallOptions(args);

        var cpu = debugContext.Cpu;

        // Record current stack pointer as the call frame boundary
        byte callFrameSP = cpu.Registers.SP.GetByte();

        // Push fake return address onto stack (address - 1 because RTS adds 1)
        // We push SentinelAddress - 1 so RTS will return to SentinelAddress
        ushort returnAddress = SentinelAddress - 1;
        cpu.Write8((uint)(StackPageBase + cpu.Registers.SP.GetByte()), (byte)(returnAddress >> 8)); // Push high byte
        cpu.Registers.SP.SetByte((byte)(cpu.Registers.SP.GetByte() - 1));
        cpu.Write8((uint)(StackPageBase + cpu.Registers.SP.GetByte()), (byte)(returnAddress & 0xFF)); // Push low byte
        cpu.Registers.SP.SetByte((byte)(cpu.Registers.SP.GetByte() - 1));

        // Set PC to target address
        cpu.SetPC(targetAddress);

        debugContext.Output.WriteLine($"Calling subroutine at ${targetAddress:X4}...");

        if (options.EnableTrace)
        {
            debugContext.Output.WriteLine("Trace mode enabled.");
        }

        var stopwatch = Stopwatch.StartNew();

        // Execute the instruction loop with call-specific termination
        var result = ExecuteInstructionLoop(
            debugContext,
            options,
            (ctx, opcode) =>
            {
                // Check timeout
                if (options.TimeoutMs > 0 && stopwatch.ElapsedMilliseconds >= options.TimeoutMs)
                {
                    return false; // Let the base class handle timeout
                }

                // Check if we've returned from the call
                // RTS was executed and SP is now at or above the original call frame
                return opcode == RtsOpcode && ctx.Cpu!.Registers.SP.GetByte() >= callFrameSP;
            });

        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"Call completed: {result.StopReason}");
        debugContext.Output.WriteLine($"  Instructions executed: {result.InstructionCount:N0}");
        debugContext.Output.WriteLine($"  Cycles consumed: {result.CycleCount:N0}");
        debugContext.Output.WriteLine($"  Elapsed time: {result.ElapsedMs}ms");
        debugContext.Output.WriteLine($"  Final PC = ${cpu.GetPC():X4}");

        if (!result.NormalCompletion)
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

    private static ExecutionOptions ParseCallOptions(string[] args)
    {
        var options = ParseCommonOptions(args);

        // Set call-specific defaults
        options.TimeoutMs = DefaultTimeoutMs;
        options.InstructionLimit = int.MaxValue; // Unlimited by default for call

        // Parse call-specific options (--limit and --timeout with space-separated values)
        for (int i = 1; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg.StartsWith("--limit", StringComparison.OrdinalIgnoreCase) &&
                !arg.Contains('=', StringComparison.Ordinal) &&
                i + 1 < args.Length &&
                int.TryParse(args[i + 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int limit))
            {
                options.InstructionLimit = limit;
                i++;
            }
            else if (arg.StartsWith("--timeout", StringComparison.OrdinalIgnoreCase) &&
                     !arg.Contains('=', StringComparison.Ordinal) &&
                     i + 1 < args.Length &&
                     int.TryParse(args[i + 1], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int timeout))
            {
                options.TimeoutMs = timeout;
                i++;
            }
        }

        return options;
    }
}