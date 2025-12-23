// <copyright file="ICpuRegisters.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

// ReSharper disable InconsistentNaming
namespace BadMango.Emulator.Core;

/// <summary>Interface for CPU register access with generic type parameters for different register sizes.</summary>
/// <typeparam name="TAccumulator">Type for the accumulator register (byte for 8-bit, ushort for 16-bit).</typeparam>
/// <typeparam name="TIndex">Type for index registers X and Y.</typeparam>
/// <typeparam name="TStack">Type for stack pointer register.</typeparam>
/// <typeparam name="TProgram">Type for program counter.</typeparam>
public interface ICpuRegisters<TAccumulator, TIndex, TStack, TProgram>
{
    /// <summary>
    /// Gets or sets the Accumulator register (RegisterAccumulator).
    /// </summary>
    RegisterAccumulator RegisterAccumulator { get; set; }

    /// <summary>
    /// Gets or sets the X index register.
    /// </summary>
    RegisterIndex X { get; set; }

    /// <summary>
    /// Gets or sets the Y index register.
    /// </summary>
    RegisterIndex Y { get; set; }

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
    RegisterProgramCounter PC { get; set; }
}