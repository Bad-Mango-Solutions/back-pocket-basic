// <copyright file="AddressingModes.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using Core;

/// <summary>
/// Delegate representing an addressing mode that computes an effective address.
/// </summary>
/// <typeparam name="TState">The CPU state type.</typeparam>
/// <param name="memory">The memory interface.</param>
/// <param name="state">Reference to the CPU state.</param>
/// <returns>The effective address computed by the addressing mode.</returns>
public delegate Addr AddressingMode<TState>(IMemory memory, ref TState state)
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
    /// Implied addressing - used for instructions that don't access memory.
    /// </summary>
    /// <param name="memory">The memory interface (not used for implied addressing).</param>
    /// <param name="state">Reference to the CPU state (not modified for implied addressing).</param>
    /// <returns>Returns zero as a placeholder address since no memory access is needed.</returns>
    /// <remarks>
    /// Returns zero as a placeholder address. Instructions using this mode
    /// typically operate only on registers or the stack (e.g., NOP, CLC, SEI).
    /// </remarks>
    public static Addr Implied(IMemory memory, ref Cpu65C02State state)
    {
        // No addressing needed, no PC increment, no cycles
        return 0;
    }

    /// <summary>
    /// Immediate addressing - returns the address of the immediate operand (PC).
    /// </summary>
    /// <param name="memory">The memory interface (not used for immediate addressing).</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The address of the immediate operand (current PC value).</returns>
    public static Addr Immediate(IMemory memory, ref Cpu65C02State state)
    {
        Addr address = state.PC;
        state.PC++;

        // Immediate mode: no extra cycles beyond the read that will happen
        return address;
    }

    /// <summary>
    /// Zero Page addressing - reads zero page address from PC.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The zero page address (0x00-0xFF).</returns>
    public static Addr ZeroPage(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = memory.Read(state.PC++);
        state.Cycles++; // 1 cycle to fetch the ZP address

        // The instruction will add 1 more cycle for the actual read
        return zpAddr;
    }

    /// <summary>
    /// Zero Page,X addressing - reads zero page address and adds X register.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective zero page address with X offset (wraps within zero page).</returns>
    public static Addr ZeroPageX(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = (byte)(memory.Read(state.PC++) + state.X);
        state.Cycles += 2; // 1 cycle to fetch ZP address, 1 cycle for indexing

        // The instruction will add 1 more cycle for the actual read
        return zpAddr;
    }

    /// <summary>
    /// Zero Page,Y addressing - reads zero page address and adds Y register.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective zero page address with Y offset (wraps within zero page).</returns>
    public static Addr ZeroPageY(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = (byte)(memory.Read(state.PC++) + state.Y);
        state.Cycles += 2; // 1 cycle to fetch ZP address, 1 cycle for indexing

        // The instruction will add 1 more cycle for the actual read
        return zpAddr;
    }

    /// <summary>
    /// Absolute addressing - reads 16-bit address from PC.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The 16-bit absolute address.</returns>
    public static Addr Absolute(IMemory memory, ref Cpu65C02State state)
    {
        Addr address = memory.ReadWord(state.PC);
        state.PC += 2;
        state.Cycles += 2; // 2 cycles to fetch the 16-bit address

        // The instruction will add 1 more cycle for the actual read
        return address;
    }

    /// <summary>
    /// Absolute,X addressing - reads 16-bit address and adds X register.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with X offset.</returns>
    public static Addr AbsoluteX(IMemory memory, ref Cpu65C02State state)
    {
        Addr baseAddr = memory.ReadWord(state.PC);
        state.PC += 2;
        Addr effectiveAddr = baseAddr + state.X;
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
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset.</returns>
    public static Addr AbsoluteY(IMemory memory, ref Cpu65C02State state)
    {
        Addr baseAddr = memory.ReadWord(state.PC);
        state.PC += 2;
        Addr effectiveAddr = baseAddr + state.Y;
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
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address read from the X-indexed zero page pointer.</returns>
    public static Addr IndirectX(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = (byte)(memory.Read(state.PC++) + state.X);
        Addr address = memory.ReadWord(zpAddr);
        state.Cycles += 4; // 1 (fetch ZP), 1 (index), 2 (read pointer from ZP)

        // The instruction will add 1 more cycle for the actual read
        return address;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing - uses zero page pointer indexed by Y.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset applied to the zero page pointer.</returns>
    public static Addr IndirectY(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = memory.Read(state.PC++);
        Addr baseAddr = memory.ReadWord(zpAddr);
        Addr effectiveAddr = baseAddr + state.Y;
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
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with X offset.</returns>
    public static Addr AbsoluteXWrite(IMemory memory, ref Cpu65C02State state)
    {
        Addr baseAddr = memory.ReadWord(state.PC);
        state.PC += 2;
        Addr effectiveAddr = baseAddr + state.X;
        state.Cycles += 3; // 2 cycles to fetch address + 1 extra for write operations

        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset.</returns>
    public static Addr AbsoluteYWrite(IMemory memory, ref Cpu65C02State state)
    {
        Addr baseAddr = memory.ReadWord(state.PC);
        state.PC += 2;
        Addr effectiveAddr = baseAddr + state.Y;
        state.Cycles += 3; // 2 cycles to fetch address + 1 extra for write operations

        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset applied to the zero page pointer.</returns>
    public static Addr IndirectYWrite(IMemory memory, ref Cpu65C02State state)
    {
        byte zpAddr = memory.Read(state.PC++);
        Addr baseAddr = memory.ReadWord(zpAddr);
        Addr effectiveAddr = baseAddr + state.Y;
        state.Cycles += 4; // 1 (fetch ZP), 2 (read pointer), 1 extra for write

        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Accumulator addressing - used for shift/rotate operations on the accumulator.
    /// </summary>
    /// <param name="memory">The memory interface (not used for accumulator addressing).</param>
    /// <param name="state">Reference to the CPU state (not modified for accumulator addressing).</param>
    /// <returns>Returns zero as a placeholder address since the operation is on the accumulator.</returns>
    /// <remarks>
    /// Returns zero as a placeholder address. Instructions using this mode operate directly
    /// on the accumulator register (e.g., ASL A, LSR A, ROL A, ROR A).
    /// </remarks>
    public static Addr Accumulator(IMemory memory, ref Cpu65C02State state)
    {
        // No addressing needed, no PC increment, no cycles
        return 0;
    }

    /// <summary>
    /// Relative addressing - used for branch instructions.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The target address for the branch (PC + signed offset).</returns>
    /// <remarks>
    /// Reads a signed byte offset from PC and computes the branch target.
    /// Branch instructions must handle the conditional logic and cycle counting.
    /// </remarks>
    public static Addr Relative(IMemory memory, ref Cpu65C02State state)
    {
        sbyte offset = (sbyte)memory.Read(state.PC++);
        state.Cycles++; // 1 cycle to fetch the offset
        Addr targetAddr = (Addr)(state.PC + offset);

        // Branch instructions will add extra cycles if branch is taken
        return targetAddr;
    }

    /// <summary>
    /// Indirect addressing - reads a 16-bit address from memory location.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The address read from the indirect pointer.</returns>
    /// <remarks>
    /// Used by JMP (Indirect). The 65C02 fixed the page wrap bug from the original 6502.
    /// </remarks>
    public static Addr Indirect(IMemory memory, ref Cpu65C02State state)
    {
        Addr pointerAddr = memory.ReadWord(state.PC);
        state.PC += 2;
        Addr targetAddr = memory.ReadWord(pointerAddr);
        state.Cycles += 4; // 2 cycles to fetch pointer address, 2 cycles to read target

        // The instruction will add 1 more cycle for execution
        return targetAddr;
    }
}