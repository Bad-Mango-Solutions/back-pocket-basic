// <copyright file="RegionManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A concrete implementation of <see cref="IRegionManager"/> for managing memory regions and mappings.
/// </summary>
/// <remarks>
/// <para>
/// The region manager maintains the complete picture of memory regions and their
/// current mapping states. It serves as the bridge between region definitions and
/// the page table used for actual memory access routing.
/// </para>
/// </remarks>
public sealed class RegionManager : IRegionManager
{
    private readonly Dictionary<string, IMemoryRegion> regions = new(StringComparer.Ordinal);
    private readonly List<MappingStack> mappingStacks = [];
    private readonly Dictionary<string, Addr> regionMappedAddresses = new(StringComparer.Ordinal);
    private int nextDeviceId = 1;

    /// <inheritdoc />
    public int RegionCount => regions.Count;

    /// <inheritdoc />
    public IReadOnlyCollection<IMemoryRegion> Regions => regions.Values;

    /// <inheritdoc />
    public void RegisterRegion(IMemoryRegion region)
    {
        ArgumentNullException.ThrowIfNull(region);

        if (regions.ContainsKey(region.Id))
        {
            throw new ArgumentException($"A region with ID '{region.Id}' already exists.", nameof(region));
        }

        regions[region.Id] = region;
    }

    /// <inheritdoc />
    public bool UnregisterRegion(string regionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionId);

        // Also remove from mapped addresses and any stacks
        regionMappedAddresses.Remove(regionId);

        foreach (var stack in mappingStacks)
        {
            stack.SetActive(regionId, false);
        }

        return regions.Remove(regionId);
    }

    /// <inheritdoc />
    public IMemoryRegion? GetRegion(string regionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionId);
        return regions.GetValueOrDefault(regionId);
    }

    /// <inheritdoc />
    public IEnumerable<IMemoryRegion> GetRegionsInRange(Addr startAddress, uint size)
    {
        var endAddress = startAddress + size;

        return regions.Values.Where(r =>
        {
            var regionEnd = r.PreferredBase + r.Size;
            return r.PreferredBase < endAddress && regionEnd > startAddress;
        });
    }

    /// <inheritdoc />
    public IMappingStack? GetMappingStack(Addr address)
    {
        return mappingStacks
            .Where(stack => address >= stack.BaseAddress && address < stack.BaseAddress + stack.Size)
            .FirstOrDefault();
    }

    /// <inheritdoc />
    public IMappingStack GetOrCreateMappingStack(Addr baseAddress, uint size)
    {
        // Check for existing stack that covers this range
        var existing = mappingStacks
            .Where(s => s.BaseAddress == baseAddress && s.Size == size)
            .FirstOrDefault();

        if (existing is not null)
        {
            return existing;
        }

        var stack = new MappingStack(baseAddress, size);
        mappingStacks.Add(stack);
        return stack;
    }

    /// <inheritdoc />
    public void BuildPageTable(IMemoryBus bus, uint pageSize = 4096)
    {
        ArgumentNullException.ThrowIfNull(bus);

        // Sort stacks by their active entry priorities
        var sortedStacks = mappingStacks
            .Where(s => s.ActiveEntry.HasValue)
            .OrderBy(s => s.ActiveEntry!.Value.Priority)
            .ThenBy(s => s.BaseAddress);

        foreach (var stack in sortedStacks)
        {
            var active = stack.ActiveEntry;
            if (active is null)
            {
                continue;
            }

            var startPage = (int)(stack.BaseAddress / pageSize);
            var numPages = (int)(stack.Size / pageSize);

            for (int i = 0; i < numPages; i++)
            {
                var pageEntry = stack.ToPageEntry(nextDeviceId, (uint)i, pageSize);
                if (pageEntry.HasValue)
                {
                    bus.MapPage(startPage + i, pageEntry.Value);
                }
            }

            nextDeviceId++;
        }
    }

    /// <inheritdoc />
    public void MapRegionAtPreferred(IMemoryRegion region, bool active = true)
    {
        ArgumentNullException.ThrowIfNull(region);

        MapRegionAt(region, region.PreferredBase, active);
    }

    /// <inheritdoc />
    public void MapRegionAt(IMemoryRegion region, Addr baseAddress, bool active = true)
    {
        ArgumentNullException.ThrowIfNull(region);

        if (!region.IsRelocatable && baseAddress != region.PreferredBase)
        {
            throw new InvalidOperationException(
                $"Region '{region.Id}' is not relocatable and must be mapped at its preferred base 0x{region.PreferredBase:X8}.");
        }

        // Register the region if not already registered
        if (!regions.ContainsKey(region.Id))
        {
            RegisterRegion(region);
        }

        // Get or create the mapping stack for this range
        var stack = GetOrCreateMappingStack(baseAddress, region.Size);

        // Create and push the mapping entry
        var entry = new MappingEntry(
            Region: region,
            IsActive: active,
            Perms: null,
            PhysicalOffset: 0,
            Priority: region.Priority);

        stack.Push(entry);

        // Track the mapped address
        regionMappedAddresses[region.Id] = baseAddress;
    }

    /// <inheritdoc />
    public bool ActivateRegion(string regionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionId);

        if (!regionMappedAddresses.TryGetValue(regionId, out var address))
        {
            return false;
        }

        var stack = GetMappingStack(address);
        return stack?.SetActive(regionId, true) ?? false;
    }

    /// <inheritdoc />
    public bool DeactivateRegion(string regionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionId);

        if (!regionMappedAddresses.TryGetValue(regionId, out var address))
        {
            return false;
        }

        var stack = GetMappingStack(address);
        return stack?.SetActive(regionId, false) ?? false;
    }

    /// <inheritdoc />
    public bool SwitchBank(string activateRegionId, string deactivateRegionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activateRegionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deactivateRegionId);

        // Both operations must succeed for the switch to be considered successful
        var activated = ActivateRegion(activateRegionId);
        var deactivated = DeactivateRegion(deactivateRegionId);

        // If one failed, try to roll back the other
        if (activated && !deactivated)
        {
            DeactivateRegion(activateRegionId);
            return false;
        }

        if (!activated && deactivated)
        {
            ActivateRegion(deactivateRegionId);
            return false;
        }

        return activated && deactivated;
    }

    /// <inheritdoc />
    public object CreateSnapshot()
    {
        // Create a snapshot of all mapping states
        var snapshot = new Dictionary<string, List<(MappingEntry Entry, Addr BaseAddress)>>();

        foreach (var stack in mappingStacks)
        {
            var stackEntries = new List<(MappingEntry Entry, Addr BaseAddress)>();
            foreach (var entry in stack.Entries)
            {
                stackEntries.Add((entry, stack.BaseAddress));
            }

            if (stackEntries.Count > 0)
            {
                snapshot[$"stack_{stack.BaseAddress:X8}"] = stackEntries;
            }
        }

        return snapshot;
    }

    /// <inheritdoc />
    public void RestoreSnapshot(object snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot is not Dictionary<string, List<(MappingEntry Entry, Addr BaseAddress)>> snapshotData)
        {
            throw new ArgumentException("Invalid snapshot format.", nameof(snapshot));
        }

        // Clear existing mappings
        mappingStacks.Clear();
        regionMappedAddresses.Clear();

        // Restore from snapshot
        foreach (var (key, entries) in snapshotData)
        {
            if (entries.Count == 0)
            {
                continue;
            }

            var baseAddress = entries[0].BaseAddress;
            var size = entries[0].Entry.Region.Size;
            var stack = new MappingStack(baseAddress, size);

            foreach (var (entry, _) in entries)
            {
                stack.Push(entry);
                regionMappedAddresses[entry.Region.Id] = baseAddress;
            }

            mappingStacks.Add(stack);
        }
    }
}