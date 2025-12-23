// <copyright file="ICpuState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

/// <summary>
/// Represents the state of a CPU, encapsulating its registers, execution cycles,
/// halted status, and the reason for halting.
/// </summary>
public interface ICpuState
{
    /// <summary>
    /// Gets or sets the total number of cycles executed.
    /// </summary>
    ulong Cycles { get; set; }

    /// <summary>
    /// Gets a value indicating whether the CPU is halted.
    /// </summary>
    /// <remarks>
    /// This property returns true if the CPU is in any halt state.
    /// For more granular halt state information, use <see cref="HaltReason"/>.
    /// </remarks>
    bool Halted { get; }

    /// <summary>
    /// Gets or sets the reason the CPU is halted.
    /// </summary>
    /// <remarks>
    /// Distinguishes between different halt states such as BRK, WAI, STP, or running (None).
    /// This enables accurate emulation of hardware behavior for each halt condition.
    /// </remarks>
    HaltState HaltReason { get; set; }
}