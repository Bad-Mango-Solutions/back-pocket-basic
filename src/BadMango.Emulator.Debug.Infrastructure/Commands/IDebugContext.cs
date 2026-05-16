// <copyright file="IDebugContext.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Debug.Infrastructure;
using BadMango.Emulator.Storage.Formats;

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
    /// Gets the composite CPU step listener that fans out debug callbacks to
    /// every registered observer (tracing, watchpoints, etc.).
    /// </summary>
    /// <remarks>
    /// May be <see langword="null"/> when no system is attached. Commands that want
    /// to observe instruction execution should add themselves here rather than
    /// calling <see cref="Core.Interfaces.Cpu.ICpu.AttachDebugger"/> directly, so
    /// they do not displace other observers.
    /// </remarks>
    CompositeDebugStepListener? StepListener { get; }

    /// <summary>
    /// Gets the breakpoint manager for the debug session.
    /// </summary>
    /// <remarks>
    /// Breakpoints are implemented as <see cref="Bus.TrapOperation.Call"/> traps
    /// on the active <see cref="Bus.Interfaces.ITrapRegistry"/>. Always non-<see langword="null"/>
    /// on a fresh <see cref="DebugContext"/>; the manager is fully wired only after
    /// a system is attached.
    /// </remarks>
    BreakpointManager Breakpoints { get; }

    /// <summary>
    /// Gets the watchpoint manager for the debug session.
    /// </summary>
    /// <remarks>
    /// Watchpoints are implemented as a <see cref="Core.Interfaces.Debugging.IDebugStepListener"/>
    /// that inspects each instruction's effective address. Always non-<see langword="null"/>
    /// on a fresh <see cref="DebugContext"/>; the manager is fully wired only after
    /// a system is attached.
    /// </remarks>
    WatchpointManager Watchpoints { get; }

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

    /// <summary>
    /// Gets the path resolver for resolving file paths in debug commands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The path resolver supports <c>library://</c> and <c>app://</c> URI schemes,
    /// as well as absolute and relative file paths.
    /// </para>
    /// <para>
    /// May be null if no path resolver has been attached to the debug context.
    /// Commands should check for null before using path resolution, or handle
    /// paths as literal file system paths when the resolver is unavailable.
    /// </para>
    /// </remarks>
    IDebugPathResolver? PathResolver { get; }

    /// <summary>
    /// Gets the disk image factory used by debug commands that operate on disk images
    /// (e.g. <c>disk create</c> / <c>disk info</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The factory exposes the same format detection and opener that runtime controllers
    /// use, so that images authored by debug commands round-trip through the same code
    /// path that real mounts use.
    /// </para>
    /// <para>
    /// May be null if no factory has been attached to the debug context. Commands
    /// should check for null and report a clear error in that case.
    /// </para>
    /// </remarks>
    DiskImageFactory? DiskImageFactory { get; }

    /// <summary>
    /// Gets the registry that owns the <see cref="DiskImageOpenResult"/> handles produced
    /// by the runtime <c>disk insert</c> path so their underlying file backends can be
    /// released on eject, re-mount, or context teardown instead of leaking until process
    /// exit.
    /// </summary>
    /// <value>
    /// The mounted-disk registry. Always non-<see langword="null"/> on a fresh
    /// <see cref="DebugContext"/>; lifetime follows the context itself.
    /// </value>
    MountedDiskRegistry MountedDisks { get; }
}