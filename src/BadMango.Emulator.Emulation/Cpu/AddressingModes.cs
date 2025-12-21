// <copyright file="AddressingModeNew.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using BadMango.Emulator.Core;

/// <summary>
/// Delegate representing an addressing mode that computes an effective address.
/// </summary>
/// <typeparam name="TState">The CPU state type.</typeparam>
/// <param name="memory">The memory interface.</param>
/// <param name="state">Reference to the CPU state.</param>
/// <returns>The effective address computed by the addressing mode.</returns>
public delegate ushort AddressingMode<TState>(IMemory memory, ref TState state)
    where TState : struct;

/// <summary>
/// Provides addressing mode implementations for 6502-family CPUs.
/// </summary>
/// <remarks>
/// Each addressing mode is a function that computes an effective address given memory and CPU state.
/// This allows clean composition with instruction handlers.
/// </remarks>
public static class AddressingModes
{
    /// <summary>
    /// Immediate addressing - returns the address of the immediate operand (PC).
    /// </summary>
    public static ushort Immediate(IMemory memory, ref Cpu65C02State state)
    {
        ushort address = state.PC;
        state.PC++;
        // Immediate mode: no extra cycles beyond the read that will happen
        return address;
    }

    /// <summary>
    /// Zero Page addressing - reads zero page address from PC.
    /// </summary>
    public static ushort ZeroPage(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = memory.Read(state.PC++);
        state.Cycles++; // 1 cycle to fetch the ZP address
        // The instruction will add 1 more cycle for the actual read
        return zpAddr;
    }

    /// <summary>
    /// Zero Page,X addressing - reads zero page address and adds X register.
    /// </summary>
    public static ushort ZeroPageX(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = (byte)(memory.Read(state.PC++) + state.X);
        state.Cycles += 2; // 1 cycle to fetch ZP address, 1 cycle for indexing
        // The instruction will add 1 more cycle for the actual read
        return zpAddr;
    }

    /// <summary>
    /// Zero Page,Y addressing - reads zero page address and adds Y register.
    /// </summary>
    public static ushort ZeroPageY(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = (byte)(memory.Read(state.PC++) + state.Y);
        state.Cycles += 2; // 1 cycle to fetch ZP address, 1 cycle for indexing
        // The instruction will add 1 more cycle for the actual read
        return zpAddr;
    }

    /// <summary>
    /// Absolute addressing - reads 16-bit address from PC.
    /// </summary>
    public static ushort Absolute(IMemory memory, ref Cpu65C02State state)
    {
        ushort address = memory.ReadWord(state.PC);
        state.PC += 2;
        state.Cycles += 2; // 2 cycles to fetch the 16-bit address
        // The instruction will add 1 more cycle for the actual read
        return address;
    }

    /// <summary>
    /// Absolute,X addressing - reads 16-bit address and adds X register.
    /// </summary>
    public static ushort AbsoluteX(IMemory memory, ref Cpu65C02State state)
    {
        ushort baseAddr = memory.ReadWord(state.PC);
        state.PC += 2;
        ushort effectiveAddr = (ushort)(baseAddr + state.X);
        state.Cycles += 2; // 2 cycles to fetch the 16-bit address
        
        // Add extra cycle if page boundary crossed
        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            state.Cycles++;
        }

        // The instruction will add 1 more cycle for the actual read
        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,Y addressing - reads 16-bit address and adds Y register.
    /// </summary>
    public static ushort AbsoluteY(IMemory memory, ref Cpu65C02State state)
    {
        ushort baseAddr = memory.ReadWord(state.PC);
        state.PC += 2;
        ushort effectiveAddr = (ushort)(baseAddr + state.Y);
        state.Cycles += 2; // 2 cycles to fetch the 16-bit address
        
        // Add extra cycle if page boundary crossed
        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            state.Cycles++;
        }

        // The instruction will add 1 more cycle for the actual read
        return effectiveAddr;
    }

    /// <summary>
    /// Indexed Indirect (Indirect,X) addressing - uses X-indexed zero page pointer.
    /// </summary>
    public static ushort IndirectX(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = (byte)(memory.Read(state.PC++) + state.X);
        ushort address = memory.ReadWord(zpAddr);
        state.Cycles += 4; // 1 (fetch ZP), 1 (index), 2 (read pointer from ZP)
        // The instruction will add 1 more cycle for the actual read
        return address;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing - uses zero page pointer indexed by Y.
    /// </summary>
    public static ushort IndirectY(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = memory.Read(state.PC++);
        ushort baseAddr = memory.ReadWord(zpAddr);
        ushort effectiveAddr = (ushort)(baseAddr + state.Y);
        state.Cycles += 3; // 1 (fetch ZP), 2 (read pointer from ZP)
        
        // Add extra cycle if page boundary crossed
        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            state.Cycles++;
        }

        // The instruction will add 1 more cycle for the actual read
        return effectiveAddr;
    }

    // Write-specific addressing modes that always take the maximum cycles

    /// <summary>
    /// Absolute,X addressing for write operations - always takes maximum cycles.
    /// </summary>
    public static ushort AbsoluteXWrite(IMemory memory, ref Cpu65C02State state)
    {
        ushort baseAddr = memory.ReadWord(state.PC);
        state.PC += 2;
        ushort effectiveAddr = (ushort)(baseAddr + state.X);
        state.Cycles += 3; // 2 cycles to fetch address + 1 extra for write operations
        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    public static ushort AbsoluteYWrite(IMemory memory, ref Cpu65C02State state)
    {
        ushort baseAddr = memory.ReadWord(state.PC);
        state.PC += 2;
        ushort effectiveAddr = (ushort)(baseAddr + state.Y);
        state.Cycles += 3; // 2 cycles to fetch address + 1 extra for write operations
        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    public static ushort IndirectYWrite(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = memory.Read(state.PC++);
        ushort baseAddr = memory.ReadWord(zpAddr);
        ushort effectiveAddr = (ushort)(baseAddr + state.Y);
        state.Cycles += 4; // 1 (fetch ZP), 2 (read pointer), 1 extra for write
        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }
}
