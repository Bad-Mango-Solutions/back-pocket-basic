// <copyright file="ICpu.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Interface for the CPU emulator.
/// </summary>
public interface ICpu
{
    /// <summary>
    /// CPU registers.
    /// </summary>
    CpuRegisters Registers { get; }

    /// <summary>
    /// Memory space.
    /// </summary>
    IMemory Memory { get; }

    /// <summary>
    /// Executes a single instruction.
    /// </summary>
    /// <returns>Number of cycles consumed.</returns>
    int Step();

    /// <summary>
    /// Executes instructions until a BRK or RTS is encountered.
    /// </summary>
    void Execute(int startAddress);

    /// <summary>
    /// Resets the CPU.
    /// </summary>
    void Reset();

    /// <summary>
    /// Whether the CPU is halted.
    /// </summary>
    bool Halted { get; }
}