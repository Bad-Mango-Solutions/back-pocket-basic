// <copyright file="PagesCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Displays the current page table in a formatted view.
/// </summary>
/// <remarks>
/// <para>
/// Shows the live state of the page table, including each page's mapped target,
/// permissions, and capabilities. Supports range selection for large page tables.
/// </para>
/// <para>
/// For composite targets, this command also displays the subregions within each
/// composite page, showing the internal structure of complex memory regions.
/// </para>
/// <para>
/// This command requires a bus to be attached to the debug context.
/// </para>
/// </remarks>
public sealed class PagesCommand : CommandHandlerBase, ICommandHelp
{
    private const int DefaultPageCount = 16;

    /// <summary>
    /// Initializes a new instance of the <see cref="PagesCommand"/> class.
    /// </summary>
    public PagesCommand()
        : base("pages", "Display current page table")
    {
    }

    /// <inheritdoc/>
    public override string Usage => "pages [start_page] [count]";

    /// <inheritdoc/>
    public string Synopsis => "pages [start_page] [count]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays the live state of the page table, including each page's mapped target, " +
        "permissions (RWX), capabilities, offset within the source, and device ID. Supports " +
        "range selection for large page tables. Use start_page and count to view specific " +
        "pages. For composite targets, also displays subregions within the page. Requires a bus-based system.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "pages                    Display first 16 pages",
        "pages $04                Display pages starting from page 4",
        "pages 0 32               Display first 32 pages",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["regions", "devicemap", "profile"];

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

        var bus = debugContext.Bus;
        var pageSize = 1 << bus.PageShift;

        // Parse optional start page and count
        int startPage = 0;
        int count = Math.Min(DefaultPageCount, bus.PageCount);

        if (args.Length > 0)
        {
            if (!TryParsePageIndex(args[0], out startPage))
            {
                return CommandResult.Error($"Invalid start page: '{args[0]}'. Use decimal or hex ($nn or 0xnn).");
            }

            if (startPage < 0 || startPage >= bus.PageCount)
            {
                return CommandResult.Error($"Start page {startPage} is out of range. Valid range: 0-{bus.PageCount - 1}.");
            }
        }

        if (args.Length > 1 && (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count < 1))
        {
            return CommandResult.Error($"Invalid count: '{args[1]}'. Expected a positive integer.");
        }

        // Clamp count to remaining pages
        count = Math.Min(count, bus.PageCount - startPage);

        debugContext.Output.WriteLine($"Page Table (pages {startPage}-{startPage + count - 1} of {bus.PageCount}, page size: 0x{pageSize:X}):");
        debugContext.Output.WriteLine();

        // Column headers with consistent widths
        // Page  Addr    Type        Perms  Source           Offset  Caps                 DevID
        debugContext.Output.WriteLine($"{"Page",-5} {"Addr",-7} {"Type",-11} {"Perms",-5} {"Source",-16} {"Offset",-7} {"Caps",-20} {"DevID",-5}");
        debugContext.Output.WriteLine(new string('─', 88));

        for (int i = 0; i < count; i++)
        {
            int pageIndex = startPage + i;
            ref readonly var entry = ref bus.GetPageEntryByIndex(pageIndex);
            uint virtAddr = (uint)(pageIndex * pageSize);

            string permsStr = FormatPerms(entry.Perms);
            string capsStr = FormatCaps(entry.Caps);
            string sourceStr = FormatSource(entry.Target);

            debugContext.Output.WriteLine(
                $"${pageIndex:X2}   ${virtAddr:X4}   {entry.RegionTag,-11} {permsStr,-5} {sourceStr,-16} ${entry.PhysicalBase:X4}   {capsStr,-20} {entry.DeviceId,-5}");

            // If this is a composite target, display its subregions
            if (entry.Target is ICompositeTarget composite)
            {
                OutputCompositeSubRegions(debugContext.Output, composite, virtAddr);
            }
        }

        if (startPage + count < bus.PageCount)
        {
            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine($"... ({bus.PageCount - startPage - count} more pages. Use 'pages {startPage + count}' to continue.)");
        }

        return CommandResult.Ok();
    }

    private static void OutputCompositeSubRegions(TextWriter output, ICompositeTarget composite, uint pageBaseAddr)
    {
        var subRegions = composite.EnumerateSubRegions().ToList();
        if (subRegions.Count == 0)
        {
            return;
        }

        foreach (var (startOffset, size, tag, targetName) in subRegions)
        {
            uint subAddr = pageBaseAddr + startOffset;
            uint endAddr = subAddr + size - 1;

            // Indent subregions and use a different format
            output.WriteLine(
                $"       └─ ${subAddr:X4}-${endAddr:X4} {tag,-11}       {targetName,-16}");
        }
    }

    private static bool TryParsePageIndex(string value, out int result)
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

    private static string FormatPerms(PagePerms perms)
    {
        char r = (perms & PagePerms.Read) != 0 ? 'R' : '-';
        char w = (perms & PagePerms.Write) != 0 ? 'W' : '-';
        char x = (perms & PagePerms.Execute) != 0 ? 'X' : '-';
        return $"{r}{w}{x}";
    }

    private static string FormatCaps(TargetCaps caps)
    {
        var parts = new List<string>(4);

        if ((caps & TargetCaps.SupportsPeek) != 0)
        {
            parts.Add("Pk");
        }

        if ((caps & TargetCaps.SupportsPoke) != 0)
        {
            parts.Add("Po");
        }

        if ((caps & TargetCaps.SupportsWide) != 0)
        {
            parts.Add("W");
        }

        if ((caps & TargetCaps.HasSideEffects) != 0)
        {
            parts.Add("SE");
        }

        if ((caps & TargetCaps.TimingSensitive) != 0)
        {
            parts.Add("TS");
        }

        return parts.Count > 0 ? string.Join(",", parts) : "None";
    }

    private static string FormatSource(IBusTarget? target)
    {
        if (target is null)
        {
            return "-";
        }

        return target.Name;
    }
}