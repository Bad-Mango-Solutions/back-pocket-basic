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
    private readonly OpcodeTable<Cpu65C02, Cpu65C02State> opcodeTable;

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

        // Create state snapshot after opcode fetch
        var state = GetState();
        opcodeTable.Execute(opcode, this, memory, ref state);

        // Update CPU state from modified state structure
        pc = state.PC;
        cycles = state.Cycles;
        a = state.A;
        x = state.X;
        y = state.Y;
        p = state.P;
        halted = state.Halted;

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
            SP = s,
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
                SP = s,
                P = p,
                PC = pc,
            },
            Cycles = cycles,
            Halted = halted,
        };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetState(Cpu65C02State state)
    {
        a = state.A;
        x = state.X;
        y = state.Y;
        s = state.SP;
        p = state.P;
        pc = state.PC;
        cycles = state.Cycles;
        halted = state.Halted;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchByte()
    {
        byte value = memory.Read(pc++);
        cycles++;
        return value;
    }
}