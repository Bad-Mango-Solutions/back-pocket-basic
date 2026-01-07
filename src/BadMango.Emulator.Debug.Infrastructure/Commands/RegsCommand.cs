// <copyright file="RegsCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using Core.Cpu;

/// <summary>
/// Displays the current state of all CPU registers.
/// </summary>
/// <remarks>
/// Shows the Accumulator (A), Index registers (X, Y), Stack Pointer (SP),
/// Program Counter (PC), and Processor Status (P) flags in a formatted display.
/// Register widths vary based on CPU mode:
/// - Native (E=0, CP=0): All registers are 32-bit
/// - 65816 Compat (E=0, CP=1): A width depends on M flag, X/Y width depends on X flag (0=16-bit, 1=8-bit)
/// - 65C02 Compat (E=1, CP=1): All registers are 8-bit, SP is 8-bit.
/// </remarks>
public sealed class RegsCommand : CommandHandlerBase, ICommandHelp
{
    private const int FrameWidth = 60; // Inner width between │ characters
    private const int RegValueWidth = 9; // Width for register values (e.g., "$00000000")

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
    public string Synopsis => "regs";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays all CPU registers in a formatted box display including PC, SP, A, X, Y, " +
        "and processor status flags. Register widths vary based on CPU mode: Native mode " +
        "shows 32-bit registers, 65816 compat shows 16-bit, and 65C02 compat shows 8-bit.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "regs                    Display all CPU registers",
        "r                       Alias for regs",
        "registers               Alias for regs",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["pc", "step", "run"];

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
        string horizontalLine = new('─', FrameWidth);
        output.WriteLine($"┌{horizontalLine}┐");
        WriteCenteredLine(output, "CPU Registers");
        output.WriteLine($"├{horizontalLine}┤");

        // Determine register widths based on mode
        bool isNative = !registers.E && !registers.CP;
        bool is65816Compat = !registers.E && registers.CP;
        bool is65C02Compat = registers.E && registers.CP;

        // Program Counter and Stack Pointer
        string pcValue;
        string spValue;

        if (isNative)
        {
            // Native mode: PC and SP are 32-bit
            pcValue = $"${registers.PC.GetAddr():X8}";
            spValue = $"${registers.SP.GetDWord():X8}";
        }
        else if (is65C02Compat)
        {
            // 65C02 compat: PC is 16-bit, SP is 8-bit
            pcValue = $"${registers.PC.GetWord():X4}";
            spValue = $"${registers.SP.GetByte():X2}";
        }
        else
        {
            // 65816 compat: PC and SP are 16-bit
            pcValue = $"${registers.PC.GetWord():X4}";
            spValue = $"${registers.SP.GetWord():X4}";
        }

        WriteLine(output, $"  {"PC",-4} = {pcValue,-RegValueWidth}    {"SP",-4} = {spValue,-RegValueWidth}");

        // Accumulator and Index registers - width depends on mode and flags
        string aValue;
        string xValue;
        string yValue;

        if (isNative)
        {
            // Native mode: A, X, Y are 32-bit
            aValue = $"${registers.A.GetDWord():X8}";
            xValue = $"${registers.X.GetDWord():X8}";
            yValue = $"${registers.Y.GetDWord():X8}";
        }
        else if (is65816Compat)
        {
            // 65816 compat: A width depends on M flag, X/Y width depends on X flag
            // M=0 means 16-bit accumulator, M=1 means 8-bit
            // X=0 means 16-bit index, X=1 means 8-bit
            bool acc8Bit = registers.P.HasFlag(ProcessorStatusFlags.M);
            bool idx8Bit = registers.P.HasFlag(ProcessorStatusFlags.X);

            aValue = acc8Bit ? $"${registers.A.GetByte():X2}" : $"${registers.A.GetWord():X4}";
            xValue = idx8Bit ? $"${registers.X.GetByte():X2}" : $"${registers.X.GetWord():X4}";
            yValue = idx8Bit ? $"${registers.Y.GetByte():X2}" : $"${registers.Y.GetWord():X4}";
        }
        else
        {
            // 65C02 compat: A, X, Y are 8-bit
            aValue = $"${registers.A.GetByte():X2}";
            xValue = $"${registers.X.GetByte():X2}";
            yValue = $"${registers.Y.GetByte():X2}";
        }

        WriteLine(output, $"  {"A",-4} = {aValue,-RegValueWidth}    {"X",-4} = {xValue,-RegValueWidth}    {"Y",-4} = {yValue,-RegValueWidth}");

        // Processor Status flags
        output.WriteLine($"├{horizontalLine}┤");
        WriteLine(output, "  Status Flags (P):");

        // Header row varies by mode:
        // - Native: M and X bits exist but are undefined/unused (show "?")
        // - 65816 compat: M and X control register sizes
        // - 65C02 compat: These bits are unused by design (show "-")
        string flagHeader;
        if (isNative)
        {
            flagHeader = "    N V ? ? D I Z C";
        }
        else if (is65C02Compat)
        {
            flagHeader = "    N V - - D I Z C";
        }
        else
        {
            // 65816 compat shows M and X headers
            flagHeader = "    N V M X D I Z C";
        }

        WriteLine(output, flagHeader);

        // Always show the actual bit values from P register
        var flagsLine = $"    {FormatFlag(registers.P, ProcessorStatusFlags.N)} {FormatFlag(registers.P, ProcessorStatusFlags.V)} {FormatFlag(registers.P, ProcessorStatusFlags.M)} {FormatFlag(registers.P, ProcessorStatusFlags.X)} {FormatFlag(registers.P, ProcessorStatusFlags.D)} {FormatFlag(registers.P, ProcessorStatusFlags.I)} {FormatFlag(registers.P, ProcessorStatusFlags.Z)} {FormatFlag(registers.P, ProcessorStatusFlags.C)}   (${(byte)registers.P:X2})";
        WriteLine(output, flagsLine);

        // Additional mode information
        output.WriteLine($"├{horizontalLine}┤");
        WriteLine(output, $"  E = {(registers.E ? "1" : "0")} / CP = {(registers.CP ? "1" : "0")} ({GetEmulationModeName(registers)})");
        WriteLine(output, $"  Interrupts = {(registers.P.HasFlag(ProcessorStatusFlags.I) ? "Disabled" : "Enabled")}");
        output.WriteLine($"└{horizontalLine}┘");
    }

    private static string GetEmulationModeName(Registers registers)
    {
        return (registers.E, registers.CP) switch
        {
            (false, false) => "Native",
            (false, true) => "Compat: 65816",
            (true, true) => "Compat: 65C02",
            (true, false) => "Invalid", // E=1/CP=0 is not a valid state
        };
    }

    private static void WriteLine(TextWriter output, string content)
    {
        output.WriteLine($"│{content.PadRight(FrameWidth)}│");
    }

    private static void WriteCenteredLine(TextWriter output, string content)
    {
        int padding = (FrameWidth - content.Length) / 2;
        string centeredContent = new string(' ', padding) + content;
        WriteLine(output, centeredContent.PadRight(FrameWidth)[..FrameWidth]);
    }

    private static char FormatFlag(ProcessorStatusFlags p, ProcessorStatusFlags flag)
    {
        return p.HasFlag(flag) ? '1' : '0';
    }
}