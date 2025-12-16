// <copyright file="MemoryAccessException.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Represents an exception that is thrown when an invalid memory access occurs
/// in the emulated memory space.
/// </summary>
/// <remarks>
/// This exception is typically thrown when an operation attempts to access a memory
/// address that is outside the valid range or violates memory access rules.
/// </remarks>
public class MemoryAccessException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryAccessException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <remarks>
    /// This constructor allows for the creation of a <see cref="MemoryAccessException"/> with a custom error message,
    /// providing additional context about the invalid memory access that occurred.
    /// </remarks>
    public MemoryAccessException(string message)
        : base(message)
    {
    }
}