// <copyright file="Cpu65C02.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu; // for ProcessorStatusFlags in reset
using BadMango.Emulator.Core.Debugger;
using BadMango.Emulator.Core.Signaling;

using Core;
using Core.Cpu;

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
/// Hot loop is optimized with AggressiveInlining, ref Registers (zero-copy), precomputed
/// routing tables in page0 targets, and 16-bit masking for fidelity. Delegate table dispatch
/// via composition provides flexibility.
/// </para>
/// <para>
/// Future 65816 base: evolve Registers with banks (PBR/DBR), direct page, E/M/X flags;
/// handlers can branch on mode or use different tables; addressing returns 24-bit capable Addrs.
/// Speculative 65832: extend bus layers for MMU/privilege (hypervisor/kernel/user separation via
/// priority/perm layers), secure models (NX, capability-based via intents).
/// </para>
/// </remarks>

/// <para>
/// Important context from Apple IIe ROM and ProDOS analysis (used to guide every host-only fix):
/// The Enhanced IIe reset vector at $FFFC/$FFFD points to the handler at $FA62. After the
/// standard CLD + stack setup and warm/cold test, the cold path issues a long series of direct
/// STA $C0xx instructions to place the machine in a known state (80STORE off so that PAGE2 and
/// RAMRD/RAMWRT control auxiliary memory, main RAM read and write selected, slot CX ROM visible
/// rather than internal, main zero page, TEXT mode, PAGE1, LORES, full screen). It then calls
/// the HOME routine (entry near $FC58) whose job is to clear the 40-column (or 80-column when
/// enabled) text page at $0400-$07FF by writing appropriate space/inverse characters using
/// computed screen-base addresses kept in zero page ($28/$29) and the current window parameters
/// ($20-$23). Only after this does the code build a slot pointer in zero page, walk from high
/// slots downward with indirect loads (LDA (00),Y) to test the three signature bytes, and on
/// success perform the indirect jump (6C 00 00) to the slot ROM entry ($Cn00).
/// </para>
/// <para>
/// The Disk II P6 ROM (256 bytes at $C600) is itself full of indexed I/O ($C08x,X where X holds
/// a value derived from the slot number), tight self-clocking GCR read loops that poll status
/// bits with BPL-style waits, checksum verification, and a final JMP $0801 into the boot block
/// that was read from track 0 sector 0. ProDOS disks begin that boot block with the distinctive
/// 01 38 B0 03 ... bytes; the loader then uses more zero-page pointers and indexed/indirect
/// operations to bring the rest of the operating system into memory.
/// </para>
/// <para>
/// The Apple IIe designers used computed and indexed addressing, zero-page indirection, and
/// self-modifying tricks extensively because both ROM space and CPU cycles were extremely
/// precious on a 1 MHz 65C02 with only 64 KB (or 128 KB with the auxiliary card). The host
/// implementation must therefore treat every address calculation as a full 16-bit value,
/// keep the stack pointer strictly within an 8-bit page-1 range, make soft-switch writes
/// (especially to $C006/$C007 for INTCXROM) affect the very next instruction fetch, and
/// deliver disk bytes and VBlank events with cycle-accurate timing so that the polling loops
/// inside the P6 ROM see data when they expect it.
/// </para>
/// <para>
/// Observed symptoms such as the program counter landing on data bytes (producing opcode
/// $DB = STP or floods of $00 = BRK), the text page remaining entirely zero after HOME should
/// have run, or the Disk II signature bytes being invisible during slot scan, are therefore
/// almost always caused by a missing 16-bit mask, a layering decision that has not yet been
/// applied, a stack pull that did not account for the documented RTS +1 behavior, or a
/// scheduler event that fired (or failed to fire) at the wrong moment relative to the CPU's
/// accumulated TCU.
/// </para>
public sealed class Cpu65C02 : CpuBase
{
    private readonly OpcodeTable opcodeTable;
    private ITrapRegistry trapRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cpu65C02"/> class with an event context.
    /// </summary>
    /// <param name="context">The event context providing access to the memory bus, signal bus, and scheduler.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public Cpu65C02(IEventContext context)
        : base(context)
    {
        opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
        trapRegistry = NullTrapRegistry.Instance;
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
        set => trapRegistry = value ?? NullTrapRegistry.Instance;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        // Note: Scheduler reset is handled by Machine.Reset(), not the CPU.
        // The CPU only resets its own state (registers, halt reason, stop flag).
        // Follow true hard reset sequence: explicitly read the reset vector from $FFFC (low) / $FFFD (high)
        // so PC is set to the address from the vector at FFFC, then execution begins at that handler address.
        // This ensures the ROM reset code (including HOME clear screen) runs for full loader fidelity.
        byte lo = Read8(Cpu65C02Constants.ResetVector);
        byte hi = Read8(Cpu65C02Constants.ResetVector + 1);
        Word vector = (Word)((hi << 8) | lo);
        Registers = new(true, vector);

        // Standard 65C02 reset flags: I=1, D=0 (clear decimal).
        // Hardware reset does not push to the stack; SP remains at the emulator cold-start
        // value ($FF) established by Registers(bool compat, Word resetVector).
        Registers.P |= ProcessorStatusFlags.I;
        Registers.P &= ~ProcessorStatusFlags.D;

        HaltReason = HaltState.None;
        ClearStopRequest();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override CpuStepResult Step()
    {
        // Use ref local for hot path perf (avoids repeated property access; enables future 65816 ref struct state sharing).
        ref var regs = ref Registers;

        // Apple II 65C02 is always in emulation mode; native 16/32-bit register widths must never apply.
        regs.E = true;
        regs.CP = true;

        // Clear TCU at the start of each instruction
        regs.TCU = Cycle.Zero;

        // Check for pending interrupts at instruction boundary using the signal bus
        bool interruptProcessed = CheckInterrupts();
        if (interruptProcessed)
        {
            // Interrupt was processed, TCU was updated by ProcessInterrupt
            // Advance the scheduler by the TCU value
            Cycle interruptCycles = regs.TCU;
            EventContext.Scheduler.Advance(interruptCycles);

            // Clear TCU after advancing scheduler
            regs.TCU = Cycle.Zero;
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
        Addr pcBefore = regs.PC.GetAddr() & 0xFFFF;  // Defensive 16-bit mask for 65C02 (flex for 65816 banks later)

        // Check for trap at this address before fetching the opcode.
        // First perform an O(1) address check; only if a trap exists at this
        // address do we proceed with the more expensive context resolution
        // and handler invocation.
        if (trapRegistry.ContainsAddress(pcBefore))
        {
            var trapResult = trapRegistry.TryExecute(pcBefore, this, Bus, EventContext);
            if (trapResult.Handled)
            {
                // Trap was handled - the handler determines how to return
                // Add cycles from the trap result
                regs.TCU += trapResult.CyclesConsumed;

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
                        regs.PC.SetAddr((uint)((rtsReturnAddress + 1) & 0xFFFF));

                        // Add RTS cycles (6 cycles for RTS)
                        regs.TCU += 6;
                        break;

                    case TrapReturnMethod.Rti:
                        // Perform RTI: pull status, then return address from stack
                        // RTI pulls status first, then PC low, then PC high (no +1)
                        Addr statusAddr = PopByte(Cpu65C02Constants.StackBase);
                        byte status = Read8(statusAddr);
                        regs.P = (ProcessorStatusFlags)status;

                        Addr rtiLowAddr = PopByte(Cpu65C02Constants.StackBase);
                        byte rtiLowByte = Read8(rtiLowAddr);
                        Addr rtiHighAddr = PopByte(Cpu65C02Constants.StackBase);
                        byte rtiHighByte = Read8(rtiHighAddr);
                        ushort rtiReturnAddress = (ushort)((rtiHighByte << 8) | rtiLowByte);
                        regs.PC.SetAddr((uint)(rtiReturnAddress & 0xFFFF));

                        // Add RTI cycles (6 cycles for RTI)
                        regs.TCU += 6;
                        break;

                    case TrapReturnMethod.None:
                        // No automatic return - use ReturnAddress if specified,
                        // otherwise continue at current PC (for JMP targets or when
                        // the handler has already set PC)
                        if (trapResult.ReturnAddress.HasValue)
                        {
                            regs.PC.SetAddr((uint)(trapResult.ReturnAddress.Value & 0xFFFF));
                        }

                        // No additional cycles for direct PC manipulation
                        break;
                }

                // Capture TCU before advancing scheduler (for return value)
                Cycle trapCycles = regs.TCU;

                // Advance the scheduler by the TCU value
                EventContext.Scheduler.Advance(trapCycles);

                // Clear TCU after advancing scheduler
                regs.TCU = Cycle.Zero;

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
                Registers = regs,
                Cycles = EventContext.Now.Value,
                Halted = false,
                HaltReason = HaltState.None,
            };
            DebugListener.OnBeforeStep(in beforeArgs);
        }

        // Execute opcode - handlers now access memory through this CPU instance
        opcodeTable.Execute(opcode, this);

        // Capture TCU before advancing scheduler (for return value)
        Cycle instructionCycles = regs.TCU;

        // Advance the scheduler by the TCU value (total cycles for this instruction)
        EventContext.Scheduler.Advance(instructionCycles);

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
                Registers = regs,
                Cycles = EventContext.Now.Value,
                InstructionCycles = (byte)instructionCycles.Value,
                Halted = Halted,
                HaltReason = HaltReason,
            };
            DebugListener.OnAfterStep(in afterArgs);
        }

        // Clear TCU after advancing scheduler (cycles have been committed)
        regs.TCU = Cycle.Zero;

        // Defensive 16-bit PC mask for 65C02 hot loop fidelity (ROM relies on exact 16-bit computed/indexed
        // addresses; prevents drift to data bytes/DB/STP from prior bugs in JMP/RTS/calc). For 65816 base
        // this will evolve to bank-aware masking (PBR etc.).
        regs.PC.SetAddr(regs.PC.GetAddr() & 0xFFFF);

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

        if (result.Value == 0xDB)
        {
            // Diagnostic: log when $DB (STP data byte) is fetched as opcode during boot investigation.
            // This should not happen in correct execution; indicates PC landed on data in ROM.
            System.Console.Error.WriteLine($"[DB FETCH] addr=${address:X4} value=DB (likely data byte fetched as opcode)");
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
    private bool CheckInterrupts()
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
        if (Signals.IsAsserted(SignalLine.IRQ) && !Registers.P.IsInterruptDisabled())
        {
            // Resume from WAI if halted
            if (HaltReason == HaltState.Wai)
            {
                HaltReason = HaltState.None;
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
        Write8(PushByte(Cpu65C02Constants.StackBase), (byte)(Registers.P & ~ProcessorStatusFlags.B));

        // Set I flag to disable further IRQs
        Registers.P.SetInterruptDisable(true);

        // 65C02 clears D flag on all interrupt entry paths (IRQ, NMI, BRK, reset)
        Registers.P &= ~ProcessorStatusFlags.D;

        // Load PC from interrupt vector
        Registers.PC.SetAddr(Read16(vector));

        // Account for 7 cycles for interrupt processing
        Registers.TCU += 7;
    }
}