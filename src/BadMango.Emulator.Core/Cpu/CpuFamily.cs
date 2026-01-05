// <copyright file="CpuFamily.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

/// <summary>
/// Identifies the hardware CPU variant being emulated.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CpuFamily"/> describes the physical chip that is "soldered to the board" -
/// it's the hardware identity, not the runtime mode. For example, a 65C816 chip always
/// reports <see cref="Cpu65C816"/> even when running in 6502 emulation mode.
/// </para>
/// <para>
/// This is distinct from <see cref="ArchitecturalMode"/>, which describes the CPU's
/// current runtime state (emulation mode vs native mode, 8-bit vs 16-bit registers).
/// </para>
/// <para>
/// Use this enum for:
/// <list type="bullet">
/// <item><description>Selecting the correct opcode table and instruction set</description></item>
/// <item><description>Determining available addressing modes</description></item>
/// <item><description>Machine profile validation</description></item>
/// <item><description>Display in debugger UI</description></item>
/// </list>
/// </para>
/// </remarks>
public enum CpuFamily
{
    /// <summary>
    /// WDC 65C02 - Enhanced 8-bit processor.
    /// </summary>
    /// <remarks>
    /// Target CPU for Pocket2e (Apple IIe clone) configurations.
    /// Supports additional instructions over the original 6502.
    /// </remarks>
    Cpu65C02,

    /// <summary>
    /// WDC 65C816 - 16-bit processor with 24-bit address bus.
    /// </summary>
    /// <remarks>
    /// Target CPU for PocketGS (Apple IIgs clone) configurations.
    /// Supports both 6502 emulation mode and native 16-bit mode.
    /// </remarks>
    Cpu65C816,

    /// <summary>
    /// Speculative 65832 - 32-bit processor with flat addressing.
    /// </summary>
    /// <remarks>
    /// Target CPU for PocketME (hypothetical modern evolution) configurations.
    /// Extends the 65xx architecture to 32-bit registers and flat 4GB addressing.
    /// </remarks>
    Cpu65832,
}