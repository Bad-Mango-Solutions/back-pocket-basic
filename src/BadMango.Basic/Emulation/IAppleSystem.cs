// <copyright file="IAppleSystem.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

/// <summary>
/// Represents the interface for emulating an Apple II system, providing access to
/// the CPU, memory, speaker, and various system-level operations such as memory
/// manipulation, keyboard input handling, and system reset.
/// </summary>
public interface IAppleSystem
{
    /// <summary>
    /// Gets the CPU emulator associated with the Apple II system.
    /// </summary>
    /// <value>
    /// An instance of <see cref="ICpu"/> that provides functionality for executing
    /// instructions, accessing CPU registers, and interacting with emulated memory.
    /// </value>
    ICpu Cpu { get; }

    /// <summary>
    /// Gets the emulated memory interface for the Apple II system.
    /// </summary>
    /// <remarks>
    /// The <see cref="IMemory"/> interface provides methods for interacting with the memory space,
    /// including reading, writing, and performing specific memory operations. This property
    /// allows access to the memory component of the emulated system, enabling manipulation
    /// of memory for various operations such as PEEK, POKE, and system-level tasks.
    /// </remarks>
    IMemory Memory { get; }

    /// <summary>
    /// Gets the emulated Apple II speaker, which provides functionality for
    /// generating audio output, such as clicks and beeps, and managing audio buffering.
    /// </summary>
    IAppleSpeaker Speaker { get; }

    /// <summary>
    /// Reads a byte of data from the specified memory address in the emulated Apple II system.
    /// </summary>
    /// <param name="address">The memory address to read from. Must be within the valid range of the emulated memory space.</param>
    /// <returns>The byte of data stored at the specified memory address.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified <paramref name="address"/> is outside the valid range of memory addresses.
    /// </exception>
    byte Peek(int address);

    /// <summary>
    /// Writes a byte of data to the specified memory address in the emulated Apple II system.
    /// </summary>
    /// <param name="address">The memory address where the byte will be written. Must be within the valid range of the emulated memory space.</param>
    /// <param name="value">The byte value to write to the specified memory address.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified <paramref name="address"/> is outside the valid range of memory addresses.
    /// </exception>
    /// <remarks>
    /// This method emulates the POKE operation in the Apple II system, enabling direct manipulation of memory.
    /// </remarks>
    void Poke(int address, byte value);

    /// <summary>
    /// Executes a machine code subroutine at the specified memory address.
    /// </summary>
    /// <param name="address">
    /// The memory address where the machine code subroutine begins.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified <paramref name="address"/> is invalid.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown if an error occurs during the execution of the machine code.
    /// </exception>
    /// <remarks>
    /// This method is used to perform low-level operations by directly invoking
    /// machine code at a specific memory location. It is primarily intended for
    /// advanced scenarios, such as interacting with ROM routines or custom
    /// machine code.
    /// </remarks>
    void Call(int address);

    /// <summary>
    /// Sets the keyboard input for the emulated Apple II system.
    /// </summary>
    /// <param name="key">
    /// The character to be set as the keyboard input. The high bit of the character
    /// is automatically set to indicate that a key is pressed.
    /// </param>
    /// <remarks>
    /// This method updates the internal keyboard buffer and writes the key value
    /// to the memory location associated with the keyboard input.
    /// </remarks>
    void SetKeyboardInput(char key);

    /// <summary>
    /// Retrieves the current character from the keyboard buffer, if available, and clears the buffer.
    /// </summary>
    /// <returns>
    /// The character from the keyboard buffer, or <c>null</c> if the buffer is empty.
    /// </returns>
    /// <remarks>
    /// This method emulates the behavior of the Apple II keyboard buffer. After retrieving the character,
    /// the keyboard buffer is cleared, and the strobe bit in the memory location associated with the keyboard
    /// is reset.
    /// </remarks>
    char? GetKeyboardInput();

    /// <summary>
    /// Resets the emulated Apple II system to its initial state.
    /// </summary>
    /// <remarks>
    /// This method reinitializes the system components, resets the CPU, and clears the keyboard input buffer,
    /// ensuring the system is restored to a clean state for further operations.
    /// </remarks>
    void Reset();
}