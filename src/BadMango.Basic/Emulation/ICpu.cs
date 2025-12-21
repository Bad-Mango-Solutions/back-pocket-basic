// <copyright file="ICpu.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

/// <summary>
/// Interface for the CPU emulator.
/// </summary>
public interface ICpu
{
    /// <summary>
    /// CPU registers.
    /// </summary>
    Cpu6502Registers Registers { get; }

    /// <summary>
    /// Memory space.
    /// </summary>
    IMemory Memory { get; }

    /// <summary>
    /// Whether the CPU is halted.
    /// </summary>
    bool Halted { get; }

    /// <summary>
    /// Executes a single instruction.
    /// </summary>
    /// <returns>Number of cycles consumed.</returns>
    int Step();

    /// <summary>
    /// Executes instructions starting from the specified memory address.
    /// </summary>
    /// <param name="startAddress">The memory address from which execution begins.</param>
    /// <remarks>
    /// This method sets the program counter to the specified start address and begins
    /// executing instructions until the CPU is halted. It is typically used to execute
    /// a sequence of machine code instructions in memory.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the CPU is in an invalid state that prevents execution.
    /// </exception>
    void Execute(int startAddress);

    /// <summary>
    /// Resets the CPU.
    /// </summary>
    void Reset();
}