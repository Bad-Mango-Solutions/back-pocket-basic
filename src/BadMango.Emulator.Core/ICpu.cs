// <copyright file="ICpu.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Base interface for CPU emulators in the 6502 family.
/// </summary>
/// <typeparam name="TRegisters">The type of the register structure for this CPU.</typeparam>
/// <typeparam name="TState">The type of the complete state structure for this CPU.</typeparam>
/// <remarks>
/// This interface defines the core contract for CPU implementations including
/// 6502, 65C02, 65816, and hypothetical 65832 processors.
/// The generic type parameters allow each CPU variant to define its own register and state structures,
/// accommodating different register widths (8-bit, 16-bit, 32-bit, 64-bit) and capabilities.
/// </remarks>
public interface ICpu<TRegisters, TState>
    where TRegisters : struct
    where TState : struct
{
    /// <summary>
    /// Gets a value indicating whether the CPU is halted.
    /// </summary>
    bool Halted { get; }

    /// <summary>
    /// Executes a single instruction.
    /// </summary>
    /// <returns>Number of cycles consumed by the instruction.</returns>
    int Step();

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
    /// <returns>A structure containing the current values of all CPU registers.</returns>
    TRegisters GetRegisters();

    /// <summary>
    /// Gets the current complete CPU state including registers and execution state.
    /// </summary>
    /// <returns>A structure containing all register values, cycle count, and other execution state.</returns>
    TState GetState();

    /// <summary>
    /// Sets the complete CPU state including registers and execution state.
    /// </summary>
    /// <param name="state">The state structure containing register values, cycle count, and other execution state to restore.</param>
    void SetState(TState state);

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
}