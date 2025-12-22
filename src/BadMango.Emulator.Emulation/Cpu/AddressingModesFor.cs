// <copyright file="AddressingModesFor.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Numerics;

using Core;

/// <summary>
/// Provides generic addressing mode implementations for 6502-family CPUs.
/// </summary>
/// <typeparam name="TRegisters">The CPU registers type.</typeparam>
/// <typeparam name="TAccumulator">The accumulator register type.</typeparam>
/// <typeparam name="TIndex">The index register type (X, Y).</typeparam>
/// <typeparam name="TStack">The stack pointer type.</typeparam>
/// <typeparam name="TProgram">The program counter type.</typeparam>
/// <remarks>
/// <para>
/// This generic class provides addressing mode implementations that work with any CPU type
/// in the 6502 family (65C02, 65816, 65832, etc.). Type parameters are specified at the class level
/// to reduce verbosity when calling methods.
/// </para>
/// <para>
/// Usage for 65C02:
/// <code>
/// using Cpu65C02AddressingModes = AddressingModesFor&lt;Cpu65C02Registers, byte, byte, byte, Word&gt;;
/// var handler = Instructions.LDA(Cpu65C02AddressingModes.Immediate);
/// </code>
/// </para>
/// <para>
/// Usage for 65816:
/// <code>
/// using Cpu65816AddressingModes = AddressingModesFor&lt;Cpu65816Registers, ushort, ushort, ushort, ushort&gt;;
/// var handler = Instructions.LDA(Cpu65816AddressingModes.Immediate);
/// </code>
/// </para>
/// </remarks>
public static class AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    where TRegisters : ICpuRegisters<TAccumulator, TIndex, TStack, TProgram>
    where TProgram : struct, IIncrementOperators<TProgram>, IBinaryInteger<TProgram>
    where TIndex : struct, INumber<TIndex>
{
    /// <summary>
    /// Implied addressing - used for instructions that don't access memory.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface (not used for implied addressing).</param>
    /// <param name="state">Reference to the CPU state (not modified for implied addressing).</param>
    /// <returns>Returns zero as a placeholder address since no memory access is needed.</returns>
    /// <remarks>
    /// Returns zero as a placeholder address. Instructions using this mode
    /// typically operate only on registers or the stack (e.g., NOP, CLC, SEI).
    /// </remarks>
    public static Addr Implied<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        // No addressing needed, no PC increment, no cycles
        return 0;
    }

    /// <summary>
    /// Immediate addressing - returns the address of the immediate operand (PC).
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface (not used for immediate addressing).</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The address of the immediate operand (current PC value).</returns>
    public static Addr Immediate<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr address = uint.CreateTruncating(state.PC);
        state.PC++;

        // Immediate mode: no extra cycles beyond the read that will happen
        return address;
    }

    /// <summary>
    /// Zero Page addressing - reads zero page address from PC.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The zero page address (0x00-0xFF).</returns>
    public static Addr ZeroPage<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        byte zpAddr = memory.Read(pc);
        state.PC++;
        state.Cycles++; // 1 cycle to fetch the ZP address

        // The instruction will add 1 more cycle for the actual read
        return zpAddr;
    }

    /// <summary>
    /// Zero Page,X addressing - reads zero page address and adds X register.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective zero page address with X offset (wraps within zero page).</returns>
    public static Addr ZeroPageX<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        byte baseAddr = memory.Read(pc);
        byte xOffset = byte.CreateTruncating(state.X);
        byte zpAddr = (byte)(baseAddr + xOffset);
        state.PC++;
        state.Cycles += 2; // 1 cycle to fetch ZP address, 1 cycle for indexing

        // The instruction will add 1 more cycle for the actual read
        return zpAddr;
    }

    /// <summary>
    /// Zero Page,Y addressing - reads zero page address and adds Y register.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective zero page address with Y offset (wraps within zero page).</returns>
    public static Addr ZeroPageY<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        byte baseAddr = memory.Read(pc);
        byte yOffset = byte.CreateTruncating(state.Y);
        byte zpAddr = (byte)(baseAddr + yOffset);
        state.PC++;
        state.Cycles += 2; // 1 cycle to fetch ZP address, 1 cycle for indexing

        // The instruction will add 1 more cycle for the actual read
        return zpAddr;
    }

    /// <summary>
    /// Absolute addressing - reads 16-bit address from PC.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The 16-bit absolute address.</returns>
    public static Addr Absolute<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        Addr address = memory.ReadWord(pc);
        state.PC++;
        state.PC++;
        state.Cycles += 2; // 2 cycles to fetch the 16-bit address

        // The instruction will add 1 more cycle for the actual read
        return address;
    }

    /// <summary>
    /// Absolute,X addressing - reads 16-bit address and adds X register.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with X offset.</returns>
    public static Addr AbsoluteX<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        Addr baseAddr = memory.ReadWord(pc);
        Addr xOffset = uint.CreateTruncating(state.X);
        Addr effectiveAddr = baseAddr + xOffset;
        state.PC++;
        state.PC++;
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
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset.</returns>
    public static Addr AbsoluteY<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        Addr baseAddr = memory.ReadWord(pc);
        Addr yOffset = uint.CreateTruncating(state.Y);
        Addr effectiveAddr = baseAddr + yOffset;
        state.PC++;
        state.PC++;
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
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address read from the X-indexed zero page pointer.</returns>
    public static Addr IndirectX<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        byte baseAddr = memory.Read(pc);
        byte xOffset = byte.CreateTruncating(state.X);
        byte zpAddr = (byte)(baseAddr + xOffset);
        Addr address = memory.ReadWord(zpAddr);
        state.PC++;
        state.Cycles += 4; // 1 (fetch ZP), 1 (index), 2 (read pointer from ZP)

        // The instruction will add 1 more cycle for the actual read
        return address;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing - uses zero page pointer indexed by Y.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset applied to the zero page pointer.</returns>
    public static Addr IndirectY<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        byte zpAddr = memory.Read(pc);
        Addr baseAddr = memory.ReadWord(zpAddr);
        Addr yOffset = uint.CreateTruncating(state.Y);
        Addr effectiveAddr = baseAddr + yOffset;
        state.PC++;
        state.Cycles += 3; // 1 (fetch ZP), 2 (read pointer from ZP)

        // Add extra cycle if page boundary crossed
        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            state.Cycles++;
        }

        // The instruction will add 1 more cycle for the actual read
        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,X addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with X offset.</returns>
    public static Addr AbsoluteXWrite<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        Addr baseAddr = memory.ReadWord(pc);
        Addr xOffset = uint.CreateTruncating(state.X);
        Addr effectiveAddr = baseAddr + xOffset;
        state.PC++;
        state.PC++;
        state.Cycles += 3; // 2 cycles to fetch address + 1 extra for write operations

        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset.</returns>
    public static Addr AbsoluteYWrite<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        Addr baseAddr = memory.ReadWord(pc);
        Addr yOffset = uint.CreateTruncating(state.Y);
        Addr effectiveAddr = baseAddr + yOffset;
        state.PC++;
        state.PC++;
        state.Cycles += 3; // 2 cycles to fetch address + 1 extra for write operations

        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset applied to the zero page pointer.</returns>
    public static Addr IndirectYWrite<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        byte zpAddr = memory.Read(pc);
        Addr baseAddr = memory.ReadWord(zpAddr);
        Addr yOffset = uint.CreateTruncating(state.Y);
        Addr effectiveAddr = baseAddr + yOffset;
        state.PC++;
        state.Cycles += 4; // 1 (fetch ZP), 2 (read pointer), 1 extra for write

        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Accumulator addressing - used for shift/rotate operations on the accumulator.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface (not used for accumulator addressing).</param>
    /// <param name="state">Reference to the CPU state (not modified for accumulator addressing).</param>
    /// <returns>Returns zero as a placeholder address since the operation is on the accumulator.</returns>
    /// <remarks>
    /// Returns zero as a placeholder address. Instructions using this mode operate directly
    /// on the accumulator register (e.g., ASL A, LSR A, ROL A, ROR A).
    /// </remarks>
    public static Addr Accumulator<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        // No addressing needed, no PC increment, no cycles
        return 0;
    }

    /// <summary>
    /// Relative addressing - used for branch instructions.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The target address for the branch (PC + signed offset).</returns>
    /// <remarks>
    /// Reads a signed byte offset from PC and computes the branch target.
    /// Branch instructions must handle the conditional logic and cycle counting.
    /// </remarks>
    public static Addr Relative<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        sbyte offset = (sbyte)memory.Read(pc);
        state.PC++;
        state.Cycles++; // 1 cycle to fetch the offset
        Addr targetAddr = (Addr)(int)(pc + 1 + offset);

        // Branch instructions will add extra cycles if branch is taken
        return targetAddr;
    }

    /// <summary>
    /// Indirect addressing - reads a 16-bit address from memory location.
    /// </summary>
    /// <typeparam name="TState">The CPU state type.</typeparam>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The address read from the indirect pointer.</returns>
    /// <remarks>
    /// Used by JMP (Indirect). The 65C02 fixed the page wrap bug from the original 6502.
    /// </remarks>
    public static Addr Indirect<TState>(IMemory memory, ref TState state)
        where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    {
        Addr pc = uint.CreateTruncating(state.PC);
        Addr pointerAddr = memory.ReadWord(pc);
        state.PC++;
        state.PC++;
        Addr targetAddr = memory.ReadWord(pointerAddr);
        state.Cycles += 4; // 2 cycles to fetch pointer address, 2 cycles to read target

        // The instruction will add 1 more cycle for execution
        return targetAddr;
    }
}

