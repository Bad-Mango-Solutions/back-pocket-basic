// <copyright file="AddressingModes.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Core;

/// <summary>
/// Provides reusable addressing mode implementations for 6502-family CPUs.
/// </summary>
/// <remarks>
/// This class contains static methods that implement common addressing modes
/// shared across CPU variants (65C02, 65816, 65832). Each method is aggressively
/// inlined for optimal performance in hot-path execution.
/// </remarks>
public static class AddressingModes
{
    /// <summary>
    /// Reads a byte using Immediate addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    /// <returns>The byte value at the current PC address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadImmediate(IMemory memory, ref ushort pc, ref ulong cycles)
    {
        byte value = memory.Read(pc++);
        cycles++;
        return value;
    }

    /// <summary>
    /// Reads a byte using Zero Page addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    /// <returns>The byte value at the zero page address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadZeroPage(IMemory memory, ref ushort pc, ref ulong cycles)
    {
        byte address = memory.Read(pc++);
        cycles += 2;
        return memory.Read(address);
    }

    /// <summary>
    /// Reads a byte using Zero Page,X addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="x">The X index register value.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    /// <returns>The byte value at the indexed zero page address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadZeroPageX(IMemory memory, ref ushort pc, byte x, ref ulong cycles)
    {
        byte address = (byte)(memory.Read(pc++) + x);
        cycles += 3;
        return memory.Read(address);
    }

    /// <summary>
    /// Reads a byte using Zero Page,Y addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="y">The Y index register value.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    /// <returns>The byte value at the indexed zero page address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadZeroPageY(IMemory memory, ref ushort pc, byte y, ref ulong cycles)
    {
        byte address = (byte)(memory.Read(pc++) + y);
        cycles += 3;
        return memory.Read(address);
    }

    /// <summary>
    /// Reads a byte using Absolute addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    /// <returns>The byte value at the absolute address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadAbsolute(IMemory memory, ref ushort pc, ref ulong cycles)
    {
        ushort address = ReadWord(memory, ref pc, ref cycles);
        cycles++;
        return memory.Read(address);
    }

    /// <summary>
    /// Reads a byte using Absolute,X addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="x">The X index register value.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    /// <returns>The byte value at the indexed absolute address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadAbsoluteX(IMemory memory, ref ushort pc, byte x, ref ulong cycles)
    {
        ushort address = ReadWord(memory, ref pc, ref cycles);
        ushort effectiveAddress = (ushort)(address + x);
        cycles++;
        if ((address & 0xFF00) != (effectiveAddress & 0xFF00))
        {
            cycles++; // Page boundary crossed
        }

        return memory.Read(effectiveAddress);
    }

    /// <summary>
    /// Reads a byte using Absolute,Y addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="y">The Y index register value.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    /// <returns>The byte value at the indexed absolute address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadAbsoluteY(IMemory memory, ref ushort pc, byte y, ref ulong cycles)
    {
        ushort address = ReadWord(memory, ref pc, ref cycles);
        ushort effectiveAddress = (ushort)(address + y);
        cycles++;
        if ((address & 0xFF00) != (effectiveAddress & 0xFF00))
        {
            cycles++; // Page boundary crossed
        }

        return memory.Read(effectiveAddress);
    }

    /// <summary>
    /// Reads a byte using Indexed Indirect (Indirect,X) addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="x">The X index register value.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    /// <returns>The byte value at the indirectly addressed location.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadIndirectX(IMemory memory, ref ushort pc, byte x, ref ulong cycles)
    {
        byte zpAddress = (byte)(memory.Read(pc++) + x);
        cycles += 2; // Opcode fetch + index addition
        ushort address = memory.ReadWord(zpAddress);
        cycles += 2; // Read word from zero page
        byte value = memory.Read(address);
        cycles++; // Final read
        return value;
    }

    /// <summary>
    /// Reads a byte using Indirect Indexed (Indirect),Y addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="y">The Y index register value.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    /// <returns>The byte value at the indirectly indexed location.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadIndirectY(IMemory memory, ref ushort pc, byte y, ref ulong cycles)
    {
        byte zpAddress = memory.Read(pc++);
        cycles++; // Fetch zero page address
        ushort address = memory.ReadWord(zpAddress);
        cycles += 2; // Read word from zero page
        ushort effectiveAddress = (ushort)(address + y);
        cycles++; // Base cycle for final read
        if ((address & 0xFF00) != (effectiveAddress & 0xFF00))
        {
            cycles++; // Page boundary crossed
        }

        return memory.Read(effectiveAddress);
    }

    /// <summary>
    /// Writes a byte using Zero Page addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="value">The byte value to write.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteZeroPage(IMemory memory, ref ushort pc, byte value, ref ulong cycles)
    {
        byte address = memory.Read(pc++);
        memory.Write(address, value);
        cycles += 2;
    }

    /// <summary>
    /// Writes a byte using Zero Page,X addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="x">The X index register value.</param>
    /// <param name="value">The byte value to write.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteZeroPageX(IMemory memory, ref ushort pc, byte x, byte value, ref ulong cycles)
    {
        byte address = (byte)(memory.Read(pc++) + x);
        memory.Write(address, value);
        cycles += 3;
    }

    /// <summary>
    /// Writes a byte using Zero Page,Y addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="y">The Y index register value.</param>
    /// <param name="value">The byte value to write.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteZeroPageY(IMemory memory, ref ushort pc, byte y, byte value, ref ulong cycles)
    {
        byte address = (byte)(memory.Read(pc++) + y);
        memory.Write(address, value);
        cycles += 3;
    }

    /// <summary>
    /// Writes a byte using Absolute addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="value">The byte value to write.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAbsolute(IMemory memory, ref ushort pc, byte value, ref ulong cycles)
    {
        ushort address = ReadWord(memory, ref pc, ref cycles);
        memory.Write(address, value);
        cycles += 2;
    }

    /// <summary>
    /// Writes a byte using Absolute,X addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="x">The X index register value.</param>
    /// <param name="value">The byte value to write.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAbsoluteX(IMemory memory, ref ushort pc, byte x, byte value, ref ulong cycles)
    {
        ushort address = ReadWord(memory, ref pc, ref cycles);
        ushort effectiveAddress = (ushort)(address + x);
        memory.Write(effectiveAddress, value);
        cycles += 2; // Index addition + write
    }

    /// <summary>
    /// Writes a byte using Absolute,Y addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="y">The Y index register value.</param>
    /// <param name="value">The byte value to write.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteAbsoluteY(IMemory memory, ref ushort pc, byte y, byte value, ref ulong cycles)
    {
        ushort address = ReadWord(memory, ref pc, ref cycles);
        ushort effectiveAddress = (ushort)(address + y);
        memory.Write(effectiveAddress, value);
        cycles += 2; // Index addition + write
    }

    /// <summary>
    /// Writes a byte using Indexed Indirect (Indirect,X) addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="x">The X index register value.</param>
    /// <param name="value">The byte value to write.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteIndirectX(IMemory memory, ref ushort pc, byte x, byte value, ref ulong cycles)
    {
        byte zpAddress = (byte)(memory.Read(pc++) + x);
        cycles += 2; // Fetch + index addition
        ushort address = memory.ReadWord(zpAddress);
        cycles += 2; // Read word from zero page
        memory.Write(address, value);
        cycles++; // Final write
    }

    /// <summary>
    /// Writes a byte using Indirect Indexed (Indirect),Y addressing mode.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented).</param>
    /// <param name="y">The Y index register value.</param>
    /// <param name="value">The byte value to write.</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteIndirectY(IMemory memory, ref ushort pc, byte y, byte value, ref ulong cycles)
    {
        byte zpAddress = memory.Read(pc++);
        cycles++; // Fetch zero page address
        ushort address = memory.ReadWord(zpAddress);
        cycles += 2; // Read word from zero page
        ushort effectiveAddress = (ushort)(address + y);
        cycles++; // Index addition
        memory.Write(effectiveAddress, value);
        cycles++; // Final write
    }

    /// <summary>
    /// Reads a 16-bit word from memory (little-endian).
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="pc">Reference to the program counter (will be incremented by 2).</param>
    /// <param name="cycles">Reference to the cycle counter (will be incremented by 2).</param>
    /// <returns>The 16-bit word value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort ReadWord(IMemory memory, ref ushort pc, ref ulong cycles)
    {
        ushort value = memory.ReadWord(pc);
        pc += 2;
        cycles += 2;
        return value;
    }
}