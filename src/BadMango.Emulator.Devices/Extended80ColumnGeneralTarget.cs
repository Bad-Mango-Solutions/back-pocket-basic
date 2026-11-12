// <copyright file="Extended80ColumnGeneralTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Bus target for general memory pages ($1000-$BFFF) that handles RAMRD/RAMWRT-based auxiliary memory switching.
/// </summary>
/// <remarks>
/// <para>
/// On the Apple IIe Extended 80-Column Card, RAMRD and RAMWRT are independent: reads and writes
/// can target different banks simultaneously. Layer-based bus mapping cannot express that, so
/// this composite target routes each access based on the current soft-switch state, matching
/// <see cref="Extended80ColumnPage0Target"/> behavior for page-aligned regions.
/// </para>
/// <para>
/// Hi-res pages ($2000-$3FFF) may still be overridden by the AUX_HIRES1 layer when
/// 80STORE + PAGE2 + HIRES are all enabled.
/// </para>
/// </remarks>
public sealed class Extended80ColumnGeneralTarget : IBusTarget
{
    private readonly IBusTarget mainMemory;
    private readonly IBusTarget auxMemory;
    private readonly Extended80ColumnDevice device;
    private readonly Addr baseOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="Extended80ColumnGeneralTarget"/> class.
    /// </summary>
    /// <param name="mainMemory">The main memory target for $1000-$BFFF.</param>
    /// <param name="auxMemory">The auxiliary memory target for $1000-$BFFF.</param>
    /// <param name="device">The Extended 80-Column device that owns soft-switch state.</param>
    /// <param name="baseOffset">
    /// The base offset subtracted from physical addresses before passing to the backing targets.
    /// </param>
    public Extended80ColumnGeneralTarget(
        IBusTarget mainMemory,
        IBusTarget auxMemory,
        Extended80ColumnDevice device,
        Addr baseOffset = 0x1000)
    {
        ArgumentNullException.ThrowIfNull(mainMemory);
        ArgumentNullException.ThrowIfNull(auxMemory);
        ArgumentNullException.ThrowIfNull(device);

        this.mainMemory = mainMemory;
        this.auxMemory = auxMemory;
        this.device = device;
        this.baseOffset = baseOffset;
    }

    /// <inheritdoc />
    public string Name => "Extended 80-Column General RAM";

    /// <inheritdoc />
    public TargetCaps Capabilities => TargetCaps.SupportsPeek | TargetCaps.SupportsPoke;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        Addr targetOffset = physicalAddress - baseOffset;
        var target = device.IsRamRdEnabled ? auxMemory : mainMemory;
        return target.Read8(targetOffset, in access);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        Addr targetOffset = physicalAddress - baseOffset;
        var target = device.IsRamWrtEnabled ? auxMemory : mainMemory;
        target.Write8(targetOffset, value, in access);
    }
}