// <copyright file="Cpu65832.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Diagnostics.CodeAnalysis;

using Core;

/// <summary>
/// Placeholder for hypothetical 65832 CPU emulator (32-bit extension).
/// </summary>
/// <remarks>
/// The 65832 is a conceptual 32-bit extension of the 65816 architecture,
/// exploring what a modern evolution of the 6502 family might look like
/// while maintaining backward compatibility principles.
/// </remarks>
[ExcludeFromCodeCoverage]
public class Cpu65832 : ICpu
{
    /// <inheritdoc/>
    public bool Halted => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool IsDebuggerAttached => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool IsStopRequested => throw new NotImplementedException();

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
    public Registers GetRegisters()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public ref CpuState GetState()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SetState(CpuState state)
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

    /// <inheritdoc/>
    public void AttachDebugger(IDebugStepListener listener)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void DetachDebugger()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void SetPC(Addr address)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Addr GetPC()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void RequestStop()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void ClearStopRequest()
    {
        throw new NotImplementedException();
    }
}