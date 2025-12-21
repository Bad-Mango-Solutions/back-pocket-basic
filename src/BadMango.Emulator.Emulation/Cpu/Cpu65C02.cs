// <copyright file="Cpu65C02.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Core;

/// <summary>
/// WDC 65C02 CPU emulator with cycle-accurate execution.
/// </summary>
/// <remarks>
/// The 65C02 is an enhanced version of the 6502 with additional instructions,
/// addressing modes, and bug fixes. This implementation provides a minimal but
/// functional CPU core with basic instruction support.
/// Optimized with aggressive inlining for maximum performance.
/// </remarks>
public class Cpu65C02 : ICpu<Cpu65C02Registers, Cpu65C02State>
{
    // Processor status flags
    private const byte FlagC = 0x01; // Carry
    private const byte FlagZ = 0x02; // Zero
    private const byte FlagI = 0x04; // Interrupt Disable
    private const byte FlagD = 0x08; // Decimal Mode
    private const byte FlagB = 0x10; // Break
    private const byte FlagU = 0x20; // Unused (always 1)
    private const byte FlagV = 0x40; // Overflow
    private const byte FlagN = 0x80; // Negative

    // Reset vector address
    private const ushort ResetVector = 0xFFFC;

    // Stack base address
    private const ushort StackBase = 0x0100;

    private readonly IMemory memory;
    private readonly OpcodeTable<Cpu65C02> opcodeTable;

    private byte a;  // Accumulator
    private byte x;  // X register
    private byte y;  // Y register
    private byte s;  // Stack pointer
    private byte p;  // Processor status
    private ushort pc; // Program counter
    private ulong cycles; // Total cycles executed
    private bool halted;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cpu65C02"/> class.
    /// </summary>
    /// <param name="memory">The memory interface for the CPU.</param>
    public Cpu65C02(IMemory memory)
    {
        this.memory = memory ?? throw new ArgumentNullException(nameof(memory));
        opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
    }

    /// <inheritdoc/>
    public bool Halted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => halted;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        a = 0;
        x = 0;
        y = 0;
        s = 0xFD;
        p = FlagU | FlagI; // Unused flag always set, interrupts disabled
        pc = memory.ReadWord(ResetVector);
        cycles = 0;
        halted = false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Step()
    {
        if (halted)
        {
            return 0;
        }

        ulong cyclesBefore = cycles;
        byte opcode = FetchByte();
        opcodeTable.Execute(opcode, this);
        return (int)(cycles - cyclesBefore);
    }

    /// <inheritdoc/>
    public void Execute(uint startAddress)
    {
        pc = (ushort)startAddress;
        halted = false;

        while (!halted)
        {
            Step();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cpu65C02Registers GetRegisters()
    {
        return new Cpu65C02Registers
        {
            A = a,
            X = x,
            Y = y,
            S = s,
            P = p,
            PC = pc,
        };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cpu65C02State GetState()
    {
        return new Cpu65C02State
        {
            Registers = new Cpu65C02Registers
            {
                A = a,
                X = x,
                Y = y,
                S = s,
                P = p,
                PC = pc,
            },
            Cycles = cycles,
        };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetState(Cpu65C02State state)
    {
        a = state.A;
        x = state.X;
        y = state.Y;
        s = state.S;
        p = state.P;
        pc = state.PC;
        cycles = state.Cycles;
    }

    // Instruction implementations

    /// <summary>
    /// BRK - Force Break instruction. Causes a software interrupt.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void BRK()
    {
        // BRK causes a software interrupt
        // Total 7 cycles: 1 (opcode fetch) + 1 (PC increment) + 2 (push PC) + 1 (push P) + 2 (read IRQ vector)
        pc++;
        memory.Write((ushort)(StackBase + s--), (byte)(pc >> 8));
        memory.Write((ushort)(StackBase + s--), (byte)(pc & 0xFF));
        memory.Write((ushort)(StackBase + s--), (byte)(p | FlagB));
        p |= FlagI;
        pc = memory.ReadWord(0xFFFE);
        cycles += 6; // 6 cycles in handler + 1 from opcode fetch in Step()
        halted = true; // For now, halt on BRK
    }

    /// <summary>
    /// LDA - Load Accumulator (Immediate addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDA_Immediate()
    {
        a = FetchByte();
        SetZN(a);
    }

    /// <summary>
    /// LDA - Load Accumulator (Zero Page addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDA_ZeroPage()
    {
        a = ReadZeroPage();
        SetZN(a);
    }

    /// <summary>
    /// LDA - Load Accumulator (Zero Page,X addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDA_ZeroPageX()
    {
        a = ReadZeroPageX();
        SetZN(a);
    }

    /// <summary>
    /// LDA - Load Accumulator (Absolute addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDA_Absolute()
    {
        a = ReadAbsolute();
        SetZN(a);
    }

    /// <summary>
    /// LDA - Load Accumulator (Absolute,X addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDA_AbsoluteX()
    {
        a = ReadAbsoluteX();
        SetZN(a);
    }

    /// <summary>
    /// LDA - Load Accumulator (Absolute,Y addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDA_AbsoluteY()
    {
        a = ReadAbsoluteY();
        SetZN(a);
    }

    /// <summary>
    /// LDA - Load Accumulator (Indexed Indirect addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDA_IndirectX()
    {
        a = ReadIndirectX();
        SetZN(a);
    }

    /// <summary>
    /// LDA - Load Accumulator (Indirect Indexed addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDA_IndirectY()
    {
        a = ReadIndirectY();
        SetZN(a);
    }

    /// <summary>
    /// LDX - Load X Register (Immediate addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDX_Immediate()
    {
        x = FetchByte();
        SetZN(x);
    }

    /// <summary>
    /// LDY - Load Y Register (Immediate addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void LDY_Immediate()
    {
        y = FetchByte();
        SetZN(y);
    }

    /// <summary>
    /// STA - Store Accumulator (Zero Page addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void STA_ZeroPage()
    {
        WriteZeroPage(a);
    }

    /// <summary>
    /// STA - Store Accumulator (Zero Page,X addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void STA_ZeroPageX()
    {
        WriteZeroPageX(a);
    }

    /// <summary>
    /// STA - Store Accumulator (Absolute addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void STA_Absolute()
    {
        WriteAbsolute(a);
    }

    /// <summary>
    /// STA - Store Accumulator (Absolute,X addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void STA_AbsoluteX()
    {
        WriteAbsoluteX(a);
    }

    /// <summary>
    /// STA - Store Accumulator (Absolute,Y addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void STA_AbsoluteY()
    {
        WriteAbsoluteY(a);
    }

    /// <summary>
    /// STA - Store Accumulator (Indexed Indirect addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void STA_IndirectX()
    {
        WriteIndirectX(a);
    }

    /// <summary>
    /// STA - Store Accumulator (Indirect Indexed addressing mode).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void STA_IndirectY()
    {
        WriteIndirectY(a);
    }

    /// <summary>
    /// NOP - No Operation instruction.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void NOP()
    {
        cycles++; // Total 2 cycles (1 from FetchByte + 1 here)
    }

    /// <summary>
    /// Handles illegal/undefined opcodes by halting execution.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IllegalOpcode()
    {
        // For illegal opcodes, just halt execution
        halted = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchByte()
    {
        byte value = memory.Read(pc++);
        cycles++;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort FetchWord()
    {
        ushort value = memory.ReadWord(pc);
        pc += 2;
        cycles += 2;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetZN(byte value)
    {
        if (value == 0)
        {
            p |= FlagZ;
        }
        else
        {
            p &= unchecked((byte)~FlagZ);
        }

        if ((value & 0x80) != 0)
        {
            p |= FlagN;
        }
        else
        {
            p &= unchecked((byte)~FlagN);
        }
    }

    // Addressing mode helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadZeroPage()
    {
        byte address = FetchByte();
        cycles++;
        return memory.Read(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadZeroPageX()
    {
        byte address = (byte)(FetchByte() + x);
        cycles += 2;
        return memory.Read(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadAbsolute()
    {
        ushort address = FetchWord();
        cycles++;
        return memory.Read(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadAbsoluteX()
    {
        ushort address = FetchWord();
        ushort effectiveAddress = (ushort)(address + x);
        cycles++;
        if ((address & 0xFF00) != (effectiveAddress & 0xFF00))
        {
            cycles++; // Page boundary crossed
        }

        return memory.Read(effectiveAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadAbsoluteY()
    {
        ushort address = FetchWord();
        ushort effectiveAddress = (ushort)(address + y);
        cycles++;
        if ((address & 0xFF00) != (effectiveAddress & 0xFF00))
        {
            cycles++; // Page boundary crossed
        }

        return memory.Read(effectiveAddress);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadIndirectX()
    {
        byte zpAddress = (byte)(FetchByte() + x);
        cycles++; // Index addition
        ushort address = memory.ReadWord(zpAddress);
        cycles += 2; // Read word from zero page
        byte value = memory.Read(address);
        cycles++; // Final read
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadIndirectY()
    {
        byte zpAddress = FetchByte();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteZeroPage(byte value)
    {
        byte address = FetchByte();
        memory.Write(address, value);
        cycles += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteZeroPageX(byte value)
    {
        byte address = (byte)(FetchByte() + x);
        memory.Write(address, value);
        cycles += 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteAbsolute(byte value)
    {
        ushort address = FetchWord();
        memory.Write(address, value);
        cycles += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteAbsoluteX(byte value)
    {
        ushort address = FetchWord();
        ushort effectiveAddress = (ushort)(address + x);
        memory.Write(effectiveAddress, value);
        cycles += 2; // Index addition + write
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteAbsoluteY(byte value)
    {
        ushort address = FetchWord();
        ushort effectiveAddress = (ushort)(address + y);
        memory.Write(effectiveAddress, value);
        cycles += 2; // Index addition + write
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteIndirectX(byte value)
    {
        byte zpAddress = (byte)(FetchByte() + x);
        cycles++; // Index addition
        ushort address = memory.ReadWord(zpAddress);
        cycles += 2; // Read word from zero page
        memory.Write(address, value);
        cycles++; // Final write
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteIndirectY(byte value)
    {
        byte zpAddress = FetchByte();
        ushort address = memory.ReadWord(zpAddress);
        cycles += 2; // Read word from zero page
        ushort effectiveAddress = (ushort)(address + y);
        cycles++; // Index addition
        memory.Write(effectiveAddress, value);
        cycles++; // Final write
    }
}