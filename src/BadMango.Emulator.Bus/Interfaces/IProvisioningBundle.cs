// <copyright file="IProvisioningBundle.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Describes the configuration for provisioning a machine.
/// </summary>
/// <remarks>
/// <para>
/// The provisioning bundle is assembled by emulator tooling (UI, CLI, configuration loader)
/// and passed to a machine's bring-up handler. It describes:
/// <list type="bullet">
/// <item><description>How much RAM the user wants</description></item>
/// <item><description>Which ROM image(s) to load</description></item>
/// <item><description>Which devices are attached</description></item>
/// <item><description>Any user-specified memory layout overrides</description></item>
/// </list>
/// </para>
/// <para>
/// The machine bring-up code receives this bundle and decides how to allocate physical
/// memory pools, where regions should be mapped, what the initial page table looks like,
/// and how to handle machine-specific quirks (bank switching, soft switches, etc.).
/// </para>
/// </remarks>
public interface IProvisioningBundle
{
    /// <summary>
    /// Gets the requested RAM size in bytes.
    /// </summary>
    /// <value>The total RAM to provision for the machine.</value>
    uint RequestedRamSize { get; }

    /// <summary>
    /// Gets the ROM images to load, keyed by their identifier.
    /// </summary>
    /// <value>A dictionary mapping ROM identifiers to their binary data.</value>
    IReadOnlyDictionary<string, ReadOnlyMemory<byte>> RomImages { get; }

    /// <summary>
    /// Gets the device configurations to attach.
    /// </summary>
    /// <value>A collection of device configuration entries.</value>
    IReadOnlyCollection<DeviceConfiguration> Devices { get; }

    /// <summary>
    /// Gets any memory layout overrides specified by the user.
    /// </summary>
    /// <value>Optional overrides for region base addresses.</value>
    IReadOnlyDictionary<string, Addr>? LayoutOverrides { get; }

    /// <summary>
    /// Gets a value indicating whether to enable debug features.
    /// </summary>
    /// <value><see langword="true"/> if debug features should be enabled.</value>
    bool EnableDebugFeatures { get; }
}