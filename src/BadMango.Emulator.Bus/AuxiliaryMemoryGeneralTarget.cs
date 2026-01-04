// <copyright file="AuxiliaryMemoryGeneralTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Bus target for general memory pages ($1000-$BFFF) that handles RAMRD/RAMWRT-based auxiliary memory switching.
/// </summary>
/// <remarks>
/// <para>
/// This target handles auxiliary memory switching for the general RAM region controlled by RAMRD/RAMWRT.
/// It does NOT handle:
/// </para>
/// <list type="bullet">
/// <item><description>Page 0 ($0000-$0FFF): Handled by <see cref="AuxiliaryMemoryPage0Target"/></description></item>
/// <item><description>Hi-res pages ($2000-$5FFF): Handled by layers when 80STORE + HIRES + PAGE2 enabled</description></item>
/// </list>
/// <para>
/// For reads, RAMRD controls whether main or auxiliary memory is accessed.
/// For writes, RAMWRT controls whether main or auxiliary memory is accessed.
/// </para>
/// </remarks>
public sealed class AuxiliaryMemoryGeneralTarget : IBusTarget
{
    private readonly IBusTarget mainMemory;
    private readonly IBusTarget auxMemory;
    private readonly AuxiliaryMemoryController controller;
    private readonly Addr baseOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuxiliaryMemoryGeneralTarget"/> class.
    /// </summary>
    /// <param name="mainMemory">The main memory target.</param>
    /// <param name="auxMemory">The auxiliary memory target.</param>
    /// <param name="controller">The auxiliary memory controller that manages switch states.</param>
    /// <param name="baseOffset">
    /// The base offset to subtract from physical addresses before passing to the targets.
    /// This allows the targets to use zero-based addressing within their allocated memory.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <see langword="null"/>.
    /// </exception>
    public AuxiliaryMemoryGeneralTarget(
        IBusTarget mainMemory,
        IBusTarget auxMemory,
        AuxiliaryMemoryController controller,
        Addr baseOffset = 0)
    {
        ArgumentNullException.ThrowIfNull(mainMemory);
        ArgumentNullException.ThrowIfNull(auxMemory);
        ArgumentNullException.ThrowIfNull(controller);

        this.mainMemory = mainMemory;
        this.auxMemory = auxMemory;
        this.controller = controller;
        this.baseOffset = baseOffset;
    }

    /// <summary>
    /// Gets the name of this target.
    /// </summary>
    /// <value>A human-readable name for the target, used for diagnostics and debugging.</value>
    public string Name => "Auxiliary Memory General";

    /// <inheritdoc />
    public TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsPoke;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        Addr targetOffset = physicalAddress - baseOffset;
        var target = controller.IsRamRdEnabled ? auxMemory : mainMemory;
        return target.Read8(targetOffset, in access);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        Addr targetOffset = physicalAddress - baseOffset;
        var target = controller.IsRamWrtEnabled ? auxMemory : mainMemory;
        target.Write8(targetOffset, value, in access);
    }
}