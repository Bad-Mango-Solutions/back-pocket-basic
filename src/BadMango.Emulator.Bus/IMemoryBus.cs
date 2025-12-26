// <copyright file="IMemoryBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

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
    byte Read8(in BusAccess access);

    /// <summary>
    /// Writes a single byte to the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <param name="value">The byte value to write.</param>
    void Write8(in BusAccess access, byte value);

    /// <summary>
    /// Reads a 16-bit word from the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>The 16-bit value at the specified address (little-endian).</returns>
    /// <remarks>
    /// <para>
    /// The bus determines whether to use atomic or decomposed access based on:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <see cref="AccessFlags.Decompose"/> flag (forces byte-wise cycles)</description></item>
    /// <item><description>Whether the access crosses a page boundary (always decomposes)</description></item>
    /// <item><description>The target's <see cref="TargetCaps.SupportsWide"/> capability</description></item>
    /// <item><description>The <see cref="AccessFlags.Atomic"/> flag and CPU mode defaults</description></item>
    /// </list>
    /// </remarks>
    Word Read16(in BusAccess access);

    /// <summary>
    /// Writes a 16-bit word to the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <param name="value">The 16-bit value to write (little-endian).</param>
    /// <remarks>
    /// <para>
    /// The bus determines whether to use atomic or decomposed access based on:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <see cref="AccessFlags.Decompose"/> flag (forces byte-wise cycles)</description></item>
    /// <item><description>Whether the access crosses a page boundary (always decomposes)</description></item>
    /// <item><description>The target's <see cref="TargetCaps.SupportsWide"/> capability</description></item>
    /// <item><description>The <see cref="AccessFlags.Atomic"/> flag and CPU mode defaults</description></item>
    /// </list>
    /// </remarks>
    void Write16(in BusAccess access, Word value);

    /// <summary>
    /// Reads a 32-bit double word from the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <returns>The 32-bit value at the specified address (little-endian).</returns>
    /// <remarks>
    /// <para>
    /// The bus determines whether to use atomic or decomposed access based on:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <see cref="AccessFlags.Decompose"/> flag (forces byte-wise cycles)</description></item>
    /// <item><description>Whether the access crosses a page boundary (always decomposes)</description></item>
    /// <item><description>The target's <see cref="TargetCaps.SupportsWide"/> capability</description></item>
    /// <item><description>The <see cref="AccessFlags.Atomic"/> flag and CPU mode defaults</description></item>
    /// </list>
    /// </remarks>
    DWord Read32(in BusAccess access);

    /// <summary>
    /// Writes a 32-bit double word to the specified address.
    /// </summary>
    /// <param name="access">The access context describing the operation.</param>
    /// <param name="value">The 32-bit value to write (little-endian).</param>
    /// <remarks>
    /// <para>
    /// The bus determines whether to use atomic or decomposed access based on:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <see cref="AccessFlags.Decompose"/> flag (forces byte-wise cycles)</description></item>
    /// <item><description>Whether the access crosses a page boundary (always decomposes)</description></item>
    /// <item><description>The target's <see cref="TargetCaps.SupportsWide"/> capability</description></item>
    /// <item><description>The <see cref="AccessFlags.Atomic"/> flag and CPU mode defaults</description></item>
    /// </list>
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
}