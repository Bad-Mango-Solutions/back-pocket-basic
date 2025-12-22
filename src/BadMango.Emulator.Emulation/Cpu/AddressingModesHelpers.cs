// <copyright file="AddressingModesHelpers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using Core;

/// <summary>
/// Helper type aliases for commonly used CPU addressing modes.
/// </summary>
/// <remarks>
/// These aliases provide convenient shortcuts for accessing generic addressing modes
/// with specific CPU type parameters, reducing verbosity at call sites.
/// </remarks>
public static class AddressingModesHelpers
{
    /// <summary>
    /// Addressing modes configured for the 65C02 CPU.
    /// </summary>
    public static class Cpu65C02
    {
        /// <summary>
        /// Implied addressing for 65C02.
        /// </summary>
        /// <typeparam name="TState">The CPU state type.</typeparam>
        /// <param name="memory">The memory interface.</param>
        /// <param name="state">Reference to the CPU state.</param>
        /// <returns>The effective address.</returns>
        public static Addr Implied<TState>(IMemory memory, ref TState state)
            where TState : struct, ICpuState<Cpu65C02Registers, byte, byte, byte, Word>
            => AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Implied(memory, ref state);

        /// <summary>
        /// Immediate addressing for 65C02.
        /// </summary>
        /// <typeparam name="TState">The CPU state type.</typeparam>
        /// <param name="memory">The memory interface.</param>
        /// <param name="state">Reference to the CPU state.</param>
        /// <returns>The effective address.</returns>
        public static Addr Immediate<TState>(IMemory memory, ref TState state)
            where TState : struct, ICpuState<Cpu65C02Registers, byte, byte, byte, Word>
            => AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.Immediate(memory, ref state);

        /// <summary>
        /// Zero Page addressing for 65C02.
        /// </summary>
        /// <typeparam name="TState">The CPU state type.</typeparam>
        /// <param name="memory">The memory interface.</param>
        /// <param name="state">Reference to the CPU state.</param>
        /// <returns>The effective address.</returns>
        public static Addr ZeroPage<TState>(IMemory memory, ref TState state)
            where TState : struct, ICpuState<Cpu65C02Registers, byte, byte, byte, Word>
            => AddressingModesFor<Cpu65C02Registers, byte, byte, byte, Word>.ZeroPage(memory, ref state);

        // Additional addressing modes can be added as needed following the same pattern
    }

    // NOTE: Cpu65816 and Cpu65832 helpers can be added once their register types
    // implement ICpuRegisters<TAccumulator, TIndex, TStack, TProgram>
}

