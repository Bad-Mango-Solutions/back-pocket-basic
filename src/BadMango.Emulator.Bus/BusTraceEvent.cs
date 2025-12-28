// <copyright file="BusTraceEvent.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.InteropServices;

/// <summary>
/// Compact trace event for bus access logging.
/// </summary>
/// <remarks>
/// <para>
/// This structure is designed for efficient ring buffer storage with no allocations.
/// It captures all relevant information about a bus access for debugging, profiling,
/// and observability purposes.
/// </para>
/// <para>
/// The structure uses explicit layout to ensure consistent memory footprint
/// regardless of platform alignment rules.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct BusTraceEvent
{
    /// <summary>
    /// The cycle when this access occurred.
    /// </summary>
    public readonly ulong Cycle;

    /// <summary>
    /// The address being accessed.
    /// </summary>
    public readonly uint Address;

    /// <summary>
    /// The value read or written.
    /// </summary>
    public readonly uint Value;

    /// <summary>
    /// The access width in bits (8, 16, or 32).
    /// </summary>
    public readonly byte WidthBits;

    /// <summary>
    /// The type of access (read, write, fetch, debug).
    /// </summary>
    public readonly AccessIntent Intent;

    /// <summary>
    /// Access flags (decompose, atomic, no side effects).
    /// </summary>
    public readonly AccessFlags Flags;

    /// <summary>
    /// The source identifier (CPU=0, DMA channels, debugger).
    /// </summary>
    public readonly int SourceId;

    /// <summary>
    /// The device that handled this access.
    /// </summary>
    public readonly int DeviceId;

    /// <summary>
    /// The region tag for the accessed page.
    /// </summary>
    public readonly RegionTag RegionTag;

    /// <summary>
    /// Initializes a new instance of the <see cref="BusTraceEvent"/> struct.
    /// </summary>
    /// <param name="cycle">The cycle when this access occurred.</param>
    /// <param name="address">The address being accessed.</param>
    /// <param name="value">The value read or written.</param>
    /// <param name="widthBits">The access width in bits.</param>
    /// <param name="intent">The type of access.</param>
    /// <param name="flags">Access flags.</param>
    /// <param name="sourceId">The source identifier.</param>
    /// <param name="deviceId">The device that handled this access.</param>
    /// <param name="regionTag">The region tag for the accessed page.</param>
    public BusTraceEvent(
        ulong cycle,
        uint address,
        uint value,
        byte widthBits,
        AccessIntent intent,
        AccessFlags flags,
        int sourceId,
        int deviceId,
        RegionTag regionTag)
    {
        Cycle = cycle;
        Address = address;
        Value = value;
        WidthBits = widthBits;
        Intent = intent;
        Flags = flags;
        SourceId = sourceId;
        DeviceId = deviceId;
        RegionTag = regionTag;
    }

    /// <summary>
    /// Creates a trace event from a bus access and its result.
    /// </summary>
    /// <param name="access">The bus access context.</param>
    /// <param name="value">The value read or written.</param>
    /// <param name="deviceId">The device that handled the access.</param>
    /// <param name="regionTag">The region tag.</param>
    /// <returns>A new <see cref="BusTraceEvent"/> capturing the access.</returns>
    public static BusTraceEvent FromAccess(
        in BusAccess access,
        uint value,
        int deviceId,
        RegionTag regionTag)
    {
        return new BusTraceEvent(
            cycle: access.Cycle,
            address: access.Address,
            value: value,
            widthBits: access.WidthBits,
            intent: access.Intent,
            flags: access.Flags,
            sourceId: access.SourceId,
            deviceId: deviceId,
            regionTag: regionTag);
    }
}