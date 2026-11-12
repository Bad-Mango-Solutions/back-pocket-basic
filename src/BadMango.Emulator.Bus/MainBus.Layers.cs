// <copyright file="MainBus.Layers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <content>
/// Layer system operations for the main memory bus.
/// </content>
public sealed partial class MainBus
{
    /// <inheritdoc />
    public MappingLayer CreateLayer(string name, int priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (layers.ContainsKey(name))
        {
            throw new ArgumentException($"A layer with name '{name}' already exists.", nameof(name));
        }

        var layer = new MappingLayer(name, priority, IsActive: false);
        layers[name] = layer;
        layeredMappings[name] = [];
        return layer;
    }

    /// <inheritdoc />
    public MappingLayer? GetLayer(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return layers.TryGetValue(name, out var layer) ? layer : null;
    }

    /// <inheritdoc />
    public void AddLayeredMapping(LayeredMapping mapping)
    {
        if (!layers.TryGetValue(mapping.Layer.Name, out var layer))
        {
            throw new ArgumentException($"Layer '{mapping.Layer.Name}' does not exist.", nameof(mapping));
        }

        ValidateAlignment(mapping.VirtualBase, mapping.Size);

        int startPage = mapping.GetStartPage(PageShift);
        int pageCount = mapping.GetPageCount(PageShift);
        if (startPage + pageCount > pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(mapping), $"Mapping (0x{mapping.VirtualBase:X} + 0x{mapping.Size:X}) exceeds address space.");
        }

        layeredMappings[mapping.Layer.Name].Add(mapping);

        // If the layer is active, recompute affected pages
        if (layer.IsActive)
        {
            RecomputeAffectedPages(startPage, pageCount);
        }
    }

    /// <inheritdoc />
    public void ActivateLayer(string layerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        if (!layers.TryGetValue(layerName, out var layer))
        {
            throw new KeyNotFoundException($"Layer '{layerName}' not found.");
        }

        if (layer.IsActive)
        {
            return; // Already active
        }

        layers[layerName] = layer.WithActive(true);

        // Recompute all pages affected by this layer's mappings
        var mappings = layeredMappings[layerName];
        // Snapshot to guard against reentrant modification during recompute (e.g. device state changes from side effects)
        foreach (var mapping in mappings.ToList())
        {
            int startPage = mapping.GetStartPage(PageShift);
            int pageCount = mapping.GetPageCount(PageShift);
            RecomputeAffectedPages(startPage, pageCount);
        }
    }

    /// <inheritdoc />
    public void DeactivateLayer(string layerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        if (!layers.TryGetValue(layerName, out var layer))
        {
            throw new KeyNotFoundException($"Layer '{layerName}' not found.");
        }

        if (!layer.IsActive)
        {
            return; // Already inactive
        }

        layers[layerName] = layer.WithActive(false);

        // Recompute all pages affected by this layer's mappings
        var mappings = layeredMappings[layerName];
        // Snapshot to guard against reentrant modification during recompute (e.g. device state changes from side effects)
        foreach (var mapping in mappings.ToList())
        {
            int startPage = mapping.GetStartPage(PageShift);
            int pageCount = mapping.GetPageCount(PageShift);
            RecomputeAffectedPages(startPage, pageCount);
        }
    }

    /// <inheritdoc />
    public bool IsLayerActive(string layerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        if (!layers.TryGetValue(layerName, out var layer))
        {
            throw new KeyNotFoundException($"Layer '{layerName}' not found.");
        }

        return layer.IsActive;
    }

    /// <inheritdoc />
    public void SetLayerPermissions(string layerName, PagePerms perms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(layerName);

        if (!layers.TryGetValue(layerName, out var layer))
        {
            throw new KeyNotFoundException($"Layer '{layerName}' not found.");
        }

        var mappings = layeredMappings[layerName];
        for (int i = 0; i < mappings.Count; i++)
        {
            mappings[i] = mappings[i] with { Perms = perms };
        }

        // If the layer is active, recompute affected pages
        if (layer.IsActive)
        {
            // Snapshot to guard against reentrant modification during recompute (e.g. device state changes from side effects)
            foreach (var mapping in mappings.ToList())
            {
                int startPage = mapping.GetStartPage(PageShift);
                int pageCount = mapping.GetPageCount(PageShift);
                RecomputeAffectedPages(startPage, pageCount);
            }
        }
    }

    /// <inheritdoc />
    public PageEntry GetEffectiveMapping(Addr address)
    {
        return GetPageEntry(address);
    }

    /// <inheritdoc />
    public IEnumerable<LayeredMapping> GetAllMappingsAt(Addr address)
    {
        // Snapshot to avoid modification during enumeration by callers (e.g. debug during execution)
        return layeredMappings.Values.ToList()
            .SelectMany(mappings => mappings.ToList())
            .Where(mapping => mapping.ContainsAddress(address));
    }

    /// <inheritdoc />
    public IEnumerable<MappingLayer> GetLayersAt(Addr address)
    {
        // Snapshot to avoid modification during enumeration by callers (e.g. debug during execution)
        return layeredMappings.ToList()
            .Where(kvp => kvp.Value.ToList().Any(mapping => mapping.ContainsAddress(address)))
            .Select(kvp => layers[kvp.Key])
            .OrderByDescending(l => l.Priority);
    }

    /// <summary>
    /// Stores the current page entry as a base mapping.
    /// Call this after setting up the initial flat page table.
    /// </summary>
    /// <param name="pageIndex">The page index to save.</param>
    /// <remarks>
    /// This method saves the current page entry to the base page table,
    /// which is used when recomputing effective mappings after layer changes.
    /// </remarks>
    public void SaveBaseMapping(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex), pageIndex, $"Page index must be between 0 and {pageTable.Length - 1}.");
        }

        basePageTable[pageIndex] = pageTable[pageIndex];
    }

    /// <summary>
    /// Stores a range of page entries as base mappings.
    /// </summary>
    /// <param name="startPage">The first page index to save.</param>
    /// <param name="pageCount">The number of consecutive pages to save.</param>
    public void SaveBaseMappingRange(int startPage, int pageCount)
    {
        if (startPage < 0 || startPage >= pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(startPage), startPage, $"Start page must be between 0 and {pageTable.Length - 1}.");
        }

        if (pageCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, "Page count cannot be negative.");
        }

        if (startPage + pageCount > pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), pageCount, $"Page range ({startPage} + {pageCount}) exceeds address space ({pageTable.Length} pages).");
        }

        for (int i = 0; i < pageCount; i++)
        {
            basePageTable[startPage + i] = pageTable[startPage + i];
        }
    }

    /// <summary>
    /// Recomputes effective page entries for a range of pages.
    /// </summary>
    /// <param name="startPage">The first page to recompute.</param>
    /// <param name="pageCount">The number of pages to recompute.</param>
    private void RecomputeAffectedPages(int startPage, int pageCount)
    {
        for (int i = 0; i < pageCount; i++)
        {
            int pageIndex = startPage + i;
            RecomputePageEntry(pageIndex);
        }
    }

    /// <summary>
    /// Recomputes the effective page entry for a single page based on all active layers.
    /// </summary>
    /// <param name="pageIndex">The page index to recompute.</param>
    private void RecomputePageEntry(int pageIndex)
    {
        Addr pageAddress = (Addr)(pageIndex << PageShift);

        // Start with the base mapping
        PageEntry effectiveEntry = basePageTable[pageIndex];
        int highestPriority = int.MinValue;

        // Check all active layers for mappings at this address
        // Snapshot collections to guard against concurrent/reentrant modification during recomputes (e.g. from device ApplyState side-effects during CPU writes)
        foreach (var kvp in layeredMappings.ToList())
        {
            var layerName = kvp.Key;
            var layer = layers[layerName];

            if (!layer.IsActive)
            {
                continue;
            }

            foreach (var mapping in kvp.Value.ToList())
            {
                if (!mapping.ContainsAddress(pageAddress))
                {
                    continue;
                }

                // Higher priority wins
                if (layer.Priority > highestPriority)
                {
                    highestPriority = layer.Priority;

                    // Calculate the physical address offset within the mapping
                    int pageOffsetInMapping = pageIndex - mapping.GetStartPage(PageShift);
                    Addr physicalBase = mapping.PhysBase + (Addr)(pageOffsetInMapping * PageSize);

                    // Check if there's a swap group for this address range
                    // If so, use the selected variant's target and physBase instead
                    IBusTarget effectiveTarget = mapping.Target;
                    Addr effectivePhysBase = physicalBase;
                    PagePerms effectivePerms = mapping.Perms;

                    lock (swapGroupLock)
                    {
                        // Snapshot to prevent modification-during-enumeration if reentrant swap or layer ops occur
                        foreach (var swapGroup in swapGroupsById.Values.ToList())
                        {
                            if (swapGroup.ContainsAddress(pageAddress) && swapGroup.ActiveVariantName is not null)
                            {
                                var variant = swapGroup.GetVariant(swapGroup.ActiveVariantName);
                                effectiveTarget = variant.Target;

                                // Calculate physical address within the variant
                                int swapPageOffset = pageIndex - swapGroup.GetStartPage(PageShift);
                                effectivePhysBase = variant.PhysBase + (Addr)(swapPageOffset * PageSize);

                                // Use the layer's permissions (which may be updated via SetLayerPermissions)
                                // but the swap group variant's target and physBase
                                break;
                            }
                        }
                    }

                    effectiveEntry = new(
                        mapping.DeviceId,
                        mapping.RegionTag,
                        effectivePerms,
                        mapping.Caps,
                        effectiveTarget,
                        effectivePhysBase);
                }
            }
        }

        pageTable[pageIndex] = effectiveEntry;
    }
}