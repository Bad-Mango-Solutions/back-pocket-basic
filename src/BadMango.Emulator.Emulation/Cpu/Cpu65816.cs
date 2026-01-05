// <copyright file="Cpu65816.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Emulation.Cpu;

using System.Diagnostics.CodeAnalysis;

using BadMango.Emulator.Bus.Interfaces;

using Core.Cpu;

/// <summary>
/// Placeholder for WDC 65816 CPU emulator (Apple IIgs processor).
/// </summary>
/// <remarks>
/// The 65816 features 16-bit registers, 24-bit addressing, and emulation mode
/// for backward compatibility with 6502 code. This will be the foundation for
/// Apple IIgs system emulation.
/// </remarks>
[ExcludeFromCodeCoverage]
public class Cpu65816 : CpuBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Cpu65816"/> class.
    /// </summary>
    /// <param name="context">The event context providing access to the memory bus, signal bus, and scheduler.</param>
    public Cpu65816(IEventContext context)
        : base(context)
    {
    }

    /// <inheritdoc/>
    public override CpuCapabilities Capabilities => CpuCapabilities.Base6502 |
                                           CpuCapabilities.SupportsEmulationFlag |
                                           CpuCapabilities.Supports16BitRegisters |
                                           CpuCapabilities.Supports65C02Instructions;

    /// <inheritdoc/>
    public override CpuStepResult Step()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override bool GetEmulationFlag() => Registers.E;
}