// <copyright file="Cpu65816Registers.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Extended registers for 65816 mode.
/// </summary>
public class Cpu65816Registers : CpuRegisters
{
    /// <summary>16-bit Accumulator (65816 mode).</summary>
    public ushort C { get; set; }

    /// <summary>Direct Page Register.</summary>
    public ushort D { get; set; }

    /// <summary>Data Bank Register.</summary>
    public byte DBR { get; set; }

    /// <summary>Program Bank Register.</summary>
    public byte PBR { get; set; }

    /// <summary>Emulation mode flag.</summary>
    public bool EmulationMode { get; set; } = true;

    public new void Reset()
    {
        base.Reset();
        C = 0;
        D = 0;
        DBR = 0;
        PBR = 0;
        EmulationMode = true;
    }
}