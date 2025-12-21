// <copyright file="OpcodeTable.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Runtime.CompilerServices;

/// <summary>
/// Represents an opcode lookup table for 6502-family CPUs.
/// </summary>
/// <typeparam name="TCpu">The CPU type this table is associated with.</typeparam>
/// <remarks>
/// This class encapsulates the opcode table, providing O(1) opcode dispatch.
/// The table maps 8-bit opcodes (0x00-0xFF) to instruction handlers.
/// </remarks>
public class OpcodeTable<TCpu>
{
    private readonly Action<TCpu>[] handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpcodeTable{TCpu}"/> class.
    /// </summary>
    /// <param name="handlers">The array of opcode handlers. Must be exactly 256 elements.</param>
    /// <exception cref="ArgumentNullException">Thrown when handlers is null.</exception>
    /// <exception cref="ArgumentException">Thrown when handlers array is not exactly 256 elements.</exception>
    public OpcodeTable(Action<TCpu>[] handlers)
    {
        if (handlers == null)
        {
            throw new ArgumentNullException(nameof(handlers));
        }

        if (handlers.Length != 256)
        {
            throw new ArgumentException("Opcode table must have exactly 256 entries.", nameof(handlers));
        }

        this.handlers = handlers;
    }

    /// <summary>
    /// Executes the handler for the specified opcode.
    /// </summary>
    /// <param name="opcode">The opcode to execute (0x00-0xFF).</param>
    /// <param name="cpu">The CPU instance to pass to the handler.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(byte opcode, TCpu cpu)
    {
        handlers[opcode](cpu);
    }

    /// <summary>
    /// Gets the handler for the specified opcode.
    /// </summary>
    /// <param name="opcode">The opcode to get the handler for (0x00-0xFF).</param>
    /// <returns>The handler for the specified opcode.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Action<TCpu> GetHandler(byte opcode)
    {
        return handlers[opcode];
    }
}