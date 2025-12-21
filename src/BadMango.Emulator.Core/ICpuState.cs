// <copyright file="ICpuState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Interface for CPU state with generic type parameters for different register sizes.
/// </summary>
/// <typeparam name="TRegisters">The type of the CPU registers.</typeparam>
/// <typeparam name="TAccumulator">Type for the accumulator register (byte for 8-bit, ushort for 16-bit).</typeparam>
/// <typeparam name="TIndex">Type for index registers X and Y.</typeparam>
/// <typeparam name="TStack">Type for stack pointer register.</typeparam>
/// <typeparam name="TProgram">Type for program counter.</typeparam>
public interface ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    where TRegisters : ICpuRegisters<TAccumulator, TIndex, TStack, TProgram>
{
    /// <summary>
    /// Gets or sets the CPU registers.
    /// </summary>
    TRegisters Registers { get; set; }

    /// <summary>
    /// Gets or sets the total number of cycles executed.
    /// </summary>
    ulong Cycles { get; set; }

    /// <summary>
    /// Gets a value indicating whether the CPU is halted.
    /// </summary>
    /// <remarks>
    /// This property returns true if the CPU is in any halt state.
    /// For more granular halt state information, use <see cref="HaltReason"/>.
    /// </remarks>
    bool Halted { get; }

    /// <summary>
    /// Gets or sets the reason the CPU is halted.
    /// </summary>
    /// <remarks>
    /// Distinguishes between different halt states such as BRK, WAI, STP, or running (None).
    /// This enables accurate emulation of hardware behavior for each halt condition.
    /// </remarks>
    HaltState HaltReason { get; set; }

    /// <summary>
    /// Gets or sets the Accumulator register (A).
    /// </summary>
    TAccumulator A { get; set; }

    /// <summary>
    /// Gets or sets the X index register.
    /// </summary>
    TIndex X { get; set; }

    /// <summary>
    /// Gets or sets the Y index register.
    /// </summary>
    TIndex Y { get; set; }

    /// <summary>
    /// Gets or sets the Stack Pointer register (SP).
    /// </summary>
    TStack SP { get; set; }

    /// <summary>
    /// Gets or sets the Processor Status register (P).
    /// </summary>
    byte P { get; set; }

    /// <summary>
    /// Gets or sets the Program Counter (PC).
    /// </summary>
    TProgram PC { get; set; }
}