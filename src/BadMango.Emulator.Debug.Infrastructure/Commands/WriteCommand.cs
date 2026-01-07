// <copyright file="WriteCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Writes to memory with side effects (like a real bus write).
/// </summary>
/// <remarks>
/// <para>
/// Performs a side-effectful write to the memory bus, which will trigger soft
/// switches and I/O device behavior.
/// </para>
/// <para>
/// This is distinct from <c>poke</c> which uses DebugWrite intent and avoids side effects.
/// Use <c>write</c> when you want to test actual hardware behavior.
/// </para>
/// <para>
/// <strong>Side Effects:</strong> May trigger soft switches, I/O device state changes,
/// and timing-sensitive operations.
/// </para>
/// </remarks>
public sealed class WriteCommand : CommandHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriteCommand"/> class.
    /// </summary>
    public WriteCommand()
        : base("write", "Write to memory with side effects")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["wr"];

    /// <inheritdoc/>
    public override string Usage => "write <address> <byte> [byte...]";

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (!debugContext.IsBusAttached || debugContext.Bus is null)
        {
            return CommandResult.Error("No bus attached. This command requires a bus-based system.");
        }

        if (args.Length == 0)
        {
            return CommandResult.Error("Address required. Usage: write <address> <byte> [byte...]");
        }

        if (!TryParseAddress(args[0], out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[0]}'. Use hex format ($1234 or 0x1234) or decimal.");
        }

        if (args.Length < 2)
        {
            return CommandResult.Error("At least one byte value required. Usage: write <address> <byte> [byte...]");
        }

        // Parse byte values
        var bytes = new List<byte>();
        for (int i = 1; i < args.Length; i++)
        {
            if (!TryParseByteValue(args[i], out byte value))
            {
                return CommandResult.Error($"Invalid byte value: '{args[i]}'. Use hex (ab, $AB, 0xAB) or decimal (0-255).");
            }

            bytes.Add(value);
        }

        var bus = debugContext.Bus;
        var pageSize = 1 << bus.PageShift;
        uint memorySize = (uint)(bus.PageCount * pageSize);

        if (address >= memorySize)
        {
            return CommandResult.Error($"Address ${address:X4} is out of range (memory size: ${memorySize:X4}).");
        }

        if (address + (uint)bytes.Count > memorySize)
        {
            return CommandResult.Error($"Write would exceed memory bounds. Start: ${address:X4}, Count: {bytes.Count}");
        }

        // Write bytes with side effects
        for (int i = 0; i < bytes.Count; i++)
        {
            WriteByte(bus, address + (uint)i, bytes[i]);
        }

        var hexValues = string.Join(" ", bytes.Select(b => $"{b:X2}"));
        debugContext.Output.WriteLine($"Wrote {bytes.Count} byte(s) to ${address:X4} (with side effects):");
        debugContext.Output.WriteLine($"  {hexValues}");

        return CommandResult.Ok();
    }

    private static void WriteByte(IMemoryBus bus, uint address, byte value)
    {
        // Use DataWrite intent for side-effectful write (not DebugWrite)
        var access = new BusAccess(
            Address: address,
            Value: value,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataWrite,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None); // No special flags

        bus.TryWrite8(access, value);
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

        // For 1-2 character values that are valid hex, treat as hex.
        // Values like "ab", "ff", "1a" are unambiguously hex.
        // Values like "5" or "99" are also parsed as hex for consistency with
        // traditional monitor-style commands. Users can use decimal prefix or
        // longer values for decimal input.
        if (value.Length <= 2 && value.All(char.IsAsciiHexDigit))
        {
            return byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        }

        // Try as decimal
        return byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}