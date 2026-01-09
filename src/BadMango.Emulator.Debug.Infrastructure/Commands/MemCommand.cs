// <copyright file="MemCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;
using System.Text;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Displays a hex dump of memory contents.
/// </summary>
/// <remarks>
/// Shows memory contents in a traditional hex dump format with:
/// - Address column on the left
/// - Hex bytes in the middle (16 bytes per line)
/// - ASCII representation on the right.
/// </remarks>
public sealed class MemCommand : CommandHandlerBase, ICommandHelp
{
    private const int DefaultByteCount = 256;
    private const int BytesPerLine = 16;
    private const int MaxByteCount = 65536;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemCommand"/> class.
    /// </summary>
    public MemCommand()
        : base("mem", "Display memory contents as hex dump")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["m", "dump", "hexdump"];

    /// <inheritdoc/>
    public override string Usage => "mem <address> [length]";

    /// <inheritdoc/>
    public string Synopsis => "mem <address> [length]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays memory contents in traditional hex dump format with address column, " +
        "hex bytes (16 per line), and ASCII representation. Uses DebugRead intent for " +
        "side-effect-free access. Defaults to 256 bytes if length not specified. " +
        "Maximum displayable length is 65536 bytes.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "mem $300                 Display 256 bytes starting at $0300",
        "mem $800 64              Display 64 bytes starting at $0800",
        "mem 0x6000 $100          Display 256 bytes starting at $6000",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["peek", "poke", "read"];

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
            return CommandResult.Error("Address required. Usage: mem <address> [length]");
        }

        if (!TryParseAddress(args[0], out uint startAddress))
        {
            return CommandResult.Error($"Invalid address: '{args[0]}'. Use hex format ($1234 or 0x1234) or decimal.");
        }

        int byteCount = DefaultByteCount;
        if (args.Length > 1 && !TryParseLength(args[1], out byteCount))
        {
            return CommandResult.Error($"Invalid length: '{args[1]}'. Expected a positive integer.");
        }

        // Clamp byte count to valid range
        if (byteCount < 1)
        {
            byteCount = 1;
        }

        if (byteCount > MaxByteCount)
        {
            byteCount = MaxByteCount;
        }

        // Calculate memory size from bus page count
        uint memorySize = (uint)debugContext.Bus.PageCount << debugContext.Bus.PageShift;

        // Validate address range
        if (startAddress >= memorySize)
        {
            return CommandResult.Error($"Address ${startAddress:X4} is out of range (memory size: ${memorySize:X4}).");
        }

        // Adjust byte count if it would exceed memory bounds
        if (startAddress + (uint)byteCount > memorySize)
        {
            byteCount = (int)(memorySize - startAddress);
        }

        var faults = new List<(uint Address, BusFault Fault)>();
        FormatHexDump(debugContext.Output, debugContext.Bus, startAddress, byteCount, faults);

        // Report any faults at the end
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

    private static void FormatHexDump(TextWriter output, IMemoryBus bus, uint startAddress, int byteCount, List<(uint Address, BusFault Fault)> faults)
    {
        // Align start address to 16-byte boundary for clean display
        uint alignedStart = startAddress & 0xFFFFFFF0;
        uint endAddress = startAddress + (uint)byteCount;

        var hexBuilder = new StringBuilder(48); // 16 bytes * 3 chars each
        var asciiBuilder = new StringBuilder(16);

        output.WriteLine($"Memory dump: ${startAddress:X4} - ${endAddress - 1:X4} ({byteCount} bytes)");
        output.WriteLine();

        for (uint addr = alignedStart; addr < endAddress; addr += BytesPerLine)
        {
            hexBuilder.Clear();
            asciiBuilder.Clear();

            for (int offset = 0; offset < BytesPerLine; offset++)
            {
                uint currentAddr = addr + (uint)offset;

                if (currentAddr < startAddress || currentAddr >= endAddress)
                {
                    hexBuilder.Append("   ");
                    asciiBuilder.Append(' ');
                }
                else
                {
                    var result = ReadByteWithFault(bus, currentAddr);
                    if (result.Fault.IsFault)
                    {
                        faults.Add((currentAddr, result.Fault));
                        hexBuilder.Append("?? ");
                        asciiBuilder.Append('?');
                    }
                    else
                    {
                        byte value = result.Value;
                        hexBuilder.Append($"{value:X2} ");

                        // ASCII representation (printable chars only)
                        if (value >= 0x20 && value < 0x7F)
                        {
                            asciiBuilder.Append((char)value);
                        }
                        else
                        {
                            asciiBuilder.Append('.');
                        }
                    }
                }

                // Add extra space at 8-byte boundary
                if (offset == 7)
                {
                    hexBuilder.Append(' ');
                }
            }

            output.WriteLine($"${addr:X4}:  {hexBuilder} |{asciiBuilder}|");
        }
    }

    private static BusResult<byte> ReadByteWithFault(IMemoryBus bus, uint address)
    {
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

    private static bool TryParseLength(string value, out int result)
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