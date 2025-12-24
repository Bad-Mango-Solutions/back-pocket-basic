// <copyright file="Cpu65C02.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using Core;

/// <summary>
/// WDC 65C02 CPU emulator with cycle-accurate execution.
/// </summary>
/// <remarks>
/// The 65C02 is an enhanced version of the 6502 with additional instructions,
/// addressing modes, and bug fixes. This implementation provides a minimal but
/// functional CPU core with basic instruction support.
/// Optimized with aggressive inlining for maximum performance.
/// </remarks>
public class Cpu65C02 : ICpu
{
    private readonly IMemory memory;
    private readonly OpcodeTable opcodeTable;

    private CpuState state; // CPU state including all registers, cycles, and halt state
    private bool irqPending;
    private bool nmiPending;
    private bool stopRequested;
    private IDebugStepListener? debugListener;

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
    public bool IsDebuggerAttached
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => debugListener is not null;
    }

    /// <inheritdoc/>
    public bool IsStopRequested
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => stopRequested;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        state = new()
        {
            Registers = new(true, memory.ReadWord(Cpu65C02Constants.ResetVector)),
            Cycles = 0,
            HaltReason = HaltState.None,
        };
        irqPending = false;
        nmiPending = false;
        stopRequested = false;
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

        // Capture state before execution for debug listener
        Addr pcBefore = state.Registers.PC.GetAddr();
        byte opcode = FetchByte();

        // Notify debug listener before execution
        if (debugListener is not null)
        {
            // Set up state for instruction tracking
            state.IsDebuggerAttached = true;
            state.Opcode = opcode;
            state.InstructionCycles = 1; // Opcode fetch cycle

            var beforeArgs = new DebugStepEventArgs
            {
                PC = pcBefore,
                Opcode = opcode,
                Registers = state.Registers,
                Cycles = cyclesBefore,
                Halted = false,
                HaltReason = HaltState.None,
            };
            debugListener.OnBeforeStep(in beforeArgs);
        }

        // Execute opcode with state
        opcodeTable.Execute(opcode, memory, ref state);

        // Notify debug listener after execution
        if (debugListener is not null)
        {
            var afterArgs = new DebugStepEventArgs
            {
                PC = pcBefore,
                Opcode = opcode,
                Instruction = state.Instruction,
                AddressingMode = state.AddressingMode,
                OperandSize = state.OperandSize,
                Operands = state.Operands,
                EffectiveAddress = state.EffectiveAddress,
                Registers = state.Registers,
                Cycles = state.Cycles,
                InstructionCycles = state.InstructionCycles,
                Halted = state.Halted,
                HaltReason = state.HaltReason,
            };
            debugListener.OnAfterStep(in afterArgs);

            // Reset debug state for next instruction
            state.IsDebuggerAttached = false;
            state.Instruction = CpuInstructions.None;
            state.AddressingMode = CpuAddressingModes.None;
            state.OperandSize = 0;
            state.EffectiveAddress = 0;
            state.InstructionCycles = 0;
        }

        return (int)(state.Cycles - cyclesBefore);
    }

    /// <inheritdoc/>
    public void Execute(uint startAddress)
    {
        state.Registers.PC.SetAddr(startAddress);
        state.HaltReason = HaltState.None;
        stopRequested = false;

        while (!Halted && !stopRequested)
        {
            Step();
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Registers GetRegisters()
    {
        return state.Registers;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref CpuState GetState()
    {
        return ref state;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetState(CpuState newState)
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

    /// <inheritdoc/>
    public void AttachDebugger(IDebugStepListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);
        debugListener = listener;
    }

    /// <inheritdoc/>
    public void DetachDebugger()
    {
        debugListener = null;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPC(Addr address)
    {
        state.Registers.PC.SetAddr(address);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Addr GetPC()
    {
        return state.Registers.PC.GetAddr();
    }

    /// <inheritdoc/>
    public void RequestStop()
    {
        stopRequested = true;
    }

    /// <inheritdoc/>
    public void ClearStopRequest()
    {
        stopRequested = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchByte()
    {
        var pc = state.Registers.PC.GetAddr();
        state.Registers.PC.Advance();
        byte value = memory.Read(pc);
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
        if (!irqPending || state.Registers.P.IsInterruptDisabled()) { return false; }

        irqPending = false;

        // Resume from WAI if halted
        if (state.HaltReason == HaltState.Wai)
        {
            state.HaltReason = HaltState.None;
        }

        ProcessInterrupt(Cpu65C02Constants.IrqVector);
        return true;
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
        var pc = state.Registers.PC.GetWord();

        // Push PC to stack (high byte first)
        memory.Write(state.PushByte(Cpu65C02Constants.StackBase), pc.HighByte());
        memory.Write(state.PushByte(Cpu65C02Constants.StackBase), pc.LowByte());

        // Push processor status (with B flag clear for hardware interrupts)
        memory.Write(state.PushByte(Cpu65C02Constants.StackBase), (byte)(state.Registers.P & ~ProcessorStatusFlags.B));

        // Set I flag to disable further IRQs
        state.Registers.P.SetInterruptDisable(true);

        // Load PC from interrupt vector
        state.Registers.PC.SetAddr(memory.ReadWord(vector));

        // Account for 7 cycles for interrupt processing
        state.Cycles += 7;
    }
}