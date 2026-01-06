// <copyright file="IDebugContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Debug.Infrastructure;

using Bus.Interfaces;

using Core.Interfaces;
using Core.Interfaces.Cpu;

/// <summary>
/// Provides extended context for debug command execution including access to emulator components.
/// </summary>
/// <remarks>
/// The debug context extends <see cref="ICommandContext"/> to provide commands with
/// access to the CPU, memory bus, and disassembler for debugging operations.
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
    /// Gets the memory bus for the debug session.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides access to the page-based memory system including page table inspection
    /// and bus-level tracing. May be null if no bus has been attached to the debug context.
    /// </para>
    /// <para>
    /// Commands should check for null before accessing memory operations.
    /// </para>
    /// </remarks>
    IMemoryBus? Bus { get; }

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
    /// Returns true if CPU, Bus, and Disassembler are all available.
    /// </remarks>
    bool IsSystemAttached { get; }

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