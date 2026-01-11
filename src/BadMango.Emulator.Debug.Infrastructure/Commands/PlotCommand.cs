// <copyright file="PlotCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Devices;

/// <summary>
/// Command to plot lo-res graphics points or lines.
/// </summary>
/// <remarks>
/// <para>
/// This command draws lo-res graphics directly to the video memory.
/// Lo-res mode uses a 40×48 grid where each byte in the text page
/// contains two vertically stacked 4-bit color blocks.
/// </para>
/// <para>
/// If a second coordinate pair is provided, draws a line from (x1,y1) to (x2,y2).
/// </para>
/// </remarks>
[DeviceDebugCommand]
public sealed class PlotCommand : CommandHandlerBase, ICommandHelp
{
    private const ushort TextPage1Base = 0x0400;
    private const int LoResWidth = 40;
    private const int LoResHeight = 48;
    private const int TextRows = 24;

    // Pre-computed row addresses for text page 1
    private static readonly ushort[] TextRowAddresses = ComputeTextRowAddresses();

    /// <summary>
    /// Initializes a new instance of the <see cref="PlotCommand"/> class.
    /// </summary>
    public PlotCommand()
        : base("plot", "Draw lo-res graphics point or line")
    {
    }

    /// <inheritdoc/>
    public string Synopsis => "plot <x> <y> [<x2> <y2>] <color>";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Draws lo-res graphics by writing directly to the text page buffer. " +
        "Lo-res mode uses a 40×48 grid with 16 colors (0-15).\n\n" +
        "Each text byte contains two 4-bit color blocks:\n" +
        "  - Low nibble (bits 0-3): Top block\n" +
        "  - High nibble (bits 4-7): Bottom block\n\n" +
        "If only one coordinate is given, plots a single point.\n" +
        "If two coordinates are given, draws a line using Bresenham's algorithm.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("x", null, "int", "X coordinate (0-39)", null),
        new("y", null, "int", "Y coordinate (0-47)", null),
        new("x2", null, "int", "End X coordinate for line (optional)", null),
        new("y2", null, "int", "End Y coordinate for line (optional)", null),
        new("color", null, "int", "Color code (0-15)", null),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "plot 20 24 1              Plot blue point at center",
        "plot 0 0 39 47 2          Draw diagonal line in dark blue",
        "plot 0 24 39 24 15        Draw horizontal white line",
        "plot 20 0 20 47 6         Draw vertical medium blue line",
    ];

    /// <inheritdoc/>
    public string? SideEffects => "Writes to video memory. Graphics appear on next renderer refresh.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["hplot", "print", "video", "gr"];

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

        // Parse arguments: x y color OR x1 y1 x2 y2 color
        if (args.Length < 3)
        {
            return CommandResult.Error("Usage: plot <x> <y> [<x2> <y2>] <color>");
        }

        if (!int.TryParse(args[0], out int x1))
        {
            return CommandResult.Error($"Invalid X coordinate: {args[0]}");
        }

        if (!int.TryParse(args[1], out int y1))
        {
            return CommandResult.Error($"Invalid Y coordinate: {args[1]}");
        }

        int x2, y2, color;

        if (args.Length >= 5)
        {
            // Line mode: x1 y1 x2 y2 color
            if (!int.TryParse(args[2], out x2))
            {
                return CommandResult.Error($"Invalid X2 coordinate: {args[2]}");
            }

            if (!int.TryParse(args[3], out y2))
            {
                return CommandResult.Error($"Invalid Y2 coordinate: {args[3]}");
            }

            if (!int.TryParse(args[4], out color))
            {
                return CommandResult.Error($"Invalid color: {args[4]}");
            }
        }
        else
        {
            // Point mode: x y color
            x2 = x1;
            y2 = y1;
            if (!int.TryParse(args[2], out color))
            {
                return CommandResult.Error($"Invalid color: {args[2]}");
            }
        }

        // Validate coordinates and color
        if (x1 < 0 || x1 >= LoResWidth || x2 < 0 || x2 >= LoResWidth)
        {
            return CommandResult.Error($"X coordinates must be between 0 and {LoResWidth - 1}.");
        }

        if (y1 < 0 || y1 >= LoResHeight || y2 < 0 || y2 >= LoResHeight)
        {
            return CommandResult.Error($"Y coordinates must be between 0 and {LoResHeight - 1}.");
        }

        if (color < 0 || color > 15)
        {
            return CommandResult.Error("Color must be between 0 and 15.");
        }

        // Draw point or line
        if (x1 == x2 && y1 == y2)
        {
            PlotPoint(debugContext, x1, y1, color);
            return CommandResult.Ok($"Plotted point at ({x1},{y1}) in color {color}.");
        }
        else
        {
            int points = DrawLine(debugContext, x1, y1, x2, y2, color);
            return CommandResult.Ok($"Drew line from ({x1},{y1}) to ({x2},{y2}) in color {color} ({points} points).");
        }
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

    private static void PlotPoint(IDebugContext debugContext, int x, int y, int color)
    {
        var bus = debugContext.Machine!.Bus;

        // Calculate text row and whether this is top or bottom block
        int textRow = y / 2;
        bool isTopBlock = (y % 2) == 0;

        // Calculate address
        ushort address = (ushort)(TextRowAddresses[textRow] + x);

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

        // Modify appropriate nibble
        byte newValue = isTopBlock
            ? (byte)((current & 0xF0) | (color & 0x0F)) // Top block is low nibble
            : (byte)((current & 0x0F) | ((color & 0x0F) << 4)); // Bottom block is high nibble

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

    private static int DrawLine(IDebugContext debugContext, int x0, int y0, int x1, int y1, int color)
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
            PlotPoint(debugContext, x0, y0, color);
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