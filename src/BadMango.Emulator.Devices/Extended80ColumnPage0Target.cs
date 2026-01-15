// <copyright file="Extended80ColumnPage0Target.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

using Interfaces;

using Addr = System.UInt32;

/// <summary>
/// Bus target for page 0 ($0000-$0FFF) that handles sub-page auxiliary memory switching
/// for the Extended 80-Column Card.
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
/// This target uses the <see cref="IExtended80ColumnDevice"/> state to determine
/// which backing memory (main or auxiliary) should handle each access.
/// </para>
/// <para>
/// Unlike the layer-based approach, this composite target checks soft switch state
/// at each memory access, enabling proper sub-page bank switching needed for
/// 80-column text mode.
/// </para>
/// </remarks>
public sealed class Extended80ColumnPage0Target : IBusTarget
{
    /// <summary>
    /// End of zero page region (exclusive).
    /// </summary>
    private const int ZeroPageEnd = 0x0100;

    /// <summary>
    /// End of stack region (exclusive).
    /// </summary>
    private const int StackEnd = 0x0200;

    /// <summary>
    /// Start of text page 1 region.
    /// </summary>
    private const int TextPageStart = 0x0400;

    /// <summary>
    /// End of text page 1 region (exclusive).
    /// </summary>
    private const int TextPageEnd = 0x0800;

    private readonly IBusTarget mainMemory;
    private readonly Extended80ColumnDevice controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="Extended80ColumnPage0Target"/> class.
    /// </summary>
    /// <param name="mainMemory">The main memory target for page 0 (must be at least 4KB).</param>
    /// <param name="controller">The Extended 80-Column device that manages switch states.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <see langword="null"/>.
    /// </exception>
    public Extended80ColumnPage0Target(
        IBusTarget mainMemory,
        Extended80ColumnDevice controller)
    {
        ArgumentNullException.ThrowIfNull(mainMemory);
        ArgumentNullException.ThrowIfNull(controller);

        this.mainMemory = mainMemory;
        this.controller = controller;
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
        bool useAux = ShouldUseAuxForRead(offset);

        if (useAux)
        {
            // Read from auxiliary RAM
            return controller.ReadAuxRam((ushort)offset);
        }

        // Read from main memory
        return mainMemory.Read8(physicalAddress, in access);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        int offset = (int)(physicalAddress & 0x0FFF);
        bool useAux = ShouldUseAuxForWrite(offset);

        if (useAux)
        {
            // Write to auxiliary RAM
            controller.WriteAuxRam((ushort)offset, value);
            return;
        }

        // Write to main memory
        mainMemory.Write8(physicalAddress, value, in access);
    }

    /// <summary>
    /// Gets the sub-region tag for a given offset (for tracing/debugging).
    /// </summary>
    /// <param name="offset">Offset within the 4KB page.</param>
    /// <returns>Region tag for the sub-region.</returns>
    public RegionTag GetSubRegionTag(Addr offset)
    {
        return offset switch
        {
            < ZeroPageEnd => RegionTag.ZeroPage,
            < StackEnd => RegionTag.Stack,
            >= TextPageStart and < TextPageEnd => RegionTag.Video,
            _ => RegionTag.Ram,
        };
    }

    /// <summary>
    /// Determines if a read access should use auxiliary memory.
    /// </summary>
    /// <param name="offset">The offset within page 0 (0x000-0xFFF).</param>
    /// <returns><see langword="true"/> to use auxiliary memory; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldUseAuxForRead(int offset)
    {
        // Zero page ($0000-$00FF) and Stack ($0100-$01FF): Controlled by ALTZP
        if (offset < StackEnd)
        {
            return controller.IsAltZpEnabled;
        }

        // Text page 1 ($0400-$07FF): Controlled by 80STORE + PAGE2
        if (offset >= TextPageStart && offset < TextPageEnd)
        {
            if (controller.Is80StoreEnabled)
            {
                return controller.IsPage2Selected;
            }

            // When 80STORE is disabled, text page follows RAMRD like general regions
            return controller.IsRamRdEnabled;
        }

        // General RAM regions ($0200-$03FF, $0800-$0FFF): Controlled by RAMRD
        return controller.IsRamRdEnabled;
    }

    /// <summary>
    /// Determines if a write access should use auxiliary memory.
    /// </summary>
    /// <param name="offset">The offset within page 0 (0x000-0xFFF).</param>
    /// <returns><see langword="true"/> to use auxiliary memory; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldUseAuxForWrite(int offset)
    {
        // Zero page ($0000-$00FF) and Stack ($0100-$01FF): Controlled by ALTZP
        if (offset < StackEnd)
        {
            return controller.IsAltZpEnabled;
        }

        // Text page 1 ($0400-$07FF): Controlled by 80STORE + PAGE2
        if (offset >= TextPageStart && offset < TextPageEnd)
        {
            if (controller.Is80StoreEnabled)
            {
                return controller.IsPage2Selected;
            }

            // When 80STORE is disabled, text page follows RAMWRT like general regions
            return controller.IsRamWrtEnabled;
        }

        // General RAM regions ($0200-$03FF, $0800-$0FFF): Controlled by RAMWRT
        return controller.IsRamWrtEnabled;
    }
}
