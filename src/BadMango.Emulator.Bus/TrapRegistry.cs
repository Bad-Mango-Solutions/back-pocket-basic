// <copyright file="TrapRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Core.Interfaces.Cpu;

using Interfaces;

/// <summary>
/// High-performance trap registry with O(1) lookup for ROM routine interception.
/// </summary>
/// <remarks>
/// <para>
/// The trap registry enables native implementations of ROM routines and I/O operations
/// for performance optimization. Traps are organized by address and operation type.
/// </para>
/// <para>
/// The registry is context-aware, supporting:
/// </para>
/// <list type="bullet">
/// <item><description>Slot dependency checks for slot ROM traps ($Cn00-$CnFF)</description></item>
/// <item><description>Expansion ROM selection checks for expansion ROM traps ($C800-$CFFF)</description></item>
/// <item><description>Language Card RAM state checks for ROM traps ($D000-$FFFF)</description></item>
/// </list>
/// </remarks>
public sealed class TrapRegistry : ITrapRegistry
{
    /// <summary>
    /// O(1) lookup for traps by address and operation.
    /// </summary>
    private readonly Dictionary<TrapKey, TrapEntry> traps = [];

    /// <summary>
    /// Category enable/disable state. Contains disabled categories.
    /// </summary>
    private readonly HashSet<TrapCategory> disabledCategories = [];

    /// <summary>
    /// Slot manager for context checks.
    /// </summary>
    private readonly ISlotManager? slotManager;

    /// <summary>
    /// Language Card controller for LC RAM state checks.
    /// </summary>
    private readonly LanguageCardController? languageCard;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrapRegistry"/> class.
    /// </summary>
    public TrapRegistry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrapRegistry"/> class
    /// with slot manager for context-aware trap execution.
    /// </summary>
    /// <param name="slotManager">The slot manager for slot dependency checks.</param>
    public TrapRegistry(ISlotManager slotManager)
    {
        this.slotManager = slotManager;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrapRegistry"/> class
    /// with slot manager and Language Card controller for full context-awareness.
    /// </summary>
    /// <param name="slotManager">The slot manager for slot dependency checks.</param>
    /// <param name="languageCard">The Language Card controller for RAM state checks.</param>
    public TrapRegistry(ISlotManager slotManager, LanguageCardController? languageCard)
    {
        this.slotManager = slotManager;
        this.languageCard = languageCard;
    }

    /// <inheritdoc />
    public int Count => traps.Count;

    /// <inheritdoc />
    public void Register(
        Addr address,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        Register(address, TrapOperation.Call, name, category, handler, description);
    }

    /// <inheritdoc />
    public void Register(
        Addr address,
        TrapOperation operation,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(handler);

        var key = new TrapKey(address, operation);
        if (traps.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"A trap for operation '{operation}' is already registered at address ${address:X4}.");
        }

        traps[key] = new TrapEntry
        {
            Address = address,
            Operation = operation,
            Name = name,
            Category = category,
            Handler = handler,
            Description = description,
        };
    }

    /// <inheritdoc />
    public void RegisterSlotDependent(
        Addr address,
        int slot,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        RegisterSlotDependent(address, TrapOperation.Call, slot, name, category, handler, description);
    }

    /// <inheritdoc />
    public void RegisterSlotDependent(
        Addr address,
        TrapOperation operation,
        int slot,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(handler);
        ValidateSlotNumber(slot);

        var key = new TrapKey(address, operation);
        if (traps.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"A trap for operation '{operation}' is already registered at address ${address:X4}.");
        }

        // Determine if this is in expansion ROM space ($C800-$CFFF)
        bool requiresExpansionRom = address >= 0xC800 && address <= 0xCFFF;

        traps[key] = new TrapEntry
        {
            Address = address,
            Operation = operation,
            Name = name,
            Category = category,
            Handler = handler,
            Description = description,
            SlotNumber = slot,
            RequiresExpansionRom = requiresExpansionRom,
        };
    }

    /// <inheritdoc />
    public bool Unregister(Addr address)
    {
        return Unregister(address, TrapOperation.Call);
    }

    /// <inheritdoc />
    public bool Unregister(Addr address, TrapOperation operation)
    {
        var key = new TrapKey(address, operation);
        return traps.Remove(key);
    }

    /// <summary>
    /// Unregisters all traps associated with a specific slot.
    /// </summary>
    /// <param name="slot">The slot number (1-7).</param>
    /// <remarks>
    /// This method is useful when removing a slot card to clean up all
    /// associated trap handlers.
    /// </remarks>
    public void UnregisterSlotTraps(int slot)
    {
        ValidateSlotNumber(slot);

        var keysToRemove = traps
            .Where(kvp => kvp.Value.SlotNumber == slot)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            traps.Remove(key);
        }
    }

    /// <inheritdoc />
    public TrapResult TryExecute(Addr address, ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        return TryExecute(address, TrapOperation.Call, cpu, bus, context);
    }

    /// <inheritdoc />
    public TrapResult TryExecute(Addr address, TrapOperation operation, ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        var key = new TrapKey(address, operation);
        if (!traps.TryGetValue(key, out var entry))
        {
            return TrapResult.NotHandled;
        }

        // Check if trap is enabled
        if (!entry.IsEnabled)
        {
            return TrapResult.NotHandled;
        }

        // Check if category is disabled
        if (disabledCategories.Contains(entry.Category))
        {
            return TrapResult.NotHandled;
        }

        // Check context conditions
        if (!CheckContext(entry))
        {
            return TrapResult.NotHandled;
        }

        // Execute the handler
        return entry.Handler(cpu, bus, context);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTrap(Addr address)
    {
        return HasTrap(address, TrapOperation.Call);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTrap(Addr address, TrapOperation operation)
    {
        var key = new TrapKey(address, operation);
        return traps.ContainsKey(key);
    }

    /// <inheritdoc />
    public TrapInfo? GetTrapInfo(Addr address)
    {
        return GetTrapInfo(address, TrapOperation.Call);
    }

    /// <inheritdoc />
    public TrapInfo? GetTrapInfo(Addr address, TrapOperation operation)
    {
        var key = new TrapKey(address, operation);
        if (traps.TryGetValue(key, out var entry))
        {
            return entry.ToTrapInfo();
        }

        return null;
    }

    /// <inheritdoc />
    public bool SetEnabled(Addr address, bool enabled)
    {
        return SetEnabled(address, TrapOperation.Call, enabled);
    }

    /// <inheritdoc />
    public bool SetEnabled(Addr address, TrapOperation operation, bool enabled)
    {
        var key = new TrapKey(address, operation);
        if (traps.TryGetValue(key, out var entry))
        {
            entry.IsEnabled = enabled;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public int SetCategoryEnabled(TrapCategory category, bool enabled)
    {
        int count = 0;

        if (enabled)
        {
            // Remove from disabled set
            if (disabledCategories.Remove(category))
            {
                // Count traps in this category
                count = traps.Values.Count(e => e.Category == category);
            }
        }
        else
        {
            // Add to disabled set
            if (disabledCategories.Add(category))
            {
                // Count traps in this category
                count = traps.Values.Count(e => e.Category == category);
            }
        }

        return count;
    }

    /// <inheritdoc />
    public IEnumerable<Addr> GetRegisteredAddresses()
    {
        return traps.Keys.Select(k => k.Address).Distinct();
    }

    /// <inheritdoc />
    public IEnumerable<TrapInfo> GetAllTraps()
    {
        return traps.Values.Select(e => e.ToTrapInfo());
    }

    /// <inheritdoc />
    public void Clear()
    {
        traps.Clear();
    }

    /// <summary>
    /// Validates that a slot number is in the valid range (1-7).
    /// </summary>
    /// <param name="slot">The slot number to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="slot"/> is not in the range 1-7.
    /// </exception>
    private static void ValidateSlotNumber(int slot)
    {
        if (slot < 1 || slot > 7)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                "Slot number must be between 1 and 7.");
        }
    }

    /// <summary>
    /// Checks if the context conditions for a trap are met.
    /// </summary>
    /// <param name="entry">The trap entry to check.</param>
    /// <returns><see langword="true"/> if the trap should fire; otherwise, <see langword="false"/>.</returns>
    private bool CheckContext(TrapEntry entry)
    {
        // Check slot dependency for slot ROM traps
        if (entry.SlotNumber is { } slot)
        {
            if (slotManager is null)
            {
                return false;
            }

            // Check if the slot has a card installed
            if (slotManager.GetCard(slot) is null)
            {
                return false;
            }

            // Check expansion ROM selection for $C800-$CFFF traps
            if (entry.RequiresExpansionRom)
            {
                if (slotManager.ActiveExpansionSlot != slot)
                {
                    return false;
                }
            }
        }

        // Check Language Card state for $D000-$FFFF traps
        if (entry.Address >= 0xD000 && languageCard is not null)
        {
            // If LC RAM is enabled for reading, the ROM isn't visible
            if (languageCard.IsRamReadEnabled)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Key for the trap dictionary combining address and operation type.
    /// </summary>
    private readonly record struct TrapKey(Addr Address, TrapOperation Operation);

    /// <summary>
    /// Internal storage for a registered trap entry.
    /// </summary>
    private sealed class TrapEntry
    {
        /// <summary>
        /// Gets or sets the ROM address where this trap is registered.
        /// </summary>
        public required Addr Address { get; set; }

        /// <summary>
        /// Gets or sets the operation type that triggers this trap.
        /// </summary>
        public required TrapOperation Operation { get; set; }

        /// <summary>
        /// Gets or sets the human-readable name for the trap.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the classification of the trap for filtering.
        /// </summary>
        public required TrapCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the native implementation delegate.
        /// </summary>
        public required TrapHandler Handler { get; set; }

        /// <summary>
        /// Gets or sets the optional detailed description for tooling.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this trap is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the slot number for slot-dependent traps.
        /// </summary>
        public int? SlotNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this trap requires expansion ROM.
        /// </summary>
        public bool RequiresExpansionRom { get; set; }

        /// <summary>
        /// Creates a TrapInfo from this entry.
        /// </summary>
        /// <returns>The trap information.</returns>
        public TrapInfo ToTrapInfo() => new(
            Address,
            Name,
            Category,
            Operation,
            Handler,
            Description,
            IsEnabled,
            SlotNumber);
    }
}