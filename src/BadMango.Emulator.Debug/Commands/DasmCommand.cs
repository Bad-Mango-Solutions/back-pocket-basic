// <copyright file="DasmCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Commands;

using System.Globalization;

using BadMango.Emulator.Core;

/// <summary>
/// Disassembles memory contents into assembly instructions.
/// </summary>
/// <remarks>
/// <para>
/// Supports multiple modes of operation:
/// - By byte count: dasm $1000 $20 (disassemble $20 bytes starting at $1000).
/// - By instruction count: dasm $1000 --instructions=10.
/// - By address range: dasm $1000 $1020 --range.
/// - From current PC: dasm (without address defaults to PC).
/// </para>
/// </remarks>
public sealed class DasmCommand : CommandHandlerBase
{
    private const int DefaultInstructionCount = 16;

    /// <summary>
    /// Initializes a new instance of the <see cref="DasmCommand"/> class.
    /// </summary>
    public DasmCommand()
        : base("dasm", "Disassemble memory contents")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["d", "disasm", "u", "unassemble"];

    /// <inheritdoc/>
    public override string Usage => "dasm [address] [length|--instructions=N|--range end_address]";

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Disassembler is null)
        {
            return CommandResult.Error("No disassembler attached to debug context.");
        }

        // Parse options
        var options = ParseOptions(debugContext, args);

        if (!options.Success)
        {
            return CommandResult.Error(options.ErrorMessage ?? "Invalid arguments.");
        }

        try
        {
            IReadOnlyList<DisassembledInstruction> instructions;

            if (options.UseRange)
            {
                instructions = debugContext.Disassembler.DisassembleRange(options.StartAddress, options.EndAddress);
            }
            else if (options.UseInstructionCount)
            {
                instructions = debugContext.Disassembler.DisassembleInstructions(options.StartAddress, options.InstructionCount);
            }
            else
            {
                instructions = debugContext.Disassembler.Disassemble(options.StartAddress, options.ByteCount);
            }

            FormatDisassembly(debugContext.Output, instructions);

            return CommandResult.Ok();
        }
        catch (ArgumentException ex)
        {
            return CommandResult.Error($"Disassembly error: {ex.Message}");
        }
    }

    private static void FormatDisassembly(TextWriter output, IReadOnlyList<DisassembledInstruction> instructions)
    {
        output.WriteLine($"Disassembly: {instructions.Count} instruction(s)");
        output.WriteLine();

        foreach (var instruction in instructions)
        {
            output.WriteLine($"${instruction.Address:X4}:  {instruction.FormatBytes(),-12} {instruction.FormatInstruction()}");
        }
    }

    private static DisasmOptions ParseOptions(IDebugContext debugContext, string[] args)
    {
        var options = new DisasmOptions();

        // Default to current PC if available, otherwise 0
        options.StartAddress = debugContext.Cpu?.GetPC() ?? 0;
        options.InstructionCount = DefaultInstructionCount;
        options.ByteCount = 32;
        options.UseInstructionCount = true; // Default mode
        options.Success = true;

        uint? firstAddress = null;
        uint? secondAddress = null;

        foreach (var arg in args)
        {
            if (arg.StartsWith("--instructions=", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = arg["--instructions=".Length..];
                if (TryParseNumber(valueStr, out int count) && count > 0)
                {
                    options.InstructionCount = count;
                    options.UseInstructionCount = true;
                    options.UseRange = false;
                }
                else
                {
                    options.Success = false;
                    options.ErrorMessage = $"Invalid instruction count: '{valueStr}'";
                    return options;
                }
            }
            else if (arg.StartsWith("--bytes=", StringComparison.OrdinalIgnoreCase))
            {
                var valueStr = arg["--bytes=".Length..];
                if (TryParseNumber(valueStr, out int count) && count > 0)
                {
                    options.ByteCount = count;
                    options.UseInstructionCount = false;
                    options.UseRange = false;
                }
                else
                {
                    options.Success = false;
                    options.ErrorMessage = $"Invalid byte count: '{valueStr}'";
                    return options;
                }
            }
            else if (arg.Equals("--range", StringComparison.OrdinalIgnoreCase))
            {
                options.UseRange = true;
                options.UseInstructionCount = false;
            }
            else if (TryParseAddress(arg, out uint address))
            {
                if (firstAddress is null)
                {
                    firstAddress = address;
                }
                else if (secondAddress is null)
                {
                    secondAddress = address;
                }
            }
            else
            {
                // Try as number (byte count or instruction count)
                if (TryParseNumber(arg, out int count) && count > 0)
                {
                    if (options.UseInstructionCount)
                    {
                        options.InstructionCount = count;
                    }
                    else
                    {
                        options.ByteCount = count;
                    }
                }
            }
        }

        // Process parsed addresses
        if (firstAddress is not null)
        {
            options.StartAddress = firstAddress.Value;
        }

        if (secondAddress is not null)
        {
            if (options.UseRange)
            {
                options.EndAddress = secondAddress.Value;
            }
            else
            {
                // Second positional could be byte count
                options.ByteCount = (int)secondAddress.Value;
                options.UseInstructionCount = false;
            }
        }

        // Validate range mode
        if (options.UseRange && options.EndAddress <= options.StartAddress)
        {
            options.EndAddress = options.StartAddress + 32; // Default range
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

        // Must have $ or 0x prefix to be treated as address to avoid ambiguity with counts
        return false;
    }

    private static bool TryParseNumber(string value, out int result)
    {
        result = 0;

        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            return int.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return int.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private sealed class DisasmOptions
    {
        public uint StartAddress { get; set; }

        public uint EndAddress { get; set; }

        public int ByteCount { get; set; }

        public int InstructionCount { get; set; }

        public bool UseRange { get; set; }

        public bool UseInstructionCount { get; set; }

        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }
    }
}