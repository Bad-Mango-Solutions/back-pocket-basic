// <copyright file="IDebugContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Debug.Infrastructure;

/// <summary>
/// Provides extended context for debug command execution including access to emulator components.
/// </summary>
/// <remarks>
/// The debug context extends <see cref="ICommandContext"/> to provide commands with
/// access to the CPU, memory, and disassembler for debugging operations.
/// </remarks>
public interface IDebugContext : ICommandContext
{
    /// <summary>
    /// Gets the CPU instance for the debug session.
    /// </summary>
    /// <remarks>
    /// May be null if no CPU has been attached to the debug context.
    /// Commands should check for null before accessing CPU operations.
    /// </remarks>
    ICpu? Cpu { get; }

    /// <summary>
    /// Gets the memory instance for the debug session.
    /// </summary>
    /// <remarks>
    /// May be null if no memory has been attached to the debug context.
    /// Commands should check for null before accessing memory operations.
    /// </remarks>
    IMemory? Memory { get; }

    /// <summary>
    /// Gets the disassembler instance for the debug session.
    /// </summary>
    /// <remarks>
    /// May be null if no disassembler has been attached to the debug context.
    /// Commands should check for null before accessing disassembly operations.
    /// </remarks>
    IDisassembler? Disassembler { get; }

    /// <summary>
    /// Gets the machine information for the current debug session.
    /// </summary>
    /// <remarks>
    /// Provides display-friendly information about the attached machine configuration,
    /// including CPU type, memory size, and display name. May be null if no system
    /// has been attached to the debug context.
    /// </remarks>
    MachineInfo? MachineInfo { get; }

    /// <summary>
    /// Gets the tracing debug listener for the debug session.
    /// </summary>
    /// <remarks>
    /// The tracing listener can be used to capture instruction execution traces
    /// for debugging and analysis. May be null if no listener has been configured.
    /// </remarks>
    TracingDebugListener? TracingListener { get; }

    /// <summary>
    /// Gets a value indicating whether a system is attached to this debug context.
    /// </summary>
    /// <remarks>
    /// Returns true if CPU, Memory, and Disassembler are all available.
    /// </remarks>
    bool IsSystemAttached { get; }

    /// <summary>
    /// Gets the memory bus for bus-aware debugging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When non-null, provides access to the page-based memory system
    /// including page table inspection and bus-level tracing.
    /// </para>
    /// <para>
    /// Legacy systems may have Memory but not Bus. New systems using the
    /// bus architecture will have both - Memory may be a <see cref="MemoryBusAdapter"/>
    /// wrapping the Bus for backward compatibility with existing debug commands.
    /// </para>
    /// </remarks>
    IMemoryBus? Bus { get; }

    /// <summary>
    /// Gets the machine instance for high-level machine control.
    /// </summary>
    /// <remarks>
    /// When non-null, provides access to Run/Step/Reset through the
    /// machine abstraction rather than direct CPU manipulation.
    /// </remarks>
    IMachine? Machine { get; }

    /// <summary>
    /// Gets a value indicating whether bus-level debugging is available.
    /// </summary>
    /// <remarks>
    /// Returns true if a memory bus has been attached to the debug context.
    /// When true, bus-level debugging capabilities such as page table inspection
    /// and bus-level tracing are available.
    /// </remarks>
    bool IsBusAttached { get; }
}