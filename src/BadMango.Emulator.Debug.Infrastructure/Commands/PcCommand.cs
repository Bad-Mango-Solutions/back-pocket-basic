// <copyright file="PcCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

/// <summary>
/// Gets or sets the Program Counter (PC) register.
/// </summary>
/// <remarks>
/// When invoked without arguments, displays the current PC value.
/// When invoked with an address argument, sets the PC to that address.
/// </remarks>
public sealed class PcCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PcCommand"/> class.
    /// </summary>
    public PcCommand()
        : base("pc", "Get or set the Program Counter")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = [];

    /// <inheritdoc/>
    public override string Usage => "pc [address]";

    /// <inheritdoc/>
    public string Synopsis => "pc [address]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Gets or sets the Program Counter (PC) register. Without arguments, displays " +
        "the current PC value and the instruction at that address. With an address " +
        "argument, sets PC to the specified value. Useful for redirecting execution " +
        "to a specific memory location.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "pc                      Display current PC and instruction",
        "pc $1000                Set PC to $1000",
        "pc 0x800                Set PC to $0800",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "When an address is provided, modifies the CPU program counter.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["regs", "step", "run", "dasm"];

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

        if (args.Length == 0)
        {
            // Display current PC
            var pc = debugContext.Cpu.GetPC();
            debugContext.Output.WriteLine($"PC = ${pc:X4}");

            // Also show the instruction at PC if disassembler is available
            if (debugContext.Disassembler is not null)
            {
                var instruction = debugContext.Disassembler.DisassembleInstruction(pc);
                debugContext.Output.WriteLine($"     {instruction.FormatBytes(),-12} {instruction.FormatInstruction()}");
            }

            return CommandResult.Ok();
        }

        // Set PC to specified address
        if (!TryParseAddress(args[0], out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[0]}'. Use hex format ($1234 or 0x1234) or decimal.");
        }

        debugContext.Cpu.SetPC(address);
        debugContext.Output.WriteLine($"PC set to ${address:X4}");

        return CommandResult.Ok();
    }

    private static bool TryParseAddress(string value, out uint result)
    {
        result = 0;

        // Try hex format first
        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            return uint.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        // Try decimal
        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}