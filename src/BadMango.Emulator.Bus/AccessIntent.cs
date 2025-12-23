// <copyright file="AccessIntent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Defines the intent or purpose of a bus access operation.
/// </summary>
/// <remarks>
/// The access intent allows the bus and connected devices to understand
/// the context of a memory operation, enabling appropriate behavior for
/// data reads, instruction fetches, DMA operations, and debugging tools.
/// </remarks>
public enum AccessIntent : byte
{
    /// <summary>
    /// Standard data read operation from the CPU.
    /// </summary>
    /// <remarks>
    /// Used for reading operands, variables, and other data during
    /// normal program execution. May trigger side effects in I/O regions.
    /// </remarks>
    DataRead,

    /// <summary>
    /// Standard data write operation from the CPU.
    /// </summary>
    /// <remarks>
    /// Used for storing results, updating variables, and writing to
    /// memory-mapped I/O. May trigger side effects in I/O regions.
    /// </remarks>
    DataWrite,

    /// <summary>
    /// Instruction fetch from the CPU.
    /// </summary>
    /// <remarks>
    /// Used when the CPU is fetching opcode bytes or instruction operands.
    /// This allows devices to differentiate between data and code accesses,
    /// which is important for instruction tracing and some ROM behaviors.
    /// </remarks>
    InstructionFetch,

    /// <summary>
    /// Debug read operation without side effects.
    /// </summary>
    /// <remarks>
    /// Used by debuggers, disassemblers, and inspection tools to read
    /// memory without triggering soft switch toggles, clearing interrupt
    /// flags, or advancing device state. Equivalent to a "Peek" operation.
    /// </remarks>
    DebugRead,

    /// <summary>
    /// Debug write operation without side effects.
    /// </summary>
    /// <remarks>
    /// Used by debuggers to patch memory without triggering I/O behavior.
    /// Equivalent to a "Poke" operation. Should be used carefully as it
    /// bypasses normal device state management.
    /// </remarks>
    DebugWrite,

    /// <summary>
    /// DMA read operation.
    /// </summary>
    /// <remarks>
    /// Used when a DMA controller is reading memory on behalf of a device.
    /// Maintains proper source identification for tracing and allows
    /// devices to respond appropriately to DMA traffic.
    /// </remarks>
    DmaRead,

    /// <summary>
    /// DMA write operation.
    /// </summary>
    /// <remarks>
    /// Used when a DMA controller is writing memory on behalf of a device.
    /// Maintains proper source identification for tracing and allows
    /// devices to respond appropriately to DMA traffic.
    /// </remarks>
    DmaWrite,
}