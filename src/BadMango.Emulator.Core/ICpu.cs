// <copyright file="ICpu.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Base interface for CPU emulators in the 6502 family.
/// </summary>
/// <remarks>
/// This interface defines the core contract for CPU implementations including
/// 6502, 65C02, 65816, and hypothetical 65832 processors.
/// </remarks>
public interface ICpu
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
    void Execute(int startAddress);

    /// <summary>
    /// Resets the CPU to its initial state.
    /// </summary>
    void Reset();
}