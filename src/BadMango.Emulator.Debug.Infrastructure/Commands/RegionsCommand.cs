// <copyright file="RegionsCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Shows all mapped memory regions in the bus-based system.
/// </summary>
/// <remarks>
/// <para>
/// Displays a formatted table of all memory regions defined in the page table,
/// grouped by region type. This command provides visibility into the memory map
/// at the page level.
/// </para>
/// <para>
/// For composite regions, this command also displays the subregions within each
/// composite page, showing the internal structure of complex memory regions.
/// </para>
/// <para>
/// This command requires a bus to be attached to the debug context.
/// </para>
/// </remarks>
public sealed class RegionsCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegionsCommand"/> class.
    /// </summary>
    public RegionsCommand()
        : base("regions", "Show all mapped memory regions")
    {
    }

    /// <inheritdoc/>
    public override string Usage => "regions";

    /// <inheritdoc/>
    public string Synopsis => "regions";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays a formatted table of all memory regions defined in the page table, " +
        "grouped by region type (RAM, ROM, I/O, etc.). Shows start address, end address, " +
        "size, type, permissions (RWX), and device ID. For composite regions, also displays " +
        "subregions within the composite. Requires a bus-based system.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "regions                 Display all memory regions",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["pages", "devicemap", "profile"];

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

        debugContext.Output.WriteLine("Memory Regions:");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"{"Start",-8} {"End",-8} {"Size",-8} {"Type",-12} {"Perms",-8} {"Device",-10}");
        debugContext.Output.WriteLine(new string('─', 60));

        // Track contiguous regions with the same properties
        int? regionStartPage = null;
        RegionTag? currentTag = null;
        PagePerms? currentPerms = null;
        int? currentDeviceId = null;
        IBusTarget? currentTarget = null;

        for (int pageIndex = 0; pageIndex < bus.PageCount; pageIndex++)
        {
            ref readonly var entry = ref bus.GetPageEntryByIndex(pageIndex);

            // Check if this page continues the current region
            bool isContinuation = regionStartPage.HasValue &&
                                  entry.RegionTag == currentTag &&
                                  entry.Perms == currentPerms &&
                                  entry.DeviceId == currentDeviceId;

            if (!isContinuation && regionStartPage.HasValue)
            {
                // Output the previous region
                OutputRegion(
                    debugContext.Output,
                    regionStartPage.Value,
                    pageIndex - 1,
                    pageSize,
                    currentTag!.Value,
                    currentPerms!.Value,
                    currentDeviceId!.Value,
                    currentTarget);
            }

            if (!isContinuation)
            {
                // Start a new region
                regionStartPage = pageIndex;
                currentTag = entry.RegionTag;
                currentPerms = entry.Perms;
                currentDeviceId = entry.DeviceId;
                currentTarget = entry.Target;
            }
        }

        // Output the last region
        if (regionStartPage.HasValue)
        {
            OutputRegion(
                debugContext.Output,
                regionStartPage.Value,
                bus.PageCount - 1,
                pageSize,
                currentTag!.Value,
                currentPerms!.Value,
                currentDeviceId!.Value,
                currentTarget);
        }

        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"Total pages: {bus.PageCount}, Page size: {pageSize} bytes (0x{pageSize:X})");

        return CommandResult.Ok();
    }

    private static void OutputRegion(
        TextWriter output,
        int startPage,
        int endPage,
        int pageSize,
        RegionTag tag,
        PagePerms perms,
        int deviceId,
        IBusTarget? target)
    {
        uint startAddr = (uint)(startPage * pageSize);
        uint endAddr = (uint)((endPage + 1) * pageSize) - 1;
        uint size = (uint)((endPage - startPage + 1) * pageSize);

        string permsStr = FormatPerms(perms);
        string tagStr = tag.ToString();

        output.WriteLine($"${startAddr:X4}    ${endAddr:X4}    ${size:X4}    {tagStr,-12} {permsStr,-8} {deviceId}");

        // If this is a composite target, display its subregions
        if (target is ICompositeTarget composite)
        {
            OutputCompositeSubRegions(output, composite, startAddr);
        }
    }

    private static void OutputCompositeSubRegions(TextWriter output, ICompositeTarget composite, uint regionBaseAddr)
    {
        var subRegions = composite.EnumerateSubRegions().ToList();
        if (subRegions.Count == 0)
        {
            return;
        }

        foreach (var (startOffset, size, tag, targetName) in subRegions)
        {
            uint subAddr = regionBaseAddr + startOffset;
            uint endAddr = subAddr + size - 1;

            // Indent subregions and use a different format
            output.WriteLine(
                $"  └─ ${subAddr:X4}    ${endAddr:X4}    ${size:X4}    {tag,-12}          {targetName}");
        }
    }

    private static string FormatPerms(PagePerms perms)
    {
        char r = (perms & PagePerms.Read) != 0 ? 'R' : '-';
        char w = (perms & PagePerms.Write) != 0 ? 'W' : '-';
        char x = (perms & PagePerms.Execute) != 0 ? 'X' : '-';
        return $"{r}{w}{x}";
    }
}