// <copyright file="Cpu65C02.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Signaling;

using Core;
using Core.Cpu;
using Core.Debugger;

/// <summary>
/// WDC 65C02 CPU emulator with cycle-accurate execution using bus-based memory access.
/// </summary>
/// <remarks>
/// <para>
/// The 65C02 is an enhanced version of the 6502 with additional instructions,
/// addressing modes, and bug fixes. This implementation provides a minimal but
/// functional CPU core with basic instruction support.
/// </para>
/// <para>
/// This CPU implementation uses the bus architecture for all memory operations.
/// The CPU computes intent; the bus enforces consequences (permissions, faults,
/// cycle counting for decomposed access).
/// </para>
/// <para>
/// Optimized with aggressive inlining for maximum performance.
/// </para>
/// </remarks>
public sealed class Cpu65C02 : CpuBase
{
    private readonly OpcodeTable opcodeTable;

    /// <summary>
    /// The concrete scheduler, when the event context supplies one, so the
    /// per-instruction <c>Advance</c> call is a direct (non-interface) call.
    /// <see langword="null"/> for other <see cref="IScheduler"/> implementations.
    /// </summary>
    private readonly Scheduler? concreteScheduler;

    /// <summary>
    /// The concrete signal bus, when the event context supplies one, so the CPU can
    /// test <see cref="SignalBus.AnyInterruptPending"/> instead of running the full
    /// interrupt check every instruction. <see langword="null"/> for other
    /// <see cref="Core.Interfaces.Signaling.ISignalBus"/> implementations.
    /// </summary>
    private readonly SignalBus? concreteSignals;

    /// <summary>
    /// The I-bit value sampled at the previous instruction boundary. This value is used
    /// for the next IRQ poll so CLI/SEI/PLP take effect one instruction later while RTI
    /// restores the value immediately.
    /// </summary>
    private bool interruptDisableSample;

    /// <summary>
    /// Set when the previous instruction modified I with the 65C02 delayed semantics and
    /// the new value must be sampled only after the next instruction completes.
    /// </summary>
    private bool interruptDisableChangePending;

    /// <summary>
    /// The deferred interrupt-disable value that becomes visible at the next instruction boundary
    /// after a CLI/SEI/PLP instruction completes.
    /// </summary>
    private bool interruptDisablePendingValue;

    private ITrapRegistry trapRegistry;

    /// <summary>
    /// The trap registry as its concrete type, when it is one, so the CPU can skip
    /// the per-instruction lookup while it is empty.
    /// </summary>
    private TrapRegistry? concreteTrapRegistry;

    /// <summary>
    /// <see langword="true"/> when <see cref="trapRegistry"/> is <see cref="NullTrapRegistry"/>,
    /// which never contains traps.
    /// </summary>
    private bool trapRegistryIsNull;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cpu65C02"/> class with an event context.
    /// </summary>
    /// <param name="context">The event context providing access to the memory bus, signal bus, and scheduler.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public Cpu65C02(IEventContext context)
        : base(context)
    {
        opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        concreteScheduler = context.Scheduler as Scheduler;
        concreteSignals = context.Signals as SignalBus;
        trapRegistry = NullTrapRegistry.Instance;
        trapRegistryIsNull = true;
    }

    /// <inheritdoc />
    public override CpuCapabilities Capabilities => CpuCapabilities.Base6502 | CpuCapabilities.Supports65C02Instructions;

    /// <summary>
    /// Gets or sets the trap registry for ROM routine interception.
    /// </summary>
    /// <value>
    /// The trap registry. Defaults to <see cref="NullTrapRegistry.Instance"/> when
    /// no custom registry is assigned, ensuring a non-null value is always available
    /// in the hot loop without requiring null checks.
    /// </value>
    /// <remarks>
    /// <para>
    /// When a trap registry is attached, the CPU will check for traps at each instruction
    /// fetch address before executing the opcode. If a trap is registered and its handler
    /// returns <see cref="TrapResult.Handled"/> = <see langword="true"/>, the CPU will
    /// perform an RTS to return to the calling code instead of executing the ROM instruction.
    /// </para>
    /// <para>
    /// This enables native implementations of ROM routines for performance optimization
    /// while maintaining compatibility with code that calls those routines.
    /// </para>
    /// <para>
    /// Setting this property to <see langword="null"/> resets it to the default
    /// <see cref="NullTrapRegistry.Instance"/>.
    /// </para>
    /// </remarks>
    public ITrapRegistry TrapRegistry
    {
        get => trapRegistry;
        set
        {
            trapRegistry = value ?? NullTrapRegistry.Instance;
            concreteTrapRegistry = trapRegistry as TrapRegistry;
            trapRegistryIsNull = trapRegistry is NullTrapRegistry;
        }
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        // Note: Scheduler reset is handled by Machine.Reset(), not the CPU.
        // The CPU only resets its own state (registers, halt reason, stop flag).
        Registers = new(true, Read16(Cpu65C02Constants.ResetVector));
        interruptDisableSample = Registers.P.IsInterruptDisabled();
        interruptDisablePendingValue = interruptDisableSample;
        interruptDisableChangePending = false;
        HaltReason = HaltState.None;
        ClearStopRequest();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override CpuStepResult Step()
    {
        // Clear TCU at the start of each instruction
        Registers.TCU = Cycle.Zero;

        // Determine the I value visible at this instruction boundary. Normally this is the
        // current register value; CLI/SEI/PLP defer their visible effect by one instruction
        // and therefore keep the previous boundary sample for one poll.
        bool irqMaskedAtBoundary = interruptDisableChangePending ? interruptDisableSample : Registers.P.IsInterruptDisabled();

        // Check for pending interrupts at instruction boundary using the signal bus.
        // With the concrete SignalBus, a single cached flag tells us whether an NMI
        // edge is pending or IRQ is asserted; if neither, CheckInterrupts would
        // return false with no side effects, so skip it.
        bool interruptProcessed = (concreteSignals is null || concreteSignals.AnyInterruptPending) && CheckInterrupts(irqMaskedAtBoundary);
        if (interruptProcessed)
        {
            // Interrupt entry updates I immediately; keep the latched boundary sample in sync.
            interruptDisableSample = Registers.P.IsInterruptDisabled();
            interruptDisableChangePending = false;

            // Interrupt was processed, TCU was updated by ProcessInterrupt
            // Advance the scheduler by the TCU value
            Cycle interruptCycles = Registers.TCU;
            AdvanceScheduler(interruptCycles);

            // Clear TCU after advancing scheduler
            Registers.TCU = Cycle.Zero;
            return new(CpuRunState.Running, interruptCycles);
        }

        if (Halted)
        {
            var haltedState = HaltReason switch
            {
                HaltState.Wai => CpuRunState.WaitingForInterrupt,
                HaltState.Stp => CpuRunState.Stopped,
                _ => CpuRunState.Halted,
            };
            return new(haltedState, Cycle.Zero);
        }

        // Capture state before execution for debug listener
        Addr pcBefore = Registers.PC.GetAddr();

        // Check for trap at this address before fetching the opcode.
        // First perform an O(1) address check; only if a trap exists at this
        // address do we proceed with the more expensive context resolution
        // and handler invocation.
        // The lookup is skipped entirely when the registry cannot contain traps
        // (NullTrapRegistry, or an empty concrete TrapRegistry).
        if (MayHaveTrapAt(pcBefore))
        {
            var trapResult = trapRegistry.TryExecute(pcBefore, this, Bus, EventContext);
            if (trapResult.Handled)
            {
                // Trap was handled - the handler determines how to return
                // Add cycles from the trap result
                Registers.TCU += trapResult.CyclesConsumed;

                // Handle return based on the method specified by the trap handler
                switch (trapResult.ReturnMethod)
                {
                    case TrapReturnMethod.Rts:
                        // Perform RTS: pull return address from stack and set PC
                        // RTS pulls low byte first, then high byte, and adds 1 to the address
                        Addr rtsLowAddr = PopByte(Cpu65C02Constants.StackBase);
                        byte rtsLowByte = Read8(rtsLowAddr);
                        Addr rtsHighAddr = PopByte(Cpu65C02Constants.StackBase);
                        byte rtsHighByte = Read8(rtsHighAddr);
                        ushort rtsReturnAddress = (ushort)((rtsHighByte << 8) | rtsLowByte);
                        Registers.PC.SetAddr((uint)(rtsReturnAddress + 1));

                        // Add RTS cycles (6 cycles for RTS)
                        Registers.TCU += 6;
                        break;

                    case TrapReturnMethod.Rti:
                        // Perform RTI: pull status, then return address from stack
                        // RTI pulls status first, then PC low, then PC high (no +1)
                        Addr statusAddr = PopByte(Cpu65C02Constants.StackBase);
                        byte status = Read8(statusAddr);
                        Registers.P = (ProcessorStatusFlags)status | ProcessorStatusFlags.R;
                        interruptDisableSample = Registers.P.IsInterruptDisabled();
                        interruptDisablePendingValue = interruptDisableSample;
                        interruptDisableChangePending = false;

                        Addr rtiLowAddr = PopByte(Cpu65C02Constants.StackBase);
                        byte rtiLowByte = Read8(rtiLowAddr);
                        Addr rtiHighAddr = PopByte(Cpu65C02Constants.StackBase);
                        byte rtiHighByte = Read8(rtiHighAddr);
                        ushort rtiReturnAddress = (ushort)((rtiHighByte << 8) | rtiLowByte);
                        Registers.PC.SetAddr(rtiReturnAddress);

                        // Add RTI cycles (6 cycles for RTI)
                        Registers.TCU += 6;
                        break;

                    case TrapReturnMethod.None:
                        // No automatic return - use ReturnAddress if specified,
                        // otherwise continue at current PC (for JMP targets or when
                        // the handler has already set PC)
                        if (trapResult.ReturnAddress.HasValue)
                        {
                            Registers.PC.SetAddr(trapResult.ReturnAddress.Value);
                        }

                        // No additional cycles for direct PC manipulation
                        break;
                }

                // Capture TCU before advancing scheduler (for return value)
                Cycle trapCycles = Registers.TCU;

                // Advance the scheduler by the TCU value
                AdvanceScheduler(trapCycles);

                // Clear TCU after advancing scheduler
                Registers.TCU = Cycle.Zero;

                return new(CpuRunState.Running, trapCycles);
            }
        }

        byte opcode = FetchByte(); // Advances TCU by 1 for the opcode fetch

        // Notify debug listener before execution
        if (DebugListener is not null)
        {
            // Initialize trace for this instruction
            var opcodeBuffer = default(OpcodeBuffer);
            opcodeBuffer[0] = opcode;
            Trace = new(
                StartPC: pcBefore,
                OpCode: opcodeBuffer,
                Instruction: CpuInstructions.None,
                AddressingMode: CpuAddressingModes.None,
                OperandSize: 0,
                Operands: default,
                EffectiveAddress: 0,
                StartCycle: EventContext.Now,
                InstructionCycles: Cycle.Zero); // TCU is the source of truth for cycles

            var beforeArgs = new DebugStepEventArgs
            {
                PC = pcBefore,
                Opcode = opcode,
                Registers = Registers,
                Cycles = EventContext.Now.Value,
                Halted = false,
                HaltReason = HaltState.None,
            };
            DebugListener.OnBeforeStep(in beforeArgs);
        }

        // Execute opcode - handlers now access memory through this CPU instance
        opcodeTable.Execute(opcode, this);

        // Latch the I state that will be sampled at the next instruction boundary.
        // CLI/SEI/PLP change I immediately, but the next IRQ poll still uses the previous
        // boundary sample. The new value becomes the sample only after the following instruction.
        // RTI restores I immediately and therefore updates the sample right away.
        bool currentI = Registers.P.IsInterruptDisabled();
        if (opcode == 0x40)
        {
            interruptDisableSample = currentI;
            interruptDisablePendingValue = currentI;
            interruptDisableChangePending = false;
        }
        else if (opcode is 0x58 or 0x78 or 0x28)
        {
            // Keep this boundary value for the next IRQ poll; the pending new value is
            // applied after the following instruction.
            interruptDisableSample = irqMaskedAtBoundary;
            interruptDisablePendingValue = currentI;
            interruptDisableChangePending = true;
        }
        else if (interruptDisableChangePending)
        {
            interruptDisableSample = interruptDisablePendingValue;
            interruptDisablePendingValue = currentI;
            interruptDisableChangePending = false;
        }
        else
        {
            interruptDisableSample = currentI;
            interruptDisablePendingValue = currentI;
            interruptDisableChangePending = false;
        }

        // Capture TCU before advancing scheduler (for return value)
        Cycle instructionCycles = Registers.TCU;

        // Advance the scheduler by the TCU value (total cycles for this instruction)
        AdvanceScheduler(instructionCycles);

        // Notify debug listener after execution
        if (DebugListener is not null)
        {
            // Apply TCU to the trace (this is a debug-only operation)
            Trace = Trace with { InstructionCycles = instructionCycles };

            var afterArgs = new DebugStepEventArgs
            {
                PC = pcBefore,
                Opcode = opcode,
                Instruction = Trace.Instruction,
                AddressingMode = Trace.AddressingMode,
                OperandSize = Trace.OperandSize,
                Operands = Trace.Operands,
                EffectiveAddress = Trace.EffectiveAddress,
                Registers = Registers,
                Cycles = EventContext.Now.Value,
                InstructionCycles = (byte)instructionCycles.Value,
                Halted = Halted,
                HaltReason = HaltReason,
            };
            DebugListener.OnAfterStep(in afterArgs);
        }

        // Clear TCU after advancing scheduler (cycles have been committed)
        Registers.TCU = Cycle.Zero;

        // Determine the run state after execution
        CpuRunState runState = HaltReason switch
        {
            HaltState.None => CpuRunState.Running,
            HaltState.Wai => CpuRunState.WaitingForInterrupt,
            HaltState.Stp => CpuRunState.Stopped,
            _ => CpuRunState.Halted,
        };

        return new(runState, instructionCycles);
    }

    // ─── Private Helper Methods ─────────────────────────────────────────
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AdvanceScheduler(Cycle cycles)
    {
        if (concreteScheduler is not null)
        {
            concreteScheduler.Advance(cycles);
        }
        else
        {
            EventContext.Scheduler.Advance(cycles);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool MayHaveTrapAt(Addr address)
    {
        if (concreteTrapRegistry is not null)
        {
            return !concreteTrapRegistry.IsEmpty && concreteTrapRegistry.ContainsAddress(address);
        }

        return !trapRegistryIsNull && trapRegistry.ContainsAddress(address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchByte()
    {
        var pc = Registers.PC.GetAddr();
        Registers.PC.Advance();
        byte value = InstructionFetch8(pc);

        // Advance TCU for the opcode fetch cycle
        Registers.TCU += 1;

        return value;
    }

    /// <summary>
    /// Fetches a byte from memory as part of instruction fetch.
    /// </summary>
    /// <param name="address">The address to fetch from.</param>
    /// <returns>The byte value at the address.</returns>
    /// <remarks>
    /// This method uses <see cref="AccessIntent.InstructionFetch"/> to allow the bus
    /// to differentiate between data reads and instruction fetches. This enables
    /// features like trap interception and NX (no-execute) enforcement.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte InstructionFetch8(Addr address)
    {
        var access = CreateInstructionFetchAccess(address, 8);
        var result = Bus.TryRead8(access);

        if (result.Failed)
        {
            // Handle bus fault - for now, return 0xFF (floating bus) and halt on unmapped
            if (result.Fault.Kind == FaultKind.Unmapped)
            {
                HaltReason = HaltState.Stp;
            }

            return 0xFF;
        }

        return result.Value;
    }

    /// <summary>
    /// Checks for pending interrupts and processes them if applicable.
    /// Uses the signal bus to poll interrupt lines.
    /// </summary>
    /// <returns>True if an interrupt was processed, false otherwise.</returns>
    /// <remarks>
    /// NMI has priority over IRQ. IRQ is maskable via the I flag.
    /// If the CPU is in WAI state, interrupts will resume execution.
    /// STP state cannot be resumed by interrupts.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CheckInterrupts(bool irqMaskedAtBoundary)
    {
        // STP cannot be resumed by interrupts
        if (HaltReason == HaltState.Stp)
        {
            return false;
        }

        // Check for NMI (non-maskable, highest priority, edge-triggered)
        if (Signals.ConsumeNmiEdge())
        {
            // Resume from WAI if halted
            if (HaltReason == HaltState.Wai)
            {
                HaltReason = HaltState.None;
            }

            ProcessInterrupt(Cpu65C02Constants.NmiVector);
            return true;
        }

        // Check for IRQ (maskable by I flag, level-triggered)
        if (Signals.IsAsserted(SignalLine.IRQ))
        {
            // WAI clears the halt on any asserted IRQ, but only vectors when I is clear.
            if (HaltReason == HaltState.Wai)
            {
                HaltReason = HaltState.None;

                if (irqMaskedAtBoundary)
                {
                    return false;
                }
            }
            else if (irqMaskedAtBoundary)
            {
                return false;
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
        var pc = Registers.PC.GetWord();

        // Push PC to stack (high byte first)
        Write8(PushByte(Cpu65C02Constants.StackBase), pc.HighByte());
        Write8(PushByte(Cpu65C02Constants.StackBase), pc.LowByte());

        // Push processor status (with B flag clear for hardware interrupts)
        Write8(PushByte(Cpu65C02Constants.StackBase), (byte)((Registers.P | ProcessorStatusFlags.R) & ~ProcessorStatusFlags.B));

        // Set I flag to disable further IRQs
        Registers.P.SetInterruptDisable(true);

        // 65C02: clear D flag on interrupt entry (NMOS 6502 leaves D unchanged)
        Registers.P.SetDecimalMode(false);

        // Load PC from interrupt vector
        Registers.PC.SetAddr(Read16(vector));

        // Account for 7 cycles for interrupt processing
        Registers.TCU += 7;
    }
}