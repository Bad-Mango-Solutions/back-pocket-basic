// <copyright file="MainBus.WideAccess.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

/// <summary>
/// Wide access (16/32-bit) operations and decomposition helpers for the main memory bus.
/// </summary>
public sealed partial class MainBus
{
    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // Decomposed mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == BusAccessMode.Decomposed)
        {
            return DecomposeRead16(access);
        }

        // Atomic mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            return page.Target!.Read16(physicalAddress, access);
        }

        return DecomposeRead16(access);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // Decomposed mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == BusAccessMode.Decomposed)
        {
            DecomposeWrite16(access, value);
            return;
        }

        // Atomic mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target!.Write16(physicalAddress, value, access);
            return;
        }

        DecomposeWrite16(access, value);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // Decomposed mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == BusAccessMode.Decomposed)
        {
            return DecomposeRead32(access);
        }

        // Atomic mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            return page.Target!.Read32(physicalAddress, access);
        }

        return DecomposeRead32(access);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        // Decomposed mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == BusAccessMode.Decomposed)
        {
            DecomposeWrite32(access, value);
            return;
        }

        // Atomic mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target!.Write32(physicalAddress, value, access);
            return;
        }

        DecomposeWrite32(access, value);
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

        // Decomposed mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == BusAccessMode.Decomposed)
        {
            return DecomposeTryRead16(access);
        }

        // Atomic mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            Word value = page.Target.Read16(physicalAddress, access);
            return BusResult<Word>.Success(value, access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        return DecomposeTryRead16(access);
    }

    /// <inheritdoc />
    public BusResult TryWrite16(in BusAccess access, Word value)
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
            return BusResult.FromFault(BusFault.Unmapped(access));
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return BusResult.FromFault(BusFault.Unmapped(access));
        }

        // Check write permission
        if (!page.CanWrite)
        {
            return BusResult.FromFault(BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag));
        }

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target.Write16(physicalAddress, value, access);
            return BusResult.Success(access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        // Decomposed mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == BusAccessMode.Decomposed)
        {
            return DecomposeTryWrite16(access, value);
        }

        // Atomic mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target.Write16(physicalAddress, value, access);
            return BusResult.Success(access, page.DeviceId, page.RegionTag, cycles: 1);
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

        // Decomposed mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == BusAccessMode.Decomposed)
        {
            return DecomposeTryRead32(access);
        }

        // Atomic mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            DWord value = page.Target.Read32(physicalAddress, access);
            return BusResult<DWord>.Success(value, access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        return DecomposeTryRead32(access);
    }

    /// <inheritdoc />
    public BusResult TryWrite32(in BusAccess access, DWord value)
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
            return BusResult.FromFault(BusFault.Unmapped(access));
        }

        ref readonly var page = ref pageTable[pageIndex];

        // Check for unmapped page
        if (page.Target is null)
        {
            return BusResult.FromFault(BusFault.Unmapped(access));
        }

        // Check write permission
        if (!page.CanWrite)
        {
            return BusResult.FromFault(BusFault.PermissionDenied(access, page.DeviceId, page.RegionTag));
        }

        // Atomic request + target supports it
        if (access.IsAtomicRequested && page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target.Write32(physicalAddress, value, access);
            return BusResult.Success(access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        // Decomposed mode default: decompose (Apple II expects byte-visible cycles)
        if (access.Mode == BusAccessMode.Decomposed)
        {
            return DecomposeTryWrite32(access, value);
        }

        // Atomic mode: use wide if available
        if (page.SupportsWide)
        {
            Addr physicalAddress = page.PhysicalBase + (access.Address & PageMask);
            page.Target.Write32(physicalAddress, value, access);
            return BusResult.Success(access, page.DeviceId, page.RegionTag, cycles: 1);
        }

        return DecomposeTryWrite32(access, value);
    }

    /// <summary>
    /// Checks if an access of the given width crosses a page boundary.
    /// </summary>
    /// <param name="address">The starting address.</param>
    /// <param name="bytes">The number of bytes in the access.</param>
    /// <returns><see langword="true"/> if the access crosses a page boundary; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    private BusResult DecomposeTryWrite16(in BusAccess access, Word value)
    {
        var access0 = access with { WidthBits = 8 };
        var result0 = TryWrite8(access0, (byte)value);
        if (result0.Failed)
        {
            return result0;
        }

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        var result1 = TryWrite8(access1, (byte)(value >> 8));
        if (result1.Failed)
        {
            return BusResult.FromFault(result1.Fault, cycles: result0.Cycles);
        }

        return BusResult.Success(cycles: result0.Cycles + result1.Cycles);
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
    private BusResult DecomposeTryWrite32(in BusAccess access, DWord value)
    {
        var access0 = access with { WidthBits = 8 };
        var result0 = TryWrite8(access0, (byte)value);
        if (result0.Failed)
        {
            return result0;
        }

        var access1 = access with { Address = access.Address + 1, WidthBits = 8 };
        var result1 = TryWrite8(access1, (byte)(value >> 8));
        if (result1.Failed)
        {
            return BusResult.FromFault(result1.Fault, cycles: result0.Cycles);
        }

        var access2 = access with { Address = access.Address + 2, WidthBits = 8 };
        var result2 = TryWrite8(access2, (byte)(value >> 16));
        if (result2.Failed)
        {
            return BusResult.FromFault(result2.Fault, cycles: result0.Cycles + result1.Cycles);
        }

        var access3 = access with { Address = access.Address + 3, WidthBits = 8 };
        var result3 = TryWrite8(access3, (byte)(value >> 24));
        if (result3.Failed)
        {
            return BusResult.FromFault(result3.Fault, cycles: result0.Cycles + result1.Cycles + result2.Cycles);
        }

        return BusResult.Success(cycles: result0.Cycles + result1.Cycles + result2.Cycles + result3.Cycles);
    }
}