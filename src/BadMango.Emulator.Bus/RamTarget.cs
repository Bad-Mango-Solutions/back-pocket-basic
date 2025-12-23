// <copyright file="RamTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// A bus target implementation for RAM (Random Access Memory).
/// </summary>
/// <remarks>
/// <para>
/// This implementation provides read-write memory with support for Peek/Poke operations
/// and atomic wide access. RAM is the most permissive memory type with no side effects.
/// </para>
/// <para>
/// The address parameter passed to read/write methods is the physical address within
/// the RAM's address space, not the CPU's address space. The bus is responsible for
/// translating CPU addresses to physical addresses via the page table.
/// </para>
/// </remarks>
public sealed class RamTarget : IBusTarget
{
    private readonly byte[] data;

    /// <summary>
    /// Initializes a new instance of the <see cref="RamTarget"/> class with the specified size.
    /// </summary>
    /// <param name="size">The size of the RAM in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when size is less than or equal to zero.</exception>
    public RamTarget(int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        data = new byte[size];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RamTarget"/> class with initial data.
    /// </summary>
    /// <param name="initialData">The initial data to populate the RAM with.</param>
    /// <exception cref="ArgumentNullException">Thrown when initialData is null.</exception>
    public RamTarget(byte[] initialData)
    {
        ArgumentNullException.ThrowIfNull(initialData);
        data = new byte[initialData.Length];
        Array.Copy(initialData, data, initialData.Length);
    }

    /// <inheritdoc />
    /// <remarks>
    /// RAM supports Peek, Poke, and wide atomic access, with no side effects.
    /// </remarks>
    public TargetCaps Capabilities =>
        TargetCaps.SupportsPeek | TargetCaps.SupportsPoke | TargetCaps.SupportsWide;

    /// <summary>
    /// Gets the size of the RAM in bytes.
    /// </summary>
    public int Size => data.Length;

    /// <inheritdoc />
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        return data[physicalAddress];
    }

    /// <inheritdoc />
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        data[physicalAddress] = value;
    }

    /// <summary>
    /// Reads a 16-bit word from the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to read from.</param>
    /// <param name="access">The access context.</param>
    /// <returns>The 16-bit value at the address (little-endian).</returns>
    public Word Read16(Addr physicalAddress, in BusAccess access)
    {
        return (Word)(data[physicalAddress] | (data[physicalAddress + 1] << 8));
    }

    /// <summary>
    /// Writes a 16-bit word to the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to write to.</param>
    /// <param name="value">The 16-bit value to write (little-endian).</param>
    /// <param name="access">The access context.</param>
    public void Write16(Addr physicalAddress, Word value, in BusAccess access)
    {
        data[physicalAddress] = (byte)value;
        data[physicalAddress + 1] = (byte)(value >> 8);
    }

    /// <summary>
    /// Reads a 32-bit double word from the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to read from.</param>
    /// <param name="access">The access context.</param>
    /// <returns>The 32-bit value at the address (little-endian).</returns>
    public DWord Read32(Addr physicalAddress, in BusAccess access)
    {
        return (DWord)(
            data[physicalAddress] |
            (data[physicalAddress + 1] << 8) |
            (data[physicalAddress + 2] << 16) |
            (data[physicalAddress + 3] << 24));
    }

    /// <summary>
    /// Writes a 32-bit double word to the specified physical address atomically.
    /// </summary>
    /// <param name="physicalAddress">The physical address to write to.</param>
    /// <param name="value">The 32-bit value to write (little-endian).</param>
    /// <param name="access">The access context.</param>
    public void Write32(Addr physicalAddress, DWord value, in BusAccess access)
    {
        data[physicalAddress] = (byte)value;
        data[physicalAddress + 1] = (byte)(value >> 8);
        data[physicalAddress + 2] = (byte)(value >> 16);
        data[physicalAddress + 3] = (byte)(value >> 24);
    }

    /// <summary>
    /// Fills the entire RAM with the specified value.
    /// </summary>
    /// <param name="value">The byte value to fill with.</param>
    public void Fill(byte value)
    {
        Array.Fill(data, value);
    }

    /// <summary>
    /// Clears the entire RAM to zero.
    /// </summary>
    public void Clear()
    {
        Array.Clear(data);
    }

    /// <summary>
    /// Gets a span over the RAM data for bulk operations.
    /// </summary>
    /// <returns>A span containing the RAM data.</returns>
    public Span<byte> AsSpan() => data.AsSpan();

    /// <summary>
    /// Gets a read-only span over the RAM data.
    /// </summary>
    /// <returns>A read-only span containing the RAM data.</returns>
    public ReadOnlySpan<byte> AsReadOnlySpan() => data.AsSpan();
}