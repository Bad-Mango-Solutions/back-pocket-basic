// <copyright file="AuxiliaryMemoryPage0Target.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Bus target for page 0 ($0000-$0FFF) that handles sub-page auxiliary memory switching.
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
/// This target uses the <see cref="AuxiliaryMemoryController"/> state to determine
/// which backing memory (main or auxiliary) should handle each access.
/// </para>
/// </remarks>
public sealed class AuxiliaryMemoryPage0Target : IBusTarget
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
    private readonly IBusTarget auxZeroPage;
    private readonly IBusTarget auxStack;
    private readonly IBusTarget auxTextPage;
    private readonly IBusTarget? auxGeneral;
    private readonly AuxiliaryMemoryController controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuxiliaryMemoryPage0Target"/> class.
    /// </summary>
    /// <param name="mainMemory">The main memory target for page 0 (must be at least 4KB).</param>
    /// <param name="auxZeroPage">The auxiliary zero page memory (256 bytes at offset 0).</param>
    /// <param name="auxStack">The auxiliary stack memory (256 bytes at offset 0).</param>
    /// <param name="auxTextPage">The auxiliary text page memory (1KB at offset 0).</param>
    /// <param name="controller">The auxiliary memory controller that manages switch states.</param>
    /// <param name="auxGeneral">
    /// Optional auxiliary general memory for $0200-$03FF and $0800-$0FFF regions (4KB at offset 0).
    /// When provided, these regions will be switched via RAMRD/RAMWRT.
    /// When <see langword="null"/>, general regions always use main memory.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required parameter is <see langword="null"/>.
    /// </exception>
    public AuxiliaryMemoryPage0Target(
        IBusTarget mainMemory,
        IBusTarget auxZeroPage,
        IBusTarget auxStack,
        IBusTarget auxTextPage,
        AuxiliaryMemoryController controller,
        IBusTarget? auxGeneral = null)
    {
        ArgumentNullException.ThrowIfNull(mainMemory);
        ArgumentNullException.ThrowIfNull(auxZeroPage);
        ArgumentNullException.ThrowIfNull(auxStack);
        ArgumentNullException.ThrowIfNull(auxTextPage);
        ArgumentNullException.ThrowIfNull(controller);

        this.mainMemory = mainMemory;
        this.auxZeroPage = auxZeroPage;
        this.auxStack = auxStack;
        this.auxTextPage = auxTextPage;
        this.auxGeneral = auxGeneral;
        this.controller = controller;
    }

    /// <summary>
    /// Gets the name of this target.
    /// </summary>
    /// <value>A human-readable name for the target, used for diagnostics and debugging.</value>
    public string Name => "Auxiliary Memory Page 0";

    /// <inheritdoc />
    public TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsPoke;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        int offset = (int)(physicalAddress & 0x0FFF);
        var (target, targetOffset) = ResolveReadTarget(offset);
        return target.Read8((Addr)targetOffset, in access);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        int offset = (int)(physicalAddress & 0x0FFF);
        var (target, targetOffset) = ResolveWriteTarget(offset);
        target.Write8((Addr)targetOffset, value, in access);
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
    /// Resolves the target and offset for a read access.
    /// </summary>
    /// <param name="offset">The offset within page 0 (0x000-0xFFF).</param>
    /// <returns>A tuple of the target and the offset within that target.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (IBusTarget Target, int Offset) ResolveReadTarget(int offset)
    {
        // Zero page ($0000-$00FF): Controlled by ALTZP
        if (offset < ZeroPageEnd)
        {
            return controller.IsAltZpEnabled
                ? (auxZeroPage, offset)
                : (mainMemory, offset);
        }

        // Stack ($0100-$01FF): Controlled by ALTZP
        if (offset < StackEnd)
        {
            return controller.IsAltZpEnabled
                ? (auxStack, offset - ZeroPageEnd)
                : (mainMemory, offset);
        }

        // Text page 1 ($0400-$07FF): Controlled by 80STORE + PAGE2
        if (offset >= TextPageStart && offset < TextPageEnd)
        {
            bool useAux = controller.Is80StoreEnabled && controller.IsPage2Selected;
            return useAux
                ? (auxTextPage, offset - TextPageStart)
                : (mainMemory, offset);
        }

        // General RAM regions ($0200-$03FF, $0800-$0FFF): Controlled by RAMRD
        if (auxGeneral is not null && controller.IsRamRdEnabled)
        {
            return (auxGeneral, offset);
        }

        return (mainMemory, offset);
    }

    /// <summary>
    /// Resolves the target and offset for a write access.
    /// </summary>
    /// <param name="offset">The offset within page 0 (0x000-0xFFF).</param>
    /// <returns>A tuple of the target and the offset within that target.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (IBusTarget Target, int Offset) ResolveWriteTarget(int offset)
    {
        // Zero page ($0000-$00FF): Controlled by ALTZP
        if (offset < ZeroPageEnd)
        {
            return controller.IsAltZpEnabled
                ? (auxZeroPage, offset)
                : (mainMemory, offset);
        }

        // Stack ($0100-$01FF): Controlled by ALTZP
        if (offset < StackEnd)
        {
            return controller.IsAltZpEnabled
                ? (auxStack, offset - ZeroPageEnd)
                : (mainMemory, offset);
        }

        // Text page 1 ($0400-$07FF): Controlled by 80STORE + PAGE2
        if (offset >= TextPageStart && offset < TextPageEnd)
        {
            bool useAux = controller.Is80StoreEnabled && controller.IsPage2Selected;
            return useAux
                ? (auxTextPage, offset - TextPageStart)
                : (mainMemory, offset);
        }

        // General RAM regions ($0200-$03FF, $0800-$0FFF): Controlled by RAMWRT
        if (auxGeneral is not null && controller.IsRamWrtEnabled)
        {
            return (auxGeneral, offset);
        }

        return (mainMemory, offset);
    }
}