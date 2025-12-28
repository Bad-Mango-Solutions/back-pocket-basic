// <copyright file="IRegionManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Manages memory regions and their mapping stacks for the Layer 1 Machine MMU.
/// </summary>
/// <remarks>
/// <para>
/// The region manager is the control plane for the machine's memory map. It holds
/// all defined memory regions and their associated mapping stacks, providing the
/// foundation for bring-up code to construct the initial page table.
/// </para>
/// <para>
/// This interface operates at the region level, not the page level. Operations here
/// affect entire memory blocks. The actual page table is built by iterating through
/// regions and their stacks.
/// </para>
/// <para>
/// The design supports future hypervisor integration by:
/// <list type="bullet">
/// <item><description>Keeping region operations atomic and deterministic</description></item>
/// <item><description>Supporting snapshot/restore of mapping stacks</description></item>
/// <item><description>Providing enumeration for region auditing</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IRegionManager
{
    /// <summary>
    /// Gets the total number of registered regions.
    /// </summary>
    /// <value>The count of memory regions.</value>
    int RegionCount { get; }

    /// <summary>
    /// Gets all registered regions.
    /// </summary>
    /// <value>A read-only collection of all memory regions.</value>
    IReadOnlyCollection<IMemoryRegion> Regions { get; }

    /// <summary>
    /// Registers a new memory region with the manager.
    /// </summary>
    /// <param name="region">The region to register.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if a region with the same ID already exists.
    /// </exception>
    void RegisterRegion(IMemoryRegion region);

    /// <summary>
    /// Removes a region from the manager.
    /// </summary>
    /// <param name="regionId">The ID of the region to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the region was found and removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool UnregisterRegion(string regionId);

    /// <summary>
    /// Gets a region by its ID.
    /// </summary>
    /// <param name="regionId">The unique identifier of the region.</param>
    /// <returns>The region, or <see langword="null"/> if not found.</returns>
    IMemoryRegion? GetRegion(string regionId);

    /// <summary>
    /// Gets all regions that overlap with a given address range.
    /// </summary>
    /// <param name="startAddress">The start of the address range.</param>
    /// <param name="size">The size of the address range.</param>
    /// <returns>All regions whose preferred mapping overlaps the range.</returns>
    IEnumerable<IMemoryRegion> GetRegionsInRange(Addr startAddress, uint size);

    /// <summary>
    /// Gets the mapping stack for a given address.
    /// </summary>
    /// <param name="address">The address to query.</param>
    /// <returns>
    /// The mapping stack covering that address, or <see langword="null"/> if no stack exists.
    /// </returns>
    IMappingStack? GetMappingStack(Addr address);

    /// <summary>
    /// Creates or gets a mapping stack for a specific address range.
    /// </summary>
    /// <param name="baseAddress">The base address of the stack.</param>
    /// <param name="size">The size of the address range.</param>
    /// <returns>The mapping stack for the specified range.</returns>
    IMappingStack GetOrCreateMappingStack(Addr baseAddress, uint size);

    /// <summary>
    /// Builds page table entries for all currently mapped regions.
    /// </summary>
    /// <param name="bus">The memory bus to populate with page entries.</param>
    /// <param name="pageSize">The page size to use for mapping.</param>
    /// <remarks>
    /// This method iterates through all regions sorted by priority and constructs
    /// page entries from their mapping stacks. It is typically called during
    /// machine bring-up before control transfers to boot ROM.
    /// </remarks>
    void BuildPageTable(IMemoryBus bus, uint pageSize = 4096);

    /// <summary>
    /// Maps a region at its preferred base address with default settings.
    /// </summary>
    /// <param name="region">The region to map.</param>
    /// <param name="active">Whether the mapping should be active initially.</param>
    /// <remarks>
    /// This is a convenience method that creates a mapping entry and pushes it
    /// onto the appropriate stack at the region's preferred base address.
    /// </remarks>
    void MapRegionAtPreferred(IMemoryRegion region, bool active = true);

    /// <summary>
    /// Maps a region at a specific address.
    /// </summary>
    /// <param name="region">The region to map.</param>
    /// <param name="baseAddress">The address to map the region at.</param>
    /// <param name="active">Whether the mapping should be active initially.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the region is not relocatable and the address differs from its preferred base.
    /// </exception>
    void MapRegionAt(IMemoryRegion region, Addr baseAddress, bool active = true);

    /// <summary>
    /// Activates a region's mapping in the address space.
    /// </summary>
    /// <param name="regionId">The ID of the region to activate.</param>
    /// <returns>
    /// <see langword="true"/> if the region was found and activated;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool ActivateRegion(string regionId);

    /// <summary>
    /// Deactivates a region's mapping in the address space.
    /// </summary>
    /// <param name="regionId">The ID of the region to deactivate.</param>
    /// <returns>
    /// <see langword="true"/> if the region was found and deactivated;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool DeactivateRegion(string regionId);

    /// <summary>
    /// Performs an atomic bank switch by activating one region and deactivating another.
    /// </summary>
    /// <param name="activateRegionId">The ID of the region to activate.</param>
    /// <param name="deactivateRegionId">The ID of the region to deactivate.</param>
    /// <returns>
    /// <see langword="true"/> if both operations succeeded;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This operation is atomic—both changes happen together or neither does.
    /// </remarks>
    bool SwitchBank(string activateRegionId, string deactivateRegionId);

    /// <summary>
    /// Creates a snapshot of the current mapping state.
    /// </summary>
    /// <returns>An opaque snapshot that can be restored later.</returns>
    /// <remarks>
    /// This supports hypervisor-level operations that need to save and restore
    /// complete memory configurations atomically.
    /// </remarks>
    object CreateSnapshot();

    /// <summary>
    /// Restores mapping state from a previously created snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore.</param>
    /// <exception cref="ArgumentException">Thrown if the snapshot is invalid.</exception>
    void RestoreSnapshot(object snapshot);
}