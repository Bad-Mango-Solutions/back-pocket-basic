// <copyright file="MappingLayer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents a named layer in the memory mapping system.
/// </summary>
/// <remarks>
/// <para>
/// Layers enable overlay support for scenarios like the Apple II Language Card,
/// which overlays 16KB of RAM over ROM at D000−FFFF. Auxiliary memory similarly
/// overlays main RAM at various regions.
/// </para>
/// <para>
/// Each layer has a priority that determines which layer's mappings take effect
/// when multiple layers define mappings for the same address. Higher priority values
/// override lower priority values for overlapping addresses.
/// </para>
/// <para>
/// The "effective" mapping for any address is cached in the page table for O(1)
/// hot-path access. When a layer is activated or deactivated, affected page entries
/// are recomputed from all active layers.
/// </para>
/// </remarks>
/// <param name="Name">The unique name identifying this layer.</param>
/// <param name="Priority">The priority of this layer. Higher values override lower values for the same address.</param>
/// <param name="IsActive">Whether this layer is currently active and contributing to effective mappings.</param>
public readonly record struct MappingLayer(string Name, int Priority, bool IsActive)
{
    /// <summary>
    /// Creates a new layer with the active state changed.
    /// </summary>
    /// <param name="active">The new active state.</param>
    /// <returns>A new <see cref="MappingLayer"/> with the specified active state.</returns>
    public MappingLayer WithActive(bool active) => this with { IsActive = active };
}