// <copyright file="MainBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// The main memory bus implementation for routing CPU and DMA memory operations.
/// </summary>
/// <remarks>
/// <para>
/// This is the core implementation of <see cref="IMemoryBus"/> that provides
/// page-based address translation, handles atomic vs decomposed access decisions,
/// and provides the foundation for observability.
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
/// </remarks>
public sealed class MainBus : IMemoryBus
{
    /// <summary>
    /// The default page shift value for 4KB pages.
    /// </summary>
    private const int DefaultPageShift = 12;

    /// <summary>
    /// The default page mask for 4KB pages (0xFFF).
    /// </summary>
    private const Addr DefaultPageMask = 0xFFF;

    /// <summary>
    /// The page size in bytes (4KB).
    /// </summary>
    private const int PageSize = 1 << DefaultPageShift;

    /// <summary>
    /// The page table array for O(1) address-to-page translation.
    /// </summary>
    private readonly PageEntry[] pageTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainBus"/> class with the specified address space size.
    /// </summary>
    /// <param name="addressSpaceBits">
    /// The number of bits in the address space. Defaults to 16 for a 64KB address space.
    /// For 128KB, use 17. For 16MB (65C816), use 24. For 4GB (65832), use 32.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="addressSpaceBits"/> is less than 12 (minimum for one 4KB page)
    /// or greater than 32.
    /// </exception>
    public MainBus(int addressSpaceBits = 16)
    {
        if (addressSpaceBits < DefaultPageShift)
        {
            throw new ArgumentOutOfRangeException(
                nameof(addressSpaceBits),
                addressSpaceBits,
                $"Address space must be at least {DefaultPageShift} bits to support 4KB pages.");
        }

        if (addressSpaceBits > 32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(addressSpaceBits),
                addressSpaceBits,
                "Address space cannot exceed 32 bits.");
        }

        int pageCount = 1 << (addressSpaceBits - DefaultPageShift);
        pageTable = new PageEntry[pageCount];
    }

    /// <inheritdoc />
    public int PageShift => DefaultPageShift;

    /// <inheritdoc />
    public Addr PageMask => DefaultPageMask;

    /// <inheritdoc />
    public int PageCount => pageTable.Length;

    /// <inheritdoc />
    public byte Read8(in BusAccess access)
    {
        ref readonly var page = ref pageTable[access.Address >> PageShift];
        Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);

        // Handle composite target dispatch
        if (page.Target is ICompositeTarget composite)
        {
            Addr offset = access.Address & PageMask;
            var subTarget = composite.ResolveTarget(offset, access.Intent);
            if (subTarget is not null)
            {
                return subTarget.Read8(physicalAddress, access);
            }

            // No sub-target found, return floating bus value
            return 0xFF;
        }

        return page.Target!.Read8(physicalAddress, access);
    }

    /// <inheritdoc />
    public void Write8(in BusAccess access, byte value)
    {
        ref readonly var page = ref pageTable[access.Address >> PageShift];
        Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);

        // Handle composite target dispatch
        if (page.Target is ICompositeTarget composite)
        {
            Addr offset = access.Address & PageMask;
            var subTarget = composite.ResolveTarget(offset, access.Intent);
            if (subTarget is not null)
            {
                subTarget.Write8(physicalAddress, value, access);
            }

            return;
        }

        page.Target?.Write8(physicalAddress, value, access);
    }

    /// <inheritdoc />
    public Word Read16(in BusAccess access)
    {
        // Cross-page check: always decompose
        if (CrossesPageBoundary(access.Address, 2))
        {
            return DecomposeRead16(access);
        }

        // Decompose flag forces byte-wise
        if (access.IsDecomposeForced)
        {
            return DecomposeRead16(access);
        }

        ref readonly var page = ref pageTable[access.Address >> PageShift];

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            return page.Target!.Read16(physicalAddress, access);
        }

        // Compat mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == CpuMode.Compat)
        {
            return DecomposeRead16(access);
        }

        // Native mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            return page.Target!.Read16(physicalAddress, access);
        }

        return DecomposeRead16(access);
    }

    /// <inheritdoc />
    public void Write16(in BusAccess access, Word value)
    {
        // Cross-page check: always decompose
        if (CrossesPageBoundary(access.Address, 2))
        {
            DecomposeWrite16(access, value);
            return;
        }

        // Decompose flag forces byte-wise
        if (access.IsDecomposeForced)
        {
            DecomposeWrite16(access, value);
            return;
        }

        ref readonly var page = ref pageTable[access.Address >> PageShift];

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target!.Write16(physicalAddress, value, access);
            return;
        }

        // Compat mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == CpuMode.Compat)
        {
            DecomposeWrite16(access, value);
            return;
        }

        // Native mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target!.Write16(physicalAddress, value, access);
            return;
        }

        DecomposeWrite16(access, value);
    }

    /// <inheritdoc />
    public DWord Read32(in BusAccess access)
    {
        // Cross-page check: always decompose
        if (CrossesPageBoundary(access.Address, 4))
        {
            return DecomposeRead32(access);
        }

        // Decompose flag forces byte-wise
        if (access.IsDecomposeForced)
        {
            return DecomposeRead32(access);
        }

        ref readonly var page = ref pageTable[access.Address >> PageShift];

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            return page.Target!.Read32(physicalAddress, access);
        }

        // Compat mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == CpuMode.Compat)
        {
            return DecomposeRead32(access);
        }

        // Native mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            return page.Target!.Read32(physicalAddress, access);
        }

        return DecomposeRead32(access);
    }

    /// <inheritdoc />
    public void Write32(in BusAccess access, DWord value)
    {
        // Cross-page check: always decompose
        if (CrossesPageBoundary(access.Address, 4))
        {
            DecomposeWrite32(access, value);
            return;
        }

        // Decompose flag forces byte-wise
        if (access.IsDecomposeForced)
        {
            DecomposeWrite32(access, value);
            return;
        }

        ref readonly var page = ref pageTable[access.Address >> PageShift];

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target!.Write32(physicalAddress, value, access);
            return;
        }

        // Compat mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == CpuMode.Compat)
        {
            DecomposeWrite32(access, value);
            return;
        }

        // Native mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target!.Write32(physicalAddress, value, access);
            return;
        }

        DecomposeWrite32(access, value);
    }

    /// <inheritdoc />
    public BusResult<byte> TryRead8(in BusAccess access)
    {
        int pageIndex = (int)(access.Address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return BusFault.Unmapped(access);
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return BusFault.Unmapped(access);
        }

        // Check read permission
        if (!page.CanRead)
        {
            return BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag);
        }

        // Check NX on instruction fetch (Native mode only)
        if (access.Intent == AccessIntent.InstructionFetch &&
            access.Mode == CpuMode.Native &&
            !page.CanExecute)
        {
            return BusFault.NoExecute(access, page.DeviceId, page.RegionTag);
        }

        // Handle composite target dispatch
        if (page.Target is ICompositeTarget composite)
        {
            Addr offset = access.Address & PageMask;
            var subTarget = composite.ResolveTarget(offset, access.Intent);
            if (subTarget is null)
            {
                // No sub-target found, return floating bus value
                return BusResult<byte>.Success(0xFF, access, page.DeviceId, page.RegionTag, cycles: 1);
            }

            Addr physicalAddress = page.PhysicalBase + offset;
            byte value = subTarget.Read8(physicalAddress, access);
            return BusResult<byte>.Success(value, access, page.DeviceId, composite.GetSubRegionTag(offset), cycles: 1);
        }

        // Perform the read
        Addr physAddr = page.PhysicalBase + (access.Address & PageMask);
        byte readValue = page.Target.Read8(physAddr, access);

        return BusResult<byte>.Success(readValue, access, page.DeviceId, page.RegionTag, cycles: 1);
    }

    /// <inheritdoc />
    public BusFault TryWrite8(in BusAccess access, byte value)
    {
        int pageIndex = (int)(access.Address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return BusFault.Unmapped(access);
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return BusFault.Unmapped(access);
        }

        // Check write permission
        if (!page.CanWrite)
        {
            return BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag);
        }

        // Handle composite target dispatch
        if (page.Target is ICompositeTarget composite)
        {
            Addr offset = access.Address & PageMask;
            var subTarget = composite.ResolveTarget(offset, access.Intent);
            if (subTarget is not null)
            {
                Addr physicalAddress = page.PhysicalBase + offset;
                subTarget.Write8(physicalAddress, value, access);
            }

            return BusFault.Success(access, page.DeviceId, page.RegionTag);
        }

        // Perform the write
        Addr physAddr = page.PhysicalBase + (access.Address & PageMask);
        page.Target.Write8(physAddr, value, access);

        return BusFault.Success(access, page.DeviceId, page.RegionTag);
    }

    /// <inheritdoc />
    public BusResult<Word> TryRead16(in BusAccess access)
    {
        // Cross-page check: always decompose
        if (CrossesPageBoundary(access.Address, 2))
        {
            return DecomposeTryRead16(access);
        }

        // Decompose flag forces byte-wise
        if (access.IsDecomposeForced)
        {
            return DecomposeTryRead16(access);
        }

        int pageIndex = (int)(access.Address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return BusFault.Unmapped(access);
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return BusFault.Unmapped(access);
        }

        // Check read permission
        if (!page.CanRead)
        {
            return BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag);
        }

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            Word value = page.Target.Read16(physicalAddress, access);
            return BusResult<Word>.Success(value, access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        // Compat mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == CpuMode.Compat)
        {
            return DecomposeTryRead16(access);
        }

        // Native mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            Word value = page.Target.Read16(physicalAddress, access);
            return BusResult<Word>.Success(value, access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        return DecomposeTryRead16(access);
    }

    /// <inheritdoc />
    public BusFault TryWrite16(in BusAccess access, Word value)
    {
        // Cross-page check: always decompose
        if (CrossesPageBoundary(access.Address, 2))
        {
            return DecomposeTryWrite16(access, value);
        }

        // Decompose flag forces byte-wise
        if (access.IsDecomposeForced)
        {
            return DecomposeTryWrite16(access, value);
        }

        int pageIndex = (int)(access.Address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return BusFault.Unmapped(access);
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return BusFault.Unmapped(access);
        }

        // Check write permission
        if (!page.CanWrite)
        {
            return BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag);
        }

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target.Write16(physicalAddress, value, access);
            return BusFault.Success(access, page.DeviceId, page.RegionTag);
        }

        // Compat mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == CpuMode.Compat)
        {
            return DecomposeTryWrite16(access, value);
        }

        // Native mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target.Write16(physicalAddress, value, access);
            return BusFault.Success(access, page.DeviceId, page.RegionTag);
        }

        return DecomposeTryWrite16(access, value);
    }

    /// <inheritdoc />
    public BusResult<DWord> TryRead32(in BusAccess access)
    {
        // Cross-page check: always decompose
        if (CrossesPageBoundary(access.Address, 4))
        {
            return DecomposeTryRead32(access);
        }

        // Decompose flag forces byte-wise
        if (access.IsDecomposeForced)
        {
            return DecomposeTryRead32(access);
        }

        int pageIndex = (int)(access.Address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return BusFault.Unmapped(access);
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return BusFault.Unmapped(access);
        }

        // Check read permission
        if (!page.CanRead)
        {
            return BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag);
        }

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            DWord value = page.Target.Read32(physicalAddress, access);
            return BusResult<DWord>.Success(value, access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        // Compat mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == CpuMode.Compat)
        {
            return DecomposeTryRead32(access);
        }

        // Native mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            DWord value = page.Target.Read32(physicalAddress, access);
            return BusResult<DWord>.Success(value, access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        return DecomposeTryRead32(access);
    }

    /// <inheritdoc />
    public BusFault TryWrite32(in BusAccess access, DWord value)
    {
        // Cross-page check: always decompose
        if (CrossesPageBoundary(access.Address, 4))
        {
            return DecomposeTryWrite32(access, value);
        }

        // Decompose flag forces byte-wise
        if (access.IsDecomposeForced)
        {
            return DecomposeTryWrite32(access, value);
        }

        int pageIndex = (int)(access.Address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return BusFault.Unmapped(access);
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return BusFault.Unmapped(access);
        }

        // Check write permission
        if (!page.CanWrite)
        {
            return BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag);
        }

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target.Write32(physicalAddress, value, access);
            return BusFault.Success(access, page.DeviceId, page.RegionTag);
        }

        // Compat mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == CpuMode.Compat)
        {
            return DecomposeTryWrite32(access, value);
        }

        // Native mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target.Write32(physicalAddress, value, access);
            return BusFault.Success(access, page.DeviceId, page.RegionTag);
        }

        return DecomposeTryWrite32(access, value);
    }

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
            pageTable[startPage + i] = new PageEntry(
                deviceId,
                regionTag,
                perms,
                caps,
                target,
                pagePhysBase);
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

    /// <summary>
    /// Checks if an access of the given width crosses a page boundary.
    /// </summary>
    /// <param name="address">The starting address.</param>
    /// <param name="bytes">The number of bytes in the access.</param>
    /// <returns><see langword="true"/> if the access crosses a page boundary; otherwise, <see langword="false"/>.</returns>
    private static bool CrossesPageBoundary(Addr address, int bytes)
    {
        return ((address & DefaultPageMask) + (uint)(bytes - 1)) > DefaultPageMask;
    }

    /// <summary>
    /// Decomposes a 16-bit read into two 8-bit reads.
    /// </summary>
    private Word DecomposeRead16(in BusAccess access)
    {
        var access0 = access with { WidthBits = 8 };
        byte low = Read8(access0);

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        byte high = Read8(access1);

        return (Word)(low | (high << 8));
    }

    /// <summary>
    /// Decomposes a 16-bit write into two 8-bit writes.
    /// </summary>
    private void DecomposeWrite16(in BusAccess access, Word value)
    {
        var access0 = access with { WidthBits = 8 };
        Write8(access0, (byte)value);

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        Write8(access1, (byte)(value >> 8));
    }

    /// <summary>
    /// Decomposes a 32-bit read into four 8-bit reads.
    /// </summary>
    private DWord DecomposeRead32(in BusAccess access)
    {
        var access0 = access with { WidthBits = 8 };
        byte b0 = Read8(access0);

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        byte b1 = Read8(access1);

        var access2 = access with { Address = access.Address + 2, WidthBits = 8 };
        byte b2 = Read8(access2);

        var access3 = access with { Address = access.Address + 3, WidthBits = 8 };
        byte b3 = Read8(access3);

        return (DWord)(b0 | (b1 << 8) | (b2 << 16) | (b3 << 24));
    }

    /// <summary>
    /// Decomposes a 32-bit write into four 8-bit writes.
    /// </summary>
    private void DecomposeWrite32(in BusAccess access, DWord value)
    {
        var access0 = access with { WidthBits = 8 };
        Write8(access0, (byte)value);

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        Write8(access1, (byte)(value >> 8));

        var access2 = access with { Address = access.Address + 2, WidthBits = 8 };
        Write8(access2, (byte)(value >> 16));

        var access3 = access with { Address = access.Address + 3, WidthBits = 8 };
        Write8(access3, (byte)(value >> 24));
    }

    /// <summary>
    /// Decomposes a 16-bit try-read into two 8-bit try-reads.
    /// </summary>
    private BusResult<Word> DecomposeTryRead16(in BusAccess access)
    {
        var access0 = access with { WidthBits = 8 };
        var result0 = TryRead8(access0);
        if (result0.Failed)
        {
            return BusResult<Word>.FromFault(result0.Fault);
        }

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        var result1 = TryRead8(access1);
        if (result1.Failed)
        {
            return BusResult<Word>.FromFault(result1.Fault, cycles: result0.Cycles);
        }

        Word value = (Word)(result0.Value | (result1.Value << 8));
        return BusResult<Word>.Success(value, cycles: result0.Cycles + result1.Cycles);
    }

    /// <summary>
    /// Decomposes a 16-bit try-write into two 8-bit try-writes.
    /// </summary>
    private BusFault DecomposeTryWrite16(in BusAccess access, Word value)
    {
        var access0 = access with { WidthBits = 8 };
        var fault0 = TryWrite8(access0, (byte)value);
        if (fault0.IsFault)
        {
            return fault0;
        }

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        return TryWrite8(access1, (byte)(value >> 8));
    }

    /// <summary>
    /// Decomposes a 32-bit try-read into four 8-bit try-reads.
    /// </summary>
    private BusResult<DWord> DecomposeTryRead32(in BusAccess access)
    {
        var access0 = access with { WidthBits = 8 };
        var result0 = TryRead8(access0);
        if (result0.Failed)
        {
            return BusResult<DWord>.FromFault(result0.Fault);
        }

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        var result1 = TryRead8(access1);
        if (result1.Failed)
        {
            return BusResult<DWord>.FromFault(result1.Fault, cycles: result0.Cycles);
        }

        var access2 = access with { Address = access.Address + 2, WidthBits = 8 };
        var result2 = TryRead8(access2);
        if (result2.Failed)
        {
            return BusResult<DWord>.FromFault(result2.Fault, cycles: result0.Cycles + result1.Cycles);
        }

        var access3 = access with { Address = access.Address + 3, WidthBits = 8 };
        var result3 = TryRead8(access3);
        if (result3.Failed)
        {
            return BusResult<DWord>.FromFault(result3.Fault, cycles: result0.Cycles + result1.Cycles + result2.Cycles);
        }

        DWord value = (DWord)(result0.Value | (result1.Value << 8) | (result2.Value << 16) | (result3.Value << 24));
        return BusResult<DWord>.Success(value, cycles: result0.Cycles + result1.Cycles + result2.Cycles + result3.Cycles);
    }

    /// <summary>
    /// Decomposes a 32-bit try-write into four 8-bit try-writes.
    /// </summary>
    private BusFault DecomposeTryWrite32(in BusAccess access, DWord value)
    {
        var access0 = access with { WidthBits = 8 };
        var fault0 = TryWrite8(access0, (byte)value);
        if (fault0.IsFault)
        {
            return fault0;
        }

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        var fault1 = TryWrite8(access1, (byte)(value >> 8));
        if (fault1.IsFault)
        {
            return fault1;
        }

        var access2 = access with { Address = access.Address + 2, WidthBits = 8 };
        var fault2 = TryWrite8(access2, (byte)(value >> 16));
        if (fault2.IsFault)
        {
            return fault2;
        }

        var access3 = access with { Address = access.Address + 3, WidthBits = 8 };
        return TryWrite8(access3, (byte)(value >> 24));
    }
}