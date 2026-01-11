// <copyright file="HplotCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Devices;

/// <summary>
/// Command to plot hi-res graphics points or lines.
/// </summary>
/// <remarks>
/// <para>
/// This command draws hi-res graphics directly to the video memory.
/// Hi-res mode uses a 280×192 pixel grid stored in the $2000-$3FFF region.
/// </para>
/// <para>
/// The syntax follows the BASIC HPLOT convention:
/// HPLOT x,y - Plot single point.
/// HPLOT x1,y1 TO x2,y2 - Draw line.
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class HplotCommand : CommandHandlerBase, ICommandHelp
{
    private const ushort HiResPage1Base = 0x2000;
    private const int HiResWidth = 280;
    private const int HiResHeight = 192;

    // Pre-computed row addresses for hi-res page 1
    private static readonly ushort[] HiResRowAddresses = ComputeHiResRowAddresses();

    /// <summary>
    /// Initializes a new instance of the <see cref="HplotCommand"/> class.
    /// </summary>
    public HplotCommand()
        : base("hplot", "Draw hi-res graphics point or line")
    {
    }

    /// <inheritdoc/>
    public string Synopsis => "hplot <x> <y> [TO <x2> <y2>] [<color>]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Draws hi-res graphics by writing directly to the hi-res buffer at $2000-$3FFF. " +
        "Hi-res mode uses a 280×192 pixel grid with monochrome display.\n\n" +
        "Each byte contains 7 pixels (bits 0-6), with bit 7 used for color fringing.\n\n" +
        "The color parameter (0 or 1) sets or clears pixels:\n" +
        "  0 - Clear pixel (black)\n" +
        "  1 - Set pixel (white/green)\n\n" +
        "Use 'TO' keyword to draw lines, following BASIC HPLOT syntax.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("x", null, "int", "X coordinate (0-279)", null),
        new("y", null, "int", "Y coordinate (0-191)", null),
        new("TO", null, "keyword", "Keyword to indicate line drawing", null),
        new("x2", null, "int", "End X coordinate for line (optional)", null),
        new("y2", null, "int", "End Y coordinate for line (optional)", null),
        new("color", null, "int", "Color (0=black, 1=white, default=1)", "1"),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "hplot 140 96               Plot point at center (white)",
        "hplot 0 0 TO 279 191       Draw diagonal line",
        "hplot 0 96 TO 279 96       Draw horizontal line at center",
        "hplot 140 0 TO 140 191     Draw vertical line at center",
        "hplot 100 100 0            Plot black point (erase)",
        "hplot 0 0 TO 50 50 0       Draw black line (erase)",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Writes to hi-res video memory. Graphics appear on next renderer refresh.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["plot", "print", "video", "hgr"];

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

        // Parse arguments: x y [color] OR x1 y1 TO x2 y2 [color]
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: hplot <x> <y> [TO <x2> <y2>] [<color>]");
        }

        if (!int.TryParse(args[0], out int x1))
        {
            return CommandResult.Error($"Invalid X coordinate: {args[0]}");
        }

        if (!int.TryParse(args[1], out int y1))
        {
            return CommandResult.Error($"Invalid Y coordinate: {args[1]}");
        }

        int x2 = x1;
        int y2 = y1;
        int color = 1; // Default to white/set

        // Look for TO keyword
        int toIndex = Array.FindIndex(args, a => a.Equals("TO", StringComparison.OrdinalIgnoreCase));

        if (toIndex >= 2 && args.Length >= toIndex + 3)
        {
            // Line mode: x1 y1 TO x2 y2 [color]
            if (!int.TryParse(args[toIndex + 1], out x2))
            {
                return CommandResult.Error($"Invalid X2 coordinate: {args[toIndex + 1]}");
            }

            if (!int.TryParse(args[toIndex + 2], out y2))
            {
                return CommandResult.Error($"Invalid Y2 coordinate: {args[toIndex + 2]}");
            }

            // Check for color after TO x2 y2
            if (args.Length > toIndex + 3)
            {
                if (!int.TryParse(args[toIndex + 3], out color))
                {
                    return CommandResult.Error($"Invalid color: {args[toIndex + 3]}");
                }
            }
        }
        else if (args.Length > 2)
        {
            // Point mode with color: x y color
            if (!int.TryParse(args[2], out color))
            {
                return CommandResult.Error($"Invalid color: {args[2]}");
            }
        }

        // Validate coordinates
        if (x1 < 0 || x1 >= HiResWidth || x2 < 0 || x2 >= HiResWidth)
        {
            return CommandResult.Error($"X coordinates must be between 0 and {HiResWidth - 1}.");
        }

        if (y1 < 0 || y1 >= HiResHeight || y2 < 0 || y2 >= HiResHeight)
        {
            return CommandResult.Error($"Y coordinates must be between 0 and {HiResHeight - 1}.");
        }

        if (color < 0 || color > 1)
        {
            return CommandResult.Error("Color must be 0 (black) or 1 (white).");
        }

        // Draw point or line
        if (x1 == x2 && y1 == y2)
        {
            HplotPoint(debugContext, x1, y1, color == 1);
            return CommandResult.Ok($"Plotted point at ({x1},{y1}) in {(color == 1 ? "white" : "black")}.");
        }
        else
        {
            int points = DrawLine(debugContext, x1, y1, x2, y2, color == 1);
            return CommandResult.Ok($"Drew line from ({x1},{y1}) to ({x2},{y2}) in {(color == 1 ? "white" : "black")} ({points} points).");
        }
    }

    private static ushort[] ComputeHiResRowAddresses()
    {
        var addresses = new ushort[HiResHeight];
        for (int row = 0; row < HiResHeight; row++)
        {
            int group = row / 64;          // 0, 1, or 2
            int subRow = (row % 64) / 8;   // 0-7
            int scanLine = row % 8;        // 0-7
            addresses[row] = (ushort)(HiResPage1Base + (scanLine * 1024) + (subRow * 128) + (group * 40));
        }

        return addresses;
    }

    private static void HplotPoint(IDebugContext debugContext, int x, int y, bool set)
    {
        var bus = debugContext.Machine!.Bus;

        // Calculate byte and bit within byte
        int byteOffset = x / 7;
        int bitPosition = x % 7;

        // Calculate address
        ushort address = (ushort)(HiResRowAddresses[y] + byteOffset);

        // Read current value
        var readAccess = new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Atomic,
            EmulationFlag: true,
            Intent: AccessIntent.DebugRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);

        byte current = bus.Read8(readAccess);

        // Modify the appropriate bit
        byte newValue;
        if (set)
        {
            newValue = (byte)(current | (1 << bitPosition));
        }
        else
        {
            newValue = (byte)(current & ~(1 << bitPosition));
        }

        // Write new value
        var writeAccess = new BusAccess(
            Address: address,
            Value: newValue,
            WidthBits: 8,
            Mode: BusAccessMode.Atomic,
            EmulationFlag: true,
            Intent: AccessIntent.DebugWrite,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.NoSideEffects);

        bus.Write8(in writeAccess, newValue);
    }

    private static int DrawLine(IDebugContext debugContext, int x0, int y0, int x1, int y1, bool set)
    {
        // Bresenham's line algorithm
        int points = 0;

        int dx = Math.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            HplotPoint(debugContext, x0, y0, set);
            points++;

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return points;
    }
}