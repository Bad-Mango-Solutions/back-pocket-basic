// <copyright file="IMemory.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Interface for the emulated memory space.
/// </summary>
public interface IMemory
{
    /// <summary>
    /// Reads a byte from memory.
    /// </summary>
    /// <returns></returns>
    byte Read(int address);

    /// <summary>
    /// Writes a byte to memory.
    /// </summary>
    void Write(int address, byte value);

    /// <summary>
    /// Reads a 16-bit word from memory (little-endian).
    /// </summary>
    /// <returns></returns>
    ushort ReadWord(int address);

    /// <summary>
    /// Writes a 16-bit word to memory (little-endian).
    /// </summary>
    void WriteWord(int address, ushort value);

    /// <summary>
    /// Clears all memory.
    /// </summary>
    void Clear();

    /// <summary>
    /// Total memory size.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Sets the speaker instance for audio output on $C030 access.
    /// </summary>
    void SetSpeaker(IAppleSpeaker? speaker);
}