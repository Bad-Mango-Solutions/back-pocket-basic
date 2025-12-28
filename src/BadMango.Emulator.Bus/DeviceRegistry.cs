// <copyright file="DeviceRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// Default implementation of <see cref="IDeviceRegistry"/> for device metadata management.
/// </summary>
/// <remarks>
/// <para>
/// The device registry provides a mapping from structural device IDs to rich metadata
/// including device type, human-readable name, and wiring path. This allows the hot path
/// to store only numeric IDs while tooling resolves IDs to meaningful names after the fact.
/// </para>
/// <para>
/// Registration is typically done at system initialization time (write-once), while lookups
/// happen frequently during trace analysis and debugging (read-many).
/// </para>
/// <para>
/// This implementation is thread-safe for concurrent reads after initialization.
/// Concurrent writes during initialization are not supported.
/// </para>
/// </remarks>
public sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly Dictionary<int, DeviceInfo> devices = [];
    private int nextId;

    /// <inheritdoc />
    public int Count => devices.Count;

    /// <inheritdoc />
    public void Register(int id, string kind, string name, string wiringPath)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(wiringPath);

        if (devices.ContainsKey(id))
        {
            throw new ArgumentException($"Device with ID {id} is already registered.", nameof(id));
        }

        devices[id] = new DeviceInfo(id, kind, name, wiringPath);

        // Keep nextId ahead of all registered IDs
        if (id >= nextId)
        {
            nextId = id + 1;
        }
    }

    /// <inheritdoc />
    public bool TryGet(int id, out DeviceInfo info)
    {
        return devices.TryGetValue(id, out info);
    }

    /// <inheritdoc />
    public DeviceInfo Get(int id)
    {
        if (devices.TryGetValue(id, out var info))
        {
            return info;
        }

        throw new KeyNotFoundException($"No device registered with ID {id}.");
    }

    /// <inheritdoc />
    public IEnumerable<DeviceInfo> GetAll()
    {
        return devices.Values;
    }

    /// <inheritdoc />
    public bool Contains(int id)
    {
        return devices.ContainsKey(id);
    }

    /// <inheritdoc />
    public int GenerateId()
    {
        return nextId++;
    }
}