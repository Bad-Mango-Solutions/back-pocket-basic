// <copyright file="MemCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;
using System.Text;

using Core.Interfaces;

/// <summary>
/// Displays a hex dump of memory contents.
/// </summary>
/// <remarks>
/// Shows memory contents in a traditional hex dump format with:
/// - Address column on the left
/// - Hex bytes in the middle (16 bytes per line)
/// - ASCII representation on the right.
/// </remarks>
public sealed class MemCommand : CommandHandlerBase
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
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Memory is null)
        {
            return CommandResult.Error("No memory attached to debug context.");
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

        // Validate address range
        if (startAddress >= debugContext.Memory.Size)
        {
            return CommandResult.Error($"Address ${startAddress:X4} is out of range (memory size: ${debugContext.Memory.Size:X4}).");
        }

        // Adjust byte count if it would exceed memory bounds
        if (startAddress + (uint)byteCount > debugContext.Memory.Size)
        {
            byteCount = (int)(debugContext.Memory.Size - startAddress);
        }

        FormatHexDump(debugContext.Output, debugContext.Memory, startAddress, byteCount);

        return CommandResult.Ok();
    }

    private static void FormatHexDump(TextWriter output, IMemory memory, uint startAddress, int byteCount)
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
                    byte value = memory.Read(currentAddr);
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

                // Add extra space at 8-byte boundary
                if (offset == 7)
                {
                    hexBuilder.Append(' ');
                }
            }

            output.WriteLine($"${addr:X4}:  {hexBuilder} |{asciiBuilder}|");
        }
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