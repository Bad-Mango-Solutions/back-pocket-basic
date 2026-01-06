// <copyright file="BasicMemory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Memory;

using System.Runtime.CompilerServices;

using Core;
using Core.Interfaces;

/// <summary>
/// Simple memory implementation for emulated systems.
/// </summary>
/// <remarks>
/// <para>
/// Provides a basic 64KB memory space without special handling for I/O or ROM regions.
/// This implementation is suitable for testing and simple emulation scenarios.
/// Implements AsMemory() and AsReadOnlyMemory() with explicit aggressive inlining for performance.
/// </para>
/// <para>
/// <b>Deprecation Notice:</b> This class is being phased out in favor of the bus-oriented
/// architecture using <c>MainBus</c>, <c>RamTarget</c>, and <c>MemoryBusAdapter</c>.
/// New code should create memory regions through machine profiles and the <c>MachineFactory</c>.
/// </para>
/// </remarks>
[Obsolete("Use MachineFactory with a regions-based profile, or construct a MainBus with RamTarget " +
          "and wrap it in MemoryBusAdapter for new code. BasicMemory is maintained for backward compatibility.")]
#pragma warning disable CS0618 // Type or member is obsolete - implementing deprecated interface
public class BasicMemory : IMemory
#pragma warning restore CS0618 // Type or member is obsolete
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