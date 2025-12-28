// <copyright file="MappingStack.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A concrete implementation of <see cref="IMappingStack"/> for managing overlays and bank switching.
/// </summary>
/// <remarks>
/// <para>
/// The mapping stack maintains an ordered list of mapping entries. The topmost active
/// entry determines the actual routing for memory accesses in this range.
/// </para>
/// </remarks>
public sealed class MappingStack : IMappingStack
{
    private readonly List<MappingEntry> entries = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingStack"/> class.
    /// </summary>
    /// <param name="baseAddress">The starting address this stack covers.</param>
    /// <param name="size">The size of the address range.</param>
    public MappingStack(Addr baseAddress, uint size)
    {
        ArgumentOutOfRangeException.ThrowIfZero(size);

        BaseAddress = baseAddress;
        Size = size;
    }

    /// <inheritdoc />
    public Addr BaseAddress { get; }

    /// <inheritdoc />
    public uint Size { get; }

    /// <inheritdoc />
    public int Count => entries.Count;

    /// <inheritdoc />
    public MappingEntry? ActiveEntry
    {
        get
        {
            // Return the topmost active entry
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].IsActive)
                {
                    return entries[i];
                }
            }

            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<MappingEntry> Entries => entries.AsReadOnly();

    /// <inheritdoc />
    public void Push(MappingEntry entry)
    {
        entries.Add(entry);
    }

    /// <inheritdoc />
    public MappingEntry? Pop()
    {
        if (entries.Count == 0)
        {
            return null;
        }

        var entry = entries[^1];
        entries.RemoveAt(entries.Count - 1);
        return entry;
    }

    /// <inheritdoc />
    public bool SetActive(string regionId, bool active)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Region.Id == regionId)
            {
                entries[i] = entries[i].WithActive(active);
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Replace(MappingEntry entry)
    {
        entries.Clear();
        entries.Add(entry);
    }

    /// <inheritdoc />
    public void Clear()
    {
        entries.Clear();
    }

    /// <inheritdoc />
    public PageEntry? ToPageEntry(int deviceId, uint pageOffset, uint pageSize = 4096)
    {
        var active = ActiveEntry;
        if (active is null)
        {
            return null;
        }

        var entry = active.Value;
        var physicalBase = (Addr)(entry.PhysicalOffset + (pageOffset * pageSize));

        return new PageEntry(
            DeviceId: deviceId,
            RegionTag: entry.EffectiveTag,
            Perms: entry.EffectivePermissions,
            Caps: entry.Region.Capabilities,
            Target: entry.Region.Target,
            PhysicalBase: physicalBase);
    }
}