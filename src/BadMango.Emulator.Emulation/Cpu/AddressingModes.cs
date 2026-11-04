// <copyright file="AddressingModes.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using Core.Cpu;
using Core.Interfaces.Cpu;

/// <summary>
/// Provides addressing mode implementations for 6502-family CPUs.
/// </summary>
/// <remarks>
/// Each addressing mode is a function that computes an effective address given a CPU instance.
/// The CPU provides access to both state and memory through its interface.
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
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>Returns zero as a placeholder address since no memory access is needed.</returns>
    /// <remarks>
    /// Returns zero as a placeholder address. Instructions using this mode
    /// typically operate only on registers or the stack (e.g., NOP, CLC, SEI).
    /// Mode-agnostic: behavior is identical across all CPU modes.
    /// </remarks>
    public static Addr Implied(ICpu cpu)
    {
        // No addressing needed, no PC increment, no cycles
        // Mode-agnostic
        if (cpu.IsDebuggerAttached)
        {
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.Implied,
                EffectiveAddress = 0,
                OperandSize = 0,
            };
        }

        return 0;
    }

    /// <summary>
    /// Immediate addressing - returns the address of the immediate operand (PC).
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The address of the immediate operand (current PC value).</returns>
    /// <remarks>
    /// Mode-aware behavior:
    /// - In emulation mode: Always fetches 8-bit immediate
    /// - In native mode with M=0: Would fetch 16-bit immediate (handled by instruction, not addressing mode)
    /// - In native mode with X=0: Would fetch 16-bit immediate for index operations.
    /// </remarks>
    public static Addr Immediate(ICpu cpu)
    {
        Addr address = cpu.Registers.PC.GetDWord();
        cpu.Registers.PC.Advance();

        if (cpu.IsDebuggerAttached)
        {
            cpu.Trace = cpu.Trace with { AddressingMode = CpuAddressingModes.Immediate };
        }

        return address;
    }

    /// <summary>
    /// Zero Page addressing - reads zero-page/direct-page address from PC.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The zero-page/direct-page address.</returns>
    public static Addr ZeroPage(ICpu cpu)
    {
        byte addrCycles = 0;
        byte zpOffset = cpu.Read8(cpu.Registers.PC.addr++);
        addrCycles++; // 1 cycle to fetch the ZP address

        Word directPage = cpu.Registers.D.GetWord();

        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        cpu.Registers.TCU += addrCycles;
        Addr effectiveAddr = (Addr)(directPage + zpOffset);

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = zpOffset;
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.ZeroPage,
                EffectiveAddress = effectiveAddr,
                OperandSize = 1,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Zero Page,X addressing - reads zero-page/direct-page address and adds X register.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The effective zero-page/direct-page address with X offset (wraps within direct page).</returns>
    public static Addr ZeroPageX(ICpu cpu)
    {
        byte addrCycles = 0;
        var pc = cpu.Registers.PC.GetAddr();
        cpu.Registers.PC.Advance();
        byte zpOffset = cpu.Read8(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = cpu.Registers.D.GetWord();

        byte x = cpu.Registers.X.GetByte();
        byte effectiveOffset = (byte)(zpOffset + x);

        addrCycles++; // 1 cycle for indexing

        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        cpu.Registers.TCU += addrCycles;
        Addr effectiveAddr = (Addr)(directPage + effectiveOffset);

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = zpOffset;
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.ZeroPageX,
                EffectiveAddress = effectiveAddr,
                OperandSize = 1,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Zero Page,Y addressing - reads zero-page/direct-page address and adds Y register.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The effective zero-page/direct-page address with Y offset (wraps within direct page).</returns>
    public static Addr ZeroPageY(ICpu cpu)
    {
        byte addrCycles = 0;
        var pc = cpu.Registers.PC.GetAddr();
        cpu.Registers.PC.Advance();
        byte zpOffset = cpu.Read8(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = cpu.Registers.D.GetWord();

        byte y = cpu.Registers.Y.GetByte();
        byte effectiveOffset = (byte)(zpOffset + y);

        addrCycles++; // 1 cycle for indexing

        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        cpu.Registers.TCU += addrCycles;
        Addr effectiveAddr = (Addr)(directPage + effectiveOffset);

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = zpOffset;
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.ZeroPageY,
                EffectiveAddress = effectiveAddr,
                OperandSize = 1,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Absolute addressing - reads 16-bit address from PC.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The 16-bit absolute address.</returns>
    public static Addr Absolute(ICpu cpu)
    {
        byte addrCycles = 0;
        Addr pc = cpu.Registers.PC.GetAddr();
        Addr address = cpu.Read16(pc);
        addrCycles += 2; // 2 cycles to fetch the 16-bit address
        cpu.Registers.PC.Advance(2);

        cpu.Registers.TCU += addrCycles;

        Addr effectiveAddr = ((Addr)cpu.Registers.DBR << 16) | address;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = (byte)(address & 0xFF);
            operands[1] = (byte)((address >> 8) & 0xFF);
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.Absolute,
                EffectiveAddress = effectiveAddr,
                OperandSize = 2,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,X addressing - reads 16-bit address and adds X register.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The effective address with X offset.</returns>
    public static Addr AbsoluteX(ICpu cpu)
    {
        byte addrCycles = 0;
        Addr pc = cpu.Registers.PC.GetAddr();
        Addr baseAddr = cpu.Read16(pc);
        addrCycles += 2; // 2 cycles to fetch the 16-bit address
        cpu.Registers.PC.Advance(2);

        Addr effectiveAddr = (baseAddr + cpu.Registers.X.GetDWord()) & 0xFFFF;

        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            addrCycles++;
        }

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = (byte)(baseAddr & 0xFF);
            operands[1] = (byte)((baseAddr >> 8) & 0xFF);
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.AbsoluteX,
                EffectiveAddress = effectiveAddr,
                OperandSize = 2,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,Y addressing - reads 16-bit address and adds Y register.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The effective address with Y offset.</returns>
    public static Addr AbsoluteY(ICpu cpu)
    {
        byte addrCycles = 0;
        Addr pc = cpu.Registers.PC.GetAddr();
        Addr baseAddr = cpu.Read16(pc);
        addrCycles += 2; // 2 cycles to fetch the 16-bit address
        cpu.Registers.PC.Advance(2);

        Addr effectiveAddr = (baseAddr + cpu.Registers.Y.GetDWord()) & 0xFFFF;

        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            addrCycles++;
        }

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = (byte)(baseAddr & 0xFF);
            operands[1] = (byte)((baseAddr >> 8) & 0xFF);
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.AbsoluteY,
                EffectiveAddress = effectiveAddr,
                OperandSize = 2,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Indexed Indirect (Indirect,X) addressing - uses X-indexed zero-page pointer.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The effective address read from the X-indexed zero-page pointer.</returns>
    public static Addr IndirectX(ICpu cpu)
    {
        byte addrCycles = 0;
        var pc = cpu.Registers.PC.GetAddr();
        cpu.Registers.PC.Advance();
        byte zpOffset = cpu.Read8(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = cpu.Registers.D.GetWord();

        byte x = cpu.Registers.X.GetByte();
        byte effectiveOffset = (byte)(zpOffset + x);

        addrCycles++; // 1 cycle for indexing

        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        Addr pointerAddr = (Addr)(directPage + effectiveOffset);

        // Read 16-bit pointer with zero-page wrap on the high-byte fetch.
        // Per the 6502/65C02 spec, all zero-page indirect modes wrap within
        // the zero page: if the pointer is at $FF, the high byte is read
        // from $00, not $100 (which would hit the stack). Mirror the helper
        // pattern used in ZeroPageIndirect: lo = Read8(ptr); hi = Read8((ptr & 0xFF00) | ((ptr + 1) & 0xFF)).
        byte addrLo = cpu.Read8(pointerAddr);
        byte addrHi = cpu.Read8((pointerAddr & 0xFF00) | ((pointerAddr + 1) & 0xFF));
        Addr address = (Addr)((addrHi << 8) | addrLo);

        addrCycles += 2; // 2 cycles to read pointer from ZP

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = zpOffset;
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.IndirectX,
                EffectiveAddress = address,
                OperandSize = 1,
                Operands = operands,
            };
        }

        return address;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing - uses zero-page pointer indexed by Y.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The effective address with Y offset applied to the zero-page pointer.</returns>
    public static Addr IndirectY(ICpu cpu)
    {
        byte addrCycles = 0;
        var pc = cpu.Registers.PC.GetAddr();
        cpu.Registers.PC.Advance();
        byte zpOffset = cpu.Read8(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = cpu.Registers.D.GetWord();

        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        Addr pointerAddr = (Addr)(directPage + zpOffset);

        // Read 16-bit pointer with zero-page wrap on the high-byte fetch.
        // Per the 6502/65C02 spec, all zero-page indirect modes wrap within
        // the zero page: if the pointer is at $FF, the high byte is read
        // from $00, not $100 (which would hit the stack). Mirror the helper
        // pattern used in ZeroPageIndirect.
        byte ptrLo = cpu.Read8(pointerAddr);
        byte ptrHi = cpu.Read8((pointerAddr & 0xFF00) | ((pointerAddr + 1) & 0xFF));
        Addr baseAddr = (Addr)((ptrHi << 8) | ptrLo);

        addrCycles += 2; // 2 cycles to read pointer from ZP

        byte y = cpu.Registers.Y.GetByte();
        Addr effectiveAddr = (baseAddr + y) & 0xFFFF;

        if ((baseAddr & 0xFF00) != (effectiveAddr & 0xFF00))
        {
            addrCycles++;
        }

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = zpOffset;
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.IndirectY,
                EffectiveAddress = effectiveAddr,
                OperandSize = 1,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,X addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The effective address with X offset.</returns>
    public static Addr AbsoluteXWrite(ICpu cpu)
    {
        byte addrCycles = 0;
        Addr pc = cpu.Registers.PC.GetAddr();
        Addr baseAddr = cpu.Read16(pc);
        cpu.Registers.PC.Advance(2);
        Addr effectiveAddr = (baseAddr + cpu.Registers.X.GetByte()) & 0xFFFF;
        addrCycles += 3; // 2 cycles to fetch address + 1 extra for write operations

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = (byte)(baseAddr & 0xFF);
            operands[1] = (byte)((baseAddr >> 8) & 0xFF);
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.AbsoluteX,
                EffectiveAddress = effectiveAddr,
                OperandSize = 2,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Absolute,Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The effective address with Y offset.</returns>
    public static Addr AbsoluteYWrite(ICpu cpu)
    {
        byte addrCycles = 0;
        Addr pc = cpu.Registers.PC.GetAddr();
        Addr baseAddr = cpu.Read16(pc);
        cpu.Registers.PC.Advance(2);
        Addr effectiveAddr = (baseAddr + cpu.Registers.Y.GetByte()) & 0xFFFF;
        addrCycles += 3; // 2 cycles to fetch address + 1 extra for write operations

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = (byte)(baseAddr & 0xFF);
            operands[1] = (byte)((baseAddr >> 8) & 0xFF);
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.AbsoluteY,
                EffectiveAddress = effectiveAddr,
                OperandSize = 2,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Indirect Indexed (Indirect),Y addressing for write operations - always takes maximum cycles.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The effective address with Y offset applied to the zero-page pointer.</returns>
    public static Addr IndirectYWrite(ICpu cpu)
    {
        byte addrCycles = 0;
        var pc = cpu.Registers.PC.GetAddr();
        cpu.Registers.PC.Advance();
        byte zpOffset = cpu.Read8(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = cpu.Registers.D.GetWord();

        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        Addr pointerAddr = (Addr)(directPage + zpOffset);

        // Read 16-bit pointer with zero-page wrap on the high-byte fetch
        // (see IndirectY/ZeroPageIndirect for the rationale).
        byte ptrLo = cpu.Read8(pointerAddr);
        byte ptrHi = cpu.Read8((pointerAddr & 0xFF00) | ((pointerAddr + 1) & 0xFF));
        Addr baseAddr = (Addr)((ptrHi << 8) | ptrLo);

        addrCycles += 2; // 2 cycles to read pointer from ZP

        byte y = cpu.Registers.Y.GetByte();
        Addr effectiveAddr = (baseAddr + y) & 0xFFFF;

        addrCycles++; // 1 extra cycle for write (always taken)

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = zpOffset;
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.IndirectY,
                EffectiveAddress = effectiveAddr,
                OperandSize = 1,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Accumulator addressing - used for shift/rotate operations on the accumulator.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>Returns zero as a placeholder address since the operation is on the accumulator.</returns>
    public static Addr Accumulator(ICpu cpu)
    {
        if (cpu.IsDebuggerAttached)
        {
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.Accumulator,
                EffectiveAddress = 0,
                OperandSize = 0,
            };
        }

        return 0;
    }

    /// <summary>
    /// Relative addressing - used for branch instructions.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The target address for the branch (PC + signed offset).</returns>
    public static Addr Relative(ICpu cpu)
    {
        byte addrCycles = 0;
        var pc = cpu.Registers.PC.GetAddr();
        cpu.Registers.PC.Advance();
        sbyte offset = (sbyte)cpu.Read8(pc);
        addrCycles++; // 1 cycle to fetch the offset

        Addr targetAddr = (Addr)(cpu.Registers.PC.GetAddr() + offset);

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = (byte)offset;
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.Relative,
                EffectiveAddress = targetAddr,
                OperandSize = 1,
                Operands = operands,
            };
        }

        return targetAddr;
    }

    /// <summary>
    /// Indirect addressing - reads a 16-bit address from memory location.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The address read from the indirect pointer.</returns>
    public static Addr Indirect(ICpu cpu)
    {
        byte addrCycles = 0;
        Addr pc = cpu.Registers.PC.GetAddr();
        Addr pointerAddr = cpu.Read16(pc);
        addrCycles += 2; // 2 cycles to fetch pointer address
        cpu.Registers.PC.Advance(2);

        Addr targetAddr = cpu.Read16(pointerAddr);

        // 65C02 adds one extra cycle for JMP indirect compared to NMOS 6502.
        // The NMOS bug (page-wrap on $xxFF) is fixed: Read16 correctly reads
        // consecutive bytes across page boundaries.
        addrCycles += 3; // 2 cycles to read target + 1 extra for 65C02

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = (byte)(pointerAddr & 0xFF);
            operands[1] = (byte)((pointerAddr >> 8) & 0xFF);
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.Indirect,
                EffectiveAddress = targetAddr,
                OperandSize = 2,
                Operands = operands,
            };
        }

        return targetAddr;
    }

    /// <summary>
    /// Zero-page indirect addressing (65C02) - reads a 16-bit address from a zero-page pointer.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The address read from the zero-page pointer.</returns>
    /// <remarks>
    /// The pointer fetch wraps within zero page: if the pointer is at $FF, the high byte
    /// is read from $00, not $100. This is consistent with other zero-page indirect modes.
    /// </remarks>
    public static Addr ZeroPageIndirect(ICpu cpu)
    {
        byte addrCycles = 0;
        var pc = cpu.Registers.PC.GetAddr();
        cpu.Registers.PC.Advance();
        byte zpOffset = cpu.Read8(pc);
        addrCycles++; // 1 cycle to fetch ZP address

        Word directPage = cpu.Registers.D.GetWord();

        if ((directPage & 0xFF) != 0)
        {
            addrCycles++;
        }

        Addr pointerAddr = (Addr)(directPage + zpOffset);

        // Read 16-bit pointer with zero-page wrap on the high-byte fetch.
        // Per the checklist, this uses the helper pattern for ZP indirect:
        // lo = Read8(ptr); hi = Read8((ptr & 0xFF00) | ((ptr + 1) & 0xFF))
        byte lo = cpu.Read8(pointerAddr);
        byte hi = cpu.Read8((pointerAddr & 0xFF00) | ((pointerAddr + 1) & 0xFF));
        Addr effectiveAddr = (Addr)((hi << 8) | lo);

        addrCycles += 2; // 2 cycles to read pointer from ZP

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = zpOffset;
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.ZeroPageIndirect,
                EffectiveAddress = effectiveAddr,
                OperandSize = 1,
                Operands = operands,
            };
        }

        return effectiveAddr;
    }

    /// <summary>
    /// Absolute indirect X addressing (65C02) - reads a 16-bit address from (abs + X).
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>The address read from the indexed pointer.</returns>
    /// <remarks>
    /// Used by JMP ($abs,X). Unlike NMOS 6502's JMP ($abs) page-wrap bug, this mode
    /// correctly reads the target address across page boundaries. Total 6 cycles.
    /// </remarks>
    public static Addr AbsoluteIndirectX(ICpu cpu)
    {
        byte addrCycles = 0;
        Addr pc = cpu.Registers.PC.GetAddr();
        Addr baseAddr = cpu.Read16(pc);
        cpu.Registers.PC.Advance(2);
        addrCycles += 2; // 2 cycles to fetch base address

        Addr pointerAddr = (baseAddr + cpu.Registers.X.GetByte()) & 0xFFFF;
        addrCycles++; // 1 cycle for indexing

        Addr targetAddr = cpu.Read16(pointerAddr);
        addrCycles += 2; // 2 cycles to read target address

        // Total: 2 (fetch base) + 1 (index) + 2 (read target) = 5 cycles from addressing mode
        // + 1 cycle from base instruction fetch = 6 cycles total for JMP ($abs,X)
        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = (byte)(baseAddr & 0xFF);
            operands[1] = (byte)((baseAddr >> 8) & 0xFF);
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.AbsoluteIndirectX,
                EffectiveAddress = targetAddr,
                OperandSize = 2,
                Operands = operands,
            };
        }

        return targetAddr;
    }

    /// <summary>
    /// Zero Page Relative addressing (Rockwell/WDC 65C02) - fetches ZP address and relative offset.
    /// </summary>
    /// <param name="cpu">The CPU instance providing state and memory access.</param>
    /// <returns>
    /// Both operands packed into one value: bits 0-7 hold the zero-page address
    /// (first operand) and bits 8-15 hold the raw relative offset byte (second
    /// operand). Callers decode them as <c>(byte)(encoded &amp; 0xFF)</c> and
    /// <c>(sbyte)((encoded &gt;&gt; 8) &amp; 0xFF)</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Used by bit branch instructions (BBR/BBS). This is a 3-byte instruction format:
    /// opcode + ZP address + signed relative offset. The addressing mode fetches both
    /// operands, advances PC by 2, and adds 2 cycles.
    /// </para>
    /// <para>
    /// The instruction handler is responsible for decoding the packed return value,
    /// reading the zero-page byte, and performing the branch logic. The returned value
    /// is not an effective address and must not be used as one.
    /// </para>
    /// </remarks>
    public static Addr ZeroPageRelative(ICpu cpu)
    {
        byte addrCycles = 0;

        // Fetch ZP address (first operand)
        byte zpAddr = cpu.Read8(cpu.Registers.PC.addr++);
        addrCycles++; // 1 cycle to fetch ZP address

        // Fetch relative offset (second operand)
        byte relativeOffset = cpu.Read8(cpu.Registers.PC.addr++);
        addrCycles++; // 1 cycle to fetch relative offset

        cpu.Registers.TCU += addrCycles;

        if (cpu.IsDebuggerAttached)
        {
            var operands = cpu.Trace.Operands;
            operands[0] = zpAddr;
            operands[1] = relativeOffset;
            cpu.Trace = cpu.Trace with
            {
                AddressingMode = CpuAddressingModes.ZeroPageRelative,
                EffectiveAddress = zpAddr,
                OperandSize = 2,
                Operands = operands,
            };
        }

        // Encode both operands in return value: low byte = ZP addr, next byte = offset
        return (Addr)((relativeOffset << 8) | zpAddr);
    }
}