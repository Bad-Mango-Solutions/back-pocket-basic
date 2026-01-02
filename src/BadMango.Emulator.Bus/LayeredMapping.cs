// <copyright file="LayeredMapping.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// Represents a mapping definition within a specific layer.
/// </summary>
/// <remarks>
/// <para>
/// Layered mappings define how a region of virtual address space should be routed
/// when its containing layer is active. Multiple layers can define mappings for
/// the same address range, with higher priority layers taking precedence.
/// </para>
/// <para>
/// Unlike direct page table entries, layered mappings are stored separately and
/// only become "effective" when their layer is active. When layers are activated
/// or deactivated, the bus recomputes the effective page table entries for all
/// affected addresses.
/// </para>
/// </remarks>
/// <param name="VirtualBase">The starting virtual address of this mapping (must be page-aligned).</param>
/// <param name="Size">The size of the mapped region in bytes (must be page-aligned).</param>
/// <param name="Layer">The layer this mapping belongs to.</param>
/// <param name="DeviceId">The structural identifier of the device handling this region.</param>
/// <param name="RegionTag">The classification of the memory region type.</param>
/// <param name="Perms">Permission flags controlling read, write, and execute access.</param>
/// <param name="Caps">Capability flags for the target device.</param>
/// <param name="Target">The bus target implementation for this region.</param>
/// <param name="PhysBase">The physical base address within the target's address space.</param>
public readonly record struct LayeredMapping(
    Addr VirtualBase,
    Addr Size,
    MappingLayer Layer,
    int DeviceId,
    RegionTag RegionTag,
    PagePerms Perms,
    TargetCaps Caps,
    IBusTarget Target,
    Addr PhysBase)
{
    /// <summary>
    /// Gets the ending virtual address of this mapping (exclusive).
    /// </summary>
    /// <value>The address immediately after the last mapped byte.</value>
    public Addr VirtualEnd => VirtualBase + Size;

    /// <summary>
    /// Checks if this mapping covers the specified address.
    /// </summary>
    /// <param name="address">The virtual address to check.</param>
    /// <returns><see langword="true"/> if this mapping covers the address; otherwise, <see langword="false"/>.</returns>
    public bool ContainsAddress(Addr address) => address >= VirtualBase && address < VirtualEnd;

    /// <summary>
    /// Gets the starting page index for this mapping.
    /// </summary>
    /// <param name="pageShift">The page shift value (e.g., 12 for 4KB pages).</param>
    /// <returns>The index of the first page covered by this mapping.</returns>
    public int GetStartPage(int pageShift) => (int)(VirtualBase >> pageShift);

    /// <summary>
    /// Gets the number of pages covered by this mapping.
    /// </summary>
    /// <param name="pageShift">The page shift value (e.g., 12 for 4KB pages).</param>
    /// <returns>The number of pages in this mapping.</returns>
    public int GetPageCount(int pageShift) => (int)(Size >> pageShift);
}