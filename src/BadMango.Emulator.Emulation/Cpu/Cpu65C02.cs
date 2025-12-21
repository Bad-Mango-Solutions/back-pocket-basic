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
    private readonly Action<Cpu65C02>[] opcodeTable;

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
        opcodeTable = BuildOpcodeTable();
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
        opcodeTable[opcode](this);
        return (int)(cycles - cyclesBefore);
    }

    /// <inheritdoc/>
    public void Execute(int startAddress)
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

    private static Action<Cpu65C02>[] BuildOpcodeTable()
    {
        var table = new Action<Cpu65C02>[256];

        // Initialize all opcodes to NOP (illegal opcodes)
        for (int i = 0; i < 256; i++)
        {
            table[i] = cpu => cpu.IllegalOpcode();
        }

        // BRK - Force Break
        table[0x00] = cpu => cpu.BRK();

        // LDA - Load Accumulator
        table[0xA9] = cpu => cpu.LDA_Immediate();
        table[0xA5] = cpu => cpu.LDA_ZeroPage();
        table[0xB5] = cpu => cpu.LDA_ZeroPageX();
        table[0xAD] = cpu => cpu.LDA_Absolute();
        table[0xBD] = cpu => cpu.LDA_AbsoluteX();
        table[0xB9] = cpu => cpu.LDA_AbsoluteY();
        table[0xA1] = cpu => cpu.LDA_IndirectX();
        table[0xB1] = cpu => cpu.LDA_IndirectY();

        // STA - Store Accumulator
        table[0x85] = cpu => cpu.STA_ZeroPage();
        table[0x95] = cpu => cpu.STA_ZeroPageX();
        table[0x8D] = cpu => cpu.STA_Absolute();
        table[0x9D] = cpu => cpu.STA_AbsoluteX();
        table[0x99] = cpu => cpu.STA_AbsoluteY();
        table[0x81] = cpu => cpu.STA_IndirectX();
        table[0x91] = cpu => cpu.STA_IndirectY();

        // NOP - No Operation
        table[0xEA] = cpu => cpu.NOP();

        return table;
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
        cycles += 3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteAbsoluteY(byte value)
    {
        ushort address = FetchWord();
        ushort effectiveAddress = (ushort)(address + y);
        memory.Write(effectiveAddress, value);
        cycles += 3;
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
        memory.Write(effectiveAddress, value);
        cycles++; // Final write
    }

    // Instruction implementations
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BRK()
    {
        // BRK causes a software interrupt
        pc++;
        memory.Write((ushort)(StackBase + s--), (byte)(pc >> 8));
        memory.Write((ushort)(StackBase + s--), (byte)(pc & 0xFF));
        memory.Write((ushort)(StackBase + s--), (byte)(p | FlagB));
        p |= FlagI;
        pc = memory.ReadWord(0xFFFE);
        cycles += 7;
        halted = true; // For now, halt on BRK
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDA_Immediate()
    {
        a = FetchByte();
        SetZN(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDA_ZeroPage()
    {
        a = ReadZeroPage();
        SetZN(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDA_ZeroPageX()
    {
        a = ReadZeroPageX();
        SetZN(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDA_Absolute()
    {
        a = ReadAbsolute();
        SetZN(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDA_AbsoluteX()
    {
        a = ReadAbsoluteX();
        SetZN(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDA_AbsoluteY()
    {
        a = ReadAbsoluteY();
        SetZN(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDA_IndirectX()
    {
        a = ReadIndirectX();
        SetZN(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LDA_IndirectY()
    {
        a = ReadIndirectY();
        SetZN(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void STA_ZeroPage()
    {
        WriteZeroPage(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void STA_ZeroPageX()
    {
        WriteZeroPageX(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void STA_Absolute()
    {
        WriteAbsolute(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void STA_AbsoluteX()
    {
        WriteAbsoluteX(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void STA_AbsoluteY()
    {
        WriteAbsoluteY(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void STA_IndirectX()
    {
        WriteIndirectX(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void STA_IndirectY()
    {
        WriteIndirectY(a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NOP()
    {
        cycles++; // Total 2 cycles (1 from FetchByte + 1 here)
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IllegalOpcode()
    {
        // For illegal opcodes, just halt execution
        halted = true;
    }
}