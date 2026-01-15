// <copyright file="Extended80ColumnPage0Target.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

/// <summary>
/// Bus target for page 0 ($0000-$0FFF) that handles sub-page auxiliary memory switching
/// for the Extended 80-Column Card using a routing table approach.
/// </summary>
/// <remarks>
/// <para>
/// Page 0 contains multiple sub-regions that can independently switch between main and auxiliary memory:
/// </para>
/// <list type="bullet">
/// <item><description>Zero page ($0000-$00FF): Controlled by ALTZP switch</description></item>
/// <item><description>Stack ($0100-$01FF): Controlled by ALTZP switch</description></item>
/// <item><description>General ($0200-$03FF): Controlled by RAMRD/RAMWRT switches</description></item>
/// <item><description>Text page 1 ($0400-$07FF): Controlled by 80STORE + PAGE2</description></item>
/// <item><description>General ($0800-$0FFF): Controlled by RAMRD/RAMWRT switches</description></item>
/// </list>
/// <para>
/// This target uses a routing table approach for efficiency. Instead of checking soft switch
/// state at each memory access, the routing table is updated when switches change. Each access
/// simply indexes into the routing table based on the high nibble of the address offset.
/// </para>
/// <para>
/// The routing table has 16 entries (one per 256-byte sub-page). When a soft switch changes,
/// only the affected entries are updated via <see cref="UpdateRouting"/>.
/// </para>
/// </remarks>
public sealed class Extended80ColumnPage0Target : IBusTarget
{
    /// <summary>
    /// Number of 256-byte sub-pages in page 0.
    /// </summary>
    private const int SubPageCount = 16;

    /// <summary>
    /// Size of each sub-page in bytes.
    /// </summary>
    private const int SubPageSize = 256;

    /// <summary>
    /// Shift value for sub-page index calculation.
    /// </summary>
    private const int SubPageShift = 8;

    /// <summary>
    /// Mask for offset within a sub-page.
    /// </summary>
    private const int SubPageMask = 0xFF;

    private readonly IBusTarget mainMemory;
    private readonly IBusTarget auxMemory;

    /// <summary>
    /// Read routing table: one target per 256-byte sub-page.
    /// Index 0 = $0000-$00FF, Index 1 = $0100-$01FF, etc.
    /// </summary>
    private readonly IBusTarget[] readRouting;

    /// <summary>
    /// Write routing table: one target per 256-byte sub-page.
    /// </summary>
    private readonly IBusTarget[] writeRouting;

    /// <summary>
    /// Initializes a new instance of the <see cref="Extended80ColumnPage0Target"/> class.
    /// </summary>
    /// <param name="mainMemory">The main memory target for page 0 (must be at least 4KB).</param>
    /// <param name="auxMemory">The auxiliary memory target for page 0 (must be at least 4KB).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <see langword="null"/>.
    /// </exception>
    public Extended80ColumnPage0Target(
        IBusTarget mainMemory,
        IBusTarget auxMemory)
    {
        ArgumentNullException.ThrowIfNull(mainMemory);
        ArgumentNullException.ThrowIfNull(auxMemory);

        this.mainMemory = mainMemory;
        this.auxMemory = auxMemory;

        // Initialize routing tables - all pointing to main memory by default
        readRouting = new IBusTarget[SubPageCount];
        writeRouting = new IBusTarget[SubPageCount];

        for (int i = 0; i < SubPageCount; i++)
        {
            readRouting[i] = mainMemory;
            writeRouting[i] = mainMemory;
        }
    }

    /// <summary>
    /// Gets the name of this target.
    /// </summary>
    /// <value>A human-readable name for the target, used for diagnostics and debugging.</value>
    public string Name => "Extended 80-Column Page 0";

    /// <inheritdoc />
    public TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsPoke;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        int offset = (int)(physicalAddress & 0x0FFF);
        int subPage = offset >> SubPageShift;
        int subOffset = offset & SubPageMask;

        // Route to the appropriate target based on sub-page
        var target = readRouting[subPage];
        Addr targetAddress = (Addr)((subPage << SubPageShift) + subOffset);
        return target.Read8(targetAddress, in access);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        int offset = (int)(physicalAddress & 0x0FFF);
        int subPage = offset >> SubPageShift;
        int subOffset = offset & SubPageMask;

        // Route to the appropriate target based on sub-page
        var target = writeRouting[subPage];
        Addr targetAddress = (Addr)((subPage << SubPageShift) + subOffset);
        target.Write8(targetAddress, value, in access);
    }

    /// <summary>
    /// Updates the routing table based on the current soft switch state.
    /// </summary>
    /// <param name="altZp">Whether ALTZP is enabled (affects $0000-$01FF).</param>
    /// <param name="store80">Whether 80STORE is enabled.</param>
    /// <param name="page2">Whether PAGE2 is selected (affects text page when 80STORE is on).</param>
    /// <param name="ramRd">Whether RAMRD is enabled (affects general RAM reads).</param>
    /// <param name="ramWrt">Whether RAMWRT is enabled (affects general RAM writes).</param>
    /// <remarks>
    /// <para>
    /// Call this method whenever any of the soft switches change. The routing table
    /// is updated atomically for the affected sub-pages. This is much more efficient
    /// than checking switch state at each memory access.
    /// </para>
    /// <para>
    /// Routing rules:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Sub-pages 0-1 ($0000-$01FF): Use aux if ALTZP, else main</description></item>
    /// <item><description>Sub-pages 2-3 ($0200-$03FF): Use aux if RAMRD/RAMWRT, else main</description></item>
    /// <item><description>Sub-pages 4-7 ($0400-$07FF): Use aux if 80STORE+PAGE2, else follow RAMRD/RAMWRT</description></item>
    /// <item><description>Sub-pages 8-15 ($0800-$0FFF): Use aux if RAMRD/RAMWRT, else main</description></item>
    /// </list>
    /// </remarks>
    public void UpdateRouting(bool altZp, bool store80, bool page2, bool ramRd, bool ramWrt)
    {
        // Sub-pages 0-1: Zero page and stack ($0000-$01FF) - controlled by ALTZP
        var zpTarget = altZp ? auxMemory : mainMemory;
        readRouting[0] = zpTarget;
        readRouting[1] = zpTarget;
        writeRouting[0] = zpTarget;
        writeRouting[1] = zpTarget;

        // Sub-pages 2-3: General RAM ($0200-$03FF) - controlled by RAMRD/RAMWRT
        readRouting[2] = ramRd ? auxMemory : mainMemory;
        readRouting[3] = ramRd ? auxMemory : mainMemory;
        writeRouting[2] = ramWrt ? auxMemory : mainMemory;
        writeRouting[3] = ramWrt ? auxMemory : mainMemory;

        // Sub-pages 4-7: Text page ($0400-$07FF) - controlled by 80STORE+PAGE2 or RAMRD/RAMWRT
        IBusTarget textReadTarget;
        IBusTarget textWriteTarget;

        if (store80)
        {
            // 80STORE mode: PAGE2 controls text page routing
            textReadTarget = page2 ? auxMemory : mainMemory;
            textWriteTarget = page2 ? auxMemory : mainMemory;
        }
        else
        {
            // Normal mode: text page follows RAMRD/RAMWRT like general memory
            textReadTarget = ramRd ? auxMemory : mainMemory;
            textWriteTarget = ramWrt ? auxMemory : mainMemory;
        }

        for (int i = 4; i < 8; i++)
        {
            readRouting[i] = textReadTarget;
            writeRouting[i] = textWriteTarget;
        }

        // Sub-pages 8-15: General RAM ($0800-$0FFF) - controlled by RAMRD/RAMWRT
        var generalReadTarget = ramRd ? auxMemory : mainMemory;
        var generalWriteTarget = ramWrt ? auxMemory : mainMemory;

        for (int i = 8; i < SubPageCount; i++)
        {
            readRouting[i] = generalReadTarget;
            writeRouting[i] = generalWriteTarget;
        }
    }

    /// <summary>
    /// Gets the sub-region tag for a given offset (for tracing/debugging).
    /// </summary>
    /// <param name="offset">Offset within the 4KB page.</param>
    /// <returns>Region tag for the sub-region.</returns>
    public RegionTag GetSubRegionTag(Addr offset)
    {
        return (offset >> SubPageShift) switch
        {
            0 => RegionTag.ZeroPage,
            1 => RegionTag.Stack,
            >= 4 and < 8 => RegionTag.Video,
            _ => RegionTag.Ram,
        };
    }
}