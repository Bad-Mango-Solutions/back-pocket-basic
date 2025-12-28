// <copyright file="AddressingModes.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;
using Core.Cpu;

/// <summary>
/// Provides addressing mode implementations for 6502-family CPUs.
/// </summary>
/// <remarks>
/// Each addressing mode is a function that computes an effective address given memory and CPU state.
/// This allows clean composition with instruction handlers.
/// Mode-aware behavior:
/// - In 65C02 mode: Standard 6502 behavior
/// - In 65816 emulation mode (E=1): 6502-compatible with stack pointer forced to page 1
/// - In 65816 native mode (E=0): Direct page relocatable, 16-bit registers possible, different cycle rules.
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
    /// Mode-agnostic: behavior is identical across all CPU modes.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr Implied(IMemory memory, ref CpuState state)
    {
        // No addressing needed, no PC increment, no cycles
        // Mode-agnostic
        if (state.IsDebuggerAttached)
        {
            state.EffectiveAddress = 0;
            state.OperandSize = 0;
            state.AddressingMode = CpuAddressingModes.Implied;
        }

        return 0;
    }

    /// <summary>
    /// Immediate addressing - returns the address of the immediate operand (PC).
    /// </summary>
    /// <param name="memory">The memory interface (not used for immediate addressing).</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The address of the immediate operand (current PC value).</returns>
    /// <remarks>
    /// Mode-aware behavior:
    /// - In emulation mode: Always fetches 8-bit immediate
    /// - In native mode with M=0: Would fetch 16-bit immediate (handled by instruction, not addressing mode)
    /// - In native mode with X=0: Would fetch 16-bit immediate for index operations.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr Immediate(IMemory memory, ref CpuState state)
    {
        Addr address = state.Registers.PC.GetDWord();
        state.Registers.PC.Advance();

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.Immediate;
        }

        // Immediate mode: no extra cycles beyond the read that will happen
        // Mode behavior: In 65816 native mode, instructions would handle 16-bit fetches themselves
        return address;
    }

    /// <summary>
    /// Zero Page addressing - reads zero-page/direct-page address from PC.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The zero-page/direct-page address.</returns>
    /// <remarks>
    /// Mode-aware behavior:
    /// - In 6502/65C02 mode: Direct page is fixed at $0000
    /// - In 65816 emulation mode: Direct page is fixed at $0000
    /// - In 65816 native mode: Direct page can be relocated via D register
    /// - Adds extra cycle if D.l != 0 (direct page not page-aligned) in native mode.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr ZeroPage(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        byte zpOffset = memory.Read(state.Registers.PC.addr++);
        addrCycles++; // 1 cycle to fetch the ZP address

        // In 65816 native mode, D register can relocate the direct page
        // In emulation mode or 6502 mode, D is always $0000
        Word directPage = state.Registers.D.GetWord();

        // Add 1 cycle if direct page low byte != 0 (not page-aligned) in native mode
        // In emulation mode, D is always 0, so no penalty
        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        state.Cycles += addrCycles;
        Addr effectiveAddr = (Addr)(directPage + zpOffset);

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.ZeroPage;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 1;
            state.Operands[0] = zpOffset;
            state.InstructionCycles += addrCycles;
        }

        // The instruction will add 1 more cycle for the actual read
        return effectiveAddr;
    }

    /// <summary>
    /// Zero Page,X addressing - reads zero-page/direct-page address and adds X register.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective zero-page/direct-page address with X offset (wraps within direct page).</returns>
    /// <remarks>
    /// Mode-aware behavior:
    /// - Wrapping behavior: Always wraps within the direct page (even in native mode)
    /// - In native mode: X can be 8 or 16 bits (controlled by X flag)
    /// - Adds extra cycle if D.l != 0 in native mode.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr ZeroPageX(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        var pc = state.Registers.PC.GetAddr();
        state.Registers.PC.Advance();
        byte zpOffset = memory.Read(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = state.Registers.D.GetWord();

        // Add X register (wraps within 256-byte direct page)
        byte x = state.Registers.X.GetByte();
        byte effectiveOffset = (byte)(zpOffset + x);

        addrCycles++; // 1 cycle for indexing

        // Add 1 cycle if direct page not page-aligned in native mode
        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        state.Cycles += addrCycles;
        Addr effectiveAddr = (Addr)(directPage + effectiveOffset);

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.ZeroPageX;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 1;
            state.Operands[0] = zpOffset;
            state.InstructionCycles += addrCycles;
        }

        // The instruction will add 1 more cycle for the actual read
        return effectiveAddr;
    }

    /// <summary>
    /// Zero Page,Y addressing - reads zero-page/direct-page address and adds Y register.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective zero-page/direct-page address with Y offset (wraps within direct page).</returns>
    /// <remarks>
    /// Mode-aware behavior: Same as ZeroPageX but with Y register.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr ZeroPageY(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        var pc = state.Registers.PC.GetAddr();
        state.Registers.PC.Advance();
        byte zpOffset = memory.Read(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = state.Registers.D.GetWord();

        // Add Y register (wraps within 256-byte direct page)
        byte y = state.Registers.Y.GetByte();
        byte effectiveOffset = (byte)(zpOffset + y);

        addrCycles++; // 1 cycle for indexing

        // Add 1 cycle if direct page not page-aligned in native mode
        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        state.Cycles += addrCycles;
        Addr effectiveAddr = (Addr)(directPage + effectiveOffset);

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.ZeroPageY;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 1;
            state.Operands[0] = zpOffset;
            state.InstructionCycles += addrCycles;
        }

        // The instruction will add 1 more cycle for the actual read
        return effectiveAddr;
    }

    /// <summary>
    /// Absolute addressing - reads 16-bit address from PC.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The 16-bit absolute address.</returns>
    /// <remarks>
    /// Mode-aware behavior:
    /// - In 6502/65C02: Direct 16-bit address
    /// - In 65816: Uses Data Bank Register (DBR) to form 24-bit address
    /// - Result is always 16-bit for 6502/65C02 compatibility
    /// - In 65816, the actual 24-bit address would be (DBR &lt;&lt; 16) | absAddr.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr Absolute(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        Addr pc = state.Registers.PC.GetAddr();
        Addr address = memory.ReadWord(pc);
        addrCycles += 2; // 2 cycles to fetch the 16-bit address
        state.Registers.PC.Advance(2);

        state.Cycles += addrCycles;

        // In 65816, this would be combined with DBR for 24-bit addressing
        Addr effectiveAddr = ((Addr)state.Registers.DBR << 16) | address;

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.Absolute;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 2;
            state.Operands[0] = (byte)(address & 0xFF);
            state.Operands[1] = (byte)((address >> 8) & 0xFF);
            state.InstructionCycles += addrCycles;
        }

        // The instruction will add 1 more cycle for the actual read
        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,X addressing - reads 16-bit address and adds X register.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with X offset.</returns>
    /// <remarks>
    /// Mode-aware behavior:
    /// - In 6502/65C02/emulation mode: Always adds page boundary cycle on cross
    /// - In 65816 native mode: Page boundary behavior depends on instruction type
    /// - X register can be 8 or 16 bits in native mode (controlled by X flag).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr AbsoluteX(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        Addr pc = state.Registers.PC.GetAddr();
        Addr baseAddr = memory.ReadWord(pc);
        addrCycles += 2; // 2 cycles to fetch the 16-bit address
        state.Registers.PC.Advance(2);

        Addr effectiveAddr = baseAddr + state.Registers.X.GetDWord();

        // Add extra cycle if page boundary crossed
        // In emulation mode (E=1): Always check page boundary
        // In native mode (E=0): Some instructions skip this check
        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            addrCycles++;
        }

        state.Cycles += addrCycles;

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.AbsoluteX;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 2;
            state.Operands[0] = (byte)(baseAddr & 0xFF);
            state.Operands[1] = (byte)((baseAddr >> 8) & 0xFF);
            state.InstructionCycles += addrCycles;
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
    /// <remarks>
    /// Mode-aware behavior: Same as AbsoluteX but with Y register.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr AbsoluteY(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        Addr pc = state.Registers.PC.GetAddr();
        Addr baseAddr = memory.ReadWord(pc);
        addrCycles += 2; // 2 cycles to fetch the 16-bit address
        state.Registers.PC.Advance(2);

        Addr effectiveAddr = baseAddr + state.Registers.Y.GetDWord();

        // Add extra cycle if page boundary crossed
        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            addrCycles++;
        }

        state.Cycles += addrCycles;

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.AbsoluteY;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 2;
            state.Operands[0] = (byte)(baseAddr & 0xFF);
            state.Operands[1] = (byte)((baseAddr >> 8) & 0xFF);
            state.InstructionCycles += addrCycles;
        }

        // The instruction will add 1 more cycle for the actual read
        return effectiveAddr;
    }

    /// <summary>
    /// Indexed Indirect (Indirect,X) addressing - uses X-indexed zero-page pointer.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address read from the X-indexed zero-page pointer.</returns>
    /// <remarks>
    /// Mode-aware behavior:
    /// - Uses direct page (D register) in 65816 native mode
    /// - Pointer read wraps within direct page
    /// - Adds extra cycle if D.l != 0 in native mode.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr IndirectX(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        var pc = state.Registers.PC.GetAddr();
        state.Registers.PC.Advance();
        byte zpOffset = memory.Read(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = state.Registers.D.GetWord();

        // Add X register (wraps within direct page)
        byte x = state.Registers.X.GetByte();
        byte effectiveOffset = (byte)(zpOffset + x);

        addrCycles++; // 1 cycle for indexing

        // Add 1 cycle if direct page not page-aligned in native mode
        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        // Read 16-bit pointer from direct page (wraps within direct page for both bytes)
        Addr pointerAddr = (Addr)(directPage + effectiveOffset);
        Addr address = memory.ReadWord(pointerAddr);

        addrCycles += 2; // 2 cycles to read pointer from ZP

        state.Cycles += addrCycles;

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.IndirectX;
            state.EffectiveAddress = address;
            state.OperandSize = 1;
            state.Operands[0] = zpOffset;
            state.InstructionCycles += addrCycles;
        }

        // The instruction will add 1 more cycle for the actual read
        return address;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing - uses zero-page pointer indexed by Y.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset applied to the zero-page pointer.</returns>
    /// <remarks>
    /// Mode-aware behavior:
    /// - Uses direct page (D register) in 65816 native mode
    /// - Page boundary crossing adds cycle in emulation mode
    /// - Adds extra cycle if D.l != 0 in native mode.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr IndirectY(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        var pc = state.Registers.PC.GetAddr();
        state.Registers.PC.Advance();
        byte zpOffset = memory.Read(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = state.Registers.D.GetWord();

        // Add 1 cycle if direct page not page-aligned in native mode
        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        // Read 16-bit pointer from direct page
        Addr pointerAddr = (Addr)(directPage + zpOffset);
        Addr baseAddr = memory.ReadWord(pointerAddr);

        addrCycles += 2; // 2 cycles to read pointer from ZP

        // Add Y register
        byte y = state.Registers.Y.GetByte();
        Addr effectiveAddr = baseAddr + y;

        // Add extra cycle if page boundary crossed
        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            addrCycles++;
        }

        state.Cycles += addrCycles;

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.IndirectY;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 1;
            state.Operands[0] = zpOffset;
            state.InstructionCycles += addrCycles;
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
    /// <remarks>
    /// Mode-aware behavior:
    /// - Always takes maximum cycles regardless of page boundary crossing
    /// - In 65816 native mode, behavior matches read mode but without conditional cycle.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr AbsoluteXWrite(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        Addr pc = state.Registers.PC.GetAddr();
        Addr baseAddr = memory.ReadWord(pc);
        state.Registers.PC.Advance(2);
        Addr effectiveAddr = baseAddr + state.Registers.X.GetByte();
        addrCycles += 3; // 2 cycles to fetch address + 1 extra for write operations

        state.Cycles += addrCycles;

        // Write operations always take the maximum cycles
        // No conditional page boundary check
        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.AbsoluteX;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 2;
            state.Operands[0] = (byte)(baseAddr & 0xFF);
            state.Operands[1] = (byte)((baseAddr >> 8) & 0xFF);
            state.InstructionCycles += addrCycles;
        }

        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset.</returns>
    /// <remarks>
    /// Mode-aware behavior: Same as AbsoluteXWrite but with Y register.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr AbsoluteYWrite(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        Addr pc = state.Registers.PC.GetAddr();
        Addr baseAddr = memory.ReadWord(pc);
        state.Registers.PC.Advance(2);
        Addr effectiveAddr = baseAddr + state.Registers.Y.GetByte();
        addrCycles += 3; // 2 cycles to fetch address + 1 extra for write operations

        state.Cycles += addrCycles;

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.AbsoluteY;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 2;
            state.Operands[0] = (byte)(baseAddr & 0xFF);
            state.Operands[1] = (byte)((baseAddr >> 8) & 0xFF);
            state.InstructionCycles += addrCycles;
        }

        // The instruction will add 1 more cycle for the actual write
        return effectiveAddr;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state.</param>
    /// <returns>The effective address with Y offset applied to the zero-page pointer.</returns>
    /// <remarks>
    /// Mode-aware behavior:
    /// - Always takes maximum cycles regardless of page boundary
    /// - Uses direct page (D register) in 65816 native mode.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr IndirectYWrite(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        var pc = state.Registers.PC.GetAddr();
        state.Registers.PC.Advance();
        byte zpOffset = memory.Read(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = state.Registers.D.GetWord();

        // Add 1 cycle if direct page not page-aligned in native mode
        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        // Read 16-bit pointer from direct page
        Addr pointerAddr = (Addr)(directPage + zpOffset);
        Addr baseAddr = memory.ReadWord(pointerAddr);

        addrCycles += 2; // 2 cycles to read pointer from ZP

        // Add Y register
        byte y = state.Registers.Y.GetByte();
        Addr effectiveAddr = baseAddr + y;

        addrCycles++; // 1 extra cycle for write (always taken)

        state.Cycles += addrCycles;

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.IndirectY;
            state.EffectiveAddress = effectiveAddr;
            state.OperandSize = 1;
            state.Operands[0] = zpOffset;
            state.InstructionCycles += addrCycles;
        }

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
    /// Mode-aware behavior:
    /// - In 6502/65C02/emulation mode: Operates on 8-bit accumulator
    /// - In 65816 native mode with M=0: Operates on 16-bit accumulator
    /// - Instruction implementation handles width differences.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr Accumulator(IMemory memory, ref CpuState state)
    {
        // No addressing needed, no PC increment, no cycles
        // Mode differences handled by instruction implementation
        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.Accumulator;
            state.EffectiveAddress = 0;
            state.OperandSize = 0;
        }

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
    /// Mode-aware behavior:
    /// - In emulation mode: Page boundary crossing adds cycle
    /// - In native mode: Some implementations may not add page boundary cycle
    /// - Branch instruction implementation handles mode-specific cycle counting.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr Relative(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        var pc = state.Registers.PC.GetAddr();
        state.Registers.PC.Advance();
        sbyte offset = (sbyte)memory.Read(pc);
        addrCycles++; // 1 cycle to fetch the offset

        Addr targetAddr = (Addr)(state.Registers.PC.GetAddr() + offset);

        state.Cycles += addrCycles;

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.Relative;
            state.EffectiveAddress = targetAddr;
            state.OperandSize = 1;
            state.Operands[0] = (byte)offset;
            state.InstructionCycles += addrCycles;
        }

        // Branch instructions will add extra cycles if branch is taken
        // Mode-aware cycle counting is handled by the branch instruction itself
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
    /// Mode-aware behavior:
    /// - In 6502: Page boundary wrapping bug (if pointer at $xxFF, high byte read from $xx00)
    /// - In 65C02: Fixed to read correctly across page boundaries
    /// - In 65816: Enhanced with additional indirect modes (not this one).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Addr Indirect(IMemory memory, ref CpuState state)
    {
        byte addrCycles = 0;
        Addr pc = state.Registers.PC.GetAddr();
        Addr pointerAddr = memory.ReadWord(pc);
        addrCycles += 2; // 2 cycles to fetch pointer address
        state.Registers.PC.Advance(2);

        // In 65C02+, this correctly handles page boundary crossing
        // In original 6502, there was a bug if pointerAddr ended in $FF
        Addr targetAddr = memory.ReadWord(pointerAddr);

        addrCycles += 2; // 2 cycles to read target

        state.Cycles += addrCycles;

        if (state.IsDebuggerAttached)
        {
            state.AddressingMode = CpuAddressingModes.Indirect;
            state.EffectiveAddress = targetAddr;
            state.OperandSize = 2;
            state.Operands[0] = (byte)(pointerAddr & 0xFF);
            state.Operands[1] = (byte)((pointerAddr >> 8) & 0xFF);
            state.InstructionCycles += addrCycles;
        }

        // The instruction will add 1 more cycle for execution
        return targetAddr;
    }
}