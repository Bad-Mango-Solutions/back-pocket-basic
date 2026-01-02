// <copyright file="MainBus.SwapGroups.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// Swap group bank switching operations for the main memory bus.
/// </summary>
public sealed partial class MainBus
{
    /// <inheritdoc />
    public uint CreateSwapGroup(string groupName, Addr virtualBase, Addr size)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ValidateAlignment(virtualBase, size);

        int startPage = (int)(virtualBase >> PageShift);
        int pageCount = (int)(size >> PageShift);

        if (startPage + pageCount > pageTable.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, $"Swap group (0x{virtualBase:X} + 0x{size:X}) exceeds address space.");
        }

        lock (swapGroupLock)
        {
            if (swapGroupIdsByName.ContainsKey(groupName))
            {
                throw new ArgumentException($"A swap group with name '{groupName}' already exists.", nameof(groupName));
            }

            uint groupId = nextSwapGroupId++;
            var swapGroup = new SwapGroup(groupId, groupName, virtualBase, size);

            swapGroupsById[groupId] = swapGroup;
            swapGroupIdsByName[groupName] = groupId;

            return groupId;
        }
    }

    /// <inheritdoc />
    public uint GetSwapGroupId(string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        lock (swapGroupLock)
        {
            if (!swapGroupIdsByName.TryGetValue(groupName, out uint groupId))
            {
                throw new KeyNotFoundException($"Swap group '{groupName}' not found.");
            }

            return groupId;
        }
    }

    /// <inheritdoc />
    public void AddSwapVariant(uint groupId, string variantName, IBusTarget target, Addr physBase, PagePerms perms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variantName);
        ArgumentNullException.ThrowIfNull(target);

        lock (swapGroupLock)
        {
            if (!swapGroupsById.TryGetValue(groupId, out var swapGroup))
            {
                throw new KeyNotFoundException($"Swap group with ID '{groupId}' not found.");
            }

            var variant = new SwapVariant(variantName, target, physBase, perms);
            swapGroup.AddVariant(variant);
        }
    }

    /// <inheritdoc />
    public void SelectSwapVariant(uint groupId, string variantName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variantName);

        lock (swapGroupLock)
        {
            if (!swapGroupsById.TryGetValue(groupId, out var swapGroup))
            {
                throw new KeyNotFoundException($"Swap group with ID '{groupId}' not found.");
            }

            // Get the variant to select (this validates the variant exists)
            var variant = swapGroup.GetVariant(variantName);

            // Atomically update all page entries for the swap group's address range
            int startPage = swapGroup.GetStartPage(PageShift);
            int pageCount = swapGroup.GetPageCount(PageShift);

            for (int i = 0; i < pageCount; i++)
            {
                int pageIndex = startPage + i;
                ref var entry = ref pageTable[pageIndex];

                // Calculate the physical address for this page within the variant
                Addr pagePhysBase = variant.PhysBase + (Addr)(i * PageSize);

                // Create new entry preserving device ID and other metadata, but updating
                // target, physical base, and permissions from the variant
                pageTable[pageIndex] = entry with
                {
                    Target = variant.Target,
                    PhysicalBase = pagePhysBase,
                    Perms = variant.Perms,
                };
            }

            // Update the active variant in the swap group
            swapGroup.SetActiveVariant(variantName);
        }
    }

    /// <inheritdoc />
    public string? GetActiveSwapVariant(uint groupId)
    {
        lock (swapGroupLock)
        {
            if (!swapGroupsById.TryGetValue(groupId, out var swapGroup))
            {
                throw new KeyNotFoundException($"Swap group with ID '{groupId}' not found.");
            }

            return swapGroup.ActiveVariantName;
        }
    }
}