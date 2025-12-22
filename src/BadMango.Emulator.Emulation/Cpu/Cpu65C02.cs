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
    private readonly IMemory memory;
    private readonly OpcodeTable<Cpu65C02, Cpu65C02State> opcodeTable;

    private Cpu65C02State state; // CPU state including all registers, cycles, and halt state
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
        get => state.Halted;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        state = new Cpu65C02State
        {
            Registers = new Cpu65C02Registers
            {
                A = 0,
                X = 0,
                Y = 0,
                SP = 0xFD,
                P = (byte)(Cpu65C02Constants.FlagU | Cpu65C02Constants.FlagI), // Unused flag always set, interrupts disabled
                PC = memory.ReadWord(Cpu65C02Constants.ResetVector),
            },
            Cycles = 0,
            HaltReason = HaltState.None,
        };
        irqPending = false;
        nmiPending = false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Step()
    {
        ulong cyclesBefore = state.Cycles;

        // Check for pending interrupts at instruction boundary
        // Note: We check even when halted because WAI can resume on interrupts
        bool interruptProcessed = CheckInterrupts();
        if (interruptProcessed)
        {
            // Interrupt was processed, cycles were updated by ProcessInterrupt
            return (int)(state.Cycles - cyclesBefore);
        }

        if (Halted)
        {
            return 0;
        }

        byte opcode = FetchByte();

        // Execute opcode with state
        opcodeTable.Execute(opcode, this, memory, ref state);

        return (int)(state.Cycles - cyclesBefore);
    }

    /// <inheritdoc/>
    public void Execute(uint startAddress)
    {
        state.PC = (Word)startAddress;
        state.HaltReason = HaltState.None;

        while (!Halted)
        {
            Step();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cpu65C02Registers GetRegisters()
    {
        return state.Registers;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Cpu65C02State GetState()
    {
        return state;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetState(Cpu65C02State newState)
    {
        state = newState;
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
        byte value = memory.Read(state.PC++);
        state.Cycles++;
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
        if (state.HaltReason == HaltState.Stp)
        {
            return false;
        }

        // Check for NMI (non-maskable, highest priority)
        if (nmiPending)
        {
            nmiPending = false;

            // Resume from WAI if halted
            if (state.HaltReason == HaltState.Wai)
            {
                state.HaltReason = HaltState.None;
            }

            ProcessInterrupt(Cpu65C02Constants.NmiVector);
            return true;
        }

        // Check for IRQ (maskable by I flag)
        if (irqPending && (state.P & Cpu65C02Constants.FlagI) == 0)
        {
            irqPending = false;

            // Resume from WAI if halted
            if (state.HaltReason == HaltState.Wai)
            {
                state.HaltReason = HaltState.None;
            }

            ProcessInterrupt(Cpu65C02Constants.IrqVector);
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
    private void ProcessInterrupt(Addr vector)
    {
        // Push PC to stack (high byte first)
        memory.Write((Word)(Cpu65C02Constants.StackBase + state.SP--), (byte)(state.PC >> 8));
        memory.Write((Word)(Cpu65C02Constants.StackBase + state.SP--), (byte)(state.PC & 0xFF));

        // Push processor status (with B flag clear for hardware interrupts)
        memory.Write((Word)(Cpu65C02Constants.StackBase + state.SP--), (byte)(state.P & ~Cpu65C02Constants.FlagB));

        // Set I flag to disable further IRQs
        state.P |= Cpu65C02Constants.FlagI;

        // Load PC from interrupt vector
        state.PC = memory.ReadWord(vector);

        // Account for 7 cycles for interrupt processing
        state.Cycles += 7;
    }
}