// <copyright file="PrintCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Text;
using System.Text.RegularExpressions;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Devices;

/// <summary>
/// Command to write text directly to the screen buffer.
/// </summary>
/// <remarks>
/// <para>
/// This command writes text to the emulated video memory at a specified row and column,
/// or at the current cursor position if not specified. The renderer will pick up
/// the changes on its next refresh cycle.
/// </para>
/// <para>
/// The command supports C-style escape sequences for 7-bit ASCII codes:
/// </para>
/// <list type="bullet">
/// <item><description>\n - Line feed (0x0A)</description></item>
/// <item><description>\r - Carriage return (0x0D)</description></item>
/// <item><description>\t - Tab (0x09)</description></item>
/// <item><description>\\ - Backslash (0x5C)</description></item>
/// <item><description>\" - Double quote (0x22)</description></item>
/// <item><description>\xNN - Hex character code</description></item>
/// </list>
/// </remarks>
[DeviceDebugCommand]
public sealed partial class PrintCommand : CommandHandlerBase, ICommandHelp
{
    private const ushort TextPage1Base = 0x0400;
    private const int TextRows = 24;
    private const int TextColumns = 40;

    // Pre-computed row addresses for text page 1
    private static readonly ushort[] TextRowAddresses = ComputeTextRowAddresses();

    /// <summary>
    /// Initializes a new instance of the <see cref="PrintCommand"/> class.
    /// </summary>
    public PrintCommand()
        : base("print", "Write text to screen buffer")
    {
    }

    /// <inheritdoc/>
    public string Synopsis => "print [<row> <col>] \"<text>\"";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Writes text directly to the emulated video memory at a specified row and column. " +
        "If row and column are omitted, text is written at the current cursor position. " +
        "The text is converted to screen codes and written to the text page buffer.\n\n" +
        "Supported escape sequences:\n" +
        "  \\n  - Line feed (0x0A)\n" +
        "  \\r  - Carriage return (0x0D)\n" +
        "  \\t  - Tab (0x09)\n" +
        "  \\\\  - Backslash (0x5C)\n" +
        "  \\\"  - Double quote (0x22)\n" +
        "  \\xNN - Hex character code (e.g., \\x41 for 'A')";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("row", null, "int", "Row number (0-23)", null),
        new("col", null, "int", "Column number (0-39)", null),
        new("text", null, "string", "Text to display (in quotes)", null),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "print 0 0 \"HELLO WORLD\"         Write at top-left",
        "print 10 15 \"CENTERED\"          Write at row 10, column 15",
        "print \"TEXT\"                    Write at current cursor position",
        "print 5 0 \"LINE1\\nLINE2\"        Write with line feed",
        "print 0 0 \"\\xC1PPLE\"            Write using hex escape (uppercase A)",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Writes to video memory. The renderer will display the text on next refresh.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["plot", "hplot", "video", "poke"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Machine is null)
        {
            return CommandResult.Error("No machine attached to debug context.");
        }

        // Parse arguments: either "text" or <row> <col> "text"
        int row = 0;
        int col = 0;
        string text;

        if (args.Length == 0)
        {
            return CommandResult.Error("Usage: print [<row> <col>] \"<text>\"");
        }

        // Check if first arg is a number (row,col mode) or a string (text only mode)
        if (args.Length >= 3 && int.TryParse(args[0], out int parsedRow))
        {
            // Row, col, text mode
            if (!int.TryParse(args[1], out int parsedCol))
            {
                return CommandResult.Error($"Invalid column number: {args[1]}");
            }

            row = parsedRow;
            col = parsedCol;
            text = string.Join(" ", args.Skip(2));
        }
        else
        {
            // Text only mode - use row 0, col 0 for now (cursor position would require state)
            text = string.Join(" ", args);
        }

        // Remove surrounding quotes if present
        if (text.StartsWith('"') && text.EndsWith('"') && text.Length >= 2)
        {
            text = text[1..^1];
        }

        // Validate row and column
        if (row < 0 || row >= TextRows)
        {
            return CommandResult.Error($"Row must be between 0 and {TextRows - 1}.");
        }

        if (col < 0 || col >= TextColumns)
        {
            return CommandResult.Error($"Column must be between 0 and {TextColumns - 1}.");
        }

        // Process escape sequences
        string processed = ProcessEscapeSequences(text);

        // Convert to screen codes and write to memory
        int written = WriteToScreen(debugContext, processed, row, col);

        return CommandResult.Ok($"Wrote {written} characters at row {row}, column {col}.");
    }

    private static ushort[] ComputeTextRowAddresses()
    {
        var addresses = new ushort[TextRows];
        for (int row = 0; row < TextRows; row++)
        {
            int group = row / 8;
            int offset = row % 8;
            addresses[row] = (ushort)(TextPage1Base + (offset * 128) + (group * 40));
        }

        return addresses;
    }

    private static string ProcessEscapeSequences(string input)
    {
        var sb = new StringBuilder(input.Length);
        int i = 0;

        while (i < input.Length)
        {
            if (input[i] == '\\' && i + 1 < input.Length)
            {
                char next = input[i + 1];
                switch (next)
                {
                    case 'n':
                        sb.Append('\n');
                        i += 2;
                        break;
                    case 'r':
                        sb.Append('\r');
                        i += 2;
                        break;
                    case 't':
                        sb.Append('\t');
                        i += 2;
                        break;
                    case '\\':
                        sb.Append('\\');
                        i += 2;
                        break;
                    case '"':
                        sb.Append('"');
                        i += 2;
                        break;
                    case 'x':
                        // Hex escape \xNN - need i + 4 total chars (\xNN)
                        if (i + 4 <= input.Length)
                        {
                            var hex = input.AsSpan(i + 2, 2);
                            if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out byte value))
                            {
                                sb.Append((char)value);
                                i += 4;
                                break;
                            }
                        }

                        sb.Append(input[i]);
                        i++;
                        break;
                    default:
                        sb.Append(input[i]);
                        i++;
                        break;
                }
            }
            else
            {
                sb.Append(input[i]);
                i++;
            }
        }

        return sb.ToString();
    }

    private static int WriteToScreen(IDebugContext debugContext, string text, int startRow, int startCol)
    {
        var bus = debugContext.Machine!.Bus;
        int row = startRow;
        int col = startCol;
        int written = 0;

        foreach (char c in text)
        {
            if (c == '\n')
            {
                row++;
                col = 0;
                if (row >= TextRows)
                {
                    break;
                }

                continue;
            }

            if (c == '\r')
            {
                col = 0;
                continue;
            }

            if (row >= TextRows || col >= TextColumns)
            {
                break;
            }

            // Convert ASCII to screen code (normal text = $80-$FF range)
            byte screenCode = AsciiToScreenCode(c);

            // Calculate address
            ushort address = (ushort)(TextRowAddresses[row] + col);

            // Write using debug access
            var access = new BusAccess(
                Address: address,
                Value: screenCode,
                WidthBits: 8,
                Mode: BusAccessMode.Atomic,
                EmulationFlag: true,
                Intent: AccessIntent.DebugWrite,
                SourceId: 0,
                Cycle: 0,
                Flags: AccessFlags.NoSideEffects);

            bus.Write8(in access, screenCode);
            written++;
            col++;

            // Wrap to next line if needed
            if (col >= TextColumns)
            {
                col = 0;
                row++;
            }
        }

        return written;
    }

    private static byte AsciiToScreenCode(char c)
    {
        // Convert ASCII to Pocket2e screen code
        // Normal characters are stored as $80-$FF (high bit set)
        int ascii = c;

        // Handle uppercase letters (A-Z = $41-$5A -> $C1-$DA)
        if (ascii >= 0x41 && ascii <= 0x5A)
        {
            return (byte)(ascii + 0x80);
        }

        // Handle lowercase letters (a-z = $61-$7A -> $E1-$FA)
        if (ascii >= 0x61 && ascii <= 0x7A)
        {
            return (byte)(ascii + 0x80);
        }

        // Handle numbers and most symbols ($20-$3F -> $A0-$BF)
        if (ascii >= 0x20 && ascii <= 0x3F)
        {
            return (byte)(ascii + 0x80);
        }

        // Handle special symbols ($5B-$60 -> $DB-$E0, $7B-$7F -> $FB-$FF)
        if (ascii >= 0x5B && ascii <= 0x7F)
        {
            return (byte)(ascii + 0x80);
        }

        // Control characters and others - return as-is with high bit
        return (byte)(ascii | 0x80);
    }
}