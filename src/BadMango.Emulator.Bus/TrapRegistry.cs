// <copyright file="TrapRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Cpu;

using Interfaces;

/// <summary>
/// High-performance trap registry with O(1) lookup for ROM routine interception.
/// </summary>
/// <remarks>
/// <para>
/// The trap registry enables native implementations of ROM routines and I/O operations
/// for performance optimization. Traps are organized by address, operation type, and
/// memory context.
/// </para>
/// <para>
/// The registry is context-aware, supporting:
/// </para>
/// <list type="bullet">
/// <item><description>Slot dependency checks for slot ROM traps ($Cn00-$CnFF)</description></item>
/// <item><description>Expansion ROM selection checks for expansion ROM traps ($C800-$CFFF)</description></item>
/// <item><description>Memory context checks for different RAM banks (LC RAM, Aux RAM, ProDOS, etc.)</description></item>
/// </list>
/// </remarks>
public sealed class TrapRegistry : ITrapRegistry, ITrapRegistryObserver
{
    /// <summary>
    /// O(1) lookup for traps by address, operation, and memory context.
    /// </summary>
    private readonly Dictionary<TrapKey, TrapEntry> traps = [];

    /// <summary>
    /// O(1) address-only index for fast pre-screening in the CPU hot loop.
    /// Maps each trapped address to the number of traps registered at that address,
    /// enabling reference-counted maintenance during register/unregister operations.
    /// </summary>
    private readonly Dictionary<Addr, int> trappedAddresses = [];

    /// <summary>
    /// Category enable/disable state. Contains disabled categories.
    /// </summary>
    private readonly HashSet<TrapCategory> disabledCategories = [];

    /// <summary>
    /// Slot manager for context checks.
    /// </summary>
    private readonly ISlotManager? slotManager;

    /// <summary>
    /// Language Card state for LC RAM state checks.
    /// </summary>
    private readonly ILanguageCardState? languageCard;

    /// <summary>
    /// Optional delegate to determine the active memory context for an address.
    /// </summary>
    private readonly Func<Addr, MemoryContext>? memoryContextResolver;

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
    /// with slot manager and Language Card state for full context-awareness.
    /// </summary>
    /// <param name="slotManager">The slot manager for slot dependency checks.</param>
    /// <param name="languageCard">The Language Card state for RAM state checks.</param>
    public TrapRegistry(ISlotManager slotManager, ILanguageCardState? languageCard)
    {
        this.slotManager = slotManager;
        this.languageCard = languageCard;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrapRegistry"/> class
    /// with a custom memory context resolver for advanced memory configurations.
    /// </summary>
    /// <param name="slotManager">The slot manager for slot dependency checks.</param>
    /// <param name="languageCard">The Language Card state for RAM state checks.</param>
    /// <param name="memoryContextResolver">
    /// A delegate that determines the active memory context for a given address.
    /// If null, the default behavior uses Language Card state for $D000-$FFFF addresses.
    /// </param>
    public TrapRegistry(
        ISlotManager? slotManager,
        ILanguageCardState? languageCard,
        Func<Addr, MemoryContext>? memoryContextResolver)
    {
        this.slotManager = slotManager;
        this.languageCard = languageCard;
        this.memoryContextResolver = memoryContextResolver;
    }

    /// <inheritdoc cref="ITrapRegistryObserver.TrapRegistered" />
    public event Action<TrapInfo>? TrapRegistered;

    /// <inheritdoc cref="ITrapRegistryObserver.TrapUnregistered" />
    public event Action<Addr, TrapOperation, MemoryContext>? TrapUnregistered;

    /// <inheritdoc cref="ITrapRegistryObserver.TrapInvoked" />
    public event Action<TrapInfo, TrapResult, Cycle>? TrapInvoked;

    /// <inheritdoc cref="ITrapRegistryObserver.TrapEnabledChanged" />
    public event Action<Addr, TrapOperation, MemoryContext, bool>? TrapEnabledChanged;

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
        RegisterWithContext(address, operation, MemoryContexts.Rom, name, category, handler, description);
    }

    /// <inheritdoc />
    public void RegisterWithContext(
        Addr address,
        MemoryContext memoryContext,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        RegisterWithContext(address, TrapOperation.Call, memoryContext, name, category, handler, description);
    }

    /// <inheritdoc />
    public void RegisterWithContext(
        Addr address,
        TrapOperation operation,
        MemoryContext memoryContext,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(handler);

        var key = new TrapKey(address, operation, memoryContext);
        if (traps.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"A trap for operation '{operation}' in context '{memoryContext}' is already registered at address ${address:X4}.");
        }

        var entry = new TrapEntry
        {
            Address = address,
            Operation = operation,
            Name = name,
            Category = category,
            Handler = handler,
            Description = description,
            MemoryContext = memoryContext,
        };

        traps[key] = entry;

        // Update address index
        AddToAddressIndex(address);

        // Notify observers
        TrapRegistered?.Invoke(entry.ToTrapInfo());
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

        var key = new TrapKey(address, operation, MemoryContexts.Rom);
        if (traps.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"A trap for operation '{operation}' is already registered at address ${address:X4}.");
        }

        // Determine if this is in expansion ROM space ($C800-$CFFF)
        bool requiresExpansionRom = address >= 0xC800 && address <= 0xCFFF;

        var entry = new TrapEntry
        {
            Address = address,
            Operation = operation,
            Name = name,
            Category = category,
            Handler = handler,
            Description = description,
            SlotNumber = slot,
            RequiresExpansionRom = requiresExpansionRom,
            MemoryContext = MemoryContexts.Rom,
        };

        traps[key] = entry;

        // Update address index
        AddToAddressIndex(address);

        // Notify observers
        TrapRegistered?.Invoke(entry.ToTrapInfo());
    }

    /// <inheritdoc />
    public void RegisterLanguageCardRam(
        Addr address,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        RegisterLanguageCardRam(address, TrapOperation.Call, name, category, handler, description);
    }

    /// <inheritdoc />
    public void RegisterLanguageCardRam(
        Addr address,
        TrapOperation operation,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        RegisterWithContext(address, operation, MemoryContexts.LanguageCardRam, name, category, handler, description);
    }

    /// <inheritdoc />
    public bool Unregister(Addr address)
    {
        return Unregister(address, TrapOperation.Call);
    }

    /// <inheritdoc />
    public bool Unregister(Addr address, TrapOperation operation)
    {
        return UnregisterWithContext(address, operation, MemoryContexts.Rom);
    }

    /// <inheritdoc />
    public bool UnregisterWithContext(Addr address, MemoryContext memoryContext)
    {
        return UnregisterWithContext(address, TrapOperation.Call, memoryContext);
    }

    /// <inheritdoc />
    public bool UnregisterWithContext(Addr address, TrapOperation operation, MemoryContext memoryContext)
    {
        var key = new TrapKey(address, operation, memoryContext);
        var removed = traps.Remove(key);
        if (removed)
        {
            RemoveFromAddressIndex(address);
            TrapUnregistered?.Invoke(address, operation, memoryContext);
        }

        return removed;
    }

    /// <inheritdoc />
    public bool UnregisterLanguageCardRam(Addr address)
    {
        return UnregisterLanguageCardRam(address, TrapOperation.Call);
    }

    /// <inheritdoc />
    public bool UnregisterLanguageCardRam(Addr address, TrapOperation operation)
    {
        return UnregisterWithContext(address, operation, MemoryContexts.LanguageCardRam);
    }

    /// <inheritdoc />
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
            RemoveFromAddressIndex(key.Address);
            TrapUnregistered?.Invoke(key.Address, key.Operation, key.MemoryContext);
        }
    }

    /// <inheritdoc />
    public void UnregisterContextTraps(MemoryContext memoryContext)
    {
        var keysToRemove = traps
            .Where(kvp => kvp.Value.MemoryContext == memoryContext)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            traps.Remove(key);
            RemoveFromAddressIndex(key.Address);
            TrapUnregistered?.Invoke(key.Address, key.Operation, key.MemoryContext);
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
        // Determine which memory context is active
        var activeContext = ResolveMemoryContext(address);

        // Try the appropriate trap based on memory context
        var key = new TrapKey(address, operation, activeContext);
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

        // Check context conditions (for slot-dependent traps)
        if (!CheckContext(entry))
        {
            return TrapResult.NotHandled;
        }

        // Execute the handler
        var result = entry.Handler(cpu, bus, context);

        // Notify observers after execution
        TrapInvoked?.Invoke(entry.ToTrapInfo(), result, context.Scheduler.Now);

        return result;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsAddress(Addr address)
    {
        return trappedAddresses.ContainsKey(address);
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
        // Fast pre-check: if no trap exists at this address at all, skip the key scan
        if (!trappedAddresses.ContainsKey(address))
        {
            return false;
        }

        // Check if any trap exists at this address for the specific operation
        return traps.Keys.Any(k => k.Address == address && k.Operation == operation);
    }

    /// <inheritdoc />
    public TrapInfo? GetTrapInfo(Addr address)
    {
        return GetTrapInfo(address, TrapOperation.Call);
    }

    /// <inheritdoc />
    public TrapInfo? GetTrapInfo(Addr address, TrapOperation operation)
    {
        return GetTrapInfo(address, operation, MemoryContexts.Rom);
    }

    /// <inheritdoc />
    public TrapInfo? GetTrapInfo(Addr address, TrapOperation operation, MemoryContext memoryContext)
    {
        var key = new TrapKey(address, operation, memoryContext);
        if (traps.TryGetValue(key, out var entry))
        {
            return entry.ToTrapInfo();
        }

        return null;
    }

    /// <inheritdoc />
    public IEnumerable<TrapInfo> GetTrapsAtAddress(Addr address)
    {
        return GetTrapsAtAddress(address, TrapOperation.Call);
    }

    /// <inheritdoc />
    public IEnumerable<TrapInfo> GetTrapsAtAddress(Addr address, TrapOperation operation)
    {
        return traps
            .Where(kvp => kvp.Key.Address == address && kvp.Key.Operation == operation)
            .Select(kvp => kvp.Value.ToTrapInfo());
    }

    /// <inheritdoc />
    public bool SetEnabled(Addr address, bool enabled)
    {
        return SetEnabled(address, TrapOperation.Call, enabled);
    }

    /// <inheritdoc />
    public bool SetEnabled(Addr address, TrapOperation operation, bool enabled)
    {
        return SetEnabled(address, operation, enabled, MemoryContexts.Rom);
    }

    /// <inheritdoc />
    public bool SetEnabled(Addr address, TrapOperation operation, bool enabled, MemoryContext memoryContext)
    {
        var key = new TrapKey(address, operation, memoryContext);
        if (traps.TryGetValue(key, out var entry))
        {
            var previousState = entry.IsEnabled;
            entry.IsEnabled = enabled;

            // Notify observers if state changed
            if (previousState != enabled)
            {
                TrapEnabledChanged?.Invoke(address, operation, memoryContext, enabled);
            }

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
    public IEnumerable<MemoryContext> GetRegisteredContexts()
    {
        return traps.Keys.Select(k => k.MemoryContext).Distinct();
    }

    /// <inheritdoc />
    public void Clear()
    {
        traps.Clear();
        trappedAddresses.Clear();
        disabledCategories.Clear();
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
    /// Resolves the active memory context for an address.
    /// </summary>
    /// <param name="address">The address to resolve.</param>
    /// <returns>The active memory context for the address.</returns>
    private MemoryContext ResolveMemoryContext(Addr address)
    {
        // Use custom resolver if provided
        if (memoryContextResolver is not null)
        {
            return memoryContextResolver(address);
        }

        // Default behavior: check Language Card state for $D000-$FFFF
        if (address >= 0xD000 && languageCard is not null && languageCard.IsRamReadEnabled)
        {
            return MemoryContexts.LanguageCardRam;
        }

        return MemoryContexts.Rom;
    }

    /// <summary>
    /// Checks if the context conditions for a trap are met.
    /// </summary>
    /// <param name="entry">The trap entry to check.</param>
    /// <returns><see langword="true"/> if the trap should fire; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This method checks slot-dependent conditions. Memory context selection is handled
    /// in the TryExecute method by selecting the appropriate trap key based on
    /// the active memory context.
    /// </remarks>
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
            if (entry.RequiresExpansionRom && slotManager.ActiveExpansionSlot != slot)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Adds an address to the address index, incrementing its reference count.
    /// </summary>
    /// <param name="address">The address to add.</param>
    private void AddToAddressIndex(Addr address)
    {
        trappedAddresses[address] = trappedAddresses.TryGetValue(address, out var count)
            ? count + 1
            : 1;
    }

    /// <summary>
    /// Removes an address from the address index, decrementing its reference count.
    /// Removes the entry entirely when the count reaches zero.
    /// </summary>
    /// <param name="address">The address to remove.</param>
    private void RemoveFromAddressIndex(Addr address)
    {
        if (trappedAddresses.TryGetValue(address, out var count))
        {
            if (count <= 1)
            {
                trappedAddresses.Remove(address);
            }
            else
            {
                trappedAddresses[address] = count - 1;
            }
        }
    }

    /// <summary>
    /// Key for the trap dictionary combining address, operation type, and memory context.
    /// </summary>
    /// <param name="Address">The memory address.</param>
    /// <param name="Operation">The operation type (read, write, call).</param>
    /// <param name="MemoryContext">The memory context this trap targets.</param>
    private readonly record struct TrapKey(Addr Address, TrapOperation Operation, MemoryContext MemoryContext);

    /// <summary>
    /// Internal storage for a registered trap entry.
    /// </summary>
    private sealed class TrapEntry
    {
        /// <summary>
        /// Gets or sets the memory address where this trap is registered.
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
        /// Gets or sets the memory context this trap targets.
        /// </summary>
        public MemoryContext MemoryContext { get; set; } = MemoryContexts.Rom;

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
            SlotNumber,
            MemoryContext);
    }
}