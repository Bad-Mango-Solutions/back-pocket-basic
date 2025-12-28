// <copyright file="HaltState.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Cpu;

/// <summary>
/// Represents the halt state of the CPU.
/// </summary>
/// <remarks>
/// The 65C02 CPU has different halt states with different resume conditions:
/// - None: CPU is running normally
/// - Wai: CPU halted by WAI instruction - resumes on IRQ or NMI
/// - Stp: CPU halted by STP instruction - resumes only on hardware RESET.
/// </remarks>
public enum HaltState : byte
{
    /// <summary>
    /// CPU is running normally.
    /// </summary>
    None = 0,

    /// <summary>
    /// CPU halted by WAI (Wait for Interrupt) instruction.
    /// </summary>
    /// <remarks>
    /// WAI puts the processor into a low-power state until an interrupt occurs.
    /// Resumes execution on IRQ (if I flag is clear) or NMI.
    /// </remarks>
    Wai = 1,

    /// <summary>
    /// CPU halted by STP (Stop) instruction.
    /// </summary>
    /// <remarks>
    /// STP stops the processor permanently until a hardware reset occurs.
    /// This is the deepest halt state and cannot be resumed by interrupts.
    /// </remarks>
    Stp = 2,
}