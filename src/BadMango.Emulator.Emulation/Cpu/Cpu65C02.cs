// <copyright file="Cpu65C02.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using BadMango.Emulator.Core;

/// <summary>
/// Placeholder for WDC 65C02 CPU emulator.
/// </summary>
/// <remarks>
/// The 65C02 is an enhanced version of the 6502 with additional instructions,
/// addressing modes, and bug fixes. This class will implement the full 65C02 instruction set.
/// </remarks>
public class Cpu65C02 : ICpu
{
    /// <inheritdoc/>
    public bool Halted => throw new NotImplementedException();

    /// <inheritdoc/>
    public int Step()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Execute(int startAddress)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Reset()
    {
        throw new NotImplementedException();
    }
}