// <copyright file="ProvisioningBundleBuilder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Builder for constructing <see cref="ProvisioningBundle"/> instances.
/// </summary>
public sealed class ProvisioningBundleBuilder
{
    private readonly Dictionary<string, ReadOnlyMemory<byte>> romImages = new(StringComparer.Ordinal);
    private readonly List<DeviceConfiguration> devices = [];
    private readonly Dictionary<string, Addr> layoutOverrides = new(StringComparer.Ordinal);
    private uint requestedRamSize;
    private bool enableDebugFeatures;

    /// <summary>
    /// Sets the requested RAM size.
    /// </summary>
    /// <param name="size">The RAM size in bytes.</param>
    /// <returns>This builder for chaining.</returns>
    public ProvisioningBundleBuilder WithRamSize(uint size)
    {
        requestedRamSize = size;
        return this;
    }

    /// <summary>
    /// Adds a ROM image.
    /// </summary>
    /// <param name="id">The ROM identifier.</param>
    /// <param name="data">The ROM binary data.</param>
    /// <returns>This builder for chaining.</returns>
    public ProvisioningBundleBuilder WithRomImage(string id, ReadOnlyMemory<byte> data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        romImages[id] = data;
        return this;
    }

    /// <summary>
    /// Adds a device configuration.
    /// </summary>
    /// <param name="configuration">The device configuration.</param>
    /// <returns>This builder for chaining.</returns>
    public ProvisioningBundleBuilder WithDevice(DeviceConfiguration configuration)
    {
        devices.Add(configuration);
        return this;
    }

    /// <summary>
    /// Adds a memory layout override.
    /// </summary>
    /// <param name="regionId">The region identifier.</param>
    /// <param name="baseAddress">The override base address.</param>
    /// <returns>This builder for chaining.</returns>
    public ProvisioningBundleBuilder WithLayoutOverride(string regionId, Addr baseAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionId);
        layoutOverrides[regionId] = baseAddress;
        return this;
    }

    /// <summary>
    /// Enables or disables debug features.
    /// </summary>
    /// <param name="enable">Whether to enable debug features.</param>
    /// <returns>This builder for chaining.</returns>
    public ProvisioningBundleBuilder WithDebugFeatures(bool enable = true)
    {
        enableDebugFeatures = enable;
        return this;
    }

    /// <summary>
    /// Builds the provisioning bundle.
    /// </summary>
    /// <returns>The configured provisioning bundle.</returns>
    public ProvisioningBundle Build()
    {
        return new(
            requestedRamSize,
            romImages.Count > 0 ? new Dictionary<string, ReadOnlyMemory<byte>>(romImages) : null,
            devices.Count > 0 ? devices.ToArray() : null,
            layoutOverrides.Count > 0 ? new Dictionary<string, Addr>(layoutOverrides) : null,
            enableDebugFeatures);
    }
}