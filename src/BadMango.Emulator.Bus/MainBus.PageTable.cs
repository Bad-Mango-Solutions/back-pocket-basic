// <copyright file="MainBus.PageTable.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// Page table management operations for the main memory bus.
/// </summary>
public sealed partial class MainBus
{
    /// <inheritdoc />
    public PageEntry GetPageEntry(Addr address)
    {
        int pageIndex = (int)(address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return default;
        }

        return pageTable[pageIndex];
    }

    /// <summary>
    /// Gets the page entry by index for direct inspection.
    /// </summary>
    /// <param name="pageIndex">The page index.</param>
    /// <returns>A reference to the page entry.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageIndex"/> is out of range.
    /// </exception>
    public ref readonly PageEntry GetPageEntryByIndex(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), pageIndex, $"Page index must be between 0 and {pageTable.Length - 1}.");
        }

        return ref pageTable[pageIndex];
    }

    /// <inheritdoc />
    public void MapPage(int pageIndex, PageEntry entry)
    {
        if (pageIndex < 0 || pageIndex >= pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), pageIndex, $"Page index must be between 0 and {pageTable.Length - 1}.");
        }

        pageTable[pageIndex] = entry;
    }

    /// <inheritdoc />
    public void MapPageRange(
        int startPage,
        int pageCount,
        int deviceId,
        RegionTag regionTag,
        PagePerms perms,
        TargetCaps caps,
        IBusTarget target,
        Addr physicalBase)
    {
        if (startPage < 0 || startPage >= pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startPage), startPage, $"Start page must be between 0 and {pageTable.Length - 1}.");
        }

        if (pageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, "Page count cannot be negative.");
        }

        if (startPage + pageCount > pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, $"Page range ({startPage} + {pageCount}) exceeds address space ({pageTable.Length} pages).");
        }

        for (int i = 0; i < pageCount; i++)
        {
            Addr pagePhysBase = physicalBase + (Addr)(i * PageSize);
            pageTable[startPage + i] = new(
                deviceId,
                regionTag,
                perms,
                caps,
                target,
                pagePhysBase);
        }
    }

    /// <inheritdoc />
    public void MapRegion(
        Addr virtualBase,
        Addr size,
        int deviceId,
        RegionTag regionTag,
        PagePerms perms,
        TargetCaps caps,
        IBusTarget target,
        Addr physicalBase)
    {
        ValidateAlignment(virtualBase, size);

        int startPage = (int)(virtualBase >> PageShift);
        int pageCount = (int)(size >> PageShift);

        if (startPage + pageCount > pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, $"Region (0x{virtualBase:X} + 0x{size:X}) exceeds address space.");
        }

        MapPageRange(startPage, pageCount, deviceId, regionTag, perms, caps, target, physicalBase);
    }

    /// <inheritdoc />
    public void MapPageAt(Addr virtualAddress, PageEntry entry)
    {
        if ((virtualAddress & PageMask) != 0)
        {
            throw new ArgumentException($"Virtual address 0x{virtualAddress:X} is not page-aligned.", nameof(virtualAddress));
        }

        int pageIndex = (int)(virtualAddress >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(virtualAddress), virtualAddress, $"Virtual address 0x{virtualAddress:X} is beyond address space.");
        }

        MapPage(pageIndex, entry);
    }

    /// <inheritdoc />
    public void SetPageEntry(int pageIndex, PageEntry entry)
    {
        MapPage(pageIndex, entry);
    }

    /// <inheritdoc />
    public void ValidateAlignment(Addr address, Addr size)
    {
        if ((address & PageMask) != 0)
        {
            throw new ArgumentException($"Address 0x{address:X} is not page-aligned.", nameof(address));
        }

        if ((size & PageMask) != 0)
        {
            throw new ArgumentException($"Size 0x{size:X} is not page-aligned.", nameof(size));
        }
    }

    /// <summary>
    /// Atomically remaps a page to a different target.
    /// Used for language card and auxiliary memory bank switching.
    /// </summary>
    /// <param name="pageIndex">The page index to remap.</param>
    /// <param name="newTarget">The new target device.</param>
    /// <param name="newPhysBase">The new physical base within the target.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageIndex"/> is out of range.
    /// </exception>
    public void RemapPage(int pageIndex, IBusTarget newTarget, Addr newPhysBase)
    {
        if (pageIndex < 0 || pageIndex >= pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), pageIndex, $"Page index must be between 0 and {pageTable.Length - 1}.");
        }

        ref var entry = ref pageTable[pageIndex];
        pageTable[pageIndex] = entry with
        {
            Target = newTarget,
            PhysicalBase = newPhysBase,
        };
    }

    /// <summary>
    /// Atomically remaps a page with full entry replacement.
    /// </summary>
    /// <param name="pageIndex">The page index to remap.</param>
    /// <param name="newEntry">The complete new page entry.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageIndex"/> is out of range.
    /// </exception>
    public void RemapPage(int pageIndex, PageEntry newEntry)
    {
        if (pageIndex < 0 || pageIndex >= pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), pageIndex, $"Page index must be between 0 and {pageTable.Length - 1}.");
        }

        pageTable[pageIndex] = newEntry;
    }

    /// <summary>
    /// Remaps a contiguous range of pages.
    /// </summary>
    /// <param name="startPage">The first page index to remap.</param>
    /// <param name="pageCount">The number of consecutive pages to remap.</param>
    /// <param name="newTarget">The new target device for all pages.</param>
    /// <param name="newPhysBase">The new physical base address for the first page.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the range exceeds address space bounds.
    /// </exception>
    public void RemapPageRange(int startPage, int pageCount, IBusTarget newTarget, Addr newPhysBase)
    {
        if (startPage < 0 || startPage >= pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startPage), startPage, $"Start page must be between 0 and {pageTable.Length - 1}.");
        }

        if (pageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, "Page count cannot be negative.");
        }

        if (startPage + pageCount > pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, $"Page range ({startPage} + {pageCount}) exceeds address space ({pageTable.Length} pages).");
        }

        for (int i = 0; i < pageCount; i++)
        {
            ref var entry = ref pageTable[startPage + i];
            Addr pagePhysBase = newPhysBase + (Addr)(i * PageSize);
            pageTable[startPage + i] = entry with
            {
                Target = newTarget,
                PhysicalBase = pagePhysBase,
            };
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        // Collect unique targets to avoid clearing the same target multiple times
        // (e.g., when multiple pages map to the same RAM target)
        var clearedTargets = new HashSet<IBusTarget>(ReferenceEqualityComparer.Instance);

        for (int i = 0; i < pageTable.Length; i++)
        {
            var target = pageTable[i].Target;
            if (target is not null && clearedTargets.Add(target))
            {
                target.Clear();
            }
        }
    }
}