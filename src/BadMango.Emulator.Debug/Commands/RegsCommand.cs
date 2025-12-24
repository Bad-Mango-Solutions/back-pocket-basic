// <copyright file="RegsCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Commands;

using BadMango.Emulator.Core;

/// <summary>
/// Displays the current state of all CPU registers.
/// </summary>
/// <remarks>
/// Shows the Accumulator (A), Index registers (X, Y), Stack Pointer (SP),
/// Program Counter (PC), and Processor Status (P) flags in a formatted display.
/// </remarks>
public sealed class RegsCommand : CommandHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegsCommand"/> class.
    /// </summary>
    public RegsCommand()
        : base("regs", "Display CPU registers")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["r", "registers"];

    /// <inheritdoc/>
    public override string Usage => "regs";

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

        var registers = debugContext.Cpu.GetRegisters();
        FormatRegisters(debugContext.Output, registers);

        return CommandResult.Ok();
    }

    private static void FormatRegisters(TextWriter output, Registers registers)
    {
        output.WriteLine("┌─────────────────────────────────────────────────┐");
        output.WriteLine("│                  CPU Registers                  │");
        output.WriteLine("├─────────────────────────────────────────────────┤");

        // Program Counter and Stack Pointer
        output.WriteLine($"│  PC = ${registers.PC.GetAddr():X4}    SP = ${registers.SP.GetWord():X4}              │");

        // Accumulator and Index registers
        output.WriteLine($"│  A  = ${registers.A.GetByte():X2}      X  = ${registers.X.GetByte():X2}      Y  = ${registers.Y.GetByte():X2}      │");

        // Processor Status flags
        output.WriteLine("├─────────────────────────────────────────────────┤");
        output.WriteLine("│  Status Flags (P):                              │");
        output.WriteLine($"│    N V - B D I Z C                              │");
        output.WriteLine($"│    {FormatFlag(registers.P, ProcessorStatusFlags.N)} {FormatFlag(registers.P, ProcessorStatusFlags.V)} 1 {FormatFlag(registers.P, ProcessorStatusFlags.B)} {FormatFlag(registers.P, ProcessorStatusFlags.D)} {FormatFlag(registers.P, ProcessorStatusFlags.I)} {FormatFlag(registers.P, ProcessorStatusFlags.Z)} {FormatFlag(registers.P, ProcessorStatusFlags.C)}   (${(byte)registers.P:X2})                     │");

        // Additional mode information
        output.WriteLine("├─────────────────────────────────────────────────┤");
        output.WriteLine($"│  E = {(registers.E ? "1 (Emulation)" : "0 (Native)")}                          │");
        output.WriteLine($"│  Interrupts = {(registers.P.HasFlag(ProcessorStatusFlags.I) ? "Disabled" : "Enabled")}                       │");
        output.WriteLine("└─────────────────────────────────────────────────┘");
    }

    private static char FormatFlag(ProcessorStatusFlags p, ProcessorStatusFlags flag)
    {
        return p.HasFlag(flag) ? '1' : '0';
    }
}