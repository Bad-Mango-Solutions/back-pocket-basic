// <copyright file="MappingEntry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// Represents a single entry in a mapping stack for a page or page range.
/// </summary>
/// <remarks>
/// <para>
/// Mapping entries form the individual layers in a mapping stack. When the CPU
/// accesses memory, the topmost active entry in the stack determines where the
/// access is routed.
/// </para>
/// <para>
/// Each entry can have different permissions, capabilities, and routing targets,
/// allowing fine-grained control over overlays and bank switching scenarios.
/// </para>
/// </remarks>
/// <param name="Region">The memory region this entry maps to.</param>
/// <param name="IsActive">Whether this entry is currently active in the stack.</param>
/// <param name="Perms">Permission overrides for this mapping (or defaults from region if null).</param>
/// <param name="PhysicalOffset">Offset within the region's physical memory.</param>
/// <param name="Priority">Priority within the stack (lower = higher priority).</param>
/// <param name="Tag">Optional override for the region tag.</param>
public readonly record struct MappingEntry(
    IMemoryRegion Region,
    bool IsActive,
    PagePerms? Perms = null,
    uint PhysicalOffset = 0,
    int Priority = 0,
    RegionTag? Tag = null)
{
    /// <summary>
    /// Gets the effective permissions for this mapping entry.
    /// </summary>
    /// <value>
    /// The explicit permissions if set, otherwise the region's default permissions.
    /// </value>
    public PagePerms EffectivePermissions => Perms ?? Region.DefaultPermissions;

    /// <summary>
    /// Gets the effective region tag for this mapping entry.
    /// </summary>
    /// <value>
    /// The explicit tag if set, otherwise the region's tag.
    /// </value>
    public RegionTag EffectiveTag => Tag ?? Region.Tag;

    /// <summary>
    /// Creates a new entry with the active state toggled.
    /// </summary>
    /// <param name="active">The new active state.</param>
    /// <returns>A new mapping entry with the updated state.</returns>
    public MappingEntry WithActive(bool active) => this with { IsActive = active };

    /// <summary>
    /// Creates a new entry with different permissions.
    /// </summary>
    /// <param name="perms">The new permissions.</param>
    /// <returns>A new mapping entry with the updated permissions.</returns>
    public MappingEntry WithPermissions(PagePerms perms) => this with { Perms = perms };
}