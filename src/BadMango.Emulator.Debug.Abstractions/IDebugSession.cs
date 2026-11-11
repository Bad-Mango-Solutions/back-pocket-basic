// <copyright file="IDebugSession.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// Defines the core contract for a debug session that can query and control emulator state.
/// </summary>
public interface IDebugSession
{
    /// <summary>
    /// Gets the current register snapshot for the active machine.
    /// </summary>
    /// <returns>The register snapshot.</returns>
    RegisterSnapshot GetRegisters();

    /// <summary>
    /// Reads a memory block from the machine.
    /// </summary>
    /// <param name="address">The memory address to read from.</param>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>A memory block containing the requested bytes.</returns>
    MemoryBlock ReadMemory(uint address, int length);

    /// <summary>
    /// Writes a memory block into the machine.
    /// </summary>
    /// <param name="address">The memory address to write to.</param>
    /// <param name="data">The bytes to write.</param>
    void WriteMemory(uint address, ReadOnlySpan<byte> data);

    /// <summary>
    /// Disassembles a sequence of instructions from the current machine state.
    /// </summary>
    /// <param name="address">The starting execution address.</param>
    /// <param name="count">The number of lines to disassemble.</param>
    /// <returns>The disassembly lines.</returns>
    IReadOnlyList<DisassemblyLine> Disassemble(uint address, int count);

    /// <summary>
    /// Steps the CPU for a number of instructions.
    /// </summary>
    /// <param name="count">The number of instructions to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of instructions processed.</returns>
    ulong Step(int count = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the machine until the provided condition is met or the budget is exhausted.
    /// </summary>
    /// <param name="condition">The completion condition.</param>
    /// <param name="budget">The execution budget.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The execution result.</returns>
    RunResult RunUntil(RunCondition condition, RunBudget budget, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the running machine.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resets the machine by the supplied reset type.
    /// </summary>
    /// <param name="resetKind">The reset mode.</param>
    void Reset(ResetKind resetKind);

    /// <summary>
    /// Adds a breakpoint at the supplied address.
    /// </summary>
    /// <param name="address">The breakpoint address.</param>
    /// <param name="label">An optional label.</param>
    void AddBreakpoint(uint address, string? label = null);

    /// <summary>
    /// Removes a breakpoint.
    /// </summary>
    /// <param name="address">The breakpoint address.</param>
    /// <returns>True when the breakpoint existed and was removed.</returns>
    bool RemoveBreakpoint(uint address);

    /// <summary>
    /// Gets all breakpoints currently configured.
    /// </summary>
    /// <returns>The current breakpoint list.</returns>
    IReadOnlyList<Breakpoint> GetBreakpoints();

    /// <summary>
    /// Clears all breakpoints.
    /// </summary>
    void ClearBreakpoints();

    /// <summary>
    /// Adds a memory watchpoint.
    /// </summary>
    /// <param name="address">The watch address.</param>
    /// <param name="access">The access type to watch.</param>
    /// <param name="stopOnHit">Whether execution should stop on hit.</param>
    /// <param name="label">An optional label.</param>
    void AddWatchpoint(uint address, WatchAccess access = WatchAccess.ReadWrite, bool stopOnHit = false, string? label = null);

    /// <summary>
    /// Removes a watchpoint.
    /// </summary>
    /// <param name="address">The watchpoint address.</param>
    /// <returns>True when the watchpoint existed and was removed.</returns>
    bool RemoveWatchpoint(uint address);

    /// <summary>
    /// Gets all currently configured watchpoints.
    /// </summary>
    /// <returns>The current watchpoint list.</returns>
    IReadOnlyList<Watchpoint> GetWatchpoints();

    /// <summary>
    /// Clears all watchpoints.
    /// </summary>
    void ClearWatchpoints();

    /// <summary>
    /// Gets the metadata describing the current machine.
    /// </summary>
    /// <returns>The machine information.</returns>
    MachineInfo GetMachineInfo();
}