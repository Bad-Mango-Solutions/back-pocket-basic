// <copyright file="RegionTag.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Identifies the type of memory region for a page entry.
/// </summary>
/// <remarks>
/// Region tags provide semantic classification for memory pages, enabling
/// tracing tools, debuggers, and policy enforcement to understand the nature
/// of different address ranges without querying individual devices.
/// </remarks>
public enum RegionTag : ushort
{
    /// <summary>
    /// Unknown or unclassified region.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Random Access Memory region.
    /// </summary>
    /// <remarks>
    /// Standard read/write memory. Typically supports Peek/Poke and atomic wide access.
    /// </remarks>
    Ram = 1,

    /// <summary>
    /// Read-Only Memory region.
    /// </summary>
    /// <remarks>
    /// Memory that can be read but not written during normal operation.
    /// Supports Peek but writes are typically ignored or cause bus errors.
    /// </remarks>
    Rom = 2,

    /// <summary>
    /// Memory-mapped I/O region.
    /// </summary>
    /// <remarks>
    /// Address range used for device control and status registers.
    /// Typically has side effects on access. Supports Peek/Poke but may have
    /// additional constraints or behaviors.
    /// </remarks>
    Io = 3,

    /// <summary>
    /// Expansion slot region.
    /// </summary>
    /// <remarks>
    /// Address range associated with an expansion slot. Access is routed
    /// through the slot multiplexer to the installed card.
    /// </remarks>
    Slot = 4,

    /// <summary>
    /// Shadowed or overlaid memory region.
    /// </summary>
    /// <remarks>
    /// Memory that can switch between different backing stores (e.g., ROM/RAM
    /// switching, language card overlay). The active backing is determined
    /// by soft switch state.
    /// </remarks>
    Shadow = 5,

    /// <summary>
    /// Unmapped or non-existent region.
    /// </summary>
    /// <remarks>
    /// Address range with no backing memory or device. Reads may return
    /// floating bus values; writes are ignored.
    /// </remarks>
    Unmapped = 6,

    /// <summary>
    /// Video display memory region.
    /// </summary>
    /// <remarks>
    /// Memory used by the video display system. May have timing-sensitive
    /// behavior and special access patterns.
    /// </remarks>
    Video = 7,

    /// <summary>
    /// Zero page region.
    /// </summary>
    /// <remarks>
    /// The first 256 bytes of memory, used for fast access variables
    /// and indirect addressing on 6502-family processors.
    /// </remarks>
    ZeroPage = 8,

    /// <summary>
    /// Hardware stack region.
    /// </summary>
    /// <remarks>
    /// Memory used by the hardware stack (typically $0100-$01FF on 6502).
    /// </remarks>
    Stack = 9,

    /// <summary>
    /// Composite region.
    /// </summary>
    /// <remarks>
    /// Memory region that combines multiple underlying regions, typically with
    /// different access permissions or behaviors.
    /// </remarks>
    Composite = 10,
}