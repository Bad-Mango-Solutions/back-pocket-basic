// <copyright file="PokeCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Writes one or more bytes to memory.
/// </summary>
/// <remarks>
/// <para>
/// Supports multiple modes of operation:
/// - Single byte: poke $1234 AB or poke $1234 $AB.
/// - Multiple bytes: poke $1234 AB CD EF or poke $1234 $AB $CD $EF.
/// - Byte sequence: poke $1234 "Hello" (writes ASCII bytes).
/// - Interactive mode: poke $1234 -i (enter bytes interactively).
/// </para>
/// <para>
/// Byte values can be specified with or without a $ or 0x prefix. Values without
/// a prefix are treated as hexadecimal.
/// </para>
/// <para>
/// In interactive mode, enter hex bytes separated by spaces. Use $addr: prefix to
/// change the write address. A blank line ends interactive mode.
/// </para>
/// </remarks>
public sealed class PokeCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PokeCommand"/> class.
    /// </summary>
    public PokeCommand()
        : base("poke", "Write bytes to memory")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["pk"];

    /// <inheritdoc/>
    public override string Usage => "poke <address> <byte> [byte...]  or  poke <address> -i  or  poke <address> \"string\"";

    /// <inheritdoc/>
    public string Synopsis => "poke <address> <byte> [byte...] | -i | \"string\"";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Writes bytes to memory using DebugWrite intent, bypassing ROM write protection " +
        "and I/O handlers. Supports single bytes, multiple bytes, ASCII strings, and " +
        "interactive mode (-i). Values can be hex (ab, $AB, 0xAB) or decimal. This is " +
        "a side-effect-free write; use 'write' for hardware-like writes. " +
        "Addresses can be specified as hex ($C000, 0xC000), decimal, or soft switch " +
        "names registered by the current machine (e.g., SPEAKER, KBDSTRB).";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("-i", "--interactive", "flag", "Enter interactive mode for continuous input", "off"),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "poke $300 A9 00 60        Write LDA #$00; RTS at $0300",
        "poke $F000 \"Hello\"        Write ASCII string at $F000 (bypasses ROM)",
        "poke $800 -i              Start interactive mode at $0800",
        "poke KBD 00               Poke keyboard address using soft switch name",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Modifies memory content. Uses DebugWrite intent which bypasses ROM write " +
        "protection and does not trigger I/O side effects.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["write", "peek", "mem", "switches"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Bus is null)
        {
            return CommandResult.Error("No memory bus attached to debug context.");
        }

        if (args.Length == 0)
        {
            return CommandResult.Error("Address required. Usage: poke <address> <byte> [byte...] or poke <address> -i");
        }

        if (!AddressParser.TryParse(args[0], debugContext.Machine, out uint startAddress))
        {
            return CommandResult.Error($"Invalid address: '{args[0]}'. Use {AddressParser.GetFormatDescription()}.");
        }

        // Check for interactive mode
        if (args.Length >= 2 && (args[1].Equals("-i", StringComparison.OrdinalIgnoreCase) ||
                                  args[1].Equals("--interactive", StringComparison.OrdinalIgnoreCase)))
        {
            return ExecuteInteractiveMode(debugContext, startAddress);
        }

        if (args.Length < 2)
        {
            return CommandResult.Error("Address and at least one byte value required. Usage: poke <address> <byte> [byte...] or poke <address> -i");
        }

        // Check if we have a string argument
        if (args.Length == 2 && args[1].StartsWith("\"", StringComparison.Ordinal))
        {
            return WriteString(debugContext, startAddress, args[1]);
        }

        // Parse byte values (allow unprefixed hex values)
        var bytes = new List<byte>();
        for (int i = 1; i < args.Length; i++)
        {
            if (!TryParseByteValue(args[i], out byte value))
            {
                return CommandResult.Error($"Invalid byte value: '{args[i]}'. Use hex (ab, $AB, 0xAB) or decimal (0-255).");
            }

            bytes.Add(value);
        }

        // Calculate memory size from bus page count
        uint memorySize = (uint)debugContext.Bus.PageCount << debugContext.Bus.PageShift;

        // Validate address range
        if (startAddress >= memorySize)
        {
            return CommandResult.Error($"Address ${startAddress:X4} is out of range (memory size: ${memorySize:X4}).");
        }

        if (startAddress + (uint)bytes.Count > memorySize)
        {
            return CommandResult.Error($"Write would exceed memory bounds. Start: ${startAddress:X4}, Count: {bytes.Count}, Memory size: ${memorySize:X4}");
        }

        // Write bytes
        WriteBytes(debugContext, startAddress, bytes);

        return CommandResult.Ok();
    }

    private static CommandResult ExecuteInteractiveMode(IDebugContext context, uint startAddress)
    {
        if (context.Input is null)
        {
            return CommandResult.Error("Interactive mode not available (no input reader).");
        }

        if (context.Bus is null)
        {
            return CommandResult.Error("No memory bus attached.");
        }

        // Calculate memory size from bus page count
        uint memorySize = (uint)context.Bus.PageCount << context.Bus.PageShift;

        context.Output.WriteLine($"Interactive poke mode starting at ${startAddress:X4}");
        context.Output.WriteLine("Enter hex bytes (space-separated). Use $addr: to change address. Blank line to finish.");
        context.Output.WriteLine();

        uint currentAddress = startAddress;
        int totalBytesWritten = 0;
        var allFaults = new List<(uint Address, BusFault Fault)>();

        while (true)
        {
            context.Output.Write($"${currentAddress:X4}: ");

            var line = context.Input.ReadLine();

            // End on null, empty, or whitespace-only line
            if (line is null || string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            // Check for address prefix (e.g., "$1234: ab cd ef")
            var trimmedLine = line.Trim();
            if (TryParseAddressPrefix(trimmedLine, context.Machine, out uint newAddress, out string remainingBytes))
            {
                currentAddress = newAddress;
                context.Output.WriteLine($"  Address changed to ${currentAddress:X4}");

                // If there are bytes after the address prefix, process them
                if (!string.IsNullOrWhiteSpace(remainingBytes))
                {
                    trimmedLine = remainingBytes;
                }
                else
                {
                    continue;
                }
            }

            var parts = trimmedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var bytes = new List<byte>();
            bool hasError = false;

            foreach (var part in parts)
            {
                if (TryParseByteValue(part, out byte value))
                {
                    bytes.Add(value);
                }
                else
                {
                    context.Error.WriteLine($"Invalid byte value: '{part}'. Skipping.");
                    hasError = true;
                }
            }

            if (bytes.Count > 0)
            {
                // Validate address range
                if (currentAddress + (uint)bytes.Count > memorySize)
                {
                    context.Error.WriteLine($"Write would exceed memory bounds at ${currentAddress:X4}. Stopping.");
                    break;
                }

                // Write bytes and track faults
                var faults = new List<(uint Address, BusFault Fault)>();
                for (int i = 0; i < bytes.Count; i++)
                {
                    var result = WriteByteWithFault(context.Bus, currentAddress + (uint)i, bytes[i]);
                    if (result.Failed)
                    {
                        faults.Add((currentAddress + (uint)i, result.Fault));
                        allFaults.Add((currentAddress + (uint)i, result.Fault));
                    }
                }

                var hexValues = string.Join(" ", bytes.Select(b => $"{b:X2}"));
                context.Output.WriteLine($"  Wrote: {hexValues}");

                // Report faults immediately for this line
                if (faults.Count > 0)
                {
                    foreach (var (faultAddr, fault) in faults)
                    {
                        context.Output.WriteLine($"  Fault at ${faultAddr:X4}: {FormatFault(fault)}");
                    }
                }

                currentAddress += (uint)bytes.Count;
                totalBytesWritten += bytes.Count;
            }
            else if (hasError)
            {
                // Had errors but no valid bytes - continue to next line
                continue;
            }
        }

        context.Output.WriteLine();
        if (totalBytesWritten > 0)
        {
            context.Output.WriteLine($"Interactive mode complete. Wrote {totalBytesWritten} byte(s) from ${startAddress:X4} to ${currentAddress - 1:X4}.");
            if (allFaults.Count > 0)
            {
                context.Output.WriteLine($"  ({allFaults.Count} fault(s) encountered)");
            }
        }
        else
        {
            context.Output.WriteLine("Interactive mode complete. No bytes written.");
        }

        return CommandResult.Ok();
    }

    private static bool TryParseAddressPrefix(string line, IMachine? machine, out uint address, out string remainingBytes)
    {
        address = 0;
        remainingBytes = string.Empty;

        // Look for pattern like "$1234:" or "0x1234:"
        int colonIndex = line.IndexOf(':', StringComparison.Ordinal);
        if (colonIndex < 1)
        {
            return false;
        }

        var addressPart = line[..colonIndex].Trim();
        if (!AddressParser.TryParse(addressPart, machine, out address))
        {
            return false;
        }

        remainingBytes = line[(colonIndex + 1)..].Trim();
        return true;
    }

    private static CommandResult WriteString(IDebugContext context, uint startAddress, string quotedString)
    {
        // Remove quotes
        var content = quotedString.Trim('"');
        var bytes = System.Text.Encoding.ASCII.GetBytes(content);

        if (context.Bus is null)
        {
            return CommandResult.Error("No memory bus attached.");
        }

        // Calculate memory size from bus page count
        uint memorySize = (uint)context.Bus.PageCount << context.Bus.PageShift;

        if (startAddress + (uint)bytes.Length > memorySize)
        {
            return CommandResult.Error($"Write would exceed memory bounds.");
        }

        WriteBytes(context, startAddress, bytes);

        return CommandResult.Ok();
    }

    private static void WriteBytes(IDebugContext context, uint startAddress, IReadOnlyList<byte> bytes)
    {
        if (context.Bus is null)
        {
            return;
        }

        var faults = new List<(uint Address, BusFault Fault)>();

        for (int i = 0; i < bytes.Count; i++)
        {
            var result = WriteByteWithFault(context.Bus, startAddress + (uint)i, bytes[i]);
            if (result.Failed)
            {
                faults.Add((startAddress + (uint)i, result.Fault));
            }
        }

        // Display confirmation
        context.Output.WriteLine($"Wrote {bytes.Count} byte(s) starting at ${startAddress:X4}:");

        // Show what was written
        var hexValues = string.Join(" ", bytes.Select(b => $"{b:X2}"));
        context.Output.WriteLine($"  {hexValues}");

        // Report any faults
        if (faults.Count > 0)
        {
            context.Output.WriteLine();
            context.Output.WriteLine($"Bus faults encountered ({faults.Count}):");
            foreach (var (faultAddr, fault) in faults)
            {
                context.Output.WriteLine($"  ${faultAddr:X4}: {FormatFault(fault)}");
            }
        }
    }

    private static BusResult WriteByteWithFault(IMemoryBus bus, uint address, byte value)
    {
        var access = new BusAccess(
            Address: address,
            Value: value,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugWrite,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);

        return bus.TryWrite8(access, value);
    }

    private static string FormatFault(BusFault fault)
    {
        return fault.Kind switch
        {
            FaultKind.Unmapped => "Unmapped - no memory or device at this address",
            FaultKind.Permission => $"Permission denied - region {fault.RegionTag} does not allow write",
            FaultKind.Nx => "No execute - this fault should not occur for writes",
            FaultKind.Misaligned => "Misaligned access",
            FaultKind.DeviceFault => "Device fault - device rejected the write",
            _ => $"Unknown fault kind: {fault.Kind}",
        };
    }

    /// <summary>
    /// Parses a byte value that can be in various formats:
    /// - Unprefixed hex: "ab", "AB", "ff"
    /// - Dollar-prefixed hex: "$ab", "$AB"
    /// - 0x-prefixed hex: "0xab", "0xAB"
    /// - Decimal: "171" (0-255).
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <param name="result">The parsed byte value.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    private static bool TryParseByteValue(string value, out byte result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Dollar-prefixed hex
        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            return byte.TryParse(value[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        // 0x-prefixed hex
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return byte.TryParse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        // Try as unprefixed hex first (if it looks like hex - contains a-f)
        if (value.Length <= 2 && IsValidHexString(value))
        {
            return byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        // Try as decimal
        return byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static bool IsValidHexString(string value)
    {
        return value.All(char.IsAsciiHexDigit);
    }
}