// <copyright file="ProvisioningBundle.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A concrete implementation of <see cref="IProvisioningBundle"/> for configuring machine bring-up.
/// </summary>
public sealed class ProvisioningBundle : IProvisioningBundle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProvisioningBundle"/> class.
    /// </summary>
    /// <param name="requestedRamSize">The requested RAM size in bytes.</param>
    /// <param name="romImages">ROM images keyed by identifier.</param>
    /// <param name="devices">Device configurations to attach.</param>
    /// <param name="layoutOverrides">Optional memory layout overrides.</param>
    /// <param name="enableDebugFeatures">Whether to enable debug features.</param>
    public ProvisioningBundle(
        uint requestedRamSize,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? romImages = null,
        IReadOnlyCollection<DeviceConfiguration>? devices = null,
        IReadOnlyDictionary<string, Addr>? layoutOverrides = null,
        bool enableDebugFeatures = false)
    {
        RequestedRamSize = requestedRamSize;
        RomImages = romImages ?? new Dictionary<string, ReadOnlyMemory<byte>>();
        Devices = devices ?? Array.Empty<DeviceConfiguration>();
        LayoutOverrides = layoutOverrides;
        EnableDebugFeatures = enableDebugFeatures;
    }

    /// <inheritdoc />
    public uint RequestedRamSize { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ReadOnlyMemory<byte>> RomImages { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<DeviceConfiguration> Devices { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Addr>? LayoutOverrides { get; }

    /// <inheritdoc />
    public bool EnableDebugFeatures { get; }

    /// <summary>
    /// Creates a builder for constructing a provisioning bundle.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static ProvisioningBundleBuilder CreateBuilder() => new();
}