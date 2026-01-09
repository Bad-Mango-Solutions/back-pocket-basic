// <copyright file="MainBus.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using Interfaces;

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
public sealed partial class MainBus : IMemoryBus
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
    /// The base page table entries before any layers are applied.
    /// Used to restore mappings when layers are deactivated.
    /// </summary>
    private readonly PageEntry[] basePageTable;

    /// <summary>
    /// Dictionary of named layers for layer lookup.
    /// </summary>
    private readonly Dictionary<string, MappingLayer> layers = new(StringComparer.Ordinal);

    /// <summary>
    /// All layered mappings organized by layer name.
    /// </summary>
    private readonly Dictionary<string, List<LayeredMapping>> layeredMappings = new(StringComparer.Ordinal);

    /// <summary>
    /// Dictionary of swap groups by ID for O(1) lookup.
    /// </summary>
    private readonly Dictionary<uint, SwapGroup> swapGroupsById = [];

    /// <summary>
    /// Dictionary of swap group IDs by name for name-based lookup.
    /// </summary>
    private readonly Dictionary<string, uint> swapGroupIdsByName = new(StringComparer.Ordinal);

    /// <summary>
    /// Lock object for thread-safe swap group operations.
    /// </summary>
    private readonly object swapGroupLock = new();

    /// <summary>
    /// Counter for generating unique swap group IDs.
    /// </summary>
    private uint nextSwapGroupId;

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
        basePageTable = new PageEntry[pageCount];
    }

    /// <inheritdoc />
    public int PageShift => DefaultPageShift;

    /// <inheritdoc />
    public Addr PageMask => DefaultPageMask;

    /// <inheritdoc />
    public int PageCount => pageTable.Length;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // Check read permission.
        // Debug reads (AccessIntent.DebugRead) bypass this check to allow
        // inspecting memory regardless of page permissions.
        if (!page.CanRead && !access.IsDebugAccess)
        {
            return BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag);
        }

        // Check NX on instruction fetch (Atomic mode only)
        if (access.Intent == AccessIntent.InstructionFetch &&
            access.Mode == BusAccessMode.Atomic &&
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
    public BusResult TryWrite8(in BusAccess access, byte value)
    {
        int pageIndex = (int)(access.Address >> PageShift);
        if (pageIndex >= pageTable.Length)
        {
            return BusResult.FromFault(BusFault.Unmapped(access));
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return BusResult.FromFault(BusFault.Unmapped(access));
        }

        // Check write permission.
        // Debug writes (AccessIntent.DebugWrite) bypass this check because:
        // 1. They're used for test setup (patching ROM with stubs)
        // 2. The target ultimately decides if the write succeeds
        //    - RomTarget accepts debug writes if constructed with Memory<byte>
        //    - RomTarget ignores debug writes if constructed with ReadOnlyMemory<byte>
        // 3. This enables ICpu.Poke8() to work for debugging and testing scenarios
        if (!page.CanWrite && !access.IsDebugAccess)
        {
            return BusResult.FromFault(BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag));
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

            return BusResult.Success(access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        // Perform the write
        Addr physAddr = page.PhysicalBase + (access.Address & PageMask);
        page.Target.Write8(physAddr, value, access);

        return BusResult.Success(access, page.DeviceId, page.RegionTag, cycles: 1);
    }
}