// <copyright file="CpuBase.cs" company="Bad Mango Solutions">
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
/// Abstract base class for CPU emulators providing common functionality.
/// </summary>
/// <remarks>
/// <para>
/// This base class provides common implementation patterns shared across
/// different CPU emulators in the 65xx family (65C02, 65816, 65832).
/// </para>
/// <para>
/// Derived classes must implement:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Capabilities"/> - CPU-specific feature flags</description></item>
/// <item><description><see cref="Step"/> - Single instruction execution</description></item>
/// <item><description><see cref="Reset"/> - CPU-specific reset behavior</description></item>
/// </list>
/// </remarks>
public abstract class CpuBase : ICpu
{
    /// <summary>
    /// The source ID used for bus access tracing.
    /// </summary>
    protected const int CpuSourceId = 0;

    /// <summary>
    /// The event context providing access to bus, signals, and scheduler.
    /// </summary>
    private readonly IEventContext context;

    /// <summary>
    /// The memory bus for all memory operations.
    /// </summary>
    private readonly IMemoryBus bus;

    /// <summary>
    /// The signal bus for interrupt handling.
    /// </summary>
    private readonly ISignalBus signals;

    /// <summary>
    /// CPU registers (directly stored, no CpuState wrapper).
    /// </summary>
    private Registers registers;

    /// <summary>
    /// Halt state managed directly by CPU.
    /// </summary>
    private HaltState haltReason;

    /// <summary>
    /// Instruction trace for debug information.
    /// </summary>
    private InstructionTrace trace;

    /// <summary>
    /// Whether a stop has been requested.
    /// </summary>
    private bool stopRequested;

    /// <summary>
    /// The attached debug listener, if any.
    /// </summary>
    private IDebugStepListener? debugListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="CpuBase"/> class with an event context.
    /// </summary>
    /// <param name="context">The event context providing access to the memory bus, signal bus, and scheduler.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    protected CpuBase(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        this.context = context;
        bus = context.Bus;
        signals = context.Signals;
    }

    /// <summary>
    /// Gets the event context providing access to bus, signals, and scheduler.
    /// </summary>
    /// <value>The event context for this CPU.</value>
    public IEventContext EventContext => context;

    /// <inheritdoc />
    public abstract CpuCapabilities Capabilities { get; }

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

    /// <summary>
    /// Gets the memory bus for all memory operations.
    /// </summary>
    protected IMemoryBus Bus => bus;

    /// <summary>
    /// Gets the signal bus for interrupt handling.
    /// </summary>
    protected ISignalBus Signals => signals;

    /// <summary>
    /// Gets the attached debug listener, or <see langword="null"/> if none is attached.
    /// </summary>
    protected IDebugStepListener? DebugListener => debugListener;

    /// <inheritdoc/>
    public abstract CpuStepResult Step();

    /// <inheritdoc/>
    public abstract void Reset();

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
    public virtual byte Read8(Addr address)
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
    public virtual void Write8(Addr address, byte value)
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
    public virtual byte Peek8(Addr address)
    {
        var access = CreateDebugReadAccess(address);
        var result = bus.TryRead8(access);

        if (result.Failed)
        {
            // Return 0xFF for any bus fault (unmapped, permission denied, etc.)
            // This matches Apple II floating bus behavior and is safe for debug reads
            return 0xFF;
        }

        return result.Value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Poke8(Addr address, byte value)
    {
        var access = CreateDebugWriteAccess(address);
        bus.TryWrite8(access, value);

        // Debug writes silently ignore failures for these reasons:
        // 1. Unmapped regions: acceptable in test scenarios
        // 2. Permission denied: handled by bus bypassing check for DebugWrite
        // 3. Target doesn't support poke: RomTarget constructed with ReadOnlyMemory
        // If a test expects poke to work and it doesn't, the test will fail
        // on subsequent read verification, which is the appropriate failure point.
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Word Read16(Addr address)
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
    public virtual void Write16(Addr address, Word value)
    {
        var access = CreateWriteAccess(address, 16);
        bus.TryWrite16(access, value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual DWord ReadValue(Addr address, byte sizeInBits)
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
    public virtual void WriteValue(Addr address, DWord value, byte sizeInBits)
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
    public virtual Addr PushByte(Addr stackBase = 0)
    {
        var old = registers.SP.stack;
        registers.SP.stack--;
        return stackBase + old;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual Addr PopByte(Addr stackBase = 0)
    {
        var old = registers.SP.stack + 1;
        registers.SP.stack++;
        return stackBase + old;
    }

    // ─── Protected Helper Methods ───────────────────────────────────────

    /// <summary>
    /// Reads a 32-bit value from memory.
    /// </summary>
    /// <param name="address">The address to read from.</param>
    /// <returns>The 32-bit value at the address.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected DWord Read32(Addr address)
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
    /// <param name="address">The address to write to.</param>
    /// <param name="value">The 32-bit value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Write32(Addr address, DWord value)
    {
        var access = CreateWriteAccess(address, 32);
        bus.TryWrite32(access, value);
    }

    /// <summary>
    /// Creates a bus access context for read operations.
    /// </summary>
    /// <param name="address">The address being accessed.</param>
    /// <param name="widthBits">The width of the access in bits.</param>
    /// <returns>A configured <see cref="BusAccess"/> for reading.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected BusAccess CreateReadAccess(Addr address, byte widthBits)
    {
        return new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: widthBits,
            Mode: BusAccessMode.Decomposed, // 65xx uses decomposed mode for accurate cycle counting
            EmulationFlag: GetEmulationFlag(),
            Intent: AccessIntent.DataRead,
            SourceId: CpuSourceId,
            Cycle: context.Now,
            Flags: AccessFlags.None);
    }

    /// <summary>
    /// Creates a bus access context for write operations.
    /// </summary>
    /// <param name="address">The address being accessed.</param>
    /// <param name="widthBits">The width of the access in bits.</param>
    /// <returns>A configured <see cref="BusAccess"/> for writing.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected BusAccess CreateWriteAccess(Addr address, byte widthBits)
    {
        return new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: widthBits,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: GetEmulationFlag(),
            Intent: AccessIntent.DataWrite,
            SourceId: CpuSourceId,
            Cycle: context.Now,
            Flags: AccessFlags.None);
    }

    /// <summary>
    /// Creates a bus access context for instruction fetch operations.
    /// </summary>
    /// <param name="address">The address being accessed.</param>
    /// <param name="widthBits">The width of the access in bits (8, 16, etc.).</param>
    /// <returns>A configured <see cref="BusAccess"/> for instruction fetch.</returns>
    /// <remarks>
    /// Instruction fetches use <see cref="AccessIntent.InstructionFetch"/> to allow the bus
    /// to differentiate between data reads and code fetches. This enables NX enforcement
    /// and trap interception at ROM entry points.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected BusAccess CreateInstructionFetchAccess(Addr address, byte widthBits)
    {
        return new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: widthBits,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: GetEmulationFlag(),
            Intent: AccessIntent.InstructionFetch,
            SourceId: CpuSourceId,
            Cycle: context.Now,
            Flags: AccessFlags.None);
    }

    /// <summary>
    /// Creates a bus access context for debug read (peek) operations.
    /// </summary>
    /// <param name="address">The address being accessed.</param>
    /// <returns>A configured <see cref="BusAccess"/> for debug read.</returns>
    /// <remarks>
    /// Debug reads use <see cref="AccessIntent.DebugRead"/> to read memory without
    /// triggering side effects such as soft switches or flag clearing.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected BusAccess CreateDebugReadAccess(Addr address)
    {
        return new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: GetEmulationFlag(),
            Intent: AccessIntent.DebugRead,
            SourceId: CpuSourceId,
            Cycle: context.Now,
            Flags: AccessFlags.NoSideEffects);
    }

    /// <summary>
    /// Creates a bus access context for debug write (poke) operations.
    /// </summary>
    /// <param name="address">The address being accessed.</param>
    /// <returns>A configured <see cref="BusAccess"/> for debug write.</returns>
    /// <remarks>
    /// Debug writes use <see cref="AccessIntent.DebugWrite"/> to write memory,
    /// bypassing ROM write protection and avoiding side effects on I/O addresses.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected BusAccess CreateDebugWriteAccess(Addr address)
    {
        return new BusAccess(
            Address: address,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: GetEmulationFlag(),
            Intent: AccessIntent.DebugWrite,
            SourceId: CpuSourceId,
            Cycle: context.Now,
            Flags: AccessFlags.NoSideEffects);
    }

    /// <summary>
    /// Gets the emulation flag value for bus access operations.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the CPU is in emulation mode (65C02 compatibility);
    /// <see langword="false"/> if in native mode (65816/65832).
    /// </returns>
    /// <remarks>
    /// The 65C02 is always in emulation mode. The 65816 and 65832 can switch between
    /// emulation and native modes. Derived classes should override this method to
    /// return the appropriate value based on their current mode.
    /// </remarks>
    protected virtual bool GetEmulationFlag() => true;
}