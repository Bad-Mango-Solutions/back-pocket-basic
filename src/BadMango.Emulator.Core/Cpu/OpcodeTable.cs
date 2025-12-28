// <copyright file="OpcodeTable.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

using System.Runtime.CompilerServices;

using Interfaces;

/// <summary>
/// Represents an opcode lookup table for 6502-family CPUs.
/// </summary>
/// <remarks>
/// This class encapsulates the opcode table, providing O(1) opcode dispatch.
/// The table maps 8-bit opcodes (0x00-0xFF) to instruction handlers.
/// Handlers receive the CPU state structure and memory interface for direct manipulation.
/// </remarks>
public class OpcodeTable
{
    private readonly OpcodeHandler[] handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpcodeTable"/> class.
    /// </summary>
    /// <param name="handlers">The array of opcode handlers. Must be exactly 256 elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when handlers is null.</exception>
    /// <exception cref="ArgumentException">Thrown when handlers array is not exactly 256 elements.</exception>
    public OpcodeTable(OpcodeHandler[] handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        if (handlers.Length != 256)
        {
            throw new ArgumentException("Opcode table must have exactly 256 entries.", nameof(handlers));
        }

        this.handlers = handlers;
    }

    /// <summary>
    /// Executes the handler for the specified opcode with machine state.
    /// </summary>
    /// <param name="opcode">The opcode to execute (0x00-0xFF).</param>
    /// <param name="memory">The memory interface.</param>
    /// <param name="state">Reference to the CPU state structure.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(byte opcode, IMemory memory, ref CpuState state)
    {
        handlers[opcode](memory, ref state);
    }

    /// <summary>
    /// Gets the handler for the specified opcode.
    /// </summary>
    /// <param name="opcode">The opcode to get the handler for (0x00-0xFF).</param>
    /// <returns>The handler for the specified opcode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OpcodeHandler GetHandler(byte opcode)
    {
        return handlers[opcode];
    }
}