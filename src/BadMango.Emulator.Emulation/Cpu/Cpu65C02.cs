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

        // Peek ahead to get operand bytes for debug (does not cost cycles)
        byte operand1 = 0;
        byte operand2 = 0;
        byte operandLength = GetOperandLength(opcode);

        if (operandLength >= 1)
        {
            operand1 = memory.Read(pcBefore + 1);
        }

        if (operandLength >= 2)
        {
            operand2 = memory.Read(pcBefore + 2);
        }

        // Notify debug listener before execution
        if (debugListener is not null)
        {
            var beforeArgs = new DebugStepEventArgs
            {
                PC = pcBefore,
                Opcode = opcode,
                OperandLength = operandLength,
                Operand1 = operand1,
                Operand2 = operand2,
                Registers = state.Registers,
                Cycles = state.Cycles,
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
                OperandLength = operandLength,
                Operand1 = operand1,
                Operand2 = operand2,
                Registers = state.Registers,
                Cycles = state.Cycles,
                Halted = state.Halted,
                HaltReason = state.HaltReason,
            };
            debugListener.OnAfterStep(in afterArgs);
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

    /// <summary>
    /// Gets the operand length in bytes for the given opcode.
    /// </summary>
    /// <param name="opcode">The opcode to check.</param>
    /// <returns>The number of operand bytes (0, 1, or 2).</returns>
    /// <remarks>
    /// This method does not cost cycles - it's used for debug purposes only.
    /// It determines the instruction length based on addressing mode patterns.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetOperandLength(byte opcode)
    {
        // Immediate and zero-page modes have 1 byte operand
        // Absolute and indirect modes have 2 byte operand
        // Implied and accumulator modes have 0 byte operand
        // This is a simplified mapping based on 6502 opcode patterns
        int mode = opcode & 0x1F;
        int group = opcode & 0x03;

        // Special cases for specific opcodes
        return opcode switch
        {
            // BRK and RTI have 0 bytes (BRK technically has 1 signature byte, but PC skips it)
            0x00 => 0, // BRK
            0x40 => 0, // RTI
            0x60 => 0, // RTS

            // JSR and JMP absolute have 2 bytes
            0x20 => 2, // JSR abs
            0x4C => 2, // JMP abs
            0x6C => 2, // JMP (ind)

            // Branch instructions have 1 byte (relative offset)
            0x10 or 0x30 or 0x50 or 0x70 or 0x90 or 0xB0 or 0xD0 or 0xF0 or 0x80 => 1,

            // WAI and STP have 0 bytes
            0xCB => 0, // WAI
            0xDB => 0, // STP

            // Implied mode instructions have 0 bytes
            _ when (mode == 0x08 || mode == 0x18 || mode == 0x0A || mode == 0x1A) && group != 0x01 => 0,

            // Immediate mode (pattern: xxx01001) has 1 byte
            _ when (opcode & 0x1F) == 0x09 => 1,

            // Zero page modes have 1 byte
            _ when (opcode & 0x1F) is 0x05 or 0x15 or 0x04 or 0x14 or 0x06 or 0x16 => 1,

            // Absolute modes have 2 bytes
            _ when (opcode & 0x1F) is 0x0D or 0x1D or 0x0C or 0x1C or 0x0E or 0x1E or 0x19 => 2,

            // Indexed indirect and indirect indexed have 1 byte (zp operand)
            _ when (opcode & 0x1F) is 0x01 or 0x11 => 1,

            // For group 01 instructions (LDA, STA, etc.)
            _ when group == 0x01 => (opcode & 0x1C) switch
            {
                0x00 => 1, // (zp,X)
                0x04 => 1, // zp
                0x08 => 1, // #imm
                0x0C => 2, // abs
                0x10 => 1, // (zp),Y
                0x14 => 1, // zp,X
                0x18 => 2, // abs,Y
                0x1C => 2, // abs,X
                _ => 0,
            },

            // For group 10 instructions (ASL, ROL, etc.)
            _ when group == 0x02 => (opcode & 0x1C) switch
            {
                0x00 => 1, // #imm (LDX only)
                0x04 => 1, // zp
                0x08 => 0, // accumulator/implied
                0x0C => 2, // abs
                0x14 => 1, // zp,X or zp,Y
                0x1C => 2, // abs,X or abs,Y
                _ => 0,
            },

            // For group 00 instructions (BIT, JMP, STY, etc.)
            _ when group == 0x00 => (opcode & 0x1C) switch
            {
                0x00 => 1, // #imm
                0x04 => 1, // zp
                0x0C => 2, // abs
                0x14 => 1, // zp,X
                0x1C => 2, // abs,X
                _ => 0,
            },

            // Default to 0 for unknown/implied
            _ => 0,
        };
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