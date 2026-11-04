// <copyright file="StepCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Cpu;

/// <summary>
/// Executes one or more CPU instructions in single-step mode.
/// </summary>
/// <remarks>
/// When invoked without arguments, executes a single instruction.
/// When invoked with a count, executes that many instructions.
/// Displays disassembly and register state after each step.
/// </remarks>
public sealed class StepCommand : CommandHandlerBase, ICommandHelp
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
    public string Synopsis => "step [count]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Executes one or more CPU instructions in single-step mode. Without arguments, " +
        "executes a single instruction. With a count, executes that many instructions. " +
        "Displays the disassembled instruction before execution and reports the total " +
        "cycles consumed. Stops early if CPU halts or stop is requested.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "step                    Execute one instruction",
        "step 10                 Execute 10 instructions",
        "s                       Alias for step",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Modifies CPU registers (PC, A, X, Y, SP, P) and potentially memory based on " +
        "the executed instructions. May trigger I/O side effects.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["run", "stop", "regs", "pc"];

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

            // Disassemble the instruction about to execute so we can show the
            // effective address (when applicable) alongside the mnemonic.
            DisassembledInstruction? disassembled = null;
            if (debugContext.Disassembler is not null)
            {
                var pc = debugContext.Cpu.GetPC();
                disassembled = debugContext.Disassembler.DisassembleInstruction(pc);
                debugContext.Output.WriteLine(
                    $"${disassembled.Address:X4}: {disassembled.FormatBytes(),-12} {disassembled.FormatInstruction()}");
            }

            var result = debugContext.Cpu.Step();
            totalCycles += (int)result.CyclesConsumed.Value;

            // Show register state and effective address after the instruction.
            var regs = debugContext.Cpu.GetRegisters();
            debugContext.Output.WriteLine(
                $"  A={regs.A.GetByte():X2} X={regs.X.GetByte():X2} Y={regs.Y.GetByte():X2} " +
                $"SP={regs.SP.GetByte():X2} P={(byte)regs.P:X2} [{FormatFlags(regs.P)}] " +
                $"PC=${debugContext.Cpu.GetPC():X4} Cyc={result.CyclesConsumed.Value}");

            if (disassembled is not null)
            {
                string? ea = FormatEffectiveAddress(disassembled);
                if (ea is not null)
                {
                    debugContext.Output.WriteLine($"  EA={ea}");
                }
            }

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

    private static string FormatFlags(ProcessorStatusFlags p)
    {
        Span<char> chars = stackalloc char[8];
        chars[0] = (p & ProcessorStatusFlags.N) != 0 ? 'N' : '.';
        chars[1] = (p & ProcessorStatusFlags.V) != 0 ? 'V' : '.';
        chars[2] = '.';
        chars[3] = '.';
        chars[4] = (p & ProcessorStatusFlags.D) != 0 ? 'D' : '.';
        chars[5] = (p & ProcessorStatusFlags.I) != 0 ? 'I' : '.';
        chars[6] = (p & ProcessorStatusFlags.Z) != 0 ? 'Z' : '.';
        chars[7] = (p & ProcessorStatusFlags.C) != 0 ? 'C' : '.';
        return new string(chars);
    }

    private static string? FormatEffectiveAddress(DisassembledInstruction instruction)
    {
        // Only memory-touching addressing modes have a meaningful effective address.
        switch (instruction.AddressingMode)
        {
            case CpuAddressingModes.ZeroPage:
            case CpuAddressingModes.ZeroPageX:
            case CpuAddressingModes.ZeroPageY:
            case CpuAddressingModes.Absolute:
            case CpuAddressingModes.AbsoluteX:
            case CpuAddressingModes.AbsoluteY:
            case CpuAddressingModes.Indirect:
            case CpuAddressingModes.IndirectX:
            case CpuAddressingModes.IndirectY:
            case CpuAddressingModes.Relative:
                return $"${GetOperandValue(instruction):X4}";
            default:
                return null;
        }
    }

    private static uint GetOperandValue(DisassembledInstruction instruction) =>
        instruction.OperandLength switch
        {
            1 => instruction.Operands[0],
            2 => (uint)(instruction.Operands[0] | (instruction.Operands[1] << 8)),
            _ => 0,
        };

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