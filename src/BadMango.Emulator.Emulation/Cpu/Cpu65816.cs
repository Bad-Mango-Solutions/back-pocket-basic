// <copyright file="Cpu65816.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Diagnostics.CodeAnalysis;

using Core;

/// <summary>
/// Placeholder for WDC 65816 CPU emulator (Apple IIgs processor).
/// </summary>
/// <remarks>
/// The 65816 features 16-bit registers, 24-bit addressing, and emulation mode
/// for backward compatibility with 6502 code. This will be the foundation for
/// Apple IIgs system emulation.
/// </remarks>
[ExcludeFromCodeCoverage]
public class Cpu65816 : ICpu<Cpu65816Registers, Cpu65816State>
{
    /// <inheritdoc/>
    public bool Halted => throw new NotImplementedException();

    /// <inheritdoc/>
    public int Step()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Execute(uint startAddress)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Cpu65816Registers GetRegisters()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Cpu65816State GetState()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SetState(Cpu65816State state)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SignalIRQ()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SignalNMI()
    {
        throw new NotImplementedException();
    }
}