// <copyright file="CompositeTargetBase.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using Interfaces;

/// <summary>
/// Abstract base class for composite bus targets that serve as containers for subregions.
/// </summary>
/// <remarks>
/// <para>
/// A composite target exists to give the <see cref="MainBus"/> a valid memory target area
/// that it can help decode and dispatch to smaller areas during read/write/peek/poke/fetch
/// operations. Subregions within the composite may or may not be backed by physical memory,
/// but can participate in instruction trapping and soft switch behaviors.
/// </para>
/// <para>
/// Subregions must be aligned to 256-byte ($100) boundaries. This constraint ensures
/// consistent addressing within the Apple II I/O page architecture.
/// </para>
/// <para>
/// When no subregion is registered for a given offset, the composite target provides
/// open-bus behavior:
/// </para>
/// <list type="bullet">
/// <item><description>Reads return $FF (floating bus value)</description></item>
/// <item><description>Writes are silently ignored (no-op)</description></item>
/// </list>
/// <para>
/// Derived classes can override <see cref="ResolveTarget"/> and <see cref="GetSubRegionTag"/>
/// to provide custom dispatch logic beyond simple subregion lookup.
/// </para>
/// </remarks>
public abstract class CompositeTargetBase : ICompositeTarget
{
    /// <summary>
    /// The required alignment for subregion start offsets and sizes (256 bytes).
    /// </summary>
    public const uint SubRegionAlignment = 0x100;

    /// <summary>
    /// The value returned for reads to unmapped subregions (floating bus).
    /// </summary>
    protected const byte FloatingBusValue = 0xFF;

    private readonly List<SubRegion> subRegions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeTargetBase"/> class.
    /// </summary>
    /// <param name="name">The name of this composite target, typically from the region mapping.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    protected CompositeTargetBase(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public virtual TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsPoke;

    /// <summary>
    /// Gets the number of registered subregions.
    /// </summary>
    public int SubRegionCount => subRegions.Count;

    /// <summary>
    /// Gets the registered subregions as a read-only list.
    /// </summary>
    protected IReadOnlyList<SubRegion> SubRegions => subRegions;

    /// <summary>
    /// Registers a subregion within this composite target.
    /// </summary>
    /// <param name="startOffset">The starting offset within the composite region (must be 256-byte aligned).</param>
    /// <param name="size">The size of the subregion in bytes (must be 256-byte aligned).</param>
    /// <param name="target">The bus target to handle accesses to this subregion.</param>
    /// <param name="tag">The region tag for this subregion.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is zero.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="startOffset"/> or <paramref name="size"/> is not 256-byte aligned.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Subregions must be aligned to 256-byte ($100) boundaries for both start offset and size.
    /// This constraint ensures consistent addressing within the Apple II I/O page architecture.
    /// </para>
    /// <para>
    /// Subregions are checked in registration order. If subregions overlap, the first
    /// registered subregion that contains the offset wins. This allows for layered
    /// configurations where more specific regions can be registered first.
    /// </para>
    /// </remarks>
    public void RegisterSubRegion(Addr startOffset, Addr size, IBusTarget target, RegionTag tag)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (size == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Subregion size must be greater than zero.");
        }

        if ((startOffset % SubRegionAlignment) != 0)
        {
            throw new ArgumentException(
                $"Subregion start offset 0x{startOffset:X} is not 256-byte aligned. " +
                $"Start offset must be a multiple of 0x{SubRegionAlignment:X}.",
                nameof(startOffset));
        }

        if ((size % SubRegionAlignment) != 0)
        {
            throw new ArgumentException(
                $"Subregion size 0x{size:X} is not 256-byte aligned. " +
                $"Size must be a multiple of 0x{SubRegionAlignment:X}.",
                nameof(size));
        }

        subRegions.Add(new SubRegion(startOffset, size, target, tag));
    }

    /// <summary>
    /// Clears all registered subregions.
    /// </summary>
    public void ClearSubRegions()
    {
        subRegions.Clear();
    }

    /// <inheritdoc />
    public virtual IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        // Search registered subregions for a match
        foreach (var subRegion in subRegions)
        {
            if (subRegion.Contains(offset))
            {
                return subRegion.Target;
            }
        }

        // No subregion found - return null for floating bus behavior
        return null;
    }

    /// <inheritdoc />
    public virtual RegionTag GetSubRegionTag(Addr offset)
    {
        // Search registered subregions for a match
        foreach (var subRegion in subRegions)
        {
            if (subRegion.Contains(offset))
            {
                return subRegion.Tag;
            }
        }

        // No subregion found - this is part of the composite itself
        return RegionTag.Composite;
    }

    /// <inheritdoc />
    public virtual IEnumerable<(Addr StartOffset, Addr Size, RegionTag Tag, string TargetName)> EnumerateSubRegions()
    {
        foreach (var subRegion in subRegions)
        {
            yield return (subRegion.StartOffset, subRegion.Size, subRegion.Tag, subRegion.Target.Name);
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual byte Read8(Addr physicalAddress, in BusAccess access)
    {
        // Try to resolve to a subregion target
        var target = ResolveTarget(physicalAddress, access.Intent);
        if (target is not null)
        {
            // Calculate offset within the subregion
            var subRegion = FindSubRegion(physicalAddress);
            if (subRegion.HasValue)
            {
                Addr subOffset = physicalAddress - subRegion.Value.StartOffset;
                return target.Read8(subOffset, in access);
            }
        }

        // Return floating bus value (open bus behavior)
        return FloatingBusValue;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        // Try to resolve to a subregion target
        var target = ResolveTarget(physicalAddress, access.Intent);
        if (target is not null)
        {
            // Calculate offset within the subregion
            var subRegion = FindSubRegion(physicalAddress);
            if (subRegion.HasValue)
            {
                Addr subOffset = physicalAddress - subRegion.Value.StartOffset;
                target.Write8(subOffset, value, in access);
                return;
            }
        }

        // No-op: writes to unmapped subregions are silently ignored
    }

    /// <summary>
    /// Finds the subregion containing the specified offset.
    /// </summary>
    /// <param name="offset">The offset to search for.</param>
    /// <returns>The matching subregion, or <see langword="null"/> if no subregion contains the offset.</returns>
    protected SubRegion? FindSubRegion(Addr offset)
    {
        foreach (var subRegion in subRegions)
        {
            if (subRegion.Contains(offset))
            {
                return subRegion;
            }
        }

        return null;
    }
}