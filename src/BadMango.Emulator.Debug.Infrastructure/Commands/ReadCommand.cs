// <copyright file="ReadCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Reads memory with side effects (like a real bus read).
/// </summary>
/// <remarks>
/// <para>
/// Performs a side-effectful read from the memory bus, which will trigger soft
/// switches and I/O device behavior. Output is in hex format only.
/// </para>
/// <para>
/// This is distinct from <c>peek</c> which uses DebugRead intent and avoids side effects.
/// Use <c>read</c> when you want to test actual hardware behavior.
/// </para>
/// <para>
/// <strong>Side Effects:</strong> May trigger soft switches, I/O device state changes,
/// and timing-sensitive operations.
/// </para>
/// </remarks>
public sealed class ReadCommand : CommandHandlerBase
{
    private const int DefaultByteCount = 1;
    private const int MaxByteCount = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadCommand"/> class.
    /// </summary>
    public ReadCommand()
        : base("read", "Read memory with side effects (hex output)")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["rd"];

    /// <inheritdoc/>
    public override string Usage => "read <address> [count]";

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
            return CommandResult.Error("Address required. Usage: read <address> [count]");
        }

        if (!TryParseAddress(args[0], out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[0]}'. Use hex format ($1234 or 0x1234) or decimal.");
        }

        int count = DefaultByteCount;
        if (args.Length > 1 && !TryParseCount(args[1], out count))
        {
            return CommandResult.Error($"Invalid count: '{args[1]}'. Expected a positive integer.");
        }

        count = Math.Clamp(count, 1, MaxByteCount);

        var bus = debugContext.Bus;
        var pageSize = 1 << bus.PageShift;
        uint memorySize = (uint)(bus.PageCount * pageSize);

        if (address >= memorySize)
        {
            return CommandResult.Error($"Address ${address:X4} is out of range (memory size: ${memorySize:X4}).");
        }

        // Adjust count if it would exceed memory bounds
        if (address + (uint)count > memorySize)
        {
            count = (int)(memorySize - address);
        }

        debugContext.Output.WriteLine($"Reading {count} byte(s) from ${address:X4} (with side effects):");
        debugContext.Output.WriteLine();

        var bytes = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            byte value = ReadByte(bus, address + (uint)i);
            bytes.Add($"{value:X2}");
        }

        // Output in groups of 16 bytes per line
        for (int i = 0; i < bytes.Count; i += 16)
        {
            uint lineAddr = address + (uint)i;
            int lineCount = Math.Min(16, bytes.Count - i);
            string hexLine = string.Join(" ", bytes.Skip(i).Take(lineCount));
            debugContext.Output.WriteLine($"${lineAddr:X4}: {hexLine}");
        }

        return CommandResult.Ok();
    }

    private static byte ReadByte(IMemoryBus bus, uint address)
    {
        // Use DataRead intent for side-effectful read (not DebugRead)
        var access = new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None); // No NoSideEffects flag

        var result = bus.TryRead8(access);
        return result.Ok ? result.Value : (byte)0xFF;
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

    private static bool TryParseCount(string value, out int result)
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
}