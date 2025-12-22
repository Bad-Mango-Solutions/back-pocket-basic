// <copyright file="OpcodeTableBuilders.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using Core;

/// <summary>
/// Provides convenient factory methods for creating opcode table builders for common CPU configurations.
/// </summary>
/// <remarks>
/// This class provides a clean API for creating builders without needing to specify
/// all the verbose generic type parameters.
/// </remarks>
public static class OpcodeTableBuilders
{
    /// <summary>
    /// Creates a new opcode table builder for the 65C02 CPU.
    /// </summary>
    /// <returns>A builder configured for the 65C02 CPU with all type parameters pre-configured.</returns>
    /// <remarks>
    /// <para>
    /// This factory method eliminates the need to specify the verbose generic type parameters
    /// when creating a builder for the 65C02 CPU.
    /// </para>
    /// <para>
    /// Usage example:
    /// <code>
    /// var builder = OpcodeTableBuilders.ForCpu65C02();
    /// handlers[0xA9] = builder.Instructions.LDA(builder.AddressingModes.Immediate);
    /// </code>
    /// </para>
    /// </remarks>
    public static GenericOpcodeTableBuilder<Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State> ForCpu65C02() =>
        new GenericOpcodeTableBuilder<Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State>();

    // TODO: Add factory methods for other CPU types (65816, 65832) as they are implemented
}
