// <copyright file="BasicMemory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Memory;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// Simple memory implementation for emulated systems.
/// </summary>
/// <remarks>
/// Provides a basic 64KB memory space without special handling for I/O or ROM regions.
/// This implementation is suitable for testing and simple emulation scenarios.
/// Implements AsMemory() and AsReadOnlyMemory() with explicit aggressive inlining for performance.
/// </remarks>
public class BasicMemory : IMemory
{
    private readonly byte[] memory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicMemory"/> class.
    /// </summary>
    /// <param name="size">The size of the memory in bytes. Defaults to 64KB (65536 bytes).</param>
    /// <remarks>
    /// Consider using constants from <see cref="MemorySizes"/> for common memory sizes.
    /// For example: <c>new BasicMemory(MemorySizes.Size64KB)</c> or <c>new BasicMemory(MemorySizes.Size128KB)</c>.
    /// </remarks>
    public BasicMemory(uint size = MemorySizes.Size64KB)
    {
        memory = new byte[size];
    }

    /// <inheritdoc/>
    public uint Size
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)memory.Length;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read(Addr address)
    {
        return memory[address];
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Addr address, byte value)
    {
        memory[address] = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Word ReadWord(Addr address)
    {
        return (Word)(memory[address] | (memory[address + 1] << 8));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteWord(Addr address, Word value)
    {
        memory[address] = (byte)(value & 0xFF);
        memory[address + 1] = (byte)(value >> 8);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DWord ReadDWord(Addr address)
    {
        return (DWord)(memory[address] | (memory[address + 1] << 8) | (memory[address + 2] << 16) |
                       (memory[address + 3] << 24));
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDWord(Addr address, DWord value)
    {
        memory[address] = (byte)(value & 0xFF);
        memory[address + 1] = (byte)((value >> 8) & 0xFF);
        memory[address + 2] = (byte)((value >> 16) & 0xFF);
        memory[address + 3] = (byte)((value >> 24) & 0xFF);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Array.Clear(memory, 0, memory.Length);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> AsReadOnlyMemory()
    {
        return new(memory);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> Inspect(int start, int length)
    {
        return new(memory, start, length);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<byte> AsMemory()
    {
        return new(memory);
    }
}