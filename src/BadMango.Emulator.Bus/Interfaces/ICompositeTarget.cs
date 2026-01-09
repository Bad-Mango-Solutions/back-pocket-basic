// <copyright file="ICompositeTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// A composite bus target that dispatches to sub-targets based on address offset.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends <see cref="IBusTarget"/> to support pages that contain
/// multiple logical regions. Rather than splitting into sub-pages (which would
/// complicate 4KB page granularity), a composite target handles the internal dispatch.
/// </para>
/// <para>
/// The Apple II I/O page ($C000-$CFFF) is a prime example:
/// </para>
/// <list type="bullet">
/// <item><description>$C000-$C0FF: Soft switches</description></item>
/// <item><description>$C100-$C7FF: Slot ROM ($Cn00 for slot n)</description></item>
/// <item><description>$C800-$CFFF: Expansion ROM (selected slot)</description></item>
/// </list>
/// <para>
/// When the <see cref="MainBus"/> detects that a page's target implements
/// <see cref="ICompositeTarget"/>, it calls <see cref="ResolveTarget"/> to determine
/// which sub-target should handle the access.
/// </para>
/// </remarks>
public interface ICompositeTarget : IBusTarget
{
    /// <summary>
    /// Resolves the actual target for a given offset within the page.
    /// </summary>
    /// <param name="offset">Offset within the 4KB page (0x000-0xFFF).</param>
    /// <param name="intent">Access intent (affects slot ROM visibility rules).</param>
    /// <returns>
    /// The target to handle this access, or <see langword="null"/> for floating bus.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The returned target receives the physical address computed by the bus,
    /// which is the page's physical base plus the offset. The sub-target should
    /// interpret this address within its own address space.
    /// </para>
    /// <para>
    /// Returning <see langword="null"/> indicates that the offset has no backing
    /// device and should return a floating bus value (typically 0xFF for reads,
    /// ignored for writes).
    /// </para>
    /// </remarks>
    IBusTarget? ResolveTarget(Addr offset, AccessIntent intent);

    /// <summary>
    /// Gets the sub-region tag for a given offset (for tracing/debugging).
    /// </summary>
    /// <param name="offset">Offset within the 4KB page.</param>
    /// <returns>Region tag for the sub-region.</returns>
    /// <remarks>
    /// This method enables observability tools to identify sub-regions within
    /// the composite page. For example, accesses to $C000-$C0FF might return
    /// <see cref="RegionTag.Io"/>, while $C100-$C7FF returns <see cref="RegionTag.Slot"/>.
    /// </remarks>
    RegionTag GetSubRegionTag(Addr offset);

    /// <summary>
    /// Enumerates all registered subregions within this composite target.
    /// </summary>
    /// <returns>
    /// An enumerable of subregion information tuples containing the start offset,
    /// size, region tag, and target name for each registered subregion.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method enables debug commands and observability tools to inspect the
    /// internal structure of composite targets. Subregions are returned in their
    /// registration order.
    /// </para>
    /// <para>
    /// For composite targets with dynamic dispatch logic (like <c>Pocket2eIOPage</c>),
    /// this method may return an empty enumerable or a fixed set of logical regions
    /// that don't correspond to registered subregions.
    /// </para>
    /// </remarks>
    IEnumerable<(Addr StartOffset, Addr Size, RegionTag Tag, string TargetName)> EnumerateSubRegions();
}