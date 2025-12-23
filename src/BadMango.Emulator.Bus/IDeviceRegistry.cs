// <copyright file="IDeviceRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Registry for device instances with human-readable metadata.
/// </summary>
/// <remarks>
/// <para>
/// The device registry provides a mapping from structural device IDs to
/// rich metadata including device type, human-readable name, and wiring path.
/// </para>
/// <para>
/// Registration is typically done at system initialization time (write-once),
/// while lookups happen frequently during trace analysis and debugging (read-many).
/// </para>
/// <para>
/// Hot path operations store only numeric device IDs. The registry is used
/// by tooling to translate IDs into meaningful names for display.
/// </para>
/// </remarks>
public interface IDeviceRegistry
{
    /// <summary>
    /// Gets the total number of registered devices.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Registers a new device with the specified metadata.
    /// </summary>
    /// <param name="id">The structural instance identifier for the device.</param>
    /// <param name="kind">The type or category of device (e.g., "SlotCard", "Ram", "MegaII").</param>
    /// <param name="name">Human-readable name for display in tools and logs.</param>
    /// <param name="wiringPath">Hierarchical path describing the device's location in the system.</param>
    /// <exception cref="ArgumentException">Thrown if a device with the same ID is already registered.</exception>
    void Register(int id, string kind, string name, string wiringPath);

    /// <summary>
    /// Attempts to retrieve device information by ID.
    /// </summary>
    /// <param name="id">The structural instance identifier to look up.</param>
    /// <param name="info">When successful, contains the device information; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the device was found; otherwise, <see langword="false"/>.</returns>
    bool TryGet(int id, out DeviceInfo info);

    /// <summary>
    /// Gets device information by ID.
    /// </summary>
    /// <param name="id">The structural instance identifier to look up.</param>
    /// <returns>The device information if found.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no device with the specified ID is registered.</exception>
    DeviceInfo Get(int id);

    /// <summary>
    /// Enumerates all registered device information.
    /// </summary>
    /// <returns>An enumerable of all registered device info records.</returns>
    IEnumerable<DeviceInfo> GetAll();

    /// <summary>
    /// Checks whether a device with the specified ID is registered.
    /// </summary>
    /// <param name="id">The device ID to check.</param>
    /// <returns><see langword="true"/> if the device is registered; otherwise, <see langword="false"/>.</returns>
    bool Contains(int id);

    /// <summary>
    /// Generates a new unique device ID.
    /// </summary>
    /// <returns>A new ID that has not been used for any registered device.</returns>
    /// <remarks>
    /// This method is useful during system initialization when devices need
    /// unique IDs but don't have predefined values.
    /// </remarks>
    int GenerateId();
}