// <copyright file="MemoryBusAdapter.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Core.Interfaces;

using Interfaces;

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
#pragma warning disable CS0618 // Type or member is obsolete - this adapter intentionally implements IMemory for backward compatibility
public sealed class MemoryBusAdapter : IMemory
#pragma warning restore CS0618 // Type or member is obsolete
{
    private readonly IMemoryBus bus;
    private readonly BusAccessMode mode;
    private readonly int sourceId;
    private ulong cycleCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBusAdapter"/> class.
    /// </summary>
    /// <param name="bus">The memory bus to wrap.</param>
    /// <param name="mode">The bus access mode for operations. Defaults to <see cref="BusAccessMode.Decomposed"/>.</param>
    /// <param name="sourceId">The source identifier for tracing. Defaults to 0 (CPU).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bus"/> is null.</exception>
    public MemoryBusAdapter(IMemoryBus bus, BusAccessMode mode = BusAccessMode.Decomposed, int sourceId = 0)
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
        var result = bus.TryWrite8(access, value);

        if (result.Failed)
        {
            ThrowForFault(result.Fault, "Write8");
        }

        cycleCount += result.Cycles;
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
        var result = bus.TryWrite16(access, value);

        if (result.Failed)
        {
            ThrowForFault(result.Fault, "Write16");
        }

        cycleCount += result.Cycles;
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
        var result = bus.TryWrite32(access, value);

        if (result.Failed)
        {
            ThrowForFault(result.Fault, "Write32");
        }

        cycleCount += result.Cycles;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Clears all mapped memory by delegating to each target's own <see cref="IBusTarget.Clear"/>
    /// method via the bus. Each target type is responsible for its own clearing behavior,
    /// allowing for efficient implementations (e.g., <c>Array.Clear</c> for RAM targets).
    /// </para>
    /// <para>
    /// This operation should NEVER cause side effects. Read-only targets (ROM) will not
    /// be affected. This method is primarily intended for testing scenarios.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        bus.Clear();
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
            EmulationFlag: mode == BusAccessMode.Decomposed,
            Intent: intent,
            SourceId: sourceId,
            Cycle: cycleCount,
            Flags: flags);
    }
}