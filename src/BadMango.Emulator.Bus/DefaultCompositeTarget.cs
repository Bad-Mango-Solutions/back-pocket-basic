// <copyright file="DefaultCompositeTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using Interfaces;

/// <summary>
/// A composite bus target that serves as a container for subpages and subregions.
/// </summary>
/// <remarks>
/// <para>
/// A composite target exists to give the <see cref="MainBus"/> a valid memory target area
/// that it can help decode and dispatch to smaller areas during read/write/peek/poke/fetch
/// operations. Subregions within the composite may or may not be backed by physical memory,
/// but can participate in instruction trapping and soft switch behaviors.
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
/// This allows profiles to define composite regions that are progressively filled in
/// with handlers, which is useful for:
/// </para>
/// <list type="bullet">
/// <item><description>Testing and bring-up before handlers are implemented</description></item>
/// <item><description>Placeholder I/O regions that will be filled in later</description></item>
/// <item><description>Minimal configurations that don't need full I/O support</description></item>
/// <item><description>Complex memory layouts like the Apple II I/O page ($C000-$CFFF)</description></item>
/// </list>
/// </remarks>
public sealed class DefaultCompositeTarget : ICompositeTarget
{
    /// <summary>
    /// The value returned for reads to unmapped subregions (floating bus).
    /// </summary>
    private const byte FloatingBusValue = 0xFF;

    private readonly List<SubRegion> subRegions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCompositeTarget"/> class.
    /// </summary>
    /// <param name="name">The name of this composite target, typically from the region mapping.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    public DefaultCompositeTarget(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsPoke;

    /// <summary>
    /// Gets the number of registered subregions.
    /// </summary>
    public int SubRegionCount => subRegions.Count;

    /// <summary>
    /// Registers a subregion within this composite target.
    /// </summary>
    /// <param name="startOffset">The starting offset within the composite region.</param>
    /// <param name="size">The size of the subregion in bytes.</param>
    /// <param name="target">The bus target to handle accesses to this subregion.</param>
    /// <param name="tag">The region tag for this subregion.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is zero.</exception>
    /// <remarks>
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
    public IBusTarget? ResolveTarget(Addr offset, AccessIntent intent)
    {
        // Search registered subregions for a match
        foreach (var subRegion in subRegions)
        {
            if (offset >= subRegion.StartOffset && offset < subRegion.StartOffset + subRegion.Size)
            {
                return subRegion.Target;
            }
        }

        // No subregion found - return null for floating bus behavior
        return null;
    }

    /// <inheritdoc />
    public RegionTag GetSubRegionTag(Addr offset)
    {
        // Search registered subregions for a match
        foreach (var subRegion in subRegions)
        {
            if (offset >= subRegion.StartOffset && offset < subRegion.StartOffset + subRegion.Size)
            {
                return subRegion.Tag;
            }
        }

        // No subregion found - this is part of the composite itself
        return RegionTag.Composite;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(Addr physicalAddress, in BusAccess access)
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
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
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

    private SubRegion? FindSubRegion(Addr offset)
    {
        foreach (var subRegion in subRegions)
        {
            if (offset >= subRegion.StartOffset && offset < subRegion.StartOffset + subRegion.Size)
            {
                return subRegion;
            }
        }

        return null;
    }

    /// <summary>
    /// Represents a subregion within the composite target.
    /// </summary>
    /// <param name="StartOffset">The starting offset within the composite region.</param>
    /// <param name="Size">The size of the subregion in bytes.</param>
    /// <param name="Target">The bus target to handle accesses to this subregion.</param>
    /// <param name="Tag">The region tag for this subregion.</param>
    private readonly record struct SubRegion(Addr StartOffset, Addr Size, IBusTarget Target, RegionTag Tag);
}