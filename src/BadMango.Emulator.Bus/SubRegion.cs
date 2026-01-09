// <copyright file="SubRegion.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// Represents a subregion within a composite bus target.
/// </summary>
/// <param name="StartOffset">The starting offset within the composite region.</param>
/// <param name="Size">The size of the subregion in bytes.</param>
/// <param name="Target">The bus target to handle accesses to this subregion.</param>
/// <param name="Tag">The region tag for this subregion.</param>
/// <remarks>
/// <para>
/// Subregions define addressable areas within a composite target. Each subregion
/// maps a range of offsets to a specific bus target, allowing the composite to
/// dispatch accesses to the appropriate handler.
/// </para>
/// <para>
/// Subregions may or may not be backed by physical memory. They can participate
/// in instruction trapping, soft switch behaviors, and other bus-level operations.
/// </para>
/// </remarks>
public readonly record struct SubRegion(Addr StartOffset, Addr Size, IBusTarget Target, RegionTag Tag)
{
    /// <summary>
    /// Gets the end offset (exclusive) of this subregion.
    /// </summary>
    public Addr EndOffset => StartOffset + Size;

    /// <summary>
    /// Determines whether the specified offset falls within this subregion.
    /// </summary>
    /// <param name="offset">The offset to check.</param>
    /// <returns>
    /// <see langword="true"/> if the offset is within this subregion; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Contains(Addr offset) => offset >= StartOffset && offset < EndOffset;
}