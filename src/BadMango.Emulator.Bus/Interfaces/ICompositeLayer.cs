// <copyright file="ICompositeLayer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// A composite layer that can dispatch to different targets based on address and access type.
/// </summary>
/// <remarks>
/// <para>
/// This interface extends the layer concept to support complex overlays with multiple
/// sub-regions and dynamic target resolution, similar to how <see cref="ICompositeTarget"/>
/// works for single pages.
/// </para>
/// <para>
/// The Language Card is a prime example requiring a composite layer:
/// </para>
/// <list type="bullet">
/// <item><description>$D000-$DFFF: Two switchable 4KB RAM banks (Bank 1 and Bank 2)</description></item>
/// <item><description>$E000-$FFFF: Single 8KB RAM bank</description></item>
/// <item><description>Read enable/disable (RAM vs ROM visibility)</description></item>
/// <item><description>Write enable/disable (write protection)</description></item>
/// <item><description>Pre-write state machine for write enable</description></item>
/// </list>
/// <para>
/// Unlike standard layers which have static mappings, composite layers can:
/// </para>
/// <list type="bullet">
/// <item><description>Return different targets for the same address based on soft switch state</description></item>
/// <item><description>Return different permissions (e.g., read-only vs read-write)</description></item>
/// <item><description>Handle the pre-write state machine for LC write enable</description></item>
/// <item><description>Support overlapping regions with different behaviors</description></item>
/// </list>
/// </remarks>
public interface ICompositeLayer
{
    /// <summary>
    /// Gets the name of this composite layer.
    /// </summary>
    /// <value>A unique identifier for this layer.</value>
    string Name { get; }

    /// <summary>
    /// Gets the base priority of this composite layer.
    /// </summary>
    /// <value>
    /// The priority value used when comparing against other layers.
    /// Higher values take precedence over lower values.
    /// </value>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this layer is currently active.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the layer should contribute to effective mappings;
    /// otherwise, <see langword="false"/>.
    /// </value>
    bool IsActive { get; }

    /// <summary>
    /// Gets the virtual address range covered by this layer.
    /// </summary>
    /// <value>A tuple of (start address, size in bytes).</value>
    (Addr Start, Addr Size) AddressRange { get; }

    /// <summary>
    /// Resolves the effective mapping for a given address and access type.
    /// </summary>
    /// <param name="address">The virtual address being accessed.</param>
    /// <param name="intent">The access intent (read, write, execute).</param>
    /// <returns>
    /// A <see cref="CompositeLayerResolution"/> containing the target and permissions,
    /// or <see langword="null"/> if this layer does not handle the address (fall through to lower layers).
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is called during address translation when the layer is active.
    /// The returned resolution determines:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Which target handles the access</description></item>
    /// <item><description>What permissions apply</description></item>
    /// <item><description>The physical base address within the target</description></item>
    /// </list>
    /// <para>
    /// Returning <see langword="null"/> indicates that the layer does not want to
    /// handle this specific access, allowing lower-priority layers or the base
    /// mapping to handle it.
    /// </para>
    /// </remarks>
    CompositeLayerResolution? ResolveMapping(Addr address, AccessIntent intent);

    /// <summary>
    /// Called when the layer is activated to allow state initialization.
    /// </summary>
    void OnActivate();

    /// <summary>
    /// Called when the layer is deactivated to allow state cleanup.
    /// </summary>
    void OnDeactivate();
}

/// <summary>
/// The result of resolving a mapping within a composite layer.
/// </summary>
/// <param name="Target">The bus target to handle the access.</param>
/// <param name="PhysicalBase">The physical base address within the target.</param>
/// <param name="Perms">The permissions for this access.</param>
/// <param name="Tag">The region tag for this mapping.</param>
/// <param name="Caps">The target capabilities.</param>
public readonly record struct CompositeLayerResolution(
    IBusTarget Target,
    Addr PhysicalBase,
    PagePerms Perms,
    RegionTag Tag,
    TargetCaps Caps);
