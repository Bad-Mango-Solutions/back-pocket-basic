// <copyright file="PeekCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

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
        "affecting emulation state. For side-effectful reads, use 'read' instead. " +
        "Addresses can be specified as hex ($C000, 0xC000), decimal, or soft switch " +
        "names registered by the current machine (e.g., SPEAKER, KBD, KBDSTRB).";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "peek $C000               Read keyboard data without triggering strobe",
        "peek KBD                  Same as above using soft switch name",
        "peek $300 16             Read 16 bytes starting at $0300",
        "peek 0x6000              Read a single byte from $6000",
        "peek SPEAKER              Peek at speaker address (no click)",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["read", "poke", "mem", "switches"];

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

        if (!AddressParser.TryParse(args[0], debugContext.Machine, out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[0]}'. Use {AddressParser.GetFormatDescription()}.");
        }

        int count = DefaultByteCount;
        if (args.Length > 1 && !AddressParser.TryParseCount(args[1], out count))
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

        var bytes = new List<string>(count);
        var faults = new List<(uint Address, BusFault Fault)>();

        for (int i = 0; i < count; i++)
        {
            var result = ReadByteWithFault(bus, address + (uint)i);
            if (result.Fault.IsFault)
            {
                faults.Add((address + (uint)i, result.Fault));
                bytes.Add("??"); // Indicate fault with ??
            }
            else
            {
                bytes.Add($"{result.Value:X2}");
            }
        }

        if (count == 1)
        {
            debugContext.Output.WriteLine($"${address:X4}: {bytes[0]}");
        }
        else
        {
            // Output in groups of 16 bytes per line
            for (int i = 0; i < bytes.Count; i += 16)
            {
                uint lineAddr = address + (uint)i;
                int lineCount = Math.Min(16, bytes.Count - i);
                string hexLine = string.Join(" ", bytes.Skip(i).Take(lineCount));
                debugContext.Output.WriteLine($"${lineAddr:X4}: {hexLine}");
            }
        }

        // Report any faults
        if (faults.Count > 0)
        {
            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine($"Bus faults encountered ({faults.Count}):");
            foreach (var (faultAddr, fault) in faults)
            {
                debugContext.Output.WriteLine($"  ${faultAddr:X4}: {FormatFault(fault)}");
            }
        }

        return CommandResult.Ok();
    }

    private static BusResult<byte> ReadByteWithFault(IMemoryBus bus, uint address)
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

        return bus.TryRead8(access);
    }

    private static string FormatFault(BusFault fault)
    {
        return fault.Kind switch
        {
            FaultKind.Unmapped => "Unmapped - no memory or device at this address",
            FaultKind.Permission => $"Permission denied - region {fault.RegionTag} does not allow read",
            FaultKind.Nx => "No execute - attempted instruction fetch from non-executable region",
            FaultKind.Misaligned => "Misaligned access",
            FaultKind.DeviceFault => "Device fault - device rejected the access",
            _ => $"Unknown fault kind: {fault.Kind}",
        };
    }
}