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
/// This implementation supports both simple integer IDs and structured <see cref="DevicePageId"/>
/// values for 65832 compatibility. Devices with valid page IDs are additionally indexed
/// by their page ID for efficient lookup.
/// </para>
/// <para>
/// This implementation is thread-safe for concurrent reads after initialization.
/// Concurrent writes during initialization are not supported.
/// </para>
/// </remarks>
public sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly Dictionary<int, DeviceInfo> devicesById = [];
    private readonly Dictionary<uint, DeviceInfo> devicesByPageId = [];
    private int nextId;

    /// <inheritdoc />
    public int Count => devicesById.Count;

    /// <inheritdoc />
    public void Register(int id, string kind, string name, string wiringPath)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(wiringPath);

        if (devicesById.ContainsKey(id))
        {
            throw new ArgumentException($"Device with ID {id} is already registered.", nameof(id));
        }

        devicesById[id] = new(id, kind, name, wiringPath);

        // Keep nextId ahead of all registered IDs
        if (id >= nextId)
        {
            nextId = id + 1;
        }
    }

    /// <inheritdoc />
    public void Register(int id, DevicePageId pageId, string kind, string name, string wiringPath)
    {
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(wiringPath);

        if (devicesById.ContainsKey(id))
        {
            throw new ArgumentException($"Device with ID {id} is already registered.", nameof(id));
        }

        if (pageId.IsValid && devicesByPageId.ContainsKey(pageId.RawValue))
        {
            throw new ArgumentException($"Device with page ID {pageId} is already registered.", nameof(pageId));
        }

        var info = new DeviceInfo(id, pageId, kind, name, wiringPath);
        devicesById[id] = info;

        if (pageId.IsValid)
        {
            devicesByPageId[pageId.RawValue] = info;
        }

        // Keep nextId ahead of all registered IDs
        if (id >= nextId)
        {
            nextId = id + 1;
        }
    }

    /// <inheritdoc />
    public bool TryGet(int id, out DeviceInfo info)
    {
        return devicesById.TryGetValue(id, out info);
    }

    /// <inheritdoc />
    public DeviceInfo Get(int id)
    {
        if (devicesById.TryGetValue(id, out var info))
        {
            return info;
        }

        throw new KeyNotFoundException($"No device registered with ID {id}.");
    }

    /// <inheritdoc />
    public bool TryGetByPageId(DevicePageId pageId, out DeviceInfo info)
    {
        return devicesByPageId.TryGetValue(pageId.RawValue, out info);
    }

    /// <inheritdoc />
    public DeviceInfo GetByPageId(DevicePageId pageId)
    {
        if (devicesByPageId.TryGetValue(pageId.RawValue, out var info))
        {
            return info;
        }

        throw new KeyNotFoundException($"No device registered with page ID {pageId}.");
    }

    /// <inheritdoc />
    public IEnumerable<DeviceInfo> GetAll()
    {
        return devicesById.Values;
    }

    /// <inheritdoc />
    public IEnumerable<DeviceInfo> GetByClass(DevicePageClass deviceClass)
    {
        return devicesByPageId.Values.Where(d => d.PageId.Class == deviceClass);
    }

    /// <inheritdoc />
    public bool Contains(int id)
    {
        return devicesById.ContainsKey(id);
    }

    /// <inheritdoc />
    public bool ContainsPageId(DevicePageId pageId)
    {
        return devicesByPageId.ContainsKey(pageId.RawValue);
    }

    /// <inheritdoc />
    public int GenerateId()
    {
        return nextId++;
    }
}