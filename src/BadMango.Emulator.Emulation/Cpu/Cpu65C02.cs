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

    // Interrupt vectors
    private const Word NmiVector = 0xFFFA;
    private const Word ResetVector = 0xFFFC;
    private const Word IrqVector = 0xFFFE;

    // Stack base address
    private const Word StackBase = 0x0100;

    private readonly IMemory memory;
    private readonly OpcodeTable<Cpu65C02, Cpu65C02State> opcodeTable;

    private byte a;  // Accumulator
    private byte x;  // X register
    private byte y;  // Y register
    private byte s;  // Stack pointer
    private byte p;  // Processor status
    private Word pc; // Program counter
    private ulong cycles; // Total cycles executed
    private bool halted;
    private HaltState haltReason;
    private bool irqPending;
    private bool nmiPending;

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
        haltReason = HaltState.None;
        irqPending = false;
        nmiPending = false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Step()
    {
        // Check for pending interrupts at instruction boundary
        if (!halted)
        {
            bool interruptProcessed = CheckInterrupts();
            if (interruptProcessed)
            {
                // Interrupt was processed, return cycles consumed
                return 7; // Interrupt processing takes 7 cycles
            }
        }

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
        s = state.SP;
        p = state.P;
        halted = state.Halted;
        haltReason = state.HaltReason;

        return (int)(cycles - cyclesBefore);
    }

    /// <inheritdoc/>
    public void Execute(uint startAddress)
    {
        pc = (Word)startAddress;
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
            HaltReason = haltReason,
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
        haltReason = state.HaltReason;
    }

    /// <inheritdoc/>
    public void SignalIRQ()
    {
        irqPending = true;
    }

    /// <inheritdoc/>
    public void SignalNMI()
    {
        nmiPending = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchByte()
    {
        byte value = memory.Read(pc++);
        cycles++;
        return value;
    }

    /// <summary>
    /// Checks for pending interrupts and processes them if applicable.
    /// </summary>
    /// <returns>True if an interrupt was processed, false otherwise.</returns>
    /// <remarks>
    /// NMI has priority over IRQ. IRQ is maskable via the I flag.
    /// If the CPU is in WAI state, interrupts will resume execution.
    /// STP state cannot be resumed by interrupts.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckInterrupts()
    {
        // STP cannot be resumed by interrupts
        if (haltReason == HaltState.Stp)
        {
            return false;
        }

        // Check for NMI (non-maskable, highest priority)
        if (nmiPending)
        {
            nmiPending = false;

            // Resume from WAI if halted
            if (haltReason == HaltState.Wai)
            {
                halted = false;
                haltReason = HaltState.None;
            }

            ProcessInterrupt(NmiVector);
            return true;
        }

        // Check for IRQ (maskable by I flag)
        if (irqPending && (p & FlagI) == 0)
        {
            irqPending = false;

            // Resume from WAI if halted
            if (haltReason == HaltState.Wai)
            {
                halted = false;
                haltReason = HaltState.None;
            }

            ProcessInterrupt(IrqVector);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Processes an interrupt by pushing state to the stack and loading the interrupt vector.
    /// </summary>
    /// <param name="vector">The address of the interrupt vector to load.</param>
    /// <remarks>
    /// Interrupt processing:
    /// 1. Push PC high byte to stack
    /// 2. Push PC low byte to stack
    /// 3. Push processor status (with B flag clear) to stack
    /// 4. Set I flag to disable interrupts
    /// 5. Load PC from interrupt vector
    /// Total: 7 cycles (handled by caller).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessInterrupt(Word vector)
    {
        // Push PC to stack (high byte first)
        memory.Write((Word)(StackBase + s--), (byte)(pc >> 8));
        memory.Write((Word)(StackBase + s--), (byte)(pc & 0xFF));

        // Push processor status (with B flag clear for hardware interrupts)
        memory.Write((Word)(StackBase + s--), (byte)(p & ~FlagB));

        // Set I flag to disable further IRQs
        p |= FlagI;

        // Load PC from interrupt vector
        pc = memory.ReadWord(vector);

        // Note: Cycles are accounted for in Step(), not here
    }
}