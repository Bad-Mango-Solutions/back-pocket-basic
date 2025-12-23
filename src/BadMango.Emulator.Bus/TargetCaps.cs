// <copyright file="TargetCaps.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Capability flags that describe what a bus target supports.
/// </summary>
/// <remarks>
/// Target capabilities are used by the bus to determine how to route
/// and execute memory operations. They influence decisions about atomic
/// vs decomposed access, side-effect handling, and timing sensitivity.
/// </remarks>
[Flags]
public enum TargetCaps : uint
{
    /// <summary>
    /// No special capabilities; basic byte-wise access only.
    /// </summary>
    None = 0,

    /// <summary>
    /// Target supports Peek operations (reads without side effects).
    /// </summary>
    /// <remarks>
    /// When set, the target can safely be read by debuggers and disassemblers
    /// without triggering soft switches, clearing interrupt flags, or causing
    /// other observable state changes. RAM and ROM typically support this.
    /// </remarks>
    SupportsPeek = 1 << 0,

    /// <summary>
    /// Target supports Poke operations (writes without side effects).
    /// </summary>
    /// <remarks>
    /// When set, the target can be written by debuggers without triggering
    /// I/O behavior. This is typically supported by RAM but not by I/O devices
    /// where any write may cause state changes.
    /// </remarks>
    SupportsPoke = 1 << 1,

    /// <summary>
    /// Target supports atomic 16-bit and 32-bit operations.
    /// </summary>
    /// <remarks>
    /// When set, the target can safely handle multi-byte reads and writes
    /// as single atomic transactions. RAM typically supports this. I/O regions
    /// often do not, as individual byte accesses may need to be visible.
    /// </remarks>
    SupportsWide = 1 << 2,

    /// <summary>
    /// Target has side effects on read and/or write operations.
    /// </summary>
    /// <remarks>
    /// When set, indicates that accessing this target may toggle soft switches,
    /// clear interrupt flags, advance device state, or cause other observable
    /// effects. This flag guides the bus in choosing Peek/Poke vs normal access.
    /// </remarks>
    HasSideEffects = 1 << 3,

    /// <summary>
    /// Target is sensitive to access timing.
    /// </summary>
    /// <remarks>
    /// When set, the target behavior may depend on the exact cycle timing of
    /// accesses. This is important for devices like video circuits and disk
    /// controllers where cycle-accurate timing affects behavior.
    /// </remarks>
    TimingSensitive = 1 << 4,
}