// <copyright file="DeviceConfiguration.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Configuration for a device to be attached during machine bring-up.
/// </summary>
/// <param name="DeviceType">The type identifier for the device.</param>
/// <param name="DeviceId">A unique identifier for this device instance.</param>
/// <param name="BaseAddress">Optional base address override for the device.</param>
/// <param name="Properties">Optional device-specific configuration properties.</param>
public readonly record struct DeviceConfiguration(
    string DeviceType,
    string DeviceId,
    Addr? BaseAddress = null,
    IReadOnlyDictionary<string, object>? Properties = null);