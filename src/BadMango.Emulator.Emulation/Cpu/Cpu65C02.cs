// <copyright file="Cpu65C02.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;

using Core;
using Core.Cpu;
using Core.Debugger;
using Core.Interfaces;
using Core.Interfaces.Cpu;
using Core.Interfaces.Debugging;

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
public class Cpu65C02 : ICpu
{
    /// <summary>
    /// The source ID used for bus access tracing.
    /// </summary>
    private const int CpuSourceId = 0;

    private readonly IEventContext context;
    private readonly IMemoryBus bus;
    private readonly ISignalBus signals;
    private readonly OpcodeTable opcodeTable;

    private Registers registers; // CPU registers (directly stored, no CpuState wrapper)
    private HaltState haltReason; // Halt state managed directly by CPU
    private InstructionTrace trace; // Instruction trace for debug information
    private bool stopRequested;
    private IDebugStepListener? debugListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="Cpu65C02"/> class with an event context.
    /// </summary>
    /// <param name="context">The event context providing access to the memory bus, signal bus, and scheduler.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public Cpu65C02(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        this.context = context;
        bus = context.Bus;
        signals = context.Signals;
        opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Cpu65C02"/> class with an IMemory interface.
    /// </summary>
    /// <param name="memory">The memory interface for the CPU.</param>
    /// <remarks>
    /// This constructor is provided for backward compatibility with existing code that uses IMemory.
    /// It creates a MemoryBusAdapter internally to route memory operations through the bus architecture.
    /// For new code, prefer using the <see cref="Cpu65C02(IEventContext)"/> constructor.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="memory"/> is null.</exception>
    [Obsolete("Use the constructor that accepts IEventContext for bus-based memory access.")]
    public Cpu65C02(IMemory memory)
    {
        ArgumentNullException.ThrowIfNull(memory, nameof(memory));

        // Create a minimal bus infrastructure for backward compatibility
        var mainBus = new MainBus();
        var scheduler = new Scheduler();
        var signalBus = new SignalBus();

        // Map the entire 64KB address space to a RAM target that wraps the IMemory
        var adapter = new MemoryToTargetAdapter(memory);
        mainBus.MapPageRange(0, 16, 0, RegionTag.Ram, PagePerms.ReadWrite, TargetCaps.None, adapter, 0);

        context = new EventContext(scheduler, signalBus, mainBus);
        bus = mainBus;
        signals = signalBus;
        opcodeTable = Cpu65C02OpcodeTableBuilder.Build();
    }

    /// <summary>
    /// Gets the event context providing access to bus, signals, and scheduler.
    /// </summary>
    /// <value>The event context for this CPU.</value>
    public IEventContext EventContext => context;

    /// <inheritdoc />
    public CpuCapabilities Capabilities => CpuCapabilities.Base6502 | CpuCapabilities.Supports65C02Instructions;

    /// <inheritdoc />
    public ref Registers Registers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref registers;
    }

    /// <inheritdoc/>
    public bool Halted
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => haltReason != HaltState.None;
    }

    /// <inheritdoc/>
    public HaltState HaltReason
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => haltReason;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => haltReason = value;
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
    public InstructionTrace Trace
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => trace;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => trace = value;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        // Reset the scheduler's timing to cycle 0
        context.Scheduler.Reset();

        registers = new(true, Read16(Cpu65C02Constants.ResetVector));
        haltReason = HaltState.None;
        stopRequested = false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CpuStepResult Step()
    {
        // Clear TCU at the start of each instruction
        registers.TCU = Cycle.Zero;

        // Check for pending interrupts at instruction boundary using the signal bus
        bool interruptProcessed = CheckInterrupts();
        if (interruptProcessed)
        {
            // Interrupt was processed, TCU was updated by ProcessInterrupt
            // Advance the scheduler by the TCU value
            Cycle interruptCycles = registers.TCU;
            context.Scheduler.Advance(interruptCycles);

            // Clear TCU after advancing scheduler
            registers.TCU = Cycle.Zero;
            return new CpuStepResult(CpuRunState.Running, interruptCycles);
        }

        if (Halted)
        {
            var haltedState = haltReason switch
            {
                HaltState.Wai => CpuRunState.WaitingForInterrupt,
                HaltState.Stp => CpuRunState.Stopped,
                _ => CpuRunState.Halted,
            };
            return new CpuStepResult(haltedState, Cycle.Zero);
        }

        // Capture state before execution for debug listener
        Addr pcBefore = registers.PC.GetAddr();
        byte opcode = FetchByte(); // Advances TCU by 1 for the opcode fetch

        // Notify debug listener before execution
        if (debugListener is not null)
        {
            // Initialize trace for this instruction
            var opcodeBuffer = new OpcodeBuffer();
            opcodeBuffer[0] = opcode;
            trace = new InstructionTrace(
                StartPC: pcBefore,
                OpCode: opcodeBuffer,
                Instruction: CpuInstructions.None,
                AddressingMode: CpuAddressingModes.None,
                OperandSize: 0,
                Operands: default,
                EffectiveAddress: 0,
                StartCycle: context.Now,
                InstructionCycles: Cycle.Zero); // TCU is the source of truth for cycles

            var beforeArgs = new DebugStepEventArgs
            {
                PC = pcBefore,
                Opcode = opcode,
                Registers = registers,
                Cycles = context.Now.Value,
                Halted = false,
                HaltReason = HaltState.None,
            };
            debugListener.OnBeforeStep(in beforeArgs);
        }

        // Execute opcode - handlers now access memory through this CPU instance
        opcodeTable.Execute(opcode, this);

        // Capture TCU before advancing scheduler (for return value)
        Cycle instructionCycles = registers.TCU;

        // Advance the scheduler by the TCU value (total cycles for this instruction)
        context.Scheduler.Advance(instructionCycles);

        // Notify debug listener after execution
        if (debugListener is not null)
        {
            // Apply TCU to the trace (this is a debug-only operation)
            trace = trace with { InstructionCycles = instructionCycles };

            var afterArgs = new DebugStepEventArgs
            {
                PC = pcBefore,
                Opcode = opcode,
                Instruction = trace.Instruction,
                AddressingMode = trace.AddressingMode,
                OperandSize = trace.OperandSize,
                Operands = trace.Operands,
                EffectiveAddress = trace.EffectiveAddress,
                Registers = registers,
                Cycles = context.Now.Value,
                InstructionCycles = (byte)instructionCycles.Value,
                Halted = Halted,
                HaltReason = haltReason,
            };
            debugListener.OnAfterStep(in afterArgs);
        }

        // Clear TCU after advancing scheduler (cycles have been committed)
        registers.TCU = Cycle.Zero;

        // Determine the run state after execution
        CpuRunState runState = haltReason switch
        {
            HaltState.None => CpuRunState.Running,
            HaltState.Wai => CpuRunState.WaitingForInterrupt,
            HaltState.Stp => CpuRunState.Stopped,
            _ => CpuRunState.Halted,
        };

        return new CpuStepResult(runState, instructionCycles);
    }

    /// <inheritdoc/>
    public void Execute(uint startAddress)
    {
        registers.PC.SetAddr(startAddress);
        haltReason = HaltState.None;
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
        return registers;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong GetCycles()
    {
        // Return the scheduler's current cycle count plus any pending TCU cycles.
        // TCU holds cycles accumulated during instruction execution.
        // When called via Step(), TCU will have already been cleared for the next instruction,
        // so this effectively returns scheduler.Now.
        // When called after a direct handler invocation (unit test pattern), TCU holds
        // the cycles that haven't been flushed to the scheduler yet.
        return context.Now.Value + registers.TCU.Value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCycles(ulong cycles)
    {
        // If the new cycle count is greater than the current scheduler time, advance the scheduler to match
        // This maintains backward compatibility with code that sets cycles via state
        if (cycles > context.Now.Value)
        {
            context.Scheduler.Advance(new Cycle(cycles - context.Now.Value));
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CpuState GetState()
    {
        return new CpuState
        {
            Registers = registers,
            Cycles = GetCycles(),
        };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetState(CpuState state)
    {
        registers = state.Registers;
        SetCycles(state.Cycles);
    }

    /// <inheritdoc/>
    public void SignalIRQ()
    {
        signals.Assert(SignalLine.IRQ, CpuSourceId, context.Now);
    }

    /// <inheritdoc/>
    public void SignalNMI()
    {
        signals.Assert(SignalLine.NMI, CpuSourceId, context.Now);
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
        registers.PC.SetAddr(address);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Addr GetPC()
    {
        return registers.PC.GetAddr();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RequestStop()
    {
        stopRequested = true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearStopRequest()
    {
        stopRequested = false;
    }

    // ─── Memory Access Methods (Bus-based) ──────────────────────────────

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(Addr address)
    {
        var access = CreateReadAccess(address, 8);
        var result = bus.TryRead8(access);

        if (result.Failed)
        {
            // Handle bus fault - for now, return 0xFF (floating bus) and halt on unmapped
            if (result.Fault.Kind == FaultKind.Unmapped)
            {
                haltReason = HaltState.Stp;
            }

            return 0xFF;
        }

        return result.Value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(Addr address, byte value)
    {
        var access = CreateWriteAccess(address, 8);
        var result = bus.TryWrite8(access, value);

        if (result.Failed && result.Fault.Kind == FaultKind.Unmapped)
        {
            // Write to unmapped page - silently ignore for write operations
            // (some systems have write-only regions or ignore writes to ROM)
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Word Read16(Addr address)
    {
        var access = CreateReadAccess(address, 16);
        var result = bus.TryRead16(access);

        if (result.Failed)
        {
            if (result.Fault.Kind == FaultKind.Unmapped)
            {
                haltReason = HaltState.Stp;
            }

            return 0xFFFF;
        }

        return result.Value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write16(Addr address, Word value)
    {
        var access = CreateWriteAccess(address, 16);
        bus.TryWrite16(access, value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DWord ReadValue(Addr address, byte sizeInBits)
    {
        return sizeInBits switch
        {
            8 => Read8(address),
            16 => Read16(address),
            32 => Read32(address),
            _ => throw new ArgumentException($"Invalid size: {sizeInBits}. Must be 8, 16, or 32.", nameof(sizeInBits)),
        };
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteValue(Addr address, DWord value, byte sizeInBits)
    {
        switch (sizeInBits)
        {
            case 8:
                Write8(address, (byte)(value & 0xFF));
                break;
            case 16:
                Write16(address, (Word)(value & 0xFFFF));
                break;
            case 32:
                Write32(address, value);
                break;
            default:
                throw new ArgumentException($"Invalid size: {sizeInBits}. Must be 8, 16, or 32.", nameof(sizeInBits));
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Addr PushByte(Addr stackBase = 0)
    {
        var old = registers.SP.stack;
        registers.SP.stack--;
        return stackBase + old;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Addr PopByte(Addr stackBase = 0)
    {
        var old = registers.SP.stack + 1;
        registers.SP.stack++;
        return stackBase + old;
    }

    // ─── Private Helper Methods ─────────────────────────────────────────

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte FetchByte()
    {
        var pc = registers.PC.GetAddr();
        registers.PC.Advance();
        byte value = Read8(pc);

        // Advance TCU for the opcode fetch cycle
        registers.TCU += 1;

        return value;
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
        if (haltReason == HaltState.Stp)
        {
            return false;
        }

        // Check for NMI (non-maskable, highest priority, edge-triggered)
        if (signals.ConsumeNmiEdge())
        {
            // Resume from WAI if halted
            if (haltReason == HaltState.Wai)
            {
                haltReason = HaltState.None;
            }

            ProcessInterrupt(Cpu65C02Constants.NmiVector);
            return true;
        }

        // Check for IRQ (maskable by I flag, level-triggered)
        if (signals.IsAsserted(SignalLine.IRQ) && !registers.P.IsInterruptDisabled())
        {
            // Resume from WAI if halted
            if (haltReason == HaltState.Wai)
            {
                haltReason = HaltState.None;
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
        var pc = registers.PC.GetWord();

        // Push PC to stack (high byte first)
        Write8(PushByte(Cpu65C02Constants.StackBase), pc.HighByte());
        Write8(PushByte(Cpu65C02Constants.StackBase), pc.LowByte());

        // Push processor status (with B flag clear for hardware interrupts)
        Write8(PushByte(Cpu65C02Constants.StackBase), (byte)(registers.P & ~ProcessorStatusFlags.B));

        // Set I flag to disable further IRQs
        registers.P.SetInterruptDisable(true);

        // Load PC from interrupt vector
        registers.PC.SetAddr(Read16(vector));

        // Account for 7 cycles for interrupt processing
        registers.TCU += 7;
    }

    /// <summary>
    /// Reads a 32-bit value from memory.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DWord Read32(Addr address)
    {
        var access = CreateReadAccess(address, 32);
        var result = bus.TryRead32(access);

        if (result.Failed)
        {
            if (result.Fault.Kind == FaultKind.Unmapped)
            {
                haltReason = HaltState.Stp;
            }

            return 0xFFFFFFFF;
        }

        return result.Value;
    }

    /// <summary>
    /// Writes a 32-bit value to memory.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Write32(Addr address, DWord value)
    {
        var access = CreateWriteAccess(address, 32);
        bus.TryWrite32(access, value);
    }

    /// <summary>
    /// Creates a bus access context for read operations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BusAccess CreateReadAccess(Addr address, byte widthBits)
    {
        return new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: widthBits,
            Mode: BusAccessMode.Decomposed, // 65C02 uses decomposed mode for accurate cycle counting
            EmulationFlag: true, // 65C02 is always in emulation mode
            Intent: AccessIntent.DataRead,
            SourceId: CpuSourceId,
            Cycle: context.Now,
            Flags: AccessFlags.None);
    }

    /// <summary>
    /// Creates a bus access context for write operations.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private BusAccess CreateWriteAccess(Addr address, byte widthBits)
    {
        return new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: widthBits,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataWrite,
            SourceId: CpuSourceId,
            Cycle: context.Now,
            Flags: AccessFlags.None);
    }

    /// <summary>
    /// Adapter that wraps an IMemory interface as an IBusTarget.
    /// Used for backward compatibility when constructing a Cpu65C02 with IMemory.
    /// </summary>
    private sealed class MemoryToTargetAdapter : IBusTarget
    {
        private readonly IMemory memory;

        public MemoryToTargetAdapter(IMemory memory)
        {
            this.memory = memory;
        }

        public TargetCaps Capabilities => TargetCaps.None;

        public byte Read8(Addr physicalAddress, in BusAccess access)
        {
            return memory.Read(physicalAddress);
        }

        public void Write8(Addr physicalAddress, byte value, in BusAccess access)
        {
            memory.Write(physicalAddress, value);
        }

        public Word Read16(Addr physicalAddress, in BusAccess access)
        {
            return memory.ReadWord(physicalAddress);
        }

        public void Write16(Addr physicalAddress, Word value, in BusAccess access)
        {
            memory.WriteWord(physicalAddress, value);
        }

        public DWord Read32(Addr physicalAddress, in BusAccess access)
        {
            return memory.ReadDWord(physicalAddress);
        }

        public void Write32(Addr physicalAddress, DWord value, in BusAccess access)
        {
            memory.WriteDWord(physicalAddress, value);
        }

        public void Clear()
        {
            memory.Clear();
        }
    }
}