// <copyright file="DeviceInfo.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Metadata describing a registered device instance.
/// </summary>
/// <remarks>
/// <para>
/// Device info provides human-readable context for structural device IDs.
/// The hot path (tracing, bus operations) stores only numeric IDs;
/// tooling resolves these to rich descriptions after the fact.
/// </para>
/// <para>
/// This separation keeps the hot path lean while enabling rich tooling
/// that can narrate bus activity in meaningful terms.
/// </para>
/// </remarks>
/// <param name="Id">The structural instance identifier for the device.</param>
/// <param name="Kind">The type or category of device (e.g., "SlotCard", "Ram", "MegaII").</param>
/// <param name="Name">Human-readable name for display in tools and logs.</param>
/// <param name="WiringPath">Hierarchical path describing the device's location in the system (e.g., "main/slots/6/disk2").</param>
public readonly record struct DeviceInfo(
    int Id,
    string Kind,
    string Name,
    string WiringPath)
{
    /// <summary>
    /// Gets a display string combining name and kind.
    /// </summary>
    /// <returns>A formatted string suitable for display.</returns>
    public override string ToString() => $"{Name} ({Kind})";
}