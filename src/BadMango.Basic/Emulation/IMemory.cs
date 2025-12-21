// <copyright file="IMemory.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

/// <summary>
/// Represents an interface for managing and interacting with the emulated memory space
/// of an Apple II system.
/// </summary>
/// <remarks>
/// The <see cref="IMemory"/> interface provides methods for reading, writing, and clearing
/// memory, as well as handling specific memory operations such as reading and writing words.
/// It also supports integration with other components like the Apple II speaker emulator.
/// </remarks>
public interface IMemory
{
    /// <summary>
    /// Gets the size of the emulated memory in bytes.
    /// </summary>
    /// <value>
    /// The total number of bytes available in the memory space.
    /// </value>
    /// <remarks>
    /// This property represents the capacity of the memory managed by the implementation of the <see cref="IMemory"/> interface.
    /// For example, the default size for <see cref="AppleMemory"/> is 64KB.
    /// </remarks>
    int Size { get; }

    /// <summary>
    /// Reads a byte of data from the specified memory address.
    /// </summary>
    /// <param name="address">The memory address from which to read the byte. Must be a valid address within the memory range.</param>
    /// <returns>The byte of data stored at the specified memory address.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified <paramref name="address"/> is outside the valid memory range.
    /// </exception>
    byte Read(int address);

    /// <summary>
    /// Writes a byte value to the specified memory address in the emulated memory space.
    /// </summary>
    /// <param name="address">The memory address where the value will be written. Must be within the valid memory range.</param>
    /// <param name="value">The byte value to write to the specified memory address.</param>
    /// <remarks>
    /// - Writing to addresses in the I/O space will trigger specific I/O handling logic.
    /// - Writing to addresses in the ROM area is not allowed and will be logged as a warning.
    /// - An exception of type <see cref="MemoryAccessException"/> will be thrown if the address is out of bounds.
    /// </remarks>
    /// <exception cref="MemoryAccessException">Thrown when the specified address is outside the valid memory range.</exception>
    void Write(int address, byte value);

    /// <summary>
    /// Reads a 16-bit word (2 bytes) from the specified memory address.
    /// </summary>
    /// <param name="address">
    /// The starting address in memory from which to read the word. The word is read as two consecutive bytes:
    /// the low byte from the specified address and the high byte from the next address.
    /// </param>
    /// <returns>
    /// A 16-bit unsigned integer representing the word read from memory. The low byte is read from the specified
    /// address, and the high byte is read from the subsequent address.
    /// </returns>
    /// <remarks>
    /// This method combines two consecutive bytes from memory into a single 16-bit word, with the low byte
    /// occupying the least significant 8 bits and the high byte occupying the most significant 8 bits.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified address or the subsequent address is outside the valid memory range.
    /// </exception>
    ushort ReadWord(int address);

    /// <summary>
    /// Writes a 16-bit word to the specified memory address in the emulated memory space.
    /// </summary>
    /// <param name="address">
    /// The starting address in memory where the word will be written.
    /// The lower byte of the word is written to this address, and the higher byte is written to the next address.
    /// </param>
    /// <param name="value">
    /// The 16-bit word to write to memory. The lower byte is extracted and written first, followed by the higher byte.
    /// </param>
    /// <remarks>
    /// This method assumes that the memory space is contiguous and that the address is valid within the bounds of the memory.
    /// </remarks>
    void WriteWord(int address, ushort value);

    /// <summary>
    /// Clears the entire emulated memory by setting all bytes to zero.
    /// </summary>
    /// <remarks>
    /// This method resets the memory to its initial state, effectively erasing all data.
    /// It is commonly used during initialization or to reset the memory for a new operation.
    /// </remarks>
    void Clear();

    /// <summary>
    /// Configures the speaker for the emulated Apple II memory.
    /// </summary>
    /// <param name="speaker">
    /// An implementation of <see cref="IAppleSpeaker"/> to be used for sound emulation.
    /// Pass <c>null</c> to disable the speaker.
    /// </param>
    void SetSpeaker(IAppleSpeaker? speaker);
}