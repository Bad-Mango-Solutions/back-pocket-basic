// <copyright file="GenericOpcodeTableBuilder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using Core;

/// <summary>
/// Provides a clean builder API for constructing opcode tables with generic instructions and addressing modes.
/// </summary>
/// <typeparam name="TCpu">The CPU type.</typeparam>
/// <typeparam name="TRegisters">The CPU registers type.</typeparam>
/// <typeparam name="TAccumulator">The accumulator register type.</typeparam>
/// <typeparam name="TIndex">The index register type (X, Y).</typeparam>
/// <typeparam name="TStack">The stack pointer type.</typeparam>
/// <typeparam name="TProgram">The program counter type.</typeparam>
/// <typeparam name="TState">The CPU state type.</typeparam>
/// <remarks>
/// <para>
/// This builder encapsulates the verbose generic type parameters required by the generic
/// instruction and addressing mode implementations, providing a clean and readable API
/// for building opcode tables.
/// </para>
/// <para>
/// Usage example:
/// <code>
/// var builder = new GenericOpcodeTableBuilder&lt;Cpu65C02, Cpu65C02Registers, byte, byte, byte, Word, Cpu65C02State&gt;();
/// handlers[0xA9] = builder.Instructions.LDA(builder.AddressingModes.Immediate);
/// </code>
/// </para>
/// </remarks>
public class GenericOpcodeTableBuilder<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>
    where TRegisters : ICpuRegisters<TAccumulator, TIndex, TStack, TProgram>
    where TState : struct, ICpuState<TRegisters, TAccumulator, TIndex, TStack, TProgram>
    where TAccumulator : struct, System.Numerics.INumber<TAccumulator>
    where TIndex : struct, System.Numerics.INumber<TIndex>
    where TStack : struct, System.Numerics.INumber<TStack>
    where TProgram : struct, System.Numerics.IIncrementOperators<TProgram>, System.Numerics.IBinaryInteger<TProgram>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenericOpcodeTableBuilder{TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState}"/> class.
    /// </summary>
    public GenericOpcodeTableBuilder()
    {
        Instructions = new InstructionsAccessor();
        AddressingModes = new AddressingModesAccessor();
    }

    /// <summary>
    /// Gets the instructions accessor for this CPU configuration.
    /// </summary>
    /// <remarks>
    /// Provides access to generic instruction implementations with the type parameters
    /// already configured, eliminating the need to specify them at each call site.
    /// </remarks>
    public InstructionsAccessor Instructions { get; }

    /// <summary>
    /// Gets the addressing modes accessor for this CPU configuration.
    /// </summary>
    /// <remarks>
    /// Provides access to generic addressing mode implementations with the type parameters
    /// already configured, eliminating the need to specify them at each call site.
    /// </remarks>
    public AddressingModesAccessor AddressingModes { get; }

    /// <summary>
    /// Provides access to generic instruction implementations.
    /// </summary>
    /// <remarks>
    /// This class wraps the static generic instruction methods, providing a cleaner API
    /// by hiding the verbose type parameter specifications.
    /// </remarks>
    public class InstructionsAccessor
    {
        /// <summary>
        /// LDA - Load Accumulator instruction.
        /// </summary>
        /// <param name="addressingMode">The addressing mode function to use.</param>
        /// <returns>An opcode handler that executes LDA with the given addressing mode.</returns>
        public OpcodeHandler<TCpu, TState> LDA(AddressingMode<TState> addressingMode) =>
            InstructionsFor<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>.LDA(addressingMode);

        /// <summary>
        /// LDX - Load X Register instruction.
        /// </summary>
        /// <param name="addressingMode">The addressing mode function to use.</param>
        /// <returns>An opcode handler that executes LDX with the given addressing mode.</returns>
        public OpcodeHandler<TCpu, TState> LDX(AddressingMode<TState> addressingMode) =>
            InstructionsFor<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>.LDX(addressingMode);

        /// <summary>
        /// LDY - Load Y Register instruction.
        /// </summary>
        /// <param name="addressingMode">The addressing mode function to use.</param>
        /// <returns>An opcode handler that executes LDY with the given addressing mode.</returns>
        public OpcodeHandler<TCpu, TState> LDY(AddressingMode<TState> addressingMode) =>
            InstructionsFor<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>.LDY(addressingMode);

        /// <summary>
        /// STA - Store Accumulator instruction.
        /// </summary>
        /// <param name="addressingMode">The addressing mode function to use.</param>
        /// <returns>An opcode handler that executes STA with the given addressing mode.</returns>
        public OpcodeHandler<TCpu, TState> STA(AddressingMode<TState> addressingMode) =>
            InstructionsFor<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>.STA(addressingMode);

        /// <summary>
        /// STX - Store X Register instruction.
        /// </summary>
        /// <param name="addressingMode">The addressing mode function to use.</param>
        /// <returns>An opcode handler that executes STX with the given addressing mode.</returns>
        public OpcodeHandler<TCpu, TState> STX(AddressingMode<TState> addressingMode) =>
            InstructionsFor<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>.STX(addressingMode);

        /// <summary>
        /// STY - Store Y Register instruction.
        /// </summary>
        /// <param name="addressingMode">The addressing mode function to use.</param>
        /// <returns>An opcode handler that executes STY with the given addressing mode.</returns>
        public OpcodeHandler<TCpu, TState> STY(AddressingMode<TState> addressingMode) =>
            InstructionsFor<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>.STY(addressingMode);

        /// <summary>
        /// NOP - No Operation instruction.
        /// </summary>
        /// <param name="addressingMode">The addressing mode function to use.</param>
        /// <returns>An opcode handler that executes NOP with the given addressing mode.</returns>
        public OpcodeHandler<TCpu, TState> NOP(AddressingMode<TState> addressingMode) =>
            InstructionsFor<TCpu, TRegisters, TAccumulator, TIndex, TStack, TProgram, TState>.NOP(addressingMode);

        // TODO: Add wrapper methods for remaining instructions as they are implemented in InstructionsFor
    }

    /// <summary>
    /// Provides access to generic addressing mode implementations.
    /// </summary>
    /// <remarks>
    /// This class wraps the static generic addressing mode methods, providing a cleaner API
    /// by hiding the verbose type parameter specifications.
    /// </remarks>
    public class AddressingModesAccessor
    {
        /// <summary>
        /// Implied addressing mode.
        /// </summary>
        public AddressingMode<TState> Implied =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.Implied;

        /// <summary>
        /// Immediate addressing mode.
        /// </summary>
        public AddressingMode<TState> Immediate =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.Immediate;

        /// <summary>
        /// Zero Page addressing mode.
        /// </summary>
        public AddressingMode<TState> ZeroPage =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.ZeroPage;

        /// <summary>
        /// Zero Page,X addressing mode.
        /// </summary>
        public AddressingMode<TState> ZeroPageX =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.ZeroPageX;

        /// <summary>
        /// Zero Page,Y addressing mode.
        /// </summary>
        public AddressingMode<TState> ZeroPageY =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.ZeroPageY;

        /// <summary>
        /// Absolute addressing mode.
        /// </summary>
        public AddressingMode<TState> Absolute =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.Absolute;

        /// <summary>
        /// Absolute,X addressing mode.
        /// </summary>
        public AddressingMode<TState> AbsoluteX =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.AbsoluteX;

        /// <summary>
        /// Absolute,Y addressing mode.
        /// </summary>
        public AddressingMode<TState> AbsoluteY =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.AbsoluteY;

        /// <summary>
        /// Indexed Indirect (Indirect,X) addressing mode.
        /// </summary>
        public AddressingMode<TState> IndirectX =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.IndirectX;

        /// <summary>
        /// Indirect Indexed (Indirect),Y addressing mode.
        /// </summary>
        public AddressingMode<TState> IndirectY =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.IndirectY;

        /// <summary>
        /// Absolute,X addressing mode for write operations.
        /// </summary>
        public AddressingMode<TState> AbsoluteXWrite =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.AbsoluteXWrite;

        /// <summary>
        /// Absolute,Y addressing mode for write operations.
        /// </summary>
        public AddressingMode<TState> AbsoluteYWrite =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.AbsoluteYWrite;

        /// <summary>
        /// Indirect Indexed (Indirect),Y addressing mode for write operations.
        /// </summary>
        public AddressingMode<TState> IndirectYWrite =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.IndirectYWrite;

        /// <summary>
        /// Accumulator addressing mode.
        /// </summary>
        public AddressingMode<TState> Accumulator =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.Accumulator;

        /// <summary>
        /// Relative addressing mode.
        /// </summary>
        public AddressingMode<TState> Relative =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.Relative;

        /// <summary>
        /// Indirect addressing mode.
        /// </summary>
        public AddressingMode<TState> Indirect =>
            AddressingModesFor<TRegisters, TAccumulator, TIndex, TStack, TProgram>.Indirect;
    }
}
