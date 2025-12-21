// <copyright file="Cpu65816Registers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Extended registers for 65816 mode.
/// </summary>
[ExcludeFromCodeCoverage]
public class Cpu65816Registers : Cpu6502Registers
{
    /// <summary>16-bit Accumulator (65816 mode).</summary>
    public ushort C { get; set; }

    /// <summary>Direct Page Register.</summary>
    public ushort DP { get; set; }

    /// <summary>Data Bank Register.</summary>
    public byte DBR { get; set; }

    /// <summary>Program Bank Register.</summary>
    public byte PBR { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the CPU is operating in emulation mode.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the CPU operates in 6502 emulation mode, which restricts
    /// certain features and behaviors to match the original 6502 processor. When set to
    /// <c>false</c>, the CPU operates in full 65816 mode, enabling extended functionality.
    /// </remarks>
    public bool EmulationMode { get; set; } = true;

    /// <summary>
    /// Resets the 65816 CPU registers to their default state.
    /// </summary>
    /// <remarks>
    /// This method overrides the base <see cref="Cpu6502Registers.Reset"/> method to reset additional
    /// registers specific to the 65816 CPU mode:
    /// <list type="bullet">
    /// <item><description><see cref="C"/> and <see cref="DP"/> are set to 0.</description></item>
    /// <item><description><see cref="DBR"/> (Data Bank Register) and <see cref="PBR"/> (Program Bank Register) are set to 0.</description></item>
    /// <item><description><see cref="EmulationMode"/> is set to <c>true</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to reset the 65816 CPU registers:
    /// <code>
    /// var registers = new Cpu65816Registers();
    /// registers.Reset();
    /// </code>
    /// </example>
    public override void Reset()
    {
        base.Reset();
        C = 0;
        DP = 0;
        DBR = 0;
        PBR = 0;
        EmulationMode = true;
    }
}