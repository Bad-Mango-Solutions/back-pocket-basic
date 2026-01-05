// <copyright file="ICpu.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Interfaces.Cpu;

using Core.Cpu;

using Debugging;

/// <summary>
/// Base interface for CPU emulators in the 6502 family.
/// </summary>
/// <remarks>
/// This interface defines the core contract for CPU implementations including
/// 6502, 65C02, 65816, and hypothetical 65832 processors.
/// The generic type parameters allow each CPU variant to define its own register and state structures,
/// accommodating different register widths (8-bit, 16-bit, 32-bit, 64-bit) and capabilities.
/// </remarks>
public interface ICpu
{
    /// <summary>
    /// Gets the capabilities supported by the CPU.
    /// </summary>
    /// <value>
    /// A combination of <see cref="CpuCapabilities"/> flags indicating the features
    /// and instruction sets supported by the CPU implementation.
    /// </value>
    /// <remarks>
    /// This property provides information about the specific capabilities of the CPU,
    /// such as support for extended instruction sets (e.g., 65C02, 65816, 65832) and
    /// register widths (e.g., 16-bit, 32-bit, 64-bit). It can be used to determine
    /// compatibility with software or to enable/disable features dynamically.
    /// </remarks>
    CpuCapabilities Capabilities { get; }

    /// <summary>
    /// Gets a reference to the CPU's register set.
    /// </summary>
    /// <value>A reference to the registers structure.</value>
    /// <remarks>
    /// This property provides direct access to CPU registers for instruction handlers
    /// and addressing modes. Modifications to the returned reference affect the CPU directly.
    /// </remarks>
    ref Registers Registers { get; }

    /// <summary>
    /// Gets or sets the instruction trace for debug tracing.
    /// </summary>
    /// <value>The current instruction trace structure.</value>
    /// <remarks>
    /// This property provides access to the instruction trace for instruction handlers
    /// and addressing modes to record debug information during execution.
    /// Use the <c>with</c> keyword to create modified copies when setting.
    /// Only meaningful when <see cref="IsDebuggerAttached"/> is true.
    /// </remarks>
    InstructionTrace Trace { get; set; }

    /// <summary>
    /// Gets a value indicating whether the CPU is halted.
    /// </summary>
    /// <remarks>
    /// This property returns true if the CPU is in any halt state (Wai or Stp).
    /// For more granular halt state information, use <see cref="HaltReason"/>.
    /// </remarks>
    bool Halted { get; }

    /// <summary>
    /// Gets or sets the reason the CPU is halted.
    /// </summary>
    /// <remarks>
    /// Distinguishes between different halt states:
    /// - None: CPU is running
    /// - Wai: Halted by WAI instruction (wait for interrupt)
    /// - Stp: Halted by STP instruction (permanent halt until reset).
    /// </remarks>
    HaltState HaltReason { get; set; }

    /// <summary>
    /// Gets a value indicating whether a debugger is currently attached.
    /// </summary>
    bool IsDebuggerAttached { get; }

    /// <summary>
    /// Gets a value indicating whether a stop has been requested.
    /// </summary>
    bool IsStopRequested { get; }

    // ─── Memory Access Methods ──────────────────────────────────────────

    /// <summary>
    /// Reads a byte from memory at the specified address.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <returns>The byte value at the specified address.</returns>
    /// <remarks>
    /// This method provides bus-based memory access for instruction handlers.
    /// The implementation routes through the memory bus with appropriate
    /// cycle counting and fault handling.
    /// </remarks>
    byte Read8(Addr address);

    /// <summary>
    /// Writes a byte to memory at the specified address.
    /// </summary>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="value">The byte value to write.</param>
    /// <remarks>
    /// This method provides bus-based memory access for instruction handlers.
    /// The implementation routes through the memory bus with appropriate
    /// cycle counting and fault handling.
    /// </remarks>
    void Write8(Addr address, byte value);

    /// <summary>
    /// Reads a byte from memory without triggering side effects (debug/peek access).
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <returns>The byte value at the specified address.</returns>
    /// <remarks>
    /// <para>
    /// This method uses <see cref="AccessIntent.DebugRead"/> to read memory without
    /// triggering soft switches, clearing flags, or causing other side effects.
    /// </para>
    /// <para>
    /// Use this for debugging, memory inspection, and test scenarios where you need
    /// to observe memory state without affecting emulation behavior.
    /// </para>
    /// </remarks>
    byte Peek8(Addr address);

    /// <summary>
    /// Writes a byte to memory bypassing write protection (debug/poke access).
    /// </summary>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="value">The byte value to write.</param>
    /// <remarks>
    /// <para>
    /// This method uses <see cref="AccessIntent.DebugWrite"/> to write memory,
    /// bypassing ROM write protection and avoiding side effects on I/O addresses.
    /// </para>
    /// <para>
    /// Use this for debugging, patching ROM content in tests, and scenarios where
    /// you need to modify memory that would normally be read-only.
    /// </para>
    /// </remarks>
    void Poke8(Addr address, byte value);

    /// <summary>
    /// Reads a 16-bit word from memory at the specified address.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <returns>The 16-bit word value at the specified address (little-endian).</returns>
    Word Read16(Addr address);

    /// <summary>
    /// Writes a 16-bit word to memory at the specified address.
    /// </summary>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="value">The 16-bit word value to write (little-endian).</param>
    void Write16(Addr address, Word value);

    /// <summary>
    /// Reads a value from memory based on the specified size.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="sizeInBits">The size to read (8, 16, or 32 bits).</param>
    /// <returns>The value read from memory, zero-extended to 32 bits.</returns>
    DWord ReadValue(Addr address, byte sizeInBits);

    /// <summary>
    /// Writes a value to memory based on the specified size.
    /// </summary>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="sizeInBits">The size to write (8, 16, or 32 bits).</param>
    void WriteValue(Addr address, DWord value, byte sizeInBits);

    // ─── Stack Operations ───────────────────────────────────────────────

    /// <summary>
    /// Pushes a byte onto the stack and decrements the stack pointer.
    /// </summary>
    /// <param name="stackBase">The base address of the stack (default 0).</param>
    /// <returns>The address where the byte should be stored (stack base + old SP).</returns>
    /// <remarks>
    /// This method decrements the stack pointer register and returns the address
    /// where the byte should be written. The caller is responsible for actually
    /// writing the value to memory using <see cref="Write8"/>.
    /// </remarks>
    Addr PushByte(Addr stackBase = 0);

    /// <summary>
    /// Pops a byte from the stack and increments the stack pointer.
    /// </summary>
    /// <param name="stackBase">The base address of the stack (default 0).</param>
    /// <returns>The address from which the byte should be read (stack base + new SP).</returns>
    /// <remarks>
    /// This method increments the stack pointer register and returns the address
    /// from which the byte should be read. The caller is responsible for actually
    /// reading the value from memory using <see cref="Read8"/>.
    /// </remarks>
    Addr PopByte(Addr stackBase = 0);

    /// <summary>
    /// Executes a single instruction.
    /// </summary>
    /// <returns>A <see cref="CpuStepResult"/> containing the run state and cycles consumed.</returns>
    CpuStepResult Step();

    /// <summary>
    /// Executes instructions starting from the specified memory address.
    /// </summary>
    /// <param name="startAddress">The memory address from which execution begins.</param>
    /// <remarks>
    /// This method sets the program counter to the specified start address and begins
    /// executing instructions until the CPU is halted.
    /// </remarks>
    void Execute(uint startAddress);

    /// <summary>
    /// Resets the CPU to its initial state.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets the current CPU register state.
    /// </summary>
    /// <returns>RegisterAccumulator structure containing the current values of all CPU registers.</returns>
    Registers GetRegisters();

    /// <summary>
    /// Gets the current cycle count as tracked by the scheduler.
    /// </summary>
    /// <returns>The total number of cycles executed since reset.</returns>
    ulong GetCycles();

    /// <summary>
    /// Sets the current cycle count, advancing the scheduler if necessary.
    /// </summary>
    /// <param name="cycles">The new cycle count value.</param>
    /// <remarks>
    /// If the new cycle count is greater than the current scheduler time,
    /// the scheduler will be advanced to match. This is useful for test
    /// scenarios that need to manipulate cycle timing.
    /// </remarks>
    void SetCycles(ulong cycles);

    /// <summary>
    /// Signals an IRQ (Interrupt Request) to the CPU.
    /// </summary>
    /// <remarks>
    /// IRQ is a maskable interrupt that can be disabled by setting the I (Interrupt Disable) flag.
    /// When an IRQ is signaled and the I flag is clear, the CPU will:
    /// - Complete the current instruction
    /// - Push PC and processor status to the stack
    /// - Set the I flag to disable further interrupts
    /// - Load PC from the IRQ vector at $FFFE-$FFFF
    /// If the CPU is in WAI state, it will resume and process the interrupt.
    /// IRQ has lower priority than NMI.
    /// </remarks>
    void SignalIRQ();

    /// <summary>
    /// Signals an NMI (Non-Maskable Interrupt) to the CPU.
    /// </summary>
    /// <remarks>
    /// NMI is a non-maskable interrupt that cannot be disabled by the I flag.
    /// When an NMI is signaled, the CPU will:
    /// - Complete the current instruction
    /// - Push PC and processor status to the stack
    /// - Set the I flag to disable IRQ interrupts
    /// - Load PC from the NMI vector at $FFFA-$FFFB
    /// If the CPU is in WAI state, it will resume and process the interrupt.
    /// NMI has higher priority than IRQ.
    /// </remarks>
    void SignalNMI();

    /// <summary>
    /// Attaches a debug listener to receive step notifications.
    /// </summary>
    /// <param name="listener">The listener to attach.</param>
    /// <remarks>
    /// When a debugger is attached, the CPU will emit step events before and after
    /// each instruction execution. This does not affect cycle counting or execution
    /// timing - debugger overhead is not charged to the emulated system.
    /// </remarks>
    void AttachDebugger(IDebugStepListener listener);

    /// <summary>
    /// Detaches the current debug listener.
    /// </summary>
    void DetachDebugger();

    /// <summary>
    /// Sets the program counter to the specified address.
    /// </summary>
    /// <param name="address">The new program counter value.</param>
    void SetPC(Addr address);

    /// <summary>
    /// Gets the current program counter value.
    /// </summary>
    /// <returns>The current program counter address.</returns>
    Addr GetPC();

    /// <summary>
    /// Requests that execution stop at the next opportunity.
    /// </summary>
    /// <remarks>
    /// This is used by debuggers to interrupt a running CPU without waiting for
    /// a halt instruction. The stop request is processed at instruction boundaries.
    /// </remarks>
    void RequestStop();

    /// <summary>
    /// Clears any pending stop request.
    /// </summary>
    void ClearStopRequest();
}