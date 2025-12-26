// <copyright file="MemoryBusAdapter.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core;

/// <summary>
/// Adapts an <see cref="IMemoryBus"/> to the <see cref="IMemory"/> interface.
/// </summary>
/// <remarks>
/// <para>
/// This adapter bridges the gap between the new bus architecture and existing
/// CPU code that expects an <see cref="IMemory"/> interface. It allows incremental
/// migration of CPU code to the new bus infrastructure without breaking existing
/// tests and functionality.
/// </para>
/// <para>
/// The adapter creates appropriate <see cref="BusAccess"/> contexts for each
/// memory operation, using sensible defaults for intent, mode, and flags.
/// </para>
/// <para>
/// <b>Fault handling:</b> When the underlying bus returns a fault, this adapter
/// throws an <see cref="InvalidOperationException"/> with fault details. This
/// matches the implicit contract of <see cref="IMemory"/> which doesn't have
/// explicit fault handling.
/// </para>
/// </remarks>
public sealed class MemoryBusAdapter : IMemory
{
    private readonly IMemoryBus bus;
    private readonly CpuMode mode;
    private readonly int sourceId;
    private ulong cycleCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBusAdapter"/> class.
    /// </summary>
    /// <param name="bus">The memory bus to wrap.</param>
    /// <param name="mode">The CPU mode for bus access operations. Defaults to <see cref="CpuMode.Compat"/>.</param>
    /// <param name="sourceId">The source identifier for tracing. Defaults to 0 (CPU).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bus"/> is null.</exception>
    public MemoryBusAdapter(IMemoryBus bus, CpuMode mode = CpuMode.Compat, int sourceId = 0)
    {
        ArgumentNullException.ThrowIfNull(bus);
        this.bus = bus;
        this.mode = mode;
        this.sourceId = sourceId;
    }

    /// <summary>
    /// Gets the size of the emulated memory in bytes.
    /// </summary>
    /// <remarks>
    /// The size is computed from the bus's page count and page size (4KB per page).
    /// </remarks>
    public uint Size => (uint)bus.PageCount << bus.PageShift;

    /// <summary>
    /// Gets the current cycle count accumulated by this adapter.
    /// </summary>
    public ulong CycleCount => cycleCount;

    /// <inheritdoc />
    public byte Read(Addr address)
    {
        var access = CreateAccess(address, 8, AccessIntent.DataRead);
        var result = bus.TryRead8(access);

        if (result.Failed)
        {
            ThrowForFault(result.Fault, "Read8");
        }

        cycleCount += result.Cycles;
        return result.Value;
    }

    /// <inheritdoc />
    public void Write(Addr address, byte value)
    {
        var access = CreateAccess(address, 8, AccessIntent.DataWrite);
        var fault = bus.TryWrite8(access, value);

        if (fault.IsFault)
        {
            ThrowForFault(fault, "Write8");
        }

        cycleCount++;
    }

    /// <inheritdoc />
    public Word ReadWord(Addr address)
    {
        var access = CreateAccess(address, 16, AccessIntent.DataRead);
        var result = bus.TryRead16(access);

        if (result.Failed)
        {
            ThrowForFault(result.Fault, "Read16");
        }

        cycleCount += result.Cycles;
        return result.Value;
    }

    /// <inheritdoc />
    public void WriteWord(Addr address, Word value)
    {
        var access = CreateAccess(address, 16, AccessIntent.DataWrite);
        var fault = bus.TryWrite16(access, value);

        if (fault.IsFault)
        {
            ThrowForFault(fault, "Write16");
        }

        cycleCount += 2; // Decomposed in compat mode
    }

    /// <inheritdoc />
    public DWord ReadDWord(Addr address)
    {
        var access = CreateAccess(address, 32, AccessIntent.DataRead);
        var result = bus.TryRead32(access);

        if (result.Failed)
        {
            ThrowForFault(result.Fault, "Read32");
        }

        cycleCount += result.Cycles;
        return result.Value;
    }

    /// <inheritdoc />
    public void WriteDWord(Addr address, DWord value)
    {
        var access = CreateAccess(address, 32, AccessIntent.DataWrite);
        var fault = bus.TryWrite32(access, value);

        if (fault.IsFault)
        {
            ThrowForFault(fault, "Write32");
        }

        cycleCount += 4; // Decomposed in compat mode
    }

    /// <inheritdoc />
    public void Clear()
    {
        // Clear is not directly supported by the bus interface.
        // This method iterates through all addresses and writes zeros.
        // This is intentionally slow as Clear is typically only used during testing.
        for (Addr addr = 0; addr < Size; addr++)
        {
            var access = CreateAccess(addr, 8, AccessIntent.DataWrite, AccessFlags.NoSideEffects);
            _ = bus.TryWrite8(access, 0);
        }
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> AsReadOnlyMemory()
    {
        // This operation is not supported by the bus adapter.
        // The bus abstraction doesn't expose contiguous memory views.
        throw new NotSupportedException(
            "MemoryBusAdapter does not support direct memory access. " +
            "Use Read/Write methods or access the underlying physical memory directly.");
    }

    /// <inheritdoc />
    public Memory<byte> AsMemory()
    {
        // This operation is not supported by the bus adapter.
        // The bus abstraction doesn't expose contiguous memory views.
        throw new NotSupportedException(
            "MemoryBusAdapter does not support direct memory access. " +
            "Use Read/Write methods or access the underlying physical memory directly.");
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> Inspect(int start, int length)
    {
        // Read the memory range into a new buffer using debug reads
        if (start < 0 || start >= Size)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, $"Start must be between 0 and {Size - 1}.");
        }

        if (length < 0 || start + length > Size)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Range ({start} + {length}) exceeds memory size ({Size}).");
        }

        var buffer = new byte[length];
        for (int i = 0; i < length; i++)
        {
            var access = CreateAccess((Addr)(start + i), 8, AccessIntent.DebugRead, AccessFlags.NoSideEffects);
            var result = bus.TryRead8(access);
            buffer[i] = result.Ok ? result.Value : (byte)0xFF;
        }

        return buffer.AsMemory();
    }

    /// <summary>
    /// Resets the cycle count to zero.
    /// </summary>
    public void ResetCycleCount() => cycleCount = 0;

    /// <summary>
    /// Throws an exception for a bus fault.
    /// </summary>
    private static void ThrowForFault(BusFault fault, string operation)
    {
        throw new InvalidOperationException(
            $"Bus fault during {operation} at address 0x{fault.Address:X8}: {fault.Kind} " +
            $"(Intent: {fault.Intent}, DeviceId: {fault.DeviceId}, RegionTag: {fault.RegionTag})");
    }

    /// <summary>
    /// Creates a bus access context with the specified parameters.
    /// </summary>
    private BusAccess CreateAccess(Addr address, byte widthBits, AccessIntent intent, AccessFlags flags = AccessFlags.None)
    {
        return new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: widthBits,
            Mode: mode,
            EmulationFlag: mode == CpuMode.Compat,
            Intent: intent,
            SourceId: sourceId,
            Cycle: cycleCount,
            Flags: flags);
    }
}