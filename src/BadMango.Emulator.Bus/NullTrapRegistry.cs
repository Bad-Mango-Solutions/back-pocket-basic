// <copyright file="NullTrapRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Core.Interfaces.Cpu;

using Interfaces;

/// <summary>
/// A no-op implementation of <see cref="ITrapRegistry"/> that contains no traps.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the null object pattern, providing a safe default
/// trap registry that never matches any address. It eliminates the need for
/// null checks in the CPU hot loop by always returning "not handled" for all
/// operations.
/// </para>
/// <para>
/// All query methods return empty results or <see langword="false"/>.
/// Registration methods throw <see cref="NotSupportedException"/> because
/// the null registry is not intended to hold traps.
/// </para>
/// </remarks>
public sealed class NullTrapRegistry : ITrapRegistry
{
    /// <summary>
    /// Gets the shared singleton instance of <see cref="NullTrapRegistry"/>.
    /// </summary>
    public static readonly NullTrapRegistry Instance = new();

    /// <inheritdoc />
    public int Count => 0;

    /// <inheritdoc />
    public void Register(
        Addr address,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        throw new NotSupportedException("Cannot register traps on the null trap registry.");
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
        throw new NotSupportedException("Cannot register traps on the null trap registry.");
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
        throw new NotSupportedException("Cannot register traps on the null trap registry.");
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
        throw new NotSupportedException("Cannot register traps on the null trap registry.");
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
        throw new NotSupportedException("Cannot register traps on the null trap registry.");
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
        throw new NotSupportedException("Cannot register traps on the null trap registry.");
    }

    /// <inheritdoc />
    public void RegisterLanguageCardRam(
        Addr address,
        string name,
        TrapCategory category,
        TrapHandler handler,
        string? description = null)
    {
        throw new NotSupportedException("Cannot register traps on the null trap registry.");
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
        throw new NotSupportedException("Cannot register traps on the null trap registry.");
    }

    /// <inheritdoc />
    public bool Unregister(Addr address) => false;

    /// <inheritdoc />
    public bool Unregister(Addr address, TrapOperation operation) => false;

    /// <inheritdoc />
    public bool UnregisterWithContext(Addr address, MemoryContext memoryContext) => false;

    /// <inheritdoc />
    public bool UnregisterWithContext(Addr address, TrapOperation operation, MemoryContext memoryContext) => false;

    /// <inheritdoc />
    public bool UnregisterLanguageCardRam(Addr address) => false;

    /// <inheritdoc />
    public bool UnregisterLanguageCardRam(Addr address, TrapOperation operation) => false;

    /// <inheritdoc />
    public void UnregisterSlotTraps(int slot)
    {
    }

    /// <inheritdoc />
    public void UnregisterContextTraps(MemoryContext memoryContext)
    {
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TrapResult TryExecute(Addr address, ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        return TrapResult.NotHandled;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TrapResult TryExecute(Addr address, TrapOperation operation, ICpu cpu, IMemoryBus bus, IEventContext context)
    {
        return TrapResult.NotHandled;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsAddress(Addr address) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTrap(Addr address) => false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasTrap(Addr address, TrapOperation operation) => false;

    /// <inheritdoc />
    public TrapInfo? GetTrapInfo(Addr address) => null;

    /// <inheritdoc />
    public TrapInfo? GetTrapInfo(Addr address, TrapOperation operation) => null;

    /// <inheritdoc />
    public TrapInfo? GetTrapInfo(Addr address, TrapOperation operation, MemoryContext memoryContext) => null;

    /// <inheritdoc />
    public IEnumerable<TrapInfo> GetTrapsAtAddress(Addr address) => [];

    /// <inheritdoc />
    public IEnumerable<TrapInfo> GetTrapsAtAddress(Addr address, TrapOperation operation) => [];

    /// <inheritdoc />
    public bool SetEnabled(Addr address, bool enabled) => false;

    /// <inheritdoc />
    public bool SetEnabled(Addr address, TrapOperation operation, bool enabled) => false;

    /// <inheritdoc />
    public bool SetEnabled(Addr address, TrapOperation operation, bool enabled, MemoryContext memoryContext) => false;

    /// <inheritdoc />
    public int SetCategoryEnabled(TrapCategory category, bool enabled) => 0;

    /// <inheritdoc />
    public IEnumerable<Addr> GetRegisteredAddresses() => [];

    /// <inheritdoc />
    public IEnumerable<TrapInfo> GetAllTraps() => [];

    /// <inheritdoc />
    public IEnumerable<MemoryContext> GetRegisteredContexts() => [];

    /// <inheritdoc />
    public void Clear()
    {
    }
}