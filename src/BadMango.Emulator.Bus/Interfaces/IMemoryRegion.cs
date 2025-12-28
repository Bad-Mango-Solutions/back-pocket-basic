// <copyright file="IMemoryRegion.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Represents a logical memory region that can be mapped into the address space.
/// </summary>
/// <remarks>
/// <para>
/// Memory regions are the building blocks of the machine's address space. Each region
/// represents a contiguous block of storage (RAM, ROM, or device space) with a
/// preferred base address where it should be mapped.
/// </para>
/// <para>
/// Regions belong to Layer 0 (physical memory substrate) and provide hints to the
/// Layer 1 MMU about where they should be placed. The bring-up code uses these hints
/// to construct the initial page table before handing off to boot ROM.
/// </para>
/// <para>
/// The region does not own the mapping—it describes what should be mapped. The actual
/// page table entries are created by the MMU based on region definitions.
/// </para>
/// </remarks>
public interface IMemoryRegion
{
    /// <summary>
    /// Gets the unique identifier for this region.
    /// </summary>
    /// <value>A unique string identifier for diagnostics and lookup.</value>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name for this region.
    /// </summary>
    /// <value>A descriptive name for debugging and tools.</value>
    string Name { get; }

    /// <summary>
    /// Gets the preferred base address where this region should be mapped.
    /// </summary>
    /// <value>
    /// The suggested starting address in the machine's address space.
    /// For example, boot ROM might hint $00000000, main RAM might hint $00040000.
    /// </value>
    Addr PreferredBase { get; }

    /// <summary>
    /// Gets the size of the region in bytes.
    /// </summary>
    /// <value>The total size of the memory region.</value>
    uint Size { get; }

    /// <summary>
    /// Gets the region tag classifying this memory type.
    /// </summary>
    /// <value>The semantic classification of the region (RAM, ROM, IO, etc.).</value>
    RegionTag Tag { get; }

    /// <summary>
    /// Gets the default permissions for pages in this region.
    /// </summary>
    /// <value>The base permission flags for the region.</value>
    PagePerms DefaultPermissions { get; }

    /// <summary>
    /// Gets the capabilities of the underlying storage.
    /// </summary>
    /// <value>The capability flags describing what operations the storage supports.</value>
    TargetCaps Capabilities { get; }

    /// <summary>
    /// Gets the bus target for this region.
    /// </summary>
    /// <value>The target that handles read/write operations for this region.</value>
    IBusTarget Target { get; }

    /// <summary>
    /// Gets the physical memory backing this region, if any.
    /// </summary>
    /// <value>
    /// The underlying physical memory, or <see langword="null"/> if this region
    /// is backed by a device rather than physical memory.
    /// </value>
    IPhysicalMemory? PhysicalMemory { get; }

    /// <summary>
    /// Gets a value indicating whether this region is relocatable.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the region can be mapped at addresses other than
    /// <see cref="PreferredBase"/>; otherwise, <see langword="false"/>.
    /// </value>
    bool IsRelocatable { get; }

    /// <summary>
    /// Gets a value indicating whether this region can participate in mapping stacks (overlays).
    /// </summary>
    /// <value>
    /// <see langword="true"/> if this region can be pushed onto a mapping stack;
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool SupportsOverlay { get; }

    /// <summary>
    /// Gets the priority of this region for automatic mapping.
    /// </summary>
    /// <value>
    /// Lower values indicate higher priority. Regions with higher priority are
    /// mapped first when resolving conflicts. Boot ROM typically has priority 0.
    /// </value>
    int Priority { get; }
}