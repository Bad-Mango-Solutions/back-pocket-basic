// <copyright file="PeekCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Reads memory without side effects (debug/peek access).
/// </summary>
/// <remarks>
/// <para>
/// Performs a side-effect-free read from memory using DebugRead intent. This will
/// not trigger soft switches or I/O device behavior. Output is in hex format only.
/// </para>
/// <para>
/// Use <c>peek</c> when you want to inspect memory without affecting emulation state.
/// For side-effectful reads (like actual hardware behavior), use <c>read</c> instead.
/// </para>
/// <para>
/// <strong>No Side Effects:</strong> This command does not modify any emulation state.
/// </para>
/// </remarks>
public sealed class PeekCommand : CommandHandlerBase, ICommandHelp
{
    private const int DefaultByteCount = 1;
    private const int MaxByteCount = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeekCommand"/> class.
    /// </summary>
    public PeekCommand()
        : base("peek", "Read memory without side effects (hex output)")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["p"];

    /// <inheritdoc/>
    public override string Usage => "peek <address> [count]";

    /// <inheritdoc/>
    public string Synopsis => "peek <address> [count]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Performs a side-effect-free read from memory using DebugRead intent. This " +
        "bypasses I/O handlers and soft switches, returning the raw memory value. " +
        "Output is in hex format only. Use this for safe memory inspection without " +
        "affecting emulation state. For side-effectful reads, use 'read' instead.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "peek $C000               Read keyboard data without triggering strobe",
        "peek $300 16             Read 16 bytes starting at $0300",
        "peek 0x6000              Read a single byte from $6000",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["read", "poke", "mem"];

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
            return CommandResult.Error("Address required. Usage: peek <address> [count]");
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

        if (count == 1)
        {
            byte value = ReadByte(bus, address);
            debugContext.Output.WriteLine($"${address:X4}: ${value:X2}");
        }
        else
        {
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
        }

        return CommandResult.Ok();
    }

    private static byte ReadByte(IMemoryBus bus, uint address)
    {
        // Use DebugRead intent for side-effect-free read
        var access = new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);

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