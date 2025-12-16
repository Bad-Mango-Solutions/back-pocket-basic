// <copyright file="IAppleSystem.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Interface for Apple II system emulation.
/// </summary>
public interface IAppleSystem
{
    /// <summary>
    /// The CPU.
    /// </summary>
    ICpu Cpu { get; }

    /// <summary>
    /// The memory.
    /// </summary>
    IMemory Memory { get; }

    /// <summary>
    /// The speaker.
    /// </summary>
    IAppleSpeaker Speaker { get; }

    /// <summary>
    /// Reads a byte from emulated memory (PEEK).
    /// </summary>
    /// <returns></returns>
    byte Peek(int address);

    /// <summary>
    /// Writes a byte to emulated memory (POKE).
    /// </summary>
    void Poke(int address, byte value);

    /// <summary>
    /// Executes machine code at the specified address (CALL).
    /// </summary>
    void Call(int address);

    /// <summary>
    /// Sets the keyboard buffer.
    /// </summary>
    void SetKeyboardInput(char key);

    /// <summary>
    /// Gets and clears the keyboard buffer.
    /// </summary>
    /// <returns></returns>
    char? GetKeyboardInput();

    /// <summary>
    /// Resets the system.
    /// </summary>
    void Reset();
}