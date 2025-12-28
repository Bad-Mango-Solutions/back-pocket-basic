// <copyright file="IMappingStack.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Represents a stack of mappings for a page or page range, supporting overlays and bank switching.
/// </summary>
/// <remarks>
/// <para>
/// Mapping stacks enable hardware behaviors like ROM overlays, bank switching, and
/// compatibility windows. Multiple mappings can exist for the same address range,
/// with the topmost active entry determining the actual routing.
/// </para>
/// <para>
/// This is a Layer 1 (Machine MMU) concept. The stack models hardware illusion,
/// not OS policy. Guest kernels build their own page tables on top of what the
/// machine MMU exposes.
/// </para>
/// <para>
/// Common use cases:
/// <list type="bullet">
/// <item><description>ROM/RAM switching (language card behavior)</description></item>
/// <item><description>Bank-switched memory expansion</description></item>
/// <item><description>Firmware shadowing</description></item>
/// <item><description>Debug overlays</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IMappingStack
{
    /// <summary>
    /// Gets the base address this stack applies to.
    /// </summary>
    /// <value>The starting address of the mapped range.</value>
    Addr BaseAddress { get; }

    /// <summary>
    /// Gets the size of the address range this stack covers.
    /// </summary>
    /// <value>The size in bytes.</value>
    uint Size { get; }

    /// <summary>
    /// Gets the number of entries in the stack.
    /// </summary>
    /// <value>The total count of mapping entries.</value>
    int Count { get; }

    /// <summary>
    /// Gets the currently active mapping entry.
    /// </summary>
    /// <value>
    /// The topmost active entry, or <see langword="null"/> if no entries are active.
    /// </value>
    MappingEntry? ActiveEntry { get; }

    /// <summary>
    /// Gets all entries in the stack, from bottom to top.
    /// </summary>
    /// <value>A read-only list of all mapping entries.</value>
    IReadOnlyList<MappingEntry> Entries { get; }

    /// <summary>
    /// Pushes a new mapping entry onto the stack.
    /// </summary>
    /// <param name="entry">The entry to push.</param>
    /// <remarks>
    /// The new entry becomes the topmost entry. If it is active, it becomes
    /// the current routing target.
    /// </remarks>
    void Push(MappingEntry entry);

    /// <summary>
    /// Removes and returns the topmost entry from the stack.
    /// </summary>
    /// <returns>The removed entry, or <see langword="null"/> if the stack is empty.</returns>
    MappingEntry? Pop();

    /// <summary>
    /// Activates or deactivates a specific entry by its region ID.
    /// </summary>
    /// <param name="regionId">The ID of the region to modify.</param>
    /// <param name="active">The new active state.</param>
    /// <returns>
    /// <see langword="true"/> if the entry was found and modified;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool SetActive(string regionId, bool active);

    /// <summary>
    /// Replaces the current stack with a single entry.
    /// </summary>
    /// <param name="entry">The entry to set as the only mapping.</param>
    /// <remarks>
    /// This is useful for atomic bank switches where the entire context changes.
    /// </remarks>
    void Replace(MappingEntry entry);

    /// <summary>
    /// Clears all entries from the stack.
    /// </summary>
    void Clear();

    /// <summary>
    /// Creates a page entry from the currently active mapping.
    /// </summary>
    /// <param name="deviceId">The device ID to assign to the page entry.</param>
    /// <param name="pageOffset">The page offset within this stack's range.</param>
    /// <param name="pageSize">The page size for calculating physical addresses.</param>
    /// <returns>
    /// A page entry suitable for the page table, or <see langword="null"/> if no mapping is active.
    /// </returns>
    PageEntry? ToPageEntry(int deviceId, uint pageOffset, uint pageSize = 4096);
}