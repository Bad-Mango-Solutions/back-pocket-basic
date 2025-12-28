// <copyright file="IMachine.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Core;

using Core.Interfaces.Cpu;

/// <summary>
/// Base interface for an assembled emulator machine.
/// Provides access to core components for debugging and control.
/// </summary>
/// <remarks>
/// <para>
/// The machine interface provides a unified abstraction for the assembled emulator system.
/// It exposes the CPU and memory bus for high-level machine control and debugging.
/// </para>
/// <para>
/// This minimal interface supports Phase D1 of the debugger infrastructure migration,
/// enabling bus-aware debugging while maintaining backward compatibility with existing
/// debug commands that access CPU and memory directly.
/// </para>
/// </remarks>
public interface IMachine
{
    /// <summary>
    /// Gets the CPU instance.
    /// </summary>
    /// <value>The CPU attached to this machine.</value>
    ICpu Cpu { get; }

    /// <summary>
    /// Gets the main memory bus.
    /// </summary>
    /// <value>The memory bus that routes all memory operations.</value>
    IMemoryBus Bus { get; }
}