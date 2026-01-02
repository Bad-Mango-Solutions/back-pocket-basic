// <copyright file="IMemoryBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Main memory bus interface for routing CPU and DMA memory operations.
/// </summary>
/// <remarks>
/// <para>
/// The memory bus is the central routing fabric for all memory operations in the
/// emulated system. It implements page-based address translation, handles atomic
/// vs decomposed access decisions, and provides the foundation for observability.
/// </para>
/// <para>
/// The bus uses 4KB pages for routing, with each page resolving to a target device
/// and physical base address. Cross-page wide accesses are automatically decomposed
/// into individual byte operations.
/// </para>
/// <para>
/// The CPU does not own memory; all memory interactions flow through the bus.
/// The CPU computes intent; the bus enforces consequences.
/// </para>
/// <para>
/// The bus never silently fixes faults. Try-style APIs return <see cref="BusResult{T}"/>
/// or <see cref="BusFault"/> so the CPU knows exactly what happened and can translate
/// faults into its architecture's exception model.
/// </para>
/// <para>
/// NX enforcement triggers only on instruction fetch intent and is ignored in Compat mode.
/// </para>
/// </remarks>
public interface IMemoryBus
{
    /// <summary>
    /// Gets the page shift value for address-to-page translation.
    /// </summary>
    /// <value>
    /// The number of bits to shift an address to get the page index.
    /// For 4KB pages, this value is 12.
    /// </value>
    int PageShift { get; }

    /// <summary>
    /// Gets the page mask for extracting the offset within a page.
    /// </summary>
    /// <value>
    /// A bitmask that extracts the offset portion of an address.
    /// For 4KB pages, this value is 0xFFF.
    /// </value>
    Addr PageMask { get; }

    /// <summary>
    /// Gets the total number of pages in the address space.
    /// </summary>
    /// <value>The count of page table entries.</value>
    int PageCount { get; }

    /// <summary>
    /// Reads a single byte from the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>The byte value at the specified address.</returns>
    /// <remarks>
    /// <para>
    /// <b>Hot-path method:</b> This method assumes the caller has already validated
    /// the access via <see cref="TryRead8"/> or knows the page is mapped. Calling this
    /// method on an unmapped page will result in undefined behavior.
    /// </para>
    /// <para>
    /// For safe access with fault detection, use <see cref="TryRead8"/> instead.
    /// </para>
    /// </remarks>
    byte Read8(in BusAccess access);

    /// <summary>
    /// Writes a single byte to the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <param name="value">The byte value to write.</param>
    /// <remarks>
    /// <para>
    /// <b>Hot-path method:</b> This method assumes the caller has already validated
    /// the access via <see cref="TryWrite8"/> or knows the page is mapped and writable.
    /// Calling this method on an unmapped or read-only page will result in undefined behavior.
    /// </para>
    /// <para>
    /// For safe access with fault detection, use <see cref="TryWrite8"/> instead.
    /// </para>
    /// </remarks>
    void Write8(in BusAccess access, byte value);

    /// <summary>
    /// Reads a 16-bit word from the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>The 16-bit value at the specified address (little-endian).</returns>
    /// <remarks>
    /// <para>
    /// <b>Hot-path method:</b> This method assumes the caller has already validated
    /// the access via <see cref="TryRead16"/> or knows all affected pages are mapped.
    /// Calling this method on unmapped pages will result in undefined behavior.
    /// </para>
    /// <para>
    /// The bus determines whether to use atomic or decomposed access based on:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <see cref="AccessFlags.Decompose"/> flag (forces byte-wise cycles)</description></item>
    /// <item><description>Whether the access crosses a page boundary (always decomposes)</description></item>
    /// <item><description>The target's <see cref="TargetCaps.SupportsWide"/> capability</description></item>
    /// <item><description>The <see cref="AccessFlags.Atomic"/> flag and CPU mode defaults</description></item>
    /// </list>
    /// <para>
    /// For safe access with fault detection, use <see cref="TryRead16"/> instead.
    /// </para>
    /// </remarks>
    Word Read16(in BusAccess access);

    /// <summary>
    /// Writes a 16-bit word to the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <param name="value">The 16-bit value to write (little-endian).</param>
    /// <remarks>
    /// <para>
    /// <b>Hot-path method:</b> This method assumes the caller has already validated
    /// the access via <see cref="TryWrite16"/> or knows all affected pages are mapped and writable.
    /// Calling this method on unmapped or read-only pages will result in undefined behavior.
    /// </para>
    /// <para>
    /// The bus determines whether to use atomic or decomposed access based on:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <see cref="AccessFlags.Decompose"/> flag (forces byte-wise cycles)</description></item>
    /// <item><description>Whether the access crosses a page boundary (always decomposes)</description></item>
    /// <item><description>The target's <see cref="TargetCaps.SupportsWide"/> capability</description></item>
    /// <item><description>The <see cref="AccessFlags.Atomic"/> flag and CPU mode defaults</description></item>
    /// </list>
    /// <para>
    /// For safe access with fault detection, use <see cref="TryWrite16"/> instead.
    /// </para>
    /// </remarks>
    void Write16(in BusAccess access, Word value);

    /// <summary>
    /// Reads a 32-bit double word from the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>The 32-bit value at the specified address (little-endian).</returns>
    /// <remarks>
    /// <para>
    /// <b>Hot-path method:</b> This method assumes the caller has already validated
    /// the access via <see cref="TryRead32"/> or knows all affected pages are mapped.
    /// Calling this method on unmapped pages will result in undefined behavior.
    /// </para>
    /// <para>
    /// The bus determines whether to use atomic or decomposed access based on:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <see cref="AccessFlags.Decompose"/> flag (forces byte-wise cycles)</description></item>
    /// <item><description>Whether the access crosses a page boundary (always decomposes)</description></item>
    /// <item><description>The target's <see cref="TargetCaps.SupportsWide"/> capability</description></item>
    /// <item><description>The <see cref="AccessFlags.Atomic"/> flag and CPU mode defaults</description></item>
    /// </list>
    /// <para>
    /// For safe access with fault detection, use <see cref="TryRead32"/> instead.
    /// </para>
    /// </remarks>
    DWord Read32(in BusAccess access);

    /// <summary>
    /// Writes a 32-bit double word to the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <param name="value">The 32-bit value to write (little-endian).</param>
    /// <remarks>
    /// <para>
    /// <b>Hot-path method:</b> This method assumes the caller has already validated
    /// the access via <see cref="TryWrite32"/> or knows all affected pages are mapped and writable.
    /// Calling this method on unmapped or read-only pages will result in undefined behavior.
    /// </para>
    /// <para>
    /// The bus determines whether to use atomic or decomposed access based on:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <see cref="AccessFlags.Decompose"/> flag (forces byte-wise cycles)</description></item>
    /// <item><description>Whether the access crosses a page boundary (always decomposes)</description></item>
    /// <item><description>The target's <see cref="TargetCaps.SupportsWide"/> capability</description></item>
    /// <item><description>The <see cref="AccessFlags.Atomic"/> flag and CPU mode defaults</description></item>
    /// </list>
    /// <para>
    /// For safe access with fault detection, use <see cref="TryWrite32"/> instead.
    /// </para>
    /// </remarks>
    void Write32(in BusAccess access, DWord value);

    /// <summary>
    /// Attempts to read a single byte from the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>A result containing the value if successful, or fault information if not.</returns>
    /// <remarks>
    /// <para>
    /// This try-style API performs permission checks before touching the device
    /// and returns faults as first-class values rather than throwing exceptions.
    /// </para>
    /// <para>
    /// Permission checks include:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Read permission for data reads</description></item>
    /// <item><description>Execute permission for instruction fetch (NX check, Native mode only)</description></item>
    /// </list>
    /// </remarks>
    BusResult<byte> TryRead8(in BusAccess access);

    /// <summary>
    /// Attempts to write a single byte to the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <param name="value">The byte value to write.</param>
    /// <returns>A result indicating success or containing fault information.</returns>
    /// <remarks>
    /// This try-style API performs write permission checks before touching the device
    /// and returns faults as first-class values rather than throwing exceptions.
    /// </remarks>
    BusResult TryWrite8(in BusAccess access, byte value);

    /// <summary>
    /// Attempts to read a 16-bit word from the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>A result containing the value if successful, or fault information if not.</returns>
    /// <remarks>
    /// <para>
    /// Permission checks are performed per byte/page when access is decomposed
    /// or crosses a page boundary.
    /// </para>
    /// </remarks>
    BusResult<Word> TryRead16(in BusAccess access);

    /// <summary>
    /// Attempts to write a 16-bit word to the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <param name="value">The 16-bit value to write.</param>
    /// <returns>A result indicating success or containing fault information.</returns>
    BusResult TryWrite16(in BusAccess access, Word value);

    /// <summary>
    /// Attempts to read a 32-bit double word from the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>A result containing the value if successful, or fault information if not.</returns>
    /// <remarks>
    /// <para>
    /// Permission checks are performed per byte/page when access is decomposed
    /// or crosses a page boundary.
    /// </para>
    /// </remarks>
    BusResult<DWord> TryRead32(in BusAccess access);

    /// <summary>
    /// Attempts to write a 32-bit double word to the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <param name="value">The 32-bit value to write.</param>
    /// <returns>A result indicating success or containing fault information.</returns>
    BusResult TryWrite32(in BusAccess access, DWord value);

    /// <summary>
    /// Gets the page entry for the specified address.
    /// </summary>
    /// <param name="address">The address to look up.</param>
    /// <returns>The page entry containing routing and capability information.</returns>
    PageEntry GetPageEntry(Addr address);

    /// <summary>
    /// Maps a page to a target device.
    /// </summary>
    /// <param name="pageIndex">The index of the page to map.</param>
    /// <param name="entry">The page entry describing the mapping.</param>
    /// <remarks>
    /// This is a control plane operation used during system initialization,
    /// bank switching, and overlay changes. It should not be called during
    /// normal hot-path execution.
    /// </remarks>
    void MapPage(int pageIndex, PageEntry entry);

    /// <summary>
    /// Maps a range of pages to a target device.
    /// </summary>
    /// <param name="startPage">The index of the first page to map.</param>
    /// <param name="pageCount">The number of consecutive pages to map.</param>
    /// <param name="deviceId">The device identifier for all pages.</param>
    /// <param name="regionTag">The region type for all pages.</param>
    /// <param name="perms">The permissions for all pages.</param>
    /// <param name="caps">The capabilities for all pages.</param>
    /// <param name="target">The bus target for all pages.</param>
    /// <param name="physicalBase">The physical base address for the first page.</param>
    /// <remarks>
    /// This convenience method maps multiple consecutive pages with incrementing
    /// physical addresses. The physical address for page N is
    /// <paramref name="physicalBase"/> + (N - <paramref name="startPage"/>) * PageSize.
    /// </remarks>
    void MapPageRange(
        int startPage,
        int pageCount,
        int deviceId,
        RegionTag regionTag,
        PagePerms perms,
        TargetCaps caps,
        IBusTarget target,
        Addr physicalBase);

    /// <summary>
    /// Maps a contiguous memory region starting at a virtual address.
    /// </summary>
    /// <param name="virtualBase">The starting virtual address (must be page-aligned).</param>
    /// <param name="size">The size of the region in bytes (must be page-aligned).</param>
    /// <param name="deviceId">The device identifier for all pages.</param>
    /// <param name="regionTag">The region type for all pages.</param>
    /// <param name="perms">The permissions for all pages.</param>
    /// <param name="caps">The capabilities for all pages.</param>
    /// <param name="target">The bus target for all pages.</param>
    /// <param name="physicalBase">The physical base address for the first page.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="virtualBase"/> or <paramref name="size"/> is not page-aligned.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the region extends beyond the address space.
    /// </exception>
    /// <remarks>
    /// This address-based API simplifies mapping by accepting virtual addresses directly
    /// instead of requiring callers to compute page indices. For example,
    /// MapRegion(0xC000, 0x1000, ...) maps the I/O page starting at $C000.
    /// </remarks>
    void MapRegion(
        Addr virtualBase,
        Addr size,
        int deviceId,
        RegionTag regionTag,
        PagePerms perms,
        TargetCaps caps,
        IBusTarget target,
        Addr physicalBase);

    /// <summary>
    /// Maps a single page at the specified virtual address.
    /// </summary>
    /// <param name="virtualAddress">The virtual address of the page to map (must be page-aligned).</param>
    /// <param name="entry">The page entry describing the mapping.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="virtualAddress"/> is not page-aligned.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the address is beyond the address space.
    /// </exception>
    /// <remarks>
    /// This address-based API simplifies page mapping by accepting a virtual address directly
    /// instead of requiring callers to compute page indices. For example,
    /// MapPageAt(0xD000, entry) maps page $0D.
    /// </remarks>
    void MapPageAt(Addr virtualAddress, PageEntry entry);

    /// <summary>
    /// Sets a page entry by index (allocates L2 table if needed for sparse tables).
    /// </summary>
    /// <param name="pageIndex">The index of the page to set.</param>
    /// <param name="entry">The page entry describing the mapping.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageIndex"/> is out of range.
    /// </exception>
    /// <remarks>
    /// This method is functionally equivalent to <see cref="MapPage"/> but provides
    /// clearer semantics for sparse page table implementations where L2 tables may
    /// need to be allocated on first write.
    /// </remarks>
    void SetPageEntry(int pageIndex, PageEntry entry);

    /// <summary>
    /// Validates that an address and size are properly page-aligned.
    /// </summary>
    /// <param name="address">The address to validate.</param>
    /// <param name="size">The size to validate.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="address"/> or <paramref name="size"/> is not page-aligned.
    /// </exception>
    void ValidateAlignment(Addr address, Addr size);

    /// <summary>
    /// Gets the page entry by index for direct inspection.
    /// </summary>
    /// <param name="pageIndex">The page index.</param>
    /// <returns>A reference to the page entry.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageIndex"/> is out of range.
    /// </exception>
    ref readonly PageEntry GetPageEntryByIndex(int pageIndex);

    /// <summary>
    /// Atomically remaps a page to a different target.
    /// </summary>
    /// <param name="pageIndex">The page index to remap.</param>
    /// <param name="newTarget">The new target device.</param>
    /// <param name="newPhysBase">The new physical base within the target.</param>
    /// <remarks>
    /// <para>
    /// This method is used for language card and auxiliary memory bank switching
    /// in Apple II-compatible machines. It preserves the page's device ID, region tag,
    /// permissions, and capabilities while changing only the target and physical base.
    /// </para>
    /// </remarks>
    void RemapPage(int pageIndex, IBusTarget newTarget, Addr newPhysBase);

    /// <summary>
    /// Atomically remaps a page with full entry replacement.
    /// </summary>
    /// <param name="pageIndex">The page index to remap.</param>
    /// <param name="newEntry">The complete new page entry.</param>
    /// <remarks>
    /// This method replaces all page entry fields, including device ID, region tag,
    /// permissions, capabilities, target, and physical base.
    /// </remarks>
    void RemapPage(int pageIndex, PageEntry newEntry);

    /// <summary>
    /// Remaps a contiguous range of pages.
    /// </summary>
    /// <param name="startPage">The first page index to remap.</param>
    /// <param name="pageCount">The number of consecutive pages to remap.</param>
    /// <param name="newTarget">The new target device for all pages.</param>
    /// <param name="newPhysBase">The new physical base address for the first page.</param>
    /// <remarks>
    /// <para>
    /// This method preserves each page's device ID, region tag, permissions, and
    /// capabilities while changing the target and physical base. Physical addresses
    /// are computed as newPhysBase + (pageIndex - startPage) * pageSize.
    /// </para>
    /// </remarks>
    void RemapPageRange(int startPage, int pageCount, IBusTarget newTarget, Addr newPhysBase);

    /// <summary>
    /// Clears all mapped memory targets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method iterates through all unique targets mapped in the page table
    /// and calls their <see cref="IBusTarget.Clear"/> method. Each target is
    /// responsible for its own clearing behavior, allowing efficient implementations
    /// (e.g., <c>Array.Clear</c> for RAM).
    /// </para>
    /// <para>
    /// This operation is intended for testing scenarios and system reset. It should
    /// NEVER cause side effects. Read-only targets (ROM) will not be affected.
    /// </para>
    /// </remarks>
    void Clear();

    /// <summary>
    /// Creates a new mapping layer with the specified name and priority.
    /// </summary>
    /// <param name="name">A unique name for the layer.</param>
    /// <param name="priority">The priority of the layer. Higher values override lower values for the same address.</param>
    /// <returns>The newly created layer.</returns>
    /// <exception cref="ArgumentException">Thrown when a layer with the same name already exists.</exception>
    /// <remarks>
    /// <para>
    /// Layers are used to implement overlays such as Language Card ROM/RAM switching
    /// and auxiliary memory banks. Each layer can contain multiple mappings that
    /// become effective when the layer is activated.
    /// </para>
    /// </remarks>
    MappingLayer CreateLayer(string name, int priority);

    /// <summary>
    /// Gets a layer by name.
    /// </summary>
    /// <param name="name">The name of the layer to retrieve.</param>
    /// <returns>The layer if found; otherwise, <see langword="null"/>.</returns>
    MappingLayer? GetLayer(string name);

    /// <summary>
    /// Adds a mapping to a layer.
    /// </summary>
    /// <param name="mapping">The layered mapping to add.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the mapping's layer does not exist or when addresses are not page-aligned.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The mapping does not take effect immediately. It becomes effective when the
    /// containing layer is activated via <see cref="ActivateLayer"/>.
    /// </para>
    /// </remarks>
    void AddLayeredMapping(LayeredMapping mapping);

    /// <summary>
    /// Activates a layer, making its mappings effective.
    /// </summary>
    /// <param name="layerName">The name of the layer to activate.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no layer with the specified name exists.</exception>
    /// <remarks>
    /// <para>
    /// When a layer is activated, the bus recomputes effective page entries for all
    /// addresses covered by the layer's mappings. Higher priority active layers
    /// override lower priority layers for overlapping addresses.
    /// </para>
    /// </remarks>
    void ActivateLayer(string layerName);

    /// <summary>
    /// Deactivates a layer, removing its mappings from the effective page table.
    /// </summary>
    /// <param name="layerName">The name of the layer to deactivate.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no layer with the specified name exists.</exception>
    /// <remarks>
    /// <para>
    /// When a layer is deactivated, affected page entries are recomputed using the
    /// remaining active layers. This may reveal underlying layers' mappings.
    /// </para>
    /// </remarks>
    void DeactivateLayer(string layerName);

    /// <summary>
    /// Checks if a layer is currently active.
    /// </summary>
    /// <param name="layerName">The name of the layer to check.</param>
    /// <returns><see langword="true"/> if the layer is active; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no layer with the specified name exists.</exception>
    bool IsLayerActive(string layerName);

    /// <summary>
    /// Sets the permissions for all mappings in a layer.
    /// </summary>
    /// <param name="layerName">The name of the layer to modify.</param>
    /// <param name="perms">The new permissions to apply.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no layer with the specified name exists.</exception>
    /// <remarks>
    /// <para>
    /// This method updates the permissions for all mappings in the layer and
    /// recomputes affected page entries if the layer is active.
    /// </para>
    /// </remarks>
    void SetLayerPermissions(string layerName, PagePerms perms);

    /// <summary>
    /// Gets the effective page entry for an address, considering all active layers.
    /// </summary>
    /// <param name="address">The virtual address to query.</param>
    /// <returns>The effective page entry from the highest priority active layer, or the base page entry if no layers cover this address.</returns>
    /// <remarks>
    /// <para>
    /// This method returns the same entry that would be used for actual memory
    /// operations. It reflects the current state of all active layers.
    /// </para>
    /// </remarks>
    PageEntry GetEffectiveMapping(Addr address);

    /// <summary>
    /// Gets all mappings that cover the specified address, from all layers.
    /// </summary>
    /// <param name="address">The virtual address to query.</param>
    /// <returns>An enumerable of all layered mappings covering the address, regardless of active state.</returns>
    IEnumerable<LayeredMapping> GetAllMappingsAt(Addr address);

    /// <summary>
    /// Gets all layers that have mappings covering the specified address.
    /// </summary>
    /// <param name="address">The virtual address to query.</param>
    /// <returns>An enumerable of layers with mappings at this address, ordered by priority (highest first).</returns>
    IEnumerable<MappingLayer> GetLayersAt(Addr address);

    /// <summary>
    /// Creates a new swap group for mutually-exclusive bank switching.
    /// </summary>
    /// <param name="groupName">A unique name for the swap group.</param>
    /// <param name="virtualBase">The starting virtual address for the swap group's address range (must be page-aligned).</param>
    /// <param name="size">The size of the address range in bytes (must be page-aligned).</param>
    /// <returns>The unique identifier assigned to the new swap group.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when a swap group with the same name already exists,
    /// or when <paramref name="virtualBase"/> or <paramref name="size"/> is not page-aligned.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the address range extends beyond the address space.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Swap groups model hardware bank switching where multiple memory regions
    /// can occupy the same virtual address range, but only one is active at a time.
    /// This is distinct from layers (which stack); swap groups are mutually exclusive
    /// variants within a layer.
    /// </para>
    /// <para>
    /// The Language Card has two 4KB banks for D000−DFFF — only one is active at a time.
    /// Auxiliary memory has similar patterns. Swap groups implement this behavior.
    /// </para>
    /// </remarks>
    uint CreateSwapGroup(string groupName, Addr virtualBase, Addr size);

    /// <summary>
    /// Gets the unique identifier of a swap group by name.
    /// </summary>
    /// <param name="groupName">The name of the swap group.</param>
    /// <returns>The unique identifier of the swap group.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no swap group with the specified name exists.</exception>
    uint GetSwapGroupId(string groupName);

    /// <summary>
    /// Adds a variant to a swap group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the swap group.</param>
    /// <param name="variantName">A unique name for the variant within the swap group.</param>
    /// <param name="target">The bus target for this variant.</param>
    /// <param name="physBase">The physical base address within the target's address space.</param>
    /// <param name="perms">Permission flags for this variant's pages.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no swap group with the specified ID exists.</exception>
    /// <exception cref="ArgumentException">Thrown when a variant with the same name already exists in the swap group.</exception>
    /// <remarks>
    /// <para>
    /// Variants represent mutually exclusive memory configurations within a swap group.
    /// After adding variants, use <see cref="SelectSwapVariant"/> to make one active.
    /// </para>
    /// </remarks>
    void AddSwapVariant(uint groupId, string variantName, IBusTarget target, Addr physBase, PagePerms perms);

    /// <summary>
    /// Selects the active variant in a swap group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the swap group.</param>
    /// <param name="variantName">The name of the variant to select.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no swap group with the specified ID exists,
    /// or when no variant with the specified name exists in the swap group.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This operation atomically updates the page table entries for the swap group's
    /// address range to point to the selected variant's target and physical base.
    /// Only one variant can be active at a time; selecting a variant deactivates
    /// the previously active variant.
    /// </para>
    /// <para>
    /// Switching is atomic — there is no intermediate state where page entries
    /// are partially updated, preventing torn reads during the switch.
    /// </para>
    /// </remarks>
    void SelectSwapVariant(uint groupId, string variantName);

    /// <summary>
    /// Gets the name of the currently active variant in a swap group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the swap group.</param>
    /// <returns>The name of the active variant, or <see langword="null"/> if no variant is active.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no swap group with the specified ID exists.</exception>
    string? GetActiveSwapVariant(uint groupId);
}