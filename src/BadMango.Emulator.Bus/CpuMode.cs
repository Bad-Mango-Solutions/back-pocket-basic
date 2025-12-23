// <copyright file="CpuMode.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Defines the CPU execution mode for bus access operations.
/// </summary>
/// <remarks>
/// The CPU mode determines the default behavior for memory access operations.
/// In Native mode (65832), atomic wide accesses are preferred when supported.
/// In Compat mode (Apple II), byte-decomposed accesses are the default for
/// visibility into individual bus cycles.
/// </remarks>
public enum CpuMode : byte
{
    /// <summary>
    /// Native 65832 mode with full 32-bit capabilities.
    /// </summary>
    /// <remarks>
    /// In Native mode, the bus prefers atomic wide operations when the target supports them,
    /// providing better performance for modern memory operations.
    /// </remarks>
    Native,

    /// <summary>
    /// Compatibility mode for Apple II-like semantics.
    /// </summary>
    /// <remarks>
    /// In Compat mode, the bus defaults to decomposed byte-wise cycles, matching the
    /// behavior expected by Apple II software and peripherals that rely on observing
    /// individual memory access cycles.
    /// </remarks>
    Compat,
}