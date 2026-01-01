// <copyright file="Cpu65816.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Diagnostics.CodeAnalysis;

using Core.Cpu;
using Core.Interfaces.Cpu;
using Core.Interfaces.Debugging;

/// <summary>
/// Placeholder for WDC 65816 CPU emulator (Apple IIgs processor).
/// </summary>
/// <remarks>
/// The 65816 features 16-bit registers, 24-bit addressing, and emulation mode
/// for backward compatibility with 6502 code. This will be the foundation for
/// Apple IIgs system emulation.
/// </remarks>
[ExcludeFromCodeCoverage]
public class Cpu65816 : ICpu
{
    /// <inheritdoc/>
    public CpuCapabilities Capabilities => CpuCapabilities.Base6502 |
                                           CpuCapabilities.SupportsEmulationFlag |
                                           CpuCapabilities.Supports16BitRegisters |
                                           CpuCapabilities.Supports65C02Instructions;

    /// <inheritdoc/>
    public bool Halted => throw new NotImplementedException();

    /// <inheritdoc/>
    public HaltState HaltReason
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

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

    /// <inheritdoc/>
    public ref CpuState State => throw new NotImplementedException();

    /// <inheritdoc/>
    public byte Read8(Addr address)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Write8(Addr address, byte value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Word Read16(Addr address)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Write16(Addr address, Word value)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public DWord ReadValue(Addr address, byte sizeInBits)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void WriteValue(Addr address, DWord value, byte sizeInBits)
    {
        throw new NotImplementedException();
    }
}