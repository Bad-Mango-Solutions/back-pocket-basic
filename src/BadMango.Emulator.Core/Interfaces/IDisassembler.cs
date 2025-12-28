// <copyright file="IDisassembler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Interfaces;

/// <summary>
/// Defines the contract for disassembling machine code from memory into structured instruction representations.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface provide methods to walk through memory regions and decode machine code
/// into <see cref="DisassembledInstruction"/> objects. This abstraction allows for different disassembler
/// implementations targeting various CPU architectures while maintaining a consistent API.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// IDisassembler disassembler = GetDisassembler();
/// var instructions = disassembler.Disassemble(0x1000, 16);
/// foreach (var instr in instructions)
/// {
///     Console.WriteLine($"${instr.Address:X4}: {instr.FormatBytes(),-12} {instr.FormatInstruction()}");
/// }
/// </code>
/// </para>
/// </remarks>
public interface IDisassembler
{
    /// <summary>
    /// Disassembles a range of memory starting at the specified address.
    /// </summary>
    /// <param name="startAddress">The starting address to disassemble from.</param>
    /// <param name="byteCount">The maximum number of bytes to disassemble.</param>
    /// <returns>A list of <see cref="DisassembledInstruction"/> objects representing the decoded instructions.</returns>
    /// <remarks>
    /// The disassembler will stop when it has processed at least <paramref name="byteCount"/> bytes
    /// or when it encounters the end of a complete instruction that would exceed the byte count.
    /// Instructions are never truncated.
    /// </remarks>
    IReadOnlyList<DisassembledInstruction> Disassemble(uint startAddress, int byteCount);

    /// <summary>
    /// Disassembles a specific number of instructions starting at the specified address.
    /// </summary>
    /// <param name="startAddress">The starting address to disassemble from.</param>
    /// <param name="instructionCount">The number of instructions to disassemble.</param>
    /// <returns>A list of <see cref="DisassembledInstruction"/> objects representing the decoded instructions.</returns>
    IReadOnlyList<DisassembledInstruction> DisassembleInstructions(uint startAddress, int instructionCount);

    /// <summary>
    /// Disassembles a single instruction at the specified address.
    /// </summary>
    /// <param name="address">The address of the instruction to disassemble.</param>
    /// <returns>A <see cref="DisassembledInstruction"/> representing the decoded instruction.</returns>
    DisassembledInstruction DisassembleInstruction(uint address);

    /// <summary>
    /// Disassembles memory within a specified address range.
    /// </summary>
    /// <param name="startAddress">The starting address (inclusive).</param>
    /// <param name="endAddress">The ending address (exclusive).</param>
    /// <returns>A list of <see cref="DisassembledInstruction"/> objects representing the decoded instructions.</returns>
    /// <exception cref="ArgumentException">Thrown when endAddress is less than startAddress.</exception>
    IReadOnlyList<DisassembledInstruction> DisassembleRange(uint startAddress, uint endAddress);
}